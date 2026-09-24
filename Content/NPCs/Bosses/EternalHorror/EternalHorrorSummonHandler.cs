using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ModLoader;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed class EternalHorrorSummonHandler : ModSystem {
    private static ushort TIMEBEFOREBOSSSPAWN => Helper.SecondsToFrames(2);
    private static byte EYESPAWNCYCLECOUNT => 4;
    private static float EYESCALEMAX => 5f;

    private record struct EyeInfo(int TimeLeft,
                                  int MaxTimeLeft,
                                  Vector2 PositionOffset,
                                  Vector2 TargetPosition,
                                  Vector2 Velocity = default,
                                  float EyeRotation = 0f,
                                  Vector2 PupilVelocity = default,
                                  bool ShouldLookAtPlayer = false,
                                  float RunAwayProgress = 0f,
                                  float Scale = 0f) {
        public readonly bool Active => TimeLeft > 0;
    }

    private static Asset<Texture2D> _eyeTexture1 = null,
                                    _eyeTexture2 = null;

    private static VertexPositionColor[] _blinkingVertexes = null;
    private static EyeInfo[] _eyeData = null;

    private static int _eyeSpawnCD,
                       _eyeSpawnCycle;

    private static bool _shouldBlink;
    private static bool _in;
    private static float _progress = 1f,
                         _delay,
                         _speedFactor;

    private static int _bossSpawnCounter;

    public static bool EternalHorrorSummonStarted;
    public static bool EternalHorrorSummonEnded { get; private set; }
    public static bool EternalHorrorShouldBeSummoned { get; private set; }

    public static void StartSummoningEternalHorror() {
        if (EternalHorrorSummonStarted) {
            return;
        }

        EternalHorrorSummonStarted = true;
        _eyeData = new EyeInfo[400];
    }

    public override void Load() {
        _blinkingVertexes = new VertexPositionColor[192];

        On_ScreenObstruction.Draw += On_ScreenObstruction_Draw;

        if (!Main.dedServ) {
            _eyeTexture1 = ModContent.Request<Texture2D>("Consolaria/Content/NPCs/Bosses/EternalHorror/EternalHorror_WanderingEye1");
            _eyeTexture2 = ModContent.Request<Texture2D>("Consolaria/Content/NPCs/Bosses/EternalHorror/EternalHorror_WanderingEye2");
        }
    }

    public override void Unload() {
        _blinkingVertexes = null;
    }

    private void ResetSummoning() {
        if (!EternalHorrorSummonEnded) {
            return;
        }

        EternalHorrorSummonEnded = false;
        EternalHorrorSummonStarted = false;

        _eyeSpawnCD = 0;
        _eyeSpawnCycle = 0;

        //Main.dayTime = true;
        //Main.time = Main.dayLength / 2;

        _in = false;

        _bossSpawnCounter = 0;

        EternalHorrorShouldBeSummoned = false;
    }

    public override void PostUpdateNPCs() {
        HandleBlinking();
        HandleEyes();

        bool bossAlive = NPC.AnyNPCs(EternalHorror.SelfType);
        if (EternalHorrorSummonEnded && !bossAlive) {
            EternalHorror.MakeMidnight();
        }

        if (!bossAlive && EternalHorrorShouldBeSummoned) {
            ResetSummoning();
        }

        if (!EternalHorrorSummonStarted) {
            return;
        }

        if (EternalHorrorSummonEnded) {
            _bossSpawnCounter++;

            float bossSpawnProgress = _bossSpawnCounter / (float)TIMEBEFOREBOSSSPAWN;
            if (!EternalHorrorShouldBeSummoned) {
                EternalHorror.ShakeStrength = Helper.Approach(EternalHorror.ShakeStrength, bossSpawnProgress * 0.75f, 0.125f / 3f);
            }

            if (_bossSpawnCounter >= TIMEBEFOREBOSSSPAWN) {
                if (!EternalHorrorShouldBeSummoned) {
                    EternalHorrorShouldBeSummoned = true;

                    NPC.SpawnOnPlayer(Main.LocalPlayer.whoAmI, EternalHorror.SelfType);
                }
            }

            return;
        }

        _eyeSpawnCD++;

        int spawnTime = 30;

        if (_eyeSpawnCycle > EYESPAWNCYCLECOUNT) {
            _eyeSpawnCycle = 0;

            Blink();

            _eyeSpawnCD = -spawnTime * 12;
        }

        Player player = Main.LocalPlayer;
        Vector2 playerCenter = player.Center;

        if (_eyeSpawnCD > spawnTime) {
            _eyeSpawnCD = 0;
            if (_eyeSpawnCycle < 0) {
                _eyeSpawnCycle = 0;
            }
            int countFactor = _eyeSpawnCycle + 1;
            int baseCount = 15;
            int eyeCount = baseCount * countFactor;
            float distanceFromPlayer = baseCount * 10 * countFactor;
            for (int i = 0; i < eyeCount; i++) {
                Vector2 position = playerCenter + Vector2.UnitY.RotatedBy(i / (float)eyeCount * MathHelper.TwoPi) * distanceFromPlayer;
                //position.Y += (position.DirectionTo(playerCenter) * distanceFromPlayer * 0.5f).Y;
                SpawnEye(position);
            }
            _eyeSpawnCycle++;
        }

        //if (_eyeSpawnCD > 0) {
        //    _eyeSpawnCD--;
        //    return;
        //}
        //if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.NumPad1)) {
        //    SpawnEye(Main.MouseWorld);

        //    _eyeSpawnCD = 10;
        //}
    }

    public static void Blink(float speedFactor = 3.5f, float delay = 0.5f) {
        if (_shouldBlink) {
            return;
        }

        _shouldBlink = true;
        _delay = delay;
        _speedFactor = speedFactor;
    }

    private void On_ScreenObstruction_Draw(On_ScreenObstruction.orig_Draw orig, SpriteBatch spriteBatch) {
        DrawEyes(spriteBatch);
        DrawBlinking(spriteBatch);

        orig(spriteBatch);
    }

    private static void SpawnEye(Vector2 position) {
        if (!EternalHorrorSummonStarted) {
            return;
        }

        Player player = Main.LocalPlayer;
        Vector2 playerCenter = player.Center;

        Vector2 velocity = position.DirectionTo(playerCenter) * 5f;

        position -= playerCenter;

        position += -velocity * 20f;

        int index = 0;
        for (int i = 0; i < _eyeData.Length; i++) {
            if (!_eyeData[index].Active) {
                break;
            }
            index++;
        }
        int timeLeft = 360;
        _eyeData[index] = new EyeInfo(TimeLeft: timeLeft,
                                      MaxTimeLeft: timeLeft,
                                      PositionOffset: position,
                                      TargetPosition: playerCenter,
                                      Velocity: velocity);
    }

    private static void DrawEyes(SpriteBatch spriteBatch) {
        if (!EternalHorrorSummonStarted) {
            return;
        }

        Player player = Main.LocalPlayer;
        Vector2 playerCenter = player.Center;

        SpriteBatch batch = spriteBatch;

        //Helper.SpriteBatchSnapshot snapshot = batch.CaptureSnapshot();
        //batch.End();
        //batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

        Texture2D eyeTexture1 = _eyeTexture1.Value;
        Texture2D eyeTexture2 = _eyeTexture2.Value;
        for (int i = 0; i < _eyeData.Length; i++) {
            EyeInfo eyeInfo = _eyeData[i];
            if (!eyeInfo.Active) {
                continue;
            }

            Vector2 position = eyeInfo.TargetPosition + eyeInfo.PositionOffset;

            Rectangle clip = eyeTexture1.Bounds;
            Vector2 origin = clip.Centered();

            Color drawColor = Lighting.GetColor(position.ToTileCoordinates());
            drawColor = Color.Lerp(drawColor, Color.White, 0.5f);

            //float scaleFactor = 1f - Utils.GetLerpValue(1f, EYESCALEMAX, eyeInfo.Scale, true);
            //scaleFactor = Ease.CubeIn(scaleFactor);

            //Main.NewText(scaleFactor);

            float scaleFactor = 1f - eyeInfo.RunAwayProgress;
            Vector2 baseScale = Vector2.One * new Vector2(1f, scaleFactor);

            Vector2 eyeScale = eyeInfo.Scale * baseScale;

            drawColor = Color.Lerp(drawColor, drawColor.MultiplyRGBA(EternalHorror.MainPurpleColor) * scaleFactor, 1f - scaleFactor);

            Color color = drawColor;

            color = color.MultiplyAlpha(Helper.Wave(0.75f, 1f, 15f, i));

            float rotation = eyeInfo.EyeRotation;
            Helper.DrawInfo drawInfo = new() {
                Clip = clip,
                Origin = origin,
                Color = color * 0.75f,
                Rotation = rotation,
                Scale = eyeScale
            };

            Vector2 eyePupilScale = Ease.CubeIn(eyeInfo.Scale) * baseScale;

            ShaderLoader.DistortShader.SetDefault(eyeTexture1.Width * 2, eyeTexture1.Height * 2);
            ShaderLoader.DistortShader.Strength = MathF.Max((_shouldBlink || EternalHorrorSummonEnded).ToInt(), _eyeSpawnCycle / (float)EYESPAWNCYCLECOUNT);
            ShaderLoader.ApplyEffect(ShaderLoader.DistortShader.Effect, spriteBatch, () => {
                batch.Draw(eyeTexture1, position, drawInfo);

                float minScale = 1f,
                      maxScale = 1f;

                float eyeScaleFactor = 0.125f;
                float eyeScaleWaveSpeedFactor = 2.5f;

                if (eyeInfo.ShouldLookAtPlayer) {
                    eyeScaleFactor = MathHelper.Lerp(0.25f, 0.375f, 0f);
                    eyeScaleWaveSpeedFactor = 12.5f;

                    //maxScale *= 0.875f;
                }

                Vector2 eyePupilPosition = position + eyeInfo.PupilVelocity;
                batch.Draw(eyeTexture2, eyePupilPosition, drawInfo
                    .WithRotation(0f)
                    .WithColorOverride(color)
                    .WithScaleOverride(eyePupilScale * Helper.Wave(minScale - eyeScaleFactor, maxScale + eyeScaleFactor / 2f, eyeScaleWaveSpeedFactor, i)));
            });
        }

        //batch.End();
        //batch.Begin(snapshot);
    }

    private static void HandleEyes() {
        if (!EternalHorrorSummonStarted) {
            return;
        }

        Player player = Main.LocalPlayer;

        bool bossSpawned = EternalHorrorShouldBeSummoned;

        ulong seed = 0u;

        for (int i = 0; i < _eyeData.Length; i++) {
            ref EyeInfo eyeInfo = ref _eyeData[i];
            if (!eyeInfo.Active) {
                continue;
            }

            Vector2 playerCenter = player.Center;

            float offsetY = 10f;
            playerCenter.Y += Helper.Wave(-offsetY, offsetY, 1f, i);

            eyeInfo.TimeLeft--;

            eyeInfo.PositionOffset += eyeInfo.Velocity;

            eyeInfo.Velocity *= 0.95f;

            Vector2 position = eyeInfo.TargetPosition + eyeInfo.PositionOffset;

            Vector2 angleToPlayer = position.DirectionTo(playerCenter);
            float eyeRotation = angleToPlayer.ToRotation() * 0.125f;

            Vector2 bossCenter = playerCenter;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (npc.type == EternalHorror.SelfType) {
                    bossCenter = npc.Center;
                    break;
                }
            }

            float getDistanceFactor(float maxDistance = 300f) => Helper.Clamp01(position.Distance(playerCenter) / maxDistance);

            if (eyeInfo.ShouldLookAtPlayer) {
                eyeInfo.Velocity += position.DirectionTo(playerCenter) * 0.125f * 0.25f * getDistanceFactor();

                float maxRotation = 0.25f * 0.75f;
                eyeInfo.EyeRotation = Helper.Wave(-maxRotation, maxRotation, 3.75f, i);

                eyeInfo.PupilVelocity = 
                    Vector2.Lerp(eyeInfo.PupilVelocity, 
                    position.DirectionTo(bossSpawned ? bossCenter : playerCenter) * 5f, 
                    0.25f) + Main.rand.NextVector2Circular(1f, 1f);
            }

            if (bossSpawned) {
                //eyeInfo.Scale = Helper.Approach(eyeInfo.Scale, EYESCALEMAX, 0.1f);

                eyeInfo.RunAwayProgress = Helper.Approach(eyeInfo.RunAwayProgress, 1f, 0.025f * Utils.Remap(Utils.RandomFloat(ref seed), 0f, 1f, 0.5f, 1f, true));

                //float progressFactor = eyeInfo.RunAwayProgress;
                //progressFactor = Ease.BounceIn(progressFactor);
                //eyeInfo.Velocity += position.DirectionFrom(bossCenter) * 2.5f * progressFactor;
            }
            else {
            }

            eyeInfo.Scale = Helper.Approach(eyeInfo.Scale, 1f, 0.1f * Utils.Remap(Utils.RandomFloat(ref seed), 0f, 1f, 0.75f, 1f, true));

            eyeInfo.TargetPosition = Vector2.Lerp(eyeInfo.TargetPosition, playerCenter, 0.75f);
        }
    }

    private static void HandleBlinking() {
        if (_shouldBlink) {
            EternalHorror.ShakeStrength = Helper.Approach(EternalHorror.ShakeStrength, 1f, 0.125f);
            EternalHorrorSummonEnded = true;

            float lerpValue = 1 / 60f;
            lerpValue *= _speedFactor;
            float progressOffset = _delay;

            if (EternalHorrorSummonStarted && _progress <= 0f) {
                for (int i = 0; i < _eyeData.Length; i++) {
                    ref EyeInfo eyeInfo = ref _eyeData[i];
                    if (!eyeInfo.Active) {
                        continue;
                    }

                    eyeInfo.ShouldLookAtPlayer = true;
                }
            }

            if (_in) {
                float to = 1f;
                _progress = Helper.Approach(_progress, to, lerpValue);
                if (_progress >= to) {
                    if (_in) {
                        _shouldBlink = false;
                    }

                    _in = false;
                }
            }
            else {
                float to = 0f - progressOffset * 1f;
                _progress = Helper.Approach(_progress, to, lerpValue);
                if (_progress <= to) {
                    _in = true;
                }
            }
        }
    }

    private static void DrawBlinking(SpriteBatch spriteBatch) {
        Helper.SpriteBatchSnapshot snapshot = spriteBatch.CaptureSnapshot();
        spriteBatch.End();

        _blinkingVertexes = new VertexPositionColor[192];
        for (int i2 = 0; i2 < _blinkingVertexes.Length; i2++) {
            _blinkingVertexes[i2].Color = EternalHorror.MainPurpleColor_Dynamic.ModifyRGB(0.125f) * 0.95f;
        }
        int num = (int)Main.screenWidth;
        int num2 = (int)Main.screenHeight;
        float num3 = _progress;
        num3 = Helper.Clamp01(num3);
        Vector2 vector = new Vector2(num, num2) / 2f;
        float num4 = num * 0.55f;
        float num5 = num4 * 0.55f;
        float num6 = num5 * 0.45f * Ease.CubeOut(num3);
        float num7 = -num5 * num3;
        float num8 = num5 * num3;
        float num9 = -num5 - num6;
        float num10 = num5 + num6;
        int num11 = 16;
        int i = 0;
        for (int j = 0; j < num11; j++) {
            float num12 = j / (float)num11;
            float num13 = (j + 1) / (float)num11;
            float num14 = MathHelper.Lerp(-num4, num4, num12);
            float num15 = MathHelper.Lerp(-num4, num4, num13);
            float num16 = MathF.Sin(num12 * MathF.PI);
            float num17 = MathF.Sin(num13 * MathF.PI);
            float num18 = num9 - num6 * num16;
            float num19 = num9 - num6 * num17;
            float num20 = MathF.Sin(num12 * MathF.PI);
            float num21 = MathF.Sin(num13 * MathF.PI);
            float num22 = num7 - num6 * 0.6f * num20;
            float num23 = num7 - num6 * 0.6f * num21;
            Vector3 vector2 = new Vector3(vector.X + num14, vector.Y + num18, 0f);
            Vector3 vector3 = new Vector3(vector.X + num15, vector.Y + num19, 0f);
            Vector3 vector4 = new Vector3(vector.X + num14, vector.Y + num22, 0f);
            Vector3 vector5 = new Vector3(vector.X + num15, vector.Y + num23, 0f);
            _blinkingVertexes[i++].Position = vector2;
            _blinkingVertexes[i++].Position = vector3;
            _blinkingVertexes[i++].Position = vector4;
            _blinkingVertexes[i++].Position = vector3;
            _blinkingVertexes[i++].Position = vector5;
            _blinkingVertexes[i++].Position = vector4;
        }
        for (int k = 0; k < num11; k++) {
            float num24 = k / (float)num11;
            float num25 = (k + 1) / (float)num11;
            float num26 = MathHelper.Lerp(-num4, num4, num24);
            float num27 = MathHelper.Lerp(-num4, num4, num25);
            float num28 = MathF.Sin(num24 * MathF.PI);
            float num29 = MathF.Sin(num25 * MathF.PI);
            float num30 = num10 + num6 * num28;
            float num31 = num10 + num6 * num29;
            float num32 = MathF.Sin(num24 * MathF.PI);
            float num33 = MathF.Sin(num25 * MathF.PI);
            float num34 = num8 + num6 * 0.6f * num32;
            float num35 = num8 + num6 * 0.6f * num33;
            Vector3 vector6 = new Vector3(vector.X + num26, vector.Y + num30, 0f);
            Vector3 vector7 = new Vector3(vector.X + num27, vector.Y + num31, 0f);
            Vector3 vector8 = new Vector3(vector.X + num26, vector.Y + num34, 0f);
            Vector3 vector9 = new Vector3(vector.X + num27, vector.Y + num35, 0f);
            _blinkingVertexes[i++].Position = vector6;
            _blinkingVertexes[i++].Position = vector7;
            _blinkingVertexes[i++].Position = vector8;
            _blinkingVertexes[i++].Position = vector7;
            _blinkingVertexes[i++].Position = vector9;
            _blinkingVertexes[i++].Position = vector8;
        }
        Matrix view = Main.GameViewMatrix.TransformationMatrix;
        Matrix projection = Matrix.CreateOrthographicOffCenter(0, num, num2, 0, 0, 1);
        Effect effect = ShaderLoader.Primitive.Value;
        effect.Parameters["World"].SetValue(view * projection);
        effect.CurrentTechnique.Passes[0].Apply();
        GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
        RasterizerState previousState = graphicsDevice.RasterizerState;
        graphicsDevice.RasterizerState = RasterizerState.CullNone;
        graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, _blinkingVertexes, 0, _blinkingVertexes.Length / 3);
        graphicsDevice.RasterizerState = previousState;

        spriteBatch.Begin(snapshot);
    }
}
