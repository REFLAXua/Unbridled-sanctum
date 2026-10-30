using System;
using System.Collections.Generic;
using UnityEngine;
using UnbridledSanctum.Combat;

namespace UnbridledSanctum.UI
{
    [System.Serializable]
    public class DialogueLineData
    {
        public DialogueSpeaker speaker = DialogueSpeaker.Player;
        public string speakerName = "Hero";
        [TextArea(2, 6)]
        public string text = "Dialogue text...";
        public string playerExpression = "Normal";
        public string enemyEmotion = "smile";
    }

    /// <summary>
    /// ScriptableObject defining an editable dialogue sequence with animated text and dynamic expressions.
    /// Create via: Right Click > Create > Game > Dialogue > Sequence
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogueSequence", menuName = "Game/Dialogue/Sequence", order = 40)]
    public class DialogueSequenceSO : ScriptableObject
    {
        [Header("Sequence Identity")]
        public string sequenceId = "unique_dialogue_id";
        public EnemyType enemyType = EnemyType.SlimeGirl;
        [Range(0, 2)]
        public int requiredCombatPhase = 0;

        [Header("Dialogue Script")]
        public List<DialogueLineData> lines = new List<DialogueLineData>();

        /// <summary>
        /// Converts this ScriptableObject definition into a runtime DialogueSequence.
        /// </summary>
        public DialogueSequence ToDialogueSequence()
        {
            DialogueSequence seq = new DialogueSequence(
                string.IsNullOrEmpty(sequenceId) ? name : sequenceId,
                enemyType,
                requiredCombatPhase
            );

            if (lines != null)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    var l = lines[i];
                    seq.AddLine(l.speaker, l.speakerName, l.text, l.playerExpression, l.enemyEmotion);
                }
            }

            return seq;
        }
    }
}
