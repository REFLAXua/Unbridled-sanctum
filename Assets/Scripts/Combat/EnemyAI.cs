using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnbridledSanctum.Combat
{
    public static class EnemyAI
    {
        // =========================================================================
        // SLIME GIRL SKILLS
        // =========================================================================

        // --- Slime Girl: Neutral Stance (Skirmish, Acid Spray, Ensnaring Pounce) ---
        private static readonly EnemySkill SlimeAcidTop = new EnemySkill(
            "Acid Spray (Top)", manaCost: 0, hpDmg: 0, clothesDmg: 20, seductionDmg: 0, shieldGain: 0, manaGain: 0,
            desc: "Sprays concentrated digestive acid targeting your upper garment!",
            armorTarget: ArmorTargetSlot.Upper, staminaDmg: 4, pranaGain: 0, stanceReq: CombatStance.Neutral
        );

        private static readonly EnemySkill SlimeGelatinBottom = new EnemySkill(
            "Gelatin Trap (Bottom)", manaCost: 0, hpDmg: 2, clothesDmg: 20, seductionDmg: 5, shieldGain: 0, manaGain: 0,
            desc: "Entangles your legs and dissolves your lower garments with acidic goo!",
            armorTarget: ArmorTargetSlot.Lower, staminaDmg: 6, pranaGain: 0, stanceReq: CombatStance.Neutral
        );

        private static readonly EnemySkill SlimeCorrosiveAll = new EnemySkill(
            "Corrosive Surge (All)", manaCost: 2, hpDmg: 3, clothesDmg: 25, seductionDmg: 10, shieldGain: 0, manaGain: 0,
            desc: "A massive wave of acidic slime erodes all armor pieces simultaneously!",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 8, pranaGain: 0, stanceReq: CombatStance.Neutral
        );

        private static readonly EnemySkill SlimeTackle = new EnemySkill(
            "Slime Tackle", manaCost: 0, hpDmg: 7, clothesDmg: 0, seductionDmg: 0, shieldGain: 0, manaGain: 0,
            desc: "Slams forcefully with dense gelatinous mass.",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 6, pranaGain: 0, stanceReq: CombatStance.Neutral
        );

        private static readonly EnemySkill SlimeEngulf = new EnemySkill(
            "Warm Engulf", manaCost: 5, hpDmg: 4, clothesDmg: 10, seductionDmg: 35, shieldGain: 4, manaGain: 0,
            desc: "Wraps the player in a warm, pulsing slime cocoon, overwhelming her senses.",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 10, pranaGain: 0, stanceReq: CombatStance.Neutral
        );

        private static readonly EnemySkill SlimeAdhesivePounce = new EnemySkill(
            "Adhesive Pounce", manaCost: 3, hpDmg: 2, clothesDmg: 15, seductionDmg: 0, shieldGain: 0, manaGain: 0,
            desc: "Leaps forward with sticky pseudopods, dragging your vessel to the floor into a grapple hold!",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 14, pranaGain: 0, stanceReq: CombatStance.Neutral,
            transitionTo: CombatStance.Grappled
        );

        // --- Slime Girl: Grappled Stance (Bodily Pinning, Stripping, Forcing Union) ---
        private static readonly EnemySkill SlimeAdhesivePin = new EnemySkill(
            "Adhesive Pin", manaCost: 0, hpDmg: 0, clothesDmg: 10, seductionDmg: 20, shieldGain: 3, manaGain: 5,
            desc: "Pins your limbs to the floor with heavy gelatinous weight, exhausting your stamina.",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 12, pranaGain: 0, stanceReq: CombatStance.Grappled
        );

        private static readonly EnemySkill SlimeDissolveLower = new EnemySkill(
            "Targeted Lower Dissolution", manaCost: 1, hpDmg: 0, clothesDmg: 35, seductionDmg: 20, shieldGain: 0, manaGain: 0,
            desc: "Concentrates intense acidic secretions directly against your lower garments to expose your vessel!",
            armorTarget: ArmorTargetSlot.Lower, staminaDmg: 6, pranaGain: 0, stanceReq: CombatStance.Grappled
        );

        private static readonly EnemySkill SlimeSensualCaress = new EnemySkill(
            "Gelatinous Caress", manaCost: 2, hpDmg: 0, clothesDmg: 5, seductionDmg: 30, shieldGain: 0, manaGain: 4,
            desc: "Soft translucent pseudopods massage your sensitive curves, sending electrical pleasure through your neural core.",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 5, pranaGain: 0, stanceReq: CombatStance.Grappled
        );

        // --- Slime Girl: Sensual & Non-penetrative sex moves (Neutral / Grappled) ---
        private static readonly EnemySkill SlimeFapToFace = new EnemySkill(
            "Fap to Face", manaCost: 1, hpDmg: 0, clothesDmg: 0, seductionDmg: 25, shieldGain: 0, manaGain: 0,
            desc: "Violently strokes its gelatinous member while staring directly into your eyes, splattering sticky fluids toward your face!",
            armorTarget: ArmorTargetSlot.Upper, staminaDmg: 8, pranaGain: 0, stanceReq: CombatStance.Neutral,
            transitionTo: null, selfArousal: 20
        );

        private static readonly EnemySkill SlimeRubPussy = new EnemySkill(
            "Rub Against Pussy", manaCost: 2, hpDmg: 0, clothesDmg: 20, seductionDmg: 30, shieldGain: 0, manaGain: 0,
            desc: "Grinds its throbbing member repeatedly against your outer vulva, melting fabric or heating bare skin with searing friction!",
            armorTarget: ArmorTargetSlot.Lower, staminaDmg: 10, pranaGain: 0, stanceReq: CombatStance.Grappled,
            transitionTo: null, selfArousal: 15
        );

        private static readonly EnemySkill SlimeBreastFondle = new EnemySkill(
            "Breast Fondle & Grind", manaCost: 2, hpDmg: 0, clothesDmg: 20, seductionDmg: 30, shieldGain: 0, manaGain: 0,
            desc: "Kneads and pinches your sensitive breasts with warm pseudopods, wearing down your top and overwhelming your senses!",
            armorTarget: ArmorTargetSlot.Upper, staminaDmg: 10, pranaGain: 0, stanceReq: CombatStance.Grappled,
            transitionTo: null, selfArousal: 15
        );

        private static readonly EnemySkill SlimeThighGrind = new EnemySkill(
            "Thigh Grinding", manaCost: 1, hpDmg: 0, clothesDmg: 10, seductionDmg: 25, shieldGain: 3, manaGain: 0,
            desc: "Slides forcefully between your clamped thighs, lubricating your legs and testing your posture balance.",
            armorTarget: ArmorTargetSlot.Lower, staminaDmg: 12, pranaGain: 0, stanceReq: CombatStance.Grappled,
            transitionTo: null, selfArousal: 15
        );

        // --- Slime Girl: Penetration Influx Skills (3 Dedicated Slots: Vagina, Sphincter, Mouth) ---
        private static readonly EnemySkill SlimeForceVagina = new EnemySkill(
            "Slime Vaginal Penetration", manaCost: 4, hpDmg: 0, clothesDmg: 15, seductionDmg: 30, shieldGain: 5, manaGain: 0,
            desc: "Slams its core flush against your exposed groin, thrusting a dense gelatinous phallus deep inside your vessel's synthetic womb!",
            armorTarget: ArmorTargetSlot.Lower, staminaDmg: 10, pranaGain: 0, stanceReq: CombatStance.Grappled,
            transitionTo: CombatStance.Penetrated, selfArousal: 0, targetSlot: PenetrationSlot.Vagina
        );

        private static readonly EnemySkill SlimeForceAnal = new EnemySkill(
            "Slime Anal Influx", manaCost: 4, hpDmg: 0, clothesDmg: 15, seductionDmg: 30, shieldGain: 5, manaGain: 0,
            desc: "Wedges a sleek, pulsing pseudopod directly into your tight anal tract, filling your lower tract with warm pressurized slime!",
            armorTarget: ArmorTargetSlot.Lower, staminaDmg: 10, pranaGain: 0, stanceReq: CombatStance.Grappled,
            transitionTo: CombatStance.Penetrated, selfArousal: 0, targetSlot: PenetrationSlot.Anal
        );
        private static readonly EnemySkill SlimeForceSphincter = SlimeForceAnal;

        private static readonly EnemySkill SlimeForceMouth = new EnemySkill(
            "Slime Oral Engulfment", manaCost: 3, hpDmg: 0, clothesDmg: 5, seductionDmg: 25, shieldGain: 3, manaGain: 0,
            desc: "Forces a thick gelatinous tendril between your lips, locking your jaw open and surging into your throat!",
            armorTarget: ArmorTargetSlot.Upper, staminaDmg: 8, pranaGain: 0, stanceReq: CombatStance.Grappled,
            transitionTo: CombatStance.Penetrated, selfArousal: 0, targetSlot: PenetrationSlot.Mouth
        );

        // --- Slime Girl: Penetrated Stance (Slot-specific sex actions) ---
        // Vaginal
        private static readonly EnemySkill SlimeRhythmicPumping = new EnemySkill(
            "Rhythmic Gel Pumping", manaCost: 0, hpDmg: 0, clothesDmg: 0, seductionDmg: 25, shieldGain: 0, manaGain: 5,
            desc: "Pumps rhythmically deep inside your vessel, sending warm ripples of pleasure across your consciousness!",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 4, pranaGain: 0, stanceReq: CombatStance.Penetrated,
            transitionTo: null, selfArousal: 15
        );

        private static readonly EnemySkill SlimeInternalMassage = new EnemySkill(
            "Internal Pseudopod Massage", manaCost: 3, hpDmg: 0, clothesDmg: 0, seductionDmg: 35, shieldGain: 4, manaGain: 8,
            desc: "Internal slime membranes expand and stimulate your synthetic womb sensors with intense pressure!",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 8, pranaGain: 0, stanceReq: CombatStance.Penetrated,
            transitionTo: null, selfArousal: 22
        );

        // Anal
        private static readonly EnemySkill SlimeSphincterPumping = new EnemySkill(
            "Anal Dilation Thrusts", manaCost: 0, hpDmg: 0, clothesDmg: 0, seductionDmg: 25, shieldGain: 0, manaGain: 5,
            desc: "Expands rhythmically within your tight anal tract, stretching your synthetic nerves with deep pleasure!",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 5, pranaGain: 0, stanceReq: CombatStance.Penetrated,
            transitionTo: null, selfArousal: 16
        );

        private static readonly EnemySkill SlimeSphincterMassage = new EnemySkill(
            "Pulsing Core Expansion", manaCost: 0, hpDmg: 0, clothesDmg: 0, seductionDmg: 35, shieldGain: 3, manaGain: 0,
            desc: "Thick gelatin expands within your anal walls, creating undulating waves of deep rectal stimulation.",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 7, pranaGain: 0, stanceReq: CombatStance.Penetrated,
            transitionTo: null, selfArousal: 20
        );

        // Oral
        private static readonly EnemySkill SlimeOralPumping = new EnemySkill(
            "Throat Influx Pumping", manaCost: 0, hpDmg: 0, clothesDmg: 0, seductionDmg: 25, shieldGain: 0, manaGain: 5,
            desc: "Pumps undulating gelatinous tendrils deep down your throat, muffling your groans into wet gurgles!",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 5, pranaGain: 0, stanceReq: CombatStance.Penetrated,
            transitionTo: null, selfArousal: 16, targetSlot: PenetrationSlot.Mouth
        );

        private static readonly EnemySkill SlimeOralSwirl = new EnemySkill(
            "Gelatinous Throat Swirl", manaCost: 0, hpDmg: 0, clothesDmg: 0, seductionDmg: 30, shieldGain: 2, manaGain: 0,
            desc: "Swirls smooth viscous tendrils around your tongue and deep against your uvula, drawing muffled sighs.",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 6, pranaGain: 0, stanceReq: CombatStance.Penetrated,
            transitionTo: null, selfArousal: 18, targetSlot: PenetrationSlot.Mouth
        );


        // =========================================================================
        // SUCCUBUS SKILLS
        // =========================================================================

        // --- Succubus: Neutral Stance (Talons, Barbed Tail, Aphrodisiac Charms) ---
        private static readonly EnemySkill SuccubusClawsTop = new EnemySkill(
            "Shadow Claws (Top)", manaCost: 0, hpDmg: 4, clothesDmg: 16, seductionDmg: 0, shieldGain: 0, manaGain: 5,
            desc: "Rakes razor-sharp demonic talons across your chest, tearing upper garments!",
            armorTarget: ArmorTargetSlot.Upper, staminaDmg: 5, pranaGain: 0, stanceReq: CombatStance.Neutral
        );

        private static readonly EnemySkill SuccubusTailBottom = new EnemySkill(
            "Spiked Tail Sweep (Bottom)", manaCost: 0, hpDmg: 4, clothesDmg: 16, seductionDmg: 0, shieldGain: 0, manaGain: 7,
            desc: "Sweeps a barbed spade tail across your legs, shredding lower garments!",
            armorTarget: ArmorTargetSlot.Lower, staminaDmg: 5, pranaGain: 0, stanceReq: CombatStance.Neutral
        );

        private static readonly EnemySkill SuccubusAbyssalLashAll = new EnemySkill(
            "Abyssal Lash (All)", manaCost: 4, hpDmg: 6, clothesDmg: 22, seductionDmg: 5, shieldGain: 0, manaGain: 0,
            desc: "Strikes with dark abyssal fury, damaging all worn armor simultaneously!",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 8, pranaGain: 0, stanceReq: CombatStance.Neutral
        );

        private static readonly EnemySkill SuccubusCharm = new EnemySkill(
            "Sensual Kiss", manaCost: 8, hpDmg: 0, clothesDmg: 0, seductionDmg: 40, shieldGain: 0, manaGain: 0,
            desc: "Blows a magical aphrodisiac kiss that sets your heart racing.",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 6, pranaGain: 0, stanceReq: CombatStance.Neutral
        );

        private static readonly EnemySkill SuccubusWhisper = new EnemySkill(
            "Lust Whisper", manaCost: 5, hpDmg: 2, clothesDmg: 0, seductionDmg: 25, shieldGain: 0, manaGain: 0,
            desc: "Whispers sinful promises into your ear, inflicting Flustered state.",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 4, pranaGain: 0, stanceReq: CombatStance.Neutral
        );

        private static readonly EnemySkill SuccubusEcstasy = new EnemySkill(
            "Chamber of Ecstasy", manaCost: 14, hpDmg: 0, clothesDmg: 0, seductionDmg: 60, shieldGain: 5, manaGain: 0,
            desc: "Summons pink ethereal mists that induce uncontrollable pleasure.",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 8, pranaGain: 0, stanceReq: CombatStance.Neutral
        );

        private static readonly EnemySkill SuccubusDemonicPounce = new EnemySkill(
            "Demonic Pounce", manaCost: 4, hpDmg: 3, clothesDmg: 12, seductionDmg: 0, shieldGain: 0, manaGain: 0,
            desc: "Swoops down from the air, knocking you flat onto your back and pinning your limbs!",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 14, pranaGain: 0, stanceReq: CombatStance.Neutral,
            transitionTo: CombatStance.Grappled
        );

        // --- Succubus: Grappled Stance (Pinning, Stripping, Dominating) ---
        private static readonly EnemySkill SuccubusWristPin = new EnemySkill(
            "Dominant Wrist Pin", manaCost: 0, hpDmg: 0, clothesDmg: 0, seductionDmg: 20, shieldGain: 0, manaGain: 5,
            desc: "Pins your wrists firmly against the ground with dark energy, smirking down at you.",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 12, pranaGain: 0, stanceReq: CombatStance.Grappled
        );

        private static readonly EnemySkill SuccubusTailStrip = new EnemySkill(
            "Barbed Tail Strip", manaCost: 2, hpDmg: 0, clothesDmg: 35, seductionDmg: 20, shieldGain: 0, manaGain: 5,
            desc: "Her flexible tail snakes between your thighs and tears your lower clothes completely away!",
            armorTarget: ArmorTargetSlot.Lower, staminaDmg: 6, pranaGain: 0, stanceReq: CombatStance.Grappled
        );

        private static readonly EnemySkill SuccubusNeckBite = new EnemySkill(
            "Sensual Neck Bite", manaCost: 3, hpDmg: 0, clothesDmg: 0, seductionDmg: 35, shieldGain: 0, manaGain: 10,
            desc: "Bites gently into your exposed collarbone, sipping your life breath while fondling your body.",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 6, pranaGain: 0, stanceReq: CombatStance.Grappled
        );

        // --- Succubus: Sensual & Non-penetrative sex moves (Neutral / Grappled) ---
        private static readonly EnemySkill SuccubusFapToFace = new EnemySkill(
            "Fap to Face", manaCost: 1, hpDmg: 0, clothesDmg: 0, seductionDmg: 30, shieldGain: 0, manaGain: 0,
            desc: "Strokes her demonic futanari cock with teasing smirk, aiming pre-cum droplets directly toward your lips and face!",
            armorTarget: ArmorTargetSlot.Upper, staminaDmg: 8, pranaGain: 0, stanceReq: CombatStance.Neutral,
            transitionTo: null, selfArousal: 25
        );

        private static readonly EnemySkill SuccubusRubPussy = new EnemySkill(
            "Rub Against Pussy", manaCost: 2, hpDmg: 0, clothesDmg: 25, seductionDmg: 35, shieldGain: 0, manaGain: 0,
            desc: "Grinds her hot demonic shaft against your clitoris and labia, teasing your entrance without breaching inside!",
            armorTarget: ArmorTargetSlot.Lower, staminaDmg: 12, pranaGain: 0, stanceReq: CombatStance.Grappled,
            transitionTo: null, selfArousal: 20
        );

        private static readonly EnemySkill SuccubusBreastFondle = new EnemySkill(
            "Breast Fondle & Grind", manaCost: 2, hpDmg: 0, clothesDmg: 25, seductionDmg: 35, shieldGain: 0, manaGain: 0,
            desc: "Firmly cups and squeezes your chest while grinding against your torso, teasing your nipples into rigid peaks!",
            armorTarget: ArmorTargetSlot.Upper, staminaDmg: 10, pranaGain: 0, stanceReq: CombatStance.Grappled,
            transitionTo: null, selfArousal: 20
        );

        private static readonly EnemySkill SuccubusThighGrind = new EnemySkill(
            "Thigh Grinding", manaCost: 1, hpDmg: 0, clothesDmg: 15, seductionDmg: 30, shieldGain: 4, manaGain: 0,
            desc: "Pumps her throbbing length snugly between your clamped thighs, generating intense erotic friction.",
            armorTarget: ArmorTargetSlot.Lower, staminaDmg: 14, pranaGain: 0, stanceReq: CombatStance.Grappled,
            transitionTo: null, selfArousal: 20
        );

        // --- Succubus: Penetration Skills (3 Dedicated Slots: Vagina, Sphincter, Mouth) ---
        private static readonly EnemySkill SuccubusMountVagina = new EnemySkill(
            "Demonic Vaginal Straddle", manaCost: 6, hpDmg: 0, clothesDmg: 20, seductionDmg: 35, shieldGain: 5, manaGain: 0,
            desc: "Mounts your hips, aligns her barbed futanari shaft with your entrance, and impales your synthetic womb in deep union!",
            armorTarget: ArmorTargetSlot.Lower, staminaDmg: 12, pranaGain: 0, stanceReq: CombatStance.Grappled,
            transitionTo: CombatStance.Penetrated, selfArousal: 0, targetSlot: PenetrationSlot.Vagina
        );

        private static readonly EnemySkill SuccubusTailAnal = new EnemySkill(
            "Barbed Tail Anal Breach", manaCost: 5, hpDmg: 0, clothesDmg: 20, seductionDmg: 35, shieldGain: 5, manaGain: 0,
            desc: "Curves her smooth prehensile tail down and penetrates your tight anal tract with exquisite demonic pressure!",
            armorTarget: ArmorTargetSlot.Lower, staminaDmg: 12, pranaGain: 0, stanceReq: CombatStance.Grappled,
            transitionTo: CombatStance.Penetrated, selfArousal: 0, targetSlot: PenetrationSlot.Anal
        );
        private static readonly EnemySkill SuccubusTailSphincter = SuccubusTailAnal;

        private static readonly EnemySkill SuccubusForceMouth = new EnemySkill(
            "Demonic Throat Domination", manaCost: 4, hpDmg: 0, clothesDmg: 0, seductionDmg: 30, shieldGain: 4, manaGain: 0,
            desc: "Pins your head back by your hair and plunges her demonic member deep into your gasping mouth!",
            armorTarget: ArmorTargetSlot.Upper, staminaDmg: 10, pranaGain: 0, stanceReq: CombatStance.Grappled,
            transitionTo: CombatStance.Penetrated, selfArousal: 0, targetSlot: PenetrationSlot.Mouth
        );

        // --- Succubus: Penetrated Stance Actions ---
        // Vaginal
        private static readonly EnemySkill SuccubusAbyssalThrusts = new EnemySkill(
            "Abyssal Thrusts", manaCost: 0, hpDmg: 0, clothesDmg: 0, seductionDmg: 30, shieldGain: 0, manaGain: 8,
            desc: "Grinds her hips relentlessly, delivering intense rhythmic thrusts that rattle your core!",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 6, pranaGain: 0, stanceReq: CombatStance.Penetrated,
            transitionTo: null, selfArousal: 16
        );

        private static readonly EnemySkill SuccubusCervixLock = new EnemySkill(
            "Barbed Core Lock", manaCost: 5, hpDmg: 0, clothesDmg: 0, seductionDmg: 45, shieldGain: 5, manaGain: 12,
            desc: "Buries herself to the hilt, locking her barbed ridge against your synthetic cervix while panting heavily!",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 12, pranaGain: 0, stanceReq: CombatStance.Penetrated,
            transitionTo: null, selfArousal: 24
        );

        // Anal
        private static readonly EnemySkill SuccubusSphincterThrusts = new EnemySkill(
            "Anal Tail Stimulation", manaCost: 0, hpDmg: 0, clothesDmg: 0, seductionDmg: 30, shieldGain: 0, manaGain: 8,
            desc: "Rotates and pulses her prehensile spade tail deep within your anal tract, eliciting helpless whimpers!",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 6, pranaGain: 0, stanceReq: CombatStance.Penetrated,
            transitionTo: null, selfArousal: 16
        );

        private static readonly EnemySkill SuccubusSphincterCoil = new EnemySkill(
            "Barbed Tail Coil", manaCost: 0, hpDmg: 0, clothesDmg: 0, seductionDmg: 35, shieldGain: 4, manaGain: 0,
            desc: "Coils her sensitive tail tip deep in your anal tract, rhythmically kneading your inner prostate zones.",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 8, pranaGain: 0, stanceReq: CombatStance.Penetrated,
            transitionTo: null, selfArousal: 22
        );

        // Oral
        private static readonly EnemySkill SuccubusOralThrusts = new EnemySkill(
            "Deep Throat Domination", manaCost: 0, hpDmg: 0, clothesDmg: 0, seductionDmg: 30, shieldGain: 0, manaGain: 8,
            desc: "Thrusts relentlessly against your throat, running her fingers through your hair as your eyes water!",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 6, pranaGain: 0, stanceReq: CombatStance.Penetrated,
            transitionTo: null, selfArousal: 16, targetSlot: PenetrationSlot.Mouth
        );

        private static readonly EnemySkill SuccubusOralFondle = new EnemySkill(
            "Throat Depths Restraint", manaCost: 0, hpDmg: 0, clothesDmg: 0, seductionDmg: 35, shieldGain: 3, manaGain: 0,
            desc: "Pushes deep into the back of your throat and holds steady, commanding complete oral submission.",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 7, pranaGain: 0, stanceReq: CombatStance.Penetrated,
            transitionTo: null, selfArousal: 20, targetSlot: PenetrationSlot.Mouth
        );


        // =========================================================================
        // ELVEN SCOUT SKILLS (Zero-phallus Female, Agility, Pheromones, Fingers & Kiss)
        // =========================================================================

        // --- Elven Scout: Neutral Stance (Precision Archery, Pheromones, Seduction) ---
        private static readonly EnemySkill ScoutAimedShot = new EnemySkill(
            "Aimed Shot", manaCost: 0, hpDmg: 8, clothesDmg: 12, seductionDmg: 0, shieldGain: 0, manaGain: 4,
            desc: "Draws her recurve bow with serene poise, releasing a piercing arrow targeting weak seams in your armor!",
            armorTarget: ArmorTargetSlot.Upper, staminaDmg: 5, pranaGain: 0, stanceReq: CombatStance.Neutral
        );

        private static readonly EnemySkill ScoutPheromoneGrenade = new EnemySkill(
            "Pheromone Grenade", manaCost: 3, hpDmg: 0, clothesDmg: 14, seductionDmg: 20, shieldGain: 0, manaGain: 0,
            desc: "Shatters a delicate glass vial of concentrated woodland pheromones, wrapping you in sweet intoxicant vapors!",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 6, pranaGain: 0, stanceReq: CombatStance.Neutral
        );

        private static readonly EnemySkill ScoutSeductiveTease = new EnemySkill(
            "Seductive Tease", manaCost: 0, hpDmg: 0, clothesDmg: 0, seductionDmg: 16, shieldGain: 0, manaGain: 5,
            desc: "Her slender ears twitch as she offers an alluring smirk: 'Quiet now, sweet vessel... our grove will take much tender care of you.'",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 3, pranaGain: 0, stanceReq: CombatStance.Neutral
        );

        private static readonly EnemySkill ScoutFacePin = new EnemySkill(
            "Face Pin & Intoxicant Hold", manaCost: 2, hpDmg: 0, clothesDmg: 0, seductionDmg: 16, shieldGain: 0, manaGain: 0,
            desc: "Darts in close and clamps both slender hands around your cheeks, pinning your head to the moss while whispering sweet elven promises!",
            armorTarget: ArmorTargetSlot.Upper, staminaDmg: 10, pranaGain: 0, stanceReq: CombatStance.Neutral,
            transitionTo: CombatStance.Grappled, targetSlot: PenetrationSlot.None, targetGrapple: GrappleZone.Upper
        );

        private static readonly EnemySkill ScoutThighPin = new EnemySkill(
            "Thigh Pin & Leg Spread", manaCost: 2, hpDmg: 0, clothesDmg: 8, seductionDmg: 14, shieldGain: 0, manaGain: 0,
            desc: "Scissor-locks her athletic bare thighs firmly around your hips, forcing your knees apart to leave your feminine cleft vulnerable!",
            armorTarget: ArmorTargetSlot.Lower, staminaDmg: 12, pranaGain: 0, stanceReq: CombatStance.Neutral,
            transitionTo: CombatStance.Grappled, targetSlot: PenetrationSlot.None, targetGrapple: GrappleZone.Lower
        );

        // --- Elven Scout: Grappled Stance (Bodily Pin, Stripping, Kiss / Finger Transition) ---
        private static readonly EnemySkill ScoutThighClamp = new EnemySkill(
            "Thigh Clamp Hold", manaCost: 0, hpDmg: 0, clothesDmg: 5, seductionDmg: 12, shieldGain: 3, manaGain: 3,
            desc: "Clamps her supple athletic thighs tightly across your torso, crushing your breath and weakening your stance.",
            armorTarget: ArmorTargetSlot.Lower, staminaDmg: 10, pranaGain: 0, stanceReq: CombatStance.Grappled
        );

        private static readonly EnemySkill ScoutStripLower = new EnemySkill(
            "Lithe Armor Peel", manaCost: 1, hpDmg: 0, clothesDmg: 30, seductionDmg: 12, shieldGain: 0, manaGain: 0,
            desc: "Slips her nimble fingers beneath the waistband of your lower garment, tearing the laces open to bare your soft thighs!",
            armorTarget: ArmorTargetSlot.Lower, staminaDmg: 5, pranaGain: 0, stanceReq: CombatStance.Grappled
        );

        private static readonly EnemySkill ScoutAphrodisiacKiss = new EnemySkill(
            "Aphrodisiac Kiss", manaCost: 2, hpDmg: 0, clothesDmg: 0, seductionDmg: 28, shieldGain: 0, manaGain: 0,
            desc: "Pins your cheeks with slender hands and captures your lips in a passionate, intoxicating elven kiss, sharing heady nectar!",
            armorTarget: ArmorTargetSlot.Upper, staminaDmg: 8, pranaGain: 0, stanceReq: CombatStance.Grappled,
            transitionTo: CombatStance.Penetrated, targetSlot: PenetrationSlot.Mouth
        );

        private static readonly EnemySkill ScoutFingerPlay = new EnemySkill(
            "Manual Incursion (Fingers)", manaCost: 2, hpDmg: 0, clothesDmg: 0, seductionDmg: 32, shieldGain: 0, manaGain: 0,
            desc: "Reaches between your exposed thighs, deftly parting your soaked petal folds to slide her agile elven fingers inside!",
            armorTarget: ArmorTargetSlot.Lower, staminaDmg: 8, pranaGain: 0, stanceReq: CombatStance.Grappled,
            transitionTo: CombatStance.Penetrated, targetSlot: PenetrationSlot.Vagina
        );

        // --- Elven Scout: Penetrated Stance (Dedicated single move per orifice) ---
        private static readonly EnemySkill ScoutNectarKiss = new EnemySkill(
            "Nectar Influx Kiss", manaCost: 0, hpDmg: 0, clothesDmg: 0, seductionDmg: 28, shieldGain: 0, manaGain: 5,
            desc: "Her tongue swirls deeply against yours, parting your lips to let warm, aphrodisiac-laced saliva trickle down your throat.",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 6, pranaGain: 0, stanceReq: CombatStance.Penetrated,
            transitionTo: null, selfArousal: 18, targetSlot: PenetrationSlot.Mouth
        );

        private static readonly EnemySkill ScoutVaginalFingers = new EnemySkill(
            "Vaginal Finger Penetration", manaCost: 0, hpDmg: 0, clothesDmg: 0, seductionDmg: 34, shieldGain: 0, manaGain: 5,
            desc: "Curves her agile elven fingers deep within your damp vessel, stroking with slick rhythmic speed that drives your hips into convulsive trembling.",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 8, pranaGain: 0, stanceReq: CombatStance.Penetrated,
            transitionTo: null, selfArousal: 20, targetSlot: PenetrationSlot.Vagina
        );


        // =========================================================================
        // GENERIC / FALLBACK SKILLS (Guarantees no weapon attacks during penetration)
        // =========================================================================
        private static readonly EnemySkill GenericPenetratedPumping = new EnemySkill(
            "Intimate Union Thrusts", manaCost: 0, hpDmg: 0, clothesDmg: 0, seductionDmg: 25, shieldGain: 0, manaGain: 5,
            desc: "Engages in intense rhythmic thrusts inside your vulnerable vessel, building friction and arousal.",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 6, pranaGain: 0, stanceReq: CombatStance.Penetrated,
            transitionTo: null, selfArousal: 15
        );

        private static readonly EnemySkill GenericGrappledHold = new EnemySkill(
            "Struggle Pin", manaCost: 0, hpDmg: 0, clothesDmg: 15, seductionDmg: 15, shieldGain: 0, manaGain: 0,
            desc: "Wrestles your body to the ground, exhausting your posture and tearing at your clothes.",
            armorTarget: ArmorTargetSlot.All, staminaDmg: 10, pranaGain: 0, stanceReq: CombatStance.Grappled
        );


        // =========================================================================
        // AI ROUTING
        // =========================================================================

        public static EnemySkill ChooseAction(Combatant enemy, Combatant player, CombatStance stance = CombatStance.Neutral)
        {
            if (enemy == null || player == null || enemy.isDefeated) return null;

            // Determine effective stance based on this specific enemy's state
            CombatStance effectiveStance;
            if (enemy.occupiedSlot != PenetrationSlot.None)
            {
                effectiveStance = CombatStance.Penetrated;
            }
            else if (enemy.isGrapplingPlayer)
            {
                effectiveStance = CombatStance.Grappled;
            }
            else
            {
                // This specific enemy is NOT yet grappling the player!
                // Even if another enemy holds the player, THIS enemy acts from Neutral stance.
                // He must first grab the player before he can attempt penetration!
                effectiveStance = CombatStance.Neutral;
            }

            // ScriptableObject Custom AI Check
            if (CombatManager.Instance != null)
            {
                EnemyDefinitionSO def = CombatManager.Instance.GetEnemyDefinition(enemy.enemyType);
                if (def != null)
                {
                    EnemySkill customAction = def.ChooseAction(enemy, player, effectiveStance);
                    if (customAction != null) return customAction;
                }
            }

            switch (enemy.enemyType)
            {
                case EnemyType.SlimeGirl:
                    return ChooseSlimeAction(enemy, player, effectiveStance);

                case EnemyType.Succubus:
                    return ChooseSuccubusAction(enemy, player, effectiveStance);

                case EnemyType.ElvenScout:
                    return ChooseScoutAction(enemy, player, effectiveStance);

                default:
                    if (effectiveStance == CombatStance.Penetrated || effectiveStance == CombatStance.ThroatLock)
                    {
                        return GenericPenetratedPumping;
                    }
                    if (effectiveStance == CombatStance.Grappled)
                        return GenericGrappledHold;
                    return SlimeTackle;
            }
        }


        // =========================================================================
        // SLIME GIRL AI
        // =========================================================================

        private static EnemySkill ChooseSlimeAction(Combatant enemy, Combatant player, CombatStance stance)
        {
            switch (stance)
            {
                case CombatStance.Penetrated:
                case CombatStance.ThroatLock:
                    return ChooseSlimePenetratedAction(enemy, player);

                case CombatStance.Grappled:
                    return ChooseSlimeGrappledAction(enemy, player);

                case CombatStance.Neutral:
                default:
                    return ChooseSlimeNeutralAction(enemy, player);
            }
        }

        private static EnemySkill ChooseSlimeNeutralAction(Combatant enemy, Combatant player)
        {
            bool hasUpper = player.upperClothesHp > 0;
            bool hasLower = player.lowerClothesHp > 0;
            var cm = CombatManager.Instance;
            bool canGrapple = cm == null || cm.CanGrapple(enemy);

            // If player's stamina is dangerously low (<= 8) or lower clothes shredded, attempt grapple pounce!
            if (canGrapple && (player.currentStamina <= 8 || !hasLower))
            {
                if (UnityEngine.Random.value < 0.60f) return SlimeAdhesivePounce;
            }

            // Sensual exhibitionism in Neutral stance
            if (enemy.gender != EntityGender.Female && (player.seductionStacks >= 1 || !hasLower) && UnityEngine.Random.value < 0.35f)
            {
                return SlimeFapToFace;
            }

            if (hasUpper || hasLower)
            {
                float roll = UnityEngine.Random.value;

                if (hasUpper && !hasLower)
                {
                    return (roll < 0.65f) ? SlimeAcidTop : ((roll < 0.85f) ? SlimeEngulf : SlimeTackle);
                }

                if (!hasUpper && hasLower)
                {
                    return (roll < 0.65f) ? SlimeGelatinBottom : ((roll < 0.85f) ? SlimeEngulf : SlimeTackle);
                }

                // Both intact
                if (roll < 0.30f)
                    return SlimeAcidTop;
                else if (roll < 0.55f)
                    return SlimeGelatinBottom;
                else if (roll < 0.70f)
                    return SlimeCorrosiveAll;
                else if (roll < 0.85f)
                    return SlimeEngulf;
                else
                    return SlimeTackle;
            }
            else
            {
                // Player completely naked! Slime attempts grapple pounce, engulf, or fap to face
                if (UnityEngine.Random.value < 0.45f)
                {
                    return SlimeAdhesivePounce;
                }
                if (enemy.gender != EntityGender.Female && UnityEngine.Random.value < 0.40f)
                {
                    return SlimeFapToFace;
                }
                if (UnityEngine.Random.value < 0.50f)
                {
                    return SlimeEngulf;
                }
                return SlimeTackle;
            }
        }

        private static EnemySkill ChooseSlimeGrappledAction(Combatant enemy, Combatant player)
        {
            bool hasLower = player.lowerClothesHp > 0;

            // Female entity: Can ONLY grapple, hold, caress, strip lower, but NEVER penetrate!
            if (!enemy.canPenetrate)
            {
                if (hasLower && UnityEngine.Random.value < 0.50f)
                {
                    return SlimeDissolveLower;
                }
                float rFem = UnityEngine.Random.value;
                if (rFem < 0.35f) return SlimeSensualCaress;
                if (rFem < 0.70f) return SlimeBreastFondle;
                return SlimeAdhesivePin;
            }

            // Futa entity: Can penetrate into available slots or perform external sensual grinding!
            var cm = CombatManager.Instance;
            bool canVagina = cm != null ? cm.CanPenetrateSlot(enemy, PenetrationSlot.Vagina) : !hasLower;
            bool canAnal = cm != null ? cm.CanPenetrateSlot(enemy, PenetrationSlot.Anal) : !hasLower;
            bool canOral = cm != null ? cm.CanPenetrateSlot(enemy, PenetrationSlot.Mouth) : true;

            // If lower garments intact:
            if (hasLower)
            {
                float rIntact = UnityEngine.Random.value;
                if (rIntact < 0.45f) return SlimeDissolveLower;
                if (rIntact < 0.65f) return SlimeRubPussy; // Grinding against clothed crotch!
                if (rIntact < 0.80f) return SlimeBreastFondle;
                if (canOral && UnityEngine.Random.value < 0.40f) return SlimeForceMouth;
                return (UnityEngine.Random.value < 0.5f) ? SlimeSensualCaress : SlimeThighGrind;
            }

            // Lower garments stripped (Heroine is naked below):
            // Penetration is NOT mandatory every turn - external grinding (Rub Against Pussy, Thighs, Breasts) is fully available!
            List<EnemySkill> availablePenSkills = new List<EnemySkill>();
            if (canVagina) availablePenSkills.Add(SlimeForceVagina);
            if (canAnal) availablePenSkills.Add(SlimeForceSphincter);
            if (canOral) availablePenSkills.Add(SlimeForceMouth);

            if (availablePenSkills.Count > 0 && UnityEngine.Random.value < 0.55f)
            {
                int rIdx = UnityEngine.Random.Range(0, availablePenSkills.Count);
                return availablePenSkills[rIdx];
            }

            // External grinding on bare body:
            float rNaked = UnityEngine.Random.value;
            if (rNaked < 0.35f) return SlimeRubPussy; // Grinds directly against exposed pussy/clitoris!
            if (rNaked < 0.60f) return SlimeThighGrind;
            if (rNaked < 0.80f) return SlimeBreastFondle;
            if (rNaked < 0.90f) return SlimeSensualCaress;

            return SlimeAdhesivePin;
        }

        private static EnemySkill ChooseSlimePenetratedAction(Combatant enemy, Combatant player)
        {
            // STRICTLY ZERO PHYSICAL ATTACKS during penetration!
            PenetrationSlot slot = enemy.occupiedSlot;

            switch (slot)
            {
                case PenetrationSlot.Mouth:
                    if (UnityEngine.Random.value < 0.50f)
                        return SlimeOralSwirl;
                    return SlimeOralPumping;

                case PenetrationSlot.Anal:
                    if (UnityEngine.Random.value < 0.50f)
                        return SlimeSphincterMassage;
                    return SlimeSphincterPumping;

                case PenetrationSlot.Vagina:
                default:
                    if (UnityEngine.Random.value < 0.50f)
                        return SlimeInternalMassage;
                    return SlimeRhythmicPumping;
            }
        }


        // =========================================================================
        // SUCCUBUS AI
        // =========================================================================

        private static EnemySkill ChooseSuccubusAction(Combatant enemy, Combatant player, CombatStance stance)
        {
            switch (stance)
            {
                case CombatStance.Penetrated:
                case CombatStance.ThroatLock:
                    return ChooseSuccubusPenetratedAction(enemy, player);

                case CombatStance.Grappled:
                    return ChooseSuccubusGrappledAction(enemy, player);

                case CombatStance.Neutral:
                default:
                    return ChooseSuccubusNeutralAction(enemy, player);
            }
        }

        private static EnemySkill ChooseSuccubusNeutralAction(Combatant enemy, Combatant player)
        {
            bool hasUpper = player.upperClothesHp > 0;
            bool hasLower = player.lowerClothesHp > 0;
            var cm = CombatManager.Instance;
            bool canGrapple = cm == null || cm.CanGrapple(enemy);

            // If player's stamina is low (<= 8) or player naked, succubus pounces to grapple!
            if (canGrapple && (player.currentStamina <= 8 || (!hasUpper && !hasLower)))
            {
                if (UnityEngine.Random.value < 0.60f) return SuccubusDemonicPounce;
            }

            // Sensual teasing & exhibitionism in Neutral
            if (enemy.gender != EntityGender.Female && (player.seductionStacks >= 1 || !hasLower) && UnityEngine.Random.value < 0.35f)
            {
                return SuccubusFapToFace;
            }

            if (player.seductionStacks >= 1 && UnityEngine.Random.value < 0.50f)
            {
                return SuccubusEcstasy;
            }

            float roll = UnityEngine.Random.value;
            if (roll < 0.35f)
                return SuccubusCharm;
            else if (roll < 0.55f)
                return SuccubusWhisper;

            // Physical attacks targeting clothing
            if (hasUpper && !hasLower)
            {
                return SuccubusClawsTop;
            }
            if (!hasUpper && hasLower)
            {
                return SuccubusTailBottom;
            }
            if (hasUpper && hasLower)
            {
                if (roll < 0.85f)
                    return SuccubusAbyssalLashAll;
                return (UnityEngine.Random.value < 0.5f) ? SuccubusClawsTop : SuccubusTailBottom;
            }

            return SuccubusTailBottom;
        }

        private static EnemySkill ChooseSuccubusGrappledAction(Combatant enemy, Combatant player)
        {
            bool hasLower = player.lowerClothesHp > 0;

            // Female entity: Can ONLY grapple, hold, bite, strip, but NEVER penetrate!
            if (!enemy.canPenetrate)
            {
                if (hasLower && UnityEngine.Random.value < 0.50f)
                {
                    return SuccubusTailStrip;
                }
                float rFem = UnityEngine.Random.value;
                if (rFem < 0.35f) return SuccubusNeckBite;
                if (rFem < 0.70f) return SuccubusBreastFondle;
                return SuccubusWristPin;
            }

            // Futa entity: Can penetrate into available slots or perform external sensual grinding!
            var cm = CombatManager.Instance;
            bool canVagina = cm != null ? cm.CanPenetrateSlot(enemy, PenetrationSlot.Vagina) : !hasLower;
            bool canAnal = cm != null ? cm.CanPenetrateSlot(enemy, PenetrationSlot.Anal) : !hasLower;
            bool canOral = cm != null ? cm.CanPenetrateSlot(enemy, PenetrationSlot.Mouth) : true;

            // If lower clothes intact:
            if (hasLower)
            {
                float rIntact = UnityEngine.Random.value;
                if (rIntact < 0.45f) return SuccubusTailStrip;
                if (rIntact < 0.65f) return SuccubusRubPussy; // Grinding against clothed crotch!
                if (rIntact < 0.80f) return SuccubusBreastFondle;
                if (canOral && UnityEngine.Random.value < 0.40f) return SuccubusForceMouth;
                return (UnityEngine.Random.value < 0.5f) ? SuccubusNeckBite : SuccubusThighGrind;
            }

            // Lower garments stripped (Heroine is naked below):
            // Penetration is NOT mandatory every turn - external grinding (Rub Against Pussy, Thighs, Breasts) is fully available!
            List<EnemySkill> availablePenSkills = new List<EnemySkill>();
            if (canVagina) availablePenSkills.Add(SuccubusMountVagina);
            if (canAnal) availablePenSkills.Add(SuccubusTailSphincter);
            if (canOral) availablePenSkills.Add(SuccubusForceMouth);

            if (availablePenSkills.Count > 0 && UnityEngine.Random.value < 0.55f)
            {
                int rIdx = UnityEngine.Random.Range(0, availablePenSkills.Count);
                return availablePenSkills[rIdx];
            }

            // External sensual grinding on bare body:
            float rNaked = UnityEngine.Random.value;
            if (rNaked < 0.35f) return SuccubusRubPussy; // Grinding against exposed clitoris & vulva!
            if (rNaked < 0.60f) return SuccubusThighGrind;
            if (rNaked < 0.80f) return SuccubusBreastFondle;
            if (rNaked < 0.90f) return SuccubusNeckBite;

            return SuccubusWristPin;
        }

        private static EnemySkill ChooseSuccubusPenetratedAction(Combatant enemy, Combatant player)
        {
            // STRICTLY ZERO PHYSICAL ATTACKS during penetration!
            PenetrationSlot slot = enemy.occupiedSlot;

            switch (slot)
            {
                case PenetrationSlot.Mouth:
                    if (UnityEngine.Random.value < 0.50f)
                        return SuccubusOralFondle;
                    return SuccubusOralThrusts;

                case PenetrationSlot.Anal:
                    if (UnityEngine.Random.value < 0.50f)
                        return SuccubusSphincterCoil;
                    return SuccubusSphincterThrusts;

                case PenetrationSlot.Vagina:
                default:
                    if (UnityEngine.Random.value < 0.50f)
                        return SuccubusCervixLock;
                    return SuccubusAbyssalThrusts;
            }
        }


        // =========================================================================
        // ELVEN SCOUT AI
        // =========================================================================

        private static EnemySkill ChooseScoutAction(Combatant enemy, Combatant player, CombatStance stance)
        {
            switch (stance)
            {
                case CombatStance.Penetrated:
                case CombatStance.ThroatLock:
                    return ChooseScoutPenetratedAction(enemy, player);

                case CombatStance.Grappled:
                    return ChooseScoutGrappledAction(enemy, player);

                case CombatStance.Neutral:
                default:
                    return ChooseScoutNeutralAction(enemy, player);
            }
        }

        private static EnemySkill ChooseScoutNeutralAction(Combatant enemy, Combatant player)
        {
            var cm = CombatManager.Instance;
            bool canLowerGrapple = cm == null || cm.CanGrapple(enemy, GrappleZone.Lower);
            bool canUpperGrapple = cm == null || cm.CanGrapple(enemy, GrappleZone.Upper);

            // Cascade 1 (Thigh Pin -> Vaginal Track): Prioritized if player's stamina is low or lower garments torn
            if (canLowerGrapple && (player.currentStamina <= 10 || player.isOuterLowerTorn))
            {
                if (UnityEngine.Random.value < 0.65f) return ScoutThighPin;
            }

            // Cascade 2 (Face Pin -> Oral Track): Prioritized if player has high arousal
            if (canUpperGrapple && (player.seduction >= 25 || player.seductionStacks >= 1))
            {
                if (UnityEngine.Random.value < 0.50f) return ScoutFacePin;
            }

            // Opportunistic thigh pin if lower zone is free
            if (canLowerGrapple && UnityEngine.Random.value < 0.35f)
            {
                return ScoutThighPin;
            }

            // High arousal tease
            if (player.seductionStacks >= 1 && UnityEngine.Random.value < 0.40f)
            {
                return ScoutSeductiveTease;
            }

            float roll = UnityEngine.Random.value;
            if (enemy.currentMana >= ScoutPheromoneGrenade.manaCost && roll < 0.35f)
            {
                return ScoutPheromoneGrenade;
            }
            if (roll < 0.70f)
            {
                return ScoutAimedShot;
            }
            return ScoutSeductiveTease;
        }

        private static EnemySkill ChooseScoutGrappledAction(Combatant enemy, Combatant player)
        {
            var cm = CombatManager.Instance;

            // --- TRACK 1: Face Pin -> Dedicated cascade into Oral Penetration (Mouth) ---
            if (enemy.heldGrappleZone == GrappleZone.Upper)
            {
                bool canOral = cm != null ? cm.CanPenetrateSlot(enemy, PenetrationSlot.Mouth) : true;
                if (canOral)
                {
                    return ScoutAphrodisiacKiss;
                }
                return ScoutSeductiveTease;
            }

            // --- TRACK 2: Thigh Pin -> Dedicated cascade into Vaginal Penetration (Fingers) ---
            bool isGroinExposed = player.isGroinExposed;
            bool canVagina = cm != null ? cm.CanPenetrateSlot(enemy, PenetrationSlot.Vagina) : isGroinExposed;

            // Step 1: If lower garments / panties are still intact -> Strip lower clothes!
            if (!isGroinExposed)
            {
                float r = UnityEngine.Random.value;
                if (r < 0.65f) return ScoutStripLower;
                return ScoutThighClamp;
            }

            // Step 2: Panties torn & groin exposed -> 100% cascade into Vaginal Finger Penetration!
            if (canVagina)
            {
                return ScoutFingerPlay;
            }

            return ScoutThighClamp;
        }

        private static EnemySkill ChooseScoutPenetratedAction(Combatant enemy, Combatant player)
        {
            PenetrationSlot slot = enemy.occupiedSlot;

            if (slot == PenetrationSlot.Mouth)
            {
                return ScoutNectarKiss;
            }

            // Vagina (Finger Penetration)
            return ScoutVaginalFingers;
        }
    }
}