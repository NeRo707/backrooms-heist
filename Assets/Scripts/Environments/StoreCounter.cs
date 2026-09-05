using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Place on the store counter in Store.unity.
/// Allows player to open a store menu to sell furniture and upgrade capacity upon pressing E.
/// </summary>
public class StoreCounter : MonoBehaviour {
  [Header("Interaction Settings")]
  [SerializeField] private float interactDistance = 3.0f;
  [SerializeField] private string promptMessage = "Press [E] to Open Store";

  [Header("Audio (Optional)")]
  [SerializeField] private AudioSource audioSource;
  [SerializeField] private AudioClip sellSound;

  private bool _playerInRange = false;
  private bool _isMenuOpen = false;

  private GameObject _storeMenuUI;
  private TextMeshProUGUI _statsText;
  private TextMeshProUGUI _levelUpText;
  private Button _levelUpButton;
  private Button _sellButton;

  private void Start() {
    if (audioSource == null) {
      audioSource = GetComponent<AudioSource>();
    }

    CreateStoreUI();
  }

  private void CreateStoreUI() {
    Canvas canvas = FindFirstObjectByType<Canvas>();
    if (canvas == null) return;

    _storeMenuUI = new GameObject("StoreMenuUI", typeof(RectTransform));
    _storeMenuUI.transform.SetParent(canvas.transform, false);
    var rect = _storeMenuUI.GetComponent<RectTransform>();
    rect.anchorMin = Vector2.zero;
    rect.anchorMax = Vector2.one;
    rect.sizeDelta = Vector2.zero;

    // Background panel
    var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
    bg.transform.SetParent(_storeMenuUI.transform, false);
    var bgImg = bg.GetComponent<Image>();
    bgImg.color = new Color(0, 0, 0, 0.9f);
    var bgRect = bg.GetComponent<RectTransform>();
    bgRect.anchorMin = Vector2.zero;
    bgRect.anchorMax = Vector2.one;
    bgRect.sizeDelta = Vector2.zero;

    // Title
    var title = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
    title.transform.SetParent(_storeMenuUI.transform, false);
    var titleText = title.GetComponent<TextMeshProUGUI>();
    titleText.text = "STORE";
    titleText.fontSize = 60;
    titleText.alignment = TextAlignmentOptions.Center;
    titleText.fontStyle = FontStyles.Bold;
    var titleRect = title.GetComponent<RectTransform>();
    titleRect.anchoredPosition = new Vector2(0, 300);
    titleRect.sizeDelta = new Vector2(600, 100);

    // Stats Text
    var stats = new GameObject("StatsText", typeof(RectTransform), typeof(TextMeshProUGUI));
    stats.transform.SetParent(_storeMenuUI.transform, false);
    _statsText = stats.GetComponent<TextMeshProUGUI>();
    _statsText.fontSize = 30;
    _statsText.alignment = TextAlignmentOptions.Center;
    var statsRect = stats.GetComponent<RectTransform>();
    statsRect.anchoredPosition = new Vector2(0, 100);
    statsRect.sizeDelta = new Vector2(800, 200);

    // Sell Button
    var sellBtnObj = CreateButton("SellButton", "SELL FURNITURE", new Vector2(-250, -100), _storeMenuUI.transform);
    _sellButton = sellBtnObj.GetComponent<Button>();
    _sellButton.onClick.AddListener(SellFurniture);

    // Level Up Button
    var levelBtnObj = CreateButton("LevelUpButton", "LEVEL UP\n$1000", new Vector2(250, -100), _storeMenuUI.transform);
    _levelUpButton = levelBtnObj.GetComponent<Button>();
    _levelUpButton.onClick.AddListener(LevelUp);
    _levelUpText = levelBtnObj.GetComponentInChildren<TextMeshProUGUI>();

    // Close Button
    var closeBtnObj = CreateButton("CloseButton", "CLOSE", new Vector2(0, -300), _storeMenuUI.transform);
    closeBtnObj.GetComponent<Button>().onClick.AddListener(CloseMenu);

    _storeMenuUI.SetActive(false);
  }

  private GameObject CreateButton(string name, string text, Vector2 pos, Transform parent) {
    var btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
    btnObj.transform.SetParent(parent, false);
    var btnImg = btnObj.GetComponent<Image>();
    btnImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

    var btn = btnObj.GetComponent<Button>();
    ColorBlock cb = btn.colors;
    cb.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    cb.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    cb.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    btn.colors = cb;

    var rect = btnObj.GetComponent<RectTransform>();
    rect.anchoredPosition = pos;
    rect.sizeDelta = new Vector2(350, 100);

    var txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
    txtObj.transform.SetParent(btnObj.transform, false);
    var txt = txtObj.GetComponent<TextMeshProUGUI>();
    txt.text = text;
    txt.fontSize = 32;
    txt.alignment = TextAlignmentOptions.Center;
    txt.color = Color.white;
    var txtRect = txtObj.GetComponent<RectTransform>();
    txtRect.anchorMin = Vector2.zero;
    txtRect.anchorMax = Vector2.one;
    txtRect.sizeDelta = Vector2.zero;

    return btnObj;
  }

  private void OnTriggerEnter(Collider other) {
    if (other.CompareTag("Player")) {
      _playerInRange = true;
      ShowPrompt();
    }
  }

  private void OnTriggerExit(Collider other) {
    if (other.CompareTag("Player")) {
      _playerInRange = false;
      if (_isMenuOpen) CloseMenu();
    }
  }

  private void Update() {
    if (!_playerInRange) return;

    bool interactPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
    if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame) {
      interactPressed = true;
    }

    if (interactPressed) {
      if (_isMenuOpen) CloseMenu();
      else OpenMenu();
    }
  }

  private void OpenMenu() {
    _isMenuOpen = true;
    _storeMenuUI.SetActive(true);
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
    // Time.timeScale = 0f;
    UpdateUI();
  }

  private void CloseMenu() {
    _isMenuOpen = false;
    _storeMenuUI.SetActive(false);
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
    // Time.timeScale = 1f;
  }

  private void UpdateUI() {
    if (Inventory.Instance == null) return;

    int count = Inventory.Instance.CollectedFurniture.Count;
    int earnings = Inventory.Instance.TotalRunValue;

    _statsText.text = $"Money: <color=#55FF55>${Inventory.Instance.PlayerMoney:N0}</color>\n\n" +
                      $"Carry Capacity: {Inventory.Instance.MaxWeight:0.#} kg (Level {Inventory.Instance.PlayerLevel})\n" +
                      $"Ready to sell: {count} item(s) for <color=#55FF55>${earnings:N0}</color>";

    int cost = GetLevelUpCost();
    _levelUpText.text = $"LEVEL UP CAPACITY\n<color=#FF5555>-${cost}</color>";

    _levelUpButton.interactable = Inventory.Instance.PlayerMoney >= cost;
    _sellButton.interactable = count > 0;
  }

  private int GetLevelUpCost() {
    return 500 * (Inventory.Instance.PlayerLevel);
  }

  public void SellFurniture() {
    if (Inventory.Instance == null) return;

    int itemCount = Inventory.Instance.CollectedFurniture.Count;
    int earnings = Inventory.Instance.TotalRunValue;

    if (itemCount <= 0) {
      HeistHUDManager.Instance?.ShowToastMessage("<color=#FFAA00>No furniture to sell!</color>");
      return;
    }

    // Cash out
    Inventory.Instance.PlayerMoney += earnings;
    Inventory.Instance.ClearCurrentRun();

    // Play sale sound
    if (audioSource != null && sellSound != null) {
      audioSource.PlayOneShot(sellSound);
    }

    // Feedback
    HeistHUDManager.Instance?.ShowToastMessage($"<color=#55FF55>Sold {itemCount} item(s) for ${earnings:N0}!</color>");
    UpdateUI();
  }

  public void LevelUp() {
    if (Inventory.Instance == null) return;

    int cost = GetLevelUpCost();
    if (Inventory.Instance.PlayerMoney >= cost) {
      Inventory.Instance.PlayerMoney -= cost;
      Inventory.Instance.PlayerLevel++;
      Inventory.Instance.MaxWeight += 10f; // Increase carry capacity by 10kg

      HeistHUDManager.Instance?.ShowToastMessage(
        $"<color=#55FF55>Upgraded Capacity to {Inventory.Instance.MaxWeight} kg!</color>");
      UpdateUI();
    }
  }

  private void ShowPrompt() {
    if (Inventory.Instance != null && !_isMenuOpen) {
      HeistHUDManager.Instance?.ShowToastMessage("[E] Open Store Menu");
    }
  }

  private void OnDrawGizmosSelected() {
    Gizmos.color = Color.green;
    Gizmos.DrawWireSphere(transform.position, interactDistance);
  }
}
