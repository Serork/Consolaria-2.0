using Consolaria.Common.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using RoA.Core.Utility.Vanilla;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed class EternalHorrorScythe : ModProjectile, IEternalHorrorProjectile {
    private static Asset<Texture2D> _glowTexture = null!;

    private float rotationTimer = (float)Math.PI;

    public override void SetStaticDefaults() {

        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;

        if (!Main.dedServ) {
            _glowTexture = Helper.RequestTexture(Texture + "_Glow");
        }
    }

    public override void SetDefaults() {
        int width = 60; int height = width;
        Projectile.Size = new Vector2(width, height);

        Projectile.hostile = true;
        Projectile.friendly = false;

        Projectile.penetrate = 1;
        Projectile.tileCollide = false;

        //Projectile.light = 0.25f;
        //Projectile.scale = 0.6f;
    }

    public override void AI() {
        if (Projectile.localAI[2] == 0f) {
            Projectile.localAI[2] = 1f;

            Projectile.scale = 0f;

            //Vector2 angle = Projectile.Center.DirectionTo(Main.LocalPlayer.Center);
            //Projectile.velocity = angle * 5f;
        }

        Projectile.scale = Helper.Approach(Projectile.scale, 1f, 0.1f);

        if (Projectile.timeLeft > 200)
            Projectile.velocity *= 1.05f;

        //int dust2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 1.2f);
        //Main.dust[dust2].noGravity = true;

        Projectile.rotation += 2 / rotationTimer;
        rotationTimer += 0.05f;

        Lighting.AddLight(Projectile.Center, 0.4f, 0.1f, 0.5f);

        if (Main.rand.NextBool(2)) {
            void makeSpawnDust() {
                Color colorTint = EternalHorror.MainPurpleColor * 0.75f;

                Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.height) * 0.5f;
                Vector2 velocity = Projectile.velocity;
                //velocity += Vector2.UnitY.RotatedBy(Projectile.identity + Main.GlobalTimeWrappedHourly * -15f + MathHelper.PiOver2) * Main.rand.NextFloat(Projectile.velocity.Length()) * 0.75f * Projectile.spriteDirection;
                position += velocity * 5f;
                velocity *= 0.2f;
                FadingParticle fadingParticle = ParticlePools.FadingParticlePool.RequestParticle();
                fadingParticle.SetBasicInfo(TextureAssets.Star[0], null, velocity, position);
                float num = 25f/* * Main.rand.NextFloat(0.5f, 1f)*/;
                fadingParticle.SetTypeInfo(num);
                fadingParticle.AccelerationPerFrame = velocity / num;
                fadingParticle.ColorTint = colorTint;
                fadingParticle.FadeInNormalizedTime = 0.5f;
                fadingParticle.FadeOutNormalizedTime = 0.5f;
                fadingParticle.Rotation = Main.rand.NextFloat() * ((float)Math.PI * 2f);
                fadingParticle.Scale = Vector2.One * (0.5f + 0.5f * Main.rand.NextFloat());
                Main.ParticleSystem_World_OverPlayers.Add(fadingParticle);
                FadingParticle fadingParticle2 = fadingParticle;
                fadingParticle = ParticlePools.FadingParticlePool.RequestParticle();
                fadingParticle.SetBasicInfo(TextureAssets.Star[0], null, velocity, position);
                fadingParticle.SetTypeInfo(num);
                fadingParticle.AccelerationPerFrame = velocity / num;
                fadingParticle.ColorTint = colorTint;
                fadingParticle.ColorTint.A = 30;
                fadingParticle.FadeInNormalizedTime = 0.5f;
                fadingParticle.FadeOutNormalizedTime = 0.5f;
                fadingParticle.Rotation = fadingParticle2.Rotation;
                fadingParticle.Scale = fadingParticle2.Scale * 0.5f;
                Main.ParticleSystem_World_OverPlayers.Add(fadingParticle);
            }

            makeSpawnDust();
        }
    }

    public override void OnKill(int timeLeft) {
        if (Main.netMode != NetmodeID.Server) {
            for (int i = 0; i < 30; i++) {
                int num506 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.Shadowflame, Projectile.velocity.X, Projectile.velocity.Y, 100, default, 1.5f);
                Main.dust[num506].noGravity = true;
                Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.Shadowflame, Projectile.velocity.X, Projectile.velocity.Y, 100, default, 1.1f);
            }
        }
    }

    public override bool PreDraw(ref Color lightColor) {
        if (EternalHorror.CanDrawOnTopOfBlinking || !NPC.AnyNPCs(EternalHorror.SelfType)) {
            SpriteBatch spriteBatch = Main.spriteBatch;
            Texture2D texture = Projectile.GetTexture();

            Vector2 position = Projectile.Center;
            Rectangle clip = texture.Bounds;
            Color drawColor = Color.Lerp(lightColor, Color.White, 0.5f);
            float rotation = Projectile.rotation;
            SpriteEffects flip = (Projectile.spriteDirection == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 screenPos = Main.screenPosition;

            Vector2 drawOrigin = clip.Centered();

            int num152 = 0;

            Texture2D value143 = _glowTexture.Value;
            float fromValue4 = Utils.Remap(Projectile.velocity.Length(), 0f, 16f, 0f, 1f);
            float num509 = Utils.Remap(fromValue4, 0f, 1f, 1f, 0.7f);
            float num510 = Utils.Remap(fromValue4, 0f, 1f, 1f, 0f);
            num510 = 1f;
            num509 = 1f;
            var dir = flip;
            int num512;
            float num511 = (float)(num512 = 24) / (float)num512;
            num510 *= num511;
            float num513 = 1.12f;

            float num153 = (float)(TextureAssets.Projectile[Projectile.type].Width() - Projectile.width) * 0.5f + (float)Projectile.width * 0.5f;
            num153 = 0f;

            num153 = Projectile.width / 2;

            for (int num514 = 0; num514 <= 4; num514++) {
                float num515 = Projectile.velocity.X * (float)num514 * 0.5f;
                float num516 = Projectile.velocity.Y * (float)num514 * 0.5f;
                num515 = 0;
                num516 = 0;

                Main.EntitySpriteDraw(value143, new Vector2(Projectile.position.X - Main.screenPosition.X + num153 + (float)num152 - num515,
                    Projectile.position.Y - Main.screenPosition.Y + (float)(Projectile.height / 2) + Projectile.gfxOffY - num516), new Microsoft.Xna.Framework.Rectangle(0, 0, value143.Width, value143.Height),
                    EternalHorror.MainPurpleColor_Dynamic * 0.5f * num513 * num509 * (0.5f - (float)num514 * 0.1f), Projectile.rotation, new Vector2(value143.Width / 2, value143.Height / 2), Projectile.scale * num510 * (1f - (float)num514 * 0.05f), dir);
                Main.EntitySpriteDraw(value143, new Vector2(Projectile.position.X - Main.screenPosition.X + num153 + (float)num152 - num515,
                    Projectile.position.Y - Main.screenPosition.Y + (float)(Projectile.height / 2) + Projectile.gfxOffY - num516), new Microsoft.Xna.Framework.Rectangle(0, 0, value143.Width, value143.Height),
                    EternalHorror.MainPurpleColor * num513 * num509 * (0.5f - (float)num514 * 0.1f), 0f - Projectile.rotation, new Vector2(value143.Width / 2, value143.Height / 2), Projectile.scale * 0.75f * num510 * (1f - (float)num514 * 0.05f), dir ^ SpriteEffects.FlipHorizontally);
            }

            for (int k = 0; k < Projectile.oldPos.Length - 1; k++) {
                Vector2 drawPos = Projectile.oldPos[k] + new Vector2(Projectile.width, Projectile.height) / 2f + Vector2.UnitY * Projectile.gfxOffY - Main.screenPosition;
                position = drawPos;
                rotation = (float)Math.Atan2(Projectile.oldPos[k].Y - Projectile.oldPos[k + 1].Y, Projectile.oldPos[k].X - Projectile.oldPos[k + 1].X);
                EternalHorror.DrawContext drawContext = new(spriteBatch, position, texture, clip, drawColor, rotation, flip, screenPos);
                EternalHorror.DrawUnderGlowEffect(drawContext, (newPosition, newColor) => {
                    Color color = new Color(60 - k * 5, 10, 60 + k * 4, 40 + k * 4) * 1.25f;
                    color = color.MultiplyRGBA(newColor);

                    spriteBatch.Draw(texture, newPosition, drawContext.Clip, color, Projectile.oldRot[k], drawOrigin, Projectile.scale - k / (float)Projectile.oldPos.Length, drawContext.Flip, 0f);
                    spriteBatch.Draw(texture, newPosition - Projectile.oldPos[k] * 0.5f + Projectile.oldPos[k + 1] * 0.5f, drawContext.Clip, color, Projectile.oldRot[k] * 0.5f + Projectile.oldRot[k + 1] * 0.5f, drawOrigin, Projectile.scale - (k + 0.5f) / (float)Projectile.oldPos.Length, drawContext.Flip, 0f);
                }, sinWaveOffset: Projectile.identity + MathHelper.Pi,
                    applyInnerOpacity: false,
                    forcedOpacity: MathHelper.Lerp(0.125f, 0.25f, 0f),
                    sinWaveOffset_BasedOnEffectIndex: MathHelper.TwoPi * 0.25f);
            }

            Projectile.QuickDraw(Projectile.GetAlpha(lightColor).MultiplyRGBA(EternalHorror.MainPurpleColor).MultiplyAlpha(0.5f) * Projectile.Opacity);
        }

        return false;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
        => target.AddBuff(BuffID.ShadowFlame, 180);

    public override Color? GetAlpha(Color lightColor)
        => new Color(255, 255, 255, 200);
}
