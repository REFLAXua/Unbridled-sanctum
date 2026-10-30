#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnbridledSanctum.Combat;
using UnbridledSanctum.UI;
using System.Collections.Generic;

namespace UnbridledSanctum.Editor
{
    public static class ContentMigrationTool
    {
        [MenuItem("Tools/Sanctum/Generate ScriptableObjects for Existing Content")]
        public static void GenerateAllScriptableObjects()
        {
            EnsureDirectories();

            var cards = GenerateCards();
            var skills = GenerateSkills();
            var enemies = GenerateEnemies(skills);
            var dialogueDb = GenerateDialogues();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ContentMigrationTool] Successfully generated ScriptableObjects:\n" +
                      $"- {cards.Count} Cards in Assets/Data/Combat/Cards/\n" +
                      $"- {skills.Count} Enemy Skills in Assets/Data/Combat/Skills/\n" +
                      $"- {enemies.Count} Enemy Definitions in Assets/Data/Combat/Enemies/\n" +
                      $"- Dialogue Sequences & Database in Assets/Data/Dialogues/");

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Content Generation Complete",
                    "Successfully generated all Cards, Enemy Skills, Enemy Definitions, and Dialogues as editable ScriptableObject assets in Assets/Data/!\n\nYou can now edit, tweak, or add new content directly through the Unity Project window.",
                    "Awesome!"
                );
            }
        }

        private static void EnsureDirectories()
        {
            string[] dirs = new string[]
            {
                "Assets/Data",
                "Assets/Data/Combat",
                "Assets/Data/Combat/Cards",
                "Assets/Data/Combat/Skills",
                "Assets/Data/Combat/Enemies",
                "Assets/Data/Dialogues",
                "Assets/Data/Dialogues/PostCombat",
                "Assets/Data/Dialogues/Events"
            };

            foreach (var d in dirs)
            {
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
            }
        }

        // =====================================================================
        // CARDS GENERATOR
        // =====================================================================
        private static List<CardSO> GenerateCards()
        {
            List<CardSO> list = new List<CardSO>();

            list.Add(CreateOrUpdateCard("strike", "Strike", 1, 1, 7, 0, 0, 0, CardTarget.SingleEnemy, "A swift physical strike dealing 7 damage."));
            list.Add(CreateOrUpdateCard("heavy_slash", "Heavy Slash", 2, 2, 16, 0, 0, 0, CardTarget.SingleEnemy, "A crushing overhead blow dealing 16 heavy damage."));
            list.Add(CreateOrUpdateCard("block", "Defend", 1, 1, 0, 6, 0, 0, CardTarget.Self, "Raise guard, gaining 6 Shield to absorb attacks."));
            list.Add(CreateOrUpdateCard("iron_fortress", "Iron Fortress", 2, 2, 0, 15, 0, 0, CardTarget.Self, "Assume an unyielding stance, gaining 15 Shield."));
            list.Add(CreateOrUpdateCard("flirt", "Flirt", 1, 1, 0, 0, 35, 0, CardTarget.SingleEnemy, "Playfully wink and whisper, raising target Seduction by 35%."));
            list.Add(CreateOrUpdateCard("allure", "Sensual Tease", 2, 2, 0, 0, 65, 0, CardTarget.SingleEnemy, "A mesmerizing seductive dance, raising target Seduction by 65%."));
            list.Add(CreateOrUpdateCard("mend_clothes", "Quick Patch", 1, 2, 0, 0, 0, 15, CardTarget.Self, "Swiftly adjust garments, restoring 15 clothes durability."));

            return list;
        }

        private static CardSO CreateOrUpdateCard(string id, string name, int ap, int cd, int dmg, int shield, int seduction, int repair, CardTarget target, string desc)
        {
            string path = $"Assets/Data/Combat/Cards/{id}.asset";
            CardSO so = AssetDatabase.LoadAssetAtPath<CardSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<CardSO>();
                AssetDatabase.CreateAsset(so, path);
            }

            so.id = id;
            so.cardName = name;
            so.apCost = ap;
            so.cooldownTurns = cd;
            so.hpDamage = dmg;
            so.shieldGain = shield;
            so.seductionAmount = seduction;
            so.clothesRepair = repair;
            so.target = target;
            so.description = desc;

            EditorUtility.SetDirty(so);
            return so;
        }

        // =====================================================================
        // ENEMY SKILLS GENERATOR
        // =====================================================================
        private static Dictionary<string, EnemySkillSO> GenerateSkills()
        {
            var dict = new Dictionary<string, EnemySkillSO>();

            // Slime skills
            dict["SlimeAcidSquirt"] = CreateOrUpdateSkill("Slime_AcidSquirt", "Acid Squirt", 0, 0, 0, 20, 0, 0, "Sprays concentrated digestive acid targeting player's garments!");
            dict["SlimeDissolve"] = CreateOrUpdateSkill("Slime_DissolvingGrasp", "Dissolving Grasp", 0, 0, 3, 25, 10, 0, "Grips tightly with corrosive gelatin, tearing at clothes and burning skin.");
            dict["SlimeTackle"] = CreateOrUpdateSkill("Slime_Tackle", "Slime Tackle", 0, 0, 7, 5, 0, 0, "Slams forcefully with dense gelatinous mass.");
            dict["SlimeEngulf"] = CreateOrUpdateSkill("Slime_WarmEngulf", "Warm Engulf", 5, 0, 4, 10, 35, 4, "Wraps the player in a warm, pulsing slime cocoon, overwhelming her senses.");

            // Succubus skills
            dict["SuccubusTailWhip"] = CreateOrUpdateSkill("Succubus_SpikedTailWhip", "Spiked Tail Whip", 0, 7, 5, 8, 0, 0, "Strikes with a barbed spade tail, harvesting vital mana from pain.");
            dict["SuccubusCharm"] = CreateOrUpdateSkill("Succubus_SensualKiss", "Sensual Kiss", 8, 0, 0, 0, 40, 0, "Blows a magical aphrodisiac kiss that sets the player's heart racing.");
            dict["SuccubusWhisper"] = CreateOrUpdateSkill("Succubus_LustWhisper", "Lust Whisper", 5, 0, 2, 0, 25, 0, "Whispers sinful promises into the player's ear, inflicting Flustered state.");
            dict["SuccubusEcstasy"] = CreateOrUpdateSkill("Succubus_ChamberOfEcstasy", "Chamber of Ecstasy", 14, 0, 0, 0, 60, 5, "Summons pink ethereal mists that induce uncontrollable pleasure.");

            return dict;
        }

        private static EnemySkillSO CreateOrUpdateSkill(string assetName, string name, int manaCost, int manaGain, int hpDmg, int clothesDmg, int sedDmg, int shield, string desc)
        {
            string path = $"Assets/Data/Combat/Skills/{assetName}.asset";
            EnemySkillSO so = AssetDatabase.LoadAssetAtPath<EnemySkillSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<EnemySkillSO>();
                AssetDatabase.CreateAsset(so, path);
            }

            so.skillName = name;
            so.manaCost = manaCost;
            so.manaGain = manaGain;
            so.hpDamage = hpDmg;
            so.clothesDamage = clothesDmg;
            so.seductionDamage = sedDmg;
            so.shieldGain = shield;
            so.description = desc;

            EditorUtility.SetDirty(so);
            return so;
        }

        // =====================================================================
        // ENEMY DEFINITIONS GENERATOR
        // =====================================================================
        private static List<EnemyDefinitionSO> GenerateEnemies(Dictionary<string, EnemySkillSO> skills)
        {
            List<EnemyDefinitionSO> list = new List<EnemyDefinitionSO>();

            // 1. Slime Girl
            string slimePath = "Assets/Data/Combat/Enemies/SlimeGirl.asset";
            EnemyDefinitionSO slime = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>(slimePath);
            if (slime == null)
            {
                slime = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
                AssetDatabase.CreateAsset(slime, slimePath);
            }
            slime.enemyType = EnemyType.SlimeGirl;
            slime.enemyName = "Slime Girl";
            slime.baseHp = 22;
            slime.hpPerFloor = 3;
            slime.maxMana = 15;
            slime.startingMana = 10;
            slime.maxSeductionStacks = 3;
            slime.skillPool.Clear();
            if (skills.ContainsKey("SlimeAcidSquirt")) slime.skillPool.Add(new WeightedEnemySkill { skill = skills["SlimeAcidSquirt"], weight = 45, condition = SkillCondition.OnlyIfPlayerHasClothes });
            if (skills.ContainsKey("SlimeDissolve")) slime.skillPool.Add(new WeightedEnemySkill { skill = skills["SlimeDissolve"], weight = 35, condition = SkillCondition.OnlyIfPlayerHasClothes });
            if (skills.ContainsKey("SlimeTackle")) slime.skillPool.Add(new WeightedEnemySkill { skill = skills["SlimeTackle"], weight = 20, condition = SkillCondition.Always });
            if (skills.ContainsKey("SlimeEngulf")) slime.skillPool.Add(new WeightedEnemySkill { skill = skills["SlimeEngulf"], weight = 40, condition = SkillCondition.OnlyIfHighSeduction });
            EditorUtility.SetDirty(slime);
            list.Add(slime);

            // 2. Succubus
            string succubPath = "Assets/Data/Combat/Enemies/Succubus.asset";
            EnemyDefinitionSO succubus = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>(succubPath);
            if (succubus == null)
            {
                succubus = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
                AssetDatabase.CreateAsset(succubus, succubPath);
            }
            succubus.enemyType = EnemyType.Succubus;
            succubus.enemyName = "Succubus";
            succubus.baseHp = 26;
            succubus.hpPerFloor = 4;
            succubus.maxMana = 30;
            succubus.startingMana = 20;
            succubus.maxSeductionStacks = 3;
            succubus.skillPool.Clear();
            if (skills.ContainsKey("SuccubusTailWhip")) succubus.skillPool.Add(new WeightedEnemySkill { skill = skills["SuccubusTailWhip"], weight = 30, condition = SkillCondition.Always });
            if (skills.ContainsKey("SuccubusCharm")) succubus.skillPool.Add(new WeightedEnemySkill { skill = skills["SuccubusCharm"], weight = 40, condition = SkillCondition.OnlyIfHighSeduction });
            if (skills.ContainsKey("SuccubusWhisper")) succubus.skillPool.Add(new WeightedEnemySkill { skill = skills["SuccubusWhisper"], weight = 30, condition = SkillCondition.Always });
            if (skills.ContainsKey("SuccubusEcstasy")) succubus.skillPool.Add(new WeightedEnemySkill { skill = skills["SuccubusEcstasy"], weight = 50, condition = SkillCondition.OnlyIfHighSeduction });
            EditorUtility.SetDirty(succubus);
            list.Add(succubus);

            return list;
        }

        // =====================================================================
        // DIALOGUES GENERATOR
        // =====================================================================
        private static DialogueDatabaseSO GenerateDialogues()
        {
            string dbPath = "Assets/Data/Dialogues/MainDialogueDatabase.asset";
            DialogueDatabaseSO db = AssetDatabase.LoadAssetAtPath<DialogueDatabaseSO>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<DialogueDatabaseSO>();
                AssetDatabase.CreateAsset(db, dbPath);
            }

            db.postCombatDialogues.Clear();
            db.eventDialogues.Clear();

            // Slime Post Combat
            var slimeVicHp0 = CreateDialogueSeq("slime_vic_hp_0", EnemyType.SlimeGirl, 0, new[]
            {
                (DialogueSpeaker.NPC, "Slime Girl", "Ow, ow, ow! How can your strikes hurt so much when my body is made of gelatin?!", "Normal", "annoyed"),
                (DialogueSpeaker.Player, "You", "Gelatin or not, you were blocking my passage. Had enough?", "Smug", "annoyed"),
                (DialogueSpeaker.NPC, "Slime Girl", "Eep! Put that blade away! I'm seeping into the stone cracks... you won't catch me!", "Normal", "suprised"),
                (DialogueSpeaker.Narrator, "Narrator", "The Slime Girl liquefied her lower half and slipped swiftly into a narrow subterranean fissure, leaving behind scattered loot.", "Smile", "suprised")
            });
            db.postCombatDialogues.Add(new PostCombatDialogueEntry { enemyType = EnemyType.SlimeGirl, resultType = CombatResultType.VictoryHp, phase = 0, sequence = slimeVicHp0 });
            db.postCombatDialogues.Add(new PostCombatDialogueEntry { enemyType = EnemyType.SlimeGirl, resultType = CombatResultType.VictoryHp, phase = 1, sequence = slimeVicHp0 });

            var slimeVicHp2 = CreateDialogueSeq("slime_vic_hp_2", EnemyType.SlimeGirl, 2, new[]
            {
                (DialogueSpeaker.NPC, "Slime Girl", "Aahnn... beaten down while burning from inside... every drop of my core is trembling...", "Normal", "lust_joy"),
                (DialogueSpeaker.Player, "You", "You pushed yourself to exhaustion. Can you even hold your shape anymore?", "Smile", "lust_joy"),
                (DialogueSpeaker.NPC, "Slime Girl", "M-My slime is so warm... if you touch me again right now, I'll melt into sweet syrup...", "Normal", "lust_suprised"),
                (DialogueSpeaker.Narrator, "Narrator", "The dazed Slime Girl puddles helplessly at your boots, completely drained of fighting will.", "Smug", "lust_suprised")
            });
            db.postCombatDialogues.Add(new PostCombatDialogueEntry { enemyType = EnemyType.SlimeGirl, resultType = CombatResultType.VictoryHp, phase = 2, sequence = slimeVicHp2 });

            var slimeVicSed = CreateDialogueSeq("slime_vic_sed", EnemyType.SlimeGirl, 2, new[]
            {
                (DialogueSpeaker.Player, "You", "Look at you... completely pliant and trembling under my gentle touch.", "Smug", "lust_joy"),
                (DialogueSpeaker.NPC, "Slime Girl", "Aahh~! Y-You stroked my sensitive core membrane! I-I can't maintain my form...", "Blush", "lust_joy"),
                (DialogueSpeaker.Player, "You", "Will you yield the way peacefully now, sweet puddle?", "Smile", "lust_joy"),
                (DialogueSpeaker.NPC, "Slime Girl", "Y-Yes! Take whatever treasure you want! Just... don't tease my core anymore today~!", "Lustful", "lust_suprised")
            });
            db.postCombatDialogues.Add(new PostCombatDialogueEntry { enemyType = EnemyType.SlimeGirl, resultType = CombatResultType.VictorySeduction, phase = 2, sequence = slimeVicSed });

            // Succubus Post Combat
            var succubVicHp0 = CreateDialogueSeq("succub_vic_hp_0", EnemyType.Succubus, 0, new[]
            {
                (DialogueSpeaker.NPC, "Succubus", "Tch! Such barbaric roughness! Do mortals know no romance beyond blunt iron?", "Angry", "annoyed"),
                (DialogueSpeaker.Player, "You", "Your cheap charms don't work on me, demon. Step aside or face true steel.", "Smug", "annoyed"),
                (DialogueSpeaker.NPC, "Succubus", "Hmph! Such an unrefined brute. I shall retreat to my velvet boudoir to nurse my bruises.", "Normal", "smile")
            });
            db.postCombatDialogues.Add(new PostCombatDialogueEntry { enemyType = EnemyType.Succubus, resultType = CombatResultType.VictoryHp, phase = 0, sequence = succubVicHp0 });
            db.postCombatDialogues.Add(new PostCombatDialogueEntry { enemyType = EnemyType.Succubus, resultType = CombatResultType.VictoryHp, phase = 1, sequence = succubVicHp0 });

            var succubVicSed = CreateDialogueSeq("succub_vic_sed", EnemyType.Succubus, 2, new[]
            {
                (DialogueSpeaker.Player, "You", "It seems the temptress became the captive of her own desires.", "Smug", "lust_joy"),
                (DialogueSpeaker.NPC, "Succubus", "Haa... haa... unbelievable... outmatched in the arts of pleasure by a mere mortal girl...", "Blush", "lust_estasy"),
                (DialogueSpeaker.Player, "You", "Who commands this sanctuary chamber now, demon?", "Smile", "lust_joy"),
                (DialogueSpeaker.NPC, "Succubus", "You do, mistress... my pride is thoroughly shattered. Please, return whenever you wish to play again~", "Lustful", "lust_intence")
            });
            db.postCombatDialogues.Add(new PostCombatDialogueEntry { enemyType = EnemyType.Succubus, resultType = CombatResultType.VictorySeduction, phase = 2, sequence = succubVicSed });

            // Dungeon Events
            var eventSpring = CreateDialogueSeq("event_spring", EnemyType.SlimeGirl, 0, new[]
            {
                (DialogueSpeaker.Narrator, "Mystic Spring", "You discover a secluded cavern grotto where warm, translucent azure liquid gently bubbles into a crystalline basin.", "Normal", "smile"),
                (DialogueSpeaker.NPC, "Slime Maiden", "Hehe! Hello traveler! Looking for a quick dip? The minerals here soothe weary muscles and melt away fatigue!", "Smile", "smile"),
                (DialogueSpeaker.Player, "You", "It looks peaceful. A momentary respite in these dark depths is a rare blessing.", "Smile", "smile"),
                (DialogueSpeaker.NPC, "Slime Maiden", "Go on, splash some on your face! It tingles with rejuvenating mana! Just don't drink too much, or you'll get bouncy!", "Delighted", "grin"),
                (DialogueSpeaker.Narrator, "Mystic Spring", "You bathed your face and hands in the warm waters, feeling vitality seep back into your body (+15 HP, clothes refreshed).", "Delighted", "smile")
            }, isEvent: true);
            db.eventDialogues.Add(new EventDialogueEntry { eventId = "Underground Spring", sequence = eventSpring });

            var eventStele = CreateDialogueSeq("event_stele", EnemyType.Succubus, 0, new[]
            {
                (DialogueSpeaker.Narrator, "Ancient Stele", "An ancient basalt monolith looms before you, pulsating with crimson demonic inscriptions of a forgotten age.", "Normal", "smile"),
                (DialogueSpeaker.NPC, "Abyssal Echo", "Mmm... drawn to forbidden knowledge, little moth? The sigils etched here whisper of boundless power and carnal secrets.", "Shocked", "smile"),
                (DialogueSpeaker.Player, "You", "Who's there?! An apparition? The whispers... they resonate directly inside my mind.", "Shocked", "smile"),
                (DialogueSpeaker.NPC, "Abyssal Echo", "Just an echo of desire left behind in stone. Dare you trace the glyph with your bare finger?", "Smile", "grin"),
                (DialogueSpeaker.Player, "You", "I will decipher your arcane secrets without succumbing to your deceit.", "Smug", "annoyed"),
                (DialogueSpeaker.Narrator, "Ancient Stele", "Deciphering the cryptic demonic runes granted you deep arcane focus and insight (+35 Gold, Mana replenished).", "Normal", "grin")
            }, isEvent: true);
            db.eventDialogues.Add(new EventDialogueEntry { eventId = "Ancient Stele", sequence = eventStele });

            EditorUtility.SetDirty(db);
            return db;
        }

        private static DialogueSequenceSO CreateDialogueSeq(string id, EnemyType enemy, int phase, (DialogueSpeaker spk, string name, string txt, string pExpr, string eEmo)[] lines, bool isEvent = false)
        {
            string subDir = isEvent ? "Events" : "PostCombat";
            string path = $"Assets/Data/Dialogues/{subDir}/{id}.asset";

            DialogueSequenceSO so = AssetDatabase.LoadAssetAtPath<DialogueSequenceSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<DialogueSequenceSO>();
                AssetDatabase.CreateAsset(so, path);
            }

            so.sequenceId = id;
            so.enemyType = enemy;
            so.requiredCombatPhase = phase;
            so.lines.Clear();

            foreach (var l in lines)
            {
                so.lines.Add(new DialogueLineData
                {
                    speaker = l.spk,
                    speakerName = l.name,
                    text = l.txt,
                    playerExpression = l.pExpr,
                    enemyEmotion = l.eEmo
                });
            }

            EditorUtility.SetDirty(so);
            return so;
        }
    }
}
#endif
