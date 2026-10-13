using Consolaria.Common.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed partial class EternalHorror : ModNPC {
    private record struct RayInfo(float Angle,
                                  float Progress = 0f) {
        public readonly bool Active => Progress > 0f;
    }

    public readonly record struct DrawContext(SpriteBatch SpriteBatch, Vector2 Position, Texture2D Texture, Rectangle Clip, Color DrawColor, float Rotation, SpriteEffects Flip, Vector2 ScreenPosition);

    private static Asset<Texture2D> _eyesTexture = null,
                                    _glowTexture = null,
                                    _shadowTexture = null,
                                    _backgroundTexture = null,
                                    _secondPhaseEyeTexture = null,
                                    _secondPhaseEyeTexture_Glow = null,
                                    _pulseTexture = null,
                                    _pulseTexture2 = null,
                                    _pulseTexture3 = null;

    public static Asset<Texture2D> RayTexture { get; private set; }

    public static bool CanDrawOnTopOfBlinking { get; private set; }

    private static float _shakeIntensity;

    public static float ShakeStrength;

    private float _seed;
    private float _raySpeed;

    private float _glowOpacity,
                  _shadowProgress,
                  _shadowTime,
                  _dashOpacity,
                  _copiesIntensity;

    private float _eyeEffectOpacity;

    private float _secondPhaseOpacity;

    private RayInfo[] _rayData = null;

    private void InitializeRays() {
        _rayData = new RayInfo[RAYCOUNTMAX];

        _seed = Main.rand.NextFloat(100f);
    }

    private void SpawnRays(float startRaySpeed = 0f) {
        _raySpeed = startRaySpeed;

        for (int i = 0; i < SPAWNRAYCOUNTONSPAWN; i++) {
            SpawnRay();
        }
    }

    private void SpawnRay() {
        int index = 0;
        for (; index < _rayData.Length; index++) {
            RayInfo rayInfo = _rayData[index];
            if (!rayInfo.Active) {
                break;
            }
        }
        float angle = NPC.rotation + index / (float)SPAWNRAYCOUNTONSPAWN * MathHelper.TwoPi;
        _rayData[index] = new RayInfo(Angle: angle,
                                      Progress: RAYSTARTPROGRESS);
    }

    private void HandleRays() {
        if (!Init) {
            return;
        }

        _raySpeed += 1 / 60f;

        ulong seed = (ulong)_seed;

        float getRandomValue() => Utils.RandomFloat(ref seed);

        for (int i = 0; i < _rayData.Length; i++) {
            ref RayInfo rayInfo = ref _rayData[i];
            if (!rayInfo.Active) {
                continue;
            }

            float angleFactor = 1f;

            float rayProgress = rayInfo.Progress;
            rayProgress = Helper.Clamp01(rayProgress);
            int direction = (i % 2 == 0).ToDirectionInt();
            rayInfo.Angle += _raySpeed * rayProgress * direction 
                * MathHelper.Lerp(0.1f, 0.25f, 0.5f) * 1f * 0.25f
                * Utils.Remap(getRandomValue(), 0f, 1f, 1f, 0.25f, true) * angleFactor;

            rayInfo.Progress = Helper.Approach(rayInfo.Progress, 0f, 1 / 60f * 1.5f);
        }

        for (int i = 0; i < 1; i++) {
            if (Main.rand.NextChance(Ease.CubeOut(GetAllRayProgress())) && Main.rand.NextBool()) {
                Color colorTint = Color.Lerp(MainRedColor_Dynamic, MainPurpleColor, 1f/*Main.rand.NextFloat()*/) * 0.5f;

                Vector2 position = NPC.Center
                    + Vector2.One.RotatedBy(NPC.rotation).RotatedByRandom(MathHelper.TwoPi) * new Vector2(NPC.width, NPC.height) * 0.5f * new Vector2(Main.rand.NextFloat(0.5f, 1f), Main.rand.NextFloat(0.5f, 1f));
                Vector2 velocity = NPC.velocity;
                position -= velocity;
                velocity *= 0.2f;
                velocity += Vector2.Normalize(NPC.Center - position) * Main.rand.NextFloat(1f, 2f);
                FadingParticle fadingParticle = ParticlePools.FadingParticlePool.RequestParticle();
                fadingParticle.SetBasicInfo(TextureAssets.Star[0], null, velocity, position);
                float num = 25f/* * Main.rand.NextFloat(0.5f, 1f)*/;
                fadingParticle.SetTypeInfo(num);
                fadingParticle.AccelerationPerFrame = velocity / num;
                fadingParticle.ColorTint = colorTint;
                fadingParticle.FadeInNormalizedTime = 0.5f;
                fadingParticle.FadeOutNormalizedTime = 0.5f;
                fadingParticle.Rotation = Main.rand.NextFloat() * ((float)Math.PI * 2f);
                fadingParticle.Scale = Vector2.One * (0.5f + 0.5f * Main.rand.NextFloat()) * 1.5f;
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
        }

        //if (Main.rand.NextBool(1) && Main.rand.NextChance(GetAllRayProgress())) {
        //    int index2 = Dust.NewDust(new Vector2(NPC.Center.X, NPC.Center.Y), 0, 0, DustID.LavaMoss, 0f, 0f, 100, new Color(255, 0, 244), Main.rand.NextFloat(1f, 1.5f));
        //    Main.dust[index2].noGravity = true;
        //    Main.dust[index2].noLight = false;
        //    Main.dust[index2].fadeIn = Main.rand.NextFloat(0, 1.5f);
        //    Main.dust[index2].position += new Vector2(Main.rand.Next(80, 120), 0).RotatedByRandom(MathHelper.TwoPi);
        //    Main.dust[index2].velocity = Vector2.Normalize(NPC.Center - Main.dust[index2].position) * Main.rand.NextFloat(1f, 2f);
        //}
    }

    public float GetAllRayProgress() {
        float rayAllProgress = 0f;
        if (Init) {
            for (int i = 0; i < _rayData.Length; i++) {
                if (_rayData[i].Active) {
                    rayAllProgress += _rayData[i].Progress;
                }
            }
            rayAllProgress /= _rayData.Length;
        }
        rayAllProgress /= 2f;
        return rayAllProgress;
    }

    private float WaveOffset => NPC.whoAmI;

    private partial void Load_Textures() {
        _eyesTexture = Helper.RequestTexture(Texture + "_Eyes");
        _glowTexture = Helper.RequestTexture(Texture + "_Glow");
        _shadowTexture = Helper.RequestTexture(Texture + "_Shadow");
        _backgroundTexture = Helper.RequestTexture(Texture + "_Background");
        RayTexture = Helper.RequestTexture(Texture + "_Ray");

        _secondPhaseEyeTexture = Helper.RequestTexture(Texture + "_Eye");
        _secondPhaseEyeTexture_Glow = Helper.RequestTexture(Texture + "_Eye_Glow");

        _pulseTexture = Helper.RequestTexture(Texture + "_ClonePulse");
        _pulseTexture2 = Helper.RequestTexture(Texture + "_ClonePulse2");
        _pulseTexture3 = Helper.RequestTexture(Texture + "_ClonePulse3");
    }

    private partial void Load_ApplyShaderEffects() {
        On_Main.UpdateTime += On_Main_UpdateTime;
    }

    private void On_Main_UpdateTime(On_Main.orig_UpdateTime orig) {
        orig();

        ApplyShaderEffects_Inner();
    }

    private static void ApplyShaderEffects_Inner() {
        if (Main.dedServ) {
            return;
        }

        MakeScreenShake();
    }

    public static Color MainPurpleColor => new(175, 85, 255);
    public static Color MainPurpleColor_Dynamic => Color.Lerp(new(175, 85, 255), Color.Lerp(new(198, 123, 173), new(131, 186, 64), 0.5f), Helper.Wave(0f, 1f, 1f, 0f));

    public static Color MainRedColor_Dynamic => Color.Lerp(new(255, 10, 25), MainPurpleColor_Dynamic, Helper.Wave(0f, 1f, 25f, 0f) * 0.25f);

    public override void FindFrame(int frameHeight) {
        int frameCount = NPC.GetMaxFrames();
        int lastFrame = IsInSecondPhase ? frameCount : 3;

        void playIdleAnimation() {
            int frame = NPC.GetCurrentFrame(frameHeight);
            ref double frameCounter = ref NPC.frameCounter;
            int frameTime = 6;

            int frameCount = NPC.GetMaxFrames();
            int phase2FirstFrame = frameCount / 2;

            if (IsInSecondPhase) {
                if (frame < phase2FirstFrame) {
                    frame = phase2FirstFrame;
                }
            }

            if (++frameCounter >= frameTime) {
                frameCounter = 0;
                frame++;
                if (frame >= lastFrame) {
                    frame = IsInSecondPhase ? phase2FirstFrame : 0;
                }
            }
            NPC.SetCurrentFrame(frame, frameHeight);
        }

        playIdleAnimation();
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
        if (Init || NPC.IsABestiaryIconDummy) {
            drawColor = NPC.GetNPCColorTintedByBuffs(npcColor: drawColor);
            drawColor = Color.Lerp(drawColor, Color.White, 0.5f);

            Draw(spriteBatch, screenPos, drawColor, rayOpacity: 1f - SecondPhaseOpacity);
        }

        return false;
    }

    private static DrawContext GetBasicDrawContext(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
        Texture2D texture = npc.GetTexture();
        SpriteEffects flip = (-npc.spriteDirection).ToSpriteEffects();
        flip = SpriteEffects.None;
        Vector2 position = npc.Center;
        float rotation = npc.rotation;
        Rectangle clip = npc.frame;

        DrawContext drawContext = new(spriteBatch, position, texture, clip, drawColor, rotation, flip, screenPos);

        return drawContext;
    }

    private void Draw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, float rayOpacity = 1f) {
        Texture2D texture = NPC.GetTexture(),
                  glowTexture = _glowTexture.Value,
                  shadowTexture = _shadowTexture.Value;
        Rectangle glowClip = glowTexture.Frame(1, 2, frameY: IsInSecondPhase ? 1 : 0);

        DrawContext drawContext = GetBasicDrawContext(NPC, spriteBatch, screenPos, drawColor);

        SpriteEffects flip = drawContext.Flip;
        Vector2 position = drawContext.Position;
        float rotation = drawContext.Rotation;

        void drawShadows() {
            drawContext = drawContext with { Texture = shadowTexture };
            DrawUnderShadowEffect(drawContext, draw: (newPosition, newColor) => {
                ShaderLoader.DistortShader.SetDefault(shadowTexture.Width * 2, shadowTexture.Height * 2);
                ShaderLoader.DistortShader.Anxiety = 0.5f + 0.5f * _shadowProgress;
                ShaderLoader.ApplyEffect(ShaderLoader.DistortShader.Effect, spriteBatch, () => {
                    Draw_Inner(NPC, drawContext with {
                        Position = newPosition,
                        DrawColor = newColor,
                    });
                });
            }, sinWaveOffset: WaveOffset,
               progress: _shadowProgress,
               opacity: 0.375f,
               sinStep: _shadowTime);
        }
        void drawSelf() {
            drawContext = drawContext with { Texture = texture };

            int num240 = 120;
            int num241 = 60;

            float step = num241 * _copiesIntensity;

            float amount8 = 0f;

            float num247 = 1f - (float)Math.Cos((step - (float)num240) / (float)num241 * ((float)Math.PI * 2f));
            num247 /= 3f;
            float num248 = 60f;

            Color npcColor = drawColor;

            Microsoft.Xna.Framework.Color value65 = Microsoft.Xna.Framework.Color.White;

            int num246 = 6;
            for (int num251 = 0; num251 < num246; num251++) {
                NPC rCurrentNPC = NPC;
                Microsoft.Xna.Framework.Color value67 = npcColor;
                value67 = Microsoft.Xna.Framework.Color.Lerp(value67, value65, amount8);
                value67 = rCurrentNPC.GetAlpha(value67);
                value67 *= 1f - num247;
                value67 *= 0.375f;
                Vector2 position23 = rCurrentNPC.Center + ((float)num251 / (float)num246 * ((float)Math.PI * 2f) + rCurrentNPC.rotation).ToRotationVector2() 
                    * num248 * num247;
                Draw_Inner(NPC, drawContext with { 
                    Position = position23,
                    DrawColor = value67
                });
                //mySpriteBatch.Draw(value64, position23, rCurrentNPC.frame, value67, rCurrentNPC.rotation, halfSize, rCurrentNPC.scale, spriteEffects, 0f);
            }

            Draw_Inner(NPC, drawContext);
        }
        void drawGlowingEyes() {
            Texture2D eyesTexture = _eyesTexture.Value;
            drawContext = drawContext with { Texture = eyesTexture };
            DrawUnderGlowEffect(drawContext, draw: (newPosition, newColor) => {
                Draw_Inner(NPC, drawContext with {
                    Position = newPosition,
                    DrawColor = newColor,
                });
            }, sinWaveOffset: WaveOffset);
        }
        Color getLaserGlowColor(Color color) => GetLaserGlowColor(color, IsInSecondPhase) * _glowOpacity;
        void drawLaserLine() {
            float rotation = Phase1LaserRotation;

            Vector2 vector = NPC.Center - Main.screenPosition;
            int num = 40;
            int num2 = 180 * num;
            num2 /= 2;
            Microsoft.Xna.Framework.Color color = MainRedColor_Dynamic;
            Microsoft.Xna.Framework.Color color2 = color;
            color.A = 0;
            color2.A /= 2;
            Texture2D value = TextureAssets.Extra[ExtrasID.FairyQueenLance].Value;
            Vector2 origin = value.Frame().Size() * new Vector2(0f, 0.5f);
            Vector2 scale = new Vector2(num2 / value.Width, 2f);
            Vector2 scale2 = new Vector2((float)(num2 / value.Width) * 0.5f, 2f);
            Color color3 = color;
            //spriteBatch.Draw(value, vector, null, color3, rotation, origin, scale2, SpriteEffects.None, 0f);
            //spriteBatch.Draw(value, vector, null, color3 * 0.3f, rotation, origin, scale, SpriteEffects.None, 0f);
            //Microsoft.Xna.Framework.Color color3 = color * Utils.GetLerpValue(60f, 55f, proj.localAI[0], clamped: true) * Utils.GetLerpValue(0f, 10f, proj.localAI[0], clamped: true);
            drawContext = new DrawContext(spriteBatch, vector, value, value.Bounds, color3, rotation, drawContext.Flip, Main.screenPosition);
            DrawUnderGlowEffect(drawContext, draw: (newPosition, newColor) => {
                newColor = getLaserGlowColor(newColor);
                newColor *= Phase1BurstLaserAttackProgress;
                spriteBatch.Draw(drawContext.Texture, newPosition, null, newColor, drawContext.Rotation, origin, scale2, drawContext.Flip, 0f);
                spriteBatch.Draw(drawContext.Texture, newPosition, null, newColor * 0.3f, drawContext.Rotation, origin, scale, drawContext.Flip, 0f);
            }, sinWaveOffset: WaveOffset + MathHelper.Pi,
               applyInnerOpacity: false,
               forcedOpacity: MathHelper.Lerp(0.125f, 0.25f, 1f),
               sinWaveOffset_BasedOnEffectIndex: MathHelper.TwoPi * 0.25f,
               sinStep: AICounter);
            //Texture2D value2 = TextureAssets.Projectile[proj.type].Value;
            //Vector2 origin2 = value2.Frame().Size() / 2f;
            //Microsoft.Xna.Framework.Color color4 = Microsoft.Xna.Framework.Color.White * Utils.GetLerpValue(0f, 20f, proj.localAI[0], clamped: true);
            //color4.A /= 2;
            //float num3 = MathHelper.Lerp(0.7f, 1f, Utils.GetLerpValue(55f, 60f, proj.localAI[0], clamped: true));
            //float lerpValue = Utils.GetLerpValue(10f, 60f, proj.localAI[0]);
            //if (lerpValue > 0f) {
            //    float lerpValue2 = Utils.GetLerpValue(0f, 1f, proj.velocity.Length(), clamped: true);
            //    for (float num4 = 1f; num4 > 0f; num4 -= 1f / 6f) {
            //        Vector2 vector2 = rotation.ToRotationVector2() * -120f * num4 * lerpValue2;
            //        spriteBatch.Draw(value2, vector + vector2, null, color * lerpValue * (1f - num4), rotation, origin2, num3, SpriteEffects.None, 0f);
            //        spriteBatch.Draw(value2, vector + vector2, null, new Microsoft.Xna.Framework.Color(255, 255, 255, 0) * 0.15f * lerpValue * (1f - num4), rotation, origin2, num3 * 0.85f, SpriteEffects.None, 0f);
            //    }

            //    for (float num5 = 0f; num5 < 1f; num5 += 0.25f) {
            //        Vector2 vector3 = (num5 * ((float)Math.PI * 2f) + rotation).ToRotationVector2() * 2f * num3;
            //        spriteBatch.Draw(value2, vector + vector3, null, color2 * lerpValue, rotation, origin2, num3, SpriteEffects.None, 0f);
            //    }

            //    spriteBatch.Draw(value2, vector, null, color2 * lerpValue, rotation, origin2, num3 * 1.1f, SpriteEffects.None, 0f);
            //}

            //spriteBatch.Draw(value2, vector, null, color4, rotation, origin2, num3, SpriteEffects.None, 0f);
        }
        void drawLaserGlow() {
            drawContext = drawContext with { 
                Texture = glowTexture,
                Clip = glowClip
            };
            Draw_Inner(NPC, drawContext with { DrawColor = getLaserGlowColor(drawColor) });
            DrawUnderGlowEffect(drawContext, draw: (newPosition, newColor) => {
                newColor = getLaserGlowColor(newColor);
                Draw_Inner(NPC, drawContext with {
                    Position = newPosition,
                    DrawColor = newColor,
                });
            }, sinWaveOffset: WaveOffset + MathHelper.Pi,
               applyInnerOpacity: false,
               forcedOpacity: MathHelper.Lerp(0.125f, 0.25f, 1f),
               sinWaveOffset_BasedOnEffectIndex: MathHelper.TwoPi * 0.25f,
               sinStep: AICounter * (IsInSecondPhase ? 5f : 1f));
        }
        void drawClones() {
            if (!Init) {
                return;
            }
            foreach (CloneInfo cloneInfo in _cloneData) {
                if (!cloneInfo.Active) {
                    continue;
                }

                float explosionProgress = cloneInfo.ExplosionProgress;

                float explosionProgress2 = cloneInfo.ExplosionProgress2;

                DrawRays_Inner(spriteBatch, NPC.As<EternalHorror>(),
                    true, cloneInfo.Opacity * MathHelper.Lerp(0.25f, 0.375f, 0.5f) * explosionProgress2, explosionProgress2, cloneInfo.VisualPosition);

                if (IsInSecondPhase) {
                    ShaderLoader.WavyCircleShader.Time = _seed + Main.GlobalTimeWrappedHourly * 5;
                    ShaderLoader.WavyCircleShader.WaveCount1 = 6f;
                    ShaderLoader.WavyCircleShader.WaveCount2 = 12f;
                    ShaderLoader.WavyCircleShader.WaveSize1 = 0.04f;
                    ShaderLoader.WavyCircleShader.WaveSize2 = 0.04f;
                    ShaderLoader.WavyCircleShader.WaveRadius = 1f - explosionProgress * 0.5f;
                    ShaderLoader.ApplyEffect(ShaderLoader.WavyCircleShader.Effect, spriteBatch, () => {
                        float _knockProgress2 = 120f * explosionProgress;

                        var circleTexture = _pulseTexture3.Value;
                        var circlePosition = cloneInfo.VisualPosition - screenPos;
                        var circleClip = Utils.Frame(circleTexture, 1, 1, frameY: 0);
                        var circleOrigin = circleClip.Centered();
                        var circleColor = Color.Lerp(MainPurpleColor_Dynamic, MainPurpleColor, explosionProgress);
                        float circleScale = Utils.GetLerpValue(74f, 120f, _knockProgress2, true);

                        circleColor.A = 0;

                        circleColor *= 1f - circleScale;

                        float progress2 = Ease.SineOut(explosionProgress);
                        circleColor *= progress2;

                        Vector2 baseScale = Vector2.One;
                        baseScale *= 2f * 1.5f; 
                        //baseScale.Y *= 0.875f;

                        Vector2 scaleFinal = baseScale * Ease.CircOut(circleScale);

                        var circleDrawInfo = new Helper.DrawInfo() {
                            Clip = circleClip,
                            Origin = circleOrigin,
                            Color = circleColor,
                            //Rotation = cloneInfo.Rotation
                        };
                        for (int num185 = 0; num185 < 4; num185++) {
                            spriteBatch.Draw(circleTexture, circlePosition + Vector2.UnitX.RotatedBy((float)num185 * ((float)Math.PI / 4f) - Math.PI) * 1f,
                                circleDrawInfo.WithColorModifier(MathHelper.Lerp(0.125f, 0.25f, 0.125f) * 0.75f).WithScaleOverride(scaleFinal),
                                false);
                        }
                    });
                }

                float rotation = cloneInfo.Rotation;

                Player target = NPC.GetTargetPlayer();
                Vector2 clonePosition = cloneInfo.VisualPosition;
                Color cloneColor = drawColor;
                float timeLeftProgress = cloneInfo.TimeLeftProgress;

                cloneColor *= cloneInfo.Opacity;
                DrawContext cloneDrawContext = drawContext with {
                    Texture = shadowTexture,
                    Position = clonePosition,
                    DrawColor = cloneColor
                };

                int length = cloneInfo.OldVisualPositions.Length - 1;
                for (int num173 = 1; num173 < length; num173 += 1) {
                    _ = ref cloneInfo.OldVisualPositions[num173];
                    Color color39 = cloneDrawContext.DrawColor;
                    color39 = color39.MultiplyRGBA(MainPurpleColor);
                    color39.R = (byte)(1f * (double)(int)color39.R * (double)(length - num173) / length);
                    color39.G = (byte)(1f * (double)(int)color39.G * (double)(length - num173) / length);
                    color39.B = (byte)(1f * (double)(int)color39.B * (double)(length - num173) / length);
                    color39.A = (byte)(1f * (double)(int)color39.A * (double)(length - num173) / length);
                    //color39 *= MathHelper.Clamp(NPC.velocity.Length(), 0f, 9f) / 9f;
                    color39 *= 1f - num173 / length;
                    //color39 *= _trailOpacity;
                    //color39 *= 0.8f;
                    color39 *= 1f;
                    color39 *= cloneInfo.DashOpacity;
                    Rectangle frame7 = NPC.frame;
                    Vector2 origin = NPC.frame.Centered();
                    Vector2 pos = cloneInfo.OldVisualPositions[num173];
                    //pos += NPC.Size / 2f;

                    color39 *= 0.5f;

                    pos = Vector2.Lerp(pos, cloneInfo.VisualPosition, Ease.SineIn(1f - cloneInfo.DashOpacity));

                    pos -= screenPos;

                    ShaderLoader.DistortShader.SetDefault(shadowTexture.Width * 2, shadowTexture.Height * 2);
                    ShaderLoader.ApplyEffect(ShaderLoader.DistortShader.Effect, spriteBatch, () => {
                        spriteBatch.Draw(shadowTexture,
                        pos,
                        frame7, color39 * NPC.Opacity, rotation, origin, NPC.scale, flip, 0f);
                    });
                }

                float starOpacityExtra = cloneInfo.AllStarOpacityFactor;

                DrawUnderShadowEffect(cloneDrawContext, draw: (newPosition, newColor) => {
                    Vector2 position = newPosition;
                    Color color = newColor;

                    ShaderLoader.DistortShader.SetDefault(shadowTexture.Width * 2, shadowTexture.Height * 2);
                    ShaderLoader.ApplyEffect(ShaderLoader.DistortShader.Effect, spriteBatch, () => {
                        Draw_Inner(NPC, cloneDrawContext with {
                            Position = position,
                            DrawColor = color,
                            Rotation = rotation,
                        }/*, scale: NPC.scale + Ease.CubeIn(explosionProgress) * 0.25f*/);
                    });
                }, sinWaveOffset: WaveOffset,
                   progress: Utils.Remap(timeLeftProgress, 0f, 1f, MathHelper.Lerp(0.375f, 0.5f, 0.5f), 1f, true) * 0.25f,
                   opacity: 0.375f + starOpacityExtra + cloneInfo.DashOpacity / 4f + explosionProgress2 / 4f,
                   sinStep: _shadowTime);

                foreach (CloneStarInfo cloneStarInfo in cloneInfo.CloneStarData) {
                    if (!cloneStarInfo.Active) {
                        continue;
                    }

                    //Vector2 vector43 = Utils.Vector2FromElipse(rCurrentNPC.localAI[0].ToRotationVector2(), vector38 * rCurrentNPC.localAI[1]);
                    float num149 = 1f;
                    float num153 = cloneStarInfo.Progress;

                    float framesBeforeSmolBeam = num149 / 2;
                    float framesBeforeSmolBeam2 = num149 / 10;

                    Vector2 baseStarPosition = cloneInfo.VisualPosition + cloneStarInfo.Position;

                    Vector2 starPosition = cloneInfo.VisualPosition.DirectionTo(baseStarPosition);
                    float starMaxRotation = (float)num153 * ((float)Math.PI * 2f / num149) * 0.125f * 0.5f;

                    starMaxRotation *= (cloneStarInfo.Rotation < MathHelper.Pi).ToDirectionInt();

                    float starScale = Utils.Remap(num153, num149 - (float)framesBeforeSmolBeam2 - (float)framesBeforeSmolBeam, num149 - (float)framesBeforeSmolBeam2, 1f, 0f);
                    float starRotation = Utils.Remap(num153, num149 - (float)framesBeforeSmolBeam2 - (float)framesBeforeSmolBeam, num149 - (float)framesBeforeSmolBeam2, 0f, 1f);

                    starScale *= 1f;

                    ShaderLoader.DistortShader.SetDefault(100, 100);
                    ShaderLoader.ApplyEffect(ShaderLoader.DistortShader.Effect, spriteBatch, () => {
                        for (int i = 0; i < 1; i++) {
                            Helper.DrawPrettyStarSparkle(1f, SpriteEffects.None, baseStarPosition - screenPos + starPosition,
                                Color.Lerp(Color.White, new Color(175, 31, 178), 0.25f) * 1f,
                                Color.Lerp(MainPurpleColor, Color.BlueViolet, 0.75f) * 1f, starRotation, 0f, 0.5f, 0.9f, 1f,
                                starMaxRotation * starRotation * starRotation + cloneStarInfo.Rotation,
                                new Vector2(10f, 6f) * starScale * starScale, new Vector2(3f, 3f));
                            //Utils.DrawLine(spriteBatch, rCurrentNPC.Center + starPosition + starOffset, rCurrentNPC.Center + starPosition * 30f + starOffset, Microsoft.Xna.Framework.Color.Cyan * starRotation, Microsoft.Xna.Framework.Color.Transparent, 16f * starScale);
                        }
                    });
                }
            }
        }
        void drawTrails() {
            int length = NPC.oldPos.Length - 1;
            for (int num173 = 1; num173 < length; num173 += 1) {
                _ = ref NPC.oldPos[num173];
                Color color39 = drawColor;
                color39 = color39.MultiplyRGBA(MainPurpleColor_Dynamic);
                color39.R = (byte)(1f * (double)(int)color39.R * (double)(length - num173) / length);
                color39.G = (byte)(1f * (double)(int)color39.G * (double)(length - num173) / length);
                color39.B = (byte)(1f * (double)(int)color39.B * (double)(length - num173) / length);
                color39.A = (byte)(1f * (double)(int)color39.A * (double)(length - num173) / length);
                //color39 *= MathHelper.Clamp(NPC.velocity.Length(), 0f, 9f) / 9f;
                color39 *= 1f - num173 / length;
                //color39 *= _trailOpacity;
                //color39 *= 0.8f;
                color39 *= 1f;
                color39 *= _dashOpacity;
                Rectangle frame7 = NPC.frame;
                Vector2 origin = NPC.frame.Centered();
                Vector2 pos = NPC.oldPos[num173];
                pos += NPC.Size / 2f;

                pos = Vector2.Lerp(pos, NPC.Center, Ease.SineIn(1f - _dashOpacity));

                pos -= screenPos;

                ShaderLoader.DistortShader.SetDefault(shadowTexture.Width * 2, shadowTexture.Height * 2);
                ShaderLoader.ApplyEffect(ShaderLoader.DistortShader.Effect, spriteBatch, () => {
                    spriteBatch.Draw(shadowTexture,
                    pos,
                    frame7, color39 * NPC.Opacity, NPC.rotation, origin, NPC.scale, flip, 0f);
                });
            }
        }

        drawTrails();
        DrawRays(NPC, spriteBatch, rayOpacity);
        drawShadows();
        drawClones();
        drawSelf();
        drawGlowingEyes();
        drawLaserGlow();
        if (!IsInSecondPhase) {
            drawLaserSpamWarning(screenPos);
        }
        //drawLaserLine();
    }

    private void drawLaserSpamWarning(Vector2 screenPos) {
        if (!HasActiveState<Phase1LaserSpamAttack>() && !HasActiveState<Phase2LaserSpamAttack>()) {
            return;
        }

        NPC rCurrentNPC = NPC;

        //Vector2 vector43 = Utils.Vector2FromElipse(rCurrentNPC.localAI[0].ToRotationVector2(), vector38 * rCurrentNPC.localAI[1]);
        float num149 = !IsInSecondPhase ? Phase1LaserSpamAttack.BEFORESPAMTIME : Phase2LaserSpamAttack.BEFORESPAMTIME;
        float num153 = AICounter;

        float framesBeforeSmolBeam = num149 / 2;
        float framesBeforeSmolBeam2 = num149 / 10;

        Vector2 rotationVector2 = Vector2.UnitY.RotatedBy(NPC.rotation);
        int starCount = IsInSecondPhase ? 2 : 3;
        for (int i = 0; i < starCount; i++) {
            Vector2 starPosition = NPC.Center.DirectionTo(NPC.GetTargetPlayer().Center);
            float starMaxRotation = (float)num153 * ((float)Math.PI * 2f / num149) * 1.5f;

            starMaxRotation *= ((i + 1) % 2 == 0).ToDirectionInt();

            float starScale = Utils.Remap(num153, num149 - (float)framesBeforeSmolBeam2 - (float)framesBeforeSmolBeam, num149 - (float)framesBeforeSmolBeam2, 1f, 0f);
            float starRotation = Utils.Remap(num153, num149 - (float)framesBeforeSmolBeam2 - (float)framesBeforeSmolBeam, num149 - (float)framesBeforeSmolBeam2, 0f, 1f);

            starScale *= 1.5f;

            float starProgress = (float)i / starCount;
            float offset = 13f;
            if (i == 1) {
                offset = 42f;

                if (IsInSecondPhase) {
                    offset *= 1.125f;
                }

                starScale *= 0.875f;
                starRotation *= 0.875f;
            }
            else if (i == 2) {
                offset = 62f;

                starScale *= 0.75f;
                starRotation *= 0.75f;
            }
            Vector2 starOffset = rotationVector2 * -offset;

            Helper.DrawPrettyStarSparkle(1f, SpriteEffects.None, rCurrentNPC.Center - screenPos + starPosition + starOffset,
                Color.Lerp(Color.White, IsInSecondPhase ? MainPurpleColor : MainRedColor_Dynamic,
                Utils.Remap(Ease.SineIn(IsInSecondPhase ? Phase2LaserSpamAttackProgress : Phase1LaserSpamAttackProgress), 0f, 1f, 0.25f, 1f, true)),
                IsInSecondPhase ? MainPurpleColor : MainRedColor_Dynamic, starRotation, 0f, 0.5f, 0.9f, 1f,
                starMaxRotation * starRotation * starRotation,
                new Vector2(10f, 6f) * starScale * starScale, new Vector2(3f, 3f));
            //Utils.DrawLine(spriteBatch, rCurrentNPC.Center + starPosition + starOffset, rCurrentNPC.Center + starPosition * 30f + starOffset, Microsoft.Xna.Framework.Color.Cyan * starRotation, Microsoft.Xna.Framework.Color.Transparent, 16f * starScale);
        }
    }

    private static void DrawRays_Inner(SpriteBatch spriteBatch, EternalHorror boss, bool explosion, float opacity, float forcedProgress = 0f, Vector2? position = null, bool firstHalf = true, bool secondHalf = true) {
        ulong seed = (ulong)boss._seed;

        void drawRay(int index, float angle, float progress2) {
            float rayWaveOffset = index;
            float rayWaveOffset2 = (rayWaveOffset + 1) * 2;

            float waveSpeed = MathHelper.Lerp(2.5f, 3.75f, 0.5f);

            float rotation = angle;

            if (explosion) {
                float rotation2 = 0.125f * 0.5f;
                rotation += Helper.Wave(-rotation2, rotation2, waveSpeed, rayWaveOffset2);
            }

            float rayProgress = progress2;
            float step = 0.125f;
            rayProgress *= Utils.GetLerpValue(0f, step, rayProgress, true);
            rayProgress *= Utils.GetLerpValue(RAYSTARTPROGRESS, RAYSTARTPROGRESS - step * 2f, rayProgress, true);

            float getRandomValue() => Utils.RandomFloat(ref seed);
            float getRemappedRandomValue(float min, float max) => Utils.Remap(getRandomValue(), 0f, 1f, min, max, true);

            Vector2 scale = Vector2.One;
            scale *= 1f;
            if (!explosion) {
                scale.X *= 2f * getRemappedRandomValue(0.5f, 1f);
                scale.Y *= 2.5f * getRemappedRandomValue(0.25f, 1f);
            }
            else {
                scale.X *= 2f * getRemappedRandomValue(0.25f, MathHelper.Lerp(0.375f, 0.5f, 0.5f));
                scale.Y *= 2.5f * getRemappedRandomValue(0.75f, 1f);

                scale.X *= 1.5f;
                scale.Y *= 1.5f;
            }

            scale.Y *= rayProgress;

            if (explosion) {
                scale.Y *= Helper.Wave(0.875f, 1f, waveSpeed, rayWaveOffset2);
            }

            Color color = MainPurpleColor;
            color *= Helper.Wave(0.75f, 1f, 15f, rayWaveOffset);
            color *= MathHelper.Lerp(0.25f, 0.375f, 0.5f);

            color *= rayProgress;

            Texture2D texture = RayTexture.Value;
            Rectangle clip = texture.Frame(2, 1, frameX: explosion ? 1 : 0);
            Vector2 origin = clip.Centered();
            Helper.DrawInfo drawInfo = new() {
                Clip = clip,
                Origin = origin,
                Rotation = rotation,
                Color = color,
                Scale = scale
            };

            Vector2 position2 = position ?? boss.NPC.Center;
            Vector2 angleDirection = Vector2.UnitY.RotatedBy(rotation);
            angleDirection *= origin.Y / 3f;
            if (explosion) {
                angleDirection *= getRemappedRandomValue(1.25f, 2f);
            }
            position2 += angleDirection;

            ShaderLoader.DistortShader.SetDefault(texture.Width * 2, texture.Height * 2);
            ShaderLoader.ApplyEffect(ShaderLoader.DistortShader.Effect, spriteBatch, () => {
                int drawCount = 3;
                for (int k = 0; k < drawCount; k++) {
                    float progress = k / (float)drawCount;
                    progress -= 0.5f;
                    spriteBatch.Draw(texture, position2,
                        drawInfo
                        .WithRotation(progress * 0.125f * 0.5f)
                        .WithColorOverride(
                            Color.Lerp(color,
                            Main.hslToRgb(getRandomValue(), 1f, 0.5f),
                            getRemappedRandomValue(0f, 0.125f))
                            .MultiplyAlpha(0f) * opacity));
                }
            });
        }

        if (!explosion) {
            for (int i = 0; i < boss._rayData.Length; i++) {
                if (!firstHalf && i > EternalHorrorCloneExplosion.SKULLCOUNT / 2) {
                    continue;
                }
                if (!secondHalf && i <= EternalHorrorCloneExplosion.SKULLCOUNT / 2) {
                    continue;
                }

                RayInfo rayInfo = boss._rayData[i];
                if (!rayInfo.Active) {
                    continue;
                }

                drawRay(i, rayInfo.Angle, forcedProgress == 0f ? rayInfo.Progress : forcedProgress);
            }

            return;
        }

        for (int i = 0; i < EternalHorrorCloneExplosion.SKULLCOUNT; i++) {
            if (!firstHalf && i % 2 == 0) {
                continue;
            }
            if (!secondHalf && i % 2 != 0) {
                continue;
            }

            float angle = MathHelper.TwoPi * i / EternalHorrorCloneExplosion.SKULLCOUNT;
            drawRay(i, angle, forcedProgress);
        }
    }

    private static void DrawRays(NPC npc, SpriteBatch spriteBatch, float opacity = 1f, float forcedProgress = 0f, Vector2? position = null) {
        EternalHorror boss = npc.As<EternalHorror>();

        if (!boss.Init) {
            return;
        }

        DrawRays_Inner(spriteBatch, boss, false, opacity, forcedProgress: forcedProgress, position: position);
    }

    private void UpdateVisuals() {
        if (NPC.IsABestiaryIconDummy || !NPC.active) {
            return;
        }

        float lerpValue = 0.1f;
        float glowOpacity = 0f;
        if (HasActiveState<Phase1BurstLaserAttack>()) {
            glowOpacity = Phase1BurstLaserAttackProgress;
        }
        if (HasActiveState<Phase1LaserSpamAttack>()) {
            glowOpacity = Phase1LaserSpamAttackProgress;
        }
        if (HasActiveState<Phase2BurstLaserAttack>()) {
            glowOpacity = Phase2BurstLaserAttackProgress;
        }
        if (HasActiveState<Phase2LaserSpamAttack>()) {
            glowOpacity = Phase2LaserSpamAttackProgress;
        }
        _glowOpacity = Helper.Approach(_glowOpacity, glowOpacity, lerpValue);

        _eyeEffectOpacity = Helper.Approach(_eyeEffectOpacity, 0f, lerpValue);

        float shadowOpacity = 0f;
        if (HasActiveState<Phase1CloneSpawn>() || HasActiveState<Phase2CloneSpawn>()) {
            shadowOpacity = Phase1ShadowSpawnProgress;
        }
        else {
            lerpValue = 1f;
        }
        _shadowProgress = Helper.Approach(_shadowProgress, shadowOpacity, lerpValue);
        _shadowTime += 1 / 60f;
        //if (_shadowProgress <= 0f) {
        //    _shadowTime = 0;
        //}
        _dashOpacity = Helper.Approach(_dashOpacity, 0f, 1 / 60f);
        ShakeStrength = Helper.Approach(ShakeStrength, 0f, 1 / 60f);

        _copiesIntensity = Helper.Approach(_copiesIntensity, 0f, 1 / 60f);

        OnIterateActiveCloneData((ref cloneInfo) => {
            cloneInfo.DashOpacity = Helper.Approach(cloneInfo.DashOpacity, 0f, 1 / 60f);
        });
    }

    private static void MakeScreenShake() {
        if (Main.dedServ) {
            return;
        }

        ApplyShake();

        Vector2 shakeCenter = Main.LocalPlayer.Center;
        if (!ShaderLoader.EternalHorrorShakeFilter.IsActive()) {
            Filters.Scene.Activate(ShaderLoader.EternalHorrorShakeFilterName, shakeCenter);
        }
        float strength = MathHelper.Lerp(Helper.Wave(0.125f, 0.375f, 5f, 0f), 1f, ShakeStrength) * _shakeIntensity;
        Filters.Scene[ShaderLoader.EternalHorrorShakeFilterName].GetShader().UseIntensity(strength);
    }

    private static void ApplyShake() {
        float lerpValue = 0.1f;
        _shakeIntensity = Helper.Approach(_shakeIntensity, EternalHorrorSummonHandler.EternalHorrorSummonEnded.ToInt(), lerpValue);
    }

    public static Color GetLaserGlowColor(Color drawColor, bool secondPhase) => drawColor.MultiplyRGBA(secondPhase ? MainPurpleColor : MainRedColor_Dynamic);

    private static void Draw_Inner(NPC npc, DrawContext drawContext, float scale = 1f) {
        npc.QuickDraw(drawContext.SpriteBatch, drawContext.ScreenPosition, drawContext.DrawColor, scale: scale,
                                                                                                  frameBox: drawContext.Clip, 
                                                                                                  rotation: drawContext.Rotation, 
                                                                                                  position: drawContext.Position, 
                                                                                                  texture: drawContext.Texture, 
                                                                                                  effect: drawContext.Flip);
    }

    public static void DrawUnderShadowEffect(DrawContext drawContext, Action<Vector2, Color> draw, float sinWaveOffset = 0f,
                                                                                                   float progress = 0f,
                                                                                                   float countStep = MathHelper.PiOver2,
                                                                                                   float opacity = 1f,
                                                                                                   float sinStep = 0f,
                                                                                                   float offsetAmount = 32f,
                                                                                                   Func<float, Vector2> shadowPositionOffset = null) {
        Vector2 position = drawContext.Position;
        float rotation = drawContext.Rotation;
        Color color = drawContext.DrawColor;
        for (int i = 0; i < 1; i++) {
            for (float k = -MathHelper.Pi; k <= MathHelper.Pi; k += countStep) {
                Vector2 shadowPosition = position;
                float alpha = (progress >= 0.5f) ? (1f - (progress - 0.5f) / 0.5f) : (progress / 0.5f);
                float offset = alpha * offsetAmount;
                float time = sinStep == 0f ? Main.GlobalTimeWrappedHourly : sinStep;
                shadowPosition += Vector2.UnitX.RotatedBy(k + time + rotation) * (float)(offset + offset * 0.5f
                    * MathF.Sin(time * 4f));
                if (shadowPositionOffset != null) {
                    shadowPosition += shadowPositionOffset(k);
                }
                //shadowPosition.X += Helper.Wave(-1f, -1f, 5f, k + sinWaveOffset) * 10f * progress;
                //shadowPosition.Y += Helper.Wave(-1f, -1f, 5f, k + MathHelper.Pi + sinWaveOffset) * 10f * progress;
                Color shadowColor = color;
                shadowColor = shadowColor.MultiplyRGBA(MainPurpleColor);
                shadowColor = Color.Lerp(shadowColor, shadowColor.MultiplyRGBA(MainPurpleColor_Dynamic), 0.5f);
                shadowColor = shadowColor.MultiplyAlpha(alpha);
                shadowColor.A /= 1;
                shadowColor *= opacity;
                draw(shadowPosition, shadowColor);
            }
        }
    }

    public static void DrawUnderGlowEffect(DrawContext drawContext, Action<Vector2, Color> draw, float sinWaveOffset = 0f, 
                                                                                                 bool applyInnerOpacity = true, 
                                                                                                 float forcedOpacity = 1f,
                                                                                                 bool drawXEffect = true,
                                                                                                 bool drawYEffect = true,
                                                                                                 float sinWaveOffset_BasedOnEffectIndex = MathHelper.TwoPi * 0.5f,
                                                                                                 float? sinStep = null) {
        sinStep ??= Main.GlobalTimeWrappedHourly;
        float sinStep_Value = sinStep.Value;
        Vector2 position = drawContext.Position;
        float rotation = drawContext.Rotation;
        int shadowCount = 20;
        for (float k = 0f; k < MathHelper.TwoPi; k += MathHelper.TwoPi / 4f) {
            for (int i = shadowCount; i > 0; i--) {
                float shadowProgress = i / (float)shadowCount;
                Vector2 eyesPosition = position;
                Vector2 rotationDirection = -Vector2.UnitY.RotatedBy(rotation + k);
                float rotationOffsetValue = shadowCount * 2 * shadowProgress;
                eyesPosition += rotationDirection * rotationOffsetValue;
                Color eyesColor = Color.White;
                eyesColor.A = 0;
                eyesColor *= 1f - shadowProgress;
                bool x = k is MathHelper.PiOver2 or (MathHelper.Pi + MathHelper.PiOver2);
                if (!drawXEffect && x) {
                    continue;
                }
                if (!drawYEffect && !x) {
                    continue;
                }
                float getWaveFactor(float waveOffset = 0f) => Helper.Wave(sinStep_Value, 0.25f, 1f, 10f, sinWaveOffset + waveOffset);
                if (applyInnerOpacity) {
                    eyesColor *= getWaveFactor(0f);
                    eyesColor *= getWaveFactor(2f);
                    eyesColor *= getWaveFactor(4f);
                    eyesColor *= getWaveFactor(6f);
                }
                float kWaveOffset = x.ToInt() * sinWaveOffset_BasedOnEffectIndex;
                eyesColor *= Helper.Wave(sinStep_Value, 0.5f, 1f, 10f, sinWaveOffset + kWaveOffset);
                if (applyInnerOpacity) {
                    eyesColor *= 0.5f;
                }
                eyesColor *= forcedOpacity;

                draw(eyesPosition, eyesColor);
            }
        }
    }

    public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) { }

    private partial void Load_BlinkingDrawing() {
        EternalHorrorSummonHandler.OverBlinkingDrawEvent += EternalHorrorSummonHandler_OverBlinkingDrawEvent;
    }

    private static void EternalHorrorSummonHandler_OverBlinkingDrawEvent(SpriteBatch spriteBatch) {
        CanDrawOnTopOfBlinking = false;

        NPC boss = null;
        bool found = false;
        foreach (NPC npc in Main.ActiveNPCs) {
            if (npc.type == SelfType) {
                boss = npc;
                found = true;
                break;
            }
        }
        if (!found) {
            return;
        }
        EternalHorror boss_modNPC = boss.As<EternalHorror>();
        if (!boss_modNPC.IsInSecondPhase_Base) {
            CanDrawOnTopOfBlinking = true;
            return;
        }

        CanDrawOnTopOfBlinking = true;

        Color drawColor = Lighting.GetColor(boss.Center.ToTileCoordinates());
        drawColor = boss.GetNPCColorTintedByBuffs(npcColor: drawColor);
        drawColor = Color.Lerp(drawColor, Color.White, 0.5f);

        drawColor *= boss_modNPC.SecondPhaseOpacity;

        Vector2 screenPos = Main.screenPosition;

        DrawRays(boss, spriteBatch, opacity: 0.25f * boss_modNPC.SecondPhaseOpacity);

        boss_modNPC.Draw(spriteBatch, screenPos, drawColor * MathHelper.Lerp(0.125f, 0.25f, 0.5f), rayOpacity: 0f);

        DrawContext drawContext = GetBasicDrawContext(boss, spriteBatch, screenPos, drawColor);
        Texture2D eyeTexture = _secondPhaseEyeTexture.Value;
        Rectangle eyeClip = eyeTexture.Bounds;
        Vector2 eyePosition = boss_modNPC.GetEyePosition(forDrawing: true);

        Color eyeEffectColor = drawContext.DrawColor;

        float blink = MathF.Sin(boss_modNPC.AICounter / 4) / 2 + 0.5f;
        eyeEffectColor = new Color(0.875f - blink, 0.5f * blink, 2.5f * blink, 0) * boss_modNPC._eyeEffectOpacity;

        drawContext = drawContext with {
            Texture = eyeTexture,
            Clip = eyeClip,
            Position = eyePosition
        };

        ShaderLoader.DistortShader.SetDefault(eyeTexture.Width * 1, eyeTexture.Height * 1);
        float anxietyFactor = 0.125f;
        ShaderLoader.DistortShader.Anxiety = anxietyFactor + anxietyFactor * boss_modNPC.SecondPhaseOpacity;
        ShaderLoader.ApplyEffect(ShaderLoader.DistortShader.Effect, spriteBatch, () => {
            Draw_Inner(boss, drawContext);

            Draw_Inner(boss, drawContext with {
                Texture = _secondPhaseEyeTexture_Glow.Value,
                DrawColor = eyeEffectColor
            });
        });

        boss_modNPC.drawLaserSpamWarning(Main.screenPosition);

        Helper.SpriteBatchSnapshot snapshot = spriteBatch.CaptureSnapshot();

        for (int j = 0; j < 1000; j++) {
            if (Main.projectile[j].active && Main.projectile[j].ModProjectile is IEternalHorrorProjectile) {
                Main.instance.DrawProj(j);
            }
        }

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        Main.ParticleSystem_World_OverPlayers.Settings.AnchorPosition = -Main.screenPosition;
        Main.ParticleSystem_World_OverPlayers.Draw(spriteBatch);

        spriteBatch.Begin(in snapshot, end: true);

        CanDrawOnTopOfBlinking = false;
    }
}
