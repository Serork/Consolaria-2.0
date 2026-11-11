using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Animations;
using Terraria.GameContent.Events;
using Terraria.GameContent.Skies.CreditsRoll;
using Terraria.Graphics.Effects;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace Consolaria.Common;

sealed class ConsolariaCreditsSky : CustomSky, ILoadable {
    sealed class WavyText : ITagHandler {
        sealed class WavyTextSnippet : TextSnippet {
            public WavyTextSnippet(string text, Color color, float scale = 1f) {
                Text = text;
                TextOriginal = text;
                Color = color;
                Scale = scale;
            }

            public override bool UniqueDraw(bool justCheckingString, out Vector2 size, SpriteBatch spriteBatch, Vector2 position = default, Color color = default, float scaleBase = 1) {
                Vector2 scale = Vector2.One * scaleBase;

                string text = Text;
                var font = FontAssets.DeathText.Value;
                Vector2 origin = font.MeasureString(text);
                origin *= 0.5f;

                var textSize = font.MeasureString(text) * scale;

                size = textSize;

                float rotation = 0f;

                float sum = 0f;

                float index = 0;

                void drawCharacter(string character) {
                    var newPosition = position - (origin * scale).RotatedBy(rotation);
                    newPosition += new Vector2(sum * scale.X, 0f).RotatedBy(rotation);

                    var characterSize = font.MeasureString(character);
                    newPosition += characterSize * 0.5f;

                    //var curRotation = rotation;
                    //curRotation += MathF.Cos(time * random.NextFloat(1.1f, 1.4f) + timeOffset * random.NextFloat(0.3f, 3f)) * rotation_multiplier * strength;

                    ChatManager.DrawColorCodedString(spriteBatch, font, character, newPosition + Vector2.UnitY * MathF.Sin(Main.GlobalTimeWrappedHourly * 5f + index * 0.5f) * characterSize.Y * 0.125f * 0.375f
                        * GetScale() * GetOpacity(), color, rotation, characterSize * 0.5f, scale);

                    sum += characterSize.X;

                    index++;
                }

                foreach (var c in text) {
                    drawCharacter(c.ToString());
                }

                return true;
            }
        }

        TextSnippet ITagHandler.Parse(string text, Color baseColor, string options) {
            if (string.IsNullOrEmpty(options)) {
                return new TextSnippet(text, baseColor);
            }

            return new WavyTextSnippet(text, baseColor);
        }
    }

    //sealed class ConsolariaCreditsSky_Music : ModSceneEffect {
    //    public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

    //    public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Music/BlightofSpring");

    //    public override bool IsSceneEffectActive(Player player) => ConsolariaCreditsSky.IsEventOngoing;
    //}

    private const int MAX_TIME_FOR_CREDITS_ROLL_IN_FRAMES = 28800;
    private static int _creditsRollRemainingTime;

    internal static int _preparationTime, _allTime;

    private int _endTime;
    private int _currentTime;
    private ConsolariaCreditsRollComposer _composer = new ConsolariaCreditsRollComposer();
    private List<IAnimationSegment> _segmentsInGame = new List<IAnimationSegment>();
    private List<IAnimationSegment> _segmentsInMainMenu = new List<IAnimationSegment>();
    private bool _isActive;
    private static bool _wantsToBeSeen;
    private static float _opacity, _opacity2;

    public int AmountOfTimeNeededForFullPlay => _endTime;

    public ConsolariaCreditsSky() {
        EnsureSegmentsAreMade();
    }

    void ILoadable.Load(Mod mod) {
        On_CreditsRollEvent.Reset += On_CreditsRollEvent_Reset;
        On_CreditsRollEvent.UpdateTime += On_CreditsRollEvent_UpdateTime;
        On_Main.TryPlayingCreditsRoll += On_Main_TryPlayingCreditsRoll;

        ChatManager.Register<WavyText>(["w", "wavy"]);

        //On_CreditsRollComposer.PlaySegment_TextRoll += On_CreditsRollComposer_PlaySegment_TextRoll;
        //On_CreditsRollComposer.PlaySegment_Grox_GuideRunningFromZombie += On_CreditsRollComposer_PlaySegment_Grox_GuideRunningFromZombie;
        //On_CreditsRollComposer.PlaySegment_Grox_MerchantAndTravelingMerchantTryingToSellJunk += On_CreditsRollComposer_PlaySegment_Grox_MerchantAndTravelingMerchantTryingToSellJunk;
        //On_CreditsRollComposer.PlaySegment_Grox_DemolitionistAndArmsDealerArguingThenNurseComes += On_CreditsRollComposer_PlaySegment_Grox_DemolitionistAndArmsDealerArguingThenNurseComes;
        //On_CreditsRollComposer.PlaySegment_TinkererAndMechanic += On_CreditsRollComposer_PlaySegment_TinkererAndMechanic;
        //On_CreditsRollComposer.PlaySegment_Grox_DryadSayingByeToTavernKeep += On_CreditsRollComposer_PlaySegment_Grox_DryadSayingByeToTavernKeep;
        //On_CreditsRollComposer.PlaySegment_Grox_WizardPartyGirlDyeTraderAndPainterPartyWithBunnies += On_CreditsRollComposer_PlaySegment_Grox_WizardPartyGirlDyeTraderAndPainterPartyWithBunnies;
        //On_CreditsRollComposer.PlaySegment_ClothierChasingTruffle += On_CreditsRollComposer_PlaySegment_ClothierChasingTruffle;
        //On_CreditsRollComposer.PlaySegment_Grox_AnglerAndPirateTalkAboutFish += On_CreditsRollComposer_PlaySegment_Grox_AnglerAndPirateTalkAboutFish;
        //On_CreditsRollComposer.PlaySegment_Grox_ZoologistAndPetsAnnoyGolfer += On_CreditsRollComposer_PlaySegment_Grox_ZoologistAndPetsAnnoyGolfer;
        //On_CreditsRollComposer.PlaySegment_Grox_SkeletonMerchantSearchesThroughBones += On_CreditsRollComposer_PlaySegment_Grox_SkeletonMerchantSearchesThroughBones;
        //On_CreditsRollComposer.PlaySegment_DryadTurningToTree += On_CreditsRollComposer_PlaySegment_DryadTurningToTree;
        //On_CreditsRollComposer.PlaySegment_Grox_SteampunkerRepairingCyborg += On_CreditsRollComposer_PlaySegment_Grox_SteampunkerRepairingCyborg;
        //On_CreditsRollComposer.PlaySegment_Grox_SantaAndTaxCollectorThrowingPresents += On_CreditsRollComposer_PlaySegment_Grox_SantaAndTaxCollectorThrowingPresents;
        //On_CreditsRollComposer.PlaySegment_Grox_WitchDoctorGoingToHisPeople += On_CreditsRollComposer_PlaySegment_Grox_WitchDoctorGoingToHisPeople;
        //On_CreditsRollComposer.PlaySegment_PrincessAndEveryoneThanksPlayer += On_CreditsRollComposer_PlaySegment_PrincessAndEveryoneThanksPlayer;

        On_Main.UpdateAudio_DecideOnTOWMusic += On_Main_UpdateAudio_DecideOnTOWMusic;
        On_Main.UpdateAudio_DecideOnNewMusic += On_Main_UpdateAudio_DecideOnNewMusic;
        On_Main.DrawPendingMouseText += On_Main_DrawPendingMouseText;
    }

    private void On_Main_UpdateAudio_DecideOnNewMusic(On_Main.orig_UpdateAudio_DecideOnNewMusic orig, Main self) {
        orig(self);

        if (SkyManager.Instance["ConsolariaCreditsRoll"].IsActive()) {
            Main.newMusic = MusicLoader.GetMusicSlot(Consolaria.Instance, "Assets/Music/BlightofSpring");
        }
    }

    private void On_Main_UpdateAudio_DecideOnTOWMusic(On_Main.orig_UpdateAudio_DecideOnTOWMusic orig, Main self) {
        orig(self);

        if (SkyManager.Instance["ConsolariaCreditsRoll"].IsActive()) {
            Main.newMusic = MusicLoader.GetMusicSlot(Consolaria.Instance, "Assets/Music/BlightofSpring");
        }
    }

    void ILoadable.Unload() { }

    public override void OnLoad() { }

    private SegmentInforReport On_CreditsRollComposer_PlaySegment_PrincessAndEveryoneThanksPlayer(On_CreditsRollComposer.orig_PlaySegment_PrincessAndEveryoneThanksPlayer orig, CreditsRollComposer self, int startTime, Vector2 sceneAnchorPosition) {
        //if (!EternalHorror.CanEternalHorrorExistInAWorld) {
        //    return orig(self, startTime, new Vector2(sceneAnchorPosition.X, sceneAnchorPosition.Y));
        //}

        var result = orig(self, startTime, new Vector2(MathF.Abs(sceneAnchorPosition.X), sceneAnchorPosition.Y));
        return result;
    }

    private SegmentInforReport On_CreditsRollComposer_PlaySegment_Grox_WitchDoctorGoingToHisPeople(On_CreditsRollComposer.orig_PlaySegment_Grox_WitchDoctorGoingToHisPeople orig, CreditsRollComposer self, int startTime, Vector2 sceneAnchorPosition) {
        //if (!EternalHorror.CanEternalHorrorExistInAWorld) {
        //    return orig(self, startTime, new Vector2(sceneAnchorPosition.X, sceneAnchorPosition.Y));
        //}

        var result = orig(self, startTime, new Vector2(MathF.Abs(sceneAnchorPosition.X), sceneAnchorPosition.Y));
        return result;
    }

    private SegmentInforReport On_CreditsRollComposer_PlaySegment_Grox_SantaAndTaxCollectorThrowingPresents(On_CreditsRollComposer.orig_PlaySegment_Grox_SantaAndTaxCollectorThrowingPresents orig, CreditsRollComposer self, int startTime, Vector2 sceneAnchorPosition) {
        //if (!EternalHorror.CanEternalHorrorExistInAWorld) {
        //    return orig(self, startTime, new Vector2(sceneAnchorPosition.X, sceneAnchorPosition.Y));
        //}

        var result = orig(self, startTime, new Vector2(MathF.Abs(sceneAnchorPosition.X), sceneAnchorPosition.Y));
        return result;
    }

    private SegmentInforReport On_CreditsRollComposer_PlaySegment_Grox_SteampunkerRepairingCyborg(On_CreditsRollComposer.orig_PlaySegment_Grox_SteampunkerRepairingCyborg orig, CreditsRollComposer self, int startTime, Vector2 sceneAnchorPosition) {
        //if (!EternalHorror.CanEternalHorrorExistInAWorld) {
        //    return orig(self, startTime, new Vector2(sceneAnchorPosition.X, sceneAnchorPosition.Y));
        //}

        var result = orig(self, startTime, new Vector2(MathF.Abs(sceneAnchorPosition.X), sceneAnchorPosition.Y));
        return result;
    }

    private SegmentInforReport On_CreditsRollComposer_PlaySegment_DryadTurningToTree(On_CreditsRollComposer.orig_PlaySegment_DryadTurningToTree orig, CreditsRollComposer self, int startTime, Vector2 sceneAnchorPosition) {
        //if (!EternalHorror.CanEternalHorrorExistInAWorld) {
        //    return orig(self, startTime, new Vector2(sceneAnchorPosition.X, sceneAnchorPosition.Y));
        //}

        var result = orig(self, startTime, new Vector2(MathF.Abs(sceneAnchorPosition.X), sceneAnchorPosition.Y));
        return result;
    }

    private SegmentInforReport On_CreditsRollComposer_PlaySegment_Grox_SkeletonMerchantSearchesThroughBones(On_CreditsRollComposer.orig_PlaySegment_Grox_SkeletonMerchantSearchesThroughBones orig, CreditsRollComposer self, int startTime, Vector2 sceneAnchorPosition) {
        //if (!EternalHorror.CanEternalHorrorExistInAWorld) {
        //    return orig(self, startTime, new Vector2(sceneAnchorPosition.X, sceneAnchorPosition.Y));
        //}

        var result = orig(self, startTime, new Vector2(MathF.Abs(sceneAnchorPosition.X), sceneAnchorPosition.Y));
        return result;
    }

    private SegmentInforReport On_CreditsRollComposer_PlaySegment_Grox_ZoologistAndPetsAnnoyGolfer(On_CreditsRollComposer.orig_PlaySegment_Grox_ZoologistAndPetsAnnoyGolfer orig, CreditsRollComposer self, int startTime, Vector2 sceneAnchorPosition) {
        //if (!EternalHorror.CanEternalHorrorExistInAWorld) {
        //    return orig(self, startTime, new Vector2(sceneAnchorPosition.X, sceneAnchorPosition.Y));
        //}

        var result = orig(self, startTime, new Vector2(MathF.Abs(sceneAnchorPosition.X), sceneAnchorPosition.Y));
        return result;
    }

    private SegmentInforReport On_CreditsRollComposer_PlaySegment_Grox_AnglerAndPirateTalkAboutFish(On_CreditsRollComposer.orig_PlaySegment_Grox_AnglerAndPirateTalkAboutFish orig, CreditsRollComposer self, int startTime, Vector2 sceneAnchorPosition) {
        //if (!EternalHorror.CanEternalHorrorExistInAWorld) {
        //    return orig(self, startTime, new Vector2(sceneAnchorPosition.X, sceneAnchorPosition.Y));
        //}

        var result = orig(self, startTime, new Vector2(MathF.Abs(sceneAnchorPosition.X), sceneAnchorPosition.Y));
        return result;
    }

    private SegmentInforReport On_CreditsRollComposer_PlaySegment_Grox_WizardPartyGirlDyeTraderAndPainterPartyWithBunnies(On_CreditsRollComposer.orig_PlaySegment_Grox_WizardPartyGirlDyeTraderAndPainterPartyWithBunnies orig, CreditsRollComposer self, int startTime, Vector2 sceneAnchorPosition) {
        //if (!EternalHorror.CanEternalHorrorExistInAWorld) {
        //    return orig(self, startTime, new Vector2(sceneAnchorPosition.X, sceneAnchorPosition.Y));
        //}

        var result = orig(self, startTime, new Vector2(MathF.Abs(sceneAnchorPosition.X), sceneAnchorPosition.Y));
        return result;
    }

    private SegmentInforReport On_CreditsRollComposer_PlaySegment_Grox_DryadSayingByeToTavernKeep(On_CreditsRollComposer.orig_PlaySegment_Grox_DryadSayingByeToTavernKeep orig, CreditsRollComposer self, int startTime, Vector2 sceneAnchorPosition) {
        //if (!EternalHorror.CanEternalHorrorExistInAWorld) {
        //    return orig(self, startTime, new Vector2(sceneAnchorPosition.X, sceneAnchorPosition.Y));
        //}

        var result = orig(self, startTime, new Vector2(MathF.Abs(sceneAnchorPosition.X), sceneAnchorPosition.Y));
        return result;
    }

    private SegmentInforReport On_CreditsRollComposer_PlaySegment_TinkererAndMechanic(On_CreditsRollComposer.orig_PlaySegment_TinkererAndMechanic orig, CreditsRollComposer self, int startTime, Vector2 sceneAnchorPosition) {
        //if (!EternalHorror.CanEternalHorrorExistInAWorld) {
        //    return orig(self, startTime, new Vector2(sceneAnchorPosition.X, sceneAnchorPosition.Y));
        //}

        var result = orig(self, startTime, new Vector2(MathF.Abs(sceneAnchorPosition.X), sceneAnchorPosition.Y));
        return result;
    }

    private SegmentInforReport On_CreditsRollComposer_PlaySegment_Grox_DemolitionistAndArmsDealerArguingThenNurseComes(On_CreditsRollComposer.orig_PlaySegment_Grox_DemolitionistAndArmsDealerArguingThenNurseComes orig, CreditsRollComposer self, int startTime, Vector2 sceneAnchorPosition) {
        //if (!EternalHorror.CanEternalHorrorExistInAWorld) {
        //    return orig(self, startTime, new Vector2(sceneAnchorPosition.X, sceneAnchorPosition.Y));
        //}

        var result = orig(self, startTime, new Vector2(MathF.Abs(sceneAnchorPosition.X), sceneAnchorPosition.Y));
        return result;
    }

    private SegmentInforReport On_CreditsRollComposer_PlaySegment_Grox_MerchantAndTravelingMerchantTryingToSellJunk(On_CreditsRollComposer.orig_PlaySegment_Grox_MerchantAndTravelingMerchantTryingToSellJunk orig, CreditsRollComposer self, int startTime, Vector2 sceneAnchorPosition) {
        //if (!EternalHorror.CanEternalHorrorExistInAWorld) {
        //    return orig(self, startTime, new Vector2(sceneAnchorPosition.X, sceneAnchorPosition.Y));
        //}

        var result = orig(self, startTime, new Vector2(MathF.Abs(sceneAnchorPosition.X), sceneAnchorPosition.Y));
        return result;
    }

    private SegmentInforReport On_CreditsRollComposer_PlaySegment_Grox_GuideRunningFromZombie(On_CreditsRollComposer.orig_PlaySegment_Grox_GuideRunningFromZombie orig, CreditsRollComposer self, int startTime, Vector2 sceneAnchorPosition) {
        //if (!EternalHorror.CanEternalHorrorExistInAWorld) {
        //    return orig(self, startTime, new Vector2(sceneAnchorPosition.X, sceneAnchorPosition.Y));
        //}

        var result = orig(self, startTime, new Vector2(MathF.Abs(sceneAnchorPosition.X), sceneAnchorPosition.Y));
        return result;
    }

    private SegmentInforReport On_CreditsRollComposer_PlaySegment_TextRoll(On_CreditsRollComposer.orig_PlaySegment_TextRoll orig, CreditsRollComposer self, int startTime, string sourceCategory, Vector2 anchorOffset) {
        //if (!EternalHorror.CanEternalHorrorExistInAWorld) {
        //    return orig(self, startTime, sourceCategory, new Vector2(anchorOffset.X, anchorOffset.Y));
        //}

        var result = orig(self, startTime, sourceCategory, new Vector2(MathF.Abs(anchorOffset.X), anchorOffset.Y));
        return result;
    }

    private SegmentInforReport On_CreditsRollComposer_PlaySegment_ClothierChasingTruffle(On_CreditsRollComposer.orig_PlaySegment_ClothierChasingTruffle orig, CreditsRollComposer self, int startTime, Vector2 sceneAnchorPosition) {
        //if (!EternalHorror.CanEternalHorrorExistInAWorld) {
        //    return orig(self, startTime, new Vector2(sceneAnchorPosition.X, sceneAnchorPosition.Y));
        //}

        var result = orig(self, startTime, new Vector2(MathF.Abs(sceneAnchorPosition.X), sceneAnchorPosition.Y));
        return result;
    }

    private void On_Main_TryPlayingCreditsRoll(On_Main.orig_TryPlayingCreditsRoll orig) {
        orig();

        if (!Main.gameMenu && CanPlayCreditsRoll() && !SkyManager.Instance["ConsolariaCreditsRoll"].IsActive())
            SkyManager.Instance.Activate("ConsolariaCreditsRoll", Vector2.Zero);
    }

    public static bool CanPlayCreditsRoll() {
        //if (!Main.gameMenu && LocalPlayer.hasCreditsSceneMusicBox)
        //    return true;

        if (Main.gameMenu && Main.menuMode == 3000)
            return true;

        return ConsolariaCreditsSky.IsEventOngoing/* && EternalHorror.CanEternalHorrorExistInAWorld*/;
    }

    private void On_CreditsRollEvent_UpdateTime(On_CreditsRollEvent.orig_UpdateTime orig) {
        orig();
        UpdateTime();

        if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F1)) {
            //ConsolariaCreditsSky.Reset_Internal();
            CreditsRollEvent.Reset();
        }
        if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F2)) {
            //CreditsRollEvent.Reset();
            EnsureSegmentsAreMade();
            ConsolariaCreditsSky.TryStartingCreditsRoll();
            //CreditsRollEvent.TryStartingCreditsRoll();
        }
        if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F3)) {
            ConsolariaCreditsSky.Reset_Internal();
            CreditsRollEvent.Reset();
        }
    }

    private void On_CreditsRollEvent_Reset(On_CreditsRollEvent.orig_Reset orig) {
        orig();
        Reset_Internal();
    }

    public static bool IsEventOngoing => _creditsRollRemainingTime > 0;

    public static void TryStartingCreditsRoll() {
        //if (!EternalHorror.CanEternalHorrorExistInAWorld) {
        //    return;
        //}

        CreditsRollEvent.Reset();

        _creditsRollRemainingTime = 28800;
        if (SkyManager.Instance["ConsolariaCreditsRoll"] is ConsolariaCreditsSky creditsRollSky)
            _creditsRollRemainingTime = creditsRollSky.AmountOfTimeNeededForFullPlay;

        //if (Main.netMode == 2)
        //    NetMessage.SendData(140, -1, -1, null, 0, _creditsRollRemainingTime);
    }

    public static void SendCreditsRollRemainingTimeToPlayer(int playerIndex) {
        //if (_creditsRollRemainingTime != 0 && Main.netMode == 2)
        //    NetMessage.SendData(140, playerIndex, -1, null, 0, _creditsRollRemainingTime);
    }

    public static void UpdateTime() {
        _creditsRollRemainingTime = Utils.Clamp(_creditsRollRemainingTime - 1, 0, 28800);
    }

    public static void Reset_Internal() {
        _creditsRollRemainingTime = 0;
    }

    public static void SetRemainingTimeDirect(int time) {
        _creditsRollRemainingTime = Utils.Clamp(time, 0, 28800);
    }

    private void EnsureSegmentsAreMade() {
        //if (_segmentsInMainMenu.Count <= 0 || _segmentsInGame.Count <= 0)
        {
            _segmentsInGame.Clear();
            _composer.FillSegments(_segmentsInGame, out _endTime, inGame: true);
            _segmentsInMainMenu.Clear();
            _composer.FillSegments(_segmentsInMainMenu, out _endTime, inGame: false);
        }
    }

    public override void Activate(Vector2 position, params object[] args) {
        _isActive = true;
        _wantsToBeSeen = true;
        if (_opacity == 0f) {
            EnsureSegmentsAreMade();
            _currentTime = 0;
        }
    }

    public override void Deactivate(params object[] args) {
        _wantsToBeSeen = false;
    }

    public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth) {
        //if (!EternalHorror.CanEternalHorrorExistInAWorld) {
        //    return;
        //}

        float num = 1f;
        if (!(num < minDepth) && !(num > maxDepth)) {
            Vector2 anchorPositionOnScreen = Main.ScreenSize.ToVector2() / 2f;
            if (Main.gameMenu)
                anchorPositionOnScreen.Y = 300f;

            GameAnimationSegment gameAnimationSegment = default(GameAnimationSegment);
            gameAnimationSegment.SpriteBatch = spriteBatch;
            gameAnimationSegment.AnchorPositionOnScreen = anchorPositionOnScreen;
            gameAnimationSegment.TimeInAnimation = _currentTime;
            gameAnimationSegment.DisplayOpacity = _opacity;
            GameAnimationSegment info = gameAnimationSegment;
            List<IAnimationSegment> list = _segmentsInGame;
            if (Main.gameMenu)
                list = _segmentsInMainMenu;

            for (int i = 0; i < list.Count; i++) {
                list[i].Draw(ref info);
            }
        }

        if (Main.gameMenu) {
            return;
        }

        float num5 = Main.GlobalTimeWrappedHourly * 0.02f /** 10f*/ % 1f;
        if (num5 < 0f)
            num5 += 1f;

        Color color = Main.hslToRgb(num5, 1f, 0.5f);

        Vector2 position = Main.ScreenSize.ToVector2() / 2f;
        position.Y = 200;

        float lastPosY = Main.ScreenSize.Y / 2f - 100;

        string text = Language.GetTextValue("Mods.Consolaria.Ty");

        Vector2 origin = FontAssets.DeathText.Value.MeasureString(text);
        origin *= 0.5f;

        origin = Vector2.Zero;

        //double easeOutExpo(double t) => t >= 1.0 ? 1.0 : 1.0 - Math.Pow(2, -10 * t);

        float lerp = Utils.GetLerpValue(_allTime - 120, _allTime, _currentTime, true);
        lerp = Ease.QuadOut(Ease.SineIn(lerp));
        //if (_currentTime > _preparationTime) {
        //    lerp = 1f - Utils.GetLerpValue(0, 120 * 0.375f, _currentTime - _preparationTime, true);
        //    lerp = Utils.Remap(lerp, 0f, 1f, 0f, 0.375f, true);
        //    lerp = Ease.CubeInOut(lerp);
        //}

        position.Y = MathHelper.Lerp(position.Y, lastPosY - 50, lerp);

        Vector2 size = new(0.7f);

        float opacity = GetOpacity();
        float scale = GetScale();

        size *= scale;

        position.X -= 11.5f * (1f - scale);
        position.Y -= 127.5f * (1f - scale);

        TextSnippet[] snippets = ChatManager.ParseMessage(text, Color.White).ToArray();
        ChatManager.ConvertNormalSnippets(snippets);

        ChatManager.DrawColorCodedStringShadow(spriteBatch, FontAssets.DeathText.Value, snippets, position, Color.Black * MathHelper.Lerp(0.125f, 0.25f, 0f) * 0.75f * opacity * opacity, 0f, origin, size);
        ChatManager.DrawColorCodedStringShadow(spriteBatch, FontAssets.DeathText.Value, snippets, position, color * MathHelper.Lerp(0.125f, 0.25f, 0.5f) * opacity * opacity, 0f, origin, size);
        ChatManager.DrawColorCodedString(spriteBatch, FontAssets.DeathText.Value, snippets, position, Color.White * opacity, 0f, origin, size, out int hoveredSnipper, origin.X * 2f, true);

        Vector2 anchorPositionOnScreen2 = Main.ScreenSize.ToVector2() / 2f;
        float offsetY = MathHelper.Lerp(150f, 162.5f, 0.5f);
        anchorPositionOnScreen2.Y = offsetY - 50;
        anchorPositionOnScreen2.Y = MathHelper.Lerp(anchorPositionOnScreen2.Y, lastPosY - 145f, lerp);
        anchorPositionOnScreen2.Y += MathF.Sin(Main.GlobalTimeWrappedHourly * 1.25f) * 5f * GetScale() * GetOpacity();
        //if (Main.gameMenu)
        //    anchorPositionOnScreen2.Y = 300f;

        DrawIcons(spriteBatch, anchorPositionOnScreen2);
    }

    private static float GetScale() {
        float opacity = _opacity2;
        if (!_wantsToBeSeen) {
            opacity = 1f;
        }
        opacity = Utils.GetLerpValue(0f, 0.5f, opacity, true);
        opacity = Ease.CubeOut(opacity);
        float scale = opacity;
        return scale;
    }

    private static float GetOpacity() {
        float opacity = _opacity2;
        opacity = Utils.GetLerpValue(0f, 1f, opacity, true);
        opacity = Ease.CubeOut(opacity);
        float scale = opacity;
        return scale;
    }

    private void DrawIcons(SpriteBatch spriteBatch, Vector2 position) {
        int num = 2;
        int num2 = num * 1;

        var vanillaIcon = ModContent.Request<Texture2D>("Consolaria/icon_small");
        var clip = vanillaIcon.Frame(1, 1);

        float opacity = GetOpacity();
        float scale = GetScale();
        //scale = 1f;

        Effect pixelShader = Main.pixelShader;

        //spriteBatch.DrawWithSnapshot(() => {
        //    pixelShader.CurrentTechnique.Passes["ColorOnly"].Apply();
        //    int num = 2;
        //    int num2 = num * 2;
        //    for (int i = -num2; i <= num2; i += num) {
        //        for (int j = -num2; j <= num2; j += num) {
        //            if (Math.Abs(i) + Math.Abs(j) == num2) {
        //                spriteBatch.Draw(vanillaIcon.Value, position + new Vector2((float)i, (float)j), clip, Color.Black * opacity, 0f, clip.Centered(), scale, default, 0f);
        //            }
        //        }
        //    }
        //    num2 = num;
        //    for (int k = -num2; k <= num2; k += num) {
        //        for (int l = -num2; l <= num2; l += num) {
        //            if (Math.Abs(k) + Math.Abs(l) == num2) {
        //                spriteBatch.Draw(vanillaIcon.Value, position + new Vector2((float)k, (float)l), clip, Color.White * opacity, 0f, clip.Centered(), scale, default, 0f);
        //            }
        //        }
        //    }
        //}, sortMode: SpriteSortMode.Immediate);
        //spriteBatch.Draw(vanillaIcon.Value, position, clip, Color.White * opacity, 0f, clip.Centered(), scale, default, 0f);
        var player = Main.LocalPlayer;
        //// adapted vanilla
        Microsoft.Xna.Framework.Rectangle mouseRectangle = new Microsoft.Xna.Framework.Rectangle((int)((float)Main.mouseX /*+ Main.screenPosition.X*/), (int)((float)Main.mouseY/* + Main.screenPosition.Y*/), 1, 1);
        Microsoft.Xna.Framework.Rectangle value2 = new Microsoft.Xna.Framework.Rectangle((int)((double)position.X - clip.Width / 2/* + Main.screenPosition.X*/), (int)(position.Y - clip.Height / 2/* + Main.screenPosition.Y*/), (int)(clip.Width/* * Main.UIScale*/), (int)(clip.Height/* * Main.UIScale*/));

        //if (!Main.mouseText && mouseRectangle.Intersects(value2)) {
        //    player.cursorItemIconEnabled = false;

        //    string text2 = "Terraria Titles";

        //    _mouseText = text2;
        //    //Main.instance.MouseTextHackZoom(text2);
        //    Main.mouseText = true;
        //    //WreathDrawing.JustDrawn = true;
        //}
        //else {
        //    //WreathDrawing.JustDrawn = false;
        //}

        float num5 = Main.GlobalTimeWrappedHourly * 0.02f /** 10f*/ % 1f;
        if (num5 < 0f)
            num5 += 1f;

        Color color = Main.hslToRgb(num5, 1f, 0.5f);

        var consolariaIcon = ModContent.Request<Texture2D>("Consolaria/Assets/Textures/ConsolariaLogo");
        clip = consolariaIcon.Value.Bounds;
        //position.X -= 370f + 20f;

        scale *= 0.5f;

        spriteBatch.DrawWithSnapshot(() => {
            pixelShader.CurrentTechnique.Passes["ColorOnly"].Apply();
            int num = 2;
            int num2 = num * 2;
            //for (int i = -num2; i <= num2; i += num) {
            //    for (int j = -num2; j <= num2; j += num) {
            //        if (Math.Abs(i) + Math.Abs(j) == num2) {
            //            spriteBatch.Draw(consolariaIcon.Value, position + new Vector2((float)i, (float)j), clip, Color.Black * opacity, 0f, clip.Centered(), scale, default, 0f);
            //        }
            //    }
            //}
            num2 = num;
            for (int k = -num2; k <= num2; k += num) {
                for (int l = -num2; l <= num2; l += num) {
                    if (Math.Abs(k) + Math.Abs(l) == num2) {
                        spriteBatch.Draw(consolariaIcon.Value, position + new Vector2((float)k, (float)l), clip, color * opacity * 0.25f * MathHelper.Lerp(0.25f, 1f, 0.125f) * opacity * MathHelper.Lerp(1f, opacity, 0.5f), 0f, clip.Centered(), scale, default, 0f);
                    }
                }
            }
        }, sortMode: SpriteSortMode.Immediate);
        spriteBatch.Draw(consolariaIcon.Value, position, clip, Color.White * opacity, 0f, clip.Centered(), scale, default, 0f);
        // adapted vanilla
        value2 = new Microsoft.Xna.Framework.Rectangle((int)((double)position.X - clip.Width / 2/* + Main.screenPosition.X*/), (int)(position.Y - clip.Height / 2/* + Main.screenPosition.Y*/), (int)(clip.Width/* * Main.UIScale*/), (int)(clip.Height/* * Main.UIScale*/));
        if (GetScale() * GetOpacity() > 0.5f && !Main.mouseText && mouseRectangle.Intersects(value2)) {
            player.cursorItemIconEnabled = false;

            string text2 = Language.GetTextValue("Mods.Consolaria.Credits");

            _mouseText = text2;
            //Main.instance.MouseTextHackZoom(text2);
            Main.mouseText = true;
            //WreathDrawing.JustDrawn = true;
        }
        else {
            //WreathDrawing.JustDrawn = false;
        }
    }

    private static string _mouseText;

    private void On_Main_DrawPendingMouseText(On_Main.orig_DrawPendingMouseText orig) {
        orig();

        if (!Main.gameMenu) {
            if (_mouseText != string.Empty && Main.instance.mouseNPCType <= -1 && Main.HoverItem.type <= 0) {
                float num5 = Main.GlobalTimeWrappedHourly * 0.02f /** 10f*/ % 1f;
                if (num5 < 0f)
                    num5 += 1f;

                Color color = Main.hslToRgb(num5, 1f, 0.5f);

                int X = Main.mouseX + 14;
                int Y = Main.mouseY + 14;
                if (Main.ThickMouse) {
                    X += 6;
                    Y += 6;
                }

                if (!Main.mouseItem.IsAir)
                    X += 34;

                var spriteBatch = Main.spriteBatch;

                float opacity = _opacity;

                ChatManager.DrawColorCodedStringShadow(spriteBatch, FontAssets.MouseText.Value, _mouseText, new Vector2(X, Y), color * 0.25f, 0f, Vector2.Zero, Vector2.One);
                ChatManager.DrawColorCodedString(spriteBatch, FontAssets.MouseText.Value, _mouseText, new Vector2(X, Y), Color.White, 0f, Vector2.Zero, Vector2.One);
            }
        }

        _mouseText = string.Empty;
    }

    public override bool IsActive() => _isActive;

    public override void Reset() {
        _currentTime = 0;
        EnsureSegmentsAreMade();
        _isActive = false;
        _wantsToBeSeen = false;
    }

    public override void Update(GameTime gameTime) {
        if (Main.gamePaused || !Main.hasFocus)
            return;

        _currentTime++;
        float num = 1f / 120f;
        if (Main.gameMenu)
            num = 1f / 15f;

        float num2 = num;
        //if (_wantsToBeSeen)
        {
            num2 *= 0.25f;
        }

        _opacity = MathHelper.Clamp(_opacity + num * (float)_wantsToBeSeen.ToDirectionInt(), 0f, 1f);
        if (_currentTime > _preparationTime || !_wantsToBeSeen) {
            _opacity2 = MathHelper.Clamp(_opacity2 + num2 * (float)_wantsToBeSeen.ToDirectionInt(), 0f, 1f);
        }
        if (_opacity == 0f && _opacity2 == 0f && !_wantsToBeSeen) {
            if (_isActive) {
                CreditsRollEvent.TryStartingCreditsRoll();
            }
            _isActive = false;
            return;
        }

        bool flag = true;
        if (!CanPlayCreditsRoll())
            flag = false;

        if (_currentTime >= _endTime)
            flag = false;

        if (!flag)
            SkyManager.Instance.Deactivate("ConsolariaCreditsRoll");
    }
}
