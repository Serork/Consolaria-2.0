using Consolaria.Common.Particles;
using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;
using static Consolaria.Content.NPCs.Bosses.EternalHorror.EternalHorror;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed partial class EternalHorror : ModNPC {
    private static byte CLONECOUNTAVAILABLE => 3;
    private static ushort CLONEACTIVETIME => Helper.SecondsToFrames(10);

    private static HashSet<CloneInfo> _cloneDataCache = [];

    private partial void Unload_Caches() {
        _cloneDataCache.Clear();
        _cloneDataCache = null;
    }

    public record struct CloneStarInfo(Vector2 Position,
                                       float Progress,
                                       float Rotation = 0f) {
        public readonly bool Active => Progress != 0f;
    }

    public record struct CloneInfo(Vector2 Position, Vector2 TargetPosition, ushort TimeLeft, 
                                                                             ushort MaxTimeLeft, 
                                                                             float Rotation = 0f, 
                                                                             Vector2 VisualPosition = default,
                                                                             Vector2 Velocity = default,
                                                                             bool ShouldUpdateVisualPosition = true,
                                                                             Vector2[] OldVisualPositions = default,
                                                                             float[] OldRotations = default,
                                                                             float DashOpacity = 0f,
                                                                             bool ShouldFade = false,
                                                                             CloneStarInfo[] CloneStarData = null,
                                                                             float AllStarOpacityFactor = 0f,
                                                                             float LerpVelocityValue = 1f) {
        public readonly float TimeLeftProgress => Helper.Clamp01((float)TimeLeft / MaxTimeLeft);
        public readonly bool Active => TimeLeftProgress > 0f;
        public readonly float Opacity {
            get {
                float timeLeftProgress = TimeLeftProgress;
                float opacity = 1f;
                opacity *= 1f - Utils.GetLerpValue(0.75f, 1f, timeLeftProgress, true);
                if (ShouldFade) {
                    opacity *= Utils.GetLerpValue(0f, 0.25f, timeLeftProgress, true);
                }
                return opacity;
            }
        }

        public readonly Vector2 GetFinalClonePosition(Player target) {
            Vector2 targetCenter = target.Center,
                    clonePosition = Position,
                    cloneTargetCenter = TargetPosition;
            Vector2 position = targetCenter;
            position += clonePosition - cloneTargetCenter;
            return position;
        }

        public readonly void AddStar(Vector2 position) {
            if (!Active) {
                return;
            }
            int nextStarIndex = 0;
            for (int i = 0; i < CloneStarData.Length; i++) {
                if (CloneStarData[i].Progress != 0f) {
                    nextStarIndex++;
                }
            }
            CloneStarData[nextStarIndex] = new CloneStarInfo(Position: position - VisualPosition,
                                                             Progress: 1f,
                                                             Rotation: MathHelper.TwoPi * Main.rand.NextFloat());
        }

        public void UpdateStars() {
            float starOpacityExtra = 0f;
            int count = 1;
            for (int i = 0; i < CloneStarData.Length; i++) {
                CloneStarData[i].Progress = Helper.Approach(CloneStarData[i].Progress, 0f, 0.05f);
                if (CloneStarData[i].Active) {
                    starOpacityExtra += 1f - CloneStarData[i].Progress;
                    count++;
                }
            }
            starOpacityExtra /= count;
            starOpacityExtra *= 1.5f;
            AllStarOpacityFactor = Helper.Approach(AllStarOpacityFactor, starOpacityExtra, 0.1f);
        }
    }

    private CloneInfo[] _cloneData = null;
    private Vector2 _dashVelocity;
    private float _forcedRotationOffset;
    private Vector2 _tempPosition;
    private Vector2 _cloneTargetPosition;

    public ref float InitValue => ref NPC.ai[0];

    public ref float AICounter => ref NPC.ai[1];
    public ref float AICounter2 => ref NPC.ai[2];
    public ref float AICounter3 => ref NPC.ai[3];

    public ref float AttackCount => ref NPC.localAI[3];
    public ref float SmoothFactor => ref NPC.localAI[2];
    public ref float Phase1BurstLaserAttackCount => ref NPC.localAI[1];
    public ref float Phase1DashAttackCount => ref NPC.localAI[1];
    public ref float Phase1LaserSpamPreparationSlowDown => ref AICounter3;

    public ref float Phase1CloneSpawn_ShouldDashAfterValue => ref NPC.localAI[0];
    public ref float Phase1DashAttack_ShouldBurstLaserAfterValue => ref NPC.localAI[0];
    public ref float Phase1SpawnSummonAttack_DoneValue => ref NPC.localAI[0];

    public bool Init {
        get => InitValue != 0f;
        set => InitValue = value.ToInt();
    }

    public bool Phase1CloneSpawn_ShouldDashAfter {
        get => Phase1CloneSpawn_ShouldDashAfterValue != 0f;
        set => Phase1CloneSpawn_ShouldDashAfterValue = value.ToInt();
    }

    public bool Phase1DashAttack_ShouldBurstLaserAfter {
        get => Phase1DashAttack_ShouldBurstLaserAfterValue != 0f;
        set => Phase1DashAttack_ShouldBurstLaserAfterValue = value.ToInt();
    }

    public bool Phase1SpawnSummonAttack_Done {
        get => Phase1SpawnSummonAttack_DoneValue != 0f;
        set => Phase1SpawnSummonAttack_DoneValue = value.ToInt();
    }

    public override bool PreAI() => base.PreAI();

    public override void AI() {
        OnSpawn();
        MakeMidnight();
        UpdateStates();
        UpdateClones();
        ForceUpdateRotation();
    }

    public override void PostAI() {
        UpdateVisuals();
    }

    private void OnSpawn() {
        if (Init) {
            return;
        }

        Init = true;

        ResetPhase1LaserAttack(applyIncreasedDelay: true);

        TargetPlayer();

        SpawnFromAbove();

        InitializeClones();

        InitializeStates();

        ActivateState<MoveToPlayer>();
        ActivateState<Phase1BurstLaserAttack>();
    }

    private void InitializeClones() {
        _cloneData = new CloneInfo[CLONECOUNTAVAILABLE];
    }

    private Vector2 GetCloneSpawnPosition() {
        Player target = NPC.GetTargetPlayer();
        Vector2 npcCenter = NPC.Center,
        targetCenter = target.Center;
        Vector2 clonePosition = targetCenter + (targetCenter - npcCenter);
        float offsetFromBoss = 650f;
        Vector2 result = npcCenter + npcCenter.DirectionTo(clonePosition) * offsetFromBoss;
        int attempts = 30;
        while (attempts-- > 0 && result.Distance(targetCenter) < offsetFromBoss / 2f) {
            result += result.DirectionFrom(targetCenter) * 10f;
        }
        return result;
    }

    private void SpawnClone() {
        int nextCloneAddedIndex = 0;
        OnIterateActiveCloneData((ref cloneInfo) => nextCloneAddedIndex++);
        if (nextCloneAddedIndex >= CLONECOUNTAVAILABLE) {
            return;
        }
        if (!NPC.HasPlayerTarget) {
            return;
        }
        Player target = NPC.GetTargetPlayer();
        Vector2 targetCenter = target.Center;
        Vector2 clonePosition = GetCloneSpawnPosition();
        ushort cloneActiveTime = CLONEACTIVETIME;
        _cloneData[nextCloneAddedIndex] = new CloneInfo(Position: clonePosition,
                                                        TargetPosition: targetCenter,
                                                        TimeLeft: cloneActiveTime,
                                                        MaxTimeLeft: cloneActiveTime,
                                                        VisualPosition: NPC.Center,
                                                        Rotation: NPC.rotation,
                                                        Velocity: default,
                                                        ShouldUpdateVisualPosition: false,
                                                        OldVisualPositions: new Vector2[NPC.oldPos.Length],
                                                        OldRotations: new float[NPC.oldRot.Length],
                                                        DashOpacity: 0f,
                                                        CloneStarData: new CloneStarInfo[100]);
    }

    private partial void InitializeStates();

    private void MakeMidnight() {
        float expFactor = 0.025f;
        if (Main.dayTime) {
            expFactor *= 4;
            float to = (float)Main.dayLength;
            float lerpValue = 1f - MathF.Exp(-expFactor);
            Main.time = MathHelper.Lerp((float)Main.time, to, lerpValue);
        }
        else {
            float to = (float)Main.nightLength / 2;
            float lerpValue = 1f - MathF.Exp(-expFactor);
            Main.time = MathHelper.Lerp((float)Main.time, to, lerpValue);
        }

        //Main.raining = true;
        //Main.cloudAlpha = 0.5f;
        //Main.lightning = 0f;
    }

    private void UpdateStates() {
        IAIState[] states = [.. _activeStates];
        foreach (IAIState activeState in states) {
            activeState.OnActiveUpdate(npc: NPC, boss: Self);
        }
    }

    private void UpdateClones() {
        for (int i = 0; i < _cloneData.Length; i++) {
            ref CloneInfo cloneInfo = ref _cloneData[i];
            if (cloneInfo.Active) {
                if (cloneInfo.TimeLeft > CLONEACTIVETIME * 0.25f || cloneInfo.ShouldFade) {
                    cloneInfo.TimeLeft--;
                }
            }
            else {
                continue;
            }

            Player target = NPC.GetTargetPlayer();

            for (int num7 = cloneInfo.OldVisualPositions.Length - 1; num7 > 0; num7--) {
                cloneInfo.OldVisualPositions[num7] = cloneInfo.OldVisualPositions[num7 - 1];
                cloneInfo.OldRotations[num7] = cloneInfo.OldRotations[num7 - 1];
            }

            cloneInfo.OldVisualPositions[0] = cloneInfo.VisualPosition;
            cloneInfo.OldRotations[0] = cloneInfo.Rotation;

            //if (!HasActiveState<Phase1DashAttack>() || Phase1LastDash) {
            //    if (!cloneInfo.ShouldUpdateVisualPosition) {
            //        cloneInfo.VisualPosition = Vector2.Lerp(cloneInfo.VisualPosition, cloneInfo.GetFinalClonePosition(target), 0.125f * cloneInfo.LerpVelocityValue);
            //    }
            //    else {
            //        cloneInfo.Velocity *= 0.98f;
            //    }
            //}
            //else {
            //    cloneInfo.ShouldUpdateVisualPosition = true;
            //}
            if (!cloneInfo.ShouldUpdateVisualPosition) {
                float lerpValueFactor = cloneInfo.LerpVelocityValue;
                cloneInfo.VisualPosition = Vector2.Lerp(cloneInfo.VisualPosition, cloneInfo.GetFinalClonePosition(target),
                    0.125f * lerpValueFactor);
            }
            else {
                //cloneInfo.Velocity *= 0.98f;
            }
            cloneInfo.VisualPosition += cloneInfo.Velocity;

            cloneInfo.UpdateStars();

            if (Main.rand.NextChance(cloneInfo.Velocity.Length() / Phase1DashAttack.DASHSTRENGTH) && Main.rand.NextBool(1)) {
                Color colorTint = MainPurpleColor * 0.5f;

                Vector2 position = cloneInfo.VisualPosition 
                    + Vector2.One.RotatedBy(cloneInfo.Rotation) * new Vector2(NPC.width, NPC.height) * new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-0.5f, 0.5f));
                Vector2 velocity = cloneInfo.Velocity;
                position += velocity;
                velocity *= 0.2f;
                FadingParticle fadingParticle = ParticlePools.FadingParticlePool.RequestParticle();
                fadingParticle.SetBasicInfo(TextureAssets.Star[0], null, velocity, position);
                float num = 25f/* * Main.rand.NextFloat(0.5f, 1f)*/;
                fadingParticle.SetTypeInfo(num);
                fadingParticle.AccelerationPerFrame = velocity / num;
                fadingParticle.ColorTint = colorTint;
                fadingParticle.FadeInNormalizedTime = 0.5f;
                fadingParticle.FadeOutNormalizedTime = 0.5f;
                fadingParticle.Rotation = Main.rand.NextFloat() * ((float)Math.PI * 2f);
                fadingParticle.Scale = Vector2.One * (0.5f + 0.5f * Main.rand.NextFloat());
                Main.ParticleSystem_World_OverPlayers.Add(fadingParticle);
                FadingParticle fadingParticle2 = fadingParticle;
                fadingParticle = ParticlePools.FadingParticlePool.RequestParticle();
                fadingParticle.SetBasicInfo(TextureAssets.Star[0], null, velocity, position);
                fadingParticle.SetTypeInfo(num);
                fadingParticle.AccelerationPerFrame = velocity / num;
                fadingParticle.ColorTint = colorTint;
                fadingParticle.ColorTint.A = 30;
                fadingParticle.FadeInNormalizedTime = 0.5f;
                fadingParticle.FadeOutNormalizedTime = 0.5f;
                fadingParticle.Rotation = fadingParticle2.Rotation;
                fadingParticle.Scale = fadingParticle2.Scale * 0.5f;
                Main.ParticleSystem_World_OverPlayers.Add(fadingParticle);
            }

            //if (!cloneInfo.ShouldUpdateVisualPosition)
            {
                Vector2 targetCenter = _cloneTargetPosition,
                        clonePosition = cloneInfo.VisualPosition;
                float angleToTarget = clonePosition.AngleTo(targetCenter) - MathHelper.PiOver2;
                cloneInfo.Rotation = cloneInfo.Rotation.AngleLerp(angleToTarget, ROTATIONLERP * 0.5f);
            }
        }
    }

    public HashSet<CloneInfo> GetActiveCloneData() {
        _cloneDataCache.Clear();
        HashSet<CloneInfo> clonePositions = _cloneDataCache;
        if (!Init) {
            return clonePositions;
        }
        foreach (CloneInfo cloneInfo in _cloneData) {
            if (!cloneInfo.Active) {
                continue;
            }

            clonePositions.Add(cloneInfo);
        }

        return clonePositions;
    }

    private delegate void RefAction<T>(ref T value);
    private void OnIterateActiveCloneData(RefAction<CloneInfo> actionWithClone) {
        for (int i = 0; i < _cloneData.Length; i++) {
            ref CloneInfo cloneInfo = ref _cloneData[i];
            if (cloneInfo.Active) {
                actionWithClone(ref cloneInfo);
            }
        }
    }

    private void TargetPlayer(bool faceTarget = false) {
        if (NPC.ShouldTargetPlayer()) {
            NPC.TargetClosest(faceTarget: faceTarget);
        }
    }

    private void SpawnFromAbove() {
        Vector2 spawnOffset = new(0f, -850f);
        NPC.Center = NPC.GetTargetPlayer().Center + spawnOffset;
    }

    private void ResetPhase1LaserAttack(bool applyIncreasedDelay = false,
                                        bool applyExtraIncreasedDelay = false) {
        if (applyIncreasedDelay) {
            AICounter = -(int)(Phase1BurstLaserAttack.LASERATTACKTIME / 1f);
            if (applyExtraIncreasedDelay) {
                AICounter *= 2f;
            }
            return;
        }
        AICounter = -(int)(Phase1BurstLaserAttack.LASERATTACKTIME / 2f);
    }

    private void ResetCounters() {
        AttackCount = 0;
        AICounter = 0f;
        AICounter2 = 0f;
        AICounter3 = 0f;
    }

    private void ShootLaser(bool shootBasedOnRotation = true, float angleShiftToPlayer = 0f) {
        if (!NPC.HasPlayerTarget) {
            return;
        }

        const float Speed = 12f;
        Player target = NPC.GetTargetPlayer();
        Vector2 vector8 = NPC.Center;
        SoundEngine.PlaySound(SoundID.Item33, vector8);
        float rotation = NPC.rotation + MathHelper.PiOver2;
        if (shootBasedOnRotation) {
            rotation = vector8.AngleTo(target.Center + target.velocity * Speed / 2f);
        }
        rotation += angleShiftToPlayer;
        float speedX = MathF.Cos(rotation) * Speed;
        float speedY = MathF.Sin(rotation) * Speed;

        Vector2 center = vector8;
        Vector2 velocity = new(speedX, speedY);

        int num5 = Main.rand.Next(20, 40);
        Vector2 positionInWorld = center;
        Vector2 movementVector = velocity * Main.rand.NextFloatDirection();
        PrettySparkleParticle prettySparkleParticle = ParticlePools.PrettySparkleParticlePool.RequestParticle();
        prettySparkleParticle.ColorTint = EternalHorror.MainRedColor_Dynamic.ModifyRGB(Main.rand.NextFloat(0.5f, 1f)) with { A = 20 };
        prettySparkleParticle.LocalPosition = positionInWorld;
        prettySparkleParticle.Rotation = (float)Math.PI / 2f;
        prettySparkleParticle.Scale = new Vector2(8f, 0.4f);
        prettySparkleParticle.FadeInNormalizedTime = 0.1f;
        prettySparkleParticle.FadeOutNormalizedTime = 0.5f;
        prettySparkleParticle.TimeToLive = num5;
        prettySparkleParticle.FadeOutEnd = num5;
        prettySparkleParticle.FadeInEnd = num5 / 2;
        prettySparkleParticle.FadeOutStart = num5 / 2;
        prettySparkleParticle.AdditiveAmount = 0.35f;
        prettySparkleParticle.Velocity = movementVector;
        prettySparkleParticle.Velocity *= 0.3f;
        Main.ParticleSystem_World_OverPlayers.Add(prettySparkleParticle);

        if (Helper.IsClient()) {
            return;
        }
        Projectile.NewProjectile(NPC.GetSource_FromAI(), vector8.X, vector8.Y, speedX, speedY, ModContent.ProjectileType<EternalHorrorLaser1>(),
            27, 1.5f, Main.myPlayer);
    }

    private void ForceUpdateRotation() {
        NPC.rotation += _forcedRotationOffset;
        _forcedRotationOffset = _forcedRotationOffset.AngleLerp(0f, 0.75f);
    }

    private void ResetSmoothFactor(float value = 0f) {
        SmoothFactor = value;
    }

    private void DestroyClonesOnContact() {
        OnIterateActiveCloneData((ref cloneInfo) => {
            Rectangle hitbox = NPC.Hitbox;
            Vector2 clonePosition = cloneInfo.VisualPosition;
            float cloneRotation = cloneInfo.Rotation;
            Vector2 cloneDirection = Vector2.UnitY.RotatedBy(cloneRotation);
            Vector2 clonePosition_Start = clonePosition + cloneDirection * NPC.height / 2f,
                    clonePosition_End = clonePosition + -cloneDirection * NPC.height / 2f;
            float collisionPoint = 0f;
            if (Collision.CheckAABBvLineCollision(hitbox.Location.ToVector2(), hitbox.Size(), clonePosition_Start, clonePosition_End, NPC.width, ref collisionPoint)) {
                cloneInfo.TimeLeft = 0;

                Vector2 position = Vector2.Lerp(clonePosition_Start, clonePosition_End, 0.5f);
                SpawnCloneExplosion(position);
            }
        });
    }

    private void SpawnCloneExplosion(Vector2 position) {
        if (!Helper.IsClient()) {
            IEntitySource source = NPC.GetSource_FromAI();
            Vector2 velocity = Vector2.Zero;
            int type = ModContent.ProjectileType<EternalHorrorCloneExplosion>();
            int damage = 30;
            float knockBack = 5f;
            Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockBack);
        }
    }
}
