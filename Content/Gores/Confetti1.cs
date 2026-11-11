using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Consolaria.Content.Gores;

class Confetti6 : Confetti1 { }
class Confetti5 : Confetti1 { }
class Confetti4 : Confetti1 { }
class Confetti3 : Confetti1 { }
class Confetti2 : Confetti1 { }
class Confetti1 : ModGore {
    public override Color? GetAlpha(Gore gore, Color lightColor) => Color.White * (1f - gore.alpha / 255f);

    public override bool Update(Gore gore) {
        gore.velocity.X *= 0.98f;
        gore.velocity.Y *= 0.98f;
        if (gore.velocity.Y < gore.scale)
            gore.velocity.Y += 0.05f;

        if ((double)gore.velocity.Y > 0.1) {
            if (gore.velocity.X > 0f)
                gore.rotation += 0.01f;
            else
                gore.rotation -= 0.01f;
        }

        return true;
    }
}
