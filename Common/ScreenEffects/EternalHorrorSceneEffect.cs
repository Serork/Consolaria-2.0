using Consolaria.Content.NPCs.Bosses.EternalHorror;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Consolaria.Common.ScreenEffects;

sealed class EternalHorrorSceneEffect : ModSceneEffect {
    public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

    public override int Music => MusicLoader.GetMusicSlot(Mod, EternalHorror.EERIEOCRAM_MUSICPATH);

    public override bool IsSceneEffectActive(Player player) => EternalHorrorSummonHandler.EternalHorrorSummonStarted;

    public override void SpecialVisuals(Player player, bool isActive) {
        player.ManageSpecialBiomeVisuals(ShaderLoader.EternalHorrorTintFilterName, EternalHorrorSummonHandler.EternalHorrorSummonEnded);
        if (ShaderLoader.EternalHorrorTintFilter.IsActive()) {
            ShaderLoader.EternalHorrorTintFilter.GetShader()
                .UseOpacity(0.125f)
                .UseTargetPosition(player.Center);
        }
    }
}
