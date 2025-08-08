using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    [Header("Data & UI")]
    public List<QuestionAndAnswer> QnA = new List<QuestionAndAnswer>();   // Question, Answers[4], CorrectAnswer=1..4
    public GameObject[] options;                                           // 4 Button GameObjects
    public TextMeshProUGUI questionText;                                   // Big question label

    [Header("HUD")]
    public TextMeshProUGUI scoreText;     // e.g. "Score: 30"
    public TextMeshProUGUI progressText;  // e.g. "3 / 10"

    [Header("Points")]
    public int pointsCorrect = 10;
    public int pointsWrong   = 0;         // set negative for penalties

    [Header("SFX")]
    public AudioSource sfxSource;         // add AudioSource to QuizManager object and drag here
    public AudioClip sfxCorrect;          // drop correct sound
    public AudioClip sfxWrong;            // drop wrong sound

    [Header("Timing")]
    [SerializeField] float nextQuestionDelay = 0.30f;  // delay before moving on
    [SerializeField] float wrongLockDelay    = 0.25f;  // if not advancing on wrong

    [Header("Flow")]
    public bool advanceOnWrong       = true;           // move to next even if wrong
    public bool revealCorrectOnWrong = true;           // flash the correct answer when wrong

    [Header("Colours")]
    public Color normalColor  = Color.white;
    public Color correctColor = new Color(0.6f, 1f, 0.6f, 1f);
    public Color wrongColor   = new Color(1f, 0.6f, 0.6f, 1f);

    // runtime
    private List<QuestionAndAnswer> pool;  // shuffled working copy
    private int   index = 0;               // current question idx
    private bool  acceptingInput = false;  // click guard
    private int   score = 0;

    void Awake()
    {
        if (!sfxSource) sfxSource = GetComponent<AudioSource>();
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

    // Fisher–Yates
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
            UpdateProgress();
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

        // validate entry so we never freeze silently
        if (qa == null ||
            string.IsNullOrWhiteSpace(qa.Question) ||
            qa.Answers == null ||
            qa.Answers.Length < options.Length ||
            qa.CorrectAnswer < 1 || qa.CorrectAnswer > options.Length)
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

            // reset visuals
            var img = go.GetComponent<Image>();
            if (img) img.color = normalColor;

            // set up answer script
            var a = go.GetComponent<AnswerScript>();
            if (a)
            {
                a.ResetState();                          // safe if it just resets visuals
                a.isCorrect   = (qa.CorrectAnswer == i + 1);
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
        int total   = pool != null ? pool.Count : 0;
        int current = Mathf.Clamp(index + 1, 0, Mathf.Max(total, 1));
        progressText.text = $"{current} / {total}";
    }

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
        index++;            // advance by exactly one
        ShowCurrent();
    }

    public void Wrong()
    {
        if (!acceptingInput) return;
        acceptingInput = false;

        score += pointsWrong;   // usually 0, or negative if you want penalties
        UpdateHUD();

        if (sfxSource && sfxWrong) sfxSource.PlayOneShot(sfxWrong);

        if (revealCorrectOnWrong) RevealCorrect();

        SetButtons(false);

        if (advanceOnWrong)
        {
            StartCoroutine(NextQuestionAfterDelay());
        }
        else
        {
            StartCoroutine(UnlockAfterDelay(wrongLockDelay));
        }
    }

    void RevealCorrect()
    {
        foreach (var go in options)
        {
            if (!go) continue;
            var a = go.GetComponent<AnswerScript>();
            if (a && a.isCorrect)
            {
                var img = go.GetComponent<Image>();
                if (img) img.color = correctColor;
            }
        }
    }

    IEnumerator UnlockAfterDelay(float t)
    {
        yield return new WaitForSeconds(t);
        acceptingInput = true;
        SetButtons(true);
    }
}
