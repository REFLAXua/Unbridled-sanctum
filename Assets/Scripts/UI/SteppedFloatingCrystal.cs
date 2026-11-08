using UnityEngine;

namespace UnbridledSanctum.UI
{
    /// <summary>
    /// Implements stepped (anime-style 8 FPS) floating levitation along a sine wave.
    /// Anchors to a fixed basePosition to completely eliminate float precision drift over time.
    /// Independent of monitor refresh rate by quantizing unscaled time into discrete FPS steps.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SteppedFloatingCrystal : MonoBehaviour
    {
        [Header("Animation Tuning")]
        [Tooltip("Quantized animation framerate (standard limited-animation anime cadence = 8 FPS)")]
        [Range(1f, 30f)]
        public float targetFps = 8f;

        [Tooltip("Peak vertical displacement amplitude in canvas pixels")]
        public float amplitude = 12f;

        [Tooltip("Full floating sine cycles per second")]
        public float frequency = 0.5f;

        [Header("Runtime State (Read Only)")]
        [SerializeField] private Vector2 basePosition;
        [SerializeField] private bool hasCapturedBasePosition;

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            CaptureBasePosition();
        }

        private void OnEnable()
        {
            CaptureBasePosition();
        }

#if UNITY_EDITOR
        private void Reset()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform != null)
            {
                basePosition = _rectTransform.anchoredPosition;
                hasCapturedBasePosition = true;
            }
        }
#endif

        /// <summary>
        /// Captures the initial anchored position as the absolute anchor point for all sine offsets.
        /// </summary>
        public void CaptureBasePosition()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform != null && !hasCapturedBasePosition)
            {
                basePosition = _rectTransform.anchoredPosition;
                hasCapturedBasePosition = true;
            }
        }

        /// <summary>
        /// Explicitly redefines the base resting position if repositioned by UI layout.
        /// </summary>
        public void SetBasePosition(Vector2 pos)
        {
            basePosition = pos;
            hasCapturedBasePosition = true;
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = pos;
            }
        }

        public Vector2 GetBasePosition() => basePosition;

        private void Update()
        {
            if (_rectTransform == null) return;

            // 1. Quantize time to discrete targetFps steps (independent of display Hz / VSync)
            float effectiveFps = Mathf.Max(1f, targetFps);
            float time = Time.unscaledTime;
            float step = 1f / effectiveFps;
            float quantizedTime = Mathf.Floor(time * effectiveFps) * step;

            // 2. Pure sine oscillation from quantized time
            float yOffset = Mathf.Sin(quantizedTime * frequency * Mathf.PI * 2f) * amplitude;

            // 3. Absolute assignment relative to basePosition: zero float drift guaranteed
            _rectTransform.anchoredPosition = new Vector2(basePosition.x, basePosition.y + yOffset);
        }

        private void OnDisable()
        {
            // Reset to base position when disabled so no awkward mid-cycle pause persists
            if (_rectTransform != null && hasCapturedBasePosition)
            {
                _rectTransform.anchoredPosition = basePosition;
            }
        }
    }
}
