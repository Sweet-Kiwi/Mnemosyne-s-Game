using UnityEngine;
using System.Collections.Generic;    // for List<T>
using UnityEngine.UI;   //for text
using TMPro;

public class QuizManager : MonoBehaviour
{
    
    [Header("Populate in Inspector")]
    public List<QuestionAndAnswer> QnA = new List<QuestionAndAnswer>();
    public GameObject[] options;
    // replace the old Text field:
    public TextMeshProUGUI questionText;

    // tracks which question we're on
    private int currentQuestion;
    
    private void Start()
    {
        GenerateQuestion();
    }

    void GenerateQuestion()
    {
        if (QnA.Count == 0)
        {
            Debug.LogWarning("No questions available in QnA!");
            return;
        }

        currentQuestion = Random.Range(0, QnA.Count);
        questionText.text = QnA[currentQuestion].Question;
        SetAnswers();
    }
    
    public void Correct()
    {
        GenerateQuestion();
    }

    void SetAnswers()
    {
        for (int i = 0; i < options.Length; i++)
        {
            // reset all to false first
            var answerScript = options[i].GetComponent<AnswerScript>();
            answerScript.isCorrect = false;
            
            var txt = options[i]
                .transform
                .GetChild(0)
                .GetComponent<TextMeshProUGUI>();
            
            if (QnA[currentQuestion].CorrectAnswer == i + 1)
                answerScript.isCorrect = true;
        }

        void GenerateQuestion()
        {
            currentQuestion = Random.Range(0, QnA.Count);
            questionText.text = QnA[currentQuestion].Question;
            SetAnswers();
        }
    }
}
