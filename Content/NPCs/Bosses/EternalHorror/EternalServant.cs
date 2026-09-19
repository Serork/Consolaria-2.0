using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed class EternalServant : ModNPC {
    private Vector2 _speed;

    public ref float AttackSpeed => ref NPC.ai[3];

    public override void SetStaticDefaults() {
        NPC.SetTrail(trailingMode: 7, length: 10 / 2);

        Main.npcFrameCount[NPC.type] = 2;

        NPCID.Sets.DontDoHardmodeScaling[Type] = true;
        NPCID.Sets.CantTakeLunchMoney[Type] = true;
        NPCID.Sets.BossBestiaryPriority.Add(Type);
    }

    public override void SetDefaults() {
        int width = 54; int height = width;
        NPC.Size = new Vector2(width, height);

        NPC.aiStyle = -1;
        AnimationType = NPCID.ServantofCthulhu;

        NPC.lifeMax = 500;
        NPC.damage = 90;

        NPC.defense = 10;
        NPC.knockBackResist = 0f;

        NPC.HitSound = SoundID.NPCHit18;
        NPC.DeathSound = SoundID.NPCHit18;

        NPC.noTileCollide = true;
        NPC.noGravity = true;
    }

    public override void AI() {
        NPC.TargetClosest();

        if (NPC.localAI[0] == 0f) {
            NPC.localAI[1] = 1f;
            NPC.localAI[2] = 1f;

            NPC.ai[0] = 2f;
            NPC.ai[1] = -480f;
            NPC.ai[2] = 0f;
        }

        NPC.localAI[1] = Helper.Approach(NPC.localAI[1], MathHelper.Lerp(0.375f, 0.5f, 0.5f), 1 / 60f * 5f);

        NPC.localAI[2] = Helper.Approach(NPC.localAI[2], Helper.Wave(0.875f, 1.125f, 5f, NPC.whoAmI), 0.1f);

        Player target = NPC.GetTargetPlayer();
        Vector2 targetCenter = target.Center;
        targetCenter -= NPC.Size / 2f;

        NPC.noTileCollide = true;
        NPC.noGravity = true;
        //NPC.Center += NPC.velocity;

        if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead)
            NPC.TargetClosest();

        if (NPC.ai[0] == 0f) {
            float num335 = 30f;
            Vector2 vector37 = new Vector2(NPC.position.X + (float)NPC.width * 0.5f, NPC.position.Y + (float)NPC.height * 0.5f);
            float num336 = Main.player[NPC.target].position.X + (float)(Main.player[NPC.target].width / 2) - vector37.X;
            float num337 = Main.player[NPC.target].position.Y + (float)(Main.player[NPC.target].height / 2) - vector37.Y;
            float num338 = (float)Math.Sqrt(num336 * num336 + num337 * num337);
            float num339 = num338;
            num338 = num335 / num338;
            num336 *= num338;
            num337 *= num338;
            NPC.velocity.X = num336;
            NPC.velocity.Y = num337;
            //NPC.rotation = (float)Math.Atan2(NPC.velocity.Y, NPC.velocity.X) + 0.785f;
            NPC.ai[0] = 1f;
            NPC.ai[1] = 0f;
            NPC.netUpdate = true;
        }
        else if (NPC.ai[0] == 1f) {
            //if (justHit) {
            //    this.ai[0] = 2f;
            //    this.ai[1] = 0f;
            //}

            NPC.velocity *= 0.99f;
            NPC.velocity *= 0.99f;

            NPC.ai[1] += 1f;

            if (NPC.ai[1] >= 100f) {
                NPC.netUpdate = true;
                NPC.ai[0] = 2f;
                NPC.ai[1] = 0f;
                NPC.velocity.X = 0f;
                NPC.velocity.Y = 0f;
            }
            else {
                //NPC.rotation = (float)Math.Atan2(NPC.velocity.Y, NPC.velocity.X) + 0.785f;
            }
        }
        else {
            //if (justHit) {
            //    this.ai[0] = 2f;
            //    this.ai[1] = 0f;
            //}

            NPC.velocity *= 0.96f;
            NPC.velocity *= 0.96f;

            NPC.ai[1] += 10f;
            NPC.ai[1] += 10f;

            float num340 = NPC.ai[1] / 120f;
            num340 = 0.1f + num340 * 0.4f;
            //NPC.rotation += num340 * (float)NPC.direction;
            if (NPC.ai[1] >= 120f) {
                NPC.netUpdate = true;
                NPC.ai[0] = 0f;
                NPC.ai[1] = 0f;
            }
        }

        float deltaTime = 1 / 60f;

        Vector2 Center = NPC.Center;

        Vector2 vector = (targetCenter - Center).SafeNormalize(Vector2.Zero);
        //if (Vector2.Dot(this.Speed.SafeNormalize(), vector) < 0.4f) {
        //    return 5;
        //}

        //float distanceToDestination = Vector2.Distance(NPC.Center, targetCenter);
        //float minDistance = 1000f;
        //float inertiaValue = 100f, extraInertiaValue = inertiaValue * 2.5f;
        //float extraInertiaFactor = 1f - Helper.Clamp01(distanceToDestination / minDistance);
        //float inertia = inertiaValue * 3.75f - extraInertiaValue * extraInertiaFactor;
        //NPC.MoveTo(targetCenter, 30f, inertia);

        NPC.rotation += NPC.velocity.Length() * (NPC.velocity.X > 0f).ToDirectionInt() * 0.125f * 0.5f * 0.5f;

        NPC.localAI[0] += 1 / 60f;

        NPC.OffsetTheSameNPC(0.1f);
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
        drawColor = NPC.GetNPCColorTintedByBuffs(npcColor: drawColor);
        drawColor = Color.Lerp(drawColor, Color.White, 0.5f);
        Texture2D texture = NPC.GetTexture();
        Vector2 position = NPC.Center;

        float WaveOffset = NPC.whoAmI;
        float _shadowTime = (WaveOffset + 1) * 2 + NPC.localAI[0];

        float timeLeftProgress = NPC.localAI[1];

        //float opacity = 1f;
        //opacity *= 1f - Utils.GetLerpValue(0.75f, 1f, timeLeftProgress, true);

        Player target = NPC.GetTargetPlayer();
        Vector2 targetCenter = target.Center;
        targetCenter -= NPC.Size / 2f;
        float targetAngle = NPC.AngleTo(targetCenter) - MathHelper.PiOver2;

        //drawColor *= opacity;

        float scale = Helper.Clamp01(NPC.localAI[2]);

        SpriteEffects flip = SpriteEffects.None;

        float _dashOpacity = 0.5f;

        int length = NPC.oldPos.Length - 1;
        for (int num173 = 1; num173 < length; num173 += 1) {
            _ = ref NPC.oldPos[num173];
            Color color39 = drawColor;
            color39 = color39.MultiplyRGBA(EternalHorror.MainPurpleColor_Dynamic);
            color39.R = (byte)(1f * (double)(int)color39.R * (double)(length - num173) / length);
            color39.G = (byte)(1f * (double)(int)color39.G * (double)(length - num173) / length);
            color39.B = (byte)(1f * (double)(int)color39.B * (double)(length - num173) / length);
            color39.A = (byte)(1f * (double)(int)color39.A * (double)(length - num173) / length);
            //color39 *= MathHelper.Clamp(NPC.velocity.Length(), 0f, 9f) / 9f;
            color39 *= 1f - num173 / length;
            //color39 *= _trailOpacity;
            //color39 *= 0.8f;
            color39 *= 1f;
            color39 *= _dashOpacity;
            Rectangle frame7 = NPC.frame;
            Vector2 origin = NPC.frame.Centered();
            Vector2 pos = NPC.oldPos[num173];
            pos += NPC.Size / 2f;

            pos = Vector2.Lerp(pos, NPC.Center, Ease.SineIn(1f - _dashOpacity));

            pos -= screenPos;

            ShaderLoader.DistortShader.SetDefault(texture.Width * 2, texture.Height * 2);
            ShaderLoader.ApplyEffect(ShaderLoader.DistortShader.Effect, spriteBatch, () => {
                spriteBatch.Draw(texture,
                pos,
                frame7, color39 * NPC.Opacity, NPC.rotation, origin, NPC.scale * scale, flip, 0f);
            });
        }

        EternalHorror.DrawContext drawContext = new(spriteBatch, position, texture, NPC.frame, drawColor, 0f, default, screenPos);
        EternalHorror.DrawUnderShadowEffect(drawContext, (newPosition, newColor) => {
            Vector2 center = NPC.Center;
            NPC.Center = newPosition;
            ShaderLoader.DistortShader.SetDefault(texture.Width * 2, texture.Height * 2);
            ShaderLoader.ApplyEffect(ShaderLoader.DistortShader.Effect, spriteBatch, () => {
                NPC.QuickDraw(spriteBatch, screenPos, newColor, texture: texture, effect: flip, scale: scale);
            });
            NPC.Center = center;
        }, sinWaveOffset: WaveOffset,
           progress: timeLeftProgress * 0.25f,
           opacity: 0.375f,
           sinStep: _shadowTime,
           offsetAmount: 32f / MathHelper.Lerp(1f, 4f, scale),
           shadowPositionOffset: (k) => Vector2.UnitY.RotatedBy(targetAngle) * 10f * (k / MathHelper.TwoPi));

        return false;
    }
}
