using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed partial class EternalHorror : ModNPC {
    public static SoundStyle HitSound => SoundID.NPCHit18 with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f) };
    public static SoundStyle DeathSound => SoundID.NPCDeath18 with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f) };
    public static SoundStyle HitSound_Servant => SoundID.NPCHit18;
    public static SoundStyle DeathSound_Servant => SoundID.NPCHit18;
    public static SoundStyle SummonSound => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/OcramRoarSummon");
    public static SoundStyle SoulAbsorptionSound => SoundID.NPCDeath6 with { MaxInstances = 100, Pitch = 0.5f + Main.rand.NextFloat(0.5f, 1f), Volume = 0.125f * 0.25f };
    public static SoundStyle SoulAbsorptionSound_Whoosh => new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/SoulWhoosh") with { Pitch = 0.375f, Volume = 0.5f };
}
