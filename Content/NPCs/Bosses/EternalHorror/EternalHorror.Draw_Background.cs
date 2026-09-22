using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed partial class EternalHorror : ModNPC {
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "DrawSunAndMoon")]
    public extern static void Main_DrawSunAndMoon(Main self, Main.SceneArea sceneArea, Color moonColor, Color sunColor, float tempMushroomInfluence);

    private struct Bolt {
        public Vector2 Position;

        public float Depth;

        public int Life;

        public bool IsAlive;

        public Vector2 Scale;
    }

    public static float ScreenObstruction { get; private set; }
    public static Color FrontColor { get; private set; }

    private record struct SunAndMoonDrawSettings(Main.SceneArea SceneArea, Color MoonColor, Color SunColor, float TempMushroomInfluence);
    private static SunAndMoonDrawSettings _sunAndMoonDrawSettings;

    private static float _purpleColorTime, _purpleColorTime2;
    private static float _purpleColorStrength;

    private static bool _active;

    private static Bolt[] _bolts = null;
    private static int _ticksUntilNextBolt;

    private static Asset<Texture2D> _boltTexture = null,
                                    _flashTexture = null;

    private partial void Load_BackgroundHooks() {
        On_ScreenDarkness.Update += On_ScreenDarkness_Update;

        On_ScreenDarkness.DrawBack += On_ScreenDarkness_DrawBack;
        On_ScreenDarkness.DrawFront += On_ScreenDarkness_DrawFront;

        On_Main.DrawSunAndMoon += On_Main_DrawSunAndMoon;

        if (!Main.dedServ) {
            _boltTexture = Main.Assets.Request<Texture2D>("Images/Misc/VortexSky/Bolt", (AssetRequestMode)1);
            _flashTexture = ModContent.Request<Texture2D>("Consolaria/Content/NPCs/Bosses/EternalHorror/Flash");
        }
    }

    private void On_Main_DrawSunAndMoon(On_Main.orig_DrawSunAndMoon orig, Main self, Main.SceneArea sceneArea, Color moonColor, Color sunColor, float tempMushroomInfluence) {
        _sunAndMoonDrawSettings = new(sceneArea, moonColor, sunColor, tempMushroomInfluence);

        orig(self, sceneArea, moonColor, sunColor, tempMushroomInfluence);
    }

    private void On_ScreenDarkness_Update(On_ScreenDarkness.orig_Update orig) {
        orig();

        ApplyScreenDarkness();

        if (!_active) {
            return;
        }

        var _random = Main.rand;

        if (_ticksUntilNextBolt <= 0) {
            _ticksUntilNextBolt = _random.Next(1, 5) * 5;
            int i;
            for (i = 0; _bolts[i].IsAlive && i != _bolts.Length - 1; i++) {
            }
            _bolts[i].IsAlive = true;
            _bolts[i].Position.X = (Main.rand.NextBool() ? _random.NextFloat(0f, 0.25f) : _random.NextFloat(0.75f, 1f)) * Main.screenWidth;
            _bolts[i].Position.Y = (Main.rand.NextBool() ? _random.NextFloat(0f, 0.25f) : _random.NextFloat(0.75f, 1f)) * Main.screenHeight;
            _bolts[i].Depth = _random.NextFloat();
            _bolts[i].Life = 100;
            _bolts[i].Scale = new Vector2(_random.NextFloat(0.5f, 1f), _random.NextFloat(0.5f, 1f));
        }
        _ticksUntilNextBolt--;
        for (int j = 0; j < _bolts.Length; j++) {
            if (_bolts[j].IsAlive) {
                _bolts[j].Life--;
                if (_bolts[j].Life <= 0) {
                    _bolts[j].IsAlive = false;
                }
            }
        }
    }

    private void On_ScreenDarkness_DrawBack(On_ScreenDarkness.orig_DrawBack orig, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch) {
        orig(spriteBatch);

        DrawDarkness_Back(spriteBatch);

        DrawSunAndMoonAgain();
    }

    private void On_ScreenDarkness_DrawFront(On_ScreenDarkness.orig_DrawFront orig, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch) {
        orig(spriteBatch);

        DrawDarkness_Front(spriteBatch);
    }

    private void DrawBolts(SpriteBatch spriteBatch, float minDepth, float maxDepth) {
        float num = Math.Min(1f, (Main.screenPosition.Y - 1000f) / 1000f);
        Vector2 vector3 = Main.screenPosition + new Vector2(Main.screenWidth >> 1, Main.screenHeight >> 1);
        Rectangle rectangle = new Rectangle(-1000, -1000, Main.screenWidth + 1000, Main.screenHeight + 1000);
        for (int i = 0; i < _bolts.Length; i++) {
            if (!_bolts[i].IsAlive || !(_bolts[i].Depth > minDepth) || !(_bolts[i].Depth < maxDepth)) {
                continue;
            }
            float depth = _bolts[i].Depth * 8f;
            Vector2 vector4 = new Vector2(1f / depth, 0.9f / depth);
            Vector2 position = (_bolts[i].Position) * vector4;
            if (rectangle.Contains((int)position.X, (int)position.Y)) {
                Texture2D value = _boltTexture.Value;
                int life = _bolts[i].Life;
                if (life > 26 && life % 2 == 0) {
                    value = _flashTexture.Value;
                }
                float num2 = (float)life / 100;
                num2 *= 1f - Utils.GetLerpValue(0.875f, 1f, num2, true);
                spriteBatch.DrawWithSnapshot(() => {
                    spriteBatch.Draw(value, position, null, MainPurpleColor_Dynamic * num * num2 * 0.5f * ScreenObstruction, 0f, Vector2.Zero, vector4.X * 20f * _bolts[i].Scale, SpriteEffects.None, 0f);
                }, blendState: BlendState.Additive);
            }
        }
    }

    private void ApplyScreenDarkness() {
        Player player = Main.LocalPlayer;

        float value = 0f;
        float amount = 1f / 60f;
        Vector2 mountedCenter = player.MountedCenter;
        for (int i = 0; i < 200; i++) {
            if (Main.npc[i].active && Main.npc[i].type == SelfType && Main.npc[i].Distance(mountedCenter) < 3000f) {
                value = 0.95f;
                FrontColor = new Color(22, 21, 18) * 0.3f;
                //amount = 0.03f;
            }
        }

        amount /= 1f;

        ScreenObstruction = Helper.Approach(ScreenObstruction, value, amount);

        if (!_active && ScreenObstruction > 0) {
            _bolts = new Bolt[500];
            for (int i = 0; i < _bolts.Length; i++) {
                _bolts[i].IsAlive = false;
            }

            _active = true;
        }

        if (ScreenObstruction <= 0f && _active) {
            _active = false;
        }

        if (_purpleColorTime == 0f) {
            _purpleColorTime = -Main.rand.NextFloat(Helper.SecondsToFrames(1f), Helper.SecondsToFrames(2.5f));
            _purpleColorTime2 = _purpleColorTime;
            _purpleColorTime *= 1.5f;
            _purpleColorStrength = Main.rand.NextFloat(0.75f);
        }
        _purpleColorTime = Helper.Approach(_purpleColorTime, 0f, 1f);
    }

    private void DrawDarkness_Back(SpriteBatch spriteBatch) {
        if (ScreenObstruction != 0f) {
            void drawLightnings() {
                float purpleColorFactor = 0f;
                float purpleColorTime_Min = _purpleColorTime2 * 0.125f * 0.75f;
                if (_purpleColorTime > purpleColorTime_Min) {
                    purpleColorFactor = Utils.GetLerpValue(purpleColorTime_Min, 0f, _purpleColorTime, true);
                }
                Color baseColor = Color.Black;
                baseColor = Color.Lerp(baseColor, MainPurpleColor_Dynamic, 0.125f * 0.5f);
                baseColor = Color.Lerp(baseColor, MainPurpleColor, purpleColorFactor * _purpleColorStrength * 0.5f);
                Color color = baseColor * ScreenObstruction;
                color *= 0.5f;
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(-2, -2, Main.screenWidth + 4, Main.screenHeight + 4), new Rectangle(0, 0, 1, 1), color);
            }
            void drawReflections() {
                float screenObstructionFactor = Utils.GetLerpValue(0.5f, 0.95f, ScreenObstruction, true);

                Texture2D background = _backgroundTexture.Value;
                Rectangle clip = background.Bounds;
                Vector2 origin = clip.Centered();
                Color color = Color.White;
                Vector2 scale = Vector2.One * 10f;
                Helper.DrawInfo drawInfo = new() {
                    Clip = clip,
                    Origin = origin,
                    Color = color,
                    Scale = scale
                };
                Vector2 position = Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) / 2f;
                int count = 10;
                for (int i = 0; i < count; i++) {
                    float progress = i / (float)count;
                    float sinStep = Main.GlobalTimeWrappedHourly + i * 2;
                    float waveFactor = sinStep % 1f * 0.1f + progress;
                    color = Color.Lerp(new Color(22, 21, 18), MainPurpleColor_Dynamic, waveFactor);
                    float scale_float = waveFactor;
                    scale_float = MathF.Max(0.125f, scale_float);
                    sinStep = MathHelper.TwoPi * waveFactor * 2f;
                    //sinStep *= (i % 2 == 0).ToDirectionInt(); 
                    spriteBatch.Draw(background, position, drawInfo.WithScale(scale_float).
                                                                    WithColor(color * 0.5f * 1f).
                                                                    WithColorModifier(screenObstructionFactor).
                                                                    WithRotation(sinStep));

                    //DrawBolts(spriteBatch, (i - 1) / (float)count, (i) / (float)count);
                }

                //DrawBolts(spriteBatch, 0f, 1f);
            }

            drawReflections();
            drawLightnings();
            drawLightnings();
        }
    }

    private void DrawSunAndMoonAgain() {
        if (ScreenObstruction != 0f) {
            if ((double)(Main.screenPosition.Y / 16f) < Main.worldSurface + 2.0) {
                float opacity = ScreenObstruction;
                Color lightingColor = Lighting.GetColor(NPC.Center.ToTileCoordinates());
                Color moonColor = Color.Lerp(_sunAndMoonDrawSettings.MoonColor, lightingColor, 0.5f);
                moonColor = Color.Lerp(moonColor, MainPurpleColor_Dynamic, 0.25f);
                moonColor *= opacity;
                Color sunColor = _sunAndMoonDrawSettings.SunColor;
                sunColor = Color.Lerp(sunColor, MainPurpleColor_Dynamic, 0.25f);
                Main_DrawSunAndMoon(Main.instance, _sunAndMoonDrawSettings.SceneArea,
                                                   moonColor,
                                                   sunColor,
                                                   _sunAndMoonDrawSettings.TempMushroomInfluence);
            }
        }
    }

    private void DrawDarkness_Front(SpriteBatch spriteBatch) {
        if (ScreenObstruction != 0f) {
            Color baseColor = FrontColor;
            baseColor = Color.Lerp(baseColor, MainPurpleColor_Dynamic, 0.125f * 0.5f);
            Color color = baseColor * ScreenObstruction;
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(-2, -2, Main.screenWidth + 4, Main.screenHeight + 4), new Rectangle(0, 0, 1, 1), color);
        }
    }
}
