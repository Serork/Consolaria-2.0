using Consolaria.Content.NPCs.Bosses.EternalHorror;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Shaders;

namespace RoA.Common.ScreenEffects;

sealed class HorrorZoneShaderData(Asset<Effect> effect, string pass) : ScreenShaderData(effect, pass) {
    public static float Time { get; internal set; }

    public static float TimeFactor { get; internal set; } = 1 / 60f;

    public override void Update(GameTime gameTime) {
        //UseImage(ResourceManager.Noise, 1);

        //var color = WorldGen.crimson ? new Color(238, 97, 94) : new Color(130, 125, 226);
        //color *= 1f;

        //UseColor(color);

        UseColor(EternalHorror.MainPurpleColor * 1f);
        //UseOpacity(0.5f);

        Shader.Parameters["strength"].SetValue(3.0f);
        Shader.Parameters["strength2"].SetValue(1.5f);
        Shader.Parameters["strength3"].SetValue(5.0f);

        Shader.Parameters["centerFactor"].SetValue(1.25f);
        Shader.Parameters["centerFactor2"].SetValue(0.075f);

        Shader.Parameters["tilingFactor"].SetValue(0.25f);
    }

    public override void Apply() {
        Time += TimeFactor / 10f;

        float time = Main.GlobalTimeWrappedHourly;
        Main.GlobalTimeWrappedHourly = Time;

        Vector2 zoom = Main.GameViewMatrix.Zoom;
        Main.GameViewMatrix.Zoom = Vector2.One;

        base.Apply();

        Main.GlobalTimeWrappedHourly = time;
        Main.GameViewMatrix.Zoom = zoom;
    }
}
