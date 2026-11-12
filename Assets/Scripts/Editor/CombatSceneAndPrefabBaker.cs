#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnbridledSanctum.Combat;
using UnbridledSanctum.UI;

namespace UnbridledSanctum.EditorScripts
{
    public static class CombatSceneAndPrefabBaker
    {
        [MenuItem("Unbridled Sanctum/UI/Bake Combat UI Prefab & Scene", false, 10)]
        public static void Bake()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera & EventSystem
            GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            Camera cam = camObj.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camObj.transform.position = new Vector3(0f, 0f, -10f);

            GameObject esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // 2. Canvas Root (1920x1080)
            GameObject canvasObj = new GameObject("CombatPreviewCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // 3. Generate baseline UI
            CombatUI combatUI = CombatUI.CreateDynamic(canvasObj.transform, null);
            GameObject panelObj = combatUI.combatPanel;
            panelObj.SetActive(true);

            // Save clean runtime prefab BEFORE adding preview mockups
            string prefabPath = "Assets/Prefabs/UI/CombatPanel.prefab";
            PrefabUtility.SaveAsPrefabAsset(panelObj, prefabPath);

            // =========================================================================
            // POPULATE WYSIWYG PREVIEW MOCKUPS INSIDE CONTAINERS (For Scene Editing Only)
            // =========================================================================

            // --- A. Player Heroine Mockup inside PlayerAvatarContainer ---
            if (combatUI.playerAvatarContainer != null)
            {
                // Clear existing children
                for (int i = combatUI.playerAvatarContainer.childCount - 1; i >= 0; i--)
                {
                    Transform c = combatUI.playerAvatarContainer.GetChild(i);
                    if (c.name.Contains("TargetHighlight")) continue; // keep highlight aura
                    Object.DestroyImmediate(c.gameObject);
                }

                GameObject dollObj = new GameObject("PreviewHeroineDoll", typeof(RectTransform));
                dollObj.transform.SetParent(combatUI.playerAvatarContainer, false);
                RectTransform drt = dollObj.GetComponent<RectTransform>();
                drt.anchorMin = new Vector2(0.5f, 0.5f);
                drt.anchorMax = new Vector2(0.5f, 0.5f);
                drt.pivot = new Vector2(0.5f, 0.5f);
                drt.anchoredPosition = new Vector2(0f, -10f);
                drt.sizeDelta = new Vector2(512f, 768f);

                // Body layer (contains canonical hair)
                CreateLayerSprite(dollObj.transform, "Body", "Assets/sprites/doll/hero_small.png");
                // Breast layer
                CreateLayerSprite(dollObj.transform, "Breast", "Assets/sprites/doll/Breast_medium.png");
                // Expression
                CreateLayerSprite(dollObj.transform, "Expression", "Assets/sprites/Expression/Emotions/Normal.png");
            }

            // --- B. Max Enemies Mockup inside EnemiesContainer (3 Enemies in V-Formation) ---
            if (combatUI.enemiesContainer != null)
            {
                // Clear any leftover children
                for (int i = combatUI.enemiesContainer.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(combatUI.enemiesContainer.GetChild(i).gameObject);
                }

                Sprite scoutSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/Characters/Scout/scout_base.png");
                Sprite frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/UI/ui_progressbar_frame.png");

                string[] enemyNames = new string[] { "Elven Scout A", "Elven Scout B", "Elven Scout C" };
                Vector2[] enemyPositions = new Vector2[]
                {
                    CombatUI.GetEnemySnakePosition(0, 3), // Front-Left
                    CombatUI.GetEnemySnakePosition(1, 3), // Center-Back
                    CombatUI.GetEnemySnakePosition(2, 3)  // Front-Right
                };

                for (int e = 0; e < 3; e++)
                {
                    GameObject mockEnemy = new GameObject($"EnemyMock_{e}_{enemyNames[e]}", typeof(RectTransform));
                    mockEnemy.transform.SetParent(combatUI.enemiesContainer, false);
                    RectTransform ert = mockEnemy.GetComponent<RectTransform>();
                    ert.sizeDelta = new Vector2(480f, 820f);
                    ert.anchoredPosition = enemyPositions[e];

                    // Full body sprite
                    GameObject spriteObj = new GameObject("FullBodySprite", typeof(RectTransform));
                    spriteObj.transform.SetParent(mockEnemy.transform, false);
                    RectTransform srt = spriteObj.GetComponent<RectTransform>();
                    srt.anchorMin = new Vector2(0.5f, 0.05f);
                    srt.anchorMax = new Vector2(0.5f, 0.96f);
                    srt.pivot = new Vector2(0.5f, 0f);
                    srt.sizeDelta = new Vector2(480f, 0f);
                    srt.anchoredPosition = Vector2.zero;
                    AspectRatioFitter arf = spriteObj.AddComponent<AspectRatioFitter>();
                    arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    arf.aspectRatio = 1024f / 1536f;
                    Image eImg = spriteObj.AddComponent<Image>();
                    eImg.sprite = scoutSprite;
                    eImg.preserveAspect = true;

                    // Overhead Name
                    GameObject nameObj = new GameObject("Name", typeof(RectTransform));
                    nameObj.transform.SetParent(mockEnemy.transform, false);
                    RectTransform nrt = nameObj.GetComponent<RectTransform>();
                    nrt.anchorMin = new Vector2(0.5f, 0.91f);
                    nrt.anchorMax = new Vector2(0.5f, 0.91f);
                    nrt.pivot = new Vector2(0.5f, 0f);
                    nrt.sizeDelta = new Vector2(280f, 32f);
                    nrt.anchoredPosition = Vector2.zero;
                    TextMeshProUGUI nTxt = nameObj.AddComponent<TextMeshProUGUI>();
                    nTxt.text = enemyNames[e];
                    nTxt.fontSize = 20;
                    nTxt.fontStyle = FontStyles.Bold;
                    nTxt.alignment = TextAlignmentOptions.Center;
                    nTxt.color = Color.white;

                    // Status Plate under feet
                    GameObject spObj = new GameObject("StatusPlate", typeof(RectTransform));
                    spObj.transform.SetParent(mockEnemy.transform, false);
                    RectTransform sprt = spObj.GetComponent<RectTransform>();
                    sprt.anchorMin = new Vector2(0.5f, 0f);
                    sprt.anchorMax = new Vector2(0.5f, 0f);
                    sprt.pivot = new Vector2(0.5f, 0.5f);
                    sprt.anchoredPosition = new Vector2(0f, -34f);
                    sprt.sizeDelta = new Vector2(250f, 38f);
                    Image spBg = spObj.AddComponent<Image>();
                    spBg.sprite = frameSprite;
                    spBg.type = Image.Type.Sliced;
                    spBg.color = Color.white;

                    HorizontalLayoutGroup sphlg = spObj.AddComponent<HorizontalLayoutGroup>();
                    sphlg.padding = new RectOffset(10, 10, 4, 4);
                    sphlg.spacing = 8f;
                    sphlg.childAlignment = TextAnchor.MiddleCenter;
                    sphlg.childControlWidth = false;
                    sphlg.childControlHeight = true;

                    // HP Text
                    GameObject hpObj = new GameObject("HPText", typeof(RectTransform));
                    hpObj.transform.SetParent(spObj.transform, false);
                    RectTransform hrt = hpObj.GetComponent<RectTransform>();
                    hrt.sizeDelta = new Vector2(105f, 30f);
                    TextMeshProUGUI hpTxt = hpObj.AddComponent<TextMeshProUGUI>();
                    hpTxt.text = "49/49 HP";
                    hpTxt.fontSize = 13;
                    hpTxt.fontStyle = FontStyles.Bold;
                    hpTxt.color = new Color(0.35f, 0.90f, 0.45f, 1f);
                    hpTxt.alignment = TextAlignmentOptions.MidlineLeft;

                    // Sed Text
                    GameObject sedObj = new GameObject("SedText", typeof(RectTransform));
                    sedObj.transform.SetParent(spObj.transform, false);
                    RectTransform srt2 = sedObj.GetComponent<RectTransform>();
                    srt2.sizeDelta = new Vector2(115f, 30f);
                    TextMeshProUGUI sedTxt = sedObj.AddComponent<TextMeshProUGUI>();
                    sedTxt.text = "0% [ ] [ ]";
                    sedTxt.fontSize = 12;
                    sedTxt.fontStyle = FontStyles.Bold;
                    sedTxt.color = new Color(1f, 0.50f, 0.80f, 1f);
                    sedTxt.alignment = TextAlignmentOptions.MidlineRight;
                }
            }

            // --- C. Cards Hand Mockup inside CardsContainer (4 Mockup Cards) ---
            if (combatUI.cardsContainer != null)
            {
                for (int i = combatUI.cardsContainer.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(combatUI.cardsContainer.GetChild(i).gameObject);
                }

                Sprite cardFrame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/UI/ui_card_frame.png");
                string[] mockCardNames = new string[] { "Strike", "Guard", "Wait & Observe", "Palm Thrust" };

                for (int c = 0; c < 4; c++)
                {
                    GameObject cObj = new GameObject($"CardMock_{c}_{mockCardNames[c]}", typeof(RectTransform));
                    cObj.transform.SetParent(combatUI.cardsContainer, false);
                    RectTransform crt = cObj.GetComponent<RectTransform>();
                    crt.sizeDelta = new Vector2(130f, 175f);

                    Image cBg = cObj.AddComponent<Image>();
                    cBg.sprite = cardFrame;
                    cBg.color = Color.white;

                    GameObject txtObj = new GameObject("Name", typeof(RectTransform));
                    txtObj.transform.SetParent(cObj.transform, false);
                    RectTransform trt = txtObj.GetComponent<RectTransform>();
                    trt.anchorMin = new Vector2(0f, 0f);
                    trt.anchorMax = new Vector2(1f, 0.28f);
                    trt.offsetMin = new Vector2(4f, 4f);
                    trt.offsetMax = new Vector2(-4f, -4f);
                    TextMeshProUGUI nTxt = txtObj.AddComponent<TextMeshProUGUI>();
                    nTxt.text = mockCardNames[c];
                    nTxt.fontSize = 12;
                    nTxt.fontStyle = FontStyles.Bold;
                    nTxt.alignment = TextAlignmentOptions.Center;
                    nTxt.color = Color.white;
                }
            }

            // =========================================================================
            // SAVE PREVIEW SCENE
            // =========================================================================
            string scenePath = "Assets/Scenes/CombatPreviewScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Combat UI Baked (Full WYSIWYG)", 
                "Full visual mockup baked successfully!\n\n" +
                "1. Heroine: Visible with face, hair & body layers!\n" +
                "2. Enemies: Max 3 Elven Scouts visible with HP/Sed bars!\n" +
                "3. Cards: 4 Mockup Cards visible in the vertical hand!\n\n" +
                "All elements are selectable and movable in Scene View!\n" +
                "When you save the prefab/scene, the runtime game uses these exact positions.", "OK");
        }

        private static void CreateLayerSprite(Transform parent, string name, string assetPath)
        {
            Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (spr == null) return;

            GameObject lObj = new GameObject(name, typeof(RectTransform));
            lObj.transform.SetParent(parent, false);
            RectTransform lrt = lObj.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            Image img = lObj.AddComponent<Image>();
            img.sprite = spr;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
    }
}
#endif
