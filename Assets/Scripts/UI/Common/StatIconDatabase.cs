using System;
using UnityEngine;

namespace UnbridledSanctum.UI
{
    /// <summary>
    /// Loads and provides cached sprites for player and combat stats
    /// (HP, Armor/Clothes, Shield/Block, Lust/Seduction, AP, Mana).
    /// Safe for both Editor and Standalone runtime.
    /// </summary>
    public static class StatIconDatabase
    {
        private static Sprite _hp;
        private static Sprite _armor;
        private static Sprite _topArmor;
        private static Sprite _bottomArmor;
        private static Sprite _block;
        private static Sprite _lust;
        private static Sprite _ap;
        private static Sprite _mana;
        private static Sprite _corruption;
        private static Sprite _lumiere;
        private static Sprite _lustid;

        public static Sprite HpIcon => _hp != null ? _hp : (_hp = LoadIcon("Icon_hp.png"));
        public static Sprite ArmorIcon => _armor != null ? _armor : (_armor = LoadIcon("Icon_arrnor.png"));
        public static Sprite TopArmorIcon => _topArmor != null ? _topArmor : (_topArmor = LoadIcon("Icon_top.png"));
        public static Sprite BottomArmorIcon => _bottomArmor != null ? _bottomArmor : (_bottomArmor = LoadIcon("Icon_bottom.png"));
        public static Sprite BlockIcon => _block != null ? _block : (_block = LoadIcon("Icon_block.png"));
        public static Sprite LustIcon => _lust != null ? _lust : (_lust = LoadIcon("Icon_lust.png"));
        public static Sprite ApIcon => _ap != null ? _ap : (_ap = LoadIcon("Icon_ap.png"));
        public static Sprite ManaIcon => _mana != null ? _mana : (_mana = LoadIcon("Icon_mana.png"));
        public static Sprite CorruptionIcon => _corruption != null ? _corruption : (_corruption = LoadIcon("Icon_corruption.png"));
        public static Sprite LumiereIcon => _lumiere != null ? _lumiere : (_lumiere = LoadIcon("Icon_lumiere.png") ?? CreateLumiereSprite());
        public static Sprite LustidIcon => _lustid != null ? _lustid : (_lustid = LoadIcon("Icon_lustid.png") ?? CreateLustidSprite());

        public static Sprite LoadIcon(string fileName)
        {
#if UNITY_EDITOR
            Sprite editorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/sprites/Icon/{fileName}");
            if (editorSprite != null) return editorSprite;
#endif
            try
            {
                string path = System.IO.Path.Combine(Application.dataPath, "sprites/Icon", fileName);
                if (System.IO.File.Exists(path))
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes))
                    {
                        tex.filterMode = FilterMode.Bilinear;
                        return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[StatIconDatabase] Could not load icon {fileName}: {ex.Message}");
            }
            return null;
        }

        private static Sprite CreateLumiereSprite()
        {
            int size = 36;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color goldCenter = new Color(1f, 0.88f, 0.35f, 1f);
            Color goldOuter = new Color(0.85f, 0.58f, 0.12f, 1f);
            Color goldRim = new Color(0.45f, 0.28f, 0.05f, 0.9f);

            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - half) / half;
                    float dy = Mathf.Abs(y - half) / half;
                    // Diamond crystal shape: |dx| + |dy| <= 1
                    float dist = dx + dy;
                    if (dist > 0.95f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (dist > 0.82f)
                    {
                        tex.SetPixel(x, y, goldRim);
                    }
                    else
                    {
                        Color c = Color.Lerp(goldCenter, goldOuter, dist / 0.82f);
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite CreateLustidSprite()
        {
            int size = 36;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color lustCenter = new Color(1f, 0.45f, 0.85f, 1f);
            Color lustOuter = new Color(0.70f, 0.15f, 0.55f, 1f);
            Color lustRim = new Color(0.35f, 0.05f, 0.30f, 0.9f);

            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - half) / (half * 0.9f);
                    float ny = (y - half) / (half * 0.9f);
                    float r = Mathf.Sqrt(nx * nx + ny * ny);

                    // Hexagonal faceted jewel shape
                    float angle = Mathf.Atan2(ny, nx);
                    float hexR = Mathf.Cos(Mathf.PI / 6f) / Mathf.Cos((Mathf.Abs(angle) % (Mathf.PI / 3f)) - Mathf.PI / 6f);
                    float normDist = r / hexR;

                    if (normDist > 0.98f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (normDist > 0.85f)
                    {
                        tex.SetPixel(x, y, lustRim);
                    }
                    else
                    {
                        Color c = Color.Lerp(lustCenter, lustOuter, normDist / 0.85f);
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}

