using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnbridledSanctum.Combat
{
    [System.Serializable]
    public class EnemySpriteSet
    {
        public EnemyType enemyType;

        [Header("Combat Phase Sprites (3 Phases)")]
        public Sprite phase0; // Phase 1 (0 stacks)
        public Sprite phase1; // Phase 2 (1 stack)
        public Sprite phase2; // Phase 3 (2+ stacks / peak charm)

        [Header("Dialogue Emotions - Phase 1 & 2 (Clean / Regular)")]
        public Sprite smile;
        public Sprite grin;
        public Sprite annoyed;
        public Sprite suprised;
        public Sprite lustful; // Special for Slime

        [Header("Dialogue Emotions - Phase 3 (Lust Expressions)")]
        public Sprite lustJoy;
        public Sprite lustSuprised;
        public Sprite lustIntence;
        public Sprite lustEstasy;

        public Sprite GetCombatSprite(int seductionStacks, bool isDefeated)
        {
            if (seductionStacks <= 0) return phase0 != null ? phase0 : phase1;
            if (seductionStacks == 1) return phase1 != null ? phase1 : phase0;
            return phase2 != null ? phase2 : (phase1 != null ? phase1 : phase0);
        }

        public Sprite GetDialogueSprite(int seductionStacks, string emotionKey)
        {
            string key = string.IsNullOrEmpty(emotionKey) ? "default" : emotionKey.Trim().ToLowerInvariant();
            bool isPhase3 = seductionStacks >= 2;

            if (!isPhase3)
            {
                // PHASE 1 & 2: Prohibited: phase2, and any lust_* expressions. Allowed: phase0, smile, grin, annoyed, suprised, lustful
                switch (key)
                {
                    case "smile": return smile != null ? smile : phase0;
                    case "grin": return grin != null ? grin : phase0;
                    case "annoyed": return annoyed != null ? annoyed : phase0;
                    case "suprised":
                    case "surprised": return suprised != null ? suprised : phase0;
                    case "lustful": return lustful != null ? lustful : (smile != null ? smile : phase0);
                    case "phase0":
                    case "default":
                    default:
                        return phase0 != null ? phase0 : (smile != null ? smile : phase1);
                }
            }
            else
            {
                // PHASE 3: Allowed: phase2 and x_lust_x expressions
                switch (key)
                {
                    case "lust_joy":
                    case "joy":
                        return lustJoy != null ? lustJoy : (phase2 != null ? phase2 : phase0);

                    case "lust_suprised":
                    case "lust_surprised":
                    case "suprised":
                    case "surprised":
                        return lustSuprised != null ? lustSuprised : (lustJoy != null ? lustJoy : phase2);

                    case "lust_intence":
                    case "lust_intense":
                    case "intense":
                        return lustIntence != null ? lustIntence : (lustJoy != null ? lustJoy : phase2);

                    case "lust_estasy":
                    case "lust_ecstasy":
                    case "ecstasy":
                        return lustEstasy != null ? lustEstasy : (lustIntence != null ? lustIntence : phase2);

                    case "phase2":
                    case "phase3":
                    case "default":
                    default:
                        return phase2 != null ? phase2 : (lustJoy != null ? lustJoy : phase0);
                }
            }
        }
    }

    [System.Serializable]
    public class ModularEnemySpriteSet
    {
        public EnemyType enemyType = EnemyType.ElvenScout;
        public Sprite baseBody;
        public Sprite upperClothes;
        public Sprite lowerClothes;
    }

    [CreateAssetMenu(fileName = "EnemySpriteDatabase", menuName = "Game/Enemy Sprite Database")]
    public class EnemySpriteDatabase : ScriptableObject
    {
        private static EnemySpriteDatabase _instance;
        public static EnemySpriteDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<EnemySpriteDatabase>("EnemySpriteDatabase");
#if UNITY_EDITOR
                    if (_instance == null)
                    {
                        _instance = AssetDatabase.LoadAssetAtPath<EnemySpriteDatabase>("Assets/Settings/EnemySpriteDatabase.asset");
                    }
#endif
                }
                return _instance;
            }
            set => _instance = value;
        }

        [Header("Enemy Sets")]
        public EnemySpriteSet slimeGirl = new EnemySpriteSet { enemyType = EnemyType.SlimeGirl };
        public EnemySpriteSet succubus = new EnemySpriteSet { enemyType = EnemyType.Succubus };

        [Header("Modular Paper Doll Enemy Sets")]
        public ModularEnemySpriteSet elvenScout = new ModularEnemySpriteSet { enemyType = EnemyType.ElvenScout };

        public EnemySpriteSet GetSet(EnemyType type)
        {
            if (type == EnemyType.SlimeGirl) return slimeGirl;
            if (type == EnemyType.Succubus) return succubus;
            return null;
        }

        public bool IsModularEnemy(EnemyType type)
        {
            return type == EnemyType.ElvenScout;
        }

        public bool TryGetModularSprites(EnemyType type, out Sprite baseSprite, out Sprite topSprite, out Sprite bottomSprite)
        {
            if (type == EnemyType.ElvenScout && elvenScout != null && elvenScout.baseBody != null)
            {
                baseSprite = elvenScout.baseBody;
                topSprite = elvenScout.upperClothes;
                bottomSprite = elvenScout.lowerClothes;
                return true;
            }
            baseSprite = null;
            topSprite = null;
            bottomSprite = null;
            return false;
        }

        public Sprite GetCombatSprite(EnemyType type, int seductionStacks, bool isDefeated)
        {
            if (type == EnemyType.ElvenScout && elvenScout != null)
            {
                return elvenScout.baseBody;
            }
            EnemySpriteSet set = GetSet(type);
            return set != null ? set.GetCombatSprite(seductionStacks, isDefeated) : null;
        }

        public Sprite GetDialogueSprite(EnemyType type, int seductionStacks, string emotionKey)
        {
            if (type == EnemyType.ElvenScout && elvenScout != null)
            {
                return elvenScout.baseBody;
            }
            EnemySpriteSet set = GetSet(type);
            return set != null ? set.GetDialogueSprite(seductionStacks, emotionKey) : null;
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Populate From Sprites Folder")]
        public void AutoPopulate()
        {
            PopulateSlime();
            PopulateSuccubus();
            PopulateScout();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log("[EnemySpriteDatabase] Auto-populated successfully.");
        }

        private void PopulateScout()
        {
            elvenScout.enemyType = EnemyType.ElvenScout;
            elvenScout.baseBody = LoadSprite("Assets/sprites/Characters/Scout/scout_base.png");
            elvenScout.upperClothes = LoadSprite("Assets/sprites/Characters/Scout/scout_top.png");
            elvenScout.lowerClothes = LoadSprite("Assets/sprites/Characters/Scout/scout_bottom.png");
        }

        private void PopulateSlime()
        {
            slimeGirl.enemyType = EnemyType.SlimeGirl;
            slimeGirl.phase0 = LoadSprite("Assets/sprites/Characters/Slime0/Slime_phase0.png");
            slimeGirl.phase1 = LoadSprite("Assets/sprites/Characters/Slime0/Slime_phase1.png");
            slimeGirl.phase2 = LoadSprite("Assets/sprites/Characters/Slime0/Slime_phase2.png");
            slimeGirl.smile = LoadSprite("Assets/sprites/Characters/Slime0/Slime_smile.png");
            slimeGirl.grin = LoadSprite("Assets/sprites/Characters/Slime0/Slime_grin.png");
            slimeGirl.annoyed = LoadSprite("Assets/sprites/Characters/Slime0/Slime_annoyed.png");
            slimeGirl.suprised = LoadSprite("Assets/sprites/Characters/Slime0/Slime_lust_suprised.png");
            slimeGirl.lustful = LoadSprite("Assets/sprites/Characters/Slime0/Slime_lustful.png");
            slimeGirl.lustJoy = LoadSprite("Assets/sprites/Characters/Slime0/Slime_lust_joy.png");
            slimeGirl.lustSuprised = LoadSprite("Assets/sprites/Characters/Slime0/Slime_lust_suprised.png");
        }

        private void PopulateSuccubus()
        {
            succubus.enemyType = EnemyType.Succubus;
            succubus.phase0 = LoadSprite("Assets/sprites/Characters/Succub0/Succub_phase0.png");
            succubus.phase1 = LoadSprite("Assets/sprites/Characters/Succub0/Succub_phase1.png");
            succubus.phase2 = LoadSprite("Assets/sprites/Characters/Succub0/Succub_phase2.png");
            succubus.smile = LoadSprite("Assets/sprites/Characters/Succub0/Succub_smile.png");
            succubus.grin = LoadSprite("Assets/sprites/Characters/Succub0/Succub_grin.png");
            succubus.annoyed = LoadSprite("Assets/sprites/Characters/Succub0/Succub_annoyed.png");
            succubus.suprised = LoadSprite("Assets/sprites/Characters/Succub0/Succub_suprised.png");
            succubus.lustJoy = LoadSprite("Assets/sprites/Characters/Succub0/Succub_lust_joy.png");
            succubus.lustIntence = LoadSprite("Assets/sprites/Characters/Succub0/Succub_lust_intence.png");
            succubus.lustEstasy = LoadSprite("Assets/sprites/Characters/Succub0/Succub_lust_estasy.png");
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
#endif
    }
}
