using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Builds the Menu scene and the game scene UI for the data persistence feature.
// Run from Tools > Data Persistence > Build Scenes (safe to re-run).
public static class DataPersistenceSetup
{
    const string MenuScenePath = "Assets/Scenes/Menu.unity";
    const string GameScenePath = "Assets/Scenes/MyGame.unity";

    static Font font;
    static Sprite uiSprite;

    [MenuItem("Tools/Data Persistence/Build Scenes")]
    public static void BuildScenes()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        BuildMenuScene();
        SetupGameScene();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true),
        };

        EditorSceneManager.OpenScene(MenuScenePath);
        Debug.Log("[DataPersistence] Menu scene built, game scene UI added, build settings updated.");
    }

    static void BuildMenuScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var camera = Object.FindFirstObjectByType<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.14f, 0.18f);

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        SceneManager.MoveGameObjectToScene(eventSystem, scene);

        Transform canvas = CreateCanvas("MenuCanvas", scene);
        var handler = canvas.gameObject.AddComponent<MenuUIHandler>();

        CreateText(canvas, "Title", "ROCK DODGER", 90, new Vector2(0, 300), new Vector2(1200, 140));
        handler.bestScoreText = CreateText(canvas, "BestScoreText", "Best Score :", 44, new Vector2(0, 180), new Vector2(1200, 80));
        CreateText(canvas, "NameLabel", "Enter your name", 36, new Vector2(0, 80), new Vector2(800, 60));
        handler.nameInput = CreateInputField(canvas, new Vector2(0, 10), new Vector2(600, 80));

        AddClick(CreateButton(canvas, "StartButton", "Start", new Vector2(0, -110)), handler.StartGame);
        AddClick(CreateButton(canvas, "ResetButton", "Reset High Score", new Vector2(0, -220)), handler.ResetHighScore);
        AddClick(CreateButton(canvas, "ExitButton", "Exit", new Vector2(0, -330)), handler.Exit);

        EditorSceneManager.SaveScene(scene, MenuScenePath);
    }

    static void SetupGameScene()
    {
        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

        // Remove a previous run's objects so re-running doesn't duplicate them.
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "GameManager" || root.name == "GameCanvas")
                Object.DestroyImmediate(root);
        }

        Transform canvas = CreateCanvas("GameCanvas", scene);

        var managerObject = new GameObject("GameManager");
        SceneManager.MoveGameObjectToScene(managerObject, scene);
        var manager = managerObject.AddComponent<GameManager>();

        manager.scoreText = CreateText(canvas, "ScoreText", "Score : 0", 40, Vector2.zero, new Vector2(1000, 70),
            new Vector2(0, 1), TextAnchor.UpperLeft);
        manager.scoreText.rectTransform.anchoredPosition = new Vector2(30, -20);

        manager.bestScoreText = CreateText(canvas, "BestScoreText", "Best Score :", 40, Vector2.zero, new Vector2(900, 70),
            new Vector2(1, 1), TextAnchor.UpperRight);
        manager.bestScoreText.rectTransform.anchoredPosition = new Vector2(-30, -20);

        var panel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas, false);
        var panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);
        manager.gameOverPanel = panel;
        manager.gameOverText = CreateText(panel.transform, "GameOverText", "GAME OVER", 60, Vector2.zero, new Vector2(1400, 500));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static Transform CreateCanvas(string name, Scene scene)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(go, scene);
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return go.transform;
    }

    static Text CreateText(Transform parent, string name, string text, int size, Vector2 position, Vector2 boxSize,
        Vector2? anchor = null, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);
        rect.anchorMin = a;
        rect.anchorMax = a;
        rect.pivot = a;
        rect.anchoredPosition = position;
        rect.sizeDelta = boxSize;

        var label = go.GetComponent<Text>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = Color.white;
        return label;
    }

    static InputField CreateInputField(Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject("NameInput", typeof(RectTransform), typeof(Image), typeof(InputField));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.sprite = uiSprite;
        image.type = Image.Type.Sliced;

        Text placeholder = CreateInputText(go.transform, "Placeholder", "Your name...", new Color(0.5f, 0.5f, 0.5f));
        placeholder.fontStyle = FontStyle.Italic;
        Text text = CreateInputText(go.transform, "Text", "", Color.black);
        text.supportRichText = false;

        var input = go.GetComponent<InputField>();
        input.textComponent = text;
        input.placeholder = placeholder;
        input.characterLimit = 16;
        return input;
    }

    static Text CreateInputText(Transform parent, string name, string value, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(20, 8);
        rect.offsetMax = new Vector2(-20, -8);

        var label = go.GetComponent<Text>();
        label.font = font;
        label.text = value;
        label.fontSize = 38;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = color;
        return label;
    }

    static Button CreateButton(Transform parent, string name, string label, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(420, 85);
        var image = go.GetComponent<Image>();
        image.sprite = uiSprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.95f, 0.65f, 0.2f);

        Text text = CreateText(go.transform, "Text", label, 36, Vector2.zero, rect.sizeDelta);
        text.color = Color.black;
        Object.DestroyImmediate(text.GetComponent<Shadow>());
        return go.GetComponent<Button>();
    }

    static void AddClick(Button button, UnityAction action)
    {
        UnityEventTools.AddPersistentListener(button.onClick, action);
    }
}
