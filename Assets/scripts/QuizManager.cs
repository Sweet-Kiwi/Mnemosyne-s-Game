using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    [Header("Data & UI")]
    public List<QuestionAndAnswer> QnA = new List<QuestionAndAnswer>();  // Question, Answers[4], CorrectAnswer=1..4
    public GameObject[] options;                                          // 4 Button GameObjects
    public TextMeshProUGUI questionText;                                  // Big question label

    [Header("HUD")]
    public TextMeshProUGUI scoreText;     // <-- assign in Inspector
    public TextMeshProUGUI progressText;  // <-- assign in Inspector: "3 / 10"

    [Header("Points")]
    public int pointsCorrect = 10;
    public int pointsWrong = 0;           // set to -5 if you want penalties

    [Header("SFX")]
    public AudioSource sfxSource;         // <-- add AudioSource to QuizManager object and drag here
    public AudioClip sfxCorrect;          // <-- drag “correct” clip here
    public AudioClip sfxWrong;            // <-- drag “wrong” clip here

    [Header("Timing")]
    [SerializeField] float nextQuestionDelay = 0.30f;  // delay after correct
    [SerializeField] float wrongLockDelay = 0.25f;     // brief lock after wrong

    private List<QuestionAndAnswer> pool;   // shuffled working copy
    private int index = 0;                  // current question
    private bool acceptingInput = false;    // click guard
    private int score = 0;

    // Awake runs before Start
    void Awake()
    {
        if (!sfxSource)
            sfxSource = GetComponent<AudioSource>();
    }
    
    void Start()
    {
        pool = new List<QuestionAndAnswer>(QnA);
        Shuffle(pool);
        index = 0;
        score = 0;
        UpdateHUD();
        ShowCurrent();
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    void ShowCurrent()
    {
        if (pool == null || pool.Count == 0)
        {
            if (questionText) questionText.text = "No questions!";
            SetButtons(false);
            UpdateProgress(); // will say 0/0
            return;
        }
        if (index >= pool.Count)
        {
            if (questionText) questionText.text = "Quiz complete!";
            SetButtons(false);
            UpdateProgress();
            return;
        }

        var qa = pool[index];

        // validate entry so we never silently freeze
        if (qa == null ||
            qa.Answers == null ||
            qa.Answers.Length < options.Length ||
            qa.CorrectAnswer < 1 || qa.CorrectAnswer > options.Length ||
            string.IsNullOrWhiteSpace(qa.Question))
        {
            Debug.LogWarning($"Bad QnA at index {index}. Skipping this entry.");
            index++;
            ShowCurrent();
            return;
        }

        if (questionText) questionText.text = qa.Question;

        for (int i = 0; i < options.Length; i++)
        {
            var go = options[i];
            if (!go) continue;

            // reset state + assign correctness
            var a = go.GetComponent<AnswerScript>();
            if (a)
            {
                a.ResetState();
                a.isCorrect = (qa.CorrectAnswer == i + 1); // data is 1-based
                a.quizManager = this;
            }

            // set label
            var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label) label.text = qa.Answers[i];

            // ensure clickable
            var b = go.GetComponent<Button>();
            if (b) b.interactable = true;
        }

        UpdateProgress();
        SetButtons(true);
        acceptingInput = true;
    }

    void SetButtons(bool on)
    {
        foreach (var go in options)
        {
            var b = go ? go.GetComponent<Button>() : null;
            if (b) b.interactable = on;
        }
    }

    void UpdateHUD()
    {
        if (scoreText) scoreText.text = $"Score: {score}";
    }

    void UpdateProgress()
    {
        if (!progressText) return;
        int total = pool != null ? pool.Count : 0;
        int current = Mathf.Clamp(index + 1, 0, Mathf.Max(total, 1));
        progressText.text = $"{current} / {total}";
    }

    // called by AnswerScript when the right button is clicked
    public void Correct()
    {
        if (!acceptingInput) return;
        acceptingInput = false;
        SetButtons(false);

        score += pointsCorrect;
        UpdateHUD();
        if (sfxSource && sfxCorrect) sfxSource.PlayOneShot(sfxCorrect);

        StartCoroutine(NextQuestionAfterDelay());
    }

    IEnumerator NextQuestionAfterDelay()
    {
        yield return new WaitForSeconds(nextQuestionDelay);
        index++;            // move forward by exactly 1
        ShowCurrent();
    }

    // called by AnswerScript when a wrong button is clicked
    public void Wrong()
    {
        if (!acceptingInput) return;
        acceptingInput = false;   // small lock so spam doesn’t double-trigger
        score += pointsWrong;
        UpdateHUD();
        if (sfxSource && sfxWrong) sfxSource.PlayOneShot(sfxWrong);

        // stay on the same question, unlock shortly
        StartCoroutine(UnlockAfterDelay(wrongLockDelay));
    }

    IEnumerator UnlockAfterDelay(float t)
    {
        yield return new WaitForSeconds(t);
        acceptingInput = true;
        SetButtons(true);
    }
}
