using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnbridledSanctum.Combat;

namespace UnbridledSanctum.UI
{
    public class EnemyCardUI : MonoBehaviour
    {
        public Combatant assignedEnemy;
        public GameObject targetHalo;
        public Image targetHaloImg;
        public GameObject targetAura;
        public Image targetAuraImg;
        public GameObject turnHalo;
        public Image turnHaloImg;
        public GameObject turnAura;
        public Image turnAuraImg;
        public GameObject turnBadge;
        public GameObject telegraphBox;
        public TextMeshProUGUI telegraphText;
        public TextMeshProUGUI nameText;
        public Image portraitImage;
        public RectTransform portraitTransform;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI mpText;
        public TextMeshProUGUI seductionText;
        public Image hitFlashOverlay;
        public Image pinkHazeOverlay;

        [Header("Engagement Status Square Badge (Grapple & Penetration)")]
        public GameObject statusSquareBadge;
        public Image statusSquareBorder;
        public Image statusSquareBg;
        public TextMeshProUGUI statusSquareText;
        public TextMeshProUGUI statusSquareLabel;

        private int lastSeductionPhase = -1;
        private float targetScale = 1f;

        private void Update()
        {
            if (targetAura != null && targetAura.activeSelf)
            {
                float pulse = 0.22f + 0.10f * Mathf.Sin(Time.time * 6f);
                if (targetAuraImg != null) targetAuraImg.color = new Color(1f, 0.85f, 0.25f, pulse);
                if (targetHaloImg != null) targetHaloImg.color = new Color(1f, 0.88f, 0.35f, pulse + 0.20f);
            }

            if (turnAura != null && turnAura.activeSelf)
            {
                float pulse = 0.18f + 0.08f * Mathf.Sin(Time.time * 4f);
                if (turnAuraImg != null) turnAuraImg.color = new Color(1f, 0.58f, 0.20f, pulse);
                if (turnHaloImg != null) turnHaloImg.color = new Color(1f, 0.68f, 0.28f, pulse + 0.18f);
            }

            if (portraitTransform != null)
            {
                portraitTransform.localScale = Vector3.Lerp(portraitTransform.localScale, Vector3.one * targetScale, Time.deltaTime * 12f);
            }
        }

        public void ShowTelegraph(EnemySkill skill)
        {
            if (telegraphBox != null)
            {
                telegraphBox.SetActive(true);
                if (telegraphText != null)
                {
                    string icon = "[ATK]";
                    if (skill.pranaGain > 0) icon = "[UNION]";
                    else if (skill.clothesDamage > 0) icon = "[ACID]";
                    else if (skill.seductionDamage > 0) icon = "[CHARM]";
                    else if (skill.shieldGain > 0) icon = "[GUARD]";

                    telegraphText.text = $"{icon} {skill.skillName}";
                }
            }
        }

        public void HideTelegraph()
        {
            if (telegraphBox != null)
            {
                telegraphBox.SetActive(false);
            }
        }

        public void Refresh(Combatant enemy, bool isSelected, bool isTargetingMode = false, bool isActiveTurn = false, bool anyEnemyTurnActive = false)
        {
            if (enemy == null) return;
            assignedEnemy = enemy;

            bool showTargetHighlight = isSelected && !enemy.isDefeated && isTargetingMode;
            if (targetHalo != null) targetHalo.SetActive(showTargetHighlight);
            if (targetAura != null) targetAura.SetActive(showTargetHighlight);

            bool showTurnHighlight = isActiveTurn && !enemy.isDefeated;
            if (turnHalo != null) turnHalo.SetActive(showTurnHighlight);
            if (turnAura != null) turnAura.SetActive(showTurnHighlight);
            if (turnBadge != null) turnBadge.SetActive(showTurnHighlight);

            if (showTargetHighlight)
            {
                targetScale = 1.08f;
            }
            else if (showTurnHighlight)
            {
                targetScale = 1.03f;
            }
            else
            {
                targetScale = 1.0f;
            }

            if (nameText != null)
            {
                if (enemy.isDefeated)
                {
                    nameText.text = enemy.name;
                    nameText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }
                else if (showTargetHighlight)
                {
                    nameText.text = enemy.name;
                    nameText.color = new Color(1f, 0.95f, 0.5f, 1f);
                }
                else if (showTurnHighlight)
                {
                    nameText.text = $"> {enemy.name}";
                    nameText.color = new Color(1f, 0.80f, 0.35f, 1f);
                }
                else
                {
                    nameText.text = enemy.name;
                    nameText.color = Color.white;
                }
            }

            // Sprite phase and dimming
            if (portraitImage != null && EnemySpriteDatabase.Instance != null)
            {
                Sprite sp = EnemySpriteDatabase.Instance.GetCombatSprite(enemy.enemyType, enemy.seductionStacks, enemy.isDefeated);
                if (sp != null)
                {
                    portraitImage.sprite = sp;
                    portraitImage.enabled = true;
                }

                if (enemy.isDefeated)
                {
                    portraitImage.color = new Color(0.35f, 0.35f, 0.40f, 0.65f);
                }
                else if (isTargetingMode)
                {
                    // Emphasize the target, dim non-targets
                    portraitImage.color = isSelected ? Color.white : new Color(0.40f, 0.42f, 0.48f, 0.65f);
                }
                else if (anyEnemyTurnActive)
                {
                    // When an enemy is taking their turn, acting enemy stays bright, others gently stepped back
                    portraitImage.color = showTurnHighlight ? Color.white : new Color(0.72f, 0.74f, 0.80f, 0.85f);
                }
                else
                {
                    portraitImage.color = Color.white;
                }

                if (lastSeductionPhase != -1 && lastSeductionPhase != enemy.seductionStacks && !enemy.isDefeated)
                {
                    PlayPhaseTransition(enemy.seductionStacks);
                }
                lastSeductionPhase = enemy.seductionStacks;
            }

            if (hpText != null)
            {
                if (enemy.isDefeated)
                {
                    hpText.text = enemy.defeatedBySeduction ? "[SUBMITTED]" : "[FLED]";
                    hpText.color = enemy.defeatedBySeduction ? new Color(1f, 0.4f, 0.7f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f);
                }
                else
                {
                    hpText.text = $"{enemy.currentHp}/{enemy.maxHp} HP";
                }
            }

            if (mpText != null)
            {
                mpText.gameObject.SetActive(false);
            }

            if (seductionText != null)
            {
                if (enemy.isDefeated)
                {
                    seductionText.text = "";
                }
                else
                {
                    string stacks = "";
                    int maxStacks = Mathf.Max(1, enemy.maxSeductionStacks);
                    for (int s = 0; s < maxStacks; s++)
                    {
                        stacks += (s < enemy.seductionStacks) ? "[X]" : "[ ]";
                    }
                    seductionText.text = $"{enemy.seduction}% " + stacks;
                }
            }

            // Engagement Status Square Badge (Strictly two statuses: Grapple & Penetration)
            if (statusSquareBadge != null)
            {
                if (enemy.isDefeated)
                {
                    statusSquareBadge.SetActive(false);
                }
                else if (enemy.occupiedSlot != PenetrationSlot.None)
                {
                    statusSquareBadge.SetActive(true);
                    if (statusSquareBorder != null) statusSquareBorder.color = new Color(1f, 0.25f, 0.70f, 1f); // Radiant Hot Pink
                    if (statusSquareBg != null) statusSquareBg.color = new Color(0.24f, 0.05f, 0.20f, 0.96f);
                    if (statusSquareText != null)
                    {
                        statusSquareText.text = "P";
                        statusSquareText.color = new Color(1f, 0.45f, 0.85f, 1f);
                    }
                    if (statusSquareLabel != null)
                    {
                        string slotTag = enemy.occupiedSlot switch
                        {
                            PenetrationSlot.Mouth => "MOUTH",
                            PenetrationSlot.Vagina => "VAGINA",
                            PenetrationSlot.Anal => "ANAL",
                            _ => "PENETRATION"
                        };
                        statusSquareLabel.text = $"PENETRATION\n[{slotTag}]";
                        statusSquareLabel.color = new Color(1f, 0.45f, 0.85f, 1f);
                    }
                }
                else if (enemy.isGrapplingPlayer)
                {
                    statusSquareBadge.SetActive(true);
                    if (statusSquareBorder != null) statusSquareBorder.color = new Color(1f, 0.55f, 0.15f, 1f); // Intense Amber
                    if (statusSquareBg != null) statusSquareBg.color = new Color(0.20f, 0.10f, 0.04f, 0.96f);
                    if (statusSquareText != null)
                    {
                        statusSquareText.text = "G";
                        statusSquareText.color = new Color(1f, 0.75f, 0.30f, 1f);
                    }
                    if (statusSquareLabel != null)
                    {
                        statusSquareLabel.text = "GRAPPLE";
                        statusSquareLabel.color = new Color(1f, 0.75f, 0.30f, 1f);
                    }
                }
                else
                {
                    statusSquareBadge.SetActive(false);
                }
            }
        }

        public void PlayHitReaction(int damage, int absorbed)
        {
            RectTransform cardRt = GetComponent<RectTransform>();
            if (absorbed > 0 && FloatingCombatText.Instance != null && cardRt != null)
            {
                FloatingCombatText.Instance.Spawn(cardRt, $"-{absorbed} Block", CombatTextType.ShieldBlock, new Vector2(0f, 160f));
            }

            if (damage > 0)
            {
                StopCoroutine("ShakeAndFlashRoutine");
                StartCoroutine(ShakeAndFlashRoutine());

                if (FloatingCombatText.Instance != null && cardRt != null)
                {
                    FloatingCombatText.Instance.Spawn(cardRt, $"-{damage}", CombatTextType.Damage, new Vector2(0f, 160f));
                }
            }
        }

        private System.Collections.IEnumerator ShakeAndFlashRoutine()
        {
            RectTransform rt = portraitTransform != null ? portraitTransform : GetComponent<RectTransform>();
            if (rt == null) yield break;

            Vector2 origAnchoredPos = rt.anchoredPosition;

            if (hitFlashOverlay != null)
            {
                hitFlashOverlay.color = new Color(1f, 0.12f, 0.12f, 0.50f);
            }

            float dur = 0.25f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float damp = 1f - (elapsed / dur);
                float x = UnityEngine.Random.Range(-10f, 10f) * damp;
                float y = UnityEngine.Random.Range(-6f, 6f) * damp;
                rt.anchoredPosition = origAnchoredPos + new Vector2(x, y);

                if (hitFlashOverlay != null)
                {
                    hitFlashOverlay.color = new Color(1f, 0.12f, 0.12f, 0.50f * damp);
                }
                yield return null;
            }

            rt.anchoredPosition = origAnchoredPos;
            if (hitFlashOverlay != null)
            {
                hitFlashOverlay.color = new Color(1f, 0.12f, 0.12f, 0f);
            }
        }

        public void PlaySeductionReaction(int amount)
        {
            RectTransform cardRt = GetComponent<RectTransform>();
            if (FloatingCombatText.Instance != null && cardRt != null)
            {
                FloatingCombatText.Instance.Spawn(cardRt, $"+{amount}% Lust", CombatTextType.Seduction, new Vector2(0f, 160f));
            }
        }

        public void PlayPhaseTransition(int newPhase)
        {
            StopCoroutine("PhaseTransitionRoutine");
            StartCoroutine(PhaseTransitionRoutine(newPhase));
        }

        private System.Collections.IEnumerator PhaseTransitionRoutine(int newPhase)
        {
            if (pinkHazeOverlay != null)
            {
                float fadeIn = 0.35f;
                float elapsed = 0f;
                while (elapsed < fadeIn)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(0f, 0.75f, elapsed / fadeIn);
                    pinkHazeOverlay.color = new Color(1f, 0.35f, 0.85f, alpha);
                    yield return null;
                }

                if (assignedEnemy != null && EnemySpriteDatabase.Instance != null && portraitImage != null)
                {
                    Sprite sp = EnemySpriteDatabase.Instance.GetCombatSprite(assignedEnemy.enemyType, newPhase, assignedEnemy.isDefeated);
                    if (sp != null)
                    {
                        portraitImage.sprite = sp;
                        portraitImage.enabled = true;
                    }
                }

                float fadeOut = 0.45f;
                elapsed = 0f;
                while (elapsed < fadeOut)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(0.75f, 0f, elapsed / fadeOut);
                    pinkHazeOverlay.color = new Color(1f, 0.35f, 0.85f, alpha);
                    yield return null;
                }
                pinkHazeOverlay.color = new Color(1f, 0.35f, 0.85f, 0f);
            }
            else
            {
                if (assignedEnemy != null && EnemySpriteDatabase.Instance != null && portraitImage != null)
                {
                    Sprite sp = EnemySpriteDatabase.Instance.GetCombatSprite(assignedEnemy.enemyType, newPhase, assignedEnemy.isDefeated);
                    if (sp != null)
                    {
                        portraitImage.sprite = sp;
                        portraitImage.enabled = true;
                    }
                }
            }
        }

        private System.Collections.IEnumerator PhasePunchAnimation()
        {
            if (portraitTransform == null) yield break;
            Vector3 origScale = Vector3.one;
            float elapsed = 0f;
            float dur = 0.35f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dur;
                float scale = 1f + 0.16f * Mathf.Sin(t * Mathf.PI);
                portraitTransform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            portraitTransform.localScale = origScale;
        }
    }
}
