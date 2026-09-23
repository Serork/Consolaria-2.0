using Consolaria.Common.Particles;
using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using System;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Renderers;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using ThoriumMod.Empowerments;
using ThoriumMod.Projectiles;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed class EternalHorrorCloneExplosion : ModProjectile {
    private static float EXPLOSIONSCALEMODIFIER => 1.5f;

    public override string Texture => "Consolaria/Assets/Textures/Empty";

    public override void SetStaticDefaults() {
        
    }

    public override void SetDefaults() {
        Projectile.SetSizeValues(30);

        Projectile.aiStyle = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;

        Projectile.friendly = false;
        Projectile.hostile = true;

        Projectile.penetrate = -1;

        Projectile.manualDirectionChange = true;
    }

    public override void AI() {
        Projectile.timeLeft = 2;

        if (Projectile.localAI[0] < 0f) {
            Projectile.localAI[0] = Helper.Approach(Projectile.localAI[0], 0f, 0.1f);
        }

        if (Projectile.localAI[0] == 0f) {
            Projectile.localAI[0] = 1f;

            if (!Helper.IsClient()) {
                int skullCount = 15;
                const float SkullSpeed = 12f;
                for (int i = 0; i < skullCount; i++) {
                    Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), Projectile.Center,
                        Vector2.UnitY.RotatedBy(MathHelper.TwoPi * i / skullCount) * SkullSpeed * 2f,
                        ModContent.ProjectileType<EternalHorrorCloneExplosionSkull>(), Projectile.damage, Projectile.knockBack);
                }
            }

            Projectile.scale = 0f;

            ActivateShockwave(rippleCount: 3f * EXPLOSIONSCALEMODIFIER, rippleSize: 5f * EXPLOSIONSCALEMODIFIER, rippleSpeed: 10f * EXPLOSIONSCALEMODIFIER);

            int size = (int)(250 * EXPLOSIONSCALEMODIFIER);
            Projectile.Resize(size, size);

            Color getColor() => EternalHorror.MainPurpleColor;

            Color color1 = EternalHorror.MainPurpleColor;
            Color color2 = EternalHorror.MainPurpleColor;
            Color color3 = EternalHorror.MainPurpleColor;

            float dustSpeedModifier = 1.5f * EXPLOSIONSCALEMODIFIER;

            for (int num949 = 0; num949 < 8; num949++) {
                if (!Main.dedServ) {
                    Gore gore6 = Gore.NewGoreDirect(Projectile.GetSource_FromThis(), Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.height) * 0.125f, Vector2.Zero, 61 + Main.rand.Next(3));
                    gore6.velocity = ((float)Math.PI * 2f * (float)num949 / 8f).ToRotationVector2() * Main.rand.NextFloat() * 8f;

                    gore6.velocity *= dustSpeedModifier;
                }
            }
            for (int num950 = 0; num950 < 40; num950++) {
                Dust dust59 = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 31);
                dust59.velocity = Projectile.DirectionTo(dust59.position).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat() * 8f;

                dust59.velocity *= dustSpeedModifier;
            }

            for (float num5 = 0f; num5 < 1f; num5 += 1f / 15f) {
                Color colorTint = Main.rand.NextFromList([color1, color2, color3]) with { A = 127 };
                Dust dust = Dust.NewDustPerfect(Projectile.Center, 267, Vector2.Zero, 0, colorTint, 2f);
                dust.noGravity = true;
                dust.velocity = new Vector2(0f, 12f).RotatedBy((float)Math.PI * 2f * num5) * Main.rand.NextFloat() * 2f;

                dust.velocity *= dustSpeedModifier;
            }

            Vector2 position = Projectile.Center;
            for (float num2 = 0f; num2 < 1f; num2 += 0.04f) {
                float num3 = 25f;
                float num4 = (float)Math.PI * 2f * num2;
                FadingParticle fadingParticle5 = ParticlePools.ParticleOrchestrator__poolFading(null).RequestParticle();
                Color colorTint = Main.rand.NextFromList([color1, color2, color3]) with { A = 20 };
                fadingParticle5.SetBasicInfo(TextureAssets.Extra[89], null, num4.ToRotationVector2() * (4f + 7f * Main.rand.NextFloat()), position + num4.ToRotationVector2() * (10f + 90f * Main.rand.NextFloat()));
                fadingParticle5.Velocity *= Main.rand.NextFloat(1f, 1.5f);
                fadingParticle5.SetTypeInfo(num3);
                fadingParticle5.AccelerationPerFrame = fadingParticle5.Velocity * (-1f / num3);
                fadingParticle5.LocalPosition -= fadingParticle5.Velocity * 4f;
                fadingParticle5.ColorTint = colorTint;
                fadingParticle5.Rotation = num4 + (float)Math.PI / 2f;
                fadingParticle5.FadeInNormalizedTime = 0.2f;
                fadingParticle5.FadeOutNormalizedTime = 0.3f;
                fadingParticle5.Scale = new Vector2(0.4f, 0.8f) * (0.6f + 0.6f * Main.rand.NextFloat());

                fadingParticle5.Velocity *= dustSpeedModifier;

                Main.ParticleSystem_World_OverPlayers.Add(fadingParticle5);
            }

            for (int num915 = 0; num915 < 30; num915++) {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.TintableDustLighted);
                //dust.position = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.height) / 2f * Projectile.scale;
                dust.noGravity = true;
                dust.scale = 1.4f;
                dust.velocity.Y += 1f * Main.rand.NextFloat();
                dust.velocity.X *= Main.rand.NextFloat();
                dust.alpha = 100;
                dust.color = getColor();
                dust.fadeIn = 0.4f + Main.rand.NextFloat() * 0.15f;
                //while (Projectile.Hitbox.Contains(dust.position.ToPoint())) {
                //    dust.position -= dust.position.DirectionTo(Projectile.Center);
                //}
                //dust.position += dust.position.DirectionTo(Projectile.Center) * 10f * Main.rand.NextFloat();
                if (Main.rand.NextBool()) {
                    dust.velocity *= new Vector2(Main.rand.NextFloat(), Main.rand.NextFloat());
                }
                dust.velocity = Projectile.DirectionTo(dust.position).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat() * 8f;

                dust.velocity *= dustSpeedModifier;
            }

            Projectile.Resize(30, 30);
        }

        HandleShockwave(distortStrength: 100f * (1f - Projectile.scale) * EXPLOSIONSCALEMODIFIER, progress: Projectile.scale * EXPLOSIONSCALEMODIFIER);

        Lighting.AddLight(Projectile.Center, EternalHorror.MainPurpleColor.ToVector3() * Projectile.scale * EXPLOSIONSCALEMODIFIER * 20f * (1f - (float)Math.Sqrt(Projectile.scale * EXPLOSIONSCALEMODIFIER)));

        Projectile.scale = Helper.Approach(Projectile.scale, 1f, 1f / 60 * 3f);
        if (Projectile.localAI[0] >= 0f && Projectile.scale >= 1f) {
            Projectile.localAI[0] = -5f;

            DeactivateShockwave();

            Projectile.Kill();
        }
    }

    public override void OnKill(int timeLeft) {
        DeactivateShockwave();
    }

    public override bool PreDraw(ref Color lightColor) {
        SpriteBatch mySpriteBatch = Main.spriteBatch;
        Matrix Transform = Main.Transform;
        SamplerState DefaultSamplerState = Main.DefaultSamplerState;
        RasterizerState Rasterizer = Main.Rasterizer;

        SpriteEffects spriteEffects = SpriteEffects.None;

        Vector2 vector65 = Projectile.Center - Main.screenPosition;
        Vector2 vector66 = vector65 - new Vector2(300f, 310f);

        mySpriteBatch.End();
        mySpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.Default, RasterizerState.CullNone, null, Transform);
        float num234 = Projectile.scale;
        DrawData value62 = new DrawData(Main.Assets.Request<Texture2D>("Images/Misc/Perlin").Value, 
            vector66 + new Vector2(300f, 300f), 
            new Microsoft.Xna.Framework.Rectangle(0, 0, 600, 600),
            new Microsoft.Xna.Framework.Color(new Vector4(1f - (float)Math.Sqrt(num234)) * 0.75f), 
            Projectile.rotation, 
            new Vector2(300f, 300f),
            Projectile.scale * (1f + num234) * new Vector2(1.5f, 1f) * 0.9375f * EXPLOSIONSCALEMODIFIER, spriteEffects);
        GameShaders.Misc["ForceField"].UseColor(EternalHorror.MainPurpleColor);
        GameShaders.Misc["ForceField"].Apply(value62);
        value62.Draw(mySpriteBatch);
        mySpriteBatch.End();
        mySpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, DefaultSamplerState, DepthStencilState.None, Rasterizer, null, Transform);

        return base.PreDraw(ref lightColor);
    }

    private void HandleShockwave(float distortStrength, float progress) {
        if (Helper.IsServer()) {
            return;
        }

        if (ShaderLoader.EternalHorrorShockwaveFilter.IsActive()) {
            var shaderData = ShaderLoader.EternalHorrorShockwaveFilter.GetShader();

            shaderData
                .UseProgress(progress)
                .UseOpacity(distortStrength * (1f - progress / 3f));
        }
    }

    private void ActivateShockwave(float rippleCount, float rippleSize, float rippleSpeed) {
        if (Helper.IsServer()) {
            return;
        }

        if (!ShaderLoader.EternalHorrorShockwaveFilter.IsActive()) {
            var shaderData = Filters.Scene.Activate(ShaderLoader.EternalHorrorShockwaveFilterName, Projectile.Center).GetShader();

            shaderData
                .UseColor(rippleCount, rippleSize, rippleSpeed)
                .UseTargetPosition(Projectile.Center);
        }
    }

    private void DeactivateShockwave() {
        if (Helper.IsServer()) {
            return;
        }

        if (ShaderLoader.EternalHorrorShockwaveFilter.IsActive()) {
            ShaderLoader.EternalHorrorShockwaveFilter.Deactivate();
        }
    }
}
