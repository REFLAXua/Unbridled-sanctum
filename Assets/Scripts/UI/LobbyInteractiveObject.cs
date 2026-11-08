using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UnbridledSanctum.UI
{
    /// <summary>
    /// Interactive layer object for the Sanctuary Hub cave scene.
    /// Handles pixel-accurate hit testing (alpha >= 0.1), mouse hover outline highlighting via Custom/UI/SpriteOutline shader,
    /// and click events.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class LobbyInteractiveObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Target Visual")]
        public Image targetImage;

        [Header("Outline Configuration")]
        public Material outlineMaterial;
        public float outlineWidth = 2.5f;
        public Color outlineColor = new Color(0.85f, 0.88f, 0.92f, 1f);

        [Header("Interactive State")]
        public bool isInteractable = true;

        public event Action OnClick;

        private Material _defaultMaterial;
        private Material _instancedOutlineMaterial;
        private bool _isHovered;

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

            // Exit is a large archway in 1080p, give it a thicker outline by default if untouched
            if (gameObject.name.ToLowerInvariant().Contains("exit") && Mathf.Approximately(outlineWidth, 2.5f))
            {
                outlineWidth = 6.0f;
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
                _instancedOutlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
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
                    _instancedOutlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
                    _instancedOutlineMaterial.SetFloat("_AlphaThreshold", 0.05f);
                }
            }
        }

        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;
            if (!interactable && _isHovered)
            {
                SetOutlineActive(false);
                _isHovered = false;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInteractable) return;
            _isHovered = true;
            SetOutlineActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            SetOutlineActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;

            OnClick?.Invoke();
        }

        public void SetOutlineActive(bool active)
        {
            if (targetImage == null) return;

            if (active && isInteractable)
            {
                EnsureOutlineMaterial();
                if (_instancedOutlineMaterial != null)
                {
                    _instancedOutlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
                    _instancedOutlineMaterial.SetColor("_OutlineColor", outlineColor);
                    targetImage.material = _instancedOutlineMaterial;
                }
            }
            else
            {
                targetImage.material = _defaultMaterial;
            }
        }

        private void OnDisable()
        {
            _isHovered = false;
            SetOutlineActive(false);
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
