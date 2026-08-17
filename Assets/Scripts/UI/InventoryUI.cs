using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Tab-key inventory panel. Shows carried items, total value, weight, level, and banked money.
/// Auto-builds itself on the existing Canvas — no manual setup needed.
/// </summary>
public class InventoryUI : MonoBehaviour
{
  // ── singleton ───────────────────────────────────────────────────
  public static InventoryUI Instance { get; private set; }

  // ── built references ────────────────────────────────────────────
  private GameObject      _panel;
  private TextMeshProUGUI _headerText;
  private TextMeshProUGUI _statsText;
  private Transform       _itemListRoot;
  private GameObject      _itemRowPrefab;   // reused for each furniture row
  private bool            _isOpen;

  // ── colours ─────────────────────────────────────────────────────
  private static readonly Color PanelBg     = new Color(0.06f, 0.06f, 0.08f, 0.93f);
  private static readonly Color HeaderColor = new Color(1f,    0.85f, 0.3f,  1f);
  private static readonly Color ValueColor  = new Color(0.4f,  1f,    0.5f,  1f);
  private static readonly Color DimColor    = new Color(0.65f, 0.65f, 0.65f, 1f);
  private static readonly Color SepColor    = new Color(1f,    1f,    1f,    0.08f);

  // ────────────────────────────────────────────────────────────────
  private void Awake()
  {
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;

    BuildPanel();
    SetPanelVisible(false);
  }

  private void OnEnable()
  {
    Inventory.OnItemCollected += _ => RefreshIfOpen();
    Inventory.OnItemRemoved += _ => RefreshIfOpen();
  }

  private void OnDisable()
  {
    Inventory.OnItemCollected -= _ => RefreshIfOpen();
    Inventory.OnItemRemoved -= _ => RefreshIfOpen();
  }

  private void Update()
  {
    bool tabPressed = Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
    if (tabPressed) TogglePanel();
  }

  // ── public API ──────────────────────────────────────────────────
  public void TogglePanel()
  {
    _isOpen = !_isOpen;
    SetPanelVisible(_isOpen);
    if (_isOpen) Refresh();
  }

  public void Refresh()
  {
    if (Inventory.Instance == null) return;

    var inv = Inventory.Instance;

    // ── header ──
    _headerText.text = $"INVENTORY   <size=70%><color=#AAAAAA>Lv {inv.playerLevel}</color></size>";

    // ── stats block ──
    float pct = inv.maxWeight > 0 ? inv.CurrentWeight / inv.maxWeight : 0f;
    string weightBar = BuildBar(pct, 14);

    _statsText.text =
      $"<color=#FFD700>BANK</color>  <b>${inv.playerMoney:N0}</b>\n" +
      $"<color=#FFD700>RUN VALUE</color>  <b><color=#55FF55>${inv.TotalRunValue:N0}</color></b>\n" +
      $"\n" +
      $"<color=#FFD700>WEIGHT</color>  {inv.CurrentWeight:0.#} / {inv.maxWeight:0.#} kg  ({inv.collectedFurniture.Count} items)\n" +
      $"<size=90%><color=#444444>{weightBar}</color></size>";

    // ── item list ──
    // Clear old rows
    foreach (Transform child in _itemListRoot) Destroy(child.gameObject);

    if (inv.collectedFurniture.Count == 0)
    {
      var empty = MakeRowText("  Nothing carried yet…", DimColor);
      empty.transform.SetParent(_itemListRoot, false);
    }
    else
    {
      // Group by item name + count
      var groups = new Dictionary<string, (int count, int value, float weight, FurnitureItem firstItem)>();
      foreach (var item in inv.collectedFurniture)
      {
        if (item == null) continue;
        if (!groups.ContainsKey(item.itemName))
          groups[item.itemName] = (0, item.saleValue, item.weight, item);
        var g = groups[item.itemName];
        groups[item.itemName] = (g.count + 1, g.value, g.weight, g.firstItem);
      }

      foreach (var kvp in groups)
      {
        string name  = kvp.Key;
        int    count = kvp.Value.count;
        int    val   = kvp.Value.value * count;
        float  wt    = kvp.Value.weight * count;
        var    fItem = kvp.Value.firstItem;

        string line = $"  <b>{name}</b>";
        if (count > 1) line += $"  <color=#AAAAAA>×{count}</color>";
        line += $"    <color=#55FF55>${val}</color>  <color=#888888>{wt:0.#} kg</color>";

        var row = MakeRowText(line, Color.white);
        row.transform.SetParent(_itemListRoot, false);

        MakeDropButton(row.transform, fItem);
      }
    }
  }

  private void DropIntoWorld(FurnitureItem item)
  {
    if (item == null || item.prefab == null) return;

    Camera cam = Camera.main;
    if (cam == null)
    {
      var playerInteract = FindFirstObjectByType<PlayerInteract>();
      if (playerInteract != null) cam = playerInteract.playerCamera;
    }
    if (cam == null) cam = FindFirstObjectByType<Camera>();
    if (cam == null) return;

    Vector3 startPos = cam.transform.position;

    // Flatten direction so we always drop in front of us, even if looking up/down
    Vector3 dropDir = cam.transform.forward;
    dropDir.y = 0;
    dropDir.Normalize();
    if (dropDir.sqrMagnitude < 0.1f) dropDir = cam.transform.up;

    float dropDist = 1.5f;

    // Make sure we don't spawn the item inside a wall
    if (Physics.Raycast(startPos, dropDir, out RaycastHit hit, dropDist))
    {
      dropDist = Mathf.Max(0.2f, hit.distance - 0.3f);
    }

    Vector3 spawnPos = startPos + dropDir * dropDist;
    Instantiate(item.prefab, spawnPos, Quaternion.identity);
  }

  // ── helpers ─────────────────────────────────────────────────────
  private void RefreshIfOpen() { if (_isOpen) Refresh(); }

  private void SetPanelVisible(bool visible)
  {
    if (_panel != null) _panel.SetActive(visible);

    // Pause / unpause mouse cursor
    Cursor.lockState = visible ? CursorLockMode.None     : CursorLockMode.Locked;
    Cursor.visible   = visible;
  }

  private static string BuildBar(float t, int length)
  {
    t = Mathf.Clamp01(t);
    int filled = Mathf.RoundToInt(t * length);
    return "[" + new string('█', filled) + new string('░', length - filled) + "]";
  }

  // ── UI construction ─────────────────────────────────────────────
  private void BuildPanel()
  {
    Canvas canvas = FindFirstObjectByType<Canvas>();
    if (canvas == null)
    {
      var go = new GameObject("HUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
      canvas = go.GetComponent<Canvas>();
      canvas.renderMode = RenderMode.ScreenSpaceOverlay;
      var scaler = go.GetComponent<CanvasScaler>();
      scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
      scaler.referenceResolution = new Vector2(1920, 1080);
    }

    // ── outer panel ──
    _panel = MakeRect("InventoryPanel", canvas.transform);
    var panelRect = _panel.GetComponent<RectTransform>();
    panelRect.anchorMin        = new Vector2(0.5f, 0.5f);
    panelRect.anchorMax        = new Vector2(0.5f, 0.5f);
    panelRect.pivot            = new Vector2(0.5f, 0.5f);
    panelRect.sizeDelta        = new Vector2(640, 520);
    panelRect.anchoredPosition = Vector2.zero;

    AddImage(_panel, PanelBg);

    // ── header ──
    var headerObj = MakeRect("Header", _panel.transform);
    var headerRect = headerObj.GetComponent<RectTransform>();
    headerRect.anchorMin        = new Vector2(0f, 1f);
    headerRect.anchorMax        = new Vector2(1f, 1f);
    headerRect.pivot            = new Vector2(0.5f, 1f);
    headerRect.sizeDelta        = new Vector2(0, 56);
    headerRect.anchoredPosition = Vector2.zero;
    AddImage(headerObj, new Color(0.1f, 0.1f, 0.14f, 1f));

    var headerTextObj = MakeRect("HeaderText", headerObj.transform);
    _headerText = AddTMP(headerTextObj, "", 26, HeaderColor);
    _headerText.alignment = TextAlignmentOptions.MidlineLeft;
    var ht = headerTextObj.GetComponent<RectTransform>();
    ht.anchorMin = Vector2.zero; ht.anchorMax = Vector2.one;
    ht.offsetMin = new Vector2(20, 0); ht.offsetMax = new Vector2(-20, 0);

    // ── stats block ──
    var statsObj = MakeRect("Stats", _panel.transform);
    var statsRect = statsObj.GetComponent<RectTransform>();
    statsRect.anchorMin        = new Vector2(0f, 1f);
    statsRect.anchorMax        = new Vector2(1f, 1f);
    statsRect.pivot            = new Vector2(0.5f, 1f);
    statsRect.sizeDelta        = new Vector2(-40, 140);
    statsRect.anchoredPosition = new Vector2(0, -64);

    var statsTextObj = MakeRect("StatsText", statsObj.transform);
    _statsText = AddTMP(statsTextObj, "", 20, Color.white);
    _statsText.alignment = TextAlignmentOptions.TopLeft;
    var st = statsTextObj.GetComponent<RectTransform>();
    st.anchorMin = Vector2.zero; st.anchorMax = Vector2.one;
    st.offsetMin = new Vector2(8, 0); st.offsetMax = Vector2.zero;

    // ── separator ──
    var sep = MakeRect("Separator", _panel.transform);
    var sepRect = sep.GetComponent<RectTransform>();
    sepRect.anchorMin        = new Vector2(0f, 1f);
    sepRect.anchorMax        = new Vector2(1f, 1f);
    sepRect.pivot            = new Vector2(0.5f, 1f);
    sepRect.sizeDelta        = new Vector2(-20, 2);
    sepRect.anchoredPosition = new Vector2(0, -210);
    AddImage(sep, SepColor);

    // ── item list (scroll view) ──
    var listObj = MakeRect("ItemList", _panel.transform);
    var listRect = listObj.GetComponent<RectTransform>();
    listRect.anchorMin        = new Vector2(0f, 0f);
    listRect.anchorMax        = new Vector2(1f, 1f);
    listRect.offsetMin        = new Vector2(10, 10);
    listRect.offsetMax        = new Vector2(-10, -215);

    var scroll = listObj.AddComponent<ScrollRect>();
    scroll.horizontal = false;

    var viewport = MakeRect("Viewport", listObj.transform);
    viewport.AddComponent<RectMask2D>();
    var vpRect = viewport.GetComponent<RectTransform>();
    vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one;
    vpRect.offsetMin = Vector2.zero; vpRect.offsetMax = Vector2.zero;

    var content = MakeRect("Content", viewport.transform);
    var contentRect = content.GetComponent<RectTransform>();
    contentRect.anchorMin        = new Vector2(0f, 1f);
    contentRect.anchorMax        = new Vector2(1f, 1f);
    contentRect.pivot            = new Vector2(0.5f, 1f);
    contentRect.sizeDelta        = new Vector2(0, 0);
    contentRect.anchoredPosition = Vector2.zero;

    var vlg = content.AddComponent<VerticalLayoutGroup>();
    vlg.childControlHeight    = false;
    vlg.childControlWidth     = true;
    vlg.childForceExpandWidth = true;
    vlg.spacing               = 4;
    vlg.padding               = new RectOffset(4, 4, 4, 4);

    content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

    scroll.viewport      = vpRect;
    scroll.content       = contentRect;
    scroll.scrollSensitivity = 30;

    _itemListRoot = content.transform;

    // ── close hint ──
    var hint = MakeRect("CloseHint", _panel.transform);
    var hintRect = hint.GetComponent<RectTransform>();
    hintRect.anchorMin        = new Vector2(0f, 0f);
    hintRect.anchorMax        = new Vector2(1f, 0f);
    hintRect.pivot            = new Vector2(0.5f, 0f);
    hintRect.sizeDelta        = new Vector2(0, 28);
    hintRect.anchoredPosition = new Vector2(0, 0);

    var hintTextObj = MakeRect("CloseHintText", hint.transform);
    var hintTmp = AddTMP(hintTextObj, "[TAB]  Close", 14, DimColor);
    hintTmp.alignment = TextAlignmentOptions.Center;
    var ht2 = hintTextObj.GetComponent<RectTransform>();
    ht2.anchorMin = Vector2.zero; ht2.anchorMax = Vector2.one;
    ht2.offsetMin = Vector2.zero; ht2.offsetMax = Vector2.zero;
  }

  // ── tiny factories ──────────────────────────────────────────────
  private static GameObject MakeRect(string name, Transform parent)
  {
    var go = new GameObject(name, typeof(RectTransform));
    go.transform.SetParent(parent, false);
    return go;
  }

  private static Image AddImage(GameObject go, Color color)
  {
    var img = go.AddComponent<Image>();
    img.color = color;
    return img;
  }

  private static TextMeshProUGUI AddTMP(GameObject go, string text, float size, Color color)
  {
    var tmp = go.AddComponent<TextMeshProUGUI>();
    tmp.text      = text;
    tmp.fontSize  = size;
    tmp.color     = color;
    tmp.richText  = true;
    return tmp;
  }

  private static GameObject MakeRowText(string text, Color color)
  {
    var go  = new GameObject("Row", typeof(RectTransform));
    var tmp = go.AddComponent<TextMeshProUGUI>();
    tmp.text     = text;
    tmp.fontSize = 19;
    tmp.color    = color;
    tmp.richText = true;

    var le = go.AddComponent<LayoutElement>();
    le.preferredHeight = 30;

    return go;
  }

  private GameObject MakeDropButton(Transform parent, FurnitureItem itemToDrop)
  {
    var go = new GameObject("DropBtn", typeof(RectTransform));
    go.transform.SetParent(parent, false);

    var rect = go.GetComponent<RectTransform>();
    rect.anchorMin = new Vector2(1, 0.5f);
    rect.anchorMax = new Vector2(1, 0.5f);
    rect.pivot = new Vector2(1, 0.5f);
    rect.anchoredPosition = new Vector2(-20, 0);
    rect.sizeDelta = new Vector2(60, 24);

    var img = AddImage(go, new Color(0.7f, 0.15f, 0.15f, 1f));

    var textObj = MakeRect("Text", go.transform);
    var tmp = AddTMP(textObj, "DROP", 14, Color.white);
    tmp.alignment = TextAlignmentOptions.Center;
    tmp.raycastTarget = false; // Prevent text from intercepting clicks
    var tRect = textObj.GetComponent<RectTransform>();
    tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one;
    tRect.offsetMin = Vector2.zero; tRect.offsetMax = Vector2.zero;

    var btn = go.AddComponent<Button>();
    // Need a Graphic to catch clicks, the Image we just added does that
    btn.targetGraphic = img;

    btn.onClick.AddListener(() => {
      Debug.Log($"[InventoryUI] Drop button clicked for {itemToDrop.itemName}");
      if (Inventory.Instance != null && Inventory.Instance.RemoveItem(itemToDrop))
      {
        DropIntoWorld(itemToDrop);
      }
    });

    return go;
  }
}
