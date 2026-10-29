using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Handles the ESC-key pause menu with Resume, Audio slider, and Exit Game.
/// Uses only the New Input System. Disables player movement/camera during pause.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }
    public bool IsPaused { get; private set; }

    // ──────────────────────────────────────────────
    //  Inspector Refs (all auto-built if null)
    // ──────────────────────────────────────────────
    [Header("UI References (auto-built if left empty)")]
    [SerializeField] private Canvas pauseCanvas;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Slider audioSlider;
    [SerializeField] private Button exitButton;
    [SerializeField] private TMP_FontAsset menuFont;

    [Header("Audio (optional – uses AudioListener volume if no mixer)")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string mixerExposedParam = "MasterVolume";

    // ──────────────────────────────────────────────
    //  Private
    // ──────────────────────────────────────────────
    private MonoBehaviour playerInputHandler;
    private MonoBehaviour playerCameraController;
    private MonoBehaviour cinemachineAxisController;

    // ──────────────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ──────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        EnsureEventSystem();
        BuildUI();
        SetPauseVisibility(false);
    }

    private void Start()
    {
        // Grab player components
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            foreach (var mb in player.GetComponents<MonoBehaviour>())
            {
                var typeName = mb.GetType().Name;
                if (typeName == "PlayerInputHandler") playerInputHandler = mb;
                if (typeName == "PlayerCameraController") playerCameraController = mb;
            }
        }

        // Grab Cinemachine input controller so mouse look stops during pause
        var fpCam = GameObject.Find("FirstPersonCam");
        if (fpCam != null)
        {
            foreach (var mb in fpCam.GetComponents<MonoBehaviour>())
            {
                if (mb.GetType().Name == "CinemachineInputAxisController")
                    cinemachineAxisController = mb;
            }
        }

        // Ensure slider initial value is hooked up
        if (audioSlider != null)
        {
            float saved = PlayerPrefs.GetFloat("MasterVolume", 1f);
            audioSlider.value = saved;
            OnVolumeChanged(saved);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────────────────────────────────
    public void TogglePause()
    {
        if (IsPaused) Resume(); else Pause();
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        SetPlayerInputActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetPauseVisibility(true);
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        SetPlayerInputActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetPauseVisibility(false);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Audio
    // ──────────────────────────────────────────────────────────────────────────
    public void OnVolumeChanged(float normalizedValue)
    {
        if (audioMixer != null)
        {
            float db = normalizedValue > 0.0001f ? Mathf.Log10(normalizedValue) * 20f : -80f;
            audioMixer.SetFloat(mixerExposedParam, db);
        }
        else
        {
            AudioListener.volume = normalizedValue;
        }

        PlayerPrefs.SetFloat("MasterVolume", normalizedValue);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────────────────────
    private void SetPlayerInputActive(bool active)
    {
        if (playerInputHandler != null) playerInputHandler.enabled = active;
        if (playerCameraController != null) playerCameraController.enabled = active;
        if (cinemachineAxisController != null) cinemachineAxisController.enabled = active;
    }

    private void SetPauseVisibility(bool visible)
    {
        if (pausePanel == null)
            return;

        pausePanel.transform.SetAsLastSibling();
        pausePanel.SetActive(visible);

        if (visible && resumeButton != null)
            EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  UI Construction
    // ──────────────────────────────────────────────────────────────────────────
    private void BuildUI()
    {
        EnsurePauseCanvas();

        if (menuFont == null)
        {
            var hudMgr = FindFirstObjectByType<HeistHUDManager>();
            if (hudMgr != null)
            {
                var fontField = typeof(HeistHUDManager).GetField("hudFont", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fontField != null) menuFont = fontField.GetValue(hudMgr) as TMP_FontAsset;
            }
        }

        // ── If pausePanel already exists in the scene, place it on the dedicated
        //    overlay canvas and just hook up listeners. ─────────────────────────
        if (pausePanel != null)
        {
            if (pausePanel.transform.parent != pauseCanvas.transform)
                pausePanel.transform.SetParent(pauseCanvas.transform, false);

            ResetPausePanelLayout();

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(Resume);
                resumeButton.onClick.AddListener(Resume);
            }
            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(ExitGame);
                exitButton.onClick.AddListener(ExitGame);
            }
            if (audioSlider != null)
            {
                audioSlider.onValueChanged.RemoveListener(OnVolumeChanged);
                audioSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
            return;
        }

        // ── Full-screen dim panel ─────────────────────────────────────────────
        pausePanel = CreatePanel("PausePanel", pauseCanvas.transform, new Color(0f, 0f, 0f, 0.8f));

        // ── Menu card ─────────────────────────────────────────────────────────
        var card = CreateCard("MenuCard", pausePanel.transform);

        // ── Title ─────────────────────────────────────────────────────────────
        CreateLabel("PAUSED", card.transform, 56f, new Vector2(0f, 130f), new Vector2(400f, 60f));

        // ── Resume button ─────────────────────────────────────────────────────
        resumeButton = CreateButton("ResumeButton", card.transform, "RESUME", new Vector2(0f, 40f));
        resumeButton.onClick.AddListener(Resume);

        // ── Audio section label ───────────────────────────────────────────────
        CreateLabel("AUDIO VOLUME", card.transform, 20f,
            new Vector2(0f, -30f), new Vector2(300f, 30f), new Color(0.75f, 0.75f, 0.75f));

        // ── Audio slider ──────────────────────────────────────────────────────
        audioSlider = CreateSlider("AudioSlider", card.transform, new Vector2(0f, -70f));
        float savedVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
        audioSlider.value = savedVol;
        OnVolumeChanged(savedVol);
        audioSlider.onValueChanged.AddListener(OnVolumeChanged);

        // ── Exit button ───────────────────────────────────────────────────────
        exitButton = CreateButton("ExitButton", card.transform, "EXIT GAME", new Vector2(0f, -140f), new Color(0.75f, 0.2f, 0.2f));
        exitButton.onClick.AddListener(ExitGame);
    }

    /// <summary>
    /// Uses an independent overlay canvas so full-screen HUD graphics cannot
    /// capture pointer events before the pause controls receive them.
    /// </summary>
    private void EnsurePauseCanvas()
    {
        if (pauseCanvas != null && pauseCanvas.gameObject.name == "PauseMenuCanvas")
            return;

        var existingCanvas = GameObject.Find("PauseMenuCanvas");
        if (existingCanvas != null)
        {
            pauseCanvas = existingCanvas.GetComponent<Canvas>();
            if (pauseCanvas != null)
                return;
        }

        var canvasObject = new GameObject(
            "PauseMenuCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        pauseCanvas = canvasObject.GetComponent<Canvas>();
        pauseCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        pauseCanvas.sortingOrder = 100;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
    }

    private void ResetPausePanelLayout()
    {
        var rectTransform = pausePanel.GetComponent<RectTransform>();
        if (rectTransform == null)
            return;

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.anchoredPosition3D = Vector3.zero;
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  UI Builder Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false; // Don't block button clicks!
        return go;
    }

    private static RectTransform CreateCard(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(480f, 400f);
        rt.anchoredPosition = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = new Color(0.06f, 0.06f, 0.06f, 0.95f);
        img.raycastTarget = false; // Don't block button clicks!
        return rt;
    }

    private TextMeshProUGUI CreateLabel(string text, Transform parent, float fontSize,
        Vector2 anchoredPos, Vector2 sizeDelta, Color? color = null)
    {
        var go = new GameObject(text + "_Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color ?? Color.white;
        tmp.raycastTarget = false;
        if (menuFont != null) tmp.font = menuFont;
        return tmp;
    }

    private Button CreateButton(string name, Transform parent, string label,
        Vector2 anchoredPos, Color? bgColor = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(300f, 50f);

        var img = go.GetComponent<Image>();
        Color normalBg = bgColor ?? new Color(0.25f, 0.25f, 0.25f, 0.95f);
        img.color = normalBg;
        img.raycastTarget = true;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor = normalBg;
        cb.highlightedColor = normalBg + new Color(0.2f, 0.2f, 0.2f, 0f);
        cb.pressedColor = normalBg - new Color(0.1f, 0.1f, 0.1f, 0f);
        btn.colors = cb;

        var txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(go.transform, false);
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
        var tmp = txtGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false; // MUST be false so button gets the raycast!
        if (menuFont != null) tmp.font = menuFont;

        return btn;
    }

    private Slider CreateSlider(string name, Transform parent, Vector2 anchoredPos)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 0.5f);
        rootRt.anchorMax = new Vector2(0.5f, 0.5f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.anchoredPosition = anchoredPos;
        rootRt.sizeDelta = new Vector2(320f, 24f);

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        var bgImg = bg.GetComponent<Image>();
        bgImg.color = new Color(0.12f, 0.12f, 0.12f);
        bgImg.raycastTarget = true;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        var fillAreaRt = fillArea.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0f, 0.2f);
        fillAreaRt.anchorMax = new Vector2(1f, 0.8f);
        fillAreaRt.sizeDelta = new Vector2(-16f, 0f);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        var fillImg = fill.GetComponent<Image>();
        fillImg.color = new Color(0.3f, 0.7f, 1f);
        fillImg.raycastTarget = false;

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(root.transform, false);
        var handleAreaRt = handleArea.GetComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.sizeDelta = new Vector2(-16f, 0f);

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        var handleRt = handle.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(20f, 24f);
        var handleImg = handle.GetComponent<Image>();
        handleImg.color = Color.white;
        handleImg.raycastTarget = true;

        var slider = root.GetComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        return slider;
    }
}
