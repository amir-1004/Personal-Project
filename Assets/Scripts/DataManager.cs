using System.IO;
using UnityEngine;

// Keeps data alive between scenes (player name) and between sessions (high score).
public class DataManager : MonoBehaviour
{
    static DataManager instance;

    public static DataManager Instance
    {
        get
        {
            // Created on demand so the game scene also works when played directly.
            if (instance == null)
            {
                var go = new GameObject("DataManager");
                instance = go.AddComponent<DataManager>();
            }
            return instance;
        }
    }

    public string PlayerName = "Player";
    public string BestPlayerName = "";
    public int BestScore;

    string SavePath => Path.Combine(Application.persistentDataPath, "savefile.json");

    [System.Serializable]
    class SaveData
    {
        public string BestPlayerName;
        public int BestScore;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        LoadHighScore();
    }

    // Returns true when the score is a new high score.
    public bool SubmitScore(int score)
    {
        if (score <= BestScore) return false;

        BestScore = score;
        BestPlayerName = PlayerName;
        SaveHighScore();
        return true;
    }

    public string BestScoreText =>
        BestScore > 0 ? $"Best Score : {BestPlayerName} : {BestScore}" : "Best Score : none yet";

    public void SaveHighScore()
    {
        var data = new SaveData { BestPlayerName = BestPlayerName, BestScore = BestScore };
        File.WriteAllText(SavePath, JsonUtility.ToJson(data));
    }

    public void LoadHighScore()
    {
        if (!File.Exists(SavePath)) return;

        var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        BestPlayerName = data.BestPlayerName;
        BestScore = data.BestScore;
    }
}
