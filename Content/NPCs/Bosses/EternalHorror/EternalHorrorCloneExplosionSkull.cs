using Consolaria.Common.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed class EternalHorrorCloneExplosionSkull : ModProjectile {
    public ref float ReflectedValue => ref Projectile.ai[2];

    public bool Reflected {
        get => ReflectedValue != 0f;
        set => ReflectedValue = value.ToInt();
    }

    public override void SetStaticDefaults() {
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 16;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
    }

    public override void SetDefaults() {
        Projectile.SetSizeValues(20);

        Projectile.aiStyle = -1;

        Projectile.hostile = true;
        Projectile.tileCollide = false;
            
        Projectile.scale = 1f;
        Projectile.alpha = 255;

        Projectile.timeLeft = 900;
        Projectile.penetrate = -1;
    }

    public override void AI() {
        Projectile.direction = Projectile.spriteDirection = (Projectile.velocity.X > 0).ToDirectionInt();

        if (Projectile.localAI[2] == 0f) {
            Projectile.localAI[2] = 1f;

            int num5 = Main.rand.Next(20, 40);
            Vector2 positionInWorld = Projectile.Center;
            Vector2 movementVector = Projectile.velocity;
            PrettySparkleParticle prettySparkleParticle = ParticlePools.PrettySparkleParticlePool.RequestParticle();
            prettySparkleParticle.ColorTint = EternalHorror.MainPurpleColor.ModifyRGB(Main.rand.NextFloat(0.5f, 1f)) with { A = 20 };
            prettySparkleParticle.LocalPosition = positionInWorld;
            prettySparkleParticle.Rotation = (float)Math.PI / 2f;
            prettySparkleParticle.Scale = new Vector2(8f, 0.4f);
            prettySparkleParticle.FadeInNormalizedTime = 0.1f;
            prettySparkleParticle.FadeOutNormalizedTime = 0.5f;
            prettySparkleParticle.TimeToLive = num5;
            prettySparkleParticle.FadeOutEnd = num5;
            prettySparkleParticle.FadeInEnd = num5 / 2;
            prettySparkleParticle.FadeOutStart = num5 / 2;
            prettySparkleParticle.AdditiveAmount = 0.35f;
            prettySparkleParticle.Velocity = movementVector;
            prettySparkleParticle.Velocity *= 0.3f;
            Main.ParticleSystem_World_OverPlayers.Add(prettySparkleParticle);
        }

        if (Projectile.timeLeft <= 895) Projectile.alpha = 50;
        Lighting.AddLight(Projectile.Center, 0.4f, 0.1f, 0.5f);

        //ReflectFromEternalHorrorClones();

        if (Main.rand.NextBool(2)) {
            void makeSpawnDust() {
                Color colorTint = EternalHorror.MainPurpleColor * 0.75f;

                Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.height) * 0.5f;
                Vector2 velocity = Projectile.velocity;
                velocity += Vector2.UnitY.RotatedBy(Projectile.identity + Main.GlobalTimeWrappedHourly * -15f + MathHelper.PiOver2) * Main.rand.NextFloat(Projectile.velocity.Length()) * 0.75f * Projectile.spriteDirection;
                position += velocity;
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

    private void MakeMovementSparkle() {
        Color colorTint = true ? EternalHorror.MainPurpleColor : EternalHorror.MainRedColor_Dynamic;

        Vector2 position = Projectile.Center;
        Vector2 velocity = Projectile.velocity;
        FadingParticle fadingParticle = ParticlePools.FadingParticlePool.RequestParticle();
        fadingParticle.SetBasicInfo(TextureAssets.Star[0], null, velocity, position);
        float num = 25f;
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

    private void ReflectFromEternalHorrorClones() {
        if (Reflected) {
            return;
        }

        foreach (NPC npc in Main.ActiveNPCs) {
            if (npc.type != EternalHorror.SelfType) {
                return;
            }

            EternalHorror boss = npc.As<EternalHorror>();
            HashSet<EternalHorror.CloneInfo> cloneData = boss.GetActiveCloneData();
            float bossRotation = npc.rotation;
            Player bossTarget = npc.GetTargetPlayer();
            Vector2 bossTargetCenter = bossTarget.Center + bossTarget.velocity * Projectile.velocity.Length() / 2f;
            foreach (EternalHorror.CloneInfo cloneInfo in cloneData) {
                if (cloneInfo.Opacity < 0.5f) {
                    continue;
                }
                Rectangle hitbox = Projectile.Hitbox;
                Vector2 clonePosition = cloneInfo.VisualPosition;
                float cloneRotation = cloneInfo.Rotation;
                Vector2 cloneDirection = Vector2.UnitY.RotatedBy(cloneRotation);
                Vector2 clonePosition_Start = clonePosition + cloneDirection * npc.height / 2f,
                        clonePosition_End = clonePosition + -cloneDirection * npc.height / 2f;
                float collisionPoint = 0f;
                if (Collision.CheckAABBvLineCollision(hitbox.Location.ToVector2(), hitbox.Size(), clonePosition_Start, clonePosition_End, npc.width, ref collisionPoint)) {
                    Projectile.velocity = Projectile.Center.DirectionTo(bossTargetCenter) * Projectile.velocity.Length();

                    cloneInfo.AddStar(Projectile.Center);

                    Reflected = true;

                    int num5 = Main.rand.Next(20, 40);
                    Vector2 positionInWorld = Projectile.Center;
                    Vector2 movementVector = Projectile.velocity;
                    PrettySparkleParticle prettySparkleParticle = ParticlePools.PrettySparkleParticlePool.RequestParticle();
                    prettySparkleParticle.ColorTint = EternalHorror.MainPurpleColor.ModifyRGB(Main.rand.NextFloat(0.5f, 1f)) with { A = 20 };
                    prettySparkleParticle.LocalPosition = positionInWorld;
                    prettySparkleParticle.Rotation = (float)Math.PI / 2f;
                    prettySparkleParticle.Scale = new Vector2(8f, 0.4f);
                    prettySparkleParticle.FadeInNormalizedTime = 0.1f;
                    prettySparkleParticle.FadeOutNormalizedTime = 0.5f;
                    prettySparkleParticle.TimeToLive = num5;
                    prettySparkleParticle.FadeOutEnd = num5;
                    prettySparkleParticle.FadeInEnd = num5 / 2;
                    prettySparkleParticle.FadeOutStart = num5 / 2;
                    prettySparkleParticle.AdditiveAmount = 0.35f;
                    prettySparkleParticle.Velocity = movementVector;
                    prettySparkleParticle.Velocity *= 0.3f;
                    Main.ParticleSystem_World_OverPlayers.Add(prettySparkleParticle);

                    return;
                }
            }
        }
    }

    public override bool PreDraw(ref Color lightColor) {
        Texture2D texture = Projectile.GetTexture();
        Vector2 position = Projectile.Center;
        Rectangle clip = texture.Bounds;
        Color drawColor = Color.Lerp(lightColor, Color.White, 0.5f);
        float rotation = Projectile.rotation;
        SpriteEffects flip = (Projectile.spriteDirection == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        Vector2 screenPos = Main.screenPosition;

        SpriteBatch spriteBatch = Main.spriteBatch;

        Vector2 drawOrigin = clip.Centered();

        for (int k = 0; k < Projectile.oldPos.Length - 1; k += 1) {
            Vector2 drawPos = Projectile.oldPos[k] + new Vector2(Projectile.width, Projectile.height) / 2f + Vector2.UnitY * Projectile.gfxOffY - Main.screenPosition;
            position = drawPos;

            float scaleFactor = Helper.Wave(0.75f, 1.5f, 20f, k * 10);

            position = drawPos;
            rotation = (float)Math.Atan2(Projectile.oldPos[k].Y - Projectile.oldPos[k + 1].Y, Projectile.oldPos[k].X - Projectile.oldPos[k + 1].X);
            EternalHorror.DrawContext drawContext = new(spriteBatch, position, texture, clip, drawColor, rotation, flip, screenPos);
            EternalHorror.DrawUnderGlowEffect(drawContext, (newPosition, newColor) => {
                Color color = true ? new Color(60 - k * 5, 10, 60 + k * 4, 40 + k * 4) : new Color(60 + k * 4, 20 - k, 10 + k * 4, 60 + k * 4);
                color = Color.White;
                color = color.MultiplyRGBA(newColor);
                color = color.MultiplyRGBA(EternalHorror.MainPurpleColor);
                drawColor = color;

                newPosition = Vector2.Lerp(newPosition, position, 0.875f);

                newPosition += Vector2.UnitY.RotatedBy(Projectile.identity + k + Projectile.oldRot[k] + Main.GlobalTimeWrappedHourly * -15f) * 10f * Projectile.spriteDirection;

                drawColor *= 0.2f;

                rotation = (float)Math.Atan2(Projectile.oldPos[k].Y - Projectile.oldPos[k + 1].Y, Projectile.oldPos[k].X - Projectile.oldPos[k + 1].X);
                spriteBatch.Draw(texture, newPosition, null, drawColor, Projectile.oldRot[k], drawOrigin, (Projectile.scale - k / (float)Projectile.oldPos.Length) * scaleFactor, Projectile.spriteDirection.ToSpriteEffects(), 0f);
                spriteBatch.Draw(texture, newPosition - Projectile.oldPos[k] * 0.5f + Projectile.oldPos[k + 1] * 0.5f, null, drawColor, Projectile.oldRot[k], drawOrigin, (Projectile.scale - k / (float)Projectile.oldPos.Length) * scaleFactor, Projectile.spriteDirection.ToSpriteEffects(), 0f);
            }, sinWaveOffset: Projectile.identity + MathHelper.Pi,
               applyInnerOpacity: false,
               forcedOpacity: MathHelper.Lerp(0.125f, 0.25f, 0f),
               sinWaveOffset_BasedOnEffectIndex: MathHelper.TwoPi * 0.25f);
        }

        return false;
    }

    public override Color? GetAlpha(Color lightColor)
        => new Color(255, 255, 255, 200);
}
