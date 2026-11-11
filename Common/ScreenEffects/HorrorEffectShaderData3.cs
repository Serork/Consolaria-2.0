using Consolaria.Content.NPCs.Bosses.EternalHorror;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Shaders;

namespace Consolaria.Common.ScreenEffects;

sealed class HorrorEffectShaderData3(Asset<Effect> effect, string pass) : ScreenShaderData(effect, pass) {
    public static float STRENGTH => 3f;

    public override void Update(GameTime gameTime) {
        //UseImage(ResourceManager.Noise, 1);

        //var color = WorldGen.crimson ? new Color(238, 97, 94) : new Color(130, 125, 226);
        //color *= 1f;

        //UseColor(color);

        UseColor(EternalHorror.MainPurpleColor.ModifyRGB(1f));
        UseOpacity(Helper.Clamp01(EternalHorror.ChromaOpacity) * 1.25f);

        Shader.Parameters["strength"].SetValue(10.0f);
        Shader.Parameters["depth"].SetValue(0f);
    }

    public override void Apply() {
        float time2 = Utils.Remap(EternalHorror.ChromaOpacity, 0f, STRENGTH, 1f, 0f, true);

        float time = Main.GlobalTimeWrappedHourly;
        Main.GlobalTimeWrappedHourly = time2;

        Vector2 zoom = Main.GameViewMatrix.Zoom;
        Main.GameViewMatrix.Zoom = Vector2.One;

        base.Apply();

        Main.GlobalTimeWrappedHourly = time;
        Main.GameViewMatrix.Zoom = zoom;
    }
}
