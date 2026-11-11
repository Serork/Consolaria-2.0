using Consolaria.Content.Items.Consumables;
using Consolaria.Content.Items.Vanity.Developer;
using Consolaria.Content.NPCs.Bosses.Lepus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Animations;
using Terraria.GameContent.Skies;
using Terraria.GameContent.Skies.CreditsRoll;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static Terraria.GameContent.Animations.Segments;

namespace Consolaria.Common;

sealed class ConsolariaCreditsRollComposer {
    private Vector2 _originAtBottom = new Vector2(0.5f, 1f);
    private Vector2 _emoteBubbleOffsetWhenOnLeft = new Vector2(-14f, -38f);
    private Vector2 _emoteBubbleOffsetWhenOnRight = new Vector2(14f, -38f);
    private Vector2 _backgroundOffset = new Vector2(76f, 166f);
    private int _endTime;
    private List<IAnimationSegment> _segments;

    public void FillSegments(List<IAnimationSegment> segmentsList, out int endTime, bool inGame) {
        _segments = segmentsList;
        int num = 0;
        int num2 = 80;
        Vector2 vector = Vector2.UnitY * -1f * num2;
        int num3 = 210;
        Vector2 vector2 = vector + Vector2.UnitX * -200f;
        Vector2 vector3 = vector2;
        if (!inGame)
            vector3 = (vector2 = Vector2.UnitY * 80f);

        int num4 = num3 * 3;
        int num5 = num3 * 3;
        int num6 = num4 - num5;
        if (!inGame) {
            num5 = 180;
            num6 = num4 - num5;
        }

        num += num5;
        num += num3;
        ConsolariaCreditsSky._preparationTime = num;

        //// test remove later
        //PlaySegment_Has2rLaughing(num - num3, new Vector2(vector2.X * -1f, vector2.Y));

        num += PlaySegment_TextRoll(num, "Mods.Consolaria.ConsolariaCreditsRollCategory_Creators", vector2).totalTime;
        num += num3;
        num += num3;
        num += 60;

        // bodya
        PlaySegment_Has2rLaughing(num - num3, new Vector2(vector2.X, vector2.Y));

        vector2.X *= -1f;
        num += PlaySegment_TextRoll(num, "Mods.Consolaria.ConsolariaCreditsRollCategory_Contributors1", vector2).totalTime;
        num += num3;
        num += num3;

        // something else
        //PlaySegment_Has2rLaughing(num - num3, new Vector2(vector2.X, vector2.Y));

        vector2.X *= -1f;
        num += PlaySegment_TextRoll(num, "Mods.Consolaria.ConsolariaCreditsRollCategory_Contributors2", vector2).totalTime;
        num += num3;
        num += num3;

        // andryuha
        //PlaySegment_Has2rLaughing(num - num3, new Vector2(vector2.X, vector2.Y));

        vector2.X *= -1f;
        num += PlaySegment_TextRoll(num, "Mods.Consolaria.ConsolariaCreditsRollCategory_Contributors3", vector2).totalTime;
        num += num3;
        num += num3;

        // something else
        //PlaySegment_Has2rLaughing(num - num3, new Vector2(vector2.X, vector2.Y));

        vector2.X *= -1f;
        num += PlaySegment_TextRoll(num, "Mods.Consolaria.ConsolariaCreditsRollCategory_Contributors4", vector2).totalTime;
        num += num3;
        num += num3;

        // serega
        //PlaySegment_Has2rLaughing(num - num3, new Vector2(vector2.X, vector2.Y));

        vector2.X *= -1f;
        num += PlaySegment_TextRoll(num, "Mods.Consolaria.ConsolariaCreditsRollCategory_Contributors5", vector2).totalTime;
        num += num3;
        num += num3;

        //num += num3;
        //num += num3;
        //num += num3;

        num -= 60;

        num += num6;

        _endTime = num + 10;
        endTime = _endTime;

        ConsolariaCreditsSky._allTime = _endTime;
    }

    private Vector2 GetSceneFixVector() => new Vector2(0f - _backgroundOffset.X, 0f);

    private SegmentInforReport PlaySegment_TextRoll(int startTime, string sourceCategory, Vector2 anchorOffset = default(Vector2)) {
        anchorOffset.Y -= 40f;
        int num = 80;
        LocalizedText[] array = Language.FindAll(Lang.CreateDialogFilter(sourceCategory + ".", null));
        for (int i = 0; i < array.Length; i++) {
            _segments.Add(new Segments.LocalizedTextSegment(startTime + i * num, array[i], anchorOffset));
        }

        SegmentInforReport result = default(SegmentInforReport);
        result.totalTime = array.Length * num + num * -1;
        return result;
    }

    private struct SimplifiedNPCInfo {
        private Vector2 _simplifiedPosition;
        private int _npcType;

        public SimplifiedNPCInfo(int npcType, Vector2 simplifiedPosition) {
            _simplifiedPosition = simplifiedPosition;
            _npcType = npcType;
        }

        public void SpawnNPC(AddNPCMethod methodToUse, Vector2 baseAnchor, int startTime, int totalSceneTime) {
            Vector2 properPosition = GetProperPosition(baseAnchor);
            int lookDirection = ((!(_simplifiedPosition.X > 0f)) ? 1 : (-1));
            int num = 240;
            int timeToJumpAt = (totalSceneTime - num) / 2 - 20 + (int)(_simplifiedPosition.X * -8f);
            methodToUse(_npcType, properPosition, lookDirection, startTime, totalSceneTime, timeToJumpAt);
        }

        private Vector2 GetProperPosition(Vector2 baseAnchor) => baseAnchor + _simplifiedPosition * new Vector2(26f, 24f);
    }

    private delegate void AddNPCMethod(int npcType, Vector2 sceneAnchoePosition, int lookDirection, int fromTime, int duration, int timeToJumpAt);

    private SegmentInforReport PlaySegment_Has2rLaughing(int startTime, Vector2 sceneAnchorPosition) {
        sceneAnchorPosition += GetSceneFixVector();
        int num = startTime;
        Asset<Texture2D> asset = ConsolariaCreditsSky.Has2rCutsceneArea;
        Rectangle rectangle = asset.Frame();
        DrawData data = new DrawData(asset.Value, Vector2.Zero, rectangle, Color.White, 0f, rectangle.Size() * new Vector2(0.5f, 1f) + new Vector2(0f, -42f), 1f, SpriteEffects.None);
        Segments.AnimationSegmentWithActions<Segments.LooseSprite> animationSegmentWithActions = new Segments.SpriteSegment(asset, num, data, sceneAnchorPosition + _backgroundOffset).UseShaderEffect(new Segments.SpriteSegment.MaskedFadeEffect()).Then(new Actions.Sprites.Fade(0f)).With(new Actions.Sprites.Fade(1f, 60));

        num += 70;

        float num6 = 1f;
        float num7 = 0f;
        int num8 = 60;
        Segments.AnimationSegmentWithActions<Player> item2 = new CustomPlayerSegment(startTime, sceneAnchorPosition + new Vector2(-100f, 0f), _originAtBottom, CustomPlayerSegment.DevType.Has2r).UseShaderEffect(new CustomPlayerSegment.ImmediateSpritebatchForPlayerDyesEffect())
            .Then(new Actions.Players.Fade(0f))
            .Then(new Actions.Players.LookAt(1))
            .With(new Actions.Players.Fade(1f, num8))
            .Then(new Actions.Players.MoveWithAcceleration(new Vector2(num6, 0), new Vector2(num7, 0f), 180))
            .Then(new Actions.Players.Wait(20))
            .Then(new Actions.Players.LookAt(-1))
            .Then(new Actions.Players.Wait(200 + 30))
            .Then(new Actions.Players.LookAt(1))
            .Then(new Actions.Players.Wait(120))
            .Then(new Actions.Players.MoveWithAcceleration(new Vector2(-num6, 0), new Vector2(num7, 0f), 75))
            .Then(new Actions.Players.MoveWithAcceleration(new Vector2(-num6, 0), new Vector2(num7, 0f), 100))
            .With(new Actions.Players.Fade(0f, 100));

        Segments.AnimationSegmentWithActions<NPC> item3 = new Segments.NPCSegment(startTime + 340, ModContent.NPCType<DisasterBunny>(), sceneAnchorPosition + new Vector2(210f, 0f), _originAtBottom)
            .Then(new Actions.NPCs.Fade(255))
            .With(new Actions.NPCs.Fade(-5, 51))
            .Then(new Actions.NPCs.Move(new Vector2(-1f, 0f), 60))
            .Then(new Actions.NPCs.Wait(60 + 60 + 30))
            .Then(new Actions.NPCs.Move(new Vector2(-1f, 0f), 90))
            .Then(new Actions.NPCs.Move(new Vector2(-1f, 0f), 85))
            .With(new Actions.NPCs.Fade(3, 85));

        num += 102;

        num += 620;

        animationSegmentWithActions.Then(new Actions.Sprites.Wait(num - startTime - 180)).Then(new Actions.Sprites.Fade(0f, 120));
        _segments.Add(animationSegmentWithActions);
        _segments.Add(item2);
        _segments.Add(item3);

        AddEmote(sceneAnchorPosition + new Vector2(101f - 20f, 0), startTime + 160, 60, 0, 15, -1);
        AddEmote(sceneAnchorPosition + new Vector2(101f - 20f - 30f, 0), startTime + 400, 60, 0, EmoteID.EmoteSilly, 1, time: 60);

        SegmentInforReport result = default(SegmentInforReport);
        result.totalTime = num - startTime;
        return result;
    }

    private void AddEmote(Vector2 sceneAnchoePosition, int fromTime, int duration, int blinkTime, int emoteId, int direction) {
        Segments.EmoteSegment item = new Segments.EmoteSegment(emoteId, fromTime + duration - blinkTime, 120, sceneAnchoePosition + _emoteBubbleOffsetWhenOnRight, (direction == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
        _segments.Add(item);
    }

    private void AddEmote(Vector2 sceneAnchoePosition, int fromTime, int duration, int blinkTime, int emoteId, int direction, int time = 120) {
        Segments.EmoteSegment item = new Segments.EmoteSegment(emoteId, fromTime + duration - blinkTime, time, sceneAnchoePosition + _emoteBubbleOffsetWhenOnRight, (direction == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
        _segments.Add(item);
    }

    public class CustomPlayerSegment : AnimationSegmentWithActions<Player> {
        public enum DevType : byte {
            Has2r,
            PeegeOn,
            Serork
        }

        public interface IShaderEffect {
            void BeforeDrawing(ref GameAnimationSegment info);

            void AfterDrawing(ref GameAnimationSegment info);
        }

        public class ImmediateSpritebatchForPlayerDyesEffect : IShaderEffect {
            public void BeforeDrawing(ref GameAnimationSegment info) {
                info.SpriteBatch.End();
                info.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.CurrentFrameFlags.Hacks.CurrentBackgroundMatrixForCreditsRoll);
            }

            public void AfterDrawing(ref GameAnimationSegment info) {
                info.SpriteBatch.End();
                info.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.CurrentFrameFlags.Hacks.CurrentBackgroundMatrixForCreditsRoll);
            }
        }

        private DevType _devType;
        private Player _player;
        private Vector2 _anchorOffset;
        private Vector2 _normalizedOriginForHitbox;
        private IShaderEffect _shaderEffect;
        private static Item _blankItem = new Item();
        private int _cannonShootTime;

        public CustomPlayerSegment(int targetTime, Vector2 anchorOffset, Vector2 normalizedHitboxOrigin, DevType devType)
            : base(targetTime) {
            _player = new Player();
            _anchorOffset = anchorOffset;
            _normalizedOriginForHitbox = normalizedHitboxOrigin;
            _devType = devType;
        }

        public CustomPlayerSegment UseShaderEffect(IShaderEffect shaderEffect) {
            _shaderEffect = shaderEffect;
            return this;
        }

        protected override void Bind(IAnimationSegmentAction<Player> act) {
            act.BindTo(_player);
        }

        public override void Draw(ref GameAnimationSegment info) {
            if ((float)info.TimeInAnimation > (float)_targetTime + base.DedicatedTimeNeeded || info.TimeInAnimation < _targetTime)
                return;

            ResetPlayerAnimation(ref info);
            float localTimeForObject = info.TimeInAnimation - _targetTime;
            ProcessActions(_player, localTimeForObject);
            if (info.DisplayOpacity != 0f) {
                _player.ResetEffects();
                _player.ResetVisibleAccessories();
                _player.UpdateMiscCounter();

                switch (_devType) {
                    case DevType.Has2r:
                        _player.hair = 26;
                        _player.GetModPlayer<EldritchRing.EldritchRingDrawLayer.EldritchRingHandler>().shouldDraw = true;
                        _player.armor[0].SetDefaults(ModContent.ItemType<Has2rMask>());
                        _player.armor[1].SetDefaults(ModContent.ItemType<Has2rJacket>());
                        _player.armor[2].SetDefaults(ModContent.ItemType<Has2rPants>());
                        _player.armor[3].SetDefaults(ModContent.ItemType<EldritchRing>());
                        break;
                    case DevType.PeegeOn:
                        _player.armor[0].SetDefaults(ModContent.ItemType<SerorkHelmet>());
                        _player.armor[1].SetDefaults(ModContent.ItemType<SerorkBreastplate>());
                        _player.armor[2].SetDefaults(ModContent.ItemType<SerorkGreaves>());
                        break;
                    case DevType.Serork:
                        _player.armor[0].SetDefaults(ModContent.ItemType<PeegeonHood>());
                        _player.armor[1].SetDefaults(ModContent.ItemType<PeegeonChestguard>());
                        _player.armor[2].SetDefaults(ModContent.ItemType<PeegeonGreaves>());
                        _player.armor[3].SetDefaults(ModContent.ItemType<PeegeonCape>());
                        break;
                }

                _player.UpdateDyes();
                _player.PlayerFrame();

                _player.position += Main.screenPosition;
                _player.position -= new Vector2(_player.width / 2, _player.height);

                int mouseX = Main.mouseX;
                int mouseY = Main.mouseY;

                // has2r
                {
                    _player.inventory[_player.selectedItem].SetDefaults(ModContent.ItemType<ConfettiGun_UI>());

                    Main.mouseX = (int)(_player.Center.X + 100);
                    Main.mouseY = (int)(_player.Center.Y - 0);

                    _cannonShootTime++;
                    if (_cannonShootTime == 500) {
                        _player.controlUseItem = true;
                        _player.releaseUseItem = true;

                        //if (_player.whoAmI == Main.myPlayer && Main.netMode != 2) {
                        //    Main.NewText(1);
                        //    Projectile.NewProjectile(_player.GetSource_FromThis(), _player.Center, Vector2.UnitY.RotatedBy(-MathHelper.PiOver2) * 5f, 178, 0, 0f, _player.whoAmI);
                        //}
                    }
                }

                _player.ItemCheck();

                _player.socialIgnoreLight = true;

                _player.opacityForAnimation *= info.DisplayOpacity;

                Item item = _player.inventory[_player.selectedItem];
                _player.inventory[_player.selectedItem] = _blankItem;

                float num = 1f - _player.opacityForAnimation;
                num = 0f;
                if (_shaderEffect != null)
                    _shaderEffect.BeforeDrawing(ref info);

                Main.PlayerRenderer.DrawPlayer(Main.Camera, _player, _player.position, 0f, _player.fullRotationOrigin, num);
                if (_shaderEffect != null)
                    _shaderEffect.AfterDrawing(ref info);

                _player.inventory[_player.selectedItem] = item;

                Main.mouseX = mouseX;
                Main.mouseY = mouseY;
            }
        }

        private void ResetPlayerAnimation(ref GameAnimationSegment info) {
            _player.CopyVisuals(Main.LocalPlayer);

            _player.position = info.AnchorPositionOnScreen + _anchorOffset;
            _player.opacityForAnimation = 1f;
        }
    }
}
