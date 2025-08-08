using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class AnswerScript : MonoBehaviour
{
    [Header("Set by QuizManager each question")]
    public bool isCorrect = false;
    public QuizManager quizManager;

    [Header("Colors")]
    public Image background;                    // usually the Button's Image
    public Color normalColor  = Color.white;
    public Color correctColor = new Color(0.60f, 0.95f, 0.60f); // green
    public Color wrongColor   = new Color(1.00f, 0.70f, 0.70f); // red

    Button btn;

    void Awake()
    {
        btn = GetComponent<Button>();
        if (!background) background = GetComponent<Image>();
        // Use Inspector wiring: Button OnClick -> AnswerScript.Answer()
        // Make sure there's EXACTLY one entry.
    }

    public void Answer()
    {
        if (background) background.color = isCorrect ? correctColor : wrongColor;

        if (isCorrect) quizManager.Correct();
        else           quizManager.Wrong();
    }

    public void ResetState()
    {
        if (!background) background = GetComponent<Image>();
        if (background) background.color = normalColor;

        if (!btn) btn = GetComponent<Button>();
        if (btn) btn.interactable = true;
    }
}