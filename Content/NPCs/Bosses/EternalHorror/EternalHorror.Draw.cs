using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed partial class EternalHorror : ModNPC {
    public readonly record struct DrawContext(SpriteBatch SpriteBatch, Vector2 Position, Texture2D Texture, Rectangle Clip, Color DrawColor, float Rotation, SpriteEffects Flip, Vector2 ScreenPosition);

    private static Asset<Texture2D> _eyeTexture = null,
                                    _glowTexture = null,
                                    _shadowTexture = null,
                                    _backgroundTexture = null;

    private static float _shakeIntensity;

    public static float ShakeStrength;

    private float _glowOpacity,
                  _shadowProgress,
                  _shadowTime,
                  _dashOpacity,
                  _copiesIntensity;

    private float WaveOffset => NPC.whoAmI;

    private partial void Load_Textures() {
        _eyeTexture = Helper.RequestTexture(Texture + "_Eyes");
        _glowTexture = Helper.RequestTexture(Texture + "_Glow");
        _shadowTexture = Helper.RequestTexture(Texture + "_Shadow");
        _backgroundTexture = Helper.RequestTexture(Texture + "_Background");
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
        int phase1LastFrame = 3;
        void playPhase1IdleAnimation() {
            int frame = NPC.GetCurrentFrame(frameHeight);
            ref double frameCounter = ref NPC.frameCounter;
            int frameTime = 6;
            if (++frameCounter >= frameTime) {
                frameCounter = 0;
                frame++;
                if (frame >= phase1LastFrame) {
                    frame = 0;
                }
            }
            NPC.SetCurrentFrame(frame, frameHeight);
        }

        playPhase1IdleAnimation();
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
        Draw(spriteBatch, screenPos, drawColor);

        return false;
    }

    private void Draw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
        drawColor = NPC.GetNPCColorTintedByBuffs(npcColor: drawColor);
        drawColor = Color.Lerp(drawColor, Color.White, 0.5f);
        Texture2D texture = NPC.GetTexture(),
                  glowTexture = _glowTexture.Value,
                  shadowTexture = _shadowTexture.Value;
        SpriteEffects flip = (-NPC.spriteDirection).ToSpriteEffects();
        flip = SpriteEffects.None;
        Vector2 position = NPC.Center;
        float rotation = NPC.rotation;
        Rectangle clip = NPC.frame,
                  glowClip = glowTexture.Bounds;

        DrawContext drawContext = new(spriteBatch, position, texture, clip, drawColor, rotation, flip, screenPos);

        void drawShadows() {
            drawContext = drawContext with { Texture = shadowTexture };
            DrawUnderShadowEffect(drawContext, draw: (newPosition, newColor) => {
                ShaderLoader.DistortShader.SetDefault(shadowTexture.Width * 2, shadowTexture.Height * 2);
                ShaderLoader.DistortShader.Anxiety = 0.5f + 0.5f * _shadowProgress;
                ShaderLoader.ApplyEffect(ShaderLoader.DistortShader.Effect, spriteBatch, () => {
                    Draw_Inner(drawContext with {
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
                Draw_Inner(drawContext with { 
                    Position = position23,
                    DrawColor = value67
                });
                //mySpriteBatch.Draw(value64, position23, rCurrentNPC.frame, value67, rCurrentNPC.rotation, halfSize, rCurrentNPC.scale, spriteEffects, 0f);
            }

            Draw_Inner(drawContext);
        }
        void drawGlowingEyes() {
            Texture2D eyesTexture = _eyeTexture.Value;
            drawContext = drawContext with { Texture = eyesTexture };
            DrawUnderGlowEffect(drawContext, draw: (newPosition, newColor) => {
                Draw_Inner(drawContext with {
                    Position = newPosition,
                    DrawColor = newColor,
                });
            }, sinWaveOffset: WaveOffset);
        }
        Color getLaserGlowColor(Color color) => GetLaserGlowColor(color) * _glowOpacity;
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
            Draw_Inner(drawContext with { DrawColor = getLaserGlowColor(drawColor) });
            DrawUnderGlowEffect(drawContext, draw: (newPosition, newColor) => {
                newColor = getLaserGlowColor(newColor);
                Draw_Inner(drawContext with {
                    Position = newPosition,
                    DrawColor = newColor,
                });
            }, sinWaveOffset: WaveOffset + MathHelper.Pi,
               applyInnerOpacity: false,
               forcedOpacity: MathHelper.Lerp(0.125f, 0.25f, 1f),
               sinWaveOffset_BasedOnEffectIndex: MathHelper.TwoPi * 0.25f,
               sinStep: AICounter);
        }
        void drawClones() {
            if (!Init) {
                return;
            }
            foreach (CloneInfo cloneInfo in _cloneData) {
                if (!cloneInfo.Active) {
                    continue;
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
                        Draw_Inner(cloneDrawContext with {
                            Position = position,
                            DrawColor = color,
                            Rotation = rotation
                        });
                    });
                }, sinWaveOffset: WaveOffset,
                   progress: Utils.Remap(timeLeftProgress, 0f, 1f, MathHelper.Lerp(0.375f, 0.5f, 0.5f), 1f, true) * 0.25f,
                   opacity: 0.375f + starOpacityExtra + cloneInfo.DashOpacity / 4f,
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
        void drawLaserSpamWarning() {
            if (!HasActiveState<Phase1LaserSpamAttack>()) {
                return;
            }

            NPC rCurrentNPC = NPC;

            //Vector2 vector43 = Utils.Vector2FromElipse(rCurrentNPC.localAI[0].ToRotationVector2(), vector38 * rCurrentNPC.localAI[1]);
            float num149 = Phase1LaserSpamAttack.BEFORESPAMTIME;
            float num153 = AICounter;

            float framesBeforeSmolBeam = num149 / 2;
            float framesBeforeSmolBeam2 = num149 / 10;

            Vector2 rotationVector2 = Vector2.UnitY.RotatedBy(NPC.rotation);
            int starCount = 3;
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
                    Color.Lerp(Color.White, MainRedColor_Dynamic, 
                    Utils.Remap(Ease.SineIn(Phase1LaserSpamAttackProgress), 0f, 1f, 0.25f, 1f, true)),
                    MainRedColor_Dynamic, starRotation, 0f, 0.5f, 0.9f, 1f,
                    starMaxRotation * starRotation * starRotation,
                    new Vector2(10f, 6f) * starScale * starScale, new Vector2(3f, 3f));
                //Utils.DrawLine(spriteBatch, rCurrentNPC.Center + starPosition + starOffset, rCurrentNPC.Center + starPosition * 30f + starOffset, Microsoft.Xna.Framework.Color.Cyan * starRotation, Microsoft.Xna.Framework.Color.Transparent, 16f * starScale);
            }
        }

        drawTrails();
        drawShadows();
        drawClones();
        drawSelf();
        drawGlowingEyes();
        drawLaserGlow();
        drawLaserSpamWarning();
        //drawLaserLine();
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
        _glowOpacity = Helper.Approach(_glowOpacity, glowOpacity, lerpValue);
        float shadowOpacity = 0f;
        if (HasActiveState<Phase1CloneSpawn>()) {
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

    public static Color GetLaserGlowColor(Color drawColor) => drawColor.MultiplyRGBA(MainRedColor_Dynamic);

    private void Draw_Inner(DrawContext drawContext) {
        NPC.QuickDraw(drawContext.SpriteBatch, drawContext.ScreenPosition, drawContext.DrawColor, frameBox: drawContext.Clip, 
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
}
