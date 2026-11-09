using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UnbridledSanctum.UI
{
    /// <summary>
    /// Interactive layer object for the Sanctuary Hub cave scene.
    /// Handles pixel-accurate hit testing (alpha >= 0.1), mouse hover outline/glow highlighting via Custom/UI/SpriteOutline shader,
    /// dynamic tooltips, and click events.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class LobbyInteractiveObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public enum HighlightType
        {
            OutlineShader,
            SunlightGlow,
            Both,
            None
        }

        [Header("Target Visual")]
        public Image targetImage;

        [Header("Highlight Mode")]
        public HighlightType highlightType = HighlightType.OutlineShader;

        [Header("Outline Configuration")]
        public Material outlineMaterial;
        public float outlineWidth = 2.5f;
        public Color outlineColor = new Color(0.85f, 0.88f, 0.92f, 1f);

        [Header("Sunlight Glow Configuration")]
        public float hoverBrightness = 1.10f;
        public Color glowColor = new Color(1.0f, 0.96f, 0.85f, 0.10f);
        public float glowTransitionSpeed = 8f;

        [Header("Interactive State")]
        public bool isInteractable = true;

        public event Action OnClick;

        private Material _defaultMaterial;
        private Material _instancedOutlineMaterial;
        private bool _isHovered;

        private float _currentBrightness = 1.0f;
        private float _currentGlowA = 0.0f;
        private float _currentOutlineWidth = 0.0f;

        private void Awake()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            if (targetImage != null)
            {
                // Enforce pixel-perfect alpha raycasting threshold
                targetImage.alphaHitTestMinimumThreshold = 0.1f;
                targetImage.raycastTarget = true;

                // If user accidentally assigned the outline material directly to the Image in Inspector, reset default material to null
                if (targetImage.material != null && targetImage.material.shader != null && targetImage.material.shader.name == "Custom/UI/SpriteOutline")
                {
                    targetImage.material = null;
                }
                _defaultMaterial = targetImage.material;
            }

            EnsureOutlineMaterial();
        }

#if UNITY_EDITOR
        private void Reset()
        {
            if (targetImage == null) targetImage = GetComponent<Image>();
            if (targetImage != null)
            {
                targetImage.alphaHitTestMinimumThreshold = 0.1f;
                targetImage.raycastTarget = true;
            }
            if (outlineMaterial == null)
            {
                outlineMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/UISpriteOutlineMat.mat");
            }
        }

        private void OnValidate()
        {
            if (targetImage == null) targetImage = GetComponent<Image>();
            if (targetImage != null)
            {
                if (targetImage.alphaHitTestMinimumThreshold < 0.05f)
                {
                    targetImage.alphaHitTestMinimumThreshold = 0.1f;
                }
            }
            if (outlineMaterial == null)
            {
                outlineMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/UISpriteOutlineMat.mat");
            }

            // Exit defaults to SunlightGlow with no outline line
            if (gameObject.name.ToLowerInvariant().Contains("exit"))
            {
                if (highlightType == HighlightType.OutlineShader)
                {
                    highlightType = HighlightType.SunlightGlow;
                }
                outlineWidth = 0f;
            }
        }
#endif

        private void EnsureOutlineMaterial()
        {
            if (_instancedOutlineMaterial != null) return;

            if (outlineMaterial != null)
            {
                _instancedOutlineMaterial = new Material(outlineMaterial);
                _instancedOutlineMaterial.name = $"{gameObject.name}_OutlineMat";
                _instancedOutlineMaterial.SetColor("_OutlineColor", outlineColor);
                _instancedOutlineMaterial.SetFloat("_OutlineWidth", 0f);
                _instancedOutlineMaterial.SetFloat("_HoverBrightness", 1.0f);
                _instancedOutlineMaterial.SetColor("_GlowColor", new Color(glowColor.r, glowColor.g, glowColor.b, 0f));
                _instancedOutlineMaterial.SetFloat("_AlphaThreshold", 0.05f);
            }
            else
            {
                Shader outlineShader = Shader.Find("Custom/UI/SpriteOutline");
                if (outlineShader != null)
                {
                    _instancedOutlineMaterial = new Material(outlineShader);
                    _instancedOutlineMaterial.name = $"{gameObject.name}_OutlineMat";
                    _instancedOutlineMaterial.SetColor("_OutlineColor", outlineColor);
                    _instancedOutlineMaterial.SetFloat("_OutlineWidth", 0f);
                    _instancedOutlineMaterial.SetFloat("_HoverBrightness", 1.0f);
                    _instancedOutlineMaterial.SetColor("_GlowColor", new Color(glowColor.r, glowColor.g, glowColor.b, 0f));
                    _instancedOutlineMaterial.SetFloat("_AlphaThreshold", 0.05f);
                }
            }
        }

        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;
            if (!interactable && _isHovered)
            {
                _isHovered = false;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInteractable) return;
            _isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;

            OnClick?.Invoke();
        }

        private void Update()
        {
            if (targetImage == null) return;

            // Determine target parameters based on hover and highlightType
            float targetWidth = 0f;
            float targetBright = 1.0f;
            float targetGlowA = 0f;

            if (_isHovered && isInteractable)
            {
                if (highlightType == HighlightType.OutlineShader || highlightType == HighlightType.Both)
                {
                    targetWidth = outlineWidth;
                }

                if (highlightType == HighlightType.SunlightGlow || highlightType == HighlightType.Both)
                {
                    targetBright = hoverBrightness;
                    targetGlowA = glowColor.a;
                }
            }

            // Smooth interpolation
            float dt = Time.unscaledDeltaTime * glowTransitionSpeed;
            _currentOutlineWidth = Mathf.MoveTowards(_currentOutlineWidth, targetWidth, dt * 10f);
            _currentBrightness = Mathf.MoveTowards(_currentBrightness, targetBright, dt * 2f);
            _currentGlowA = Mathf.MoveTowards(_currentGlowA, targetGlowA, dt);

            bool needsCustomMaterial = _currentOutlineWidth > 0.01f || Mathf.Abs(_currentBrightness - 1.0f) > 0.01f || _currentGlowA > 0.005f;

            if (needsCustomMaterial)
            {
                EnsureOutlineMaterial();
                if (_instancedOutlineMaterial != null)
                {
                    _instancedOutlineMaterial.SetFloat("_OutlineWidth", _currentOutlineWidth);
                    _instancedOutlineMaterial.SetColor("_OutlineColor", outlineColor);
                    _instancedOutlineMaterial.SetFloat("_HoverBrightness", _currentBrightness);
                    _instancedOutlineMaterial.SetColor("_GlowColor", new Color(glowColor.r, glowColor.g, glowColor.b, _currentGlowA));

                    if (targetImage.material != _instancedOutlineMaterial)
                    {
                        targetImage.material = _instancedOutlineMaterial;
                    }
                }
            }
            else
            {
                if (targetImage.material != _defaultMaterial)
                {
                    targetImage.material = _defaultMaterial;
                }
            }
        }

        private void OnDisable()
        {
            _isHovered = false;
            _currentBrightness = 1.0f;
            _currentGlowA = 0f;
            _currentOutlineWidth = 0f;
            if (targetImage != null) targetImage.material = _defaultMaterial;
        }

        private void OnDestroy()
        {
            if (_instancedOutlineMaterial != null)
            {
                Destroy(_instancedOutlineMaterial);
                _instancedOutlineMaterial = null;
            }
        }
    }
}
