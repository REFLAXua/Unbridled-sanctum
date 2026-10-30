using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject storing pools of hair, emotion, blush, and color options for the Character Creator.
/// </summary>
[CreateAssetMenu(fileName = "CharacterAppearanceDatabase", menuName = "Game/Appearance Database")]
public class CharacterAppearanceDatabase : ScriptableObject
{
    [Header("Hair Styles")]
    public List<Sprite> hairFrontStyles = new List<Sprite>();
    public List<Sprite> hairBackStyles = new List<Sprite>();

    [Header("Emotion Pool (Delighted 2, Normal, Sleepy, smug)")]
    public List<Sprite> emotionStyles = new List<Sprite>();

    [Header("Blush Options (null = None, then 1, 2)")]
    public List<Sprite> blushStyles = new List<Sprite>();

    [Header("Preset Palettes")]
    public Color[] hairColorPresets = new Color[]
    {
        new Color(1.00f, 1.00f, 1.00f, 1f), // Pure Silver (Original Default)
        new Color(0.15f, 0.12f, 0.10f, 1f), // Black / Dark Brown
        new Color(0.40f, 0.25f, 0.15f, 1f), // Chestnut Brown
        new Color(0.85f, 0.70f, 0.40f, 1f), // Blonde
        new Color(0.75f, 0.30f, 0.15f, 1f), // Ginger / Red
        new Color(0.90f, 0.55f, 0.70f, 1f), // Sakura Pink
        new Color(0.30f, 0.45f, 0.85f, 1f), // Sapphire Blue
        new Color(0.45f, 0.25f, 0.55f, 1f)  // Violet
    };

    public Color[] eyeColorPresets = new Color[]
    {
        new Color(0.95f, 0.40f, 0.80f, 1f), // Magenta / Original
        new Color(0.20f, 0.55f, 0.95f, 1f), // Ocean Blue
        new Color(0.20f, 0.80f, 0.50f, 1f), // Emerald Green
        new Color(0.60f, 0.35f, 0.15f, 1f), // Amber / Hazel
        new Color(0.85f, 0.20f, 0.20f, 1f), // Ruby Red
        new Color(0.65f, 0.30f, 0.85f, 1f), // Amethyst Purple
        new Color(0.85f, 0.70f, 0.20f, 1f), // Golden Yellow
        new Color(0.50f, 0.55f, 0.60f, 1f)  // Slate Grey
    };
}
