using Consolaria.Content.NPCs.Bosses.Ocram;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed partial class EternalHorror : ModNPC {
    private static float ROTATIONLERP => 0.5f;

    private interface IAIState {
        public void OnActiveUpdate(NPC npc, EternalHorror boss);

        public void OnStart(NPC npc, EternalHorror boss) { }
        public void OnEnd(NPC npc, EternalHorror boss) { }
    }

    private readonly struct MoveToPlayer : IAIState {
        void IAIState.OnActiveUpdate(NPC npc, EternalHorror boss) {
            boss.TargetPlayer();
            npc.FaceTarget_New();

            bool shouldDash = boss.HasActiveState<Phase1DashAttack>(),
                 shouldSpamLasers = boss.HasActiveState<Phase1LaserSpamAttack>(),
                 shouldSpawnSummons = boss.HasActiveState<Phase1SummonSpawnAttack>();

            Player target = npc.GetTargetPlayer();
            Vector2 targetCenter = target.Center,
                    baseTargetCenter = targetCenter;

            boss._cloneTargetPosition = targetCenter;

            const int MinDistanceToTargetInPixels = 300;

            void smoothEverything() {
                if (shouldDash) {
                    return;
                }
                boss.SmoothFactor = Helper.Approach(boss.SmoothFactor, 1f, 0.025f);
            }
            void lookAtTarget() {
                float angleToTarget = npc.AngleTo(baseTargetCenter) - MathHelper.PiOver2;
                if (shouldDash) {
                    return;
                }
                void setRotation(float smoothFactor = 1f) {
                    npc.rotation = npc.rotation.AngleLerp(angleToTarget, ROTATIONLERP * boss.SmoothFactor * smoothFactor);
                }
                if (shouldSpamLasers) {
                    float angleFactor = boss.Phase1LaserSpamPreparationSlowDown;
                    angleFactor = Utils.Remap(angleFactor, Phase1LaserSpamAttack.PPEPARATIONSLOWLASTFACTOR, 1f, 0f, 1f, true);
                    setRotation(angleFactor);

                    return;
                }
                setRotation();
            }
            void makeTargetPositionABitHigher() {
                if (shouldDash) {
                    return;
                }
                targetCenter.Y -= 100f;
            }
            void moveToTarget() {
                const float Speed = 15f,
                            Inertia = 20f;
                const float Deceleration = 0.99f;
                npc.MoveToWithDeceleration(targetCenter, Speed * boss.SmoothFactor, Inertia * boss.SmoothFactor, MinDistanceToTargetInPixels, Deceleration);

                Vector2 targetPosition = Vector2.Zero.MoveTowards(targetCenter - npc.Center, 4f * boss.SmoothFactor);
                npc.velocity = npc.velocity.MoveTowards(targetPosition, 2f / 15f * boss.SmoothFactor);
            }
            void moveFromTargetIfClose() {
                if (shouldDash) {
                    return;
                }
                float distance = npc.Distance(targetCenter);
                float minDistance = MinDistanceToTargetInPixels / 2f;
                if (distance < minDistance) {
                    npc.velocity += npc.DirectionFrom(targetCenter) * (0.25f + 0.75f * Helper.Clamp01(1f - distance / minDistance)) * boss.SmoothFactor;
                }
                else {
                    if (npc.velocity.Length() < 1f) {
                        npc.velocity *= 0.95f;
                    }
                }
            }
            void moveUpwardsIfClose() {
                if (shouldDash) {
                    return;
                }
                if (npc.Center.Y > targetCenter.Y) {
                    npc.velocity -= Vector2.UnitY * 0.5f * boss.SmoothFactor;
                }
            }
            void slowDownWhenCloseToTarget() {
                if (shouldDash) {
                    return;
                }
                npc.velocity *= Helper.Clamp01(npc.Distance(targetCenter) / (MinDistanceToTargetInPixels / 5f));
            }

            smoothEverything();
            lookAtTarget();
            makeTargetPositionABitHigher();
            moveToTarget();
            moveFromTargetIfClose();
            moveUpwardsIfClose();
            slowDownWhenCloseToTarget();
        }

        void IAIState.OnStart(NPC npc, EternalHorror boss) { }
        void IAIState.OnEnd(NPC npc, EternalHorror boss) { }
    }

    private readonly struct Phase1BurstLaserAttack : IAIState {
        public static float LASERATTACKTIME => Helper.SecondsToFrames(1);
        public static byte LASERATTACKCOUNT => 3;

        void IAIState.OnActiveUpdate(NPC npc, EternalHorror boss) {
            Player target = npc.GetTargetPlayer();
            Vector2 targetCenter = target.Center;

            boss._cloneTargetPosition = targetCenter;

            float laserProgress = boss.Phase1BurstLaserAttackProgress;
            float laserProgress_ForLaserRotation = laserProgress * Utils.GetLerpValue(1f, 0.5f, laserProgress, true);

            void shootLasers() {
                if ((boss.AICounter < 0f && laserProgress > 0f) || boss.AICounter >= LASERATTACKTIME * 0.5f) {
                    if (boss.AICounter % 2 == 0) {
                        boss.ShootLaser(shootBasedOnRotation: true);
                    }
                }
            }
            void prepareLasers() {
                ref float laserRotation = ref boss.Phase1LaserRotation;
                float newLaserRotation = npc.AngleTo(targetCenter);
                laserRotation = Utils.AngleLerp(laserRotation, newLaserRotation, laserProgress_ForLaserRotation);

                bool shotLaser = ++boss.AICounter >= LASERATTACKTIME;
                if (!shotLaser) {
                    return;
                }

                boss.ResetPhase1LaserAttack();

                boss.Phase1BurstLaserAttackCount++;

                bool shotLasers = ++boss.AttackCount >= LASERATTACKCOUNT;
                if (!shotLasers) {
                    return;
                }

                boss.ResetCounters();
                boss.DeactivateState<Phase1BurstLaserAttack>();

                bool shouldDash = boss.Phase1BurstLaserAttackCount > Phase1DashAttack.LASERATTACKCOUNTNEEDED;
                if (shouldDash) {
                    boss.Phase1BurstLaserAttackCount = 0;
                    boss.ActivateState<Phase1DashAttack>();

                    return;
                }

                boss.ActivateState<Phase1CloneSpawn>();
            }

            prepareLasers();
            shootLasers();
        }

        void IAIState.OnStart(NPC npc, EternalHorror boss) { }
        void IAIState.OnEnd(NPC npc, EternalHorror boss) { }
    }

    private readonly struct Phase1CloneSpawn : IAIState {
        public static float SHADOWSPAWNTIME => Helper.SecondsToFrames(1);

        void IAIState.OnActiveUpdate(NPC npc, EternalHorror boss) {
            void prepareAndSpawnClone() {
                bool justStarted = boss.AICounter == 0f;
                if (justStarted) {
                    boss.SpawnClone();
                }

                bool shadowSpawnProgress = ++boss.AICounter >= SHADOWSPAWNTIME;
                if (shadowSpawnProgress) {
                    boss.ResetCounters();

                    if (boss.Phase1CloneSpawn_ShouldDashAfter) {
                        boss.Phase1CloneSpawn_ShouldDashAfter = false;
                        boss.ActivateState<Phase1DashAttack>();
                        boss.Phase1DashAttack_ShouldBurstLaserAfter = true;
                    }
                    else {
                        boss.ActivateState<Phase1BurstLaserAttack>();
                    }
                    boss.DeactivateState<Phase1CloneSpawn>();
                }
            }

            prepareAndSpawnClone();
        }

        void IAIState.OnStart(NPC npc, EternalHorror boss) { }
        void IAIState.OnEnd(NPC npc, EternalHorror boss) { }
    }

    private readonly struct Phase1DashAttack : IAIState {
        public static float DASHTIME => Helper.SecondsToFrames(0.75f);
        public static float LASERATTACKCOUNTNEEDED => 5;
        public static float DASHATTACKCOUNT => 5;

        public static SoundStyle DashSound => SoundID.Roar with { PitchVariance = 0.15f, MaxInstances = 0 };

        void IAIState.OnActiveUpdate(NPC npc, EternalHorror boss) {
            Player target = npc.GetTargetPlayer();
            Vector2 targetCenter = target.Center;

            bool lastDash = boss.Phase1LastDash;

            float dashProgress = boss.AICounter / DASHTIME;

            if (lastDash) {
                boss.OnIterateActiveCloneData((ref cloneInfo) => {
                    cloneInfo.LerpVelocityValue = Helper.Approach(cloneInfo.LerpVelocityValue, 1f, 0.05f);

                    cloneInfo.DashOpacity = Helper.Approach(cloneInfo.DashOpacity, 0f, 0.1f);
                });
                foreach (CloneInfo cloneInfo in boss._cloneData) {
                    if (cloneInfo.Active) {
                        targetCenter = Vector2.Lerp(npc.Center, cloneInfo.VisualPosition, 0.875f);
                        break;
                    }
                }
            }
            else {
                boss.OnIterateActiveCloneData((ref cloneInfo) => {
                    cloneInfo.LerpVelocityValue = Helper.Approach(cloneInfo.LerpVelocityValue, 0f, 0.05f);
                });
            }

            boss._cloneTargetPosition = targetCenter;

            Vector2 baseTargetCenter = targetCenter;

            targetCenter += targetCenter.DirectionTo(npc.Center) * 10f;

            float dashStrength = 40f;

            _shakeStrength = Helper.Approach(_shakeStrength, dashProgress, 0.125f);

            bool didAtLeastOneDash = boss.Phase1DashAttackCount > 0;

            float dashPreparationFactor = 0.25f;

            bool shouldResetState() {
                if (boss.Phase1DashAttackCount >= DASHATTACKCOUNT) {
                    boss.ActivateState<MoveToPlayer>();
                    boss.DeactivateState<Phase1DashAttack>();
                    if (boss.Phase1DashAttack_ShouldBurstLaserAfter) {
                        boss.Phase1DashAttack_ShouldBurstLaserAfter = false;
                        boss.ActivateState<Phase1SummonSpawnAttack>();
                    }
                    else {
                        boss.ActivateState<Phase1LaserSpamAttack>();
                    }
                    boss.ResetCounters();

                    boss.ResetSmoothFactor(value: 0.25f);
                    boss.Phase1DashAttackCount = 0;

                    boss._dashVelocity *= 0f;

                    boss.OnIterateActiveCloneData((ref cloneInfo) => {
                        //cloneInfo.ShouldUpdateVisualPosition = true;
                        cloneInfo.ShouldFade = true;
                    });

                    return true;
                }

                return false;
            }
            void lookAtTarget() {
                if (!boss.HasActiveState<MoveToPlayer>()) {
                    boss.SmoothFactor = Helper.Approach(boss.SmoothFactor, dashProgress, 0.025f);
                }
                float angleToTarget = npc.AngleTo(baseTargetCenter) - MathHelper.PiOver2;
                npc.rotation = npc.rotation.AngleLerp(angleToTarget, ROTATIONLERP * boss.SmoothFactor);
            }
            void prepareDash() {
                boss.AICounter += 1f;
                if (!didAtLeastOneDash) {
                    boss.AICounter -= 0.25f;
                }
                bool shouldDash = boss.AICounter >= DASHTIME;

                boss._copiesIntensity = dashProgress;

                if (shouldDash) {
                    if (shouldResetState()) {
                        return;
                    }

                    npc.ResetTrails();

                    SoundEngine.PlaySound(DashSound, npc.Center);

                    boss.AICounter = -DASHTIME;

                    boss.DeactivateState<MoveToPlayer>();

                    Vector2 dashDirection = npc.DirectionTo(targetCenter);
                    boss._dashVelocity = dashDirection * dashStrength;

                    if (!lastDash) {
                        boss.OnIterateActiveCloneData((ref cloneInfo) => {
                            Vector2 clonePosition = cloneInfo.VisualPosition;
                            dashDirection = clonePosition.DirectionTo(targetCenter);
                            cloneInfo.Velocity = dashDirection * dashStrength;
                        });
                    }

                    boss.Phase1DashAttackCount++;

                    boss.ResetSmoothFactor();
                }
                else if (!didAtLeastOneDash) {
                    float smoothFactor = dashProgress;
                    dashProgress *= 1f - Utils.GetLerpValue(1f - dashPreparationFactor, 1f, smoothFactor, true);
                    npc.velocity -= npc.DirectionTo(targetCenter) * 1f * dashProgress;
                }
            }
            void slowDown() {
                float velocityDeceleration = 0.98f;
                boss._dashVelocity *= velocityDeceleration;
                float dashOpacity = boss._dashVelocity.Length() / dashStrength;
                if (!lastDash) {
                    boss.OnIterateActiveCloneData((ref cloneInfo) => {
                        cloneInfo.Velocity *= velocityDeceleration;
                        cloneInfo.DashOpacity = Helper.Approach(cloneInfo.DashOpacity, dashOpacity, 1f);
                    });
                }
                boss._dashOpacity = Helper.Approach(boss._dashOpacity, dashOpacity, 1f);

            }
            bool shouldSlowDownAfterDash() {
                bool preparingDash = boss.AICounter < 0f;
                if (preparingDash) {
                    return true;
                }

                return false;
            }
            void extraSlowDownAfterDash() {
                slowDown();

                if (didAtLeastOneDash) {
                    float smoothFactor = boss.SmoothFactor;
                    smoothFactor *= 1f - Utils.GetLerpValue(1f - dashPreparationFactor, 1f, smoothFactor, true);
                    npc.velocity += npc.DirectionTo(targetCenter) * 1f * smoothFactor;
                }
            }
            void prepareSelfAfterDash() {
                boss._copiesIntensity = 0f;
                if (didAtLeastOneDash) {
                    float lerpValue = 0.25f;
                    npc.velocity = Vector2.Lerp(npc.velocity, boss._dashVelocity, lerpValue);
                    float velocityRotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                    npc.rotation = npc.rotation.AngleLerp(velocityRotation, lerpValue * 0.5f);
                }
            }
            void destroyClones() {
                bool shouldDestroyClones = npc.velocity.Length() > dashStrength * MathHelper.Lerp(0.5f, 0.875f, 0.5f);
                if (lastDash && shouldDestroyClones) {
                    boss.DestroyClonesOnContact();
                }
            }

            destroyClones();

            prepareDash();

            if (shouldSlowDownAfterDash()) {
                prepareSelfAfterDash();
                slowDown();
                return;
            }

            lookAtTarget();
            extraSlowDownAfterDash();
        }

        void IAIState.OnStart(NPC npc, EternalHorror boss) { }
        void IAIState.OnEnd(NPC npc, EternalHorror boss) { }
    }

    private readonly struct Phase1LaserSpamAttack : IAIState {
        public static float BEFORESPAMTIME => Helper.SecondsToFrames(1.5f);
        public static float SPAMATTACKTIME => Helper.SecondsToFrames(2f);

        public static float PPEPARATIONSLOWLASTFACTOR => 0.75f;

        void IAIState.OnActiveUpdate(NPC npc, EternalHorror boss) {
            float preparationProgress = boss.Phase1LaserSpamAttackProgress;
            bool shouldStartAttack = boss.AICounter >= BEFORESPAMTIME;
            float attackTime_ForSlowDown = SPAMATTACKTIME,
                  attackProgress_ForSlowDown = (boss.AICounter - BEFORESPAMTIME) / attackTime_ForSlowDown;
            attackProgress_ForSlowDown = Helper.Clamp01(attackProgress_ForSlowDown);
            float preparationSlowDown = Utils.Remap(1f - attackProgress_ForSlowDown, 0f, 1f, PPEPARATIONSLOWLASTFACTOR, 1f, true);

            Player target = npc.GetTargetPlayer();
            Vector2 targetCenter = target.Center;

            boss._cloneTargetPosition = targetCenter;

            void prepareLasers() {
                boss.AICounter += 1f;
                boss.Phase1LaserSpamPreparationSlowDown = preparationSlowDown;
            }
            void spamLasers() {
                if (shouldStartAttack) {
                    float laserAttackFireRate = Utils.Remap(attackProgress_ForSlowDown, 0f, 1f, 0.25f, 1f, true);
                    boss.AICounter2 += laserAttackFireRate;
                    float rotationOffset = MathHelper.PiOver4 * 0.5f * laserAttackFireRate;
                    if (boss.AICounter2 >= 1f) {
                        float waveStep = attackProgress_ForSlowDown * 10f;
                        if (npc.IsFacingLeft()) {
                            waveStep += MathHelper.TwoPi;
                        }
                        float randomAngleShift = Helper.Wave(waveStep, -1f, 1f, 2.5f, boss.WaveOffset) * rotationOffset;
                        randomAngleShift *= Main.rand.NextFloat(0.75f, 1.25f);
                        float forcedRotationOffset = randomAngleShift * 0.25f;
                        forcedRotationOffset *= 1f - Utils.GetLerpValue(0.75f, 1f, attackProgress_ForSlowDown, true);
                        boss._forcedRotationOffset += forcedRotationOffset;
                        boss.ShootLaser(shootBasedOnRotation: false, angleShiftToPlayer: randomAngleShift);
                        boss.AICounter2 = 0f;
                    }
                }
            }
            void stopAttacking() {
                bool shouldStopAttack = boss.AICounter >= SPAMATTACKTIME + BEFORESPAMTIME;
                if (shouldStopAttack) {
                    boss.ResetCounters();
                    boss.ResetSmoothFactor();
                    boss.DeactivateState<Phase1LaserSpamAttack>();
                    boss.ActivateState<Phase1CloneSpawn>();

                    boss.Phase1CloneSpawn_ShouldDashAfter = true;
                }
            }
            void slowDownWhenShooting() {
                npc.velocity *= preparationSlowDown;
            }

            prepareLasers();
            spamLasers();
            stopAttacking();
            slowDownWhenShooting();
        }

        void IAIState.OnStart(NPC npc, EternalHorror boss) { }
        void IAIState.OnEnd(NPC npc, EternalHorror boss) { }
    }

    private readonly struct Phase1SummonSpawnAttack : IAIState {
        public static float MOVETOPLAYERTIME => Helper.SecondsToFrames(1f);
        public static float SLOWDOWNTIME => Helper.SecondsToFrames(1f);
        public static float ATTACKTIME => Helper.SecondsToFrames(1.5f);

        public static SoundStyle SummonSpawnSound => SoundID.NPCDeath45;

        // the mess begins
        // sometimes i just wanna make stuff in game
        // not writing a ton of code
        void IAIState.OnActiveUpdate(NPC npc, EternalHorror boss) {
            Player target = npc.GetTargetPlayer();
            Vector2 targetCenter = target.Center,
                    baseTargetCenter = targetCenter;

            boss._cloneTargetPosition = targetCenter;

            boss.AICounter += 1f;

            const float WaveMovementSpeed = 30f;

            float delayTime = -MOVETOPLAYERTIME / 4f;

            if (boss.AICounter < 0f) {
                boss.Phase1SpawnSummonAttack_Done = true;
            }

            if (boss.Phase1SpawnSummonAttack_Done) {
                npc.velocity *= MathHelper.Lerp(0.95f, 0.98f, 0f);

                if (boss.AICounter > delayTime / 2f) {
                    boss.SmoothFactor = Helper.Approach(boss.SmoothFactor, 1f, 0.025f);
                    lookAtTarget();
                }

                if (boss.SmoothFactor >= 1f) {
                    boss.Phase1SpawnSummonAttack_Done = false;

                    boss.ResetCounters();
                    boss.ResetSmoothFactor();

                    boss.ResetPhase1LaserAttack(applyIncreasedDelay: true, applyExtraIncreasedDelay: true);

                    boss.ActivateState<MoveToPlayer>();
                    boss.DeactivateState<Phase1SummonSpawnAttack>();
                    boss.ActivateState<Phase1BurstLaserAttack>();
                }

                return;
            }

            float preparationProgress = boss.AICounter / MOVETOPLAYERTIME;
            bool shouldStopPreparing = boss.AICounter >= MOVETOPLAYERTIME;
            if (!shouldStopPreparing) {
                float preparationSlowDown = 1f - preparationProgress;
                preparationSlowDown *= 1f - Utils.GetLerpValue(0.5f, 1f, preparationSlowDown, true);

                //npc.velocity *= preparationSlowDown;

                //boss.ResetSmoothFactor(preparationSlowDown);

                boss.ActivateState<MoveToPlayer>();

                return;
            }

            boss.DeactivateState<MoveToPlayer>();

            void lookAtTarget() {
                float angleToTarget = npc.AngleTo(baseTargetCenter) - MathHelper.PiOver2;
                void setRotation(float smoothFactor = 1f) {
                    npc.rotation = npc.rotation.AngleLerp(angleToTarget, ROTATIONLERP * boss.SmoothFactor * smoothFactor);
                }
                setRotation();
            }

            float preparationProgress2 = (boss.AICounter - MOVETOPLAYERTIME) / SLOWDOWNTIME;

            if (preparationProgress2 >= 1f) {
                float attackProgress = (boss.AICounter - MOVETOPLAYERTIME - SLOWDOWNTIME) / ATTACKTIME;
                attackProgress = Ease.SineOut(attackProgress);

                if (boss._tempPosition == default) {
                    boss._tempPosition = targetCenter + npc.DirectionTo(targetCenter).RotatedBy(-MathHelper.PiOver2 * 0.25f) * 3000f;
                }

                float lerpValue = ROTATIONLERP * attackProgress;
                float smoothFactor = MathHelper.Lerp(0.125f, 0.25f, 1f);
                float smoothFactor_End = 1f - Utils.GetLerpValue(1f - smoothFactor, 1f, attackProgress, true);
                //lerpValue *= 1f - Utils.GetLerpValue(1f - smoothFactor, 1f, attackProgress, true);
                lerpValue *= Utils.GetLerpValue(0f, smoothFactor, attackProgress, true);
                lerpValue *= 2f;

                float dashOpacity = 0.5f;
                boss._dashOpacity = Helper.Approach(boss._dashOpacity, dashOpacity, smoothFactor * 0.25f);
                boss.OnIterateActiveCloneData((ref cloneInfo) => {
                    cloneInfo.DashOpacity = Helper.Approach(cloneInfo.DashOpacity, dashOpacity, smoothFactor * 0.25f);
                });

                Vector2 destination = boss._tempPosition;
                Vector2 velocity = npc.DirectionTo(destination);
                float wave = MathF.Sin(attackProgress * 60f * 0.15f + npc.IsFacingLeft().ToInt() * MathHelper.Pi);
                npc.velocity = Vector2.Lerp(npc.velocity, velocity.RotatedBy(wave) * WaveMovementSpeed, lerpValue);
                npc.rotation = npc.rotation.AngleLerp(npc.velocity.ToRotation() - MathHelper.PiOver2, lerpValue);

                if (attackProgress >= 1f) {
                    boss.ResetCounters();
                    boss.ResetSmoothFactor();

                    boss.AICounter = delayTime;
                }

                if (boss._dashOpacity >= dashOpacity / 4f && boss.AICounter % 6 == 0) {
                    SoundEngine.PlaySound(SummonSpawnSound, npc.Center);
                    if (!Helper.IsClient()) {
                        for (int i = 0; i < 1; i++) {
                            Vector2 spawnPosition = npc.Center + Main.rand.NextVector2Circular(npc.width, npc.height) * 0f;
                            int servantIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<EternalServant>());
                            Main.npc[servantIndex].velocity += npc.velocity * 0.75f;
                        }
                    }
                }
            }
            else {
                npc.ResetTrails();

                boss._copiesIntensity = preparationProgress2;

                _shakeStrength = Helper.Approach(_shakeStrength, preparationProgress2, 0.125f);

                lookAtTarget();

                boss._tempPosition = default;

                npc.velocity *= 1f - 0.05f * preparationProgress2;
            }
        }

        void IAIState.OnStart(NPC npc, EternalHorror boss) { }
        void IAIState.OnEnd(NPC npc, EternalHorror boss) { }
    }

    private Dictionary<Type, IAIState> _states = null;
    private HashSet<IAIState> _activeStates = null;

    public ref float Phase1LaserRotation => ref NPC.ai[2];

    public float Phase1BurstLaserAttackProgress => Helper.Clamp01(AICounter / Phase1BurstLaserAttack.LASERATTACKTIME);
    public float Phase1ShadowSpawnProgress => Helper.Clamp01(AICounter / Phase1CloneSpawn.SHADOWSPAWNTIME);
    public float Phase1LaserSpamAttackProgress => Ease.SineIn(Helper.Clamp01(AICounter / Phase1LaserSpamAttack.BEFORESPAMTIME));
    public bool Phase1LastDash => Phase1DashAttackCount >= Phase1DashAttack.DASHATTACKCOUNT - 1;

    private partial void InitializeStates() {
        _states = [];
        _activeStates = [];
    }

    private void AddState<T>() where T : struct, IAIState => _states.TryAdd(typeof(T), new T());

    private void ActivateState<T>() where T : struct, IAIState {
        if (!Init) {
            return;
        }
        AddState<T>();
        IAIState stateToActivate = _states[typeof(T)];
        if (!_activeStates.Contains(stateToActivate)) {
            stateToActivate.OnStart(npc: NPC, boss: Self);
        }
        else {
            return;
        }
        _activeStates.Add(stateToActivate);
    }

    private void DeactivateState<T>() where T : IAIState {
        if (!Init) {
            return;
        }
        IAIState stateToDeactivate = _states[typeof(T)];
        if (!_activeStates.Contains(stateToDeactivate)) {
            return;
        }
        stateToDeactivate.OnEnd(npc: NPC, boss: Self);
        _activeStates.Remove(stateToDeactivate);
    }

    private bool HasActiveState<T>() where T : IAIState {
        if (!Init) {
            return false;
        }
        if (_states.TryGetValue(typeof(T), out IAIState state)) {
            return _activeStates.Contains(state);
        }
        return false;
    }
}
