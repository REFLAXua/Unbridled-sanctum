using System;
using System.Collections.Generic;
using UnityEngine;
using UnbridledSanctum.Combat;

namespace UnbridledSanctum.UI
{
    public enum DialogueSpeaker
    {
        Player,
        NPC,
        Narrator
    }

    [System.Serializable]
    public class DialogueLine
    {
        public DialogueSpeaker speaker;
        public string speakerName;
        [TextArea(2, 5)]
        public string text;

        [Header("Emotions")]
        public string playerExpression; // "Normal", "Smile", "Shocked", "Annoyed", "Crying", "Smug", "Sleepy", "Delighted"
        public string enemyEmotion;     // "smile", "annoyed", "grin", "suprised", "lust_joy", "lust_intence", "lust_estasy", etc.

        public DialogueLine(DialogueSpeaker speaker, string speakerName, string text, string playerExpression = "Normal", string enemyEmotion = "default")
        {
            this.speaker = speaker;
            this.speakerName = speakerName;
            this.text = text;
            this.playerExpression = playerExpression;
            this.enemyEmotion = enemyEmotion;
        }
    }

    [System.Serializable]
    public class DialogueSequence
    {
        public string dialogueId;
        public EnemyType enemyType;
        public int enemyCombatPhase; // 0, 1, 2 (seduction stacks)
        public bool hasEnemy = true;
        public List<DialogueLine> lines = new List<DialogueLine>();
        public Action onComplete;

        public DialogueSequence(string id, EnemyType enemyType, int enemyCombatPhase, Action onComplete = null, bool hasEnemy = true)
        {
            this.dialogueId = id;
            this.enemyType = enemyType;
            this.enemyCombatPhase = enemyCombatPhase;
            this.onComplete = onComplete;
            this.hasEnemy = hasEnemy;
        }

        public void AddLine(DialogueSpeaker speaker, string speakerName, string text, string playerExpression = "Normal", string enemyEmotion = "default")
        {
            lines.Add(new DialogueLine(speaker, speakerName, text, playerExpression, enemyEmotion));
        }
    }
}
