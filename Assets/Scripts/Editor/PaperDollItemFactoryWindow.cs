#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace UnbridledSanctum.Editor
{
    /// <summary>
    /// Editor tool to streamline the Paper Doll pipeline:
    /// 1. Configure Sprite 2D UI importer settings in batch (Zero-offset, Alpha is Transparency, TrueColor RGBA32).
    /// 2. Generate and configure ItemData ScriptableObjects with automatic 4-tier body size sprite binding.
    /// Access via: Tools > Sanctum > Paper Doll & Item Factory
    /// </summary>
    public class PaperDollItemFactoryWindow : EditorWindow
    {
        private Vector2 scrollPos;

        [Header("Batch Sprite Configuration")]
        private DefaultAsset targetSpriteFolder;

        [Header("Create New ItemData")]
        private string newItemId = "new_item";
        private string newItemName = "New Outfit";
        [TextArea(2, 4)]
        private string newItemDescription = "Description of this clothing or item.";
        private EquipSlot newEquipSlot = EquipSlot.UpperClothes;
        private int newDurability = 65;
        private int newDamageReduction = 1;
        private bool newHidesBreastLayer = true;
        private Sprite newIcon;

        private Sprite spriteSmall;
        private Sprite spriteMedium;
        private Sprite spriteLarge;
        private Sprite spriteXLarge;

        private DefaultAsset targetItemOutputFolder;

        [MenuItem("Tools/Sanctum/Paper Doll & Item Factory")]
        public static void OpenWindow()
        {
            var window = GetWindow<PaperDollItemFactoryWindow>("Item Factory");
            window.minSize = new Vector2(480, 680);
            window.Show();
        }

        private void OnEnable()
        {
            if (targetItemOutputFolder == null)
            {
                var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Resources/Items");
                if (folder != null) targetItemOutputFolder = folder;
            }
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("🎨 Paper Doll & Item Factory", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Стандарт разрешения холста: 1024×1536 (Full-Canvas Zero-Offset, RGBA32). Подробнее: SPRITE_CREATION_GUIDE.md", MessageType.Info);

            EditorGUILayout.Space(12);
            DrawBatchImporterSection();

            EditorGUILayout.Space(16);
            DrawItemDataGeneratorSection();

            EditorGUILayout.Space(16);
            DrawQuickScanSelectionSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawBatchImporterSection()
        {
            EditorGUILayout.LabelField("1. Пакетная настройка спрайтов (Texture Importer)", EditorStyles.boldLabel);
            targetSpriteFolder = (DefaultAsset)EditorGUILayout.ObjectField("Целевая папка спрайтов", targetSpriteFolder, typeof(DefaultAsset), false);

            if (GUILayout.Button("Применить настройки (Sprite 2D UI, RGBA32, No Mipmaps)", GUILayout.Height(30)))
            {
                if (targetSpriteFolder == null)
                {
                    EditorUtility.DisplayDialog("Ошибка", "Выберите целевую папку со спрайтами!", "OK");
                    return;
                }

                string folderPath = AssetDatabase.GetAssetPath(targetSpriteFolder);
                ConfigureSpritesInFolder(folderPath);
            }
        }

        private void DrawItemDataGeneratorSection()
        {
            EditorGUILayout.LabelField("2. Создание ItemData ScriptableObject", EditorStyles.boldLabel);

            newItemId = EditorGUILayout.TextField("ID предмета (file slug)", newItemId);
            newItemName = EditorGUILayout.TextField("Название предмета", newItemName);
            newItemDescription = EditorGUILayout.TextField("Описание", newItemDescription);

            EditorGUILayout.Space(4);
            newEquipSlot = (EquipSlot)EditorGUILayout.EnumPopup("Слот экипировки", newEquipSlot);
            newDurability = EditorGUILayout.IntField("Прочность (Durability)", newDurability);
            newDamageReduction = EditorGUILayout.IntField("Защита (Armor)", newDamageReduction);
            newHidesBreastLayer = EditorGUILayout.Toggle("Скрывать слой груди (hidesBreastLayer)", newHidesBreastLayer);

            EditorGUILayout.Space(6);
            newIcon = (Sprite)EditorGUILayout.ObjectField("Иконка предмета (Icon)", newIcon, typeof(Sprite), false);

            EditorGUILayout.LabelField("Спрайты по размерам тела (1024×1536):", EditorStyles.miniBoldLabel);
            spriteSmall = (Sprite)EditorGUILayout.ObjectField("Small (0)", spriteSmall, typeof(Sprite), false);
            spriteMedium = (Sprite)EditorGUILayout.ObjectField("Medium (1)", spriteMedium, typeof(Sprite), false);
            spriteLarge = (Sprite)EditorGUILayout.ObjectField("Large (2)", spriteLarge, typeof(Sprite), false);
            spriteXLarge = (Sprite)EditorGUILayout.ObjectField("XLarge (3)", spriteXLarge, typeof(Sprite), false);

            EditorGUILayout.Space(6);
            targetItemOutputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Папка сохранения ItemData", targetItemOutputFolder, typeof(DefaultAsset), false);

            if (GUILayout.Button("✨ Создать ItemData ScriptableObject", GUILayout.Height(34)))
            {
                CreateItemDataAsset();
            }
        }

        private void DrawQuickScanSelectionSection()
        {
            EditorGUILayout.LabelField("3. Автозаполнение из выбранных в Project файлов", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Выделите в окне Project файлы спрайтов (например, dress_small, dress_medium, dress_large, dress_xlarge) и нажмите кнопку ниже.", MessageType.None);

            if (GUILayout.Button("Автозаполнить поля из выделенных файлов", GUILayout.Height(28)))
            {
                AutoFillFromSelection();
            }
        }

        private void AutoFillFromSelection()
        {
            Object[] selected = Selection.objects;
            if (selected == null || selected.Length == 0)
            {
                EditorUtility.DisplayDialog("Внимание", "Сначала выделите спрайты в Project Window!", "OK");
                return;
            }

            foreach (var obj in selected)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (spr == null) continue;

                string lower = spr.name.ToLowerInvariant();

                // Guess ID from prefix if contains _small/_medium/_large/_xlarge
                int lastUnderscore = lower.LastIndexOf('_');
                if (lastUnderscore > 0)
                {
                    newItemId = lower.Substring(0, lastUnderscore);
                    newItemName = ObjectNames.NicifyVariableName(newItemId);
                }

                if (lower.EndsWith("_small") || lower.EndsWith("_0")) spriteSmall = spr;
                else if (lower.EndsWith("_medium") || lower.EndsWith("_1")) spriteMedium = spr;
                else if (lower.EndsWith("_large") || lower.EndsWith("_2")) spriteLarge = spr;
                else if (lower.EndsWith("_xlarge") || lower.EndsWith("_3")) spriteXLarge = spr;
                else if (lower.EndsWith("_icon") || lower.Contains("icon")) newIcon = spr;
                else if (spriteSmall == null) spriteSmall = spr;
            }

            if (newIcon == null && spriteSmall != null)
            {
                newIcon = spriteSmall;
            }
        }

        private void CreateItemDataAsset()
        {
            if (string.IsNullOrWhiteSpace(newItemId))
            {
                EditorUtility.DisplayDialog("Ошибка", "Укажите ID предмета!", "OK");
                return;
            }

            string outFolder = targetItemOutputFolder != null ? AssetDatabase.GetAssetPath(targetItemOutputFolder) : "Assets/Resources/Items";
            if (!Directory.Exists(outFolder))
            {
                Directory.CreateDirectory(outFolder);
            }

            string assetPath = $"{outFolder}/{newItemId}.asset";

            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
            bool isNew = (item == null);
            if (isNew)
            {
                item = CreateInstance<ItemData>();
            }

            item.itemName = newItemName;
            item.description = newItemDescription;
            item.itemType = ItemType.Equipment;
            item.isEquippable = true;
            item.equipSlot = newEquipSlot;
            item.durability = newDurability;
            item.damageReduction = newDamageReduction;
            item.hidesBreastLayer = newHidesBreastLayer;
            item.icon = newIcon;

            List<BodySizeSprite> list = new List<BodySizeSprite>();
            if (spriteSmall != null) list.Add(new BodySizeSprite { bodySize = BodySizeType.Small, sprite = spriteSmall });
            if (spriteMedium != null) list.Add(new BodySizeSprite { bodySize = BodySizeType.Medium, sprite = spriteMedium });
            if (spriteLarge != null) list.Add(new BodySizeSprite { bodySize = BodySizeType.Large, sprite = spriteLarge });
            if (spriteXLarge != null) list.Add(new BodySizeSprite { bodySize = BodySizeType.XLarge, sprite = spriteXLarge });

            item.spritesByBodySize = list.ToArray();

            if (isNew)
            {
                AssetDatabase.CreateAsset(item, assetPath);
            }

            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = item;
            EditorGUIUtility.PingObject(item);
            Debug.Log($"[PaperDollItemFactory] Успешно создан/обновлен ItemData: {assetPath}");
        }

        public static void ConfigureSpritesInFolder(string folderPath)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new string[] { folderPath });
            int count = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    bool changed = false;

                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        changed = true;
                    }
                    if (importer.spriteImportMode != SpriteImportMode.Single)
                    {
                        importer.spriteImportMode = SpriteImportMode.Single;
                        changed = true;
                    }
                    if (!importer.alphaIsTransparency)
                    {
                        importer.alphaIsTransparency = true;
                        changed = true;
                    }
                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        changed = true;
                    }
                    if (importer.filterMode != FilterMode.Bilinear)
                    {
                        importer.filterMode = FilterMode.Bilinear;
                        changed = true;
                    }
                    if (importer.maxTextureSize != 2048)
                    {
                        importer.maxTextureSize = 2048;
                        changed = true;
                    }

                    // Default platform TrueColor RGBA32
                    TextureImporterPlatformSettings defaultSettings = importer.GetDefaultPlatformTextureSettings();
                    if (defaultSettings.format != TextureImporterFormat.RGBA32 || defaultSettings.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        defaultSettings.textureCompression = TextureImporterCompression.Uncompressed;
                        defaultSettings.format = TextureImporterFormat.RGBA32;
                        defaultSettings.compressionQuality = 100;
                        defaultSettings.maxTextureSize = 2048;
                        defaultSettings.overridden = true;
                        importer.SetPlatformTextureSettings(defaultSettings);
                        changed = true;
                    }

                    if (changed)
                    {
                        EditorUtility.SetDirty(importer);
                        importer.SaveAndReimport();
                        count++;
                    }
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Готово", $"Настройки успешно применены к {count} текстурам в папке {folderPath}.", "OK");
        }
    }
}
#endif
