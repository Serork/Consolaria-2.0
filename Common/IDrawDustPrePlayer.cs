using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.ModLoader;

namespace Consolaria.Common;

interface IDrawDustPrePlayer {
    void DrawPrePlayer(Dust dust);
}

sealed class DrawBehindPlayer : ILoadable {
    void ILoadable.Load(Mod mod) {
        On_LegacyPlayerRenderer.DrawPlayers += On_LegacyPlayerRenderer_DrawPlayers;
    }

    void ILoadable.Unload() {
    }

    private void On_LegacyPlayerRenderer_DrawPlayers(On_LegacyPlayerRenderer.orig_DrawPlayers orig, LegacyPlayerRenderer self, Camera camera, System.Collections.Generic.IEnumerable<Player> players) {
        Main.spriteBatch.BeginWorld(shader: false);
        for (int i = 0; i < Main.maxDustToDraw; i++) {
            Dust dust = Main.dust[i];
            if (!dust.active)
                continue;
            ModDust modDust = DustLoader.GetDust(dust.type);
            if (modDust != null && modDust is IDrawDustPrePlayer drawDustPostPlayer) {
                Vector2 screenPosition = Main.screenPosition;
                int screenWidth = Main.screenWidth,
                    screenHeight = Main.screenHeight;
                Microsoft.Xna.Framework.Rectangle rectangle = new Microsoft.Xna.Framework.Rectangle((int)screenPosition.X - 500, (int)screenPosition.Y - 50, screenWidth + 1000, screenHeight + 100);
                rectangle = new Microsoft.Xna.Framework.Rectangle((int)screenPosition.X - 1000, (int)screenPosition.Y - 1050, screenWidth + 2000, screenHeight + 2100);
                Microsoft.Xna.Framework.Rectangle rectangle2 = rectangle;
                if (new Microsoft.Xna.Framework.Rectangle((int)dust.position.X, (int)dust.position.Y, 4, 4).Intersects(rectangle)) {
                    drawDustPostPlayer.DrawPrePlayer(dust);
                }
            }
        }
        Main.spriteBatch.End();

        orig(self, camera, players);
    }
}
