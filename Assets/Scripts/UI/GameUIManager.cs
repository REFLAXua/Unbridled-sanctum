using UnbridledSanctum.Combat;
using UnbridledSanctum.UI;
using UnbridledSanctum.Data;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Top-level UI manager. Orchestrates game flow:
/// StartMenu -> CharacterCreator -> Lobby (Sanctuary Cave with Wardrobe) <-> Expedition (Fullscreen Map).
/// </summary>
public class GameUIManager : MonoBehaviour
{
    public enum GameFlowState
    {
        StartMenu,
        CharacterCreator,
        Lobby,
        Expedition
    }

    [Header("Flow State")]
    public GameFlowState currentState = GameFlowState.StartMenu;

    [Header("Core References")]
    public CharacterState characterState;
    public PaperDollController paperDoll;
    public InventoryManager inventoryManager;
    public InventoryUI inventoryUI;

    [Header("Screens")]
    public StartMenuUI startMenuUI;
    public CharacterCreatorUI characterCreatorUI;
    public LobbyUI lobbyUI;
    public ExpeditionMapUI expeditionMapUI;
    public CombatUI combatUI;
    public CombatManager combatManager;

    private int maxFloorReached = 1;

    private void Awake()
    {
        EnsureScreens();
    }

    private void Start()
    {
        // Initialize PaperDoll with current character state
        if (paperDoll != null && characterState != null)
        {
            paperDoll.Initialize(characterState);
        }

        // Initialize FleshTransformationManager
        if (FleshTransformationManager.Instance == null)
        {
            GameObject ftmObj = new GameObject("FleshTransformationManager");
            ftmObj.AddComponent<FleshTransformationManager>();
        }
        FleshTransformationManager.Instance.Initialize(characterState);

        // Bind StartMenu
        if (startMenuUI != null)
        {
            startMenuUI.OnPlayClicked += HandlePlayClicked;
        }

        // Bind CharacterCreator
        if (characterCreatorUI != null)
        {
            characterCreatorUI.OnCharacterAccepted += HandleCharacterAccepted;
        }

        // Bind Lobby & Expedition
        if (lobbyUI != null)
        {
            lobbyUI.OnStartExpedition += HandleStartExpedition;
        }

        if (expeditionMapUI != null)
        {
            expeditionMapUI.OnSurrenderToLobby += HandleSurrenderToLobby;
            expeditionMapUI.OnFloorCompleted += HandleFloorCompleted;
        }

        // Setup initial view
        if (startMenuUI != null)
        {
            SetState(GameFlowState.StartMenu);
        }
        else
        {
            SetState(GameFlowState.Lobby);
        }
    }

    private void EnsureScreens()
    {
        CurrencyManager.EnsureExists();

        Transform canvasTrans = transform;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null) canvasTrans = canvas.transform;

        if (lobbyUI == null)
        {
            lobbyUI = canvasTrans.GetComponentInChildren<LobbyUI>(true);
            if (lobbyUI == null)
            {
                lobbyUI = LobbyUI.CreateDynamic(canvasTrans, characterState, inventoryManager);
            }
            else
            {
                lobbyUI.Initialize(characterState, inventoryManager);
            }
        }
        else
        {
            lobbyUI.Initialize(characterState, inventoryManager);
        }

        if (expeditionMapUI == null)
        {
            expeditionMapUI = canvasTrans.GetComponentInChildren<ExpeditionMapUI>(true);
            if (expeditionMapUI == null)
            {
                expeditionMapUI = ExpeditionMapUI.CreateDynamic(canvasTrans);
            }
        }

        if (combatManager == null)
        {
            combatManager = gameObject.GetComponent<CombatManager>();
            if (combatManager == null) combatManager = gameObject.AddComponent<CombatManager>();
        }
        combatManager.Initialize(characterState);

        if (combatUI == null)
        {
            combatUI = canvasTrans.GetComponentInChildren<CombatUI>(true);
            if (combatUI == null)
            {
                combatUI = CombatUI.CreateDynamic(canvasTrans, combatManager);
            }
        }

        combatUI.OnCombatVictoryFinished += HandleCombatVictoryFinished;
        combatUI.OnCombatDefeatToLobby += HandleCombatDefeatToLobby;
        expeditionMapUI.BindCombat(combatManager, combatUI);

        if (PostCombatTransitionUI.Instance == null)
        {
            PostCombatTransitionUI.CreateDynamic(canvasTrans);
        }

        if (DialogueUI.Instance == null)
        {
            DialogueUI dialogueUI = canvasTrans.GetComponentInChildren<DialogueUI>(true);
            if (dialogueUI == null)
            {
                dialogueUI = DialogueUI.CreateDynamic(canvasTrans, characterState);
            }
            else
            {
                dialogueUI.Initialize(characterState);
            }
        }
    }

    private void HandleCombatVictoryFinished()
    {
        if (combatUI != null) combatUI.Hide();
        if (expeditionMapUI != null) expeditionMapUI.OnCombatRoomResolved();
    }

    private void HandleCombatDefeatToLobby()
    {
        if (combatUI != null) combatUI.Hide();
        if (expeditionMapUI != null) expeditionMapUI.Hide();
        if (combatManager != null) combatManager.RepairPlayerClothes();
        if (CurrencyManager.Instance != null) CurrencyManager.Instance.ConvertRunCurrencyToEndExpedition();
        SetState(GameFlowState.Lobby);
    }

    private void OnDestroy()
    {
        if (startMenuUI != null)
            startMenuUI.OnPlayClicked -= HandlePlayClicked;

        if (characterCreatorUI != null)
            characterCreatorUI.OnCharacterAccepted -= HandleCharacterAccepted;

        if (lobbyUI != null)
            lobbyUI.OnStartExpedition -= HandleStartExpedition;

        if (expeditionMapUI != null)
            expeditionMapUI.OnSurrenderToLobby -= HandleSurrenderToLobby;
            expeditionMapUI.OnFloorCompleted -= HandleFloorCompleted;
    }

    private void Update()
    {
        // Only allow toggling popup inventory during an Expedition
        if (currentState == GameFlowState.Expedition)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (inventoryUI != null)
                {
                    inventoryUI.ToggleInventory();
                }
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (inventoryUI != null)
                {
                    inventoryUI.ToggleInventory();
                }
            }
        }
    }

    public void SetState(GameFlowState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case GameFlowState.StartMenu:
                if (startMenuUI != null) startMenuUI.Show();
                if (characterCreatorUI != null) characterCreatorUI.Hide();
                if (lobbyUI != null) lobbyUI.Hide();
                if (expeditionMapUI != null) expeditionMapUI.Hide(); if (combatUI != null) combatUI.Hide();
                if (paperDoll != null)
                {
                    paperDoll.gameObject.SetActive(false);
                    paperDoll.SetZoomControlsVisible(false);
                }
                break;

            case GameFlowState.CharacterCreator:
                if (startMenuUI != null) startMenuUI.Hide();
                if (characterCreatorUI != null) characterCreatorUI.Show();
                if (lobbyUI != null) lobbyUI.Hide();
                if (expeditionMapUI != null) expeditionMapUI.Hide(); if (combatUI != null) combatUI.Hide();
                if (paperDoll != null)
                {
                    paperDoll.gameObject.SetActive(true);
                    paperDoll.SetExpeditionMode(false);
                    paperDoll.SetZoomControlsVisible(true);
                }
                break;

            case GameFlowState.Lobby:
                if (startMenuUI != null) startMenuUI.Hide();
                if (characterCreatorUI != null) characterCreatorUI.Hide();
                if (expeditionMapUI != null) expeditionMapUI.Hide(); if (combatUI != null) combatUI.Hide();
                if (paperDoll != null)
                {
                    paperDoll.gameObject.SetActive(true);
                    paperDoll.SetExpeditionMode(false);
                    paperDoll.SetZoomControlsVisible(true);
                }
                if (lobbyUI != null) lobbyUI.Show(maxFloorReached);
                break;

            case GameFlowState.Expedition:
                if (startMenuUI != null) startMenuUI.Hide();
                if (characterCreatorUI != null) characterCreatorUI.Hide();
                if (lobbyUI != null) lobbyUI.Hide();
                if (paperDoll != null)
                {
                    paperDoll.gameObject.SetActive(true);
                    paperDoll.SetExpeditionMode(true);
                    paperDoll.SetZoomControlsVisible(false);
                }
                if (expeditionMapUI != null) expeditionMapUI.Show();
                break;
        }
    }

    private void HandlePlayClicked()
    {
        if (DialogueUI.Instance != null) DialogueUI.Instance.Initialize(characterState);
        SetState(GameFlowState.Lobby);
    }

    private void HandleCharacterAccepted()
    {
        if (DialogueUI.Instance != null) DialogueUI.Instance.Initialize(characterState);
        SetState(GameFlowState.Lobby);
    }

    private void HandleStartExpedition()
    {
        if (expeditionMapUI != null)
        {
            expeditionMapUI.StartNewExpedition(1);
        }
        SetState(GameFlowState.Expedition);
    }

    private void HandleSurrenderToLobby()
    {
        SetState(GameFlowState.Lobby);
    }

    private void HandleFloorCompleted(int completedFloor)
    {
        maxFloorReached = Mathf.Max(maxFloorReached, completedFloor + 1);
    }
}
