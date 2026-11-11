using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Animations;
using Terraria.ID;
using Terraria.Localization;

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
        num += PlaySegment_TextRoll(num, "Mods.Consolaria.ConsolariaCreditsRollCategory_Creators", vector2).totalTime;
        num += num3;
        num += num3;
        num += 60;

        // bodya
        PlaySegment_DryadTurningToTree(num - num3, new Vector2(vector2.X, vector2.Y));

        vector2.X *= -1f;
        num += PlaySegment_TextRoll(num, "Mods.Consolaria.ConsolariaCreditsRollCategory_Contributors1", vector2).totalTime;
        num += num3;
        num += num3;

        // something else
        PlaySegment_DryadTurningToTree(num - num3, new Vector2(vector2.X, vector2.Y));

        vector2.X *= -1f;
        num += PlaySegment_TextRoll(num, "Mods.Consolaria.ConsolariaCreditsRollCategory_Contributors2", vector2).totalTime;
        num += num3;
        num += num3;

        // andryuha
        PlaySegment_DryadTurningToTree(num - num3, new Vector2(vector2.X, vector2.Y));

        vector2.X *= -1f;
        num += PlaySegment_TextRoll(num, "Mods.Consolaria.ConsolariaCreditsRollCategory_Contributors3", vector2).totalTime;
        num += num3;
        num += num3;

        // something else
        PlaySegment_DryadTurningToTree(num - num3, new Vector2(vector2.X, vector2.Y));

        vector2.X *= -1f;
        num += PlaySegment_TextRoll(num, "Mods.Consolaria.ConsolariaCreditsRollCategory_Contributors4", vector2).totalTime;
        num += num3;
        num += num3;

        // serega
        PlaySegment_DryadTurningToTree(num - num3, new Vector2(vector2.X, vector2.Y));

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

    private SegmentInforReport PlaySegment_DryadTurningToTree(int startTime, Vector2 sceneAnchorPosition) {
        sceneAnchorPosition += GetSceneFixVector();
        int num = startTime;
        Asset<Texture2D> asset = TextureAssets.Extra[217];
        Rectangle rectangle = asset.Frame();
        DrawData data = new DrawData(asset.Value, Vector2.Zero, rectangle, Color.White, 0f, rectangle.Size() * new Vector2(0.5f, 1f) + new Vector2(0f, -42f), 1f, SpriteEffects.None);
        Segments.AnimationSegmentWithActions<Segments.LooseSprite> animationSegmentWithActions = new Segments.SpriteSegment(asset, num, data, sceneAnchorPosition + _backgroundOffset).UseShaderEffect(new Segments.SpriteSegment.MaskedFadeEffect()).Then(new Actions.Sprites.Fade(0f)).With(new Actions.Sprites.Fade(1f, 60));
        Segments.AnimationSegmentWithActions<NPC> animationSegmentWithActions2 = new Segments.NPCSegment(startTime, 20, sceneAnchorPosition, _originAtBottom).Then(new Actions.NPCs.Fade(255)).With(new Actions.NPCs.Fade(-5, 51)).Then(new Actions.NPCs.Move(new Vector2(1f, 0f), 60))
            .Then(new Actions.NPCs.Wait(10))
            .Then(new Actions.NPCs.Fade(0));

        num += (int)animationSegmentWithActions2.DedicatedTimeNeeded;
        Asset<Texture2D> asset2 = TextureAssets.Extra[215];
        Rectangle rectangle2 = asset2.Frame(1, 9);
        DrawData data2 = new DrawData(asset2.Value, Vector2.Zero, rectangle2, Color.White, 0f, rectangle2.Size() * new Vector2(0.5f, 1f), 1f, SpriteEffects.FlipHorizontally);
        Vector2 vector = new Vector2(1f, 0f) * 60f + new Vector2(2f, 4f);
        Segments.AnimationSegmentWithActions<Segments.LooseSprite> animationSegmentWithActions3 = new Segments.SpriteSegment(asset2, num, data2, sceneAnchorPosition + vector).Then(new Actions.Sprites.SetFrameSequence(new Point[9] {
            new Point(0, 0),
            new Point(0, 1),
            new Point(0, 2),
            new Point(0, 3),
            new Point(0, 4),
            new Point(0, 5),
            new Point(0, 6),
            new Point(0, 7),
            new Point(0, 8)
        }, 8, 0, 0)).Then(new Actions.Sprites.Wait(30));

        num += (int)animationSegmentWithActions3.DedicatedTimeNeeded;
        Segments.AnimationSegmentWithActions<NPC> item = new Segments.NPCSegment(num, 46, sceneAnchorPosition + new Vector2(-100f, 0f), _originAtBottom).Then(new Actions.NPCs.Fade(255)).With(new Actions.NPCs.Fade(-5, 51)).Then(new Actions.NPCs.Move(new Vector2(1f, 0f), 60))
            .Then(new Actions.NPCs.Wait(90))
            .Then(new Actions.NPCs.Move(new Vector2(1f, 0f), 120))
            .With(new Actions.NPCs.Fade(3, 85));

        Segments.AnimationSegmentWithActions<NPC> item2 = new Segments.NPCSegment(num + 60, 299, sceneAnchorPosition + new Vector2(170f, 0f), _originAtBottom).Then(new Actions.NPCs.Fade(255)).With(new Actions.NPCs.Fade(-5, 51)).Then(new Actions.NPCs.Move(new Vector2(-1f, 0f), 60))
            .Then(new Actions.NPCs.Wait(60))
            .Then(new Actions.NPCs.Move(new Vector2(-1f, 0f), 90))
            .Then(new Actions.NPCs.Move(new Vector2(-1f, 0f), 85))
            .With(new Actions.NPCs.Fade(3, 85));

        float x = 1.5f;
        Segments.AnimationSegmentWithActions<NPC> item3 = new Segments.NPCSegment(num + 45, 74, sceneAnchorPosition + new Vector2(-80f, -70f), _originAtBottom).Then(new Actions.NPCs.Fade(255)).With(new Actions.NPCs.Fade(-5, 51)).Then(new Actions.NPCs.Move(new Vector2(x, 0f), 85))
            .With(new Actions.NPCs.MoveWithRotor(new Vector2(10f, 0f), (float)Math.PI * 2f / 85f, new Vector2(0f, 1f), 85))
            .Then(new Actions.NPCs.Move(new Vector2(x, 0f), 85))
            .With(new Actions.NPCs.MoveWithRotor(new Vector2(4f, 0f), (float)Math.PI * 2f / 85f, new Vector2(0f, 1f), 85))
            .With(new Actions.NPCs.Fade(3, 85));

        Segments.AnimationSegmentWithActions<NPC> item4 = new Segments.NPCSegment(num + 180, 656, sceneAnchorPosition + new Vector2(20f, 0f), _originAtBottom).Then(new Actions.NPCs.Variant(1)).Then(new Actions.NPCs.LookAt(1)).Then(new Actions.NPCs.Fade(255))
            .With(new Actions.NPCs.Fade(-5, 51))
            .Then(new Actions.NPCs.Wait(60))
            .Then(new Actions.NPCs.Move(new Vector2(0.5f, 0f), 60))
            .Then(new Actions.NPCs.Wait(60))
            .Then(new Actions.NPCs.DoBunnyRestAnimation(90))
            .Then(new Actions.NPCs.Wait(90))
            .With(new Actions.NPCs.Fade(3, 120));

        Segments.EmoteSegment item5 = new Segments.EmoteSegment(0, num + 360, 60, sceneAnchorPosition + new Vector2(36f, -10f), SpriteEffects.FlipHorizontally, Vector2.Zero);
        animationSegmentWithActions3.Then(new Actions.Sprites.Wait(420)).Then(new Actions.Sprites.Fade(0f, 120));
        num += 620;
        animationSegmentWithActions.Then(new Actions.Sprites.Wait(num - startTime - 180)).Then(new Actions.Sprites.Fade(0f, 120));
        _segments.Add(animationSegmentWithActions);
        _segments.Add(animationSegmentWithActions2);
        _segments.Add(animationSegmentWithActions3);
        _segments.Add(item);
        _segments.Add(item2);
        _segments.Add(item3);
        _segments.Add(item4);
        _segments.Add(item5);
        SegmentInforReport result = default(SegmentInforReport);
        result.totalTime = num - startTime;
        return result;
    }
}
