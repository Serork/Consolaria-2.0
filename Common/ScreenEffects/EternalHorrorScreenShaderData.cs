using Consolaria.Content.NPCs.Bosses.EternalHorror;
using Terraria;
using Terraria.Graphics.Shaders;

namespace Consolaria.Common.ScreenEffects;

sealed class EternalHorrorScreenShaderData : ScreenShaderData {
    private int _eternalHorrorIndex = -1;
    private bool _aimAtPlayer;

    public EternalHorrorScreenShaderData(string passName, bool aimAtPlayer)
        : base(passName) {
        _aimAtPlayer = aimAtPlayer;
    }

    private void UpdateMoonLordIndex() {
        if (_aimAtPlayer || (_eternalHorrorIndex >= 0 && Main.npc[_eternalHorrorIndex].active && Main.npc[_eternalHorrorIndex].type == EternalHorror.SelfType))
            return;

        int moonLordIndex = -1;
        for (int i = 0; i < Main.npc.Length; i++) {
            if (Main.npc[i].active && Main.npc[i].type == EternalHorror.SelfType) {
                moonLordIndex = i;
                break;
            }
        }

        _eternalHorrorIndex = moonLordIndex;
    }

    public override void Apply() {
        UpdateMoonLordIndex();
        if (_aimAtPlayer)
            UseTargetPosition(Main.LocalPlayer.Center);
        else if (_eternalHorrorIndex != -1)
            UseTargetPosition(Main.npc[_eternalHorrorIndex].Center);

        base.Apply();
    }
}
