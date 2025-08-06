using UnityEngine;
[System.Serializable]

public class QuestionAndAnswer
{
    public string Question;           // the question text
    public string[] Answers;          // <-- this array of answer strings
    public int CorrectAnswer;         // the 1-based index of the correct answer
}