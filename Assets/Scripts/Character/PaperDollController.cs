using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// Controls the layered Paper Doll character display in UI Canvas.
/// All sprites share the canvas coordinate space and overlay accurately.
/// Includes smooth Zoom In/Out controls (+ / - buttons) focusing down to elbows with safe forehead headroom.
/// Also supports smooth shifting to HUD position for Expedition Mode.
/// </summary>
public class PaperDollController : MonoBehaviour
{
    [Header("Body Sprites (one per body size)")]
    public BodySizeSprite[] bodySprites;

    [Header("Modular Breast Sprites (overlaid on hero_small)")]
    [Tooltip("Assign Small, Medium, Large/XLarge breast sprites here. When populated, hero_small is used as base body.")]
    public BodySizeSprite[] breastSprites;

    [Header("UI Image Layers - assign in Inspector")]
    public Image hairBackImage;
    public Image bodyImage;
    public Image groinImage;
    public Image breastImage;
    public Image underwearImage;

    [Header("Underwear Layers (Split: Bra & Panties)")]
    public Image underUpperImage; // Bra / Upper Underwear
    public Image underLowerImage; // Panties / Lower Underwear

    public Image clothesImage;
    public Image lowerClothesImage;
    public Image expressionImage;
    public Image blushImage;
    public Image eyebrowsImage;
    public Image hairFrontImage;
    public Image accessoryImage;

    [Header("Head Container Anchor")]
    public RectTransform headContainer;

    [Header("Shaders & Materials")]
    public Shader eyeColorShader;
    private Material eyeMaterialInstance;

    [Header("Zoom Settings")]
    public float[] zoomScales = new float[] { 1.0f, 1.45f, 2.15f };
    public float[] zoomYOffsets = new float[] { 0f, -280f, -620f };
    private int currentZoomIndex = 0;
    private Vector2 baseAnchoredPosition;
    private bool basePosInitialized = false;

    private GameObject zoomControlsObj;
    private Button zoomInButton;
    private Button zoomOutButton;


    private bool allowZoomControls = true;

    private CharacterState characterState;

    private void Awake()
    {
        EnsureGroinLayer();
        EnsureBreastLayer();
        EnsureUnderwearLayers();
        EnsureLowerClothesLayer();
        EnsureExpressionLayer();

        // Enforce preserveAspect and stretch on all paper doll layers
        Image[] allLayers = { hairBackImage, bodyImage, groinImage, breastImage, underwearImage, underLowerImage, underUpperImage, lowerClothesImage, clothesImage, expressionImage, blushImage, hairFrontImage, eyebrowsImage, accessoryImage };
        foreach (var img in allLayers)
        {
            if (img != null)
            {
                img.preserveAspect = true;
                if (img.sprite == null)
                {
                    img.enabled = false;
                }
            }
        }

        // Clean up legacy hair and blush layers (hair is on body, blush/emotions are integrated in expressions)
        if (hairBackImage != null) ClearImage(hairBackImage);
        if (hairFrontImage != null) ClearImage(hairFrontImage);
        if (blushImage != null) ClearImage(blushImage);

#if UNITY_EDITOR
        if (breastSprites == null || breastSprites.Length == 0)
        {
            AutoAssignBreastSprites();
        }
#endif

        RefreshBody();
    }

    private void Start()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null && !basePosInitialized)
        {
            baseAnchoredPosition = rt.anchoredPosition;
            basePosInitialized = true;
        }

        EnsureZoomControls();
    }

    private void OnEnable()
    {
        if (zoomControlsObj != null)
            zoomControlsObj.SetActive(allowZoomControls);
    }

    private void OnDisable()
    {
        if (zoomControlsObj != null)
            zoomControlsObj.SetActive(false);
    }

    [Header("Expedition Positioning")]
    public float expeditionXOffset = 150f;
    private bool isInExpeditionMode = false;

    private void Update()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null && basePosInitialized)
        {
            float targetScale = zoomScales[Mathf.Clamp(currentZoomIndex, 0, zoomScales.Length - 1)];
            float targetY = baseAnchoredPosition.y + zoomYOffsets[Mathf.Clamp(currentZoomIndex, 0, zoomYOffsets.Length - 1)];
            float targetX = baseAnchoredPosition.x + (isInExpeditionMode ? expeditionXOffset : 0f);

            Vector3 curScale = rt.localScale;
            float newScale = Mathf.Lerp(curScale.x, targetScale, Time.unscaledDeltaTime * 12f);
            rt.localScale = new Vector3(newScale, newScale, 1f);

            Vector2 curPos = rt.anchoredPosition;
            float newX = Mathf.Lerp(curPos.x, targetX, Time.unscaledDeltaTime * 12f);
            float newY = Mathf.Lerp(curPos.y, targetY, Time.unscaledDeltaTime * 12f);
            rt.anchoredPosition = new Vector2(newX, newY);
        }
    }

    public void SetExpeditionMode(bool isExpedition)
    {
        isInExpeditionMode = isExpedition;
        SetZoomControlsVisible(!isExpedition);
        if (isExpedition)
        {
            currentZoomIndex = 0;
            UpdateZoomButtonsInteractable();
        }

        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null && basePosInitialized)
        {
            float targetX = baseAnchoredPosition.x + (isInExpeditionMode ? expeditionXOffset : 0f);
            rt.anchoredPosition = new Vector2(targetX, rt.anchoredPosition.y);
        }
    }

    public void SetZoomControlsVisible(bool visible)
    {
        allowZoomControls = visible;
        if (zoomControlsObj != null)
        {
            zoomControlsObj.SetActive(visible && gameObject.activeInHierarchy);
        }
    }

    public void Initialize(CharacterState state)
    {
        characterState = state;
        characterState.OnBodySizeChanged += OnBodySizeChanged;
        characterState.OnEquipmentChanged += RefreshEquipment;
        characterState.OnAppearanceChanged += RefreshAppearance;

        RefreshBody();
        RefreshEquipment();
        RefreshAppearance();
    }

    private void OnDestroy()
    {
        if (characterState != null)
        {
            characterState.OnBodySizeChanged -= OnBodySizeChanged;
            characterState.OnEquipmentChanged -= RefreshEquipment;
            characterState.OnAppearanceChanged -= RefreshAppearance;
        }

        if (eyeMaterialInstance != null)
        {
            Destroy(eyeMaterialInstance);
        }

        if (zoomControlsObj != null)
        {
            Destroy(zoomControlsObj);
        }
    }

    private void OnBodySizeChanged(BodySizeType newSize)
    {
        RefreshBody();
        RefreshEquipment(); // clothes sprite also depends on body size
    }

    public bool HasModularBreastSprites
    {
        get
        {
            if (breastSprites != null && breastSprites.Length > 0) return true;
#if UNITY_EDITOR
            if (System.IO.File.Exists("Assets/sprites/doll/Breast_medium.png"))
            {
                AutoAssignBreastSprites();
                return breastSprites != null && breastSprites.Length > 0;
            }
#endif
            return false;
        }
    }

    /// <summary>Switches the base body sprite to match current body size, or uses hero_small if modular breasts are configured.</summary>
    public void RefreshBody()
    {
        if (HasModularBreastSprites)
        {
            // Use hero_small (BodySizeType.Small) as the base body
            Sprite baseSmallSprite = null;
            if (bodySprites != null)
            {
                foreach (var bs in bodySprites)
                {
                    if (bs.bodySize == BodySizeType.Small && bs.sprite != null)
                    {
                        baseSmallSprite = bs.sprite;
                        break;
                    }
                }
                if (baseSmallSprite == null)
                {
                    foreach (var bs in bodySprites)
                    {
                        if (bs.sprite != null)
                        {
                            baseSmallSprite = bs.sprite;
                            break;
                        }
                    }
                }
            }

#if UNITY_EDITOR
            if (baseSmallSprite == null)
            {
                baseSmallSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/doll/hero_small.png");
            }
#endif

            if (baseSmallSprite != null)
            {
                SetImage(bodyImage, baseSmallSprite);
            }
            else
            {
                ClearImage(bodyImage);
            }

            RefreshBreastLayer();
            return;
        }

        if (characterState == null)
        {
            if (bodySprites != null && bodySprites.Length > 0 && bodySprites[0].sprite != null)
            {
                SetImage(bodyImage, bodySprites[0].sprite);
            }
            return;
        }

        // Fallback to legacy full-body switching
        if (bodySprites != null)
        {
            foreach (var bs in bodySprites)
            {
                if (bs.bodySize == characterState.CurrentBodySize)
                {
                    SetImage(bodyImage, bs.sprite);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Updates or hides the modular breast layer depending on equipped clothing and body size.
    /// </summary>
    public void RefreshBreastLayer()
    {
        if (breastImage == null) return;

        if (!HasModularBreastSprites || characterState == null)
        {
            ClearImage(breastImage);
            return;
        }

        if (characterState.CurrentBodySize == BodySizeType.Small)
        {
            // Small is already integrated into hero_small base body, no overlay needed
            ClearImage(breastImage);
            return;
        }

        // Find matching breast sprite for current body size (Medium, Large, XLarge)
        Sprite bSprite = null;
        if (breastSprites != null)
        {
            foreach (var bs in breastSprites)
            {
                if (bs.bodySize == characterState.CurrentBodySize && bs.sprite != null)
                {
                    bSprite = bs.sprite;
                    break;
                }
            }

            // Fallback to highest available if size is XLarge and only Large exists
            if (bSprite == null && breastSprites.Length > 0)
            {
                for (int i = breastSprites.Length - 1; i >= 0; i--)
                {
                    if (breastSprites[i].sprite != null)
                    {
                        bSprite = breastSprites[i].sprite;
                        break;
                    }
                }
            }
        }

#if UNITY_EDITOR
        if (bSprite == null)
        {
            string bPath = characterState.CurrentBodySize switch
            {
                BodySizeType.Medium => "Assets/sprites/doll/Breast_medium.png",
                BodySizeType.Large => "Assets/sprites/doll/Breast_large.png",
                BodySizeType.XLarge => "Assets/sprites/doll/Breast_xlarge.png",
                _ => null
            };
            if (bPath != null)
            {
                bSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(bPath);
            }
        }
#endif

        if (bSprite == null)
        {
            ClearImage(breastImage);
            return;
        }

        // Check if equipped upper or combined clothing hides the breast layer
        ItemData upper = characterState.GetEquippedItem(EquipSlot.UpperClothes);
        bool hasVisibleUpperClothes = (clothesImage != null && clothesImage.enabled && clothesImage.sprite != null);
        bool shouldHide = hasVisibleUpperClothes && (upper != null && upper.hidesBreastLayer);

        if (shouldHide)
        {
            breastImage.enabled = false;
        }
        else
        {
            SetImage(breastImage, bSprite);
        }
    }

    public void EnsureGroinLayer()
    {
        if (groinImage == null && bodyImage != null)
        {
            Transform existing = transform.Find("Groin");
            if (existing != null)
            {
                groinImage = existing.GetComponent<Image>();
            }
            else
            {
                GameObject gObj = new GameObject("Groin", typeof(RectTransform), typeof(Image));
                gObj.transform.SetParent(transform, false);

                int bodyIdx = bodyImage.transform.GetSiblingIndex();
                gObj.transform.SetSiblingIndex(bodyIdx + 1);

                RectTransform gRt = gObj.GetComponent<RectTransform>();
                RectTransform bodyRt = bodyImage.rectTransform;
                gRt.anchorMin = bodyRt.anchorMin;
                gRt.anchorMax = bodyRt.anchorMax;
                gRt.pivot = bodyRt.pivot;
                gRt.anchoredPosition = bodyRt.anchoredPosition;
                gRt.sizeDelta = bodyRt.sizeDelta;

                groinImage = gObj.GetComponent<Image>();
                groinImage.raycastTarget = false;
                groinImage.enabled = false;
            }
        }

        if (groinImage != null)
        {
            groinImage.preserveAspect = true;
            if (groinImage.sprite == null)
            {
                groinImage.enabled = false;
            }
        }
    }

    /// <summary>
    /// Updates groin deformation overlay. Strictly visible ONLY when character has no underwear or lower clothing covering the area.
    /// </summary>
    public void RefreshGroinLayer()
    {
        EnsureGroinLayer();
        if (groinImage == null) return;

        Sprite gSprite = characterState != null ? characterState.selectedGroin : null;
        if (gSprite == null)
        {
            ClearImage(groinImage);
            return;
        }

        // Groin is strictly visible only if NOT covered by underwear, lower clothes, or full-body combined outfit
        bool hasUnderwear = (underLowerImage != null && underLowerImage.enabled && underLowerImage.sprite != null) ||
                            (underwearImage != null && underwearImage.enabled && underwearImage.sprite != null);
        bool hasLower = (lowerClothesImage != null && lowerClothesImage.enabled && lowerClothesImage.sprite != null);

        bool hasCombinedSuit = false;
        if (characterState != null)
        {
            ItemData upperOrComb = characterState.GetEquippedItem(EquipSlot.UpperClothes);
            hasCombinedSuit = (upperOrComb != null && upperOrComb.IsCombinedArmor && clothesImage != null && clothesImage.enabled && clothesImage.sprite != null);
        }

        if (hasUnderwear || hasLower || hasCombinedSuit)
        {
            groinImage.enabled = false;
        }
        else
        {
            SetImage(groinImage, gSprite);
        }
    }

    public void SetGroinSprite(Sprite sprite)
    {
        if (characterState != null)
        {
            characterState.SetGroin(sprite);
        }
        else
        {
            if (groinImage != null)
            {
                SetImage(groinImage, sprite);
            }
        }
    }

    public void EnsureBreastLayer()
    {
        EnsureGroinLayer();
        if (breastImage == null && bodyImage != null)
        {
            Transform existing = transform.Find("Breast");
            if (existing != null)
            {
                breastImage = existing.GetComponent<Image>();
            }
            else
            {
                GameObject bObj = new GameObject("Breast", typeof(RectTransform), typeof(Image));
                bObj.transform.SetParent(transform, false);

                int targetIdx = groinImage != null ? groinImage.transform.GetSiblingIndex() + 1 : bodyImage.transform.GetSiblingIndex() + 1;
                bObj.transform.SetSiblingIndex(targetIdx);

                RectTransform bRt = bObj.GetComponent<RectTransform>();
                RectTransform bodyRt = bodyImage.rectTransform;
                bRt.anchorMin = bodyRt.anchorMin;
                bRt.anchorMax = bodyRt.anchorMax;
                bRt.pivot = bodyRt.pivot;
                bRt.anchoredPosition = bodyRt.anchoredPosition;
                bRt.sizeDelta = bodyRt.sizeDelta;

                breastImage = bObj.GetComponent<Image>();
                breastImage.raycastTarget = false;
                breastImage.enabled = false;
            }
        }

        if (breastImage != null)
        {
            breastImage.preserveAspect = true;
            if (breastImage.sprite == null)
            {
                breastImage.enabled = false;
            }
        }
    }

    public void EnsureUnderwearLayers()
    {
        if (bodyImage == null) return;

        if (underLowerImage == null)
        {
            if (underwearImage != null)
            {
                underLowerImage = underwearImage;
            }
            else
            {
                Transform existing = transform.Find("UnderLower");
                if (existing != null)
                {
                    underLowerImage = existing.GetComponent<Image>();
                }
                else
                {
                    GameObject ulObj = new GameObject("UnderLower", typeof(RectTransform), typeof(Image));
                    ulObj.transform.SetParent(transform, false);

                    int insertIdx = groinImage != null ? groinImage.transform.GetSiblingIndex() + 1 : bodyImage.transform.GetSiblingIndex() + 1;
                    ulObj.transform.SetSiblingIndex(insertIdx);

                    RectTransform ulRt = ulObj.GetComponent<RectTransform>();
                    RectTransform bodyRt = bodyImage.rectTransform;
                    ulRt.anchorMin = bodyRt.anchorMin;
                    ulRt.anchorMax = bodyRt.anchorMax;
                    ulRt.pivot = bodyRt.pivot;
                    ulRt.anchoredPosition = bodyRt.anchoredPosition;
                    ulRt.sizeDelta = bodyRt.sizeDelta;

                    underLowerImage = ulObj.GetComponent<Image>();
                    underLowerImage.raycastTarget = false;
                    underLowerImage.enabled = false;
                }
            }
        }

        if (underUpperImage == null)
        {
            Transform existing = transform.Find("UnderUpper");
            if (existing != null)
            {
                underUpperImage = existing.GetComponent<Image>();
            }
            else
            {
                GameObject uuObj = new GameObject("UnderUpper", typeof(RectTransform), typeof(Image));
                uuObj.transform.SetParent(transform, false);

                int insertIdx = underLowerImage != null ? underLowerImage.transform.GetSiblingIndex() + 1 : (breastImage != null ? breastImage.transform.GetSiblingIndex() + 1 : bodyImage.transform.GetSiblingIndex() + 1);
                uuObj.transform.SetSiblingIndex(insertIdx);

                RectTransform uuRt = uuObj.GetComponent<RectTransform>();
                RectTransform bodyRt = bodyImage.rectTransform;
                uuRt.anchorMin = bodyRt.anchorMin;
                uuRt.anchorMax = bodyRt.anchorMax;
                uuRt.pivot = bodyRt.pivot;
                uuRt.anchoredPosition = bodyRt.anchoredPosition;
                uuRt.sizeDelta = bodyRt.sizeDelta;

                underUpperImage = uuObj.GetComponent<Image>();
                underUpperImage.raycastTarget = false;
                underUpperImage.enabled = false;
            }
        }

        if (underLowerImage != null)
        {
            underLowerImage.preserveAspect = true;
            if (underLowerImage.sprite == null) underLowerImage.enabled = false;
        }

        if (underUpperImage != null)
        {
            underUpperImage.preserveAspect = true;
            if (underUpperImage.sprite == null) underUpperImage.enabled = false;
        }
    }

    public void EnsureLowerClothesLayer()
    {
        if (lowerClothesImage == null && bodyImage != null)
        {
            Transform existing = transform.Find("LowerClothes");
            if (existing != null)
            {
                lowerClothesImage = existing.GetComponent<Image>();
            }
            else
            {
                GameObject lObj = new GameObject("LowerClothes", typeof(RectTransform), typeof(Image));
                lObj.transform.SetParent(transform, false);

                int clothesIdx = clothesImage != null ? clothesImage.transform.GetSiblingIndex() : transform.childCount - 1;
                lObj.transform.SetSiblingIndex(clothesIdx);

                RectTransform lRt = lObj.GetComponent<RectTransform>();
                RectTransform bodyRt = bodyImage.rectTransform;
                lRt.anchorMin = bodyRt.anchorMin;
                lRt.anchorMax = bodyRt.anchorMax;
                lRt.pivot = bodyRt.pivot;
                lRt.anchoredPosition = bodyRt.anchoredPosition;
                lRt.sizeDelta = bodyRt.sizeDelta;

                lowerClothesImage = lObj.GetComponent<Image>();
                lowerClothesImage.raycastTarget = false;
                lowerClothesImage.enabled = false;
            }
        }

        if (lowerClothesImage != null)
        {
            lowerClothesImage.preserveAspect = true;
            if (lowerClothesImage.sprite == null)
            {
                lowerClothesImage.enabled = false;
            }
        }
    }

    public void EnsureExpressionLayer()
    {
        if (expressionImage != null)
        {
            if (!expressionImage.gameObject.activeSelf)
            {
                expressionImage.gameObject.SetActive(true);
            }

            if (bodyImage != null)
            {
                if (expressionImage.transform.parent != transform)
                {
                    expressionImage.transform.SetParent(transform, false);
                    expressionImage.transform.SetAsLastSibling();
                }

                RectTransform expRt = expressionImage.rectTransform;
                RectTransform bodyRt = bodyImage.rectTransform;
                expRt.anchorMin = bodyRt.anchorMin;
                expRt.anchorMax = bodyRt.anchorMax;
                expRt.pivot = bodyRt.pivot;
                expRt.anchoredPosition = bodyRt.anchoredPosition;
                expRt.sizeDelta = bodyRt.sizeDelta;
                expRt.localRotation = Quaternion.identity;
                expRt.localScale = Vector3.one;
            }

            expressionImage.preserveAspect = true;

            if (expressionImage.sprite == null)
            {
                Sprite baseSpr = characterState != null ? characterState.selectedExpression : null;
#if UNITY_EDITOR
                if (baseSpr == null)
                {
                    baseSpr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/Expression/Emotions/Normal.png");
                }
#endif
                if (baseSpr != null)
                {
                    expressionImage.sprite = baseSpr;
                    expressionImage.enabled = true;
                }
            }
            else
            {
                expressionImage.enabled = true;
            }
        }
    }

    private Coroutine tempEmotionCoroutine;
    private bool hadClothesLastCheck = false;
    private bool hasInitializedClothesTracking = false;

    /// <summary>Refreshes all inventory-managed equipment layers.</summary>
    public void RefreshEquipment()
    {
        EnsureGroinLayer();
        EnsureBreastLayer();
        EnsureLowerClothesLayer();

        bool hadClothes = (clothesImage != null && clothesImage.enabled && clothesImage.sprite != null) ||
                          (lowerClothesImage != null && lowerClothesImage.enabled && lowerClothesImage.sprite != null);
        if (!hasInitializedClothesTracking)
        {
            hadClothesLastCheck = hadClothes;
            hasInitializedClothesTracking = true;
        }

        RefreshSlot(EquipSlot.UpperClothes, clothesImage);
        if (clothesImage == null || !clothesImage.enabled || clothesImage.sprite == null)
        {
            RefreshSlot(EquipSlot.Clothes, clothesImage);
        }

        ItemData lower = characterState != null ? characterState.GetEquippedItem(EquipSlot.LowerClothes) : null;
        if (lower != null && !lower.IsCombinedArmor)
        {
            RefreshSlot(EquipSlot.LowerClothes, lowerClothesImage != null ? lowerClothesImage : underwearImage);
        }
        else
        {
            ClearImage(lowerClothesImage);
        }

        EnsureUnderwearLayers();
        RefreshSlot(EquipSlot.UnderUpper, underUpperImage);
        RefreshSlot(EquipSlot.UnderLower, underLowerImage != null ? underLowerImage : underwearImage);
        RefreshSlot(EquipSlot.Accessory, accessoryImage);

        RefreshBreastLayer();
        RefreshGroinLayer();

        bool hasClothesNow = (clothesImage != null && clothesImage.enabled && clothesImage.sprite != null) ||
                             (lowerClothesImage != null && lowerClothesImage.enabled && lowerClothesImage.sprite != null);
        if (hadClothesLastCheck && !hasClothesNow)
        {
            GameUIManager gum = FindFirstObjectByType<GameUIManager>();
            bool isLobby = (gum != null && gum.currentState == GameUIManager.GameFlowState.Lobby);
            if (!isLobby)
            {
                TriggerTemporaryEmotion("shocked", 1.8f);
            }
        }
        hadClothesLastCheck = hasClothesNow;
    }

    /// <summary>Refreshes hair, face expressions, blush, and custom colors.</summary>
    public void RefreshAppearance()
    {
        if (characterState == null) return;

        // Legacy hair layers are cleared (hair is drawn directly on the canonical body sprite)
        if (hairBackImage != null)
        {
            ClearImage(hairBackImage);
            hairBackImage.enabled = false;
            hairBackImage.gameObject.SetActive(false);
        }
        if (hairFrontImage != null)
        {
            ClearImage(hairFrontImage);
            hairFrontImage.enabled = false;
            hairFrontImage.gameObject.SetActive(false);
        }

        // Expression (Natural art color, no shader tinting needed)
        if (expressionImage != null)
        {
            if (characterState.selectedExpression != null)
            {
                if (!expressionImage.gameObject.activeSelf) expressionImage.gameObject.SetActive(true);
                expressionImage.sprite = characterState.selectedExpression;
                expressionImage.enabled = true;
                expressionImage.preserveAspect = true;
                expressionImage.color = Color.white;
                expressionImage.material = null;
            }
            else
            {
                Sprite defExpr = null;
#if UNITY_EDITOR
                defExpr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/Expression/Emotions/Normal.png");
#endif
                if (defExpr != null)
                {
                    if (!expressionImage.gameObject.activeSelf) expressionImage.gameObject.SetActive(true);
                    expressionImage.sprite = defExpr;
                    expressionImage.enabled = true;
                    expressionImage.preserveAspect = true;
                }
                else
                {
                    ClearImage(expressionImage);
                }
            }
        }

        // Blush
        if (blushImage != null)
        {
            if (characterState.selectedBlush != null)
            {
                blushImage.sprite = characterState.selectedBlush;
                blushImage.enabled = true;
                blushImage.preserveAspect = true;
                blushImage.color = Color.white;
            }
            else
            {
                ClearImage(blushImage);
            }
        }

        RefreshGroinLayer();
    }

    private void RefreshSlot(EquipSlot slot, Image targetImage)
    {
        if (targetImage == null) return;

        ItemData equipped = characterState.GetEquippedItem(slot);
        if (equipped != null)
        {
            Sprite spr = equipped.GetSpriteForSize(characterState.CurrentBodySize);
            if (spr != null)
            {
                SetImage(targetImage, spr);
                return;
            }
        }

        ClearImage(targetImage);
    }

    private void SetImage(Image img, Sprite sprite)
    {
        if (img == null) return;
        if (sprite == null)
        {
            ClearImage(img);
            return;
        }
        img.sprite = sprite;
        img.enabled = true;
        img.preserveAspect = true;
        img.color = Color.white;
    }

    private void LateUpdate()
    {
        SyncHairBack();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        SyncHairBack();
        if (groinImage == null && transform != null)
        {
            Transform g = transform.Find("Groin");
            if (g != null) groinImage = g.GetComponent<Image>();
        }
        if (breastImage == null && transform != null)
        {
            Transform b = transform.Find("Breast");
            if (b != null) breastImage = b.GetComponent<Image>();
        }

        if (bodySprites == null || bodySprites.Length == 0 || bodySprites[0].sprite == null)
        {
            AutoAssignBodySprites();
        }

        if (breastSprites == null || breastSprites.Length == 0)
        {
            AutoAssignBreastSprites();
        }
    }

    [ContextMenu("Auto-Assign Body Sprites")]
    public void AutoAssignBodySprites()
    {
        var list = new System.Collections.Generic.List<BodySizeSprite>();
        Sprite sm = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/doll/hero_small.png");
        if (sm != null) list.Add(new BodySizeSprite { bodySize = BodySizeType.Small, sprite = sm });
        Sprite md = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/doll/hero_medium.png");
        if (md != null) list.Add(new BodySizeSprite { bodySize = BodySizeType.Medium, sprite = md });
        Sprite lg = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/doll/hero_large.png");
        if (lg != null) list.Add(new BodySizeSprite { bodySize = BodySizeType.Large, sprite = lg });
        Sprite xlg = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/doll/hero_xlarge.png");
        if (xlg != null) list.Add(new BodySizeSprite { bodySize = BodySizeType.XLarge, sprite = xlg });

        if (list.Count > 0)
        {
            bodySprites = list.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[PaperDollController] Auto-assigned {list.Count} body sprites!");
        }

        if (bodyImage != null && sm != null)
        {
            bodyImage.sprite = sm;
            bodyImage.enabled = true;
            UnityEditor.EditorUtility.SetDirty(bodyImage);
        }
    }

    [ContextMenu("Auto-Assign Breast Sprites")]
    public void AutoAssignBreastSprites()
    {
        var list = new System.Collections.Generic.List<BodySizeSprite>();
        
        Sprite med = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/doll/Breast_medium.png");
        if (med != null) list.Add(new BodySizeSprite { bodySize = BodySizeType.Medium, sprite = med });
        
        Sprite lrg = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/doll/Breast_large.png");
        if (lrg != null) list.Add(new BodySizeSprite { bodySize = BodySizeType.Large, sprite = lrg });
        
        Sprite xlrg = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprites/doll/Breast_xlarge.png");
        if (xlrg != null) list.Add(new BodySizeSprite { bodySize = BodySizeType.XLarge, sprite = xlrg });

        if (list.Count > 0)
        {
            breastSprites = list.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[PaperDollController] Auto-assigned {list.Count} modular breast sprites!");
        }
    }
#endif

    public void SyncHairBack()
    {
        if (hairBackImage != null && headContainer != null)
        {
            RectTransform hbRect = hairBackImage.rectTransform;
            if (hbRect.parent == transform)
            {
                if (hbRect.anchoredPosition != headContainer.anchoredPosition || 
                    hbRect.sizeDelta != headContainer.sizeDelta || 
                    hbRect.localRotation != headContainer.localRotation)
                {
                    hbRect.anchorMin = headContainer.anchorMin;
                    hbRect.anchorMax = headContainer.anchorMax;
                    hbRect.pivot = headContainer.pivot;
                    hbRect.anchoredPosition = headContainer.anchoredPosition;
                    hbRect.sizeDelta = headContainer.sizeDelta;
                    hbRect.localRotation = headContainer.localRotation;
                }
            }
        }
    }

    private void ClearImage(Image img)
    {
        if (img == null) return;
        img.sprite = null;
        img.enabled = false;
    }

    // =========================================================================
    // ZOOM SYSTEM (+ / - BUTTONS)
    // =========================================================================

    public void ZoomIn()
    {
        if (currentZoomIndex < zoomScales.Length - 1)
        {
            currentZoomIndex++;
            UpdateZoomButtonsInteractable();
        }
    }

    public void ZoomOut()
    {
        if (currentZoomIndex > 0)
        {
            currentZoomIndex--;
            UpdateZoomButtonsInteractable();
        }
    }

    private void UpdateZoomButtonsInteractable()
    {
        if (zoomInButton != null)
            zoomInButton.interactable = currentZoomIndex < zoomScales.Length - 1;
        if (zoomOutButton != null)
            zoomOutButton.interactable = currentZoomIndex > 0;
    }

    public void EnsureZoomControls()
    {
        if (transform.parent == null) return;

        Transform existing = transform.parent.Find("PaperDollZoomControls");
        if (existing != null)
        {
            zoomControlsObj = existing.gameObject;
            zoomInButton = zoomControlsObj.transform.Find("ZoomInBtn")?.GetComponent<Button>();
            zoomOutButton = zoomControlsObj.transform.Find("ZoomOutBtn")?.GetComponent<Button>();
        }
        else
        {
            zoomControlsObj = new GameObject("PaperDollZoomControls", typeof(RectTransform));
            zoomControlsObj.transform.SetParent(transform.parent, false);

            VerticalLayoutGroup vlg = zoomControlsObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            zoomInButton = CreateZoomButton(zoomControlsObj.transform, "ZoomInBtn", "+", () => ZoomIn());
            zoomOutButton = CreateZoomButton(zoomControlsObj.transform, "ZoomOutBtn", "-", () => ZoomOut());
        }

        // Position on the far LEFT side of the screen (x = 35, y = 35) so the right-docked Lobby window does not overlap
        RectTransform zrt = zoomControlsObj.GetComponent<RectTransform>();
        if (zrt != null)
        {
            zrt.anchorMin = new Vector2(0f, 0f);
            zrt.anchorMax = new Vector2(0f, 0f);
            zrt.pivot = new Vector2(0f, 0f);
            zrt.sizeDelta = new Vector2(50f, 114f);
            zrt.anchoredPosition = new Vector2(35f, 35f);
        }

        zoomControlsObj.transform.SetAsLastSibling();
        zoomControlsObj.SetActive(allowZoomControls && gameObject.activeInHierarchy);
        UpdateZoomButtonsInteractable();
    }

    private Button CreateZoomButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(0.16f, 0.18f, 0.25f, 0.92f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;

        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.18f, 0.20f, 0.28f, 0.92f);
        cb.highlightedColor = new Color(0.30f, 0.40f, 0.55f, 1f);
        cb.pressedColor = new Color(0.12f, 0.14f, 0.20f, 1f);
        cb.disabledColor = new Color(0.14f, 0.14f, 0.18f, 0.45f);
        btn.colors = cb;

        btn.onClick.AddListener(onClick);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 28;
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

    public void TriggerTemporaryEmotion(string emotionName, float duration)
    {
        if (!gameObject.activeInHierarchy) return;
        if (expressionImage == null) return;
        if (characterState == null || characterState.appearanceDatabase == null) return;

        Sprite targetSprite = FindEmotionSprite(characterState.appearanceDatabase, emotionName);
        if (targetSprite == null) return;

        if (tempEmotionCoroutine != null) StopCoroutine(tempEmotionCoroutine);
        tempEmotionCoroutine = StartCoroutine(PaperDollTempEmotionRoutine(targetSprite, duration));
    }

    private IEnumerator PaperDollTempEmotionRoutine(Sprite tempSprite, float duration)
    {
        Sprite baseEmotion = characterState != null ? characterState.selectedExpression : null;
        expressionImage.sprite = tempSprite;
        expressionImage.enabled = true;

        yield return new WaitForSeconds(duration);

        if (expressionImage != null)
        {
            if (baseEmotion != null)
            {
                expressionImage.sprite = baseEmotion;
            }
            else if (characterState != null && characterState.selectedExpression != null)
            {
                expressionImage.sprite = characterState.selectedExpression;
            }
        }
        tempEmotionCoroutine = null;
    }

    private Sprite FindEmotionSprite(CharacterAppearanceDatabase db, string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        string cleanKey = key.Trim();
        string normKey = cleanKey.Replace(" ", "").ToLowerInvariant();

        if (normKey == "crying" || normKey == "hurt") cleanKey = "Pain";
        else if (normKey.Contains("angry")) cleanKey = "Angry";
        normKey = cleanKey.Replace(" ", "").ToLowerInvariant();

        if (db != null && db.emotionStyles != null)
        {
            foreach (var s in db.emotionStyles)
            {
                if (s == null) continue;
                if (s.name.IndexOf(cleanKey, StringComparison.OrdinalIgnoreCase) >= 0) return s;
                string sNorm = s.name.Replace(" ", "").ToLowerInvariant();
                if (sNorm.Contains(normKey) || normKey.Contains(sNorm)) return s;
            }
        }

        Sprite[] loaded = Resources.FindObjectsOfTypeAll<Sprite>();
        if (loaded != null)
        {
            foreach (var s in loaded)
            {
                if (s == null) continue;
                if (s.name.IndexOf(cleanKey, StringComparison.OrdinalIgnoreCase) >= 0) return s;
                string sNorm = s.name.Replace(" ", "").ToLowerInvariant();
                if (sNorm == normKey || sNorm.Contains(normKey)) return s;
            }
        }

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets($"{cleanKey} t:Sprite", new[] { "Assets/sprites/Expression/Emotions", "Assets/sprites/Expression" });
        if (guids != null && guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            Sprite sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) return sp;
        }
#endif
        return null;
    }
}
