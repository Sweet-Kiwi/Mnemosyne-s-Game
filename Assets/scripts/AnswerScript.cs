using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class AnswerScript : MonoBehaviour
{
    [Header("Set by QuizManager")]
    public bool isCorrect = false;        // QuizManager flips this on the right button
    public QuizManager quizManager;       // drag your QuizManager here

    [Header("Optional feedback (assign the Button's Image)")]
    public Image background;              // usually the same Image that the Button uses
    public Color normalColor  = Color.white;
    public Color correctColor = new Color(0.65f, 1f, 0.65f);
    public Color wrongColor   = new Color(1f, 0.65f, 0.65f);

    private Button _btn;

    private void Awake()
    {
        _btn = GetComponent<Button>();
        if (!background) background = GetComponent<Image>();
    }

    private void OnEnable()
    {
        // If this object is disabled/enabled between questions,
        // reset visuals automatically.
        ResetState();
    }

    /// <summary>Called by the Button's OnClick.</summary>
    public void Answer()
    {
        if (_btn) _btn.interactable = false;

        if (isCorrect)
        {
            if (background) background.color = correctColor;
            quizManager.Correct();
        }
        else
        {
            if (background) background.color = wrongColor;
            quizManager.Wrong();
        }
    }

    /// <summary>Use this to reset visuals before showing a new question.</summary>
    public void ResetState()
    {
        if (background) background.color = normalColor;
        if (_btn) _btn.interactable = true;
        // isCorrect will be set by QuizManager when it assigns answers
    }
}
