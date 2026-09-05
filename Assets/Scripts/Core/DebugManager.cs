using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using AZE.AdvancedFirstPerson; // For PlayerMovementStateMachine

public class DebugManager : MonoBehaviour {
  [Header("NoClip Settings")]
  [SerializeField] private float noclipSpeed = 20f;
  [SerializeField] private float noclipFastSpeed = 50f;

  private bool noclipEnabled = false;
  private bool showSceneMenu = false;

  // References to disable during noclip
  private CharacterController charController;
  private PlayerMovementStateMachine movementStateMachine;
  private PlayerInputHandler inputHandler;

  private List<string> buildScenes = new List<string>();

  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  private static void AutoInitialize() {
    var go = new GameObject("DebugManager");
    go.AddComponent<DebugManager>();
    DontDestroyOnLoad(go);
  }

  private void OnEnable() {
    SceneManager.sceneLoaded += OnSceneLoaded;
  }

  private void OnDisable() {
    SceneManager.sceneLoaded -= OnSceneLoaded;
  }

  private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
    noclipEnabled = false;
    showSceneMenu = false;
    charController = null;
  }

  private void Start() {
    // Get all scenes in build settings
    int sceneCount = SceneManager.sceneCountInBuildSettings;
    for (int i = 0; i < sceneCount; i++) {
      string path = SceneUtility.GetScenePathByBuildIndex(i);
      string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
      if (!string.IsNullOrEmpty(sceneName)) {
        buildScenes.Add(sceneName);
      }
    }
  }

  private void Update() {
    if (Keyboard.current == null) return;

    // Toggle NoClip
    if (Keyboard.current.backquoteKey.wasPressedThisFrame) {
      ToggleNoClip();
    }

    // Toggle Scene Menu
    if (Keyboard.current.f5Key.wasPressedThisFrame) {
      showSceneMenu = !showSceneMenu;

      // Toggle cursor state when menu opens/closes
      if (showSceneMenu) {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
      } else {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
      }
    }

    // Handle NoClip movement
    if (noclipEnabled && charController != null) {
      HandleNoClipMovement();
    }
  }

  private void ToggleNoClip() {
    noclipEnabled = !noclipEnabled;

    if (noclipEnabled) {
      charController = FindFirstObjectByType<CharacterController>();
      if (charController != null) {
        movementStateMachine = charController.GetComponent<PlayerMovementStateMachine>();
        inputHandler = charController.GetComponent<PlayerInputHandler>();
      }
    }

    if (charController != null) {
      // Disable physics movement components
      charController.enabled = !noclipEnabled;
      if (movementStateMachine != null) movementStateMachine.enabled = !noclipEnabled;

      // Note: We keep PlayerInputHandler enabled so camera looking still works!

      Debug.Log($"NoClip is now {(noclipEnabled ? "ON" : "OFF")}");

      // Optional HUD feedback
      if (HeistHUDManager.Instance != null) {
        HeistHUDManager.Instance.ShowToastMessage(
          $"NOCLIP {(noclipEnabled ? "<color=#00FF00>ENABLED</color>" : "<color=#FF0000>DISABLED</color>")}");
      }
    } else {
      Debug.LogWarning("Cannot toggle NoClip: CharacterController not found.");
      noclipEnabled = false;
    }
  }

  private void HandleNoClipMovement() {
    Transform camTransform = Camera.main != null ? Camera.main.transform : charController.transform;

    float speed = Keyboard.current.leftShiftKey.isPressed ? noclipFastSpeed : noclipSpeed;

    Vector3 moveDir = Vector3.zero;

    if (Keyboard.current.wKey.isPressed) moveDir += camTransform.forward;
    if (Keyboard.current.sKey.isPressed) moveDir -= camTransform.forward;
    if (Keyboard.current.aKey.isPressed) moveDir -= camTransform.right;
    if (Keyboard.current.dKey.isPressed) moveDir += camTransform.right;

    if (Keyboard.current.eKey.isPressed || Keyboard.current.spaceKey.isPressed) moveDir += Vector3.up;
    if (Keyboard.current.qKey.isPressed || Keyboard.current.leftCtrlKey.isPressed) moveDir -= Vector3.up;

    charController.transform.position += moveDir.normalized * (speed * Time.deltaTime);
  }

  private void OnGUI() {
    if (!showSceneMenu) return;

    // Draw a simple centered menu
    float width = 300;
    float height = 50 + (buildScenes.Count * 40);
    float x = (Screen.width - width) / 2;
    float y = (Screen.height - height) / 2;

    GUI.Box(new Rect(x, y, width, height), "Scene Switcher (F5 to close)");

    for (int i = 0; i < buildScenes.Count; i++) {
      string sceneName = buildScenes[i];
      if (GUI.Button(new Rect(x + 10, y + 30 + (i * 40), width - 20, 30), sceneName)) {
        // Unpause time just in case, hide cursor
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SceneManager.LoadScene(i);
      }
    }
  }
}
