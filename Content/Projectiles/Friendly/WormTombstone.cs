using Consolaria.Content.Projectiles.Friendly.Pets;
using Consolaria.Content.Tiles;

using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Content.Projectiles.Friendly;

public class WormTombstone : ModProjectile {
    public override void Load() {
        On_NPC.DropTombstoneTownNPC += On_NPC_DropTombstoneTownNPC;
    }

    private void On_NPC_DropTombstoneTownNPC(On_NPC.orig_DropTombstoneTownNPC orig, NPC self, Terraria.Localization.NetworkText deathText) {
        if (Main.player[Player.FindClosest(self.position, self.width, self.height)].GetModPlayer<WormData>().IsWormPetActive) {
            if (Main.netMode != 1) {
                float num = 0.1f;
                if (Main.rand.Next(2) == 0)
                    num = -0.1f;

                float num2;
                for (num2 = (float)Main.rand.Next(-35, 36) * 0.1f; num2 < 2f && num2 > -2f; num2 += (float)Main.rand.Next(-30, 31) * 0.1f) {
                }

                //int num3 = Main.rand.Next(6);
                //if (self.type != 17 && self.type != 441) {
                //    num3 = ((num3 != 0) ? (num3 + 200) : 43);
                //}
                //else {
                //    num3 = Main.rand.Next(5);
                //    num3 += 527;
                //}
                int num3 = ModContent.ProjectileType<WormTombstone>();

                int num4 = Projectile.NewProjectile(self.GetSource_FromAI(), self.position.X + (float)(self.width / 2), self.position.Y + (float)(self.height / 2), (float)Main.rand.Next(10, 30) * num + num2, (float)Main.rand.Next(-40, -20) * 0.1f, num3, 0, 0f, Main.myPlayer);
                Main.projectile[num4].miscText = deathText.ToString();
            }
            return;
        }

        orig(self, deathText);
    }

    public override void SetDefaults() {
        Projectile.width = 30;
        Projectile.height = 32;
        Projectile.aiStyle = -1;
        Projectile.penetrate = -1;
        Projectile.knockBack = 12f;
    }

    public override void AI() {
        if (Projectile.velocity.Y == 0f) {
            Projectile.velocity.X *= 0.98f;
        }
        Projectile.rotation += Projectile.velocity.X * 0.1f;
        Projectile.velocity.Y += 0.2f;
        if (Projectile.owner != Main.myPlayer) {
            return;
        }
        int x = (int)((Projectile.position.X + Projectile.width / 2) / 16f);
        int y = (int)((Projectile.position.Y + Projectile.height - 4f) / 16f);
        if (Main.tile[x, y].HasTile) {
            return;
        }
        int tileType = ModContent.TileType<WormTombstoneTile>();
        WorldGen.PlaceTile(x, y, tileType, mute: false, forced: false, Projectile.owner);
        if (Main.tile[x, y].HasTile) {
            if (Main.netMode != NetmodeID.SinglePlayer) {
                NetMessage.SendData(MessageID.SpawnTileData, -1, -1, null, 1, x, y, tileType, 0);
            }
            int sign = Sign.ReadSign(x, y);
            if (sign >= 0) {
                Sign.TextSign(sign, Projectile.miscText);
            }
            Projectile.Kill();
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity) {
        if (Projectile.velocity.X != oldVelocity.X) {
            Projectile.velocity.X = oldVelocity.X * -0.75f;
        }
        if (Projectile.velocity.Y != oldVelocity.Y && Projectile.velocity.Y > 1.5) {
            Projectile.velocity.Y = oldVelocity.Y * -0.7f;
        }
        return false;
    }
}