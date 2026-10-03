using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Score, lives and game over for the game scene.
public class GameManager : MonoBehaviour
{
    public const string MenuSceneName = "Menu";

    public static GameManager Instance { get; private set; }

    public int startLives = 3;
    public float pointsPerSecond = 10f;

    public Text scoreText;
    public Text bestScoreText;
    public GameObject gameOverPanel;
    public Text gameOverText;

    public bool IsGameOver { get; private set; }

    float score;
    int lives;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        lives = startLives;
        gameOverPanel.SetActive(false);
        bestScoreText.text = DataManager.Instance.BestScoreText;
        UpdateScoreText();
    }

    void Update()
    {
        if (IsGameOver)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.rKey.wasPressedThisFrame) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            if (keyboard.mKey.wasPressedThisFrame) SceneManager.LoadScene(MenuSceneName);
            return;
        }

        score += pointsPerSecond * Time.deltaTime;
        UpdateScoreText();
    }

    public void LoseLife()
    {
        if (IsGameOver) return;

        lives--;
        UpdateScoreText();
        if (lives <= 0) GameOver();
    }

    public void AddLife()
    {
        if (IsGameOver) return;

        lives++;
        UpdateScoreText();
    }

    void GameOver()
    {
        IsGameOver = true;
        FindFirstObjectByType<SpawnManager>()?.CancelInvoke();

        var data = DataManager.Instance;
        bool newBest = data.SubmitScore((int)score);
        bestScoreText.text = data.BestScoreText;

        gameOverText.text = (newBest ? "NEW HIGH SCORE!\n" : "GAME OVER\n") +
                            $"{data.PlayerName} : {(int)score}\n\nR - Restart    M - Menu";
        gameOverPanel.SetActive(true);
    }

    void UpdateScoreText()
    {
        scoreText.text = $"{DataManager.Instance.PlayerName}   Score : {(int)score}   Lives : {lives}";
    }
}
