using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using UnbridledSanctum.Combat;

/// <summary>
/// Unity Editor Window allowing game designers to inspect and manually customize
/// enemy stats, movesets, AI weights, and naked likelihood modifiers.
/// Open via: Window > Sanctum > Enemy Editor.
/// </summary>
public class EnemyEditorWindow : EditorWindow
{
    private const string RESOURCES_ENEMY_PATH = "Assets/Resources/Enemies";
    private const string RESOURCES_SKILLS_PATH = "Assets/Resources/Enemies/Skills";

    private List<EnemyDefinitionSO> loadedEnemies = new List<EnemyDefinitionSO>();
    private int selectedEnemyIndex = 0;
    private Vector2 scrollPos;
    private SerializedObject serializedEnemy;

    [MenuItem("Window/Sanctum/Enemy Editor", false, 50)]
    [MenuItem("Tools/Sanctum/Enemy Editor", false, 50)]
    public static void ShowWindow()
    {
        EnemyEditorWindow window = GetWindow<EnemyEditorWindow>("Enemy Editor");
        window.minSize = new Vector2(560f, 620f);
        window.LoadAllEnemies();
    }

    private void OnEnable()
    {
        LoadAllEnemies();
    }

    private void LoadAllEnemies()
    {
        loadedEnemies.Clear();
        EnsureDirectories();

        string[] guids = AssetDatabase.FindAssets("t:EnemyDefinitionSO", new[] { RESOURCES_ENEMY_PATH });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var def = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>(path);
            if (def != null && !loadedEnemies.Contains(def))
            {
                loadedEnemies.Add(def);
            }
        }

        if (loadedEnemies.Count == 0)
        {
            GenerateDefaultEnemyAssets();
        }

        selectedEnemyIndex = Mathf.Clamp(selectedEnemyIndex, 0, Mathf.Max(0, loadedEnemies.Count - 1));
        UpdateSerializedObject();
    }

    private void UpdateSerializedObject()
    {
        if (loadedEnemies.Count > 0 && selectedEnemyIndex < loadedEnemies.Count && loadedEnemies[selectedEnemyIndex] != null)
        {
            serializedEnemy = new SerializedObject(loadedEnemies[selectedEnemyIndex]);
        }
        else
        {
            serializedEnemy = null;
        }
    }

    private void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Enemies"))
            AssetDatabase.CreateFolder("Assets/Resources", "Enemies");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Enemies/Skills"))
            AssetDatabase.CreateFolder("Assets/Resources/Enemies", "Skills");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("SANCTUM ENEMY & MOVESET EDITOR", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Configure enemy health, floor scaling, semen specimen mutations, and customize their movesets with conditions and 'Likelihood When Naked' modifiers.", MessageType.Info);

        EditorGUILayout.Space(6);

        // Top Toolbar: Selector + Actions
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (loadedEnemies.Count > 0)
        {
            string[] names = new string[loadedEnemies.Count];
            for (int i = 0; i < loadedEnemies.Count; i++)
            {
                names[i] = loadedEnemies[i] != null ? loadedEnemies[i].enemyName : $"Enemy {i}";
            }

            int prevIdx = selectedEnemyIndex;
            selectedEnemyIndex = EditorGUILayout.Popup(selectedEnemyIndex, names, EditorStyles.toolbarPopup, GUILayout.Width(200));
            if (prevIdx != selectedEnemyIndex)
            {
                UpdateSerializedObject();
            }
        }
        else
        {
            EditorGUILayout.LabelField("No enemies found in Resources/Enemies", EditorStyles.miniLabel);
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("+ New Enemy", EditorStyles.toolbarButton, GUILayout.Width(95)))
        {
            CreateNewEnemy();
        }

        if (GUILayout.Button("Regen Defaults", EditorStyles.toolbarButton, GUILayout.Width(105)))
        {
            GenerateDefaultEnemyAssets();
            LoadAllEnemies();
        }

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(65)))
        {
            LoadAllEnemies();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        if (serializedEnemy == null || selectedEnemyIndex >= loadedEnemies.Count || loadedEnemies[selectedEnemyIndex] == null)
        {
            EditorGUILayout.HelpBox("Please select or create an enemy to edit.", MessageType.Warning);
            return;
        }

        serializedEnemy.Update();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EnemyDefinitionSO currentDef = loadedEnemies[selectedEnemyIndex];

        // --- 1. Enemy Identity Section ---
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("1. ENEMY IDENTITY & METADATA", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedEnemy.FindProperty("enemyType"));
        EditorGUILayout.PropertyField(serializedEnemy.FindProperty("enemyName"));
        EditorGUILayout.PropertyField(serializedEnemy.FindProperty("gender"));
        EditorGUILayout.PropertyField(serializedEnemy.FindProperty("enemyIcon"));
        EditorGUILayout.Space(4);

        SerializedProperty growthProp = serializedEnemy.FindProperty("canTriggerBreastGrowth");
        if (growthProp != null)
        {
            EditorGUILayout.PropertyField(growthProp, new GUIContent("Can Trigger Breast Growth", "If true, semen from this specific enemy specimen triggers heroine breast expansion during post-combat."));
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);

        // --- 2. Health, Mana & Defenses ---
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("2. COMBAT STATS & SCALING", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedEnemy.FindProperty("baseHp"));
        EditorGUILayout.PropertyField(serializedEnemy.FindProperty("hpPerFloor"));
        EditorGUILayout.PropertyField(serializedEnemy.FindProperty("maxMana"));
        EditorGUILayout.PropertyField(serializedEnemy.FindProperty("startingMana"));
        EditorGUILayout.PropertyField(serializedEnemy.FindProperty("maxSeductionStacks"));
        EditorGUILayout.PropertyField(serializedEnemy.FindProperty("baseShield"));
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);

        // --- 3. Moveset / AI Skill Pool ---
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("3. AI MOVESET & WEIGHTS", EditorStyles.boldLabel);
        if (GUILayout.Button("+ Add Move", GUILayout.Width(100)))
        {
            AddEmptySkillToPool(currentDef);
        }
        EditorGUILayout.EndHorizontal();

        SerializedProperty poolProp = serializedEnemy.FindProperty("skillPool");
        if (poolProp != null)
        {
            for (int i = 0; i < poolProp.arraySize; i++)
            {
                SerializedProperty element = poolProp.GetArrayElementAtIndex(i);
                SerializedProperty skillProp = element.FindPropertyRelative("skill");
                SerializedProperty weightProp = element.FindPropertyRelative("weight");
                SerializedProperty condProp = element.FindPropertyRelative("condition");
                SerializedProperty nakedProp = element.FindPropertyRelative("likelihoodWhenNaked");

                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.BeginHorizontal();
                string skillTitle = (skillProp.objectReferenceValue != null) ? skillProp.objectReferenceValue.name : $"Move #{i + 1} [Empty]";
                EditorGUILayout.LabelField($"#{i + 1}: {skillTitle}", EditorStyles.boldLabel);

                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    poolProp.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(skillProp, new GUIContent("Skill Asset"));
                EditorGUILayout.PropertyField(weightProp, new GUIContent("Base Weight (1-100)"));
                EditorGUILayout.PropertyField(condProp, new GUIContent("Condition"));

                if (nakedProp != null)
                {
                    float curNaked = nakedProp.floatValue;
                    string hint = curNaked <= 0.001f ? " [Disabled when naked]" : (curNaked > 1.05f ? " [Favored when naked]" : "");
                    EditorGUILayout.Slider(nakedProp, 0f, 3f, new GUIContent($"Likelihood When Naked ({curNaked:F1}x){hint}"));
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(12);

        // Save & Ping Controls
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Changes", GUILayout.Height(32)))
        {
            serializedEnemy.ApplyModifiedProperties();
            EditorUtility.SetDirty(currentDef);
            AssetDatabase.SaveAssets();
            Debug.Log($"[EnemyEditorWindow] Saved changes to {currentDef.enemyName}!");
        }

        if (GUILayout.Button("Ping in Project", GUILayout.Height(32), GUILayout.Width(130)))
        {
            EditorGUIUtility.PingObject(currentDef);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(16);
        EditorGUILayout.EndScrollView();

        serializedEnemy.ApplyModifiedProperties();
    }

    private void AddEmptySkillToPool(EnemyDefinitionSO def)
    {
        if (def == null) return;
        Undo.RecordObject(def, "Add Enemy Skill");
        def.skillPool.Add(new WeightedEnemySkill
        {
            skill = null,
            weight = 10,
            condition = SkillCondition.Always,
            likelihoodWhenNaked = 1.0f
        });
        EditorUtility.SetDirty(def);
        UpdateSerializedObject();
    }

    private void CreateNewEnemy()
    {
        EnsureDirectories();
        EnemyDefinitionSO newEnemy = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
        newEnemy.enemyName = "Custom Foe";
        newEnemy.baseHp = 25;
        newEnemy.hpPerFloor = 3;
        newEnemy.maxMana = 20;
        newEnemy.startingMana = 10;

        string uniquePath = AssetDatabase.GenerateUniqueAssetPath($"{RESOURCES_ENEMY_PATH}/NewEnemy.asset");
        AssetDatabase.CreateAsset(newEnemy, uniquePath);
        AssetDatabase.SaveAssets();

        LoadAllEnemies();
        selectedEnemyIndex = loadedEnemies.IndexOf(newEnemy);
        UpdateSerializedObject();
    }

    [MenuItem("Tools/Sanctum/Generate Default Enemy Assets", false, 31)]
    public static void GenerateDefaultEnemyAssets()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Enemies"))
            AssetDatabase.CreateFolder("Assets/Resources", "Enemies");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Enemies/Skills"))
            AssetDatabase.CreateFolder("Assets/Resources/Enemies", "Skills");

        // 1. Create or load Slime Girl
        string slimePath = $"{RESOURCES_ENEMY_PATH}/SlimeGirl.asset";
        EnemyDefinitionSO slimeDef = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>(slimePath);
        if (slimeDef == null)
        {
            slimeDef = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
            slimeDef.enemyType = EnemyType.SlimeGirl;
            slimeDef.enemyName = "Slime Girl";
            slimeDef.gender = EntityGender.Futa;
            slimeDef.baseHp = 22;
            slimeDef.hpPerFloor = 3;
            slimeDef.maxMana = 15;
            slimeDef.startingMana = 10;
            slimeDef.maxSeductionStacks = 3;
            slimeDef.canTriggerBreastGrowth = true; // Slime specimen triggers growth for testing!
            AssetDatabase.CreateAsset(slimeDef, slimePath);
        }

        // 2. Create or load Succubus
        string succPath = $"{RESOURCES_ENEMY_PATH}/Succubus.asset";
        EnemyDefinitionSO succDef = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>(succPath);
        if (succDef == null)
        {
            succDef = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
            succDef.enemyType = EnemyType.Succubus;
            succDef.enemyName = "Succubus";
            succDef.gender = EntityGender.Futa;
            succDef.baseHp = 26;
            succDef.hpPerFloor = 4;
            succDef.maxMana = 20;
            succDef.startingMana = 12;
            succDef.maxSeductionStacks = 3;
            succDef.canTriggerBreastGrowth = false;
            AssetDatabase.CreateAsset(succDef, succPath);
        }

        // Create sample skills for Slime
        var sFap = CreateOrLoadSkill("Slime_FapToFace", "Fap to Face", 1, 0, 0, 25, 0, 8, 20, CombatStance.Neutral, "Violently strokes its gelatinous member while staring directly into your eyes, splattering sticky fluids toward your face!");
        var sRub = CreateOrLoadSkill("Slime_RubPussy", "Rub Against Pussy", 2, 0, 20, 30, 0, 10, 15, CombatStance.Grappled, "Grinds its throbbing member repeatedly against your outer vulva, melting fabric and stoking searing friction without entry!");
        var sBreast = CreateOrLoadSkill("Slime_BreastFondle", "Breast Fondle & Grind", 2, 0, 20, 30, 0, 10, 15, CombatStance.Grappled, "Kneads and pinches your sensitive breasts with warm pseudopods, wearing down your top and overwhelming your senses!");
        var sThigh = CreateOrLoadSkill("Slime_ThighGrind", "Thigh Grinding", 1, 0, 10, 25, 3, 12, 15, CombatStance.Grappled, "Slides forcefully between your clamped thighs, lubricating your legs and testing your posture balance.");

        if (slimeDef.skillPool == null || slimeDef.skillPool.Count == 0)
        {
            slimeDef.skillPool = new List<WeightedEnemySkill>
            {
                new WeightedEnemySkill { skill = sFap, weight = 15, condition = SkillCondition.Always, likelihoodWhenNaked = 1.2f },
                new WeightedEnemySkill { skill = sRub, weight = 20, condition = SkillCondition.OnlyInGrappledStance, likelihoodWhenNaked = 1.5f },
                new WeightedEnemySkill { skill = sBreast, weight = 18, condition = SkillCondition.OnlyInGrappledStance, likelihoodWhenNaked = 1.2f },
                new WeightedEnemySkill { skill = sThigh, weight = 16, condition = SkillCondition.OnlyInGrappledStance, likelihoodWhenNaked = 1.0f }
            };
            EditorUtility.SetDirty(slimeDef);
        }

        // Create sample skills for Succubus
        var sucFap = CreateOrLoadSkill("Succubus_FapToFace", "Fap to Face", 1, 0, 0, 30, 0, 8, 25, CombatStance.Neutral, "Strokes her demonic futanari cock with teasing smirk, aiming pre-cum droplets directly toward your lips and face!");
        var sucRub = CreateOrLoadSkill("Succubus_RubPussy", "Rub Against Pussy", 2, 0, 25, 35, 0, 12, 20, CombatStance.Grappled, "Grinds her hot demonic shaft against your clitoris and labia, teasing your entrance without breaching inside!");
        var sucBreast = CreateOrLoadSkill("Succubus_BreastFondle", "Breast Fondle & Grind", 2, 0, 25, 35, 0, 10, 20, CombatStance.Grappled, "Firmly cups and squeezes your chest while grinding against your torso, teasing your nipples into rigid peaks!");
        var sucThigh = CreateOrLoadSkill("Succubus_ThighGrind", "Thigh Grinding", 1, 0, 15, 30, 4, 14, 20, CombatStance.Grappled, "Pumps her throbbing length snugly between your clamped thighs, generating intense erotic friction.");

        if (succDef.skillPool == null || succDef.skillPool.Count == 0)
        {
            succDef.skillPool = new List<WeightedEnemySkill>
            {
                new WeightedEnemySkill { skill = sucFap, weight = 15, condition = SkillCondition.Always, likelihoodWhenNaked = 1.3f },
                new WeightedEnemySkill { skill = sucRub, weight = 22, condition = SkillCondition.OnlyInGrappledStance, likelihoodWhenNaked = 1.6f },
                new WeightedEnemySkill { skill = sucBreast, weight = 18, condition = SkillCondition.OnlyInGrappledStance, likelihoodWhenNaked = 1.2f },
                new WeightedEnemySkill { skill = sucThigh, weight = 16, condition = SkillCondition.OnlyInGrappledStance, likelihoodWhenNaked = 1.1f }
            };
            EditorUtility.SetDirty(succDef);
        }

        AssetDatabase.SaveAssets();
    }

    private static EnemySkillSO CreateOrLoadSkill(string assetName, string name, int mana, int hpDmg, int clothDmg, int sedDmg, int shield, int stamDmg, int arousal, CombatStance stance, string desc)
    {
        string path = $"{RESOURCES_SKILLS_PATH}/{assetName}.asset";
        EnemySkillSO skill = AssetDatabase.LoadAssetAtPath<EnemySkillSO>(path);
        if (skill == null)
        {
            skill = ScriptableObject.CreateInstance<EnemySkillSO>();
            skill.skillName = name;
            skill.manaCost = mana;
            skill.hpDamage = hpDmg;
            skill.clothesDamage = clothDmg;
            skill.seductionDamage = sedDmg;
            skill.shieldGain = shield;
            skill.staminaDamage = stamDmg;
            skill.selfArousalGain = arousal;
            skill.stanceRequirement = stance;
            skill.description = desc;
            AssetDatabase.CreateAsset(skill, path);
        }
        return skill;
    }
}
