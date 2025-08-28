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
    public List<QuestionAndAnswer> QnA = new List<QuestionAndAnswer>();   // question bank
    public GameObject[] options;                                           // 4 Button GameObjects
    public TextMeshProUGUI questionText;                                   // big question label

    [Header("Panels")]
    public GameObject startPanel;      // shown at launch
    public GameObject quizPanel;       // main quiz UI (question + options)

    [Header("Name Input")]
    public TMP_InputField nameInput;   // on the start screen
    public string playerName = "Player";
    const string KeyPlayerName = "PLAYER_NAME";

    [Header("HUD")]
    public TextMeshProUGUI scoreText;     // e.g. "Score: 30"
    public TextMeshProUGUI progressText;  // e.g. "3 / 10"

    [Header("Streak Bonus")]
    public bool enableStreakBonus = true;
    public int streakThreshold = 3;       // every 3 in a row…
    public int streakBonusPoints = 5;     // …add +5 points
    public TextMeshProUGUI streakToast;   // small “+5 Streak!” label (optional)

    [Header("Points")]
    public int pointsCorrect = 10;

    [Header("SFX")]
    public AudioSource sfxSource;         // add AudioSource to this object and drag here
    public AudioClip sfxCorrect;
    public AudioClip sfxWrong;

    [Header("Timing")]
    [SerializeField] float nextQuestionDelay = 0.30f;  // delay before moving on (both right & wrong)

    [Header("Flow")]
    public bool advanceOnWrong       = true;           // move to next even if wrong
    public bool revealCorrectOnWrong = true;           // flash the correct answer when wrong

    [Header("Colours")]
    public Color normalColor  = Color.white;
    public Color correctColor = new Color(0.6f, 1f, 0.6f, 1f);
    public Color wrongColor   = new Color(1f, 0.6f, 0.6f, 1f);

    [Header("Results & Leaderboard Panels")]
    public GameObject resultsPanel;             // panel to show at the end
    public TextMeshProUGUI resultsScoreText;    // "Score: 80"
    public TextMeshProUGUI resultsPercentText;  // "80%"
    public TextMeshProUGUI resultsStreakText;   // "Best Streak: 5"
    public TextMeshProUGUI resultsHeaderText;   // "Nice run, Alex!"

    public GameObject leaderboardPanel;         // simple panel to list top results
    public TextMeshProUGUI leaderboardText;     // multi-line top-10 display

    // runtime state
    List<QuestionAndAnswer> pool;   // shuffled working copy
    int index = 0;                  // current question idx
    bool acceptingInput = false;    // click guard
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
        // Show start; hide quiz + results + leaderboard
        if (resultsPanel) resultsPanel.SetActive(false);
        if (leaderboardPanel) leaderboardPanel.SetActive(false);
        if (quizPanel) quizPanel.SetActive(false);
        if (startPanel) startPanel.SetActive(true);

        // Pre-fill last name
        if (nameInput)
            nameInput.text = PlayerPrefs.GetString(KeyPlayerName, "");
    }

    // === UI: Start pressed (direct start, without router) ===
    public void OnStartPressed()
    {
        var entered = nameInput ? nameInput.text.Trim() : "";
        playerName = string.IsNullOrEmpty(entered) ? "Player" : entered;

        PlayerPrefs.SetString(KeyPlayerName, playerName);
        PlayerPrefs.Save();

        PrepareAndStartQuiz();
    }

    // === Called by BookPageRouter after the flip ===
    public void BeginQuizFromRouter()
    {
        if (nameInput != null)
        {
            var entered = nameInput.text.Trim();
            playerName = string.IsNullOrEmpty(entered) ? "Player" : entered;
            PlayerPrefs.SetString(KeyPlayerName, playerName);
            PlayerPrefs.Save();
        }

        PrepareAndStartQuiz();
    }

    // === UI: Play Again on results ===
    public void RestartQuiz()
    {
        PrepareAndStartQuiz();
    }

    // === Dev/Settings: Clear leaderboard button (optional) ===
    public void OnClearLeaderboardClicked()
    {
        LeaderboardService.ClearAll();
        RefreshLeaderboardUI(); // updates the on-screen list if visible
    }

    // Central reset
    public void PrepareAndStartQuiz()
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
                a.ResetState();
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

        // scoring
        score += pointsCorrect;
        correctCount += 1;

        // streak bonus
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

        // reset streak
        currentStreak = 0;

        // reveal the correct button (optional)
        if (revealCorrectOnWrong) RevealCorrect();

        if (sfxSource && sfxWrong) sfxSource.PlayOneShot(sfxWrong);

        SetButtons(false);

        if (advanceOnWrong)
        {
            StartCoroutine(NextQuestionAfterDelay());
        }
        else
        {
            StartCoroutine(UnlockAfterDelay(0.25f));
        }
    }

    IEnumerator NextQuestionAfterDelay()
    {
        yield return new WaitForSeconds(nextQuestionDelay);
        index++;            // advance by exactly one
        ShowCurrent();
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

        // Results UI
        if (quizPanel) quizPanel.SetActive(false);
        if (resultsPanel) resultsPanel.SetActive(true);

        float percent = (totalQuestions > 0) ? (100f * correctCount / totalQuestions) : 0f;

        if (resultsHeaderText)  resultsHeaderText.text  = $"Nice run, {playerName}!";
        if (resultsScoreText)   resultsScoreText.text   = $"Score: {score}";
        if (resultsPercentText) resultsPercentText.text = $"{percent:0}%";
        if (resultsStreakText)  resultsStreakText.text  = $"Best Streak: {bestStreak}";

        // Save to leaderboard
        var entry = new LeaderboardEntry
        {
            name = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName,
            score = score,
            bestStreak = bestStreak,
            dateISO = DateTime.Now.ToString("yyyy-MM-dd")
        };
        LeaderboardService.AddEntry(entry);

        // Populate on-screen leaderboard
        RefreshLeaderboardUI();
    }

    void RefreshLeaderboardUI()
    {
        if (leaderboardPanel) leaderboardPanel.SetActive(true);
        if (!leaderboardText) return;

        var top = LeaderboardService.GetTop(10);
        if (top.Count == 0)
        {
            leaderboardText.text = "No scores yet.";
            return;
        }

        StringBuilder sb = new StringBuilder();
        int rank = 1;
        foreach (var e in top)
        {
            sb.AppendLine($"{rank,2}. {e.name,-12}  {e.score,4}  (Streak {e.bestStreak})  {e.dateISO}");
            rank++;
        }
        leaderboardText.text = sb.ToString();
    }
}
