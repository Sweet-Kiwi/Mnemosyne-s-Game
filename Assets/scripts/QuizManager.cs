using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    [Header("Populate in Inspector")]
    public List<QuestionAndAnswer> QnA = new List<QuestionAndAnswer>();  // your data (Question, Answers[4], CorrectAnswer=1..4)
    public GameObject[] options;                                          // 4 Button GameObjects
    public TextMeshProUGUI questionText;                                  // big question label

    [Header("Timing")]
    [SerializeField] float nextQuestionDelay = 0.30f;  // delay after correct before showing next
    [SerializeField] float wrongLockDelay = 0.25f;     // brief lock after wrong, so spam clicks don’t glitch

    private List<QuestionAndAnswer> pool;   // working copy (keeps your Inspector list intact)
    private int index = 0;                  // current question in pool
    private bool acceptingInput = false;    // input guard

    void Start()
    {
        // build a shuffled working pool
        pool = new List<QuestionAndAnswer>(QnA);
        Shuffle(pool);
        index = 0;
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
        // end states
        if (pool == null || pool.Count == 0)
        {
            if (questionText) questionText.text = "No questions!";
            SetButtons(false);
            return;
        }
        if (index >= pool.Count)
        {
            if (questionText) questionText.text = "Quiz complete!";
            SetButtons(false);
            return;
        }

        var qa = pool[index];

        // validate data so we never “silently skip”
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

        // set question text
        if (questionText) questionText.text = qa.Question;

        // fill/reset buttons
        for (int i = 0; i < options.Length; i++)
        {
            var go = options[i];
            if (!go) continue;

            var a = go.GetComponent<AnswerScript>();
            if (a)
            {
                a.quizManager = this;
                a.ResetState();                              // normal color, interactable = true
                a.isCorrect = (qa.CorrectAnswer == i + 1);   // data is 1-based
            }

            var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label)
            {
                // safe: Answers.Length has been checked above
                label.text = qa.Answers[i];
            }
        }

        SetButtons(true);
        acceptingInput = true;
        // Debug.Log($"Showing {index+1}/{pool.Count}: {qa.Question}");
    }

    void SetButtons(bool on)
    {
        foreach (var go in options)
        {
            var b = go ? go.GetComponent<Button>() : null;
            if (b) b.interactable = on;
        }
    }

    // called by AnswerScript if the clicked option was correct
    public void Correct()
    {
        if (!acceptingInput) return;
        acceptingInput = false;
        SetButtons(false);

        StartCoroutine(NextQuestionAfterDelay());
    }

    IEnumerator NextQuestionAfterDelay()
    {
        yield return new WaitForSeconds(nextQuestionDelay);
        index++;                // advance EXACTLY one
        ShowCurrent();
    }

    // called by AnswerScript if the clicked option was wrong
    public void Wrong()
    {
        if (!acceptingInput) return;
        acceptingInput = false;                       // brief lock so spam clicks don’t double-trigger
        StartCoroutine(UnlockAfterDelay(wrongLockDelay));
        // stays on the same question; you can add SFX or shake here if you want
    }

    IEnumerator UnlockAfterDelay(float t)
    {
        yield return new WaitForSeconds(t);
        acceptingInput = true;
        SetButtons(true);
    }
}
