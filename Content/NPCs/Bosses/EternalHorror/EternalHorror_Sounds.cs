using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed partial class EternalHorror : ModNPC {
    private float _breatheSoundSlot;
    private float _roarSoundSlot;

    //public static SoundStyle HitSound => SoundID.NPCHit18 with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f) };
    //public static SoundStyle DeathSound => SoundID.NPCDeath18 with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f) };
    //public static SoundStyle HitSound_Servant => SoundID.NPCHit18;
    //public static SoundStyle DeathSound_Servant => SoundID.NPCHit18;
    public static SoundStyle RitualSound() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/Ritual");
    public static SoundStyle BreatheSound() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/Breathe") with { Pitch = -0.25f, Volume = 1.25f };
    public static SoundStyle RoarSound() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/Roar") with { Pitch = -0.25f };
    public static SoundStyle SoulAbsorptionSound() => SoundID.NPCDeath6 with { MaxInstances = 100, Pitch = 0.5f + Main.rand.NextFloat(0.5f, 1f), Volume = 0.125f * 0.25f };
    public static SoundStyle SoulAbsorptionSound_Whoosh() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/SoulWhoosh") with { Pitch = 0.375f, Volume = 0.5f };
    public static SoundStyle LaserSound() => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/EternalHorror/Laser") with { MaxInstances = 50, PitchVariance = 0.25f, Volume = 0.25f };

    private void PlaySoundOnSpawn() {
        _breatheSoundSlot = SoundEngine.PlaySound(BreatheSound(), NPC.Center).ToFloat();
        _roarSoundSlot = SoundEngine.PlaySound(RoarSound(), NPC.Center).ToFloat();
    }

    private void AdjustSoundPositions() {
        Helper.GetSoundAndAdjust(_breatheSoundSlot, (result) => {
            result.Position = NPC.Center;
        });
        Helper.GetSoundAndAdjust(_roarSoundSlot, (result) => {
            result.Position = NPC.Center;
        });
    }
}
