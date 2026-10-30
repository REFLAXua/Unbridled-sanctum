#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UnbridledSanctum.Editor
{
    public static class TextureSettingsTool
    {
        [MenuItem("Tools/Sanctum/Set Enemy Sprites Uncompressed (TrueColor RGBA32)")]
        public static void SetEnemySpritesUncompressed()
        {
            string[] searchFolders = new string[]
            {
                "Assets/sprites/Characters/Slime0",
                "Assets/sprites/Characters/Succub0"
            };

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", searchFolders);
            int count = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.anisoLevel = 16;
                    importer.maxTextureSize = 2048;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;

                    // Default platform
                    TextureImporterPlatformSettings defaultSettings = importer.GetDefaultPlatformTextureSettings();
                    defaultSettings.textureCompression = TextureImporterCompression.Uncompressed;
                    defaultSettings.format = TextureImporterFormat.RGBA32;
                    defaultSettings.compressionQuality = 100;
                    defaultSettings.maxTextureSize = 2048;
                    defaultSettings.overridden = true;
                    importer.SetPlatformTextureSettings(defaultSettings);

                    // Standalone platform
                    TextureImporterPlatformSettings standaloneSettings = importer.GetPlatformTextureSettings("Standalone");
                    standaloneSettings.textureCompression = TextureImporterCompression.Uncompressed;
                    standaloneSettings.format = TextureImporterFormat.RGBA32;
                    standaloneSettings.compressionQuality = 100;
                    standaloneSettings.maxTextureSize = 2048;
                    standaloneSettings.overridden = true;
                    importer.SetPlatformTextureSettings(standaloneSettings);

                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                    count++;
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[TextureSettingsTool] Successfully reimported {count} enemy sprites with TrueColor RGBA32 (Uncompressed, Aniso 16, Bilinear).");
        }
    }
}
#endif
