using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeistHUDManager : MonoBehaviour {
  public static HeistHUDManager Instance { get; private set; }

  [Header("References")]
  [SerializeField] private PlayerInteract playerInteract;
  [SerializeField] private Canvas hudCanvas;

  [Header("UI Elements")]
  [SerializeField] private TextMeshProUGUI interactionPromptText;
  [SerializeField] private TextMeshProUGUI weightMeterText;
  [SerializeField] private TextMeshProUGUI toastNotificationText;
  // [SerializeField] private Image crosshairDot;
  [SerializeField] private TMP_FontAsset hudFont; // Assign in Inspector

  private Coroutine activeToastCoroutine;

  private void Awake() {
    if (Instance != null && Instance != this) {
      Destroy(gameObject);
      return;
    }

    Instance = this;

    EnsureUIReferences();
  }

  private void OnEnable() {
    Inventory.OnItemCollected += HandleItemCollected;
    Inventory.OnInventoryMessage += ShowToastMessage;
  }

  private void OnDisable() {
    Inventory.OnItemCollected -= HandleItemCollected;
    Inventory.OnInventoryMessage -= ShowToastMessage;
  }

  private void Start() {
    if (playerInteract == null) {
      playerInteract = FindFirstObjectByType<PlayerInteract>();
    }

    UpdateWeightMeter();
  }

  private void Update() {
    UpdateInteractionPrompt();
    UpdateWeightMeter();
  }

  private void UpdateInteractionPrompt() {
    if (interactionPromptText == null) return;

    if (playerInteract != null && playerInteract.CurrentHoverItem != null) {
      var itemData = playerInteract.CurrentHoverItem.ItemData;
      if (itemData != null) {
        interactionPromptText.text =
          $"[E] Take {itemData.ItemName} <color=#FFD700>(${itemData.SaleValue})</color>\n<size=80%><color=#AAAAAA>{itemData.Weight:0.#} kg</color></size>";
        interactionPromptText.gameObject.SetActive(true);
        return;
      }
    }

    interactionPromptText.gameObject.SetActive(false);
  }

  private void UpdateWeightMeter() {
    if (weightMeterText == null || Inventory.Instance == null) return;

    float current = Inventory.Instance.CurrentWeight;
    float max = Inventory.Instance.MaxWeight;
    int count = Inventory.Instance.CollectedFurniture.Count;

    weightMeterText.text = $"<size=100%>LOOT BAG</size>\n<b>{current:0.#} / {max:0.#} kg</b> ({count} items)";
  }

  private void HandleItemCollected(FurnitureItem item) {
    if (item != null) {
      ShowToastMessage($"<color=#55FF55>+ {item.ItemName}</color> ({item.Weight:0.#} kg, ${item.SaleValue})");
    }
  }

  public void ShowToastMessage(string message) {
    if (toastNotificationText == null) return;

    if (activeToastCoroutine != null) {
      StopCoroutine(activeToastCoroutine);
    }

    activeToastCoroutine = StartCoroutine(AnimateToast(message));
  }

  private IEnumerator AnimateToast(string message) {
    toastNotificationText.text = message;
    toastNotificationText.gameObject.SetActive(true);
    toastNotificationText.canvasRenderer.SetAlpha(1f);

    yield return new WaitForSeconds(2.0f);

    float fadeTime = 0.5f;
    float elapsed = 0f;
    while (elapsed < fadeTime) {
      elapsed += Time.deltaTime;
      float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
      toastNotificationText.canvasRenderer.SetAlpha(alpha);
      yield return null;
    }

    toastNotificationText.gameObject.SetActive(false);
  }

  private void EnsureUIReferences() {
    if (hudCanvas == null) {
      hudCanvas = FindFirstObjectByType<Canvas>();
    }

    if (hudCanvas == null) {
      var canvasObj = new GameObject("HUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
      hudCanvas = canvasObj.GetComponent<Canvas>();
      hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

      var scaler = canvasObj.GetComponent<CanvasScaler>();
      scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
      scaler.referenceResolution = new Vector2(1920, 1080);
    }

    // Build Reticle if missing
    // if (crosshairDot == null) {
    //   var dotObj = new GameObject("Reticle", typeof(RectTransform), typeof(Image));
    //   dotObj.transform.SetParent(hudCanvas.transform, false);
    //   crosshairDot = dotObj.GetComponent<Image>();
    //   crosshairDot.color = new Color(1f, 1f, 1f, 0.6f);
    //   var rect = dotObj.GetComponent<RectTransform>();
    //   rect.sizeDelta = new Vector2(4f, 4f);
    //   rect.anchoredPosition = Vector2.zero;
    // }

    // Build Interaction Prompt Text if missing
    if (interactionPromptText == null) {
      var promptObj = new GameObject("InteractionPrompt", typeof(RectTransform), typeof(TextMeshProUGUI));
      promptObj.transform.SetParent(hudCanvas.transform, false);
      interactionPromptText = promptObj.GetComponent<TextMeshProUGUI>();
      interactionPromptText.alignment = TextAlignmentOptions.Center;
      interactionPromptText.fontSize = 24;
      interactionPromptText.color = Color.white;
      interactionPromptText.font = hudFont;
      var rect = promptObj.GetComponent<RectTransform>();
      rect.anchoredPosition = new Vector2(0f, -80f);
      rect.sizeDelta = new Vector2(600f, 100f);
      promptObj.SetActive(false);
    }

    // Build Weight Meter Text if missing
    if (weightMeterText == null) {
      var weightObj = new GameObject("WeightMeter", typeof(RectTransform), typeof(TextMeshProUGUI));
      weightObj.transform.SetParent(hudCanvas.transform, false);
      weightMeterText = weightObj.GetComponent<TextMeshProUGUI>();
      weightMeterText.alignment = TextAlignmentOptions.BottomLeft;
      weightMeterText.fontSize = 30;
      weightMeterText.font = hudFont;
      weightMeterText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
      var rect = weightObj.GetComponent<RectTransform>();
      rect.anchorMin = new Vector2(1f, 0f);
      rect.anchorMax = new Vector2(1f, 0f);
      rect.pivot = new Vector2(1f, 0f);
      rect.anchoredPosition = new Vector2(-1445f, 55f);
      rect.sizeDelta = new Vector2(400f, 100f);
    }

    // Build Toast Notification Text if missing
    if (toastNotificationText == null) {
      var toastObj = new GameObject("ToastNotification", typeof(RectTransform), typeof(TextMeshProUGUI));
      toastObj.transform.SetParent(hudCanvas.transform, false);
      toastNotificationText = toastObj.GetComponent<TextMeshProUGUI>();
      toastNotificationText.alignment = TextAlignmentOptions.Center;
      toastNotificationText.fontSize = 20;
      toastNotificationText.font = hudFont;
      toastNotificationText.color = Color.white;
      var rect = toastObj.GetComponent<RectTransform>();
      rect.anchoredPosition = new Vector2(0f, -180f);
      rect.sizeDelta = new Vector2(800f, 60f);
      toastObj.SetActive(false);
    }
  }
}
