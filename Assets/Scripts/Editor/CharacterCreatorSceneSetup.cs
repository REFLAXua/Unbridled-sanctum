#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public static class CharacterCreatorSceneSetup
{
    [MenuItem("Tools/Game/Setup Character Creator & Start Menu")]
    public static void Setup()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No Canvas found in the current scene!", "OK");
            return;
        }

        GameUIManager gameUI = Object.FindFirstObjectByType<GameUIManager>();
        CharacterState charState = Object.FindFirstObjectByType<CharacterState>();
        PaperDollController paperDoll = Object.FindFirstObjectByType<PaperDollController>();

        // Load Database
        CharacterAppearanceDatabase database = AssetDatabase.LoadAssetAtPath<CharacterAppearanceDatabase>(
            "Assets/Settings/CharacterAppearanceDatabase.asset");

        if (charState != null && database != null)
        {
            charState.appearanceDatabase = database;
            EditorUtility.SetDirty(charState);
        }

        // =========================================================================
        // 1. CLEANUP PREVIOUS PANELS TO PREVENT OVERLAPPING / DUPLICATE TEXTS
        // =========================================================================
        Transform oldStart = canvas.transform.Find("StartMenuPanel");
        if (oldStart != null)
        {
            Undo.DestroyObjectImmediate(oldStart.gameObject);
        }

        Transform oldCreator = canvas.transform.Find("CharacterCreatorPanel");
        if (oldCreator != null)
        {
            Undo.DestroyObjectImmediate(oldCreator.gameObject);
        }

        // =========================================================================
        // 2. CONFIGURE PAPER DOLL LAYERING & HEAD CONTAINER (PRESERVING COORDINATES)
        // =========================================================================
        if (paperDoll != null)
        {
            RectTransform pdRect = paperDoll.GetComponent<RectTransform>();
            if (pdRect != null)
            {
                pdRect.anchoredPosition = new Vector2(380f, 0f);
                pdRect.sizeDelta = new Vector2(720f, -40f);
            }

            // Detect and preserve user's adjusted position/size
            Transform oldHead = paperDoll.transform.Find("HeadContainer");
            Vector2 preservedHeadPos = new Vector2(0f, 385f);
            Vector2 preservedHeadSize = new Vector2(520f, 520f);

            if (oldHead != null)
            {
                RectTransform ohr = oldHead.GetComponent<RectTransform>();
                if (ohr != null)
                {
                    preservedHeadPos = ohr.anchoredPosition;
                    preservedHeadSize = ohr.sizeDelta;
                }
                // Unparent existing layers before destroying container
                Transform[] toSave = { oldHead.Find("HairBack"), oldHead.Find("Expression"), oldHead.Find("Blush"), oldHead.Find("Eyebrows"), oldHead.Find("HairFront") };
                foreach (var t in toSave)
                {
                    if (t != null) t.SetParent(paperDoll.transform, false);
                }
                Undo.DestroyObjectImmediate(oldHead.gameObject);
            }
            else
            {
                // If user adjusted HairFront or Expression directly before HeadContainer existed
                Transform hf = paperDoll.transform.Find("HairFront");
                if (hf != null)
                {
                    RectTransform hfr = hf.GetComponent<RectTransform>();
                    if (hfr != null && (hfr.anchoredPosition != Vector2.zero || (hfr.sizeDelta.x > 0 && hfr.sizeDelta.y > 0)))
                    {
                        preservedHeadPos = hfr.anchoredPosition;
                        if (hfr.sizeDelta.x > 50 && hfr.sizeDelta.y > 50)
                            preservedHeadSize = hfr.sizeDelta;
                    }
                }
            }

            // Get or create Blush
            Transform blushTrans = paperDoll.transform.Find("Blush");
            Image blushImg = null;
            if (blushTrans == null)
            {
                GameObject blushObj = new GameObject("Blush", typeof(RectTransform));
                blushObj.transform.SetParent(paperDoll.transform, false);
                blushImg = blushObj.AddComponent<Image>();
                blushImg.enabled = true;
                blushImg.preserveAspect = true;
                blushImg.raycastTarget = false;
                blushTrans = blushObj.transform;
            }
            else
            {
                blushImg = blushTrans.GetComponent<Image>();
                blushImg.preserveAspect = true;
            }

            // Ensure HairBack is direct child of PaperDollPanel and at SIBLING 0 (BEHIND BODY)
            Transform hairBackTrans = paperDoll.transform.Find("HairBack");
            if (hairBackTrans != null)
            {
                hairBackTrans.SetParent(paperDoll.transform, false);
                hairBackTrans.SetSiblingIndex(0); // Very bottom layer, behind body!

                RectTransform hbRect = hairBackTrans.GetComponent<RectTransform>();
                hbRect.anchorMin = new Vector2(0.5f, 0.5f);
                hbRect.anchorMax = new Vector2(0.5f, 0.5f);
                hbRect.pivot = new Vector2(0.5f, 0.5f);
                hbRect.anchoredPosition = preservedHeadPos;
                hbRect.sizeDelta = preservedHeadSize;

                Image hbImg = hairBackTrans.GetComponent<Image>();
                hbImg.preserveAspect = true;
                paperDoll.hairBackImage = hbImg;
            }

            // Body layer (Sibling 1)
            Transform bodyTrans = paperDoll.transform.Find("Body");
            if (bodyTrans != null)
            {
                bodyTrans.SetSiblingIndex(1);
            }

            // Underwear layer (Sibling 2)
            Transform underwearTrans = paperDoll.transform.Find("Underwear");
            if (underwearTrans != null)
            {
                underwearTrans.SetSiblingIndex(2);
            }

            // Clothes layer (Sibling 3)
            Transform clothesTrans = paperDoll.transform.Find("Clothes");
            if (clothesTrans != null)
            {
                clothesTrans.SetSiblingIndex(3);
            }

            // Create HeadContainer (Sibling 4, IN FRONT OF BODY/CLOTHES)
            GameObject headObj = new GameObject("HeadContainer", typeof(RectTransform));
            headObj.transform.SetParent(paperDoll.transform, false);
            headObj.transform.SetSiblingIndex(4);

            RectTransform headRect = headObj.GetComponent<RectTransform>();
            headRect.anchorMin = new Vector2(0.5f, 0.5f);
            headRect.anchorMax = new Vector2(0.5f, 0.5f);
            headRect.pivot = new Vector2(0.5f, 0.5f);
            headRect.anchoredPosition = preservedHeadPos;
            headRect.sizeDelta = preservedHeadSize;

            // Put Expression, Blush, Eyebrows, HairFront inside HeadContainer
            Transform exprTrans = paperDoll.transform.Find("Expression");
            Transform eyeTrans = paperDoll.transform.Find("Eyebrows");
            Transform hairFrontTrans = paperDoll.transform.Find("HairFront");

            if (exprTrans != null)
            {
                exprTrans.SetParent(headObj.transform, false);
                SetLayerRect(exprTrans);
                paperDoll.expressionImage = exprTrans.GetComponent<Image>();
            }

            if (blushTrans != null)
            {
                blushTrans.SetParent(headObj.transform, false);
                SetLayerRect(blushTrans);
                paperDoll.blushImage = blushImg;
            }

            if (eyeTrans != null)
            {
                eyeTrans.SetParent(headObj.transform, false);
                SetLayerRect(eyeTrans);
                paperDoll.eyebrowsImage = eyeTrans.GetComponent<Image>();
            }

            if (hairFrontTrans != null)
            {
                hairFrontTrans.SetParent(headObj.transform, false);
                SetLayerRect(hairFrontTrans);
                paperDoll.hairFrontImage = hairFrontTrans.GetComponent<Image>();
            }

            // Accessory (Sibling 5)
            Transform accTrans = paperDoll.transform.Find("Accessory");
            if (accTrans != null)
            {
                accTrans.SetSiblingIndex(5);
            }

            paperDoll.headContainer = headRect;

            // Assign Preview Sprites in Edit Mode
            Sprite defaultFront = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/Hair/Hair front/Short.png");
            Sprite defaultBack = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/Hair/Hair behind/Short Hair.png");
            Sprite defaultExpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/Expression/Emotions/Normal.png");
            Sprite defaultBlush = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/Blush/1.png");

            if (paperDoll.hairFrontImage != null && defaultFront != null)
            {
                paperDoll.hairFrontImage.sprite = defaultFront;
                paperDoll.hairFrontImage.enabled = true;
            }
            if (paperDoll.hairBackImage != null && defaultBack != null)
            {
                paperDoll.hairBackImage.sprite = defaultBack;
                paperDoll.hairBackImage.enabled = true;
            }
            if (paperDoll.expressionImage != null && defaultExpr != null)
            {
                paperDoll.expressionImage.sprite = defaultExpr;
                paperDoll.expressionImage.enabled = true;
            }
            if (paperDoll.blushImage != null && defaultBlush != null)
            {
                paperDoll.blushImage.sprite = defaultBlush;
                paperDoll.blushImage.enabled = true;
            }

            Shader eyeShader = Shader.Find("Custom/UI/EyeColorReplacement");
            if (eyeShader != null)
            {
                paperDoll.eyeColorShader = eyeShader;
            }

            EditorUtility.SetDirty(paperDoll);
        }

        // =========================================================================
        // 3. BUILD START MENU PANEL (FRESH)
        // =========================================================================
        GameObject startPanelObj = new GameObject("StartMenuPanel", typeof(RectTransform));
        startPanelObj.transform.SetParent(canvas.transform, false);

        RectTransform startRect = startPanelObj.GetComponent<RectTransform>();
        startRect.anchorMin = Vector2.zero;
        startRect.anchorMax = Vector2.one;
        startRect.sizeDelta = Vector2.zero;
        startRect.anchoredPosition = Vector2.zero;

        Image bg = startPanelObj.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.09f, 0.12f, 0.96f);

        // Title: UNBRIDLED SANCTUM
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform));
        titleObj.transform.SetParent(startPanelObj.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "UNBRIDLED SANCTUM";
        titleText.fontSize = 52;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.95f, 0.85f, 0.60f, 1f);

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 80f);
        titleRect.sizeDelta = new Vector2(700f, 80f);

        // PLAY Button
        GameObject playBtnObj = new GameObject("PlayButton", typeof(RectTransform));
        playBtnObj.transform.SetParent(startPanelObj.transform, false);
        Image playBg = playBtnObj.AddComponent<Image>();
        playBg.color = new Color(0.22f, 0.25f, 0.35f, 1f);

        Button playBtn = playBtnObj.AddComponent<Button>();
        playBtn.targetGraphic = playBg;

        RectTransform playRect = playBtnObj.GetComponent<RectTransform>();
        playRect.anchorMin = new Vector2(0.5f, 0.5f);
        playRect.anchorMax = new Vector2(0.5f, 0.5f);
        playRect.anchoredPosition = new Vector2(0f, -40f);
        playRect.sizeDelta = new Vector2(240f, 60f);

        GameObject playTextObj = new GameObject("Text", typeof(RectTransform));
        playTextObj.transform.SetParent(playBtnObj.transform, false);
        TextMeshProUGUI playText = playTextObj.AddComponent<TextMeshProUGUI>();
        playText.text = "PLAY";
        playText.fontSize = 28;
        playText.fontStyle = FontStyles.Bold;
        playText.alignment = TextAlignmentOptions.Center;
        playText.color = Color.white;
        playText.raycastTarget = false;

        RectTransform ptRect = playTextObj.GetComponent<RectTransform>();
        ptRect.anchorMin = Vector2.zero;
        ptRect.anchorMax = Vector2.one;
        ptRect.sizeDelta = Vector2.zero;

        StartMenuUI startMenuUI = startPanelObj.AddComponent<StartMenuUI>();
        startMenuUI.startMenuPanel = startPanelObj;
        startMenuUI.playButton = playBtn;

        // =========================================================================
        // 4. BUILD CHARACTER CREATOR PANEL (FRESH)
        // =========================================================================
        GameObject creatorObj = new GameObject("CharacterCreatorPanel", typeof(RectTransform));
        creatorObj.transform.SetParent(canvas.transform, false);

        RectTransform crRect = creatorObj.GetComponent<RectTransform>();
        crRect.anchorMin = new Vector2(1f, 0f);
        crRect.anchorMax = new Vector2(1f, 1f);
        crRect.pivot = new Vector2(1f, 0.5f);
        crRect.anchoredPosition = new Vector2(-40f, 0f);
        crRect.sizeDelta = new Vector2(580f, -80f);

        Image crBg = creatorObj.AddComponent<Image>();
        crBg.color = new Color(0.12f, 0.13f, 0.17f, 0.94f);

        CharacterCreatorUI creatorUI = creatorObj.AddComponent<CharacterCreatorUI>();
        creatorUI.creatorPanel = creatorObj;
        creatorUI.characterState = charState;
        creatorUI.database = database;
        creatorUI.paperDoll = paperDoll;

        // Title
        GameObject cTitleObj = new GameObject("CreatorTitle", typeof(RectTransform));
        cTitleObj.transform.SetParent(creatorObj.transform, false);
        TextMeshProUGUI cTitle = cTitleObj.AddComponent<TextMeshProUGUI>();
        cTitle.text = "CHARACTER CREATION";
        cTitle.fontSize = 24;
        cTitle.fontStyle = FontStyles.Bold;
        cTitle.alignment = TextAlignmentOptions.Center;
        cTitle.color = new Color(0.95f, 0.95f, 0.95f, 1f);

        RectTransform cTitleRect = cTitleObj.GetComponent<RectTransform>();
        cTitleRect.anchorMin = new Vector2(0f, 1f);
        cTitleRect.anchorMax = new Vector2(1f, 1f);
        cTitleRect.pivot = new Vector2(0.5f, 1f);
        cTitleRect.anchoredPosition = new Vector2(0f, -15f);
        cTitleRect.sizeDelta = new Vector2(0f, 40f);

        // Container for controls
        GameObject content = new GameObject("ControlsContainer", typeof(RectTransform));
        content.transform.SetParent(creatorObj.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.anchoredPosition = new Vector2(0f, -25f);
        contentRect.sizeDelta = new Vector2(-40f, -120f);

        VerticalLayoutGroup vert = content.AddComponent<VerticalLayoutGroup>();
        vert.spacing = 14;
        vert.childControlWidth = true;
        vert.childControlHeight = false;
        vert.childForceExpandWidth = true;
        vert.childForceExpandHeight = false;
        vert.padding = new RectOffset(15, 15, 10, 10);

        CreateSelectorRow(content.transform, "Body Size", out creatorUI.bodySizeLabel, out creatorUI.bodySizePrevBtn, out creatorUI.bodySizeNextBtn);
        CreateSelectorRow(content.transform, "Hair Front", out creatorUI.hairFrontLabel, out creatorUI.hairFrontPrevBtn, out creatorUI.hairFrontNextBtn);
        CreateSelectorRow(content.transform, "Hair Back", out creatorUI.hairBackLabel, out creatorUI.hairBackPrevBtn, out creatorUI.hairBackNextBtn);
        CreatePaletteRow(content.transform, "Hair Color", out creatorUI.hairColorPaletteContainer);
        CreateSelectorRow(content.transform, "Emotion", out creatorUI.emotionLabel, out creatorUI.emotionPrevBtn, out creatorUI.emotionNextBtn);
        CreatePaletteRow(content.transform, "Eye Color", out creatorUI.eyeColorPaletteContainer);
        // Blush option removed: handled dynamically in combat seduction phases

        // Bottom Buttons: Random + Accept
        GameObject btnRow = new GameObject("ActionButtonsRow", typeof(RectTransform));
        btnRow.transform.SetParent(creatorObj.transform, false);
        RectTransform btnRowRect = btnRow.GetComponent<RectTransform>();
        btnRowRect.anchorMin = new Vector2(0f, 0f);
        btnRowRect.anchorMax = new Vector2(1f, 0f);
        btnRowRect.pivot = new Vector2(0.5f, 0f);
        btnRowRect.anchoredPosition = new Vector2(0f, 20f);
        btnRowRect.sizeDelta = new Vector2(-40f, 50f);

        HorizontalLayoutGroup btnHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        btnHlg.spacing = 20;
        btnHlg.childAlignment = TextAnchor.MiddleCenter;
        btnHlg.childControlWidth = false;
        btnHlg.childControlHeight = false;

        creatorUI.randomButton = CreateActionButton(btnRow.transform, "Random", new Color(0.35f, 0.35f, 0.45f, 1f), new Vector2(160f, 46f));
        creatorUI.acceptButton = CreateActionButton(btnRow.transform, "Accept", new Color(0.20f, 0.55f, 0.35f, 1f), new Vector2(220f, 46f));

        creatorObj.SetActive(false);

        // =========================================================================
        // 5. WIRE INTO GAME UI MANAGER
        // =========================================================================
        if (gameUI != null)
        {
            gameUI.startMenuUI = startMenuUI;
            gameUI.characterCreatorUI = creatorUI;
            gameUI.currentState = GameUIManager.GameFlowState.StartMenu;
            EditorUtility.SetDirty(gameUI);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("Character Creator & Start Menu successfully rebuilt cleanly in scene!");
    }

    private static void SetLayerRect(Transform t)
    {
        RectTransform rt = t.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        Image img = t.GetComponent<Image>();
        if (img != null) img.preserveAspect = true;
    }

    private static void CreateSelectorRow(Transform parent, string header, out TextMeshProUGUI valText, out Button prevBtn, out Button nextBtn)
    {
        GameObject row = new GameObject(header + "_Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 44f);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleCenter;

        GameObject lblObj = new GameObject("Header", typeof(RectTransform));
        lblObj.transform.SetParent(row.transform, false);
        TextMeshProUGUI lbl = lblObj.AddComponent<TextMeshProUGUI>();
        lbl.text = header + ":";
        lbl.fontSize = 16;
        lbl.fontStyle = FontStyles.Bold;
        lbl.alignment = TextAlignmentOptions.Left;
        lbl.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        lblObj.GetComponent<RectTransform>().sizeDelta = new Vector2(110f, 0f);

        prevBtn = CreateMiniButton(row.transform, "<");

        GameObject valObj = new GameObject("Value", typeof(RectTransform));
        valObj.transform.SetParent(row.transform, false);
        valText = valObj.AddComponent<TextMeshProUGUI>();
        valText.text = "...";
        valText.fontSize = 16;
        valText.alignment = TextAlignmentOptions.Center;
        valText.color = Color.white;
        valObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 0f);

        nextBtn = CreateMiniButton(row.transform, ">");
    }

    private static void CreatePaletteRow(Transform parent, string header, out Transform container)
    {
        GameObject row = new GameObject(header + "_PaletteRow", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 42f);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleCenter;

        GameObject lblObj = new GameObject("Header", typeof(RectTransform));
        lblObj.transform.SetParent(row.transform, false);
        TextMeshProUGUI lbl = lblObj.AddComponent<TextMeshProUGUI>();
        lbl.text = header + ":";
        lbl.fontSize = 16;
        lbl.fontStyle = FontStyles.Bold;
        lbl.alignment = TextAlignmentOptions.Left;
        lbl.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        lblObj.GetComponent<RectTransform>().sizeDelta = new Vector2(110f, 0f);

        GameObject pal = new GameObject("PaletteContainer", typeof(RectTransform));
        pal.transform.SetParent(row.transform, false);
        HorizontalLayoutGroup palHlg = pal.AddComponent<HorizontalLayoutGroup>();
        palHlg.spacing = 6;
        palHlg.childControlWidth = false;
        palHlg.childControlHeight = false;
        palHlg.childAlignment = TextAnchor.MiddleLeft;
        pal.GetComponent<RectTransform>().sizeDelta = new Vector2(320f, 36f);

        container = pal.transform;
    }

    private static Button CreateMiniButton(Transform parent, string label)
    {
        GameObject btnObj = new GameObject("ArrowBtn_" + label, typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);
        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(0.25f, 0.28f, 0.35f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(36f, 36f);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 18;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        txt.raycastTarget = false;

        RectTransform tr = txtObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.sizeDelta = Vector2.zero;

        return btn;
    }

    private static Button CreateActionButton(Transform parent, string label, Color bgColor, Vector2 size)
    {
        GameObject btnObj = new GameObject(label + "Button", typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);
        Image bg = btnObj.AddComponent<Image>();
        bg.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        btnObj.GetComponent<RectTransform>().sizeDelta = size;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 20;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        txt.raycastTarget = false;

        RectTransform tr = txtObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.sizeDelta = Vector2.zero;

        return btn;
    }
}
#endif
