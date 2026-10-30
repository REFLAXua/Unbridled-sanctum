using System;
using System.Collections.Generic;
using UnityEngine;
using UnbridledSanctum.Combat;

namespace UnbridledSanctum.UI
{
    [System.Serializable]
    public class PostCombatDialogueEntry
    {
        public EnemyType enemyType;
        public CombatResultType resultType;
        [Range(0, 2)]
        public int phase = 0;
        public DialogueSequenceSO sequence;
    }

    [System.Serializable]
    public class EventDialogueEntry
    {
        public string eventId;
        public DialogueSequenceSO sequence;
    }

    /// <summary>
    /// ScriptableObject database containing all dialogue sequences for encounters and events.
    /// Create via: Right Click > Create > Game > Dialogue > Database
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueDatabase", menuName = "Game/Dialogue/Database", order = 41)]
    public class DialogueDatabaseSO : ScriptableObject
    {
        [Header("Post-Combat Dialogues")]
        public List<PostCombatDialogueEntry> postCombatDialogues = new List<PostCombatDialogueEntry>();

        [Header("Dungeon Exploration & Event Encounters")]
        public List<EventDialogueEntry> eventDialogues = new List<EventDialogueEntry>();

        public DialogueSequence GetPostCombatSequence(Combatant enemy, CombatResult result)
        {
            if (enemy == null || result == null) return null;
            int phase = Mathf.Clamp(enemy.seductionStacks, 0, 2);

            for (int i = 0; i < postCombatDialogues.Count; i++)
            {
                var entry = postCombatDialogues[i];
                if (entry.enemyType == enemy.enemyType && entry.resultType == result.type && entry.phase == phase)
                {
                    if (entry.sequence != null)
                    {
                        return entry.sequence.ToDialogueSequence();
                    }
                }
            }

            return null;
        }

        public DialogueSequence GetEventSequence(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return null;

            for (int i = 0; i < eventDialogues.Count; i++)
            {
                var entry = eventDialogues[i];
                if (entry.eventId == eventId && entry.sequence != null)
                {
                    return entry.sequence.ToDialogueSequence();
                }
            }

            return null;
        }
    }
}
