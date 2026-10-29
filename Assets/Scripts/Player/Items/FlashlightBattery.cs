using UnityEngine;

/// <summary>
/// Manages the flashlight battery — drains over time while the light is on,
/// applies a low-battery flicker effect, and forces the light off when dead.
/// Attach to the same GameObject as <see cref="FlashLight"/>.
/// Call <see cref="Recharge"/> from a battery pickup to restore charge.
/// </summary>
public class FlashlightBattery : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Battery Settings")]
    [SerializeField] private float maxBattery = 100f;
    [Tooltip("Battery units drained per second in Flashlight mode.")]
    [SerializeField] private float flashlightDrainRate = 4f;
    [Tooltip("Battery units drained per second in Night-Vision mode.")]
    [SerializeField] private float nvgDrainRate = 8f;

    [Header("Low-Battery Flicker")]
    [Tooltip("Below this battery percentage (0-100) the flicker effect activates.")]
    [SerializeField] private float flickerThreshold = 20f;
    [Tooltip("How strongly the flicker varies intensity (0 = none, 1 = max).")]
    [SerializeField][Range(0f, 1f)] private float flickerIntensityMult = 0.4f;

    // ── Public API ────────────────────────────────────────────────────────────
    /// Current battery charge (0 … maxBattery).
    public float CurrentBattery { get; private set; }

    /// Battery level normalised to [0, 1].
    public float BatteryPercent => CurrentBattery / maxBattery;

    /// True when the battery has been fully depleted.
    public bool IsDead => CurrentBattery <= 0f;

    /// Fires every frame the battery changes, passing the normalised [0, 1] level.
    /// Subscribe from the HUD to drive the battery indicator.
    public static event System.Action<float> OnBatteryChanged;

    // ── Private ───────────────────────────────────────────────────────────────
    private FlashLight _flashLight;
    private Light _light;
    private float _baseIntensity;
    private float _flickerTimer;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        CurrentBattery = maxBattery;
        _flashLight = GetComponent<FlashLight>();
        _light = GetComponentInChildren<Light>();

        if (_light != null)
            _baseIntensity = _light.intensity;

        if (_flashLight == null)
            Debug.LogError("[FlashlightBattery] No FlashLight component found on this GameObject.", this);
    }

    private void Update()
    {
        // Respect pause state.
        if (PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused)
            return;

        if (_flashLight == null) return;

        LightMode mode = _flashLight.CurrentMode;

        // ── Battery is dead ───────────────────────────────────────────────────
        if (IsDead)
        {
            // Keep forcing off in case the player somehow re-activated the light.
            if (mode != LightMode.Off)
                _flashLight.ForceOff();

            if (_light != null) _light.enabled = false;
            return;
        }

        // Light is off — nothing to drain.
        if (mode == LightMode.Off) return;

        // ── Drain ─────────────────────────────────────────────────────────────
        float drainRate = (mode == LightMode.NightVision) ? nvgDrainRate : flashlightDrainRate;
        CurrentBattery = Mathf.Max(0f, CurrentBattery - drainRate * Time.deltaTime);
        OnBatteryChanged?.Invoke(BatteryPercent);

        // ── Flicker effect ────────────────────────────────────────────────────
        if (_light != null)
        {
            if (BatteryPercent * 100f < flickerThreshold)
            {
                _flickerTimer -= Time.deltaTime;
                if (_flickerTimer <= 0f)
                {
                    // Perlin noise gives smoother, less regular flicker than pure Random.
                    float noise = Mathf.PerlinNoise(Time.time * 15f, 0f);
                    _light.intensity = _baseIntensity * Mathf.Lerp(1f, noise, flickerIntensityMult);
                    _flickerTimer = Random.Range(0.03f, 0.12f);
                }
            }
            else
            {
                // Outside flicker range — restore base intensity (unless a
                // separate FlashlightFlicker component is also managing it).
                if (GetComponent<FlashlightFlicker>() == null)
                    _light.intensity = _baseIntensity;
            }
        }

        // ── Just died this frame ──────────────────────────────────────────────
        if (IsDead)
        {
            _flashLight.ForceOff();
            if (_light != null) _light.enabled = false;
            OnBatteryChanged?.Invoke(0f);
        }
    }

    // ── Public Methods ────────────────────────────────────────────────────────
    /// <summary>
    /// Adds <paramref name="amount"/> units of charge, clamped to maxBattery.
    /// Call this from a battery pickup.
    /// </summary>
    public void Recharge(float amount)
    {
        CurrentBattery = Mathf.Min(maxBattery, CurrentBattery + amount);
        OnBatteryChanged?.Invoke(BatteryPercent);
    }
}
