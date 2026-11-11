using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Consolaria.Content.Dusts;

class Confetti4 : Confetti1 { }
class Confetti3 : Confetti1 { }
class Confetti2 : Confetti1 { }
class Confetti1 : ModDust {
    public override Color? GetAlpha(Dust dust, Color lightColor) => Color.Lerp(Color.White, lightColor, Ease.CubeOut(1f - (float)dust.customData));

    public override void OnSpawn(Dust dust) {
        dust.customData = 1f;
    }

    public override bool Update(Dust dust) {
        dust.customData = Helper.Approach((float)dust.customData, 0f, 1 / 60f * 0.125f * 0.575f);

        dust.BasicDust(applyGravity: false);

        dust.velocity.X *= 0.98f;
        dust.velocity.Y *= 0.98f;
        if (dust.velocity.Y < 1f)
            dust.velocity.Y += 0.05f;

        dust.scale += 0.009f;
        dust.rotation -= dust.velocity.X * 0.4f;
        if (dust.velocity.X > 0f)
            dust.rotation += 0.005f;
        else
            dust.rotation -= 0.005f;

        return false;
    }
}