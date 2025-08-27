using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuizManager : MonoBehaviour
{
    [Header("Data & UI")]
    public List<QuestionAndAnswer> QnA = new List<QuestionAndAnswer>();
    public GameObject[] options;
    public TextMeshProUGUI questionText;

    [Header("Panels")]
    public GameObject startPanel;
    public GameObject quizPanel;

    [Header("Name Input")]
    public TMP_InputField nameInput;
    public string playerName = "Player";
    const string KeyPlayerName = "PLAYER_NAME";

    [Header("HUD")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI progressText;

    [Header("Streak Bonus")]
    public bool enableStreakBonus = true;
    public int streakThreshold = 3;
    public int streakBonusPoints = 5;
    public TextMeshProUGUI streakToast;

    [Header("Points")]
    public int pointsCorrect = 10;

    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip sfxCorrect;
    public AudioClip sfxWrong;

    [Header("Timing")]
    [SerializeField] float nextQuestionDelay = 0.30f;

    [Header("Flow")]
    public bool advanceOnWrong       = true;
    public bool revealCorrectOnWrong = true;

    [Header("Colours")]
    public Color normalColor  = Color.white;
    public Color correctColor = new Color(0.6f, 1f, 0.6f, 1f);
    public Color wrongColor   = new Color(1f, 0.6f, 0.6f, 1f);

    [Header("Results & Leaderboard Panels")]
    public GameObject resultsPanel;
    public TextMeshProUGUI resultsScoreText;
    public TextMeshProUGUI resultsPercentText;
    public TextMeshProUGUI resultsStreakText;
    public TextMeshProUGUI resultsHeaderText;
    public GameObject leaderboardPanel;
    public TextMeshProUGUI leaderboardText;

    [Header("Page Flip (optional)")]
    public BookFlipController bookFlip; // drag your RightPage_Flip here (optional)

    // runtime
    List<QuestionAndAnswer> pool;
    int index = 0;
    bool acceptingInput = false;
    int score = 0;
    int totalQuestions = 0;
    int correctCount = 0;
    int currentStreak = 0;
    int bestStreak = 0;

    void Awake()
    {
        if (!sfxSource) sfxSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (resultsPanel) resultsPanel.SetActive(false);
        if (leaderboardPanel) leaderboardPanel.SetActive(false);
        if (quizPanel) quizPanel.SetActive(false);
        if (startPanel) startPanel.SetActive(true);

        if (nameInput)
            nameInput.text = PlayerPrefs.GetString(KeyPlayerName, "");
    }

    // UI: Start pressed
    public void OnStartPressed()
    {
        var entered = nameInput ? nameInput.text.Trim() : "";
        playerName = string.IsNullOrEmpty(entered) ? "Player" : entered;

        PlayerPrefs.SetString(KeyPlayerName, playerName);
        PlayerPrefs.Save();

        PrepareAndStartQuiz();
    }

    // UI: Play Again
    public void RestartQuiz()
    {
        PrepareAndStartQuiz();
    }

    // Optional: clear leaderboard button
    public void OnClearLeaderboardClicked()
    {
        LeaderboardService.ClearAll();
        RefreshLeaderboardUI();
    }

    void PrepareAndStartQuiz()
    {
        if (startPanel) startPanel.SetActive(false);
        if (resultsPanel) resultsPanel.SetActive(false);
        if (leaderboardPanel) leaderboardPanel.SetActive(false);
        if (quizPanel) quizPanel.SetActive(true);

        pool = new List<QuestionAndAnswer>(QnA);
        Shuffle(pool);

        index = 0;
        score = 0;
        correctCount = 0;
        currentStreak = 0;
        bestStreak = 0;
        totalQuestions = pool.Count;

        UpdateHUD();
        ShowCurrent();
    }

    // Fisher–Yates
    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    void ShowCurrent()
    {
        if (pool == null || pool.Count == 0 || index >= pool.Count)
        {
            EndQuiz();
            return;
        }

        var qa = pool[index];

        // validate entry
        if (qa == null ||
            string.IsNullOrWhiteSpace(qa.Question) ||
            qa.Answers == null ||
            qa.Answers.Length < options.Length ||
            qa.CorrectAnswer < 1 || qa.CorrectAnswer > options.Length)
        {
            Debug.LogWarning($"Bad QnA at index {index}. Skipping.");
            index++;
            ShowCurrent();
            return;
        }

        if (questionText) questionText.text = qa.Question;

        for (int i = 0; i < options.Length; i++)
        {
            var go = options[i];
            if (!go) continue;

            var img = go.GetComponent<Image>();
            if (img) img.color = normalColor;

            var a = go.GetComponent<AnswerScript>();
            if (a)
            {
                a.ResetState();
                a.isCorrect   = (qa.CorrectAnswer == i + 1);
                a.quizManager = this;
                // keep colors in sync with AnswerScript if it exposes them
                a.normalColor  = normalColor;
                a.correctColor = correctColor;
                a.wrongColor   = wrongColor;
            }

            var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label) label.text = qa.Answers[i];

            var b = go.GetComponent<Button>();
            if (b) b.interactable = true;
        }

        UpdateProgress();
        SetButtons(true);
        acceptingInput = true;
        if (streakToast) streakToast.gameObject.SetActive(false);
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
        int current = Mathf.Clamp(index + 1, 0, Mathf.Max(totalQuestions, 1));
        progressText.text = $"{current} / {totalQuestions}";
    }

    // ===== Called from AnswerScript =====
    public void Correct()
    {
        if (!acceptingInput) return;
        acceptingInput = false;

        SetButtons(false);

        score += pointsCorrect;
        correctCount += 1;

        currentStreak += 1;
        if (currentStreak > bestStreak) bestStreak = currentStreak;

        if (enableStreakBonus && streakThreshold > 0 && (currentStreak % streakThreshold == 0))
        {
            score += streakBonusPoints;
            if (streakToast)
            {
                streakToast.text = $"+{streakBonusPoints} Streak!";
                streakToast.gameObject.SetActive(true);
            }
        }

        UpdateHUD();
        if (sfxSource && sfxCorrect) sfxSource.PlayOneShot(sfxCorrect);

        StartCoroutine(NextQuestionAfterDelay());
    }

    public void Wrong()
    {
        if (!acceptingInput) return;
        acceptingInput = false;

        currentStreak = 0;

        if (revealCorrectOnWrong) RevealCorrect();
        if (sfxSource && sfxWrong) sfxSource.PlayOneShot(sfxWrong);

        SetButtons(false);

        if (advanceOnWrong)
            StartCoroutine(NextQuestionAfterDelay());
        else
            StartCoroutine(UnlockAfterDelay(0.25f));
    }

    IEnumerator NextQuestionAfterDelay()
    {
        if (bookFlip)
        {
            // Flip and advance index at mid-flip
            yield return StartCoroutine(bookFlip.FlipToNext(() =>
            {
                index++;
            }));
            ShowCurrent();
        }
        else
        {
            yield return new WaitForSeconds(nextQuestionDelay);
            index++;
            ShowCurrent();
        }
    }

    IEnumerator UnlockAfterDelay(float t)
    {
        yield return new WaitForSeconds(t);
        acceptingInput = true;
        SetButtons(true);
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

    void EndQuiz()
    {
        SetButtons(false);

        if (quizPanel)    quizPanel.SetActive(false);
        if (resultsPanel) resultsPanel.SetActive(true);

        float percent = (totalQuestions > 0) ? (100f * correctCount / totalQuestions) : 0f;

        if (resultsHeaderText)  resultsHeaderText.text  = $"Nice run, {playerName}!";
        if (resultsScoreText)   resultsScoreText.text   = $"Score: {score}";
        if (resultsPercentText) resultsPercentText.text = $"{percent:0}%";
        if (resultsStreakText)  resultsStreakText.text  = $"Best Streak: {bestStreak}";

        var entry = new LeaderboardEntry
        {
            name      = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName,
            score     = score,
            bestStreak= bestStreak,
            dateISO   = DateTime.Now.ToString("yyyy-MM-dd")
        };
        LeaderboardService.AddEntry(entry);

        RefreshLeaderboardUI();
    }
