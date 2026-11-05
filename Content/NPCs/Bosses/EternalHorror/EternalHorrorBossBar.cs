using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed class EternalHorrorBossBar : ModBossBar {
    private int bossHeadIndex = -1;

    public override Asset<Texture2D> GetIconTexture(ref Rectangle? iconFrame) {
        // Display the previously assigned head index
        if (bossHeadIndex != -1) {
            return TextureAssets.NpcHeadBoss[bossHeadIndex];
        }
        return null;
    }

    public override bool PreDraw(SpriteBatch spriteBatch, NPC npc, ref BossBarDrawParams drawParams) {
        if (!npc.active)
            return false;

        bossHeadIndex = npc.GetBossHeadTextureIndex();

        return !npc.As<EternalHorror>().Dead && EternalHorrorSummonHandler.EternalHorrorSummonEnded;
    }
}
