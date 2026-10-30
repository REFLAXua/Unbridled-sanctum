using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// Post-Combat Transition screen triggered between combat nodes when Semen/Prana
/// accumulation reaches the transformation threshold (e.g. 200ml).
/// Safely swaps body size and clothing in PaperDoll without combat desyncs.
/// </summary>
public class PostCombatTransitionUI : MonoBehaviour
{
    public static PostCombatTransitionUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject panelRoot;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public TextMeshProUGUI progressionBadgeText;
    public TextMeshProUGUI narrativeText;
    public Slider pranaSlider;
    public Image pranaSliderFill;
    public TextMeshProUGUI pranaSliderLabel;
    public Button proceedButton;
    public TextMeshProUGUI proceedBtnText;

    private Action onCompleteCallback;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    /// <summary>
    /// Displays the Post-Combat Transformation screen with narrative text and proceed trigger.
    /// </summary>
    public void ShowTransformation(Action onComplete)
    {
        onCompleteCallback = onComplete;

        FleshTransformationManager mgr = FleshTransformationManager.Instance;
        if (mgr == null || !mgr.IsTransformationPending)
        {
            Debug.LogWarning("[PostCombatTransitionUI] ShowTransformation bypassed: Transformation not pending.");
            Action earlyCb = onCompleteCallback;
            onCompleteCallback = null;
            earlyCb?.Invoke();
            return;
        }

        if (panelRoot != null) panelRoot.SetActive(true);
        if (mgr != null && mgr.characterState != null)
        {
            BodySizeType curSize = mgr.characterState.CurrentBodySize;
            BodySizeType nextSize = mgr.GetNextBodySize(curSize);

            if (titleText != null)
            {
                titleText.text = "<color=#FF66CC>FLESH METAMORPHOSIS</color>";
            }

            if (subtitleText != null)
            {
                subtitleText.text = "DEMONIC ESSENCE THRESHOLD EXCEEDED (200 ml)";
            }

            if (progressionBadgeText != null)
            {
                progressionBadgeText.text = $"<color=#88AAEE>[ {curSize.ToString().ToUpper()} ]</color>   ────▶   <color=#FF77DD>[ {nextSize.ToString().ToUpper()} ]</color>";
            }

            if (narrativeText != null)
            {
                narrativeText.text = 
                    "The concentrated demonic essence courses violently through your vessels and core!\n\n" +
                    "A scorching rush of supernatural heat centers directly in your chest. Tissues swell, soften, and expand under the fluid influx.\n\n" +
                    $"<color=#FF99EE>Your breasts pulse with feverish vitality (+1 Size: {curSize} → {nextSize})!</color>";
            }

            if (pranaSlider != null)
            {
                pranaSlider.minValue = 0;
                pranaSlider.maxValue = mgr.thresholdMl;
                pranaSlider.value = mgr.currentPranaMl;
            }

            if (pranaSliderLabel != null)
            {
                pranaSliderLabel.text = $"SEMEN RESERVOIR OVERFLOW: {mgr.currentPranaMl} / {mgr.thresholdMl} ml";
            }
        }

        if (proceedButton != null)
        {
            proceedButton.onClick.RemoveAllListeners();
            proceedButton.onClick.AddListener(HandleProceedClicked);
        }

        if (proceedBtnText != null)
        {
            proceedBtnText.text = "STABILIZE FLESH & PROCEED";
        }
    }

    private void HandleProceedClicked()
    {
        FleshTransformationManager mgr = FleshTransformationManager.Instance;
        if (mgr != null)
        {
            mgr.ExecuteTransformation(out BodySizeType oldS, out BodySizeType newS);
            Debug.Log($"[PostCombatTransitionUI] Applied body transformation: {oldS} -> {newS}");
        }

        if (panelRoot != null) panelRoot.SetActive(false);

        Action cb = onCompleteCallback;
        onCompleteCallback = null;
        cb?.Invoke();
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    /// <summary>
    /// Procedurally creates the PostCombatTransitionUI within a parent Canvas if not present.
    /// </summary>
    public static PostCombatTransitionUI CreateDynamic(Transform parent)
    {
        GameObject root = new GameObject("PostCombatTransitionPanel", typeof(RectTransform));
        root.transform.SetParent(parent, false);

        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Canvas c = root.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 300;
        root.AddComponent<GraphicRaycaster>();

        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.03f, 0.07f, 0.96f);

        PostCombatTransitionUI ui = root.AddComponent<PostCombatTransitionUI>();
        ui.panelRoot = root;

        // Container Card
        GameObject card = new GameObject("TransitionCard", typeof(RectTransform));
        card.transform.SetParent(root.transform, false);
        RectTransform crt = card.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(720f, 540f);

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.09f, 0.08f, 0.14f, 0.98f);

        // Title
        GameObject tObj = new GameObject("TitleText", typeof(RectTransform));
        tObj.transform.SetParent(card.transform, false);
        RectTransform trt = tObj.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -24f);
        trt.sizeDelta = new Vector2(-40f, 44f);
        ui.titleText = tObj.AddComponent<TextMeshProUGUI>();
        ui.titleText.fontSize = 28;
        ui.titleText.fontStyle = FontStyles.Bold;
        ui.titleText.alignment = TextAlignmentOptions.Center;
        ui.titleText.color = new Color(1f, 0.45f, 0.8f, 1f);

        // Subtitle
        GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform));
        subObj.transform.SetParent(card.transform, false);
        RectTransform srt = subObj.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 1f);
        srt.anchorMax = new Vector2(1f, 1f);
        srt.pivot = new Vector2(0.5f, 1f);
        srt.anchoredPosition = new Vector2(0f, -70f);
        srt.sizeDelta = new Vector2(-40f, 26f);
        ui.subtitleText = subObj.AddComponent<TextMeshProUGUI>();
        ui.subtitleText.fontSize = 15;
        ui.subtitleText.alignment = TextAlignmentOptions.Center;
        ui.subtitleText.color = new Color(0.7f, 0.72f, 0.85f, 1f);

        // Progression Badge
        GameObject badgObj = new GameObject("BadgeText", typeof(RectTransform));
        badgObj.transform.SetParent(card.transform, false);
        RectTransform brt = badgObj.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 1f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.pivot = new Vector2(0.5f, 1f);
        brt.anchoredPosition = new Vector2(0f, -112f);
        brt.sizeDelta = new Vector2(-40f, 36f);
        ui.progressionBadgeText = badgObj.AddComponent<TextMeshProUGUI>();
        ui.progressionBadgeText.fontSize = 20;
        ui.progressionBadgeText.fontStyle = FontStyles.Bold;
        ui.progressionBadgeText.alignment = TextAlignmentOptions.Center;

        // Prana Slider Bar
        GameObject sldObj = new GameObject("PranaSlider", typeof(RectTransform));
        sldObj.transform.SetParent(card.transform, false);
        RectTransform slrt = sldObj.GetComponent<RectTransform>();
        slrt.anchorMin = new Vector2(0.5f, 1f);
        slrt.anchorMax = new Vector2(0.5f, 1f);
        slrt.pivot = new Vector2(0.5f, 1f);
        slrt.anchoredPosition = new Vector2(0f, -160f);
        slrt.sizeDelta = new Vector2(560f, 24f);

        Image slBg = sldObj.AddComponent<Image>();
        slBg.color = new Color(0.15f, 0.12f, 0.22f, 1f);

        Slider sld = sldObj.AddComponent<Slider>();
        ui.pranaSlider = sld;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sldObj.transform, false);
        RectTransform fart = fillArea.GetComponent<RectTransform>();
        fart.anchorMin = Vector2.zero;
        fart.anchorMax = Vector2.one;
        fart.sizeDelta = Vector2.zero;

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform));
        fillObj.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        ui.pranaSliderFill = fillObj.AddComponent<Image>();
        ui.pranaSliderFill.color = new Color(0.95f, 0.35f, 0.75f, 1f);
        sld.fillRect = fillRt;

        // Slider Label
        GameObject slLblObj = new GameObject("PranaLabel", typeof(RectTransform));
        slLblObj.transform.SetParent(sldObj.transform, false);
        RectTransform sllRt = slLblObj.GetComponent<RectTransform>();
        sllRt.anchorMin = Vector2.zero;
        sllRt.anchorMax = Vector2.one;
        sllRt.sizeDelta = Vector2.zero;
        ui.pranaSliderLabel = slLblObj.AddComponent<TextMeshProUGUI>();
        ui.pranaSliderLabel.fontSize = 13;
        ui.pranaSliderLabel.fontStyle = FontStyles.Bold;
        ui.pranaSliderLabel.alignment = TextAlignmentOptions.Center;
        ui.pranaSliderLabel.color = Color.white;

        // Narrative Box
        GameObject narrObj = new GameObject("NarrativeText", typeof(RectTransform));
        narrObj.transform.SetParent(card.transform, false);
        RectTransform nrt = narrObj.GetComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0.5f, 0.5f);
        nrt.anchorMax = new Vector2(0.5f, 0.5f);
        nrt.pivot = new Vector2(0.5f, 0.5f);
        nrt.anchoredPosition = new Vector2(0f, -25f);
        nrt.sizeDelta = new Vector2(580f, 180f);
        ui.narrativeText = narrObj.AddComponent<TextMeshProUGUI>();
        ui.narrativeText.fontSize = 16;
        ui.narrativeText.alignment = TextAlignmentOptions.Center;
        ui.narrativeText.color = new Color(0.90f, 0.88f, 0.95f, 1f);

        // Proceed Button
        GameObject pBtnObj = new GameObject("ProceedButton", typeof(RectTransform));
        pBtnObj.transform.SetParent(card.transform, false);
        RectTransform pbrt = pBtnObj.GetComponent<RectTransform>();
        pbrt.anchorMin = new Vector2(0.5f, 0f);
        pbrt.anchorMax = new Vector2(0.5f, 0f);
        pbrt.pivot = new Vector2(0.5f, 0f);
        pbrt.anchoredPosition = new Vector2(0f, 30f);
        pbrt.sizeDelta = new Vector2(360f, 52f);

        Image pbg = pBtnObj.AddComponent<Image>();
        pbg.color = new Color(0.75f, 0.25f, 0.65f, 1f);

        ui.proceedButton = pBtnObj.AddComponent<Button>();
        ui.proceedButton.targetGraphic = pbg;

        GameObject ptxtObj = new GameObject("Text", typeof(RectTransform));
        ptxtObj.transform.SetParent(pBtnObj.transform, false);
        RectTransform ptrt = ptxtObj.GetComponent<RectTransform>();
        ptrt.anchorMin = Vector2.zero;
        ptrt.anchorMax = Vector2.one;
        ptrt.sizeDelta = Vector2.zero;
        ui.proceedBtnText = ptxtObj.AddComponent<TextMeshProUGUI>();
        ui.proceedBtnText.fontSize = 18;
        ui.proceedBtnText.fontStyle = FontStyles.Bold;
        ui.proceedBtnText.alignment = TextAlignmentOptions.Center;
        ui.proceedBtnText.color = Color.white;

        root.SetActive(false);
        return ui;
    }
}
