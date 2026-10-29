using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds and controls the main-menu UI. Uses uGUI with the Input System UI module.
/// </summary>
public sealed class MainMenuController : MonoBehaviour
{
    [Header("Scene Navigation")]
    [SerializeField] private string gameplaySceneName = "Store";

    [Header("UI References")]
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button exitGameButton;

    private void Awake()
    {
        EnsureEventSystem();
        BuildMenuIfNeeded();

        startGameButton.onClick.RemoveListener(StartGame);
        startGameButton.onClick.AddListener(StartGame);
        exitGameButton.onClick.RemoveListener(ExitGame);
        exitGameButton.onClick.AddListener(ExitGame);
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EventSystem.current?.SetSelectedGameObject(startGameButton.gameObject);
    }

    public void StartGame()
    {
        if (!Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            Debug.LogError($"Main menu cannot load '{gameplaySceneName}'. Add it to Build Settings or assign a valid scene name.");
            return;
        }

        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        var eventSystemObject = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        eventSystemObject.transform.SetParent(transform, false);
    }

    private void BuildMenuIfNeeded()
    {
        if (menuCanvas != null)
            return;

        var canvasObject = new GameObject(
            "MainMenuCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        menuCanvas = canvasObject.GetComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menuCanvas.sortingOrder = 100;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var backdrop = CreateImage(
            "Backdrop",
            menuCanvas.transform,
            new Color(0.025f, 0.055f, 0.09f, 1f),
            false);
        StretchToFill(backdrop.rectTransform);

        var accent = CreateImage(
            "Accent",
            menuCanvas.transform,
            new Color(0.08f, 0.55f, 0.7f, 0.16f),
            false);
        var accentRect = accent.rectTransform;
        accentRect.anchorMin = new Vector2(0f, 0.78f);
        accentRect.anchorMax = new Vector2(1f, 0.79f);
        accentRect.offsetMin = Vector2.zero;
        accentRect.offsetMax = Vector2.zero;

        var card = CreateImage(
            "MenuCard",
            menuCanvas.transform,
            new Color(0.055f, 0.09f, 0.135f, 0.96f),
            false);
        var cardRect = card.rectTransform;
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(620f, 520f);
        cardRect.anchoredPosition = new Vector2(0f, -10f);

        var eyebrow = CreateLabel(
            "Eyebrow",
            card.transform,
            "WELCOME TO",
            20f,
            new Color(0.42f, 0.78f, 0.9f, 1f));
        SetCenteredRect(eyebrow.rectTransform, new Vector2(0f, 165f), new Vector2(480f, 36f));

        var title = CreateLabel(
            "Title",
            card.transform,
            "3D SANDBOX",
            64f,
            Color.white);
        title.fontStyle = FontStyles.Bold;
        SetCenteredRect(title.rectTransform, new Vector2(0f, 98f), new Vector2(560f, 82f));

        var subtitle = CreateLabel(
            "Subtitle",
            card.transform,
            "Choose an option to continue",
            22f,
            new Color(0.7f, 0.77f, 0.84f, 1f));
        SetCenteredRect(subtitle.rectTransform, new Vector2(0f, 38f), new Vector2(500f, 36f));

        startGameButton = CreateButton(
            "StartGameButton",
            card.transform,
            "START GAME",
            new Vector2(0f, -55f),
            new Color(0.05f, 0.52f, 0.7f, 1f));

        exitGameButton = CreateButton(
            "ExitGameButton",
            card.transform,
            "EXIT GAME",
            new Vector2(0f, -150f),
            new Color(0.48f, 0.12f, 0.16f, 1f));

        var version = CreateLabel(
            "Footer",
            card.transform,
            "PRESS ENTER TO START",
            15f,
            new Color(0.48f, 0.57f, 0.65f, 1f));
        SetCenteredRect(version.rectTransform, new Vector2(0f, -218f), new Vector2(480f, 30f));
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string text,
        Vector2 anchoredPosition,
        Color backgroundColor)
    {
        var image = CreateImage(name, parent, backgroundColor, true);
        var rectTransform = image.rectTransform;
        SetCenteredRect(rectTransform, anchoredPosition, new Vector2(420f, 66f));

        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        var label = CreateLabel("Label", image.transform, text, 26f, Color.white);
        label.fontStyle = FontStyles.Bold;
        StretchToFill(label.rectTransform);

        return button;
    }

    private static Image CreateImage(string name, Transform parent, Color color, bool raycastTarget)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        gameObject.transform.SetParent(parent, false);

        var image = gameObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private static TextMeshProUGUI CreateLabel(
        string name,
        Transform parent,
        string text,
        float fontSize,
        Color color)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(parent, false);

        var label = gameObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private static void SetCenteredRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }

    private static void StretchToFill(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}