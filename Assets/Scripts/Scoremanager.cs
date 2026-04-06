using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI movesText;
    public Slider progressBar;

    [Header("Level Settings")]
    public int targetScore = 1000;
    public int movesLeft = 30;

    private int currentScore = 0;
    private int highScore = 0;

    void Awake()
    {
        instance = this;
        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    void Start()
    {
        UpdateUI();
        Debug.Log("✅ ScoreManager iniciado correctamente.");
    }

    public void AddScore(int points)
    {
        currentScore += points;
        Debug.Log($"🍬 +{points} puntos → Total: {currentScore}");

        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", highScore);
        }

        StartCoroutine(AnimateScore());
        UpdateUI();
        CheckWin();
    }

    public void UseMove()
    {
        movesLeft--;
        UpdateUI();
        Debug.Log($"Movimientos restantes: {movesLeft}");
        if (movesLeft <= 0 && currentScore < targetScore)
            GameOver();
    }

    void CheckWin()
    {
        if (currentScore >= targetScore)
            Debug.Log("🏆 ¡NIVEL COMPLETADO!");
    }

    void GameOver()
    {
        Debug.Log("💀 GAME OVER");
    }

    IEnumerator AnimateScore()
    {
        if (scoreText != null)
        {
            scoreText.transform.localScale = Vector3.one * 1.3f;
            yield return new WaitForSeconds(0.1f);
            scoreText.transform.localScale = Vector3.one;
        }
    }

    void UpdateUI()
    {
        if (scoreText) scoreText.text = currentScore.ToString("N0");
        if (highScoreText) highScoreText.text = "Best: " + highScore.ToString("N0");
        if (movesText) movesText.text = movesLeft.ToString();
        if (progressBar) progressBar.value = (float)currentScore / targetScore;
    }

    public int GetScore() => currentScore;
}