using UnbridledSanctum.Combat;
using UnbridledSanctum.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnbridledSanctum.Expedition;
using UnbridledSanctum.Data;

/// <summary>
/// Manages the Expedition screen:
///  - Character stays static on the left (unobstructed).
///  - No screen dimming/darkening overlay.
///  - Map camera follows the player's current tile, smoothly centering on it.
///  - Invisible boundary (RectMask2D viewport) hides any room tile that moves beyond it.
///  - Supports movement via WASD, Arrow Keys, and direct room clicking.
///  - Displays encounters (Combat, Event, Treasure, Next Floor Exit).
///  - Silent traversal for Empty and Start rooms.
///  - Includes Surrender button to safely return to Sanctuary Cave.
///  - All text in English.
/// </summary>
public class ExpeditionMapUI : MonoBehaviour
{
    [Header("Main Panel")]
    public GameObject expeditionPanel;

    [Header("Map Viewport & Container")]
    public RectTransform mapViewport;
    public RectTransform mapContainer;

    [Header("Top Header Info")]
    public TextMeshProUGUI floorTitleText;
    public TextMeshProUGUI controlsHintText;
    public Button surrenderButton;

    [Header("Player Status HUD (Top Left, Always Visible)")]
    public GameObject playerStatusPanel;
    public TextMeshProUGUI playerHpText;
    public TextMeshProUGUI playerUpperClothesText;
    public TextMeshProUGUI playerLowerClothesText;
    public TextMeshProUGUI playerClothesText;
    public TextMeshProUGUI playerShieldText;
    public TextMeshProUGUI playerLustText;
    public TextMeshProUGUI playerCorruptionText;
    public TextMeshProUGUI playerLumieresText;
    public TextMeshProUGUI playerLustidText;
    public TooltipPopup statTooltipPopup;

    [Header("Encounter Modal")]
    public GameObject encounterModal;
    public TextMeshProUGUI encounterTitleText;
    public TextMeshProUGUI encounterDescText;
    public Button primaryActionButton;
    public TextMeshProUGUI primaryActionText;
    public Button secondaryActionButton;
    public TextMeshProUGUI secondaryActionText;

    public event Action OnSurrenderToLobby;
    public event Action<int> OnFloorCompleted;

    private CombatManager combatManager;
    private CombatUI combatUI;
    private DungeonRoom currentCombatRoom;

    public void BindCombat(CombatManager cManager, CombatUI cUI)
    {
        if (this.combatManager != null)
        {
            this.combatManager.OnPersistentStatsChanged -= RefreshPlayerStats;
        }
        this.combatManager = cManager;
        this.combatUI = cUI;
        if (this.combatManager != null)
        {
            this.combatManager.OnPersistentStatsChanged += RefreshPlayerStats;
        }
    }

    private void OnDestroy()
    {
        if (this.combatManager != null)
        {
            this.combatManager.OnPersistentStatsChanged -= RefreshPlayerStats;
        }
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnLumieresChanged -= HandleCurrenciesChanged;
        }
    }

    private void HandleCurrenciesChanged(int _)
    {
        RefreshPlayerStats();
    }

    private DungeonFloor currentFloor;
    private int highestFloorReached = 1;
    private Vector2Int previousPos;
    private Dictionary<Vector2Int, GameObject> roomTiles = new Dictionary<Vector2Int, GameObject>();
    private Vector2 targetMapContainerPos;

    private void Awake()
    {
        if (expeditionPanel == null) expeditionPanel = gameObject;
    }

    private void Start()
    {
        if (surrenderButton != null)
        {
            surrenderButton.onClick.AddListener(HandleSurrenderClicked);
        }
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnLumieresChanged += HandleCurrenciesChanged;
        }
    }

    public void StartNewExpedition(int startingFloor = 1)
    {
        CombatManager.Instance?.ResetPlayerStatsForNewExpedition();
        CurrencyManager.Instance?.ResetRunCurrency();
        highestFloorReached = Mathf.Max(highestFloorReached, startingFloor);
        LoadFloor(startingFloor);
        Show();
        RefreshPlayerStats();
    }

    public void RefreshPlayerStats()
    {
        CombatManager cm = combatManager != null ? combatManager : CombatManager.Instance;
        if (cm == null) return;

        if (playerHpText != null)
        {
            playerHpText.text = $"{cm.persistentHp} / {cm.persistentMaxHp} HP";
        }

        if (playerUpperClothesText != null)
        {
            if (cm.persistentUpperClothesHp > 0)
                playerUpperClothesText.text = $"{cm.persistentUpperClothesHp} / {cm.persistentMaxUpperClothesHp}";
            else
                playerUpperClothesText.text = $"0 / {cm.persistentMaxUpperClothesHp} [Torn!]";
            playerUpperClothesText.color = (cm.persistentUpperClothesHp > 0) ? new Color(0.40f, 0.80f, 1f, 1f) : new Color(0.85f, 0.35f, 0.35f, 0.9f);
        }

        if (playerLowerClothesText != null)
        {
            if (cm.persistentLowerClothesHp > 0)
                playerLowerClothesText.text = $"{cm.persistentLowerClothesHp} / {cm.persistentMaxLowerClothesHp}";
            else
                playerLowerClothesText.text = $"0 / {cm.persistentMaxLowerClothesHp} [Torn!]";
            playerLowerClothesText.color = (cm.persistentLowerClothesHp > 0) ? new Color(0.40f, 0.80f, 1f, 1f) : new Color(0.85f, 0.35f, 0.35f, 0.9f);
        }

        if (playerClothesText != null && playerClothesText != playerUpperClothesText)
        {
            playerClothesText.text = $"{cm.persistentClothesHp} / {cm.persistentMaxClothesHp}";
        }

        if (playerShieldText != null)
        {
            playerShieldText.text = $"{cm.persistentShield}";
        }

        if (playerLustText != null)
        {
            playerLustText.text = $"{cm.persistentSeduction}%  ({cm.persistentSeductionStacks}/3)";
        }

        if (playerCorruptionText != null)
        {
            playerCorruptionText.text = $"{cm.persistentCorruption} / {cm.maxCorruption}";
            playerCorruptionText.color = (cm.persistentCorruption > 0) ? new Color(0.95f, 0.40f, 0.85f, 1f) : new Color(0.70f, 0.75f, 0.85f, 0.8f);
        }

        if (playerLumieresText != null)
        {
            playerLumieresText.text = CurrencyManager.Instance != null ? CurrencyManager.Instance.CurrentLumieres.ToString() : "0";
        }
    }

    public void LoadFloor(int floorNum)
    {
        currentFloor = DungeonFloorGenerator.Generate(floorNum);
        previousPos = currentFloor.startPos;
        highestFloorReached = Mathf.Max(highestFloorReached, floorNum);

        if (floorTitleText != null)
        {
            floorTitleText.text = $"CATACOMBS - FLOOR {currentFloor.floorNumber}";
        }

        RebuildMapVisuals();
        CheckCurrentRoomEncounter();
        RefreshPlayerStats();
    }

    public void Show()
    {
        if (expeditionPanel != null) expeditionPanel.SetActive(true);
        RefreshPlayerStats();
    }

    public void Hide()
    {
        if (expeditionPanel != null) expeditionPanel.SetActive(false);
        if (encounterModal != null) encounterModal.SetActive(false);
    }

    private void Update()
    {
        if (expeditionPanel == null || !expeditionPanel.activeInHierarchy) return;
        if (currentFloor == null) return;

        // Camera tracks the player's current room tile
        if (mapContainer != null)
        {
            float step = 84f;
            targetMapContainerPos = -new Vector2(currentFloor.currentPos.x * step, currentFloor.currentPos.y * step);
            mapContainer.anchoredPosition = Vector2.Lerp(mapContainer.anchoredPosition, targetMapContainerPos, Time.unscaledDeltaTime * 12f);
            UpdateTileBoundaryVisibility();
        }

        // If modal is showing with combat or next floor decision, pause map keyboard inputs
        if (encounterModal != null && encounterModal.activeInHierarchy)
        {
            bool confirmPressed = WasKeyJustPressed(Key.Enter) || WasKeyJustPressed(Key.Space) || WasKeyJustPressed(Key.NumpadEnter);
            try
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                    confirmPressed = true;
            }
            catch {}

            if (confirmPressed)
            {
                if (primaryActionButton != null && primaryActionButton.gameObject.activeInHierarchy && primaryActionButton.interactable)
                {
                    primaryActionButton.onClick.Invoke();
                }
            }
            else
            {
                bool cancelPressed = WasKeyJustPressed(Key.Escape);
                try
                {
                    if (Input.GetKeyDown(KeyCode.Escape)) cancelPressed = true;
                }
                catch {}

                if (cancelPressed)
                {
                    if (secondaryActionButton != null && secondaryActionButton.gameObject.activeInHierarchy && secondaryActionButton.interactable)
                    {
                        secondaryActionButton.onClick.Invoke();
                    }
                }
            }
            return;
        }

        // WASD & Arrow Keys movement
        if (WasKeyJustPressed(Key.W) || WasKeyJustPressed(Key.UpArrow))
        {
            TryMove(new Vector2Int(0, 1));
        }
        else if (WasKeyJustPressed(Key.S) || WasKeyJustPressed(Key.DownArrow))
        {
            TryMove(new Vector2Int(0, -1));
        }
        else if (WasKeyJustPressed(Key.A) || WasKeyJustPressed(Key.LeftArrow))
        {
            TryMove(new Vector2Int(-1, 0));
        }
        else if (WasKeyJustPressed(Key.D) || WasKeyJustPressed(Key.RightArrow))
        {
            TryMove(new Vector2Int(1, 0));
        }
    }

    private bool WasKeyJustPressed(Key key)
    {
        if (Keyboard.current != null)
        {
            var keyControl = Keyboard.current[key];
            if (keyControl != null && keyControl.wasPressedThisFrame) return true;
        }

        // Legacy fallback
        KeyCode kc = KeyCode.None;
        switch (key)
        {
            case Key.W: kc = KeyCode.W; break;
            case Key.S: kc = KeyCode.S; break;
            case Key.A: kc = KeyCode.A; break;
            case Key.D: kc = KeyCode.D; break;
            case Key.UpArrow: kc = KeyCode.UpArrow; break;
            case Key.DownArrow: kc = KeyCode.DownArrow; break;
            case Key.LeftArrow: kc = KeyCode.LeftArrow; break;
            case Key.RightArrow: kc = KeyCode.RightArrow; break;
            case Key.Enter: kc = KeyCode.Return; break;
            case Key.Space: kc = KeyCode.Space; break;
        }
        return kc != KeyCode.None && Input.GetKeyDown(kc);
    }

    public void TryMove(Vector2Int delta)
    {
        Vector2Int target = currentFloor.currentPos + delta;
        if (currentFloor.CanMoveTo(target))
        {
            previousPos = currentFloor.currentPos;
            currentFloor.currentPos = target;

            DungeonRoom r = currentFloor.CurrentRoom;
            r.isVisited = true;
            r.isDiscovered = true;
            DungeonFloorGenerator.DiscoverAdjacent(currentFloor, target);

            RefreshMapVisuals();
            CheckCurrentRoomEncounter();
        }
    }

    // =========================================================================
    // ENCOUNTER SYSTEM
    // =========================================================================

    private void CheckCurrentRoomEncounter()
    {
        DungeonRoom room = currentFloor.CurrentRoom;
        if (room == null) return;

        // Never show popup for Start or Empty rooms, or already resolved rooms
        if (room.type == RoomType.Start || room.type == RoomType.Empty || room.isResolved)
        {
            return;
        }

        ShowEncounter(room);
    }

    private void ShowEncounter(DungeonRoom room)
    {
        if (encounterModal == null) return;

        encounterTitleText.text = room.title;
        encounterDescText.text = room.description;

        primaryActionButton.onClick.RemoveAllListeners();
        secondaryActionButton.onClick.RemoveAllListeners();
        secondaryActionButton.gameObject.SetActive(false);

        switch (room.type)
        {
            case RoomType.Combat:
                encounterTitleText.text = "[COMBAT] " + room.title;
                primaryActionText.text = "Engage in Battle";
                secondaryActionText.text = "Retreat Back";
                secondaryActionButton.gameObject.SetActive(true);

                primaryActionButton.onClick.AddListener(() =>
                {
                    encounterModal.SetActive(false);
                    StartCombatEncounter(room);
                });

                secondaryActionButton.onClick.AddListener(() =>
                {
                    // Retreat to previous room
                    currentFloor.currentPos = previousPos;
                    encounterModal.SetActive(false);
                    RefreshMapVisuals();
                });
                break;

            case RoomType.Event:
                encounterTitleText.text = "[EVENT] " + room.title;
                primaryActionText.text = "Investigate";
                secondaryActionText.text = "Pass By";
                secondaryActionButton.gameObject.SetActive(true);

                primaryActionButton.onClick.AddListener(() =>
                {
                    if (DialogueUI.Instance != null)
                    {
                        DialogueSequence seq = DialogueLibrary.GetEventDialogue(room.title, null);
                        if (seq != null)
                        {
                            encounterModal.SetActive(false);
                            DialogueUI.Instance.PlayDialogue(seq, onComplete: () =>
                            {
                                int eventLumi = room.title.IndexOf("Stele", StringComparison.OrdinalIgnoreCase) >= 0 ? 35 : 20;
                                CurrencyManager.Instance?.AddLumieres(eventLumi);
                                room.isResolved = true;
                                RefreshMapVisuals();
                            });
                            return;
                        }
                    }

                    room.isResolved = true;
                    encounterDescText.text += "\n\nYou carefully inspected the area and felt a soothing surge of energy.";
                    primaryActionText.text = "Finish";
                    secondaryActionButton.gameObject.SetActive(false);
                    primaryActionButton.onClick.RemoveAllListeners();
                    primaryActionButton.onClick.AddListener(() =>
                    {
                        encounterModal.SetActive(false);
                        RefreshMapVisuals();
                    });
                });

                secondaryActionButton.onClick.AddListener(() =>
                {
                    room.isResolved = true;
                    encounterModal.SetActive(false);
                    RefreshMapVisuals();
                });
                break;

            case RoomType.Treasure:
                encounterTitleText.text = "[TREASURE] " + room.title;
                primaryActionText.text = "Claim Loot!";
                primaryActionButton.onClick.AddListener(() =>
                {
                    room.isResolved = true;
                    int loot = UnityEngine.Random.Range(40, 75);
                    CurrencyManager.Instance?.AddLumieres(loot);
                    encounterDescText.text += $"\n\nYou discovered an abandoned stash containing <color=#FFDD55>+{loot} Lumieres</color> and valuable supplies!";
                    primaryActionText.text = "Continue";
                    primaryActionButton.onClick.RemoveAllListeners();
                    primaryActionButton.onClick.AddListener(() =>
                    {
                        encounterModal.SetActive(false);
                        RefreshMapVisuals();
                    });
                });
                break;

            case RoomType.Exit:
                encounterTitleText.text = "[EXIT] STAIRCASE TO NEXT FLOOR";
                encounterDescText.text = $"You found the passage to Floor {currentFloor.floorNumber + 1}!\nDo you wish to descend deeper into the catacombs, or continue exploring this floor?";
                primaryActionText.text = $"Descend to Floor {currentFloor.floorNumber + 1}";
                secondaryActionText.text = "Stay on Floor";
                secondaryActionButton.gameObject.SetActive(true);

                primaryActionButton.onClick.AddListener(() =>
                {
                    encounterModal.SetActive(false);
                    OnFloorCompleted?.Invoke(currentFloor.floorNumber);
                    LoadFloor(currentFloor.floorNumber + 1);
                });

                secondaryActionButton.onClick.AddListener(() =>
                {
                    encounterModal.SetActive(false);
                });
                break;

            default:
                room.isResolved = true;
                return;
        }

        encounterModal.SetActive(true);
    }

    private void StartCombatEncounter(DungeonRoom room)
    {
        currentCombatRoom = room;
        if (combatManager != null)
        {
            List<EnemyType> list = new List<EnemyType>();
            // Max 3 enemies on battlefield
            int count = (currentFloor.floorNumber <= 1) ? UnityEngine.Random.Range(1, 3) : UnityEngine.Random.Range(2, 4);
            for (int i = 0; i < count; i++)
            {
                list.Add(UnityEngine.Random.value < 0.55f ? EnemyType.SlimeGirl : EnemyType.Succubus);
            }
            Hide();
            combatManager.StartCombat(list, currentFloor.floorNumber);
        }
        else
        {
            // Fallback
            room.isResolved = true;
            RefreshMapVisuals();
        }
    }

    public void OnCombatRoomResolved()
    {
        if (currentCombatRoom != null)
        {
            currentCombatRoom.isResolved = true;
            currentCombatRoom = null;
        }
        Show();
        RefreshMapVisuals();
    }

    private void HandleSurrenderClicked()
    {
        CurrencyManager.Instance?.ConvertRunCurrencyToEndExpedition();
        OnSurrenderToLobby?.Invoke();
    }

    // =========================================================================
    // PROCEDURAL MAP VISUALIZATION (CAMERA TRACKING & INVISIBLE BOUNDARY)
    // =========================================================================

    private void RebuildMapVisuals()
    {
        if (mapContainer == null) return;

        foreach (Transform child in mapContainer)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        roomTiles.Clear();

        float tileSize = 68f;
        float spacing = 16f;
        float step = tileSize + spacing;

        foreach (var kvp in currentFloor.rooms)
        {
            Vector2Int pos = kvp.Key;
            DungeonRoom room = kvp.Value;

            GameObject tile = new GameObject($"Tile_{pos.x}_{pos.y}", typeof(RectTransform));
            tile.transform.SetParent(mapContainer, false);

            RectTransform tr = tile.GetComponent<RectTransform>();
            tr.sizeDelta = new Vector2(tileSize, tileSize);
            tr.anchoredPosition = new Vector2(pos.x * step, pos.y * step);

            Image img = tile.AddComponent<Image>();
            img.color = GetRoomColor(room, pos == currentFloor.currentPos);

            Button btn = tile.AddComponent<Button>();
            btn.targetGraphic = img;
            Vector2Int targetPos = pos;
            btn.onClick.AddListener(() =>
            {
                if (currentFloor.CanMoveTo(targetPos))
                {
                    TryMove(targetPos - currentFloor.currentPos);
                }
            });

            // Icon/Text
            GameObject txtObj = new GameObject("Icon", typeof(RectTransform));
            txtObj.transform.SetParent(tile.transform, false);
            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.fontSize = 26;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            txt.raycastTarget = false;

            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            bool isCurrent = (pos == currentFloor.currentPos);
            bool isAdjacent = currentFloor.CanMoveTo(pos);
            bool isKnown = isCurrent || isAdjacent || room.isVisited || room.isDiscovered;
            tile.SetActive(isKnown);

            roomTiles[pos] = tile;
        }

        // Snap camera immediately to the player's starting room
        targetMapContainerPos = -new Vector2(currentFloor.currentPos.x * step, currentFloor.currentPos.y * step);
        mapContainer.anchoredPosition = targetMapContainerPos;

        UpdateTileBoundaryVisibility();
        RefreshMapVisuals();
    }

    private void UpdateTileBoundaryVisibility()
    {
        if (mapViewport == null || mapContainer == null || currentFloor == null) return;

        // Viewport half-extents defining the invisible boundary
        float halfW = (mapViewport.rect.width > 0 ? mapViewport.rect.width * 0.5f : 550f) + 34f;
        float halfH = (mapViewport.rect.height > 0 ? mapViewport.rect.height * 0.5f : 400f) + 34f;

        foreach (var kvp in roomTiles)
        {
            Vector2Int pos = kvp.Key;
            GameObject tileObj = kvp.Value;
            if (tileObj == null) continue;

            if (!currentFloor.rooms.TryGetValue(pos, out DungeonRoom room))
            {
                if (tileObj.activeSelf) tileObj.SetActive(false);
                continue;
            }

            // Only known rooms (current, adjacent, visited, or discovered) can be visible
            bool isCurrent = (pos == currentFloor.currentPos);
            bool isAdjacent = currentFloor.CanMoveTo(pos);
            bool isKnown = isCurrent || isAdjacent || room.isVisited || room.isDiscovered;

            if (!isKnown)
            {
                if (tileObj.activeSelf) tileObj.SetActive(false);
                continue;
            }

            // Position of tile relative to mapViewport center
            Vector2 tilePosInViewport = (Vector2)tileObj.transform.localPosition + mapContainer.anchoredPosition;

            bool isInside = Mathf.Abs(tilePosInViewport.x) <= halfW && Mathf.Abs(tilePosInViewport.y) <= halfH;
            if (tileObj.activeSelf != isInside)
            {
                tileObj.SetActive(isInside);
            }
        }
    }

    private void RefreshMapVisuals()
    {
        if (currentFloor == null) return;

        foreach (var kvp in currentFloor.rooms)
        {
            Vector2Int pos = kvp.Key;
            DungeonRoom room = kvp.Value;

            if (!roomTiles.TryGetValue(pos, out GameObject tile) || tile == null) continue;

            bool isCurrent = (pos == currentFloor.currentPos);
            bool isAdjacent = currentFloor.CanMoveTo(pos);
            bool isKnown = isCurrent || isAdjacent || room.isVisited || room.isDiscovered;

            // Fog of war: completely hide cells that are not yet discovered, visited, or adjacent
            if (!isKnown)
            {
                if (tile.activeSelf) tile.SetActive(false);
                continue;
            }

            Image img = tile.GetComponent<Image>();
            TextMeshProUGUI txt = tile.GetComponentInChildren<TextMeshProUGUI>();
            Button btn = tile.GetComponent<Button>();

            btn.interactable = isAdjacent || isCurrent;
            img.color = GetRoomColor(room, isCurrent);

            if (isCurrent)
            {
                txt.text = "@"; // Player Avatar / Current Location
                txt.color = new Color(1f, 0.95f, 0.30f, 1f);
            }
            else if (!room.isVisited)
            {
                // Discovered / adjacent but not yet stepped on
                txt.text = "?";
                txt.color = new Color(0.70f, 0.75f, 0.85f, 0.70f);
            }
            else
            {
                // Visited room (including completed ones)
                txt.color = Color.white;
                switch (room.type)
                {
                    case RoomType.Start:
                        txt.text = "S";
                        break;
                    case RoomType.Empty:
                        txt.text = "";
                        break;
                    case RoomType.Combat:
                        txt.text = room.isResolved ? "-" : "X";
                        if (room.isResolved) txt.color = new Color(0.65f, 0.65f, 0.70f, 0.7f);
                        break;
                    case RoomType.Event:
                        txt.text = room.isResolved ? "-" : "!";
                        if (room.isResolved) txt.color = new Color(0.65f, 0.65f, 0.70f, 0.7f);
                        break;
                    case RoomType.Treasure:
                        txt.text = room.isResolved ? "-" : "$";
                        if (room.isResolved) txt.color = new Color(0.65f, 0.65f, 0.70f, 0.7f);
                        break;
                    case RoomType.Exit:
                        txt.text = ">>";
                        break;
                }
            }
        }

        UpdateTileBoundaryVisibility();
    }

    private Color GetRoomColor(DungeonRoom room, bool isCurrent)
    {
        if (isCurrent)
        {
            return new Color(0.20f, 0.50f, 0.75f, 1f); // Bright blue glow for player
        }

        if (!room.isVisited)
        {
            return new Color(0.22f, 0.25f, 0.32f, 0.90f); // Discovered unexplored room
        }

        switch (room.type)
        {
            case RoomType.Start:
                return new Color(0.20f, 0.40f, 0.30f, 0.90f);
            case RoomType.Empty:
                return new Color(0.18f, 0.20f, 0.26f, 0.85f);
            case RoomType.Combat:
                return room.isResolved ? new Color(0.25f, 0.22f, 0.25f, 0.85f) : new Color(0.60f, 0.20f, 0.20f, 0.95f);
            case RoomType.Event:
                return room.isResolved ? new Color(0.25f, 0.25f, 0.22f, 0.85f) : new Color(0.55f, 0.40f, 0.15f, 0.95f);
            case RoomType.Treasure:
                return room.isResolved ? new Color(0.22f, 0.26f, 0.22f, 0.85f) : new Color(0.20f, 0.55f, 0.30f, 0.95f);
            case RoomType.Exit:
                return new Color(0.50f, 0.25f, 0.65f, 0.95f);
            default:
                return new Color(0.20f, 0.20f, 0.25f, 0.85f);
        }
    }

    // =========================================================================
    // DYNAMIC SETUP (CLEAN & NON-OBSCURING)
    // =========================================================================

    public static ExpeditionMapUI CreateDynamic(Transform parent)
    {
        GameObject panelObj = new GameObject("ExpeditionPanel", typeof(RectTransform));
        panelObj.transform.SetParent(parent, false);

        RectTransform pr = panelObj.GetComponent<RectTransform>();
        pr.anchorMin = Vector2.zero;
        pr.anchorMax = Vector2.one;
        pr.sizeDelta = Vector2.zero;
        pr.anchoredPosition = Vector2.zero;

        // NOTE: No dimming or dark background image! Completely transparent screen.

        ExpeditionMapUI expUI = panelObj.AddComponent<ExpeditionMapUI>();
        expUI.expeditionPanel = panelObj;

        // Top Header Title (Centered at top)
        GameObject titleObj = new GameObject("FloorTitle", typeof(RectTransform));
        titleObj.transform.SetParent(panelObj.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "CATACOMBS - FLOOR 1";
        title.fontSize = 32;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.92f, 0.80f, 0.50f, 1f);

        RectTransform tr = titleObj.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0.5f, 1f);
        tr.anchorMax = new Vector2(0.5f, 1f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -25f);
        tr.sizeDelta = new Vector2(500f, 45f);
        expUI.floorTitleText = title;

        // Player Status HUD (Top Left, transparent background without bounding box)
        GameObject statsObj = new GameObject("PlayerStatusHUD", typeof(RectTransform));
        statsObj.transform.SetParent(panelObj.transform, false);
        RectTransform psr = statsObj.GetComponent<RectTransform>();
        psr.anchorMin = new Vector2(0f, 1f);
        psr.anchorMax = new Vector2(0f, 1f);
        psr.pivot = new Vector2(0f, 1f);
        psr.anchoredPosition = new Vector2(25f, -25f);
        psr.sizeDelta = new Vector2(330f, 440f);
        expUI.playerStatusPanel = statsObj;

        VerticalLayoutGroup vlg = statsObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.spacing = 8f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Floating Tooltip Popup for Expedition Map (fixed position to the right of enlarged stats panel)
        GameObject tipObj = new GameObject("StatTooltipPopup", typeof(RectTransform));
        tipObj.transform.SetParent(panelObj.transform, false);
        Image tipBg = tipObj.AddComponent<Image>();
        tipBg.color = new Color(0.06f, 0.08f, 0.12f, 0.98f);
        Outline tipOutline = tipObj.AddComponent<Outline>();
        tipOutline.effectColor = new Color(0.25f, 0.30f, 0.40f, 0.60f);
        tipOutline.effectDistance = new Vector2(1f, -1f);

        RectTransform tipRt = tipObj.GetComponent<RectTransform>();
        tipRt.sizeDelta = new Vector2(280f, 90f);
        tipRt.anchorMin = new Vector2(0f, 1f);
        tipRt.anchorMax = new Vector2(0f, 1f);
        tipRt.pivot = new Vector2(0f, 1f);
        tipRt.anchoredPosition = new Vector2(340f, -25f);

        GameObject tipTxtObj = new GameObject("Text", typeof(RectTransform));
        tipTxtObj.transform.SetParent(tipObj.transform, false);
        TextMeshProUGUI tipTxt = tipTxtObj.AddComponent<TextMeshProUGUI>();
        tipTxt.fontSize = 13;
        tipTxt.color = new Color(0.92f, 0.94f, 0.98f, 1f);
        tipTxt.raycastTarget = false;
        tipTxt.textWrappingMode = TextWrappingModes.Normal;
        RectTransform tipTxtRt = tipTxtObj.GetComponent<RectTransform>();
        tipTxtRt.anchorMin = Vector2.zero;
        tipTxtRt.anchorMax = Vector2.one;
        tipTxtRt.offsetMin = new Vector2(10f, 8f);
        tipTxtRt.offsetMax = new Vector2(-10f, -8f);

        TooltipPopup tipPopup = tipObj.AddComponent<TooltipPopup>();
        tipPopup.panel = tipObj;
        tipPopup.text = tipTxt;
        tipPopup.rectTransform = tipRt;
        tipPopup.SetFixedPosition(new Vector2(340f, -25f));
        tipObj.SetActive(false);
        expUI.statTooltipPopup = tipPopup;

        Vector2 statIconSize = new Vector2(44f, 44f);
        expUI.playerHpText = StatRowFactory.Create(statsObj.transform, StatIconDatabase.HpIcon, "HP", new Color(0.75f, 0.15f, 0.20f, 0.95f), "Health Points (HP):\nYour vital stamina. If reduced to 0, corruption activates.", "30 / 30", new Color(0.35f, 0.90f, 0.45f, 1f), tipPopup, statIconSize);
        expUI.playerUpperClothesText = StatRowFactory.Create(statsObj.transform, StatIconDatabase.TopArmorIcon, "U-ARM", new Color(0.20f, 0.55f, 0.85f, 0.95f), "Upper Armor (U-ARM):\nDurability of your upper garment. Protects your chest and provides damage reduction.", "65 / 65", new Color(0.40f, 0.80f, 1f, 1f), tipPopup, statIconSize);
        expUI.playerLowerClothesText = StatRowFactory.Create(statsObj.transform, StatIconDatabase.BottomArmorIcon, "L-ARM", new Color(0.18f, 0.45f, 0.75f, 0.95f), "Lower Armor (L-ARM):\nDurability of your lower garment. Protects your modesty and legs, and provides damage reduction.", "65 / 65", new Color(0.40f, 0.80f, 1f, 1f), tipPopup, statIconSize);
        expUI.playerClothesText = expUI.playerUpperClothesText;
        expUI.playerShieldText = StatRowFactory.Create(statsObj.transform, StatIconDatabase.BlockIcon, "SHD", new Color(0.12f, 0.55f, 0.65f, 0.95f), "Shield Guard:\nAbsorbs incoming physical damage before Health is affected.", "0", new Color(0.40f, 0.90f, 1f, 1f), tipPopup, statIconSize);
        expUI.playerLustText = StatRowFactory.Create(statsObj.transform, StatIconDatabase.LustIcon, "LUST", new Color(0.75f, 0.20f, 0.55f, 0.95f), "Lust / Seduction:\nReaching 100% triggers climax submission and adds a Climax stack!", "0%  (0/3)", new Color(1f, 0.45f, 0.75f, 1f), tipPopup, statIconSize);
        expUI.playerCorruptionText = StatRowFactory.Create(statsObj.transform, StatIconDatabase.CorruptionIcon, "CORR", new Color(0.50f, 0.15f, 0.65f, 0.95f), "Corruption:\nDefeat adds +1 Corruption and preserves you with 1 HP. If it reaches 3/3, you are fully consumed!", "0 / 3", new Color(0.85f, 0.45f, 0.95f, 1f), tipPopup, statIconSize);
        expUI.playerLumieresText = StatRowFactory.Create(statsObj.transform, StatIconDatabase.LumiereIcon, "LUM", new Color(0.85f, 0.65f, 0.15f, 0.95f), "Lumieres:\nRun currency gathered from battles, chests, and cavern events. Converted into Lustid upon returning to Sanctuary.", "0", new Color(1f, 0.88f, 0.35f, 1f), tipPopup, statIconSize);

        // Surrender Button (Top Right)
        GameObject surrObj = new GameObject("SurrenderButton", typeof(RectTransform));
        surrObj.transform.SetParent(panelObj.transform, false);
        Image surrBg = surrObj.AddComponent<Image>();
        surrBg.color = new Color(0.55f, 0.20f, 0.20f, 0.90f);

        Button surrBtn = surrObj.AddComponent<Button>();
        surrBtn.targetGraphic = surrBg;

        RectTransform sbr = surrObj.GetComponent<RectTransform>();
        sbr.anchorMin = new Vector2(1f, 1f);
        sbr.anchorMax = new Vector2(1f, 1f);
        sbr.pivot = new Vector2(1f, 1f);
        sbr.anchoredPosition = new Vector2(-40f, -25f);
        sbr.sizeDelta = new Vector2(200f, 46f);

        GameObject surrTxtObj = new GameObject("Text", typeof(RectTransform));
        surrTxtObj.transform.SetParent(surrObj.transform, false);
        TextMeshProUGUI surrTxt = surrTxtObj.AddComponent<TextMeshProUGUI>();
        surrTxt.text = "Surrender";
        surrTxt.fontSize = 20;
        surrTxt.fontStyle = FontStyles.Bold;
        surrTxt.alignment = TextAlignmentOptions.Center;
        surrTxt.color = Color.white;
        surrTxt.raycastTarget = false;

        RectTransform stxtr = surrTxtObj.GetComponent<RectTransform>();
        stxtr.anchorMin = Vector2.zero;
        stxtr.anchorMax = Vector2.one;
        stxtr.sizeDelta = Vector2.zero;

        expUI.surrenderButton = surrBtn;

        // Map Viewport (Invisible Boundary with RectMask2D, Centered on Screen)
        GameObject viewportObj = new GameObject("MapViewport", typeof(RectTransform));
        viewportObj.transform.SetParent(panelObj.transform, false);

        RectTransform vr = viewportObj.GetComponent<RectTransform>();
        vr.anchorMin = new Vector2(0.5f, 0.5f);
        vr.anchorMax = new Vector2(0.5f, 0.5f);
        vr.pivot = new Vector2(0.5f, 0.5f);
        vr.anchoredPosition = Vector2.zero;
        vr.sizeDelta = new Vector2(1100f, 800f);
        viewportObj.AddComponent<RectMask2D>();
        expUI.mapViewport = vr;

        // Map Content Container (Smoothly moving inside Viewport to follow the player)
        GameObject mapObj = new GameObject("MapContainer", typeof(RectTransform));
        mapObj.transform.SetParent(viewportObj.transform, false);

        RectTransform mr = mapObj.GetComponent<RectTransform>();
        mr.anchorMin = new Vector2(0.5f, 0.5f);
        mr.anchorMax = new Vector2(0.5f, 0.5f);
        mr.pivot = new Vector2(0.5f, 0.5f);
        mr.anchoredPosition = Vector2.zero;
        mr.sizeDelta = Vector2.zero;
        expUI.mapContainer = mr;

        // Controls Hint (Centered at Bottom)
        GameObject hintObj = new GameObject("ControlsHint", typeof(RectTransform));
        hintObj.transform.SetParent(panelObj.transform, false);
        TextMeshProUGUI hint = hintObj.AddComponent<TextMeshProUGUI>();
        hint.text = "Controls: [W][A][S][D] or [Arrow Keys] to move between rooms. Click adjacent room. [ESC] for Inventory.";
        hint.fontSize = 18;
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = new Color(0.65f, 0.70f, 0.80f, 0.85f);

        RectTransform hr = hintObj.GetComponent<RectTransform>();
        hr.anchorMin = new Vector2(0.5f, 0f);
        hr.anchorMax = new Vector2(0.5f, 0f);
        hr.pivot = new Vector2(0.5f, 0f);
        hr.anchoredPosition = new Vector2(0f, 20f);
        hr.sizeDelta = new Vector2(1000f, 32f);
        expUI.controlsHintText = hint;

        // Encounter Modal Panel (Popup Dialog Centered on Screen)
        GameObject modalObj = new GameObject("EncounterModal", typeof(RectTransform));
        modalObj.transform.SetParent(panelObj.transform, false);

        RectTransform modr = modalObj.GetComponent<RectTransform>();
        modr.anchorMin = new Vector2(0.5f, 0.5f);
        modr.anchorMax = new Vector2(0.5f, 0.5f);
        modr.pivot = new Vector2(0.5f, 0.5f);
        modr.anchoredPosition = Vector2.zero;
        modr.sizeDelta = new Vector2(640f, 420f);

        Image modalBg = modalObj.AddComponent<Image>();
        modalBg.color = new Color(0.12f, 0.14f, 0.19f, 0.98f);

        // Modal Title
        GameObject mTitleObj = new GameObject("Title", typeof(RectTransform));
        mTitleObj.transform.SetParent(modalObj.transform, false);
        TextMeshProUGUI mt = mTitleObj.AddComponent<TextMeshProUGUI>();
        mt.fontSize = 26;
        mt.fontStyle = FontStyles.Bold;
        mt.alignment = TextAlignmentOptions.Center;
        mt.color = new Color(0.95f, 0.85f, 0.50f, 1f);

        RectTransform mtr = mTitleObj.GetComponent<RectTransform>();
        mtr.anchorMin = new Vector2(0f, 1f);
        mtr.anchorMax = new Vector2(1f, 1f);
        mtr.pivot = new Vector2(0.5f, 1f);
        mtr.anchoredPosition = new Vector2(0f, -20f);
        mtr.sizeDelta = new Vector2(-40f, 45f);
        expUI.encounterTitleText = mt;

        // Modal Description
        GameObject mDescObj = new GameObject("Description", typeof(RectTransform));
        mDescObj.transform.SetParent(modalObj.transform, false);
        TextMeshProUGUI md = mDescObj.AddComponent<TextMeshProUGUI>();
        md.fontSize = 20;
        md.alignment = TextAlignmentOptions.TopLeft;
        md.color = new Color(0.85f, 0.88f, 0.92f, 1f);
        md.lineSpacing = 12;

        RectTransform mdr = mDescObj.GetComponent<RectTransform>();
        mdr.anchorMin = new Vector2(0f, 0f);
        mdr.anchorMax = new Vector2(1f, 1f);
        mdr.pivot = new Vector2(0.5f, 0.5f);
        mdr.anchoredPosition = new Vector2(0f, 15f);
        mdr.sizeDelta = new Vector2(-50f, -150f);
        expUI.encounterDescText = md;

        // Primary Action Button
        GameObject pBtnObj = new GameObject("PrimaryActionButton", typeof(RectTransform));
        pBtnObj.transform.SetParent(modalObj.transform, false);
        Image pBg = pBtnObj.AddComponent<Image>();
        pBg.color = new Color(0.22f, 0.55f, 0.35f, 1f);

        Button pBtn = pBtnObj.AddComponent<Button>();
        pBtn.targetGraphic = pBg;

        RectTransform pbr = pBtnObj.GetComponent<RectTransform>();
        pbr.anchorMin = new Vector2(0.5f, 0f);
        pbr.anchorMax = new Vector2(0.5f, 0f);
        pbr.pivot = new Vector2(0.5f, 0f);
        pbr.anchoredPosition = new Vector2(0f, 25f);
        pbr.sizeDelta = new Vector2(340f, 52f);

        GameObject pTxtObj = new GameObject("Text", typeof(RectTransform));
        pTxtObj.transform.SetParent(pBtnObj.transform, false);
        TextMeshProUGUI pt = pTxtObj.AddComponent<TextMeshProUGUI>();
        pt.fontSize = 22;
        pt.fontStyle = FontStyles.Bold;
        pt.alignment = TextAlignmentOptions.Center;
        pt.color = Color.white;
        pt.raycastTarget = false;

        RectTransform ptr = pTxtObj.GetComponent<RectTransform>();
        ptr.anchorMin = Vector2.zero;
        ptr.anchorMax = Vector2.one;
        ptr.sizeDelta = Vector2.zero;

        expUI.primaryActionButton = pBtn;
        expUI.primaryActionText = pt;

        // Secondary Action Button
        GameObject sBtnObj = new GameObject("SecondaryActionButton", typeof(RectTransform));
        sBtnObj.transform.SetParent(modalObj.transform, false);
        Image sBg = sBtnObj.AddComponent<Image>();
        sBg.color = new Color(0.35f, 0.38f, 0.45f, 1f);

        Button sBtn = sBtnObj.AddComponent<Button>();
        sBtn.targetGraphic = sBg;

        RectTransform sbr2 = sBtnObj.GetComponent<RectTransform>();
        sbr2.anchorMin = new Vector2(0.5f, 0f);
        sbr2.anchorMax = new Vector2(0.5f, 0f);
        sbr2.pivot = new Vector2(0.5f, 0f);
        sbr2.anchoredPosition = new Vector2(0f, 85f);
        sbr2.sizeDelta = new Vector2(340f, 48f);

        GameObject sTxtObj = new GameObject("Text", typeof(RectTransform));
        sTxtObj.transform.SetParent(sBtnObj.transform, false);
        TextMeshProUGUI st2 = sTxtObj.AddComponent<TextMeshProUGUI>();
        st2.fontSize = 20;
        st2.fontStyle = FontStyles.Bold;
        st2.alignment = TextAlignmentOptions.Center;
        st2.color = Color.white;
        st2.raycastTarget = false;

        RectTransform str2 = sTxtObj.GetComponent<RectTransform>();
        str2.anchorMin = Vector2.zero;
        str2.anchorMax = Vector2.one;
        str2.sizeDelta = Vector2.zero;

        expUI.secondaryActionButton = sBtn;
        expUI.secondaryActionText = st2;

        modalObj.SetActive(false);
        expUI.encounterModal = modalObj;

        panelObj.SetActive(false);
        return expUI;
    }
}

