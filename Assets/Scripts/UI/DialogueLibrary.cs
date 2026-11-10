using System;
using UnityEngine;
using UnbridledSanctum.Combat;

namespace UnbridledSanctum.UI
{
    /// <summary>
    /// Repository and factory for post-combat and dungeon event dialogues.
    /// Strictly adheres to emotion rules:
    /// - Enemy Phase 0-1 (seductionStacks < 2): Allowed: smile, grin, annoyed, suprised, lustful. NEVER phase2 or x_lust_x.
    /// - Enemy Phase 2 (seductionStacks >= 2): Allowed: phase2 and x_lust_x (lust_joy, lust_suprised, lust_intence, lust_estasy).
    /// </summary>
    public static class DialogueLibrary
    {
        public static bool HasPostCombatDialogue(Combatant enemy, CombatResult result)
        {
            if (enemy == null || result == null) return false;
            return enemy.enemyType == EnemyType.SlimeGirl || enemy.enemyType == EnemyType.Succubus;
        }

        public static DialogueSequence GetPostCombatDialogue(Combatant enemy, CombatResult result, Combatant player = null)
        {
            if (enemy == null || result == null) return null;

            if (DialogueUI.Instance != null && DialogueUI.Instance.dialogueDatabase != null)
            {
                DialogueSequence customSeq = DialogueUI.Instance.dialogueDatabase.GetPostCombatSequence(enemy, result);
                if (customSeq != null) return customSeq;
            }

            int phase = Mathf.Clamp(enemy.seductionStacks, 0, 2);
            string enemyName = enemy.name;
            string playerName = player != null && !string.IsNullOrEmpty(player.name) ? player.name : "You";

            if (enemy.enemyType == EnemyType.SlimeGirl)
            {
                return CreateSlimeCombatDialogue(enemy, result, phase, enemyName, playerName);
            }
            else if (enemy.enemyType == EnemyType.Succubus)
            {
                return CreateSuccubusCombatDialogue(enemy, result, phase, enemyName, playerName);
            }

            return null;
        }

        // =========================================================================
        // SLIME GIRL DIALOGUES
        // =========================================================================

        private static DialogueSequence CreateSlimeCombatDialogue(Combatant enemy, CombatResult result, int phase, string enemyName, string playerName)
        {
            DialogueSequence seq = new DialogueSequence($"slime_combat_{result.type}_{phase}", EnemyType.SlimeGirl, phase);

            switch (result.type)
            {
                case CombatResultType.VictoryHp:
                    if (phase < 2)
                    {
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Ow, ow, ow! How can your strikes hurt so much when my body is made of gelatin?!", "Normal", "annoyed");
                        seq.AddLine(DialogueSpeaker.Player, playerName, "Gelatin or not, you were blocking my passage. Had enough?", "Smug", "annoyed");
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Eep! Put that blade away! I'm seeping into the stone cracks... you won't catch me!", "Normal", "suprised");
                        seq.AddLine(DialogueSpeaker.Narrator, "Narrator", "The Slime Girl liquefied her lower half and slipped swiftly into a narrow subterranean fissure, leaving behind scattered loot.", "Smile", "suprised");
                    }
                    else
                    {
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Aahnn... beaten down while burning from inside... every drop of my core is trembling...", "Normal", "lust_joy");
                        seq.AddLine(DialogueSpeaker.Player, playerName, "You pushed yourself to exhaustion. Can you even hold your shape anymore?", "Smile", "lust_joy");
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "M-My slime is so warm... if you touch me again right now, I'll melt into sweet syrup...", "Normal", "lust_suprised");
                        seq.AddLine(DialogueSpeaker.Narrator, "Narrator", "The dazed Slime Girl puddles helplessly at your boots, completely drained of fighting will.", "Smug", "lust_suprised");
                    }
                    break;

                case CombatResultType.VictorySeduction:
                    // Seduction victory implies phase 2 (climax reached)
                    seq.AddLine(DialogueSpeaker.NPC, enemyName, "Haah... haah... the sensations... my body is rippling uncontrollably... You're so intoxicating...", "Normal", "lust_joy");
                    seq.AddLine(DialogueSpeaker.Player, playerName, "Look at you, trembling like warm honey. Completely at my mercy.", "Smug", "lust_joy");
                    seq.AddLine(DialogueSpeaker.NPC, enemyName, "I... I can't hold my viscosity anymore... Do whatever you desire with me...", "Shocked", "lust_suprised");
                    seq.AddLine(DialogueSpeaker.Narrator, "Narrator", "The Slime Girl surrendered completely to carnal submission, gazing up at you with glassy, lovestruck eyes.", "Delighted", "lust_joy");
                    break;

                case CombatResultType.DefeatHp:
                    if (phase < 2)
                    {
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Aww, out of stamina already? Cavern slimes aren't as squishy as you thought, are they?", "Crying", "grin");
                        seq.AddLine(DialogueSpeaker.Player, playerName, "D-Damn it... your acidic slime wore me down...", "Crying", "grin");
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Sleep tight, adventurer! I'll gently spit you out by the cavern stream once you're softened!", "Sleepy", "smile");
                    }
                    else
                    {
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Hehehe... you tried so hard to tease me, but collapsed first! Now it's my turn to engulf you entirely~", "Sleepy", "lust_joy");
                        seq.AddLine(DialogueSpeaker.Player, playerName, "Too... warm... my limbs won't obey...", "Sleepy", "lust_joy");
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Mmm, your feverish warmth feels incredible... drift away into sweet dreams inside my embrace~", "Sleepy", "lust_suprised");
                    }
                    break;

                case CombatResultType.DefeatSeduction:
                    if (phase < 2)
                    {
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Oho? Look whose cheeks are flushed crimson! You couldn't handle my jiggling curves, could you?", "Crying", "lustful");
                        seq.AddLine(DialogueSpeaker.Player, playerName, "I... I can't think straight anymore... my heart is pounding...", "Crying", "lustful");
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Let the sweet numbness wash over you~ Rest now, darling.", "Sleepy", "smile");
                    }
                    else
                    {
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Ahaha! Both of us drenched in sweet ecstasy, but your mind snapped first! You're completely mine now~", "Sleepy", "lust_joy");
                        seq.AddLine(DialogueSpeaker.Player, playerName, "Everything is spinning... overwhelming bliss...", "Sleepy", "lust_joy");
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Sleep softly... you'll wake up when the heat cools down...", "Sleepy", "lust_suprised");
                    }
                    break;

                case CombatResultType.CorruptionSurvival:
                    seq.AddLine(DialogueSpeaker.NPC, enemyName, "Wh-What is that dark pulsing aura around you?! You should have collapsed, yet you're still standing?!", "Shocked", "suprised");
                    seq.AddLine(DialogueSpeaker.Player, playerName, "Corruption binds my soul... I will not fall here!", "Smug", "annoyed");
                    seq.AddLine(DialogueSpeaker.Narrator, "Narrator", "A sinister miasma surged through your veins, keeping you alive at the brink of death (1 HP). Corruption spreads deeper into your soul (+1).", "Smile", "suprised");
                    break;
            }

            return seq;
        }

        // =========================================================================
        // SUCCUBUS DIALOGUES
        // =========================================================================

        private static DialogueSequence CreateSuccubusCombatDialogue(Combatant enemy, CombatResult result, int phase, string enemyName, string playerName)
        {
            DialogueSequence seq = new DialogueSequence($"succubus_combat_{result.type}_{phase}", EnemyType.Succubus, phase);

            switch (result.type)
            {
                case CombatResultType.VictoryHp:
                    if (phase < 2)
                    {
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Tch! You crude brute! Ruining my immaculate wings with sheer force... uncivilized barbarian!", "Normal", "annoyed");
                        seq.AddLine(DialogueSpeaker.Player, playerName, "Keep your wicked charms to yourself, demoness. Steel speaks louder than whispers.", "Smug", "annoyed");
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Hmph! Next time I won't play with my food. Savor this fleeting victory while your mortal heart still beats!", "Normal", "grin");
                        seq.AddLine(DialogueSpeaker.Narrator, "Narrator", "With a flap of leathery wings and a curse in abyssal tongue, the Succubus vanished into the brimstone haze.", "Smile", "grin");
                    }
                    else
                    {
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Haaaah... beaten while burning up... how utterly humiliating... yet delightfully thrilling...", "Normal", "lust_intence");
                        seq.AddLine(DialogueSpeaker.Player, playerName, "Looks like your demonic pride couldn't withstand the pressure.", "Smile", "lust_intence");
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Ahnnn... don't look at me like that with those victorious eyes... it makes me shiver to my very core...", "Normal", "lust_estasy");
                        seq.AddLine(DialogueSpeaker.Narrator, "Narrator", "The demoness kneels trembling before you, her breath hitched in overwhelming, exquisite submission.", "Smug", "lust_estasy");
                    }
                    break;

                case CombatResultType.VictorySeduction:
                    seq.AddLine(DialogueSpeaker.NPC, enemyName, "H-How...?! Me, a mistress of the Abyss... bested in the arts of carnal seduction by a mortal...?!", "Normal", "lust_intence");
                    seq.AddLine(DialogueSpeaker.Player, playerName, "Seems your ancient charms were no match for someone who knows how to tease your desires.", "Smug", "lust_intence");
                    seq.AddLine(DialogueSpeaker.NPC, enemyName, "Ah... haah... my knees are failing me... The sweetest humiliation I have ever tasted... What will you do with me now...?", "Delighted", "lust_estasy");
                    seq.AddLine(DialogueSpeaker.Narrator, "Narrator", "The haughty Succubus collapsed onto the stone floor, completely captivated and yearning for your touch.", "Smile", "lust_estasy");
                    break;

                case CombatResultType.DefeatHp:
                    if (phase < 2)
                    {
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Is that all the resilience your fragile mortal body possessed? What a disappointing toy.", "Crying", "smile");
                        seq.AddLine(DialogueSpeaker.Player, playerName, "Ghh... darkness is closing in...", "Crying", "smile");
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Fret not. The Sanctuary cave will nurse your wounds... come back when you're worth my time.", "Sleepy", "grin");
                    }
                    else
                    {
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Fufufu... we both burned so hotly, but your mortal vessel cracked before mine!", "Sleepy", "lust_joy");
                        seq.AddLine(DialogueSpeaker.Player, playerName, "My strength... completely drained...", "Sleepy", "lust_joy");
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Rest in my shadow, precious soul. Next time we dance, let us dive even deeper into the abyss...", "Sleepy", "lust_intence");
                    }
                    break;

                case CombatResultType.DefeatSeduction:
                    if (phase < 2)
                    {
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Look into my eyes... there we go. Surrender your thoughts, your will, your longing to me.", "Sleepy", "smile");
                        seq.AddLine(DialogueSpeaker.Player, playerName, "Yes... whatever you say... mistress...", "Sleepy", "smile");
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Such an obedient little pet. Drift away into delightful dreams...", "Sleepy", "grin");
                    }
                    else
                    {
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Ahahaha! Heavenly ecstasy! Your mind melted into pure adoration for me! Delicious... absolutely divine!", "Sleepy", "lust_estasy");
                        seq.AddLine(DialogueSpeaker.Player, playerName, "Lost in your warmth... forever...", "Sleepy", "lust_estasy");
                        seq.AddLine(DialogueSpeaker.NPC, enemyName, "Mmm, you gave me such exquisite thrills... awaken safely in your cave, and return to me soon~", "Sleepy", "lust_intence");
                    }
                    break;

                case CombatResultType.CorruptionSurvival:
                    seq.AddLine(DialogueSpeaker.NPC, enemyName, "Impossible! The lethal blow struck you true... how does your cursed heart still beat?!", "Shocked", "lust_suprised");
                    seq.AddLine(DialogueSpeaker.Player, playerName, "The corruption in my veins refuses to let me die... not here!", "Smug", "annoyed");
                    seq.AddLine(DialogueSpeaker.Narrator, "Narrator", "Dark abyssal energy knitted your broken spirit back together. You barely cling to life with 1 HP as corruption takes hold (+1).", "Smile", "lust_intence");
                    break;
            }

            return seq;
        }

        // =========================================================================
        // DUNGEON EVENT DIALOGUES
        // =========================================================================

        public static DialogueSequence GetEventDialogue(string eventTitle, CharacterState playerState)
        {
            string cleanTitle = string.IsNullOrEmpty(eventTitle) ? "Underground Spring" : eventTitle.Trim();

            if (DialogueUI.Instance != null && DialogueUI.Instance.dialogueDatabase != null)
            {
                DialogueSequence customSeq = DialogueUI.Instance.dialogueDatabase.GetEventSequence(cleanTitle);
                if (customSeq != null) return customSeq;
            }

            if (cleanTitle.IndexOf("Spring", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Elven grove spring
                DialogueSequence seq = new DialogueSequence("event_spring", EnemyType.ElvenScout, 0, hasEnemy: false);
                seq.AddLine(DialogueSpeaker.Narrator, "Enchanted Spring", "You discover a secluded glade where pure emerald spring water bubbles gently over ancient elven stones.", "Normal");
                seq.AddLine(DialogueSpeaker.Player, "You", "The water sparkles with tranquil vitality. A brief respite under the ancient canopy.", "Smile");
                seq.AddLine(DialogueSpeaker.Narrator, "Enchanted Spring", "Drinking deeply and washing the trail dust from your skin restores vitality (+15 HP, clothes refreshed).", "Delighted");
                return seq;
            }
            else if (cleanTitle.IndexOf("Stele", StringComparison.OrdinalIgnoreCase) >= 0 || cleanTitle.IndexOf("Obelisk", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Ancient Elven Monolith
                DialogueSequence seq = new DialogueSequence("event_stele", EnemyType.ElvenScout, 0, hasEnemy: false);
                seq.AddLine(DialogueSpeaker.Narrator, "Verdant Monolith", "An ancient moss-covered stone pillar rises between towering roots, radiating faint primordial essence.", "Normal");
                seq.AddLine(DialogueSpeaker.Player, "You", "Ancient elven runes... they speak of an endless fertility ritual and reverence for life.", "Shocked");
                seq.AddLine(DialogueSpeaker.Narrator, "Verdant Monolith", "Meditating before the sacred glyphs sharpens your mind and replenishes your reserves (+35 Lumieres, Mana replenished).", "Normal");
                return seq;
            }
            else
            {
                // General forest exploration
                DialogueSequence seq = new DialogueSequence("event_camp", EnemyType.ElvenScout, 0, hasEnemy: false);
                seq.AddLine(DialogueSpeaker.Narrator, "Canopy Alcove", "You find shelter beneath the arched roots of a massive golden oak.", "Normal");
                seq.AddLine(DialogueSpeaker.Player, "You", "The rustle of leaves masks the forest sounds. Time to catch my breath.", "Smile");
                seq.AddLine(DialogueSpeaker.Narrator, "Canopy Alcove", "Foraging through the roots, you uncover dried rations and fragrant elven herbs (+20 Lumieres).", "Delighted");
                return seq;
            }
        }
    }
}
