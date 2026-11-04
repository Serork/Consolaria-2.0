using Consolaria;
using Consolaria.Content.NPCs.Bosses.EternalHorror;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Shaders;

namespace RoA.Common.ScreenEffects;

sealed class HorrorDeathShaderData(Asset<Effect> effect, string pass) : ScreenShaderData(effect, pass) {
    public override void Update(GameTime gameTime) {
        //UseImage(ResourceManager.Noise, 1);

        //var color = WorldGen.crimson ? new Color(238, 97, 94) : new Color(130, 125, 226);
        //color *= 1f;

        //UseColor(color);

        UseColor(EternalHorror.MainPurpleColor);
        UseOpacity(EternalHorror.Whitening_ForDeath);

        Shader.Parameters["q"].SetValue(0.0f);
    }

    public override void Apply() {
        float time2 = Utils.Remap(/*Ease.CubeOut*/(1f - EternalHorror.Whitening_ForDeath), 0f, 1f, -1f, 1f, true);

        float time = Main.GlobalTimeWrappedHourly;
        Main.GlobalTimeWrappedHourly = time2;

        Vector2 zoom = Main.GameViewMatrix.Zoom;
        Main.GameViewMatrix.Zoom = Vector2.One;

        base.Apply();

        Main.GlobalTimeWrappedHourly = time;
        Main.GameViewMatrix.Zoom = zoom;
    }
}
