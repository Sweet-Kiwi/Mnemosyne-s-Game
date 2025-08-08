using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class QuizManager : MonoBehaviour
{
    [Header("Populate in Inspector")]
    public List<QuestionAndAnswer> QnA = new List<QuestionAndAnswer>();   // your questions
    public GameObject[] options;                                           // 4 button GameObjects
    public TextMeshProUGUI questionText;                                   // the big question label

    // which question we’re on (index in QnA)
    private int currentQuestion = -1;

    private void Start()
    {
        GenerateQuestion();
    }

    public void GenerateQuestion()
    {
        if (QnA == null || QnA.Count == 0)
        {
            Debug.LogWarning("No questions available in QnA!", this);
            ClearUI();
            return;
        }

        currentQuestion = Random.Range(0, QnA.Count);

        // set the question
        if (questionText != null)
            questionText.text = QnA[currentQuestion].Question;

        SetAnswers();
    }

    private void SetAnswers()
    {
        if (currentQuestion < 0 || currentQuestion >= QnA.Count) return;

        // reset and fill all buttons
        for (int i = 0; i < options.Length; i++)
        {
            var btn = options[i];
            if (!btn) continue;

            // reset correctness
            var a = btn.GetComponent<AnswerScript>();
            if (a) a.isCorrect = false;

            // set label
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label)
            {
                if (i < QnA[currentQuestion].Answers.Length)
                    label.text = QnA[currentQuestion].Answers[i];
                else
                    label.text = "";
            }
        }

        // mark the correct option (CorrectAnswer is 1-based)
        int correctIndex = Mathf.Clamp(QnA[currentQuestion].CorrectAnswer - 1, 0, options.Length - 1);
        var correct = options[correctIndex] ? options[correctIndex].GetComponent<AnswerScript>() : null;
        if (correct) correct.isCorrect = true;
    }

    public void Correct()
    {
        Debug.Log("Correct!", this);
        // remove the used question so it doesn’t repeat
        if (currentQuestion >= 0 && currentQuestion < QnA.Count)
            QnA.RemoveAt(currentQuestion);

        GenerateQuestion();
    }

    public void Wrong()
    {
        Debug.Log("Wrong Answer", this);
        // try another question (or keep the same if you want)
        GenerateQuestion();
    }

    private void ClearUI()
    {
        if (questionText) questionText.text = "";
        foreach (var o in options)
        {
            var t = o ? o.GetComponentInChildren<TextMeshProUGUI>() : null;
            if (t) t.text = "";
            var a = o ? o.GetComponent<AnswerScript>() : null;
            if (a) a.isCorrect = false;
        }
    }
}
