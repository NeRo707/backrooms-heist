using TMPro;
using UnityEngine;

/// <summary>
/// Displays Battery and Stamina text readouts on the VHS camcorder HUD.
/// Placed right above the AM 12:00 / Date text on the bottom right.
/// No ugly colored bars — pure retro text.
/// </summary>
public class GameplayHUDExtension : MonoBehaviour
{
    public static GameplayHUDExtension Instance { get; private set; }

    [Header("UI Text References")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private TextMeshProUGUI batteryText;
    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private TMP_FontAsset hudFont;

    private float _targetStamina = 1f;
    private float _currentStamina = 1f;
    private float _targetBattery = 1f;
    private float _currentBattery = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildUIIfNeeded();
    }

    private void OnEnable()
    {
        StaminaSystem.OnStaminaChanged += HandleStaminaChanged;
        FlashlightBattery.OnBatteryChanged += HandleBatteryChanged;
    }

    private void OnDisable()
    {
        StaminaSystem.OnStaminaChanged -= HandleStaminaChanged;
        FlashlightBattery.OnBatteryChanged -= HandleBatteryChanged;
    }

    private void HandleStaminaChanged(float normalized)
    {
        _targetStamina = Mathf.Clamp01(normalized);
    }

    private void HandleBatteryChanged(float normalized)
    {
        _targetBattery = Mathf.Clamp01(normalized);
    }

    private void Update()
    {
        _currentStamina = Mathf.Lerp(_currentStamina, _targetStamina, Time.deltaTime * 12f);
        _currentBattery = Mathf.Lerp(_currentBattery, _targetBattery, Time.deltaTime * 12f);

        if (batteryText != null)
        {
            int pct = Mathf.RoundToInt(_currentBattery * 100f);
            if (_currentBattery <= 0.15f)
            {
                // Low battery warning: blink every half second
                bool blinkOn = (Mathf.FloorToInt(Time.time * 3f) % 2) == 0;
                batteryText.text = blinkOn ? $"<color=#FF4444>BATTERY  {pct}%</color>" : "";
            }
            else if (_currentBattery <= 0.3f)
            {
                batteryText.text = $"<color=#FFAA44>BATTERY  {pct}%</color>";
            }
            else
            {
                batteryText.text = $"BATTERY  {pct}%";
            }
        }

        if (staminaText != null)
        {
            int pct = Mathf.RoundToInt(_currentStamina * 100f);
            bool isExhausted = StaminaSystem.Instance != null && StaminaSystem.Instance.IsExhausted;

            if (isExhausted)
            {
                staminaText.text = $"<color=#FF4444>STAMINA  {pct}%</color>";
            }
            else
            {
                staminaText.text = $"STAMINA  {pct}%";
            }
        }
    }

    private void BuildUIIfNeeded()
    {
        if (targetCanvas == null)
        {
            targetCanvas = GetComponent<Canvas>();
            if (targetCanvas == null)
            {
                var mainsCanvas = GameObject.Find("MAINS/UI/Canvas");
                if (mainsCanvas != null) targetCanvas = mainsCanvas.GetComponent<Canvas>();
            }
            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>();
            }
        }

        if (hudFont == null)
        {
            var hudMgr = FindFirstObjectByType<HeistHUDManager>();
            if (hudMgr != null)
            {
                var fontField = typeof(HeistHUDManager).GetField("hudFont", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fontField != null) hudFont = fontField.GetValue(hudMgr) as TMP_FontAsset;
            }
        }

        if (targetCanvas == null) return;

        // Clean up old Meters_HUD bar if present
        Transform oldBars = targetCanvas.transform.Find("Meters_HUD");
        if (oldBars != null) Destroy(oldBars.gameObject);

        // Find or create Battery Text
        if (batteryText == null)
        {
            Transform existing = targetCanvas.transform.Find("BatteryText");
            if (existing != null)
            {
                batteryText = existing.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                var go = new GameObject("BatteryText", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(targetCanvas.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-72.4f, -835f);
                rt.sizeDelta = new Vector2(400f, 40f);

                batteryText = go.GetComponent<TextMeshProUGUI>();
                batteryText.font = hudFont;
                batteryText.fontSize = 32f;
                batteryText.alignment = TextAlignmentOptions.Right;
                batteryText.color = Color.white;
                batteryText.text = "BATTERY  100%";
                batteryText.raycastTarget = false;
            }
        }

        // Find or create Stamina Text
        if (staminaText == null)
        {
            Transform existing = targetCanvas.transform.Find("StaminaText");
            if (existing != null)
            {
                staminaText = existing.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                var go = new GameObject("StaminaText", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(targetCanvas.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-72.4f, -875f);
                rt.sizeDelta = new Vector2(400f, 40f);

                staminaText = go.GetComponent<TextMeshProUGUI>();
                staminaText.font = hudFont;
                staminaText.fontSize = 32f;
                staminaText.alignment = TextAlignmentOptions.Right;
                staminaText.color = Color.white;
                staminaText.text = "STAMINA  100%";
                staminaText.raycastTarget = false;
            }
        }
    }
}
