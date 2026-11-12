#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnbridledSanctum.UI;

namespace UnbridledSanctum.EditorScripts
{
    public static class CombatSceneSaver
    {
        [MenuItem("Unbridled Sanctum/UI/Save Scene Layout to Prefab", false, 11)]
        public static void SaveSceneToPrefab()
        {
            Scene activeScene = SceneManager.GetActiveScene();

            CombatUI combatUI = Object.FindFirstObjectByType<CombatUI>(FindObjectsInactive.Include);
            if (combatUI == null || combatUI.combatPanel == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find CombatUI or CombatPanel in the active scene!", "OK");
                return;
            }

            GameObject panelObj = combatUI.combatPanel;

            // Create a clean clone in memory to strip mockups without touching the preview scene
            GameObject cleanClone = Object.Instantiate(panelObj);
            cleanClone.name = "CombatPanel";

            CombatUI cloneUI = cleanClone.GetComponent<CombatUI>();

            // 1. Remove mockups from EnemiesContainer on clone
            if (cloneUI != null && cloneUI.enemiesContainer != null)
            {
                for (int i = cloneUI.enemiesContainer.childCount - 1; i >= 0; i--)
                {
                    Transform c = cloneUI.enemiesContainer.GetChild(i);
                    Object.DestroyImmediate(c.gameObject);
                }
            }

            // 2. Remove mockups from CardsContainer on clone
            if (cloneUI != null && cloneUI.cardsContainer != null)
            {
                for (int i = cloneUI.cardsContainer.childCount - 1; i >= 0; i--)
                {
                    Transform c = cloneUI.cardsContainer.GetChild(i);
                    Object.DestroyImmediate(c.gameObject);
                }
            }

            // 3. Remove preview doll from PlayerAvatarContainer on clone
            if (cloneUI != null && cloneUI.playerAvatarContainer != null)
            {
                for (int i = cloneUI.playerAvatarContainer.childCount - 1; i >= 0; i--)
                {
                    Transform c = cloneUI.playerAvatarContainer.GetChild(i);
                    if (c.name.Contains("TargetHighlight")) continue; // keep highlight aura
                    Object.DestroyImmediate(c.gameObject);
                }
            }

            // Save clean prefab
            string prefabPath = "Assets/Prefabs/UI/CombatPanel.prefab";
            PrefabUtility.SaveAsPrefabAsset(cleanClone, prefabPath);
            Object.DestroyImmediate(cleanClone);

            // Also save the active scene so whatever the user moved remains saved in the scene
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Layout Saved Successfully", 
                "Combat UI layout has been saved to the runtime prefab and scene!\n\n" +
                $"Prefab: {prefabPath}\n" +
                "All mockups were stripped from runtime prefab, while remaining visible in this preview scene for editing.", "OK");
        }
    }
}
#endif
