using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MenuUIHandler : MonoBehaviour
{
    public const string GameSceneName = "MyGame";

    public InputField nameInput;
    public Text bestScoreText;

    void Start()
    {
        var data = DataManager.Instance;
        nameInput.text = data.PlayerName == "Player" ? "" : data.PlayerName;
        bestScoreText.text = data.BestScoreText;
    }

    public void StartGame()
    {
        string playerName = nameInput.text.Trim();
        DataManager.Instance.PlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
        SceneManager.LoadScene(GameSceneName);
    }

    public void ResetHighScore()
    {
        var data = DataManager.Instance;
        data.BestScore = 0;
        data.BestPlayerName = "";
        data.SaveHighScore();
        bestScoreText.text = data.BestScoreText;
    }

    public void Exit()
    {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }
}
