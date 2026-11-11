using Consolaria.Content.Dusts;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Content.Items.Consumables;

sealed class ConfettiGun_UI : ModItem {
    public override Color? GetAlpha(Color lightColor) => Color.White;

    public override void SetDefaults() {
        Item.useStyle = 5;
        Item.shootSpeed = 5f;
        Item.shoot = ModContent.ProjectileType<ConfettiGun_UI_Shot>();
        Item.damage = 0;
        Item.width = 18;
        Item.height = 20;
        Item.maxStack = Item.CommonMaxStack;
        Item.consumable = true;
        Item.UseSound = SoundID.Item11;
        Item.useAnimation = 15;
        Item.useTime = 15;
        Item.noMelee = true;
        Item.value = 100;
        Item.DamageType = DamageClass.Ranged;
    }
}

sealed class ConfettiGun_UI_Shot : ModProjectile {
    public override string Texture => "Consolaria/Assets/Textures/Empty";

    public override void SetDefaults() {
        Projectile.width = 10;
        Projectile.height = 10;
        Projectile.aiStyle = 1;
        Projectile.alpha = 255;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 2;
    }

    public override void OnKill(int timeLeft) {
        for (int num512 = 0; num512 < 85; num512++) {
            int num513 = Main.rand.NextFromList([ModContent.DustType<Confetti1>(), ModContent.DustType<Confetti2>(), ModContent.DustType<Confetti3>(), ModContent.DustType<Confetti4>()]);
            int num514 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, num513, Projectile.velocity.X, Projectile.velocity.Y, 0, default(Color), 1.2f);
            Main.dust[num514].velocity.X += (float)Main.rand.Next(-50, 51) * 0.01f;
            Main.dust[num514].velocity.Y += (float)Main.rand.Next(-50, 51) * 0.01f;
            Main.dust[num514].velocity.X *= 1f + (float)Main.rand.Next(-50, 51) * 0.01f;
            Main.dust[num514].velocity.Y *= 1f + (float)Main.rand.Next(-50, 51) * 0.01f;
            Main.dust[num514].velocity.X += (float)Main.rand.Next(-50, 51) * 0.05f;
            Main.dust[num514].velocity.Y += (float)Main.rand.Next(-50, 51) * 0.05f;
            Dust dust2 = Main.dust[num514];
            dust2.scale *= 1f + (float)Main.rand.Next(-30, 31) * 0.01f;
        }

        for (int num515 = 0; num515 < 40; num515++) {
            int num516 = Main.rand.NextFromList([ModContent.GoreType<Gores.Confetti1>(), ModContent.GoreType<Gores.Confetti2>(), ModContent.GoreType<Gores.Confetti3>(), ModContent.GoreType<Gores.Confetti4>(), ModContent.GoreType<Gores.Confetti5>(), ModContent.GoreType<Gores.Confetti6>()]);
            if (!Main.dedServ) {
                int num517 = Gore.NewGore(Projectile.GetSource_FromAI(), Projectile.position, Projectile.velocity, num516);
                Main.gore[num517].velocity.X += (float)Main.rand.Next(-50, 51) * 0.01f;
                Main.gore[num517].velocity.Y += (float)Main.rand.Next(-50, 51) * 0.01f;
                Main.gore[num517].velocity.X *= 1f + (float)Main.rand.Next(-50, 51) * 0.01f;
                Main.gore[num517].velocity.Y *= 1f + (float)Main.rand.Next(-50, 51) * 0.01f;
                Gore gore2 = Main.gore[num517];
                gore2.scale *= 1f + (float)Main.rand.Next(-20, 21) * 0.01f;
                Main.gore[num517].velocity.X += (float)Main.rand.Next(-50, 51) * 0.05f;
                Main.gore[num517].velocity.Y += (float)Main.rand.Next(-50, 51) * 0.05f;
            }
        }
    }
}
