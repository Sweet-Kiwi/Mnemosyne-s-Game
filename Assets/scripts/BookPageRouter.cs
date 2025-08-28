using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BookPageRouter : MonoBehaviour
{
    [Header("Flip Controller")]
    public BookFlipController bookFlip;   // drag the RightPage_Flip object (with BookFlipController)

    [Header("Panels")]
    public GameObject menuPanel;       // Start menu (Start/Settings/Languages)
    public GameObject quizPanel;       // The quiz UI
    public GameObject settingsPanel;   // Settings screen
    public GameObject languagesPanel;  // Languages screen

    [Header("Book Art (front-face image changes per page)")]
    public Image frontFaceImage;       // The Image on RightPage_Flip/FrontFace
    public Sprite menuSprite;
    public Sprite quizSprite;
    public Sprite settingsSprite;
    public Sprite languagesSprite;

    [Header("Quiz")]
    public QuizManager quiz;           // drag your QuizManager here

    void Start()
    {
        // Show only menu on load
        ShowOnly(menuPanel);
        ApplyBookSprite(menuSprite);
    }

    // ---------- public buttons ----------

    public void BeginQuiz()
    {
        if (!bookFlip) { // no animator? Just switch instantly
            ShowOnly(quizPanel);
            ApplyBookSprite(quizSprite);
            if (quiz) quiz.BeginQuizFromRouter();
            return;
        }

        // Flip, then at mid turn on quiz + start quiz logic
        StartCoroutine(bookFlip.FlipToNext(() =>
        {
            ShowOnly(quizPanel);
            ApplyBookSprite(quizSprite);
            if (quiz) quiz.BeginQuizFromRouter();
        }));
    }

    public void GoToSettings()
    {
        if (!bookFlip) {
            ShowOnly(settingsPanel);
            ApplyBookSprite(settingsSprite);
            return;
        }

        StartCoroutine(bookFlip.FlipToNext(() =>
        {
            ShowOnly(settingsPanel);
            ApplyBookSprite(settingsSprite);
        }));
    }

    public void GoToLanguages()
    {
        if (!bookFlip) {
            ShowOnly(languagesPanel);
            ApplyBookSprite(languagesSprite);
            return;
        }

        StartCoroutine(bookFlip.FlipToNext(() =>
        {
            ShowOnly(languagesPanel);
            ApplyBookSprite(languagesSprite);
        }));
    }

    public void BackToMenu()
    {
        if (!bookFlip) {
            ShowOnly(menuPanel);
            ApplyBookSprite(menuSprite);
            return;
        }

        StartCoroutine(bookFlip.FlipToNext(() =>
        {
            ShowOnly(menuPanel);
            ApplyBookSprite(menuSprite);
        }));
    }

    // ---------- helpers ----------

    void ShowOnly(GameObject target)
    {
        if (menuPanel)      menuPanel.SetActive(target == menuPanel);
        if (quizPanel)      quizPanel.SetActive(target == quizPanel);
        if (settingsPanel)  settingsPanel.SetActive(target == settingsPanel);
        if (languagesPanel) languagesPanel.SetActive(target == languagesPanel);
    }

    void ApplyBookSprite(Sprite s)
    {
        if (frontFaceImage) frontFaceImage.sprite = s;
    }
}
