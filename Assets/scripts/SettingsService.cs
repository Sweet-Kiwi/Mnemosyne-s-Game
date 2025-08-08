using UnityEngine;

public static class SettingsService
{
    const string KeyVolume = "SFX_VOLUME";
    const string KeyAdvanceOnWrong = "ADV_ON_WRONG";
    const string KeyRevealCorrect = "REV_CORRECT";

    public static void SaveVolume(float v) => PlayerPrefs.SetFloat(KeyVolume, Mathf.Clamp01(v));
    public static float LoadVolume(float def = 1f) => PlayerPrefs.GetFloat(KeyVolume, def);

    public static void SaveAdvanceOnWrong(bool v) => PlayerPrefs.SetInt(KeyAdvanceOnWrong, v ? 1 : 0);
    public static bool LoadAdvanceOnWrong(bool def = true) => PlayerPrefs.GetInt(KeyAdvanceOnWrong, def ? 1 : 0) == 1;

    public static void SaveRevealCorrect(bool v) => PlayerPrefs.SetInt(KeyRevealCorrect, v ? 1 : 0);
    public static bool LoadRevealCorrect(bool def = true) => PlayerPrefs.GetInt(KeyRevealCorrect, def ? 1 : 0) == 1;

    public static void ApplyToQuizManager(QuizManager qm)
    {
        if (qm == null) return;
        qm.advanceOnWrong = LoadAdvanceOnWrong(qm.advanceOnWrong);
        qm.revealCorrectOnWrong = LoadRevealCorrect(qm.revealCorrectOnWrong);
        if (qm.sfxSource) qm.sfxSource.volume = LoadVolume(qm.sfxSource.volume);
    }
}