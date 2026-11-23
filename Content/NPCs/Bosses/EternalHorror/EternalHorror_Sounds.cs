using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed partial class EternalHorror : ModNPC {
    private record struct AmbientSoundInfo(float SlotID, float Time);

    private float _breatheSoundSlot;
    private float _roarSoundSlot;
    private float _cloneSoundSlot;
    private float _explosionPreparationSoundSlot, _explosionPreparationSoundSlot2;
    private float _scytheAttackPreparationSoundSlot, _scytheAttackPreparationSoundSlot2;
    private float _laserDashSoundSlot;
    private float _deathRoarSoundSlot;
    private float _spiralSoundSlot;
    private AmbientSoundInfo[] _ambientSoundSlots;

    public static SoundStyle HitSound() => SoundID.NPCHit18 with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f) };
    public static SoundStyle DeathSound() => SoundID.NPCDeath18 with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f) };
    public static SoundStyle HitSound_Servant() => SoundID.NPCHit9 with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f) };
    public static SoundStyle DeathSound_Servant() => SoundID.NPCDeath11 with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f) };
    public static SoundStyle RitualSound() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/Ritual") with { PauseBehavior = PauseBehavior.PauseWithGame };
    public static SoundStyle BreatheSound() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/Breathe") with { Pitch = 0f, Volume = 1.5f, PauseBehavior = PauseBehavior.PauseWithGame };
    public static SoundStyle RoarSound() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/Roar") with { Pitch = -0.25f };
    public static SoundStyle SoulAbsorptionSound() => SoundID.NPCDeath6 with { MaxInstances = 100, Pitch = 0.5f + Main.rand.NextFloat(0.5f, 1f), Volume = 0.125f * 0.25f };
    public static SoundStyle SoulAbsorptionSound_Whoosh() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/SoulWhoosh") with { Pitch = 0.375f, Volume = 0.5f, PauseBehavior = PauseBehavior.PauseWithGame };
    public static SoundStyle LaserSound() => SoundID.Item33 with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f), MaxInstances = 1, PitchVariance = 0.24f, Volume = 0.5f };
    public static SoundStyle LaserSound_New1() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/Laser1") with { MaxInstances = 10, Pitch = 0f, Volume = 1f, PitchVariance = 0.15f };
    public static SoundStyle LaserSound_New2() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/Laser2") with { MaxInstances = 10, Pitch = 0f, Volume = 1f, PitchVariance = 0.15f };
    public static SoundStyle CloneSound() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/Clone") with { Pitch = 0.25f, Volume = 1f, PitchVariance = 0.15f };
    public static SoundStyle LaserReflectSound() => SoundID.Item150 with { Pitch = -1f, Volume = 0.375f };
    public static SoundStyle DashSound_Phase1() => SoundID.Roar with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f), PitchVariance = 0.15f, MaxInstances = 0 };
    public static SoundStyle DashSound_Phase2() => SoundID.ForceRoar with { Pitch = 0.3f + -MathHelper.Lerp(0.125f, 0.25f, 0.5f), PitchVariance = 0.15f, MaxInstances = 0 };
    public static SoundStyle SummonSound() => SoundID.NPCDeath45 with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f), PitchVariance = 0.15f };
    public static SoundStyle ExplosionSound() => SoundID.Item14 with { Pitch = -Main.rand.NextFloat(0.75f, 1f), PitchVariance = 0.15f, MaxInstances = 0 };
    public static SoundStyle CloneExplosionPreparation() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/ClonePreparation") with { Volume = 0.875f, Pitch = -1f, PitchVariance = 0.15f };
    public static SoundStyle CloneExplosionPreparation2() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/ClonePreparation2") with { Volume = 0.75f, Pitch = -0.25f, PitchVariance = 0.15f };
    public static SoundStyle AmbientSound1() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/Ambient1") with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f), PitchVariance = 0.15f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew, MaxInstances = 1 };
    public static SoundStyle AmbientSound2() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/Ambient2") with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f), PitchVariance = 0.15f, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew, MaxInstances = 1 };
    public static SoundStyle ServantDashSound() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/ServantDash") with { Volume = MathHelper.Lerp(0.25f, 0.375f, 0.5f), Pitch = 0.75f, PitchVariance = 0.2f, MaxInstances = 10 };
    public static SoundStyle ScytheAttackPreparationSound() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/ScytheAttackPreparation") with { Volume = 1f, Pitch = -MathHelper.Lerp(0f, 0.25f, 1f), PitchVariance = 0.15f };
    public static SoundStyle ScytheAttackLaserSound() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/ScytheAttackLaser") with { Volume = 0.875f, Pitch = -0.5f, PitchVariance = 0.15f };
    public static SoundStyle ScytheCastSound() => SoundID.Item8 with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f), MaxInstances = 10, PitchVariance = 0.24f, Volume = 1f };
    public static SoundStyle TransitionSoundExtra() => SoundID.NPCHit1 with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f), PitchVariance = 0.24f };
    public static SoundStyle LaserDashSound() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/LaserDash") with { Volume = 1f, Pitch = -0.25f, PitchVariance = 0.15f, PauseBehavior = PauseBehavior.PauseWithGame };
    public static SoundStyle DeathRoarSound() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/DeathRoar") with { Volume = 1f, Pitch = 0.25f, PitchVariance = 0f, PauseBehavior = PauseBehavior.PauseWithGame };
    public static SoundStyle SpiralSound() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/Spiral") with { Volume = 0.875f, Pitch = 0f, PitchVariance = 0f, PauseBehavior = PauseBehavior.PauseWithGame };

    private void PlaySoundOnSpawn() {
        _breatheSoundSlot = SoundEngine.PlaySound(BreatheSound(), NPC.Center).ToFloat();
        _roarSoundSlot = SoundEngine.PlaySound(RoarSound(), NPC.Center).ToFloat();
    }

    private void PlayCloneSound() {
        _cloneSoundSlot = SoundEngine.PlaySound(CloneSound(), NPC.Center).ToFloat();
    }

    private void PlaySummonPreparationSound() {
        _explosionPreparationSoundSlot = SoundEngine.PlaySound(CloneExplosionPreparation(), NPC.Center).ToFloat();
    }

    private void PlayCloneExplosionPreparationSound2() {
        _explosionPreparationSoundSlot2 = SoundEngine.PlaySound(CloneExplosionPreparation2(), NPC.Center).ToFloat();
    }

    private void IntiAmbientSounds() {
        _ambientSoundSlots = new AmbientSoundInfo[25];
    }

    private void TryToPlayAmbientSounds() {
        if (!Init) {
            return;
        }

        int index = 0;
        for (int i = 0; i < _ambientSoundSlots.Length; i++) {
            if (_ambientSoundSlots[i].Time <= 0) {
                index = i;
                break;
            }
            else {
                _ambientSoundSlots[i].Time--;
            }
        }

        Player localPlayer = Main.LocalPlayer;
        if (Main.rand.Next(50) == 0) {
            _ambientSoundSlots[index] = new AmbientSoundInfo(SoundEngine.PlaySound(Main.rand.NextBool() ? AmbientSound1() : AmbientSound2(), localPlayer.Center).ToFloat(), Helper.SecondsToFrames(7));
        }
    }

    private void PlayScytheAttackPreparationSound() {
        _scytheAttackPreparationSoundSlot = SoundEngine.PlaySound(ScytheAttackPreparationSound(), NPC.Center).ToFloat();
    }

    private void PlayScytheAttackLaserSound() {
        _scytheAttackPreparationSoundSlot2 = SoundEngine.PlaySound(ScytheAttackLaserSound(), NPC.Center).ToFloat();
    }

    private void PlayLaserDashSound() {
        _laserDashSoundSlot = SoundEngine.PlaySound(LaserDashSound(), NPC.Center).ToFloat();
    }

    private void PlayDeathRoarSound() {
        _deathRoarSoundSlot = SoundEngine.PlaySound(DeathRoarSound(), NPC.Center).ToFloat();
    }

    private void PlaySpiralSound() {
        _spiralSoundSlot = SoundEngine.PlaySound(SpiralSound(), NPC.Center).ToFloat();
    }

    private void AdjustSoundPositions() {
        Helper.GetSoundAndAdjust(_breatheSoundSlot, (result) => {
            result.Position = NPC.Center;
        });
        Helper.GetSoundAndAdjust(_roarSoundSlot, (result) => {
            result.Position = NPC.Center;
        });
        Helper.GetSoundAndAdjust(_cloneSoundSlot, (result) => {
            result.Position = NPC.Center;
        });
        Helper.GetSoundAndAdjust(_explosionPreparationSoundSlot, (result) => {
            result.Position = NPC.Center;
        });
        Helper.GetSoundAndAdjust(_scytheAttackPreparationSoundSlot, (result) => {
            result.Position = NPC.Center;
        });
        Helper.GetSoundAndAdjust(_laserDashSoundSlot, (result) => {
            result.Position = NPC.Center;
        });
        Helper.GetSoundAndAdjust(_deathRoarSoundSlot, (result) => {
            result.Position = NPC.Center;
        });
        Helper.GetSoundAndAdjust(_spiralSoundSlot, (result) => {
            result.Position = NPC.Center;
        });
        for (int i = 0; i < _ambientSoundSlots.Length; i++) {
            if (_ambientSoundSlots[i].Time > 0) {
                Helper.GetSoundAndAdjust(_ambientSoundSlots[i].SlotID, (result) => {
                    result.Position = NPC.Center;
                });
            }
        }
    }
}
