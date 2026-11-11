using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Animations;
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

        vector2.X *= -1f;
        num += PlaySegment_TextRoll(num, "Mods.Consolaria.ConsolariaCreditsRollCategory_Contributors1", vector2).totalTime;
        num += num3;
        num += num3;

        vector2.X *= -1f;
        num += PlaySegment_TextRoll(num, "Mods.Consolaria.ConsolariaCreditsRollCategory_Contributors2", vector2).totalTime;
        num += num3;
        num += num3;

        vector2.X *= -1f;
        num += PlaySegment_TextRoll(num, "Mods.Consolaria.ConsolariaCreditsRollCategory_Contributors3", vector2).totalTime;
        num += num3;
        num += num3;

        vector2.X *= -1f;
        num += PlaySegment_TextRoll(num, "Mods.Consolaria.ConsolariaCreditsRollCategory_Contributors4", vector2).totalTime;
        num += num3;
        num += num3;

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
}
