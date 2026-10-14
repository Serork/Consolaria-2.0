using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Consolaria.Content.Gores;

class EternalHorrorGore3 : EternalHorrorGore1 { }
class EternalHorrorGore2 : EternalHorrorGore1 { }
class EternalHorrorGore1 : ModGore {
    public override Color? GetAlpha(Gore gore, Color lightColor) {
        Color drawColor = lightColor;
        drawColor = Color.Lerp(drawColor, Color.White, 0.5f);
        drawColor *= 1f - gore.alpha / 255f;
        return drawColor;
    }
}
