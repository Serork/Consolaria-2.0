using System.Runtime.CompilerServices;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.Renderers;

namespace Consolaria.Common.Particles;

static class ParticlePools {
    [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "_poolFading")]
    public extern static ref ParticlePool<FadingParticle> ParticleOrchestrator__poolFading(ParticleOrchestrator self);

    [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "_poolPrettySparkle")]
    public extern static ref ParticlePool<PrettySparkleParticle> ParticleOrchestrator__poolPrettySparkle(ParticleOrchestrator self);

    public static ParticlePool<FadingParticle> FadingParticlePool => ParticleOrchestrator__poolFading(null);
    public static ParticlePool<PrettySparkleParticle> PrettySparkleParticlePool => ParticleOrchestrator__poolPrettySparkle(null);
}
