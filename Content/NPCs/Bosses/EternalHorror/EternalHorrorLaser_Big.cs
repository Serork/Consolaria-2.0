using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.GameContent.Animations.Actions.Sprites;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed class EternalHorrorLaser_Big : ModProjectile, IEternalHorrorProjectile {
    private static Asset<Texture2D> _laser = null!;

    private float _scaleX;

    public override void SetStaticDefaults() {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2000;

        if (!Main.dedServ) {
            _laser = ModContent.Request<Texture2D>(Texture + "2");
        }
    }

    public override void SetDefaults() {
        Projectile.SetSizeValues(18);

        //Projectile.width = 18;
        //Projectile.height = 18;
        Projectile.aiStyle = -1;
        Projectile.friendly = false;
        Projectile.hostile = true;
        //Projectile.magic = true;
        Projectile.penetrate = -1;
        Projectile.Opacity = 0f;
        Projectile.tileCollide = false;
    }

    public override void CutTiles() {
        Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * Projectile.localAI[1], (float)Projectile.width * Projectile.scale, DelegateMethods.CutTiles);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
        float collisionPoint11 = 0f;
        if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + Projectile.velocity * Projectile.localAI[1], 22f * Projectile.scale, ref collisionPoint11))
            return true;

        return false;
    }

    public override void AI() {
        if (!Main.projectile[(int)Projectile.ai[1]].active || Main.projectile[(int)Projectile.ai[1]].type != ModContent.ProjectileType<EternalHorrorLaser_Holdout>()) {
            Projectile.Kill();
            return;
        }

        Vector2? vector59 = null;
        if (Projectile.velocity.HasNaNs() || Projectile.velocity == Vector2.Zero)
            Projectile.velocity = -Vector2.UnitY;

        float num705 = (float)(int)Projectile.ai[0] - 2.5f;
        Vector2 vector65 = Vector2.Normalize(Main.projectile[(int)Projectile.ai[1]].velocity);
        Projectile projectile2 = Main.projectile[(int)Projectile.ai[1]];
        float num706 = num705 * ((float)Math.PI / 6f);
        float num707 = 20f;
        Vector2 zero2 = Vector2.Zero;
        float num708 = 1f;
        float num709 = 15f;
        float num710 = -2f;

        float progress = Ease.CubeOut(projectile2.ai[0] / 180f);

        num708 = 1f - progress;
        num709 = 20f - progress * 14f;
        //if (projectile2.ai[0] < 120f) {
        //    num707 = 20f - 4f * (projectile2.ai[0] / 120f);
        //    Projectile.Opacity = projectile2.ai[0] / 120f * 0.4f;
        //}
        //else {
        //    num707 = 16f - 10f * ((projectile2.ai[0] - 120f) / 60f);
        //    Projectile.Opacity = 0.4f + (projectile2.ai[0] - 120f) / 60f * 0.6f;
        //}

        if (projectile2.ai[1] >= 0f) {
            Projectile.Opacity = Helper.Approach(Projectile.Opacity, 1f, 1 / 60f / MathHelper.Lerp(3.75f, 5f, 0.5f));
        }
        else {
            _scaleX = Helper.Approach(_scaleX, 0f, 0.125f * 0.5f);
        }

        num710 = -22f + progress * 20f;

        //if (projectile2.ai[0] < 180f) {
        //    num708 = 1f - projectile2.ai[0] / 180f;
        //    num709 = 20f - projectile2.ai[0] / 180f * 14f;
        //    //if (projectile2.ai[0] < 120f) {
        //    //    num707 = 20f - 4f * (projectile2.ai[0] / 120f);
        //    //    Projectile.Opacity = projectile2.ai[0] / 120f * 0.4f;
        //    //}
        //    //else {
        //    //    num707 = 16f - 10f * ((projectile2.ai[0] - 120f) / 60f);
        //    //    Projectile.Opacity = 0.4f + (projectile2.ai[0] - 120f) / 60f * 0.6f;
        //    //}

        //    Projectile.Opacity = 1f;

        //    num710 = -22f + projectile2.ai[0] / 180f * 20f;
        //}
        //else {
        //    num708 = 0f;
        //    num707 = 1.75f;
        //    num709 = 6f;
        //    Projectile.Opacity = 1f;
        //    num710 = -2f;
        //}

        //Projectile.ai[0] += 1f;

        float num711 = (Projectile.ai[0] + num705 * num707) / (num707 * 6f) * ((float)Math.PI * 2f);
        num706 = Vector2.UnitY.RotatedBy(num711).Y * ((float)Math.PI / 3f) * num708;
        zero2 = (Vector2.UnitY.RotatedBy(num711) * new Vector2(4f, num709)).RotatedBy(projectile2.velocity.ToRotation());
        Projectile.position = projectile2.Center + vector65 * 16f - Projectile.Size / 2f + new Vector2(0f, 0f - Main.projectile[(int)Projectile.ai[1]].gfxOffY);
        Projectile.position += projectile2.velocity.ToRotation().ToRotationVector2() * num710;
        Projectile.position += zero2;
        Projectile.velocity = Vector2.Normalize(projectile2.velocity).RotatedBy(num706);
        if (projectile2.ai[1] >= 0f) {
            Projectile.scale = 2f * (1f - num708);
            _scaleX = 1f;
        }
        //damage = projectile2.damage;
        if (projectile2.ai[0] >= 180f) {
            Projectile.damage *= 3;
            vector59 = projectile2.Center;
        }

        if (!Collision.CanHitLine(Main.player[Projectile.owner].Center, 0, 0, projectile2.Center, 0, 0))
            vector59 = Main.player[Projectile.owner].Center;

        //friendly = projectile2.ai[0] > 30f;

        if (Projectile.velocity.HasNaNs() || Projectile.velocity == Vector2.Zero)
            Projectile.velocity = -Vector2.UnitY;

        float num715 = Projectile.velocity.ToRotation();

        Projectile.rotation = num715 - (float)Math.PI / 2f;
        Projectile.velocity = num715.ToRotationVector2();
        float num716 = 0f;
        float num717 = 0f;
        Vector2 samplingPoint = Projectile.Center;
        if (vector59.HasValue)
            samplingPoint = vector59.Value;

        num716 = 2f;
        num717 = 0f;

        float[] array2 = new float[(int)num716];
        Collision.LaserScan(samplingPoint, Projectile.velocity, num717 * Projectile.scale, 2400f, array2);
        float num718 = 0f;
        for (int num719 = 0; num719 < array2.Length; num719++) {
            num718 += array2[num719];
        }

        num718 /= num716;
        float amount = 0.5f;

        num718 = 2000f;

        //amount = 1 / 60f * 5f;

        Projectile.localAI[1] = MathHelper.Lerp(Projectile.localAI[1], num718, amount);

        Projectile.localAI[2] -= 3f * 1.5f;
        if (Projectile.localAI[2] < 0f) {
            Projectile.localAI[2] = 256f;
        }

        DelegateMethods.v3_1 = EternalHorror.MainPurpleColor.ModifyRGB(0.5f).ToVector3() * 1f * Projectile.Opacity * Ease.QuartOut(_scaleX);
        Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * Projectile.localAI[1], (float)Projectile.width * Projectile.scale, DelegateMethods.CastLight);
    }

    public override bool ShouldUpdatePosition() => false;

    private static int _segment;

    public static void RainbowLaserDraw(int stage, Vector2 currentPosition, float distanceLeft, Rectangle lastFrame, out float distCovered, out Rectangle frame, out Vector2 origin, out Color color) {
        color = DelegateMethods.c_1;
        color *= 0.5f;
        switch (stage) {
            case 0:
                distCovered = 33f;
                frame = new Rectangle(0, 0, 26, 22);
                origin = frame.Size() / 2f;
                break;
            case 1:
                frame = new Rectangle(0, 25, 26, 28);
                distCovered = frame.Height;
                origin = new Vector2(frame.Width / 2, 0f);
                break;
            case 2:
                distCovered = 22f;
                frame = new Rectangle(0, 56, 26, 22);
                origin = new Vector2(frame.Width / 2, 1f);
                break;
            default:
                distCovered = 9999f;
                frame = Rectangle.Empty;
                origin = Vector2.Zero;
                color = Color.Transparent;
                break;
        }


        if (_segment <= 3f) {
            float scaleFactor = MathHelper.Lerp(0.125f, 1f, _segment / 3f);
            color *= scaleFactor;
        }

        _segment++;
    }

    public override bool PreDraw(ref Color lightColor) {
        //if (EternalHorror.CanDrawOnTopOfBlinking || !NPC.AnyNPCs(EternalHorror.SelfType))
        {
            Projectile proj = Projectile;

            if (!(proj.velocity == Vector2.Zero)) {
                Texture2D value67 = TextureAssets.Projectile[proj.type].Value;
                float num270 = proj.localAI[1] * 0.975f;
                float laserLuminance = 0.5f;
                float laserAlphaMultiplier = 0f;
                proj.GetLastPrismHue(proj.ai[0], ref laserLuminance, ref laserAlphaMultiplier);
                Microsoft.Xna.Framework.Color color73 = EternalHorror.MainPurpleColor.ModifyRGB(0.5f);
                color73.A = (byte)((float)(int)color73.A * laserAlphaMultiplier);
                float wave = (Projectile.whoAmI + 1) * 2f;
                color73 *= Helper.Wave(0.25f, 1.5f, 15f, wave);
                color73 *= 0.5f;
                Vector2 vector56 = proj.Center.Floor();
                vector56 += proj.velocity * proj.scale * 10.5f * 0.75f;
                num270 -= proj.scale * 14.5f * proj.scale;
                Vector2 vector57 = new Vector2(proj.scale);
                float time = (Main.GlobalTimeWrappedHourly + wave) * 1f;
                float squish = MathF.Sin(time) * 0.25f;
                Vector2 scale_ray = new(1f + squish, 1f - squish);
                vector57 = scale_ray * proj.scale;
                DelegateMethods.f_1 = 1f;
                DelegateMethods.c_1 = color73 * 0.75f * proj.Opacity;
                _ = proj.oldPos[0] + new Vector2(proj.width, proj.height) / 2f + Vector2.UnitY * proj.gfxOffY - Main.screenPosition;

                Texture2D laser = _laser.Value;

                float _scale = 1f;

                Vector2 scale2 = new Vector2(MathHelper.Lerp(vector57.X, 1f, 0.5f), vector57.Y);

                scale2.X *= 0.25f;

                var rect = new Rectangle?(new Rectangle((int)Projectile.localAI[2], 0, 512, laser.Height));

                float num = 5000f;

                Color color = DelegateMethods.c_1;

                float _scaleX2 = Ease.QuartOut(_scaleX);

                void drawLaser(Color color) {
                    var batch = Main.spriteBatch;
                    ShaderLoader.HorrorLaserShader.Start = (int)(Projectile.localAI[2] * 1f) / (float)laser.Width;
                    ShaderLoader.HorrorLaserShader.End = ((int)Projectile.localAI[2] + 256 / 1.5f) / (float)laser.Width;
                    ShaderLoader.HorrorLaserShader.Start2 = ((int)Projectile.localAI[2] + 256 / 1) / (float)laser.Width;
                    ShaderLoader.HorrorLaserShader.End2 = ((int)Projectile.localAI[2] + 512 * 1f) / (float)laser.Width;
                    ShaderLoader.HorrorLaserShader.Pow = 0.5f;
                    ShaderLoader.ApplyEffect(ShaderLoader.HorrorLaser.Value, batch, () => {
                        batch.Draw(laser,
                            Projectile.Center - Main.screenPosition,
                            rect,
                            color,
                            Projectile.rotation + MathHelper.PiOver2,
                            new Vector2(0.0f, (float)(512 / 4f)),
                            new Vector2((float)((double)num / 512) * (1f - (_scale - 1f) * 0.15f), 0.75f) * scale2 * new Vector2(1f, _scaleX2), SpriteEffects.None, 0.0f);
                    });
                }

                drawLaser(color);
                drawLaser(color);

                _segment = 0;
                Utils.DrawLaser(Main.spriteBatch, value67, vector56 - Main.screenPosition, vector56 + proj.velocity * num270 - Main.screenPosition, vector57 * new Vector2(_scaleX2, 1f), RainbowLaserDraw);
                DelegateMethods.c_1 = new Microsoft.Xna.Framework.Color(255, 255, 255, 127) * 0.75f * proj.Opacity;

                _segment = 0;
                Utils.DrawLaser(Main.spriteBatch, value67, vector56 - Main.screenPosition, vector56 + proj.velocity * num270 - Main.screenPosition, vector57 / 2f * new Vector2(_scaleX2, 1f), RainbowLaserDraw);

                drawLaser(color);

                //});
            }
        }

        return false;
    }
}
