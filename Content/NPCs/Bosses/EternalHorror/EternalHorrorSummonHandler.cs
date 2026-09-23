using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed class EternalHorrorSummonHandler : ModSystem {
    private record struct EyeInfo(int TimeLeft,
                                  int MaxTimeLeft,
                                  Vector2 PositionOffset,
                                  Vector2 TargetPosition,
                                  Vector2 Velocity = default,
                                  float EyeRotation = 0f,
                                  Vector2 PupilVelocity = default,
                                  bool ShouldLookAtPlayer = false) {
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
    private static float _progress,
                         _delay,
                         _speedFactor;

    public static bool EternalHorrorSummonStarted;
    public static bool EternalHorrorShouldBeSummoned { get; private set; }

    public static void StartSummoningEternalHorror() {
        if (EternalHorrorSummonStarted) {
            return;
        }

        EternalHorrorSummonStarted = true;
        _eyeData = new EyeInfo[200];
    }

    public override void Load() {
        _blinkingVertexes = new VertexPositionColor[192];

        On_Main.DrawInterface += On_Main_DrawInterface;

        if (!Main.dedServ) {
            _eyeTexture1 = ModContent.Request<Texture2D>("Consolaria/Content/NPCs/Bosses/EternalHorror/EternalHorror_WanderingEye1");
            _eyeTexture2 = ModContent.Request<Texture2D>("Consolaria/Content/NPCs/Bosses/EternalHorror/EternalHorror_WanderingEye2");
        }
    }

    public override void Unload() {
        _blinkingVertexes = null;
    }

    public override void PostUpdateNPCs() {
        HandleBlinking();
        HandleEyes();

        if (!EternalHorrorSummonStarted) {
            return;
        }

        _eyeSpawnCD++;

        int spawnTime = 30;

        if (_eyeSpawnCycle > 4) {
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
            int eyeCount = 10 * countFactor;
            float distanceFromPlayer = 100f * countFactor;
            for (int i = 0; i < eyeCount; i++) {
                SpawnEye(playerCenter + Vector2.UnitY.RotatedBy(i / (float)eyeCount * MathHelper.TwoPi) * distanceFromPlayer);
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

    private void On_Main_DrawInterface(On_Main.orig_DrawInterface orig, Main self, Microsoft.Xna.Framework.GameTime gameTime) {
        DrawEyes();
        DrawBlinking();

        orig(self, gameTime);
    }

    private static void SpawnEye(Vector2 position) {
        if (!EternalHorrorSummonStarted) {
            return;
        }

        Player player = Main.LocalPlayer;
        Vector2 playerCenter = player.Center;

        position -= playerCenter;

        Vector2 velocity = -Vector2.UnitY * 5f;

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

    private static void DrawEyes() {
        if (!EternalHorrorSummonStarted) {
            return;
        }

        Player player = Main.LocalPlayer;
        Vector2 playerCenter = player.Center;

        SpriteBatch batch = Main.spriteBatch;

        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

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
            Color color = Color.White;
            float rotation = eyeInfo.EyeRotation;
            Helper.DrawInfo drawInfo = new() {
                Clip = clip,
                Origin = origin,
                Color = color,
                Rotation = rotation
            };

            batch.Draw(eyeTexture1, position, drawInfo);

            Vector2 eyePupilPosition = position + eyeInfo.PupilVelocity;
            batch.Draw(eyeTexture2, eyePupilPosition, drawInfo.WithRotation(0f));
        }

        batch.End();
    }

    private static void HandleEyes() {
        if (!EternalHorrorSummonStarted) {
            return;
        }

        Player player = Main.LocalPlayer;

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

            if (eyeInfo.ShouldLookAtPlayer) {
                float maxRotation = 0.25f * 0.75f;
                eyeInfo.EyeRotation = Helper.Wave(-maxRotation, maxRotation, 3.75f, i);

                eyeInfo.PupilVelocity = Vector2.Lerp(eyeInfo.PupilVelocity, position.DirectionTo(player.Center) * 5f, 0.25f) + Main.rand.NextVector2Circular(1f, 1f);
            }

            eyeInfo.TargetPosition = Vector2.Lerp(eyeInfo.TargetPosition, playerCenter, 0.75f);
        }
    }

    private static void HandleBlinking() {
        if (_shouldBlink) {
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
                float to = 0f - progressOffset / 2f;
                _progress = Helper.Approach(_progress, to, lerpValue);
                if (_progress <= to) {
                    _in = true;
                }
            }
        }
    }

    private static void DrawBlinking() {
        if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.NumPad2)) {
            Blink();
        }

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
    }
}
