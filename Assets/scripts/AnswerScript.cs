using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class AnswerScript : MonoBehaviour
{
    [Header("Set by QuizManager each question")]
    public bool isCorrect = false;
    public QuizManager quizManager;

    [Header("Colors (assign or keep defaults)")]
    public Image background;                    // usually the Button's Image; if left empty we'll try to get it
    public Color normalColor  = Color.white;    // default button color
    public Color correctColor = new Color(0.60f, 0.95f, 0.60f); // pale green
    public Color wrongColor   = new Color(1.00f, 0.70f, 0.70f); // pale red

    Button btn;

    void Awake()
    {
        btn = GetComponent<Button>();
        if (!background) background = GetComponent<Image>();
        // IMPORTANT: We’re using the Inspector’s OnClick() to call Answer() once.
        // Make sure the Button’s OnClick list has EXACTLY one entry: this.Answer().
    }

    public void Answer()
    {
        // color feedback on the clicked button
        if (background) background.color = isCorrect ? correctColor : wrongColor;

        // notify manager
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