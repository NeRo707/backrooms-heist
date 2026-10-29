using UnityEngine;
using AZE.AdvancedFirstPerson;

/// <summary>
/// Controls player stamina.
/// Sprinting drains stamina. When depleted, the player cannot sprint until recovered.
/// Attach to the Player GameObject alongside PlayerMovementStateMachine.
/// </summary>
public class StaminaSystem : MonoBehaviour
{
    public static StaminaSystem Instance { get; private set; }

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [Tooltip("Stamina drained per second while sprinting.")]
    [SerializeField] private float drainRate = 22f;
    [Tooltip("Stamina recovered per second while not sprinting.")]
    [SerializeField] private float regenRate = 14f;
    [Tooltip("Delay in seconds before regeneration starts after stopping sprint.")]
    [SerializeField] private float regenDelay = 1.2f;
    [Tooltip("Stamina must reach this percentage before sprinting can be resumed after exhaustion.")]
    [Range(0f, 1f)]
    [SerializeField] private float exhaustionRecoveryPercent = 0.25f;

    public float CurrentStamina { get; private set; }
    public float MaxStamina => maxStamina;
    public float StaminaPercent => maxStamina > 0f ? CurrentStamina / maxStamina : 0f;
    public bool IsExhausted { get; private set; }

    /// <summary>Invoked whenever stamina changes. Passes normalized [0, 1] value.</summary>
    public static event System.Action<float> OnStaminaChanged;

    private PlayerMovementStateMachine _movement;
    private PlayerInputHandler _inputHandler;
    private float _originalRunSpeed;
    private float _regenTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        CurrentStamina = maxStamina;
        _movement = GetComponent<PlayerMovementStateMachine>();
        _inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void Start()
    {
        if (_movement != null)
        {
            _originalRunSpeed = _movement.RunSpeed;
        }
    }

    private void Update()
    {
        if (PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused)
            return;

        if (_movement == null || _inputHandler == null) return;

        bool isMoving = _movement.CurrentMoveVelocity.sqrMagnitude > 0.05f;
        bool isSprintRequested = _inputHandler.SprintPressed && _movement.IsGrounded && isMoving;

        if (isSprintRequested && !IsExhausted)
        {
            // Drain stamina
            CurrentStamina = Mathf.Max(0f, CurrentStamina - drainRate * Time.deltaTime);
            _regenTimer = regenDelay;

            if (CurrentStamina <= 0f)
            {
                IsExhausted = true;
                _movement.RunSpeed = _movement.WalkSpeed;
            }

            OnStaminaChanged?.Invoke(StaminaPercent);
        }
        else
        {
            // If exhausted and still trying to run, ensure speed is capped to walk
            if (IsExhausted)
            {
                _movement.RunSpeed = _movement.WalkSpeed;
            }

            // Regen delay timer
            if (_regenTimer > 0f)
            {
                _regenTimer -= Time.deltaTime;
            }
            else if (CurrentStamina < maxStamina)
            {
                CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + regenRate * Time.deltaTime);
                OnStaminaChanged?.Invoke(StaminaPercent);

                // Check recovery from exhaustion
                if (IsExhausted && StaminaPercent >= exhaustionRecoveryPercent)
                {
                    IsExhausted = false;
                    _movement.RunSpeed = _originalRunSpeed;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (_movement != null && _originalRunSpeed > 0f)
        {
            _movement.RunSpeed = _originalRunSpeed;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
}
