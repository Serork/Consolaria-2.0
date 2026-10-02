using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Utilities;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed class EternalHorrorSummonHandler : ModSystem {
    private static ushort TIMEBEFOREBOSSSPAWN => Helper.SecondsToFrames(2);
    private static byte EYESPAWNCYCLECOUNT => 4;
    private static float EYESCALEMAX => 5f;

    private record struct EyeInfo(int TimeLeft,
                                  int MaxTimeLeft,
                                  int MaxTimeLeftForSelfCollapse,
                                  Vector2 PositionOffset,
                                  Vector2 TargetPosition,
                                  Vector2 Velocity = default,
                                  float EyeRotation = 0f,
                                  Vector2 PupilVelocity = default,
                                  bool ShouldLookAtPlayer = false,
                                  float RunAwayProgress = 0f,
                                  float Scale = 0f,
                                  bool SpawnedSoul = false
                                  //,
                                  //bool SoulSpawnNatureally = false
                                  ) {
        public readonly bool Active => TimeLeft > 0;
        public readonly Vector2 VisualPosition => TargetPosition + PositionOffset;
    }

    private record struct SoulInfo(int TimeLeft,
                                   int MaxTimeLeft,
                                   Vector2 PositionOffset,
                                   Vector2 TargetPosition,
                                   Vector2 Velocity = default,
                                   Vector2 TargetVelocity = default,
                                   Vector2 ForcedBossPosition = default) {
        public readonly bool Active => TimeLeft > 0;
        public readonly Vector2 VisualPosition => TargetPosition + PositionOffset;
    }

    private static Asset<Texture2D> _eyeTexture1 = null,
                                    _eyeTexture2 = null;

    private static VertexPositionColor[] _blinkingVertexes = null;
    private static EyeInfo[] _eyeData = null;
    private static SoulInfo[] _soulData = null;

    private static SlotId? _spawnSoundSlotID = null;
    private static bool _soulWhooshPlayed;

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

        _spawnSoundSlotID = SoundEngine.PlaySound(new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/OcramRoarSummon"), Main.LocalPlayer.Center);

        EternalHorrorSummonStarted = true;
        _eyeData = new EyeInfo[400];
        _soulData = new SoulInfo[400];

        Helper.NewMessage(Language.GetTextValue("Mods.Consolaria.EternalHorrorSpawnMessage"), new Color(50, 255, 130));
    }

    public override void Load() {
        _blinkingVertexes = new VertexPositionColor[192];

        On_ScreenObstruction.Draw += On_ScreenObstruction_Draw;

        if (!Main.dedServ) {
            _eyeTexture1 = ModContent.Request<Texture2D>("Consolaria/Content/NPCs/Bosses/EternalHorror/EternalServant");
            _eyeTexture2 = ModContent.Request<Texture2D>("Consolaria/Content/NPCs/Bosses/EternalHorror/EternalServant_Eye");
        }

        On_Main.DrawNPCs += On_Main_DrawNPCs;
    }

    private void On_Main_DrawNPCs(On_Main.orig_DrawNPCs orig, Main self, bool behindTiles) {
        orig(self, behindTiles);
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

        _soulWhooshPlayed = false;
    }

    public override void PostUpdateNPCs() {
        if (_spawnSoundSlotID is not null && SoundEngine.TryGetActiveSound(_spawnSoundSlotID.Value, out ActiveSound result)) {
            result.Position = Main.LocalPlayer.Center;
        } 

        HandleBlinking();
        HandleEyes();
        HandleSouls();

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

            if (bossSpawnProgress >= 0f && !_soulWhooshPlayed) {
                SoundEngine.PlaySound(new SoundStyle($"{nameof(Consolaria)}/Assets/Sounds/SoulWhoosh") with { Pitch = 0.25f, Volume = 0.5f }, Main.LocalPlayer.Center);

                _soulWhooshPlayed = true;
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
        DrawSouls(spriteBatch);
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

        int timeLeft2 = 0;

        _eyeData[index] = new EyeInfo(TimeLeft: timeLeft,
                                      MaxTimeLeft: timeLeft,
                                      MaxTimeLeftForSelfCollapse: timeLeft2,
                                      PositionOffset: position,
                                      TargetPosition: playerCenter,
                                      Velocity: velocity);
    }

    private static void SpawnSoul(EyeInfo eyeInfo, Vector2 forcedBossPosition = default) {
        if (!EternalHorrorSummonStarted) {
            return;
        }

        Player player = Main.LocalPlayer;
        Vector2 playerCenter = player.Center;

        Vector2 position = eyeInfo.VisualPosition;

        Vector2 velocity = -Vector2.UnitY * 50f;
        velocity.X *= Main.rand.NextFloat(0.5f, 1f);

        position -= playerCenter;

        int index = 0;
        for (int i = 0; i < _soulData.Length; i++) {
            if (!_soulData[index].Active) {
                break;
            }
            index++;
        }
        int timeLeft = 27 + Main.rand.Next(10);
        _soulData[index] = new SoulInfo(TimeLeft: timeLeft,
                                        MaxTimeLeft: timeLeft,
                                        PositionOffset: position,
                                        TargetPosition: playerCenter,
                                        Velocity: Vector2.Zero,
                                        TargetVelocity: velocity,
                                        ForcedBossPosition: forcedBossPosition);
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

        ulong seed = 0u;

        float getRandomValue() => Utils.RandomFloat(ref seed);
        float getRemappedRandomValue(float min, float max) => Utils.Remap(getRandomValue(), 0f, 1f, min, max, true);

        for (int i = 0; i < _eyeData.Length; i++) {
            EyeInfo eyeInfo = _eyeData[i];
            if (!eyeInfo.Active) {
                continue;
            }

            Vector2 position = eyeInfo.VisualPosition;

            float waveOffset = (Main.GlobalTimeWrappedHourly + i);

            int maxFrames = 4;
            int frameY = (int)(waveOffset * 7.5f % maxFrames);
            Rectangle clip = eyeTexture1.Frame(1, maxFrames, frameY: frameY);
            Vector2 origin = clip.Centered();

            Color drawColor = Lighting.GetColor(position.ToTileCoordinates());
            float brightness = drawColor.ToVector3().Length() / 3f;
            brightness = Ease.CubeOut(brightness);
            drawColor = Color.Lerp(drawColor, Color.White, 0.375f);

            //float scaleFactor = 1f - Utils.GetLerpValue(1f, EYESCALEMAX, eyeInfo.Scale, true);
            //scaleFactor = Ease.CubeIn(scaleFactor);

            //Main.NewText(scaleFactor);

            float scaleFactor = 1f - eyeInfo.RunAwayProgress;
            Vector2 baseScale = Vector2.One * new Vector2(1f, scaleFactor);

            baseScale *= MathHelper.Lerp(0.875f, 1f, 0.25f);

            Vector2 eyeScale = eyeInfo.Scale * baseScale;

            drawColor = Color.Lerp(drawColor, drawColor.MultiplyRGBA(EternalHorror.MainPurpleColor) * scaleFactor, 1f - scaleFactor);

            Color color = drawColor;

            float alphaModifier = Helper.Wave(0.75f, 1f, 15f, i);
            color = Color.Lerp(color.ModifyRGB(alphaModifier), color.MultiplyAlpha(alphaModifier), brightness);

            float rotation = eyeInfo.EyeRotation + (eyeInfo.RunAwayProgress <= 0f).ToInt() * getRemappedRandomValue(-1f, 1f) * MathHelper.TwoPi * 0.125f * 0.125f;
            Helper.DrawInfo drawInfo = new() {
                Clip = clip,
                Origin = origin,
                Color = color * 0.75f,
                Rotation = rotation,
                Scale = eyeScale
            };

            void drawSelf(Vector2 positionOffset = default, float colorFactor = 1f) {
                float fadeProgress = 1f - eyeInfo.RunAwayProgress;

                float rotationAmount = Utils.Remap(fadeProgress, 1f, 0f, 1f, 7.5f, true) - 1f;
                float pupilRotation = eyeInfo.PupilVelocity.ToRotation();
                float cos = MathF.Cos(pupilRotation);
                float sin = MathF.Sin(pupilRotation);
                baseScale.X *= 1f + rotationAmount * Math.Abs(cos);
                baseScale.Y *= 1f + rotationAmount * Math.Abs(sin);

                Vector2 eyePupilScale = Ease.CubeIn(eyeInfo.Scale) * baseScale;

                color = Color.Lerp(color, EternalHorror.MainPurpleColor with { A = 0 }
                , Ease.QuintOut(eyeInfo.RunAwayProgress));

                color *= MathF.Pow(fadeProgress, 15f);

                Vector2 position2 = position + positionOffset;
                batch.Draw(eyeTexture1, position2, drawInfo.WithColorOverride(color * 0.75f)
                    .WithScaleOverride(baseScale));

                float minScale = 1f,
                      maxScale = 1f;

                float eyeScaleFactor = 0.125f;
                float eyeScaleWaveSpeedFactor = 2.5f;

                if (eyeInfo.ShouldLookAtPlayer) {
                    eyeScaleFactor = MathHelper.Lerp(0.25f, 0.375f, 0f);
                    eyeScaleWaveSpeedFactor = 12.5f;

                    //maxScale *= 0.875f;
                }

                //color *= colorFactor;

                Vector2 eyePupilPosition = position2 + eyeInfo.PupilVelocity;
                int pupilMaxFrames = 2;
                int pupilFrameY = (int)(waveOffset * 7.5f % pupilMaxFrames);
                Rectangle pupilClip = eyeTexture2.Frame(1, pupilMaxFrames/*, frameY: pupilFrameY*/);
                Vector2 pupilOrigin = pupilClip.Centered();
                batch.Draw(eyeTexture2, eyePupilPosition, (drawInfo with { Clip = pupilClip, Origin = pupilOrigin })
                    .WithRotation(0f)
                    .WithColorOverride(Color.Lerp(color,
                                       Color.Lerp(color,
                                       Main.hslToRgb(getRandomValue(), 1f, 0.5f),
                                       getRemappedRandomValue(0f, 0.125f)),
                                       (eyeInfo.RunAwayProgress <= 0f).ToInt()))
                    .WithScaleOverride(eyePupilScale * Helper.Wave(minScale - eyeScaleFactor, maxScale + eyeScaleFactor / 2f, eyeScaleWaveSpeedFactor, i)));
            }

            if (!eyeInfo.SpawnedSoul) {
                if (eyeInfo.ShouldLookAtPlayer) {
                    ShaderLoader.DistortShader.SetDefault(eyeTexture1.Width * 2, eyeTexture1.Height * 2);
                    ShaderLoader.DistortShader.Strength = MathF.Max((_shouldBlink || EternalHorrorSummonEnded).ToInt(), _eyeSpawnCycle / (float)EYESPAWNCYCLECOUNT);
                    ShaderLoader.ApplyEffect(ShaderLoader.DistortShader.Effect, spriteBatch, () => {
                        drawSelf();
                    });
                }
                else {
                    drawSelf();
                }
            }
            else {
                float WaveOffset = 0f;
                float _shadowTime = 0f;
                float scale = 1f;
                float timeLeftProgress = 1f;

                EternalHorror.DrawContext drawContext = new(spriteBatch, position, eyeTexture2, clip, drawColor, 0f, default, Main.screenPosition);
                EternalHorror.DrawUnderShadowEffect(drawContext, (newPosition, newColor) => {
                    drawSelf();
                }, sinWaveOffset: WaveOffset,
                   progress: timeLeftProgress * 0.25f,
                   opacity: 0.375f,
                   sinStep: _shadowTime,
                   offsetAmount: 32f / MathHelper.Lerp(1f, 4f, scale));
            }

            //else {
            //    ShaderLoader.BlurShader.SetDefault(eyeTexture1.Width * 2, eyeTexture1.Height * 2);
            //    ShaderLoader.BlurShader.Pixel = Vector2.One * 2;
            //    ShaderLoader.BlurShader.Fade = fadeProgress;
            //    ShaderLoader.ApplyEffect(ShaderLoader.BlurShader.Effect, spriteBatch, () => {
            //        drawSelf();
            //    });
            //}
        }

        //batch.End();
        //batch.Begin(snapshot);
    }

    private static void DrawSouls(SpriteBatch spriteBatch) {
        if (!EternalHorrorSummonStarted) {
            return;
        }

        Texture2D texture = EternalHorror.RayTexture.Value;

        ulong seed = 0u;

        float getRandomValue() => Utils.RandomFloat(ref seed);
        float getRemappedRandomValue(float min, float max) => Utils.Remap(getRandomValue(), 0f, 1f, min, max, true);

        for (int i = 0; i < _soulData.Length; i++) {
            SoulInfo soulInfo = _soulData[i];
            if (!soulInfo.Active) {
                continue;
            }

            Vector2 position = soulInfo.VisualPosition;

            float progress = 1f - soulInfo.TimeLeft / (float)soulInfo.MaxTimeLeft,
                  progress2 = progress;
            float step = 0.125f;
            float progressStep1 = Utils.GetLerpValue(0f, step / 2f, progress2, true);
            float progressStep2 = 1f - Utils.GetLerpValue(1f - step * 4f, 1f, progress2, true);
            progress = progressStep1;
            progress *= progressStep2;

            Rectangle clip = texture.Frame(2, 1, frameX: 1);
            Vector2 origin = clip.Centered();

            Color color = Color.White;
            color = color.MultiplyRGBA(EternalHorror.MainPurpleColor);

            //color = Color.Lerp(color,
            //                   Main.hslToRgb(getRandomValue(), 1f, 0.5f),
            //                   getRemappedRandomValue(0f, 0.125f));

            color *= progress;

            color *= 1f;

            float rotation = soulInfo.Velocity.ToRotation() + MathHelper.PiOver2;
            Vector2 scale = Vector2.One;

            //scale *= progress;

            scale.X *= 1.5f;

            scale.Y *= progressStep1;
            scale.Y *= Utils.Remap(progressStep2, 0f, 1f, 0f, 5f, true);

            Helper.DrawInfo drawInfo = new() {
                Clip = clip,
                Origin = origin,
                Color = color,
                Rotation = rotation,
                Scale = scale
            };

            ShaderLoader.DistortShader.SetDefault(texture.Width * 2, texture.Height * 2);
            ShaderLoader.ApplyEffect(ShaderLoader.DistortShader.Effect, spriteBatch, () => {
                int drawCount = 3;
                for (int k = 0; k < drawCount; k++) {
                    float progress = k / (float)drawCount;
                    progress -= 0.5f;

                    spriteBatch.Draw(texture, position, drawInfo
                            .WithRotation(progress * 0.125f * 0.5f)
                            .WithColorOverride(
                                Color.Lerp(color,
                                Main.hslToRgb(getRandomValue(), 1f, 0.5f),
                                getRemappedRandomValue(0f, 0.125f))
                                .MultiplyAlpha(0f) * 0.125f * 0.5f));
                }
            });
        }
    }

    private static void HandleEyes() {
        if (!EternalHorrorSummonStarted) {
            return;
        }

        Player player = Main.LocalPlayer;

        ulong seed = 0u;

        for (int i = 0; i < _eyeData.Length; i++) {
            ref EyeInfo eyeInfo = ref _eyeData[i];
            if (!eyeInfo.Active) {
                continue;
            }

            bool bossSpawned = EternalHorrorShouldBeSummoned;

            Vector2 playerCenter = player.Center;

            float offsetY = 10f;
            playerCenter.Y += Helper.Wave(-offsetY, offsetY, 1f, i);
            playerCenter.Y += Helper.Wave(-offsetY, offsetY, 1f, MathHelper.PiOver2 + i) / 2f;

            eyeInfo.TimeLeft--;

            eyeInfo.PositionOffset += eyeInfo.Velocity;

            eyeInfo.Velocity *= 0.95f;

            Vector2 position = eyeInfo.VisualPosition;

            Vector2 angleToPlayer = position.DirectionTo(playerCenter);
            float eyeRotation = angleToPlayer.ToRotation() * 0.125f;

            Vector2 bossCenter = playerCenter;
            float bossRotation = 0f;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (npc.type == EternalHorror.SelfType) {
                    bossRotation = npc.rotation;
                    bossCenter = npc.Center;
                    break;
                }
            }
            bossCenter -= Vector2.UnitY.RotatedBy(bossRotation) * 600f;

            float t = _bossSpawnCounter / (float)TIMEBEFOREBOSSSPAWN;
            t = Helper.Clamp01(t);
            t = MathF.Pow(t, 3f);
            float bossSpawnProgress = Utils.Remap(t, 0f, 1f, 1f, 2f, true);

            bool bossSpawned2 = eyeInfo.TimeLeft <= eyeInfo.MaxTimeLeftForSelfCollapse * bossSpawnProgress;
            if (bossSpawned2 && !bossSpawned) {
                bossCenter.Y += EternalHorror.SPAWNOFFSETY;

                if (eyeInfo.TimeLeft == eyeInfo.MaxTimeLeftForSelfCollapse) {
                    SoundEngine.PlaySound(SoundID.NPCDeath6 with { MaxInstances = 100, Pitch = 0.5f + Main.rand.NextFloat(0.5f, 1f), Volume = 0.125f * 0.25f }, position);
                }

                bossSpawned = true;
                //playerCenter = bossCenter;
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

                eyeInfo.Velocity += position.DirectionTo(bossCenter) * eyeInfo.RunAwayProgress;

                eyeInfo.RunAwayProgress = Helper.Approach(eyeInfo.RunAwayProgress, 1f, 0.025f * Utils.Remap(Utils.RandomFloat(ref seed), 0f, 1f, 0.5f, 1f, true));
                if (eyeInfo.RunAwayProgress >= 0.125f && !eyeInfo.SpawnedSoul) {
                    eyeInfo.SpawnedSoul = true;

                    if (Main.rand.NextBool()) {
                        SpawnSoul(eyeInfo, bossSpawned2 ? bossCenter : default);
                    }
                }

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

    private static void HandleSouls() {
        if (!EternalHorrorSummonStarted) {
            return;
        }

        Player player = Main.LocalPlayer;

        bool bossSpawned = EternalHorrorShouldBeSummoned;

        ulong seed = 0u;

        for (int i = 0; i < _soulData.Length; i++) {
            ref SoulInfo soulInfo = ref _soulData[i];
            if (!soulInfo.Active) {
                continue;
            }

            Vector2 playerCenter = player.Center;

            Vector2 position = soulInfo.VisualPosition;

            soulInfo.TimeLeft--;

            soulInfo.PositionOffset += soulInfo.Velocity;

            soulInfo.Velocity = Vector2.Lerp(soulInfo.Velocity, soulInfo.TargetVelocity, 0.125f * 0.5f);

            Vector2 bossCenter = playerCenter;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (npc.type == EternalHorror.SelfType) {
                    bossCenter = npc.Center;
                    break;
                }
            }

            if (soulInfo.ForcedBossPosition != default) {
                bossCenter = soulInfo.ForcedBossPosition;
            }

            Vector2 velocity = position.DirectionTo(bossCenter) * 50f;

            soulInfo.TargetVelocity = Vector2.Lerp(soulInfo.TargetVelocity, velocity, 0.25f);

            soulInfo.TargetPosition = Vector2.Lerp(soulInfo.TargetPosition, playerCenter, 0.875f);
        }
    }

    private static void HandleBlinking() {
        if (_shouldBlink) {
            EternalHorror.ShakeStrength = Helper.Approach(EternalHorror.ShakeStrength, 1f, 0.125f);

            if (!EternalHorrorSummonEnded) {
                for (int i = 0; i < _eyeData.Length; i++) {
                    ref EyeInfo eyeInfo = ref _eyeData[i];
                    if (!eyeInfo.Active) {
                        continue;
                    }

                    eyeInfo.TimeLeft = eyeInfo.MaxTimeLeft;
                    eyeInfo.MaxTimeLeftForSelfCollapse = (int)(eyeInfo.MaxTimeLeft * Main.rand.NextFloat(0f, MathHelper.Lerp(0.75f, 0.875f, 0.5f)));
                }
            }

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

        float brightness = Lighting.GetColor(Main.LocalPlayer.Center.ToTileCoordinates()).ToVector3().Length() / 3f;

        _blinkingVertexes = new VertexPositionColor[192];
        for (int i2 = 0; i2 < _blinkingVertexes.Length; i2++) {
            _blinkingVertexes[i2].Color = EternalHorror.MainPurpleColor_Dynamic.ModifyRGB(MathHelper.Lerp(MathHelper.Lerp(0.125f, 0.25f, 0.25f), 1f, brightness * 0f)) * 0.95f;
        }
        DisplayMode desktop = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        int screenWidth = desktop.Width;
        int screenHeight = desktop.Height;
        int num = screenWidth;
        int num2 = screenHeight;
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
