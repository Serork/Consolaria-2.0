using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed class EternalHorrorLaser_Holdout : ModProjectile {
    public NPC Target => Main.npc[(int)Projectile.ai[2]];

    public override string Texture => "Consolaria/Assets/Textures/Empty";

    public override void SetStaticDefaults() {
        ProjectileID.Sets.NeedsUUID[Type] = true;
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
    }

    public override void SetDefaults() {
        Projectile.SetSizeValues(14, 18);

        //Projectile.width = 14;
        //Projectile.height = 18;
        Projectile.aiStyle = -1;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        //Projectile.magic = true;
        Projectile.ignoreWater = true;
    }

    public override void AI() {
        if (Projectile.ai[2] < 0 || Projectile.ai[2] > Main.npc.Length || !Target.active || Target.type != EternalHorror.SelfType) {
            Projectile.Kill();
            return;
        }

        //Player player = Main.player[Projectile.owner];
        float num = (float)Math.PI / 2f;
        Vector2 vector = Target.Center;
        int num2 = 2;
        float num3 = 0f;

        float num37 = 30f;
        if (Projectile.ai[0] > 90f)
            num37 = 15f;

        if (Projectile.ai[0] > 120f)
            num37 = 5f;

        /*
        damage = (int)((float)player.inventory[player.selectedItem].damage * player.magicDamage);
        */
        //damage = (int)player.GetTotalDamage(DamageClass.Magic).ApplyTo(player.inventory[player.selectedItem].damage);

        bool flag8 = false;
        if (Projectile.ai[0] % num37 == 0f)
            flag8 = true;

        int num38 = 10;
        bool flag9 = false;
        if (Projectile.ai[0] % num37 == 0f)
            flag9 = true;

        if (Projectile.ai[1] >= 1f) {
            Projectile.ai[1] = 0f;
            flag9 = true;
            if (Main.myPlayer == Projectile.owner) {
                float num39 = 30f/*player.inventory[player.selectedItem].shootSpeed*/ * Projectile.scale;
                Vector2 vector18 = vector;
                Vector2 value6 = Target.Center + Vector2.UnitY.RotatedBy(Target.rotation) * 5f - vector18;
                //if (player.gravDir == -1f)
                //    value6.Y = (float)(Main.screenHeight - Main.mouseY) + Main.screenPosition.Y - vector18.Y;

                Vector2 value7 = Vector2.Normalize(value6);
                if (float.IsNaN(value7.X) || float.IsNaN(value7.Y))
                    value7 = -Vector2.UnitY;

                value7 = Vector2.Normalize(Vector2.Lerp(value7, Vector2.Normalize(Projectile.velocity), 0.5f));
                value7 *= num39;
                if (value7.X != Projectile.velocity.X || value7.Y != Projectile.velocity.Y)
                    Projectile.netUpdate = true;

                Projectile.velocity = value7;
            }
        }

        //frameCounter++;
        //int num40 = ((!(ai[0] < 120f)) ? 1 : 4);
        //if (frameCounter >= num40) {
        //    frameCounter = 0;
        //    if (++frame >= 5)
        //        frame = 0;
        //}

        //if (soundDelay <= 0) {
        //    soundDelay = num38;
        //    soundDelay *= 2;
        //    if (ai[0] != 1f)
        //        SoundEngine.PlaySound(SoundID.Item15, position);
        //}

        if (flag9 && Main.myPlayer == Projectile.owner) {
            bool flag10 = false;
            flag10 = !flag8 /*|| player.CheckMana(player.inventory[player.selectedItem], pay: true)*/;
            flag10 = true;
            if (/*player.channel &&*/ flag10) {
                if (Projectile.ai[0] == 0f) {
                    Vector2 center2 = Projectile.Center;
                    Vector2 vector19 = Vector2.Normalize(Projectile.velocity);
                    if (float.IsNaN(vector19.X) || float.IsNaN(vector19.Y))
                        vector19 = -Vector2.UnitY;

                    int uuid = Projectile.GetByUUID(Projectile.owner, Projectile.whoAmI);

                    int num41 = Projectile.damage;
                    for (int m = 0; m < 6; m++) {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), center2.X, center2.Y, vector19.X, vector19.Y, ModContent.ProjectileType<EternalHorrorLaser_Big>(), num41, Projectile.knockBack, Projectile.owner, m, uuid);
                    }

                    Projectile.netUpdate = true;
                }
            }
            else {
                Projectile.Kill();
            }
        }

        var target = Target.As<EternalHorror>();

        Projectile.position = target.GetEyePosition(true, laser: true) - Projectile.Size / 2f;
        Projectile.rotation = Projectile.velocity.ToRotation() + num;
        //Projectile.position += Vector2.UnitY.RotatedBy(Projectile.rotation) * 50f;
        Projectile.spriteDirection = Projectile.direction;
        Projectile.timeLeft = 2;
        //player.ChangeDir(Projectile.direction);
        //player.heldProj = Projectile.whoAmI;
        //player.SetDummyItemTime(num2);
        //player.itemRotation = MathHelper.WrapAngle((float)Math.Atan2(Projectile.velocity.Y * (float)Projectile.direction, Projectile.velocity.X * (float)Projectile.direction) + num3);

        if (Projectile.ai[0] < 180f) {
            Projectile.ai[0] += 2f - 1f * Projectile.ai[0] / 180f;
        }
        Projectile.ai[1] += 1f;
    }

    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;

    public override bool ShouldUpdatePosition() => false;
}
