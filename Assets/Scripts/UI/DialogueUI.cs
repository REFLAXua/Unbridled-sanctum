using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnbridledSanctum.Combat;

namespace UnbridledSanctum.UI
{
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }

        [Header("Root & Backdrop")]
        public GameObject dialogueRoot;
        public Image backdropDimmer;

        [Header("Customizable Dialogue Database (Inspector)")]
        [Tooltip("Custom ScriptableObject database containing post-combat and dungeon event dialogues.")]
        public DialogueDatabaseSO dialogueDatabase;

        [Header("Left Speaker (Player - Rib-height Bust View)")]
        public RectTransform playerPortraitContainer;
        public CanvasGroup playerCanvasGroup;
        public GameObject playerCloneObj;
        public Image playerHairBack;
        public Image playerBody;
        public Image playerUnderwear;
        public Image playerClothes;
        public Image playerExpression;
        public Image playerBlush;
        public Image playerHairFront;
        public Image playerEyebrows;

        [Header("Right Speaker (Enemy/NPC - Rib-height Bust View)")]
        public RectTransform enemyPortraitContainer;
        public CanvasGroup enemyCanvasGroup;
        public Image enemyPortraitImage;
        public RectTransform enemyPortraitTransform;

        [Header("Dialogue Box")]
        public RectTransform dialogueBoxRect;
        public Image dialogueBoxBg;
        public TextMeshProUGUI speakerNameText;
        public TextMeshProUGUI dialogueMessageText;
        public GameObject continueIndicator;
        public Button dialogueClickArea;

        [Header("Runtime State")]
        private DialogueSequence currentSequence;
        private int currentLineIndex = -1;
        private bool isTyping = false;
        private Coroutine typingCoroutine;
        private string fullTextToType = "";
        private CharacterState cachedCharState;
        private PaperDollController cachedMainPd;
        private readonly Dictionary<Image, Color> playerBaseColors = new Dictionary<Image, Color>();

        public bool IsDialogueActive => dialogueRoot != null && dialogueRoot.activeSelf;
        public event Action OnDialogueCompleted;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            Canvas c = GetComponent<Canvas>();
            if (c == null) c = gameObject.AddComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder = 100;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }

        private void Update()
        {
            if (!IsDialogueActive) return;

            bool confirmPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
            try
            {
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    var kb = UnityEngine.InputSystem.Keyboard.current;
                    if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                        confirmPressed = true;
                }
            }
            catch {}

            if (confirmPressed)
            {
                AdvanceDialogue();
            }
        }

        public void Initialize(CharacterState charState)
        {
            cachedCharState = charState;
            RefreshPlayerLayers();
        }

        public void PlayDialogue(DialogueSequence sequence, Action onComplete = null)
        {
            if (sequence == null || sequence.lines == null || sequence.lines.Count == 0)
            {
                sequence?.onComplete?.Invoke();
                onComplete?.Invoke();
                return;
            }

            if (onComplete != null)
            {
                Action prev = sequence.onComplete;
                sequence.onComplete = () =>
                {
                    prev?.Invoke();
                    onComplete.Invoke();
                };
            }

            currentSequence = sequence;
            currentLineIndex = -1;

            RefreshPlayerLayers();

            // Hide background paper doll during dialogue so there is no duplicate ghosting
            if (cachedMainPd == null)
            {
                cachedMainPd = FindFirstObjectByType<PaperDollController>();
            }
            if (cachedMainPd != null) cachedMainPd.gameObject.SetActive(false);

            if (enemyPortraitContainer != null)
            {
                enemyPortraitContainer.gameObject.SetActive(sequence.hasEnemy);
            }

            if (dialogueRoot != null) dialogueRoot.SetActive(true);
            dialogueRoot.transform.SetAsLastSibling();

            StopAllCoroutines();
            StartCoroutine(SlideInDialogueBox());
            AdvanceDialogue();
        }

        public void AdvanceDialogue()
        {
            if (isTyping)
            {
                // Instantly complete typing current line
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                dialogueMessageText.text = fullTextToType;
                isTyping = false;
                if (continueIndicator != null) continueIndicator.SetActive(true);
                return;
            }

            currentLineIndex++;
            if (currentSequence == null || currentLineIndex >= currentSequence.lines.Count)
            {
                EndDialogue();
                return;
            }

            DisplayLine(currentSequence.lines[currentLineIndex]);
        }

        private void DisplayLine(DialogueLine line)
        {
            if (line == null) return;

            // 1. Update Speaker Name & Color
            speakerNameText.text = line.speakerName;
            switch (line.speaker)
            {
                case DialogueSpeaker.Player:
                    speakerNameText.color = new Color(1.0f, 0.85f, 0.35f, 1f); // Warm Gold
                    SetSpeakerActiveState(isPlayer: true);
                    break;

                case DialogueSpeaker.NPC:
                    if (currentSequence != null && currentSequence.enemyType == EnemyType.Succubus)
                        speakerNameText.color = new Color(1.0f, 0.45f, 0.75f, 1f); // Demonic Rose
                    else
                        speakerNameText.color = new Color(0.35f, 0.88f, 1.0f, 1f);  // Cyan Slime
                    SetSpeakerActiveState(isPlayer: false);
                    break;

                case DialogueSpeaker.Narrator:
                default:
                    speakerNameText.color = new Color(0.85f, 0.85f, 0.90f, 1f);
                    SetSpeakerActiveState(isPlayer: false, isNarrator: true);
                    break;
            }

            // 2. Update Player Expression
            UpdatePlayerEmotion(line.playerExpression);

            // 3. Update Enemy Emotion Sprite
            UpdateEnemySprite(line.enemyEmotion);

            // 4. Typewriter reveal text
            fullTextToType = line.text;
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypewriterRoutine(fullTextToType));
        }

        private void SetSpeakerActiveState(bool isPlayer, bool isNarrator = false)
        {
            float playerDim = (isNarrator || !isPlayer) ? 0.48f : 1.0f;
            float enemyDim = (isNarrator || isPlayer) ? 0.48f : 1.0f;

            ApplyPlayerDim(playerDim);
            ApplyEnemyDim(enemyDim);

            if (playerPortraitContainer != null)
            {
                playerPortraitContainer.localScale = (isPlayer && !isNarrator) ? new Vector3(1.02f, 1.02f, 1f) : Vector3.one;
            }

            if (enemyPortraitContainer != null)
            {
                enemyPortraitContainer.localScale = (!isPlayer && !isNarrator) ? new Vector3(1.02f, 1.02f, 1f) : Vector3.one;
            }
        }

        private void ApplyPlayerDim(float dimFactor)
        {
            if (playerCanvasGroup != null) playerCanvasGroup.alpha = 1.0f; // ALWAYS 100% OPAQUE - NO TRANSPARENCY

            foreach (var kvp in playerBaseColors)
            {
                Image img = kvp.Key;
                if (img == null) continue;
                Color baseCol = kvp.Value;
                img.color = new Color(
                    baseCol.r * dimFactor,
                    baseCol.g * dimFactor,
                    baseCol.b * (dimFactor < 0.99f ? dimFactor * 1.06f : dimFactor),
                    baseCol.a // Strictly preserve base alpha (never see-through!)
                );
            }
        }

        private void ApplyEnemyDim(float dimFactor)
        {
            if (enemyCanvasGroup != null) enemyCanvasGroup.alpha = 1.0f; // ALWAYS 100% OPAQUE - NO TRANSPARENCY

            if (enemyPortraitImage != null)
            {
                enemyPortraitImage.color = new Color(
                    dimFactor,
                    dimFactor,
                    dimFactor < 0.99f ? dimFactor * 1.06f : dimFactor,
                    1.0f // Strictly opaque
                );
            }
        }

        private void UpdatePlayerEmotion(string expressionName)
        {
            if (playerExpression == null && playerCloneObj != null)
            {
                foreach (Image img in playerCloneObj.GetComponentsInChildren<Image>(true))
                {
                    if (img.gameObject.name.IndexOf("Expression", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        playerExpression = img;
                        break;
                    }
                }
            }

            if (playerExpression == null) return;
            if (cachedCharState == null) cachedCharState = FindFirstObjectByType<CharacterState>();
            if (cachedCharState == null || cachedCharState.appearanceDatabase == null) return;

            if (string.IsNullOrEmpty(expressionName) || expressionName.Equals("Normal", StringComparison.OrdinalIgnoreCase))
            {
                playerExpression.sprite = cachedCharState.selectedExpression;
            }
            else
            {
                var db = cachedCharState.appearanceDatabase;
                Sprite found = db.emotionStyles.Find(s => s != null && s.name.IndexOf(expressionName, StringComparison.OrdinalIgnoreCase) >= 0);
                if (found != null)
                {
                    playerExpression.sprite = found;
                }
                else
                {
                    playerExpression.sprite = cachedCharState.selectedExpression;
                }
            }
            playerExpression.enabled = (playerExpression.sprite != null);
            if (playerExpression.enabled)
            {
                playerBaseColors[playerExpression] = Color.white;
            }
        }

        private void UpdateEnemySprite(string emotionKey)
        {
            if (enemyPortraitImage == null || currentSequence == null) return;

            if (EnemySpriteDatabase.Instance != null)
            {
                Sprite sp = EnemySpriteDatabase.Instance.GetDialogueSprite(
                    currentSequence.enemyType,
                    currentSequence.enemyCombatPhase,
                    emotionKey
                );

                if (sp != null)
                {
                    enemyPortraitImage.sprite = sp;
                    enemyPortraitImage.enabled = true;
                }
            }
        }

        public void RefreshPlayerLayers()
        {
            PaperDollController pd = FindFirstObjectByType<PaperDollController>(FindObjectsInactive.Include);
            if (pd != null)
            {
                if (playerCloneObj == null && playerPortraitContainer != null)
                {
                    GameObject pClone = Instantiate(pd.gameObject, playerPortraitContainer);
                    pClone.name = "PlayerDialogueBust";
                    pClone.SetActive(true);

                    PaperDollController cPd = pClone.GetComponent<PaperDollController>();
                    if (cPd != null) DestroyImmediate(cPd);

                    // Remove zoom controls or button elements on clone
                    foreach (Transform child in pClone.transform)
                    {
                        if (child.name.IndexOf("Zoom", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            child.name.IndexOf("Btn", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Destroy(child.gameObject);
                        }
                    }

                    RectTransform pcr = pClone.GetComponent<RectTransform>();
                    pcr.anchorMin = new Vector2(0f, 1f);
                    pcr.anchorMax = new Vector2(0f, 1f);
                    pcr.pivot = new Vector2(0.5f, 1f);
                    pcr.sizeDelta = new Vector2(720f, 1080f);
                    pcr.localScale = new Vector3(1.60f, 1.60f, 1f);
                    pcr.anchoredPosition = new Vector2(450f, -35f); // Lowered so hair crown is fully visible

                    playerCloneObj = pClone;
                }

                if (playerCloneObj != null)
                {
                    RectTransform pcr = playerCloneObj.GetComponent<RectTransform>();
                    if (pcr != null)
                    {
                        pcr.anchoredPosition = new Vector2(450f, -35f);
                    }

                    SyncImage(playerCloneObj, "HairBack", pd.hairBackImage);
                    SyncImage(playerCloneObj, "Body", pd.bodyImage);
                    SyncImage(playerCloneObj, "Groin", pd.groinImage);
                    SyncImage(playerCloneObj, "Breast", pd.breastImage);
                    SyncImage(playerCloneObj, "Underwear", pd.underwearImage);
                    SyncImage(playerCloneObj, "LowerClothes", pd.lowerClothesImage);
                    SyncImage(playerCloneObj, "Clothes", pd.clothesImage);
                    SyncImage(playerCloneObj, "Eyebrows", pd.eyebrowsImage);
                    SyncImage(playerCloneObj, "HairFront", pd.hairFrontImage);
                    SyncImage(playerCloneObj, "Accessory", pd.accessoryImage);
                    SyncImage(playerCloneObj, "Blush", pd.blushImage);
                    SyncImage(playerCloneObj, "Expression", pd.expressionImage);

                    playerBaseColors.Clear();
                    foreach (Image img in playerCloneObj.GetComponentsInChildren<Image>(true))
                    {
                        if (img != null)
                        {
                            playerBaseColors[img] = img.color;
                            if (img.gameObject.name.IndexOf("Expression", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                playerExpression = img;
                            }
                        }
                    }
                }
            }
        }

        private static void SyncImage(GameObject root, string nameSubstr, Image source)
        {
            if (root == null) return;
            Transform directChild = root.transform.Find(nameSubstr);
            if (directChild != null && directChild.TryGetComponent<Image>(out var directImg))
            {
                ApplySync(directImg, source);
                return;
            }

            Image[] images = root.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                if (string.Equals(img.gameObject.name, nameSubstr, StringComparison.OrdinalIgnoreCase))
                {
                    ApplySync(img, source);
                    return;
                }
            }
            foreach (Image img in images)
            {
                if (img.gameObject.name.IndexOf(nameSubstr, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ApplySync(img, source);
                    return;
                }
            }
        }

        private static void ApplySync(Image img, Image source)
        {
            if (source != null && source.sprite != null)
            {
                img.sprite = source.sprite;
                img.color = source.color;
                img.material = source.material;
                img.preserveAspect = true;
                img.enabled = source.enabled;
                img.gameObject.SetActive(source.gameObject.activeSelf && source.enabled);
            }
            else
            {
                img.sprite = null;
                img.enabled = false;
                img.gameObject.SetActive(false);
            }
        }

        private IEnumerator TypewriterRoutine(string message)
        {
            isTyping = true;
            if (continueIndicator != null) continueIndicator.SetActive(false);
            dialogueMessageText.text = "";

            float charDelay = 0.022f;
            for (int i = 0; i < message.Length; i++)
            {
                dialogueMessageText.text += message[i];
                yield return new WaitForSeconds(charDelay);
            }

            isTyping = false;
            if (continueIndicator != null) continueIndicator.SetActive(true);
        }

        private IEnumerator SlideInDialogueBox()
        {
            if (dialogueBoxRect == null) yield break;

            Vector2 targetPos = new Vector2(0f, 140f);
            Vector2 startPos = new Vector2(0f, -220f);
            dialogueBoxRect.anchoredPosition = startPos;

            float elapsed = 0f;
            float duration = 0.35f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                dialogueBoxRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                yield return null;
            }
            dialogueBoxRect.anchoredPosition = targetPos;
        }

        public void EndDialogue()
        {
            StopAllCoroutines();
            if (dialogueRoot != null) dialogueRoot.SetActive(false);

            // Restore background paper doll
            if (cachedMainPd != null)
            {
                cachedMainPd.gameObject.SetActive(true);
            }
            else
            {
                PaperDollController mainPd = FindFirstObjectByType<PaperDollController>(FindObjectsInactive.Include);
                if (mainPd != null) mainPd.gameObject.SetActive(true);
            }

            Action callback = currentSequence?.onComplete;
            currentSequence = null;
            currentLineIndex = -1;

            callback?.Invoke();
            OnDialogueCompleted?.Invoke();
        }

        // =========================================================================
        // DYNAMIC CONSTRUCTOR
        // =========================================================================

        public static DialogueUI CreateDynamic(Transform canvasParent, CharacterState charState)
        {
            GameObject rootObj = new GameObject("DialogueCanvasOverlay", typeof(RectTransform));
            rootObj.transform.SetParent(canvasParent, false);

            RectTransform rootRt = rootObj.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;
            rootRt.anchoredPosition = Vector2.zero;

            DialogueUI ui = rootObj.AddComponent<DialogueUI>();
            ui.dialogueRoot = rootObj;

            Canvas dCanvas = rootObj.GetComponent<Canvas>();
            if (dCanvas == null) dCanvas = rootObj.AddComponent<Canvas>();
            dCanvas.overrideSorting = true;
            dCanvas.sortingOrder = 100;
            if (rootObj.GetComponent<GraphicRaycaster>() == null)
            {
                rootObj.AddComponent<GraphicRaycaster>();
            }

            // 1. Semi-transparent backdrop dimmer (dims the game behind)
            GameObject dimObj = new GameObject("BackdropDimmer", typeof(RectTransform));
            dimObj.transform.SetParent(rootObj.transform, false);
            Image dimImg = dimObj.AddComponent<Image>();
            dimImg.color = new Color(0.02f, 0.02f, 0.05f, 0.80f);
            RectTransform dimRt = dimObj.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.sizeDelta = Vector2.zero;
            ui.backdropDimmer = dimImg;

            // 2. Left Portrait Viewport (Player - Rib-height Bust View, expanded box so arms/elbows/hair are unclipped)
            GameObject pViewObj = new GameObject("PlayerBustViewport", typeof(RectTransform));
            pViewObj.transform.SetParent(rootObj.transform, false);
            RectTransform pvr = pViewObj.GetComponent<RectTransform>();
            pvr.anchorMin = new Vector2(0f, 0f);
            pvr.anchorMax = new Vector2(0f, 0f);
            pvr.pivot = new Vector2(0f, 0f);
            pvr.anchoredPosition = new Vector2(-150f, 110f);
            pvr.sizeDelta = new Vector2(980f, 760f); // Expanded with generous headroom for hair crown
            pViewObj.AddComponent<RectMask2D>();
            ui.playerPortraitContainer = pvr;
            ui.playerCanvasGroup = pViewObj.AddComponent<CanvasGroup>();
            ui.playerCanvasGroup.alpha = 1.0f;

            // 3. Right Portrait Viewport (Enemy/NPC - Rib-height Bust View with chest/breasts, expanded box for wings/arms)
            GameObject eViewObj = new GameObject("EnemyBustViewport", typeof(RectTransform));
            eViewObj.transform.SetParent(rootObj.transform, false);
            RectTransform evr = eViewObj.GetComponent<RectTransform>();
            evr.anchorMin = new Vector2(1f, 0f);
            evr.anchorMax = new Vector2(1f, 0f);
            evr.pivot = new Vector2(1f, 0f);
            evr.anchoredPosition = new Vector2(250f, 130f);
            evr.sizeDelta = new Vector2(1250f, 720f); // Expanded invisible box so wings/arms are unclipped
            eViewObj.AddComponent<RectMask2D>();
            ui.enemyPortraitContainer = evr;
            ui.enemyCanvasGroup = eViewObj.AddComponent<CanvasGroup>();
            ui.enemyCanvasGroup.alpha = 1.0f;

            // Inner enemy sprite (anchored so head & chest/breasts are framed, cutting below ribs)
            GameObject eSpriteObj = new GameObject("EnemySprite", typeof(RectTransform));
            eSpriteObj.transform.SetParent(eViewObj.transform, false);
            Image eImg = eSpriteObj.AddComponent<Image>();
            eImg.preserveAspect = true;
            eImg.raycastTarget = false;
            RectTransform esr = eSpriteObj.GetComponent<RectTransform>();
            esr.anchorMin = new Vector2(1f, 1f);
            esr.anchorMax = new Vector2(1f, 1f);
            esr.pivot = new Vector2(0.5f, 1f);
            esr.anchoredPosition = new Vector2(-570f, 25f);
            esr.sizeDelta = new Vector2(1020f, 1480f); // Restored to large full size
            ui.enemyPortraitImage = eImg;
            ui.enemyPortraitTransform = esr;

            // 4. Bottom Dialogue Box
            GameObject boxObj = new GameObject("DialogueBox", typeof(RectTransform));
            boxObj.transform.SetParent(rootObj.transform, false);
            RectTransform br = boxObj.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0.5f, 0f);
            br.anchorMax = new Vector2(0.5f, 0f);
            br.pivot = new Vector2(0.5f, 0f);
            br.anchoredPosition = new Vector2(0f, 140f);
            br.sizeDelta = new Vector2(1180f, 230f);
            ui.dialogueBoxRect = br;

            Image boxBg = boxObj.AddComponent<Image>();
            boxBg.color = new Color(0.09f, 0.11f, 0.17f, 0.96f);
            ui.dialogueBoxBg = boxBg;

            // Click Area covering whole screen to advance
            Button clickBtn = rootObj.AddComponent<Button>();
            clickBtn.transition = Selectable.Transition.None;
            clickBtn.onClick.AddListener(ui.AdvanceDialogue);
            ui.dialogueClickArea = clickBtn;

            // Speaker Name Banner Plate
            GameObject namePlate = new GameObject("SpeakerPlate", typeof(RectTransform));
            namePlate.transform.SetParent(boxObj.transform, false);
            Image npBg = namePlate.AddComponent<Image>();
            npBg.color = new Color(0.18f, 0.22f, 0.32f, 0.98f);
            npBg.raycastTarget = false;
            RectTransform npr = namePlate.GetComponent<RectTransform>();
            npr.anchorMin = new Vector2(0f, 1f);
            npr.anchorMax = new Vector2(0f, 1f);
            npr.pivot = new Vector2(0f, 0f);
            npr.anchoredPosition = new Vector2(28f, -6f);
            npr.sizeDelta = new Vector2(280f, 44f);

            GameObject nameTxtObj = new GameObject("NameText", typeof(RectTransform));
            nameTxtObj.transform.SetParent(namePlate.transform, false);
            TextMeshProUGUI nameTxt = nameTxtObj.AddComponent<TextMeshProUGUI>();
            nameTxt.text = "Speaker";
            nameTxt.fontSize = 22;
            nameTxt.fontStyle = FontStyles.Bold;
            nameTxt.alignment = TextAlignmentOptions.Center;
            nameTxt.color = Color.white;
            nameTxt.raycastTarget = false;
            RectTransform ntr = nameTxtObj.GetComponent<RectTransform>();
            ntr.anchorMin = Vector2.zero;
            ntr.anchorMax = Vector2.one;
            ntr.sizeDelta = Vector2.zero;
            ui.speakerNameText = nameTxt;

            // Dialogue Message Text
            GameObject msgObj = new GameObject("MessageText", typeof(RectTransform));
            msgObj.transform.SetParent(boxObj.transform, false);
            TextMeshProUGUI msgTxt = msgObj.AddComponent<TextMeshProUGUI>();
            msgTxt.text = "...";
            msgTxt.fontSize = 21;
            msgTxt.lineSpacing = 14;
            msgTxt.alignment = TextAlignmentOptions.TopLeft;
            msgTxt.color = new Color(0.92f, 0.94f, 0.98f, 1f);
            msgTxt.raycastTarget = false;
            RectTransform mr = msgObj.GetComponent<RectTransform>();
            mr.anchorMin = new Vector2(0f, 0f);
            mr.anchorMax = new Vector2(1f, 1f);
            mr.pivot = new Vector2(0.5f, 0.5f);
            mr.anchoredPosition = new Vector2(0f, -14f);
            mr.sizeDelta = new Vector2(-80f, -80f);
            ui.dialogueMessageText = msgTxt;

            // Blinking continue arrow prompt
            GameObject contObj = new GameObject("ContinueIndicator", typeof(RectTransform));
            contObj.transform.SetParent(boxObj.transform, false);
            TextMeshProUGUI contTxt = contObj.AddComponent<TextMeshProUGUI>();
            contTxt.text = "▼  [Click to Continue]";
            contTxt.fontSize = 16;
            contTxt.fontStyle = FontStyles.Bold;
            contTxt.alignment = TextAlignmentOptions.Right;
            contTxt.color = new Color(1f, 0.85f, 0.35f, 0.9f);
            contTxt.raycastTarget = false;
            RectTransform cr = contObj.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(1f, 0f);
            cr.anchorMax = new Vector2(1f, 0f);
            cr.pivot = new Vector2(1f, 0f);
            cr.anchoredPosition = new Vector2(-28f, 14f);
            cr.sizeDelta = new Vector2(250f, 30f);
            ui.continueIndicator = contObj;

            ui.Initialize(charState);
            if (Instance == null) Instance = ui;
            rootObj.SetActive(false);
            return ui;
        }

        private static Image CreateLayerImage(Transform parent, string layerName)
        {
            GameObject obj = new GameObject(layerName, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            Image img = obj.AddComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            return img;
        }
    }
}
