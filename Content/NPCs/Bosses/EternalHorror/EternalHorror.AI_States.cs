using Consolaria.Common.Particles;
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

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed partial class EternalHorror : ModNPC {
    private static float ROTATIONLERP => 0.5f;

    private interface IAIState {
        public void OnActiveUpdate(NPC npc, EternalHorror boss);

        public void OnStart(NPC npc, EternalHorror boss) { }
        public void OnEnd(NPC npc, EternalHorror boss) { }
    }

    private readonly struct Phase1MoveToPlayer : IAIState {
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

                boss.ResetLaserAttack();

                boss.LaserAttackCount++;

                bool shotLasers = ++boss.AttackCount >= LASERATTACKCOUNT;
                if (!shotLasers) {
                    return;
                }

                boss.ResetCounters();
                boss.DeactivateState<Phase1BurstLaserAttack>();

                bool shouldDash = boss.LaserAttackCount > Phase1DashAttack.LASERATTACKCOUNTNEEDED;
                if (shouldDash) {
                    boss.LaserAttackCount = 0;
                    boss.ActivateState<Phase1DashAttack>();

                    //boss.ResetSmoothFactor();

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

                float dustProgress = boss.AICounter / SHADOWSPAWNTIME;

                ShakeStrength = Helper.Approach(ShakeStrength, dustProgress, 0.125f);

                dustProgress = Helper.YoYo(dustProgress);
                boss.SpawnPreparationDust_Shadow(dustProgress);

                if (shadowSpawnProgress) {
                    boss.ResetCounters();

                    if (boss.Phase1CloneSpawn_ShouldDashAfter) {
                        boss.Phase1CloneSpawn_ShouldDashAfter = false;
                        boss.ActivateState<Phase1DashAttack>();
                        boss.Phase1DashAttack_ShouldBurstLaserAfter = true;

                        //boss.ResetSmoothFactor();
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
        public static float DASHTIME => Helper.SecondsToFrames(0.75f * 0.75f);
        public static float LASERATTACKCOUNTNEEDED => 5;
        public static float DASHATTACKCOUNT => 5;
        public static float DASHSTRENGTH => 40f;

        public static SoundStyle DashSound => SoundID.Roar with { PitchVariance = 0.15f, MaxInstances = 0 };

        void IAIState.OnActiveUpdate(NPC npc, EternalHorror boss) {
            Player target = npc.GetTargetPlayer();
            Vector2 targetCenter = target.Center;

            float dashProgress = boss.AICounter / DASHTIME;

            float dustProgress = dashProgress;
            dustProgress = Helper.YoYo(dustProgress);
            boss.SpawnPreparationDust_Shadow(dustProgress);

            //if (boss.Phase1LastDash) {
            //    boss.OnIterateActiveCloneData((ref cloneInfo) => {
            //        //if (cloneInfo.LerpVelocityValue == 0f) {
            //        //    Player target = npc.GetTargetPlayer();
            //        //    Vector2 targetCenter = target.Center;
            //        //    Vector2 clonePosition = boss.GetCloneSpawnPosition();
            //        //    cloneInfo.TargetPosition = Vector2.Lerp(cloneInfo.TargetPosition, targetCenter, 0.1f);
            //        //    cloneInfo.Position = Vector2.Lerp(cloneInfo.Position, clonePosition, 0.1f);
            //        //}

            //        //cloneInfo.LerpVelocityValue = Helper.Approach(cloneInfo.LerpVelocityValue, 1f, 0.05f);

            //        //cloneInfo.DashOpacity = Helper.Approach(cloneInfo.DashOpacity, 0f, 0.1f);
            //    });
            //    foreach (CloneInfo cloneInfo in boss._cloneData) {
            //        if (cloneInfo.Active) {
            //            Vector2 targetCenter2 = targetCenter;
            //            targetCenter = Vector2.Lerp(npc.Center, cloneInfo.VisualPosition, 0.875f);
            //            targetCenter = Vector2.Lerp(targetCenter, targetCenter2, 0.875f);
            //            break;
            //        }
            //    }
            //}
            //else {
            //    //boss.OnIterateActiveCloneData((ref cloneInfo) => {
            //    //    cloneInfo.LerpVelocityValue = Helper.Approach(cloneInfo.LerpVelocityValue, 0f, 0.05f);
            //    //    if (cloneInfo.LerpVelocityValue <= 0f) {
            //    //        cloneInfo.ShouldUpdateVisualPosition = false;
            //    //    }
            //    //});
            //}

            boss._cloneTargetPosition = targetCenter;

            Vector2 baseTargetCenter = targetCenter;

            targetCenter += targetCenter.DirectionTo(npc.Center) * 10f;

            float dashStrength = DASHSTRENGTH;

            ShakeStrength = Helper.Approach(ShakeStrength, dashProgress, 0.125f);

            bool didAtLeastOneDash = boss.Phase1DashAttackCount > 0;

            float dashPreparationFactor = 0.25f;

            bool shouldResetState() {
                if (boss.Phase1DashAttackCount >= DASHATTACKCOUNT) {
                    boss.ActivateState<Phase1MoveToPlayer>();
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
                if (!boss.HasActiveState<Phase1MoveToPlayer>()) {
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

                    SoundEngine.PlaySound(DashSound with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f) }, npc.Center);

                    boss.AICounter = -DASHTIME;

                    boss.DeactivateState<Phase1MoveToPlayer>();

                    Vector2 dashDirection = npc.DirectionTo(targetCenter);
                    boss._dashVelocity = dashDirection * dashStrength;

                    boss.Phase1DashAttackCount++;

                    //if (!boss.Phase1LastDash)
                    {
                        boss.OnIterateActiveCloneData((ref cloneInfo) => {
                            Vector2 clonePosition = cloneInfo.VisualPosition;
                            dashDirection = clonePosition.DirectionTo(targetCenter);
                            cloneInfo.Velocity = dashDirection * dashStrength;
                            cloneInfo.ShouldUpdateVisualPosition = true;
                        });
                    }

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
                //if (!boss.Phase1LastDash) 
                {
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
                if (boss.Phase1AtLeast1Dash && shouldDestroyClones) {
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

            boss.OnIterateActiveCloneData((ref cloneInfo) => {
                Vector2 position = cloneInfo.VisualPosition;
                float maxDistance = 600f;
                float distanceFactor = npc.Distance(position) / maxDistance;
                distanceFactor = Helper.Clamp01(distanceFactor);
                distanceFactor = 1f - distanceFactor;
                npc.Center += npc.DirectionFrom(position) * distanceFactor * 2.5f;
            });

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
        public static float MOVETOPLAYERTIME => Helper.SecondsToFrames(1f * 0.75f);
        public static float SLOWDOWNTIME => Helper.SecondsToFrames(1f * 0.875f);
        public static float ATTACKTIME => Helper.SecondsToFrames(1.5f * 0.875f);

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

                    boss.ResetLaserAttack(applyIncreasedDelay: true, applyExtraIncreasedDelay: true);

                    boss.ActivateState<Phase1MoveToPlayer>();
                    boss.DeactivateState<Phase1SummonSpawnAttack>();
                    boss.ActivateState<Phase1BurstLaserAttack>();
                }

                return;
            }

            float preparationProgress = boss.AICounter / MOVETOPLAYERTIME;

            float dustProgress = preparationProgress;
            dustProgress = Helper.YoYo(dustProgress);
            boss.SpawnPreparationDust_Shadow(dustProgress);

            bool shouldStopPreparing = boss.AICounter >= MOVETOPLAYERTIME;
            if (!shouldStopPreparing) {
                float preparationSlowDown = 1f - preparationProgress;
                preparationSlowDown *= 1f - Utils.GetLerpValue(0.5f, 1f, preparationSlowDown, true);

                //npc.velocity *= preparationSlowDown;

                //boss.ResetSmoothFactor(preparationSlowDown);

                boss.ActivateState<Phase1MoveToPlayer>();

                return;
            }

            boss.DeactivateState<Phase1MoveToPlayer>();

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
                lerpValue *= 1.875f;

                float dashOpacity = 0.5f;
                boss._dashOpacity = Helper.Approach(boss._dashOpacity, dashOpacity, smoothFactor * 0.25f);
                boss.OnIterateActiveCloneData((ref cloneInfo) => {
                    cloneInfo.DashOpacity = Helper.Approach(cloneInfo.DashOpacity, dashOpacity, smoothFactor * 0.25f);
                });

                Vector2 destination = boss._tempPosition;
                Vector2 velocity = npc.DirectionTo(destination);
                float wave = MathF.Sin(attackProgress * 60f * 0.15f + npc.IsFacingLeft().ToInt() * MathHelper.Pi);
                npc.velocity = Vector2.Lerp(npc.velocity, velocity.RotatedBy(wave) * WaveMovementSpeed, lerpValue);

                npc.velocity += npc.DirectionTo(targetCenter) * 5f * lerpValue;

                npc.rotation = npc.rotation.AngleLerp(npc.velocity.ToRotation() - MathHelper.PiOver2, lerpValue);

                boss.SpawnPreparationDust_Shadow(1f);

                if (attackProgress >= 1f) {
                    boss.ResetCounters();
                    boss.ResetSmoothFactor();

                    boss.AICounter = delayTime;
                }

                if (boss._dashOpacity >= dashOpacity / 4f && boss.AICounter % 6 == 0) {
                    SoundEngine.PlaySound(SummonSpawnSound with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f) }, npc.Center);

                    Vector2 getPosition() => npc.Center + Main.rand.NextVector2Circular(npc.width, npc.height) * 0f;
                    void makeSpawnDust() {
                        Color colorTint = MainPurpleColor * 0.5f;

                        Vector2 position = getPosition() + Main.rand.NextVector2Circular(npc.width, npc.height) * 0.25f;
                        Vector2 velocity = Vector2.UnitY.RotatedBy(MathHelper.TwoPi * Main.rand.NextFloat()) * 2.5f * Main.rand.NextFloat();
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

                    for (int l = 0; l < 4; l++) {
                        if (Main.rand.NextBool()) {
                            continue;
                        }
                        makeSpawnDust();
                    }

                    if (!Helper.IsClient()) {
                        for (int i = 0; i < 1; i++) {
                            Vector2 spawnPosition = getPosition();
                            int servantIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<EternalServant>());
                            Main.npc[servantIndex].velocity += npc.velocity * 0.75f;
                        }
                    }
                }
            }
            else {
                npc.ResetTrails();

                boss._copiesIntensity = preparationProgress2;

                ShakeStrength = Helper.Approach(ShakeStrength, preparationProgress2, 0.125f);

                lookAtTarget();

                boss._tempPosition = default;

                npc.velocity *= 1f - 0.05f * preparationProgress2;
            }
        }

        void IAIState.OnStart(NPC npc, EternalHorror boss) { }
        void IAIState.OnEnd(NPC npc, EternalHorror boss) { }
    }

    private readonly struct Phase2Transition : IAIState {
        private static ushort PREPARATIONTIME => Helper.SecondsToFrames(2);

        void IAIState.OnActiveUpdate(NPC npc, EternalHorror boss) {
            boss.AICounter++;

            float attackProgress_ForSlowDown = boss.AICounter / PREPARATIONTIME;
            attackProgress_ForSlowDown = Helper.Clamp01(attackProgress_ForSlowDown);
            float preparationSlowDown = Utils.Remap(1f - attackProgress_ForSlowDown, 0f, 1f, 0.75f, 1f, true);

            npc.velocity *= preparationSlowDown;
            //npc.rotation *= preparationSlowDown;

            npc.dontTakeDamage = true;

            npc.reflectsProjectiles = true;
            npc.ReflectProjectiles(NPC.Hitbox);

            float transitionTime = 100f;

            transitionTime *= 0.375f;

            float progress = boss._ai5 / transitionTime;
            for (int i = 0; i < 2; i++) {
                boss.SpawnPreparationDust_Shadow(Helper.YoYo(progress));
            }

            Player target = npc.GetTargetPlayer();
            Vector2 targetCenter = target.Center,
                    baseTargetCenter = targetCenter;
            float angleToTarget = npc.AngleTo(baseTargetCenter) - MathHelper.PiOver2;

            boss._cloneTargetPosition = targetCenter;

            ShakeStrength = Helper.Approach(ShakeStrength, progress, 0.125f);

            boss.SmoothFactor = Helper.Approach(boss.SmoothFactor, 1f, 0.025f * 0.025f * 1f);

            Vector2 getPosition() => npc.Center + Main.rand.NextVector2Circular(npc.width, npc.height) / 2f;

            if (boss.AICounter2 == 1f || boss.AICounter2 == 2f) {
                boss._ai5 += 1f;

                if (boss.AICounter2 == 1f || boss._ai4 == 1f) {

                    boss.AICounter3 += 0.01f;
                    if ((double)boss.AICounter3 > 0.5)
                        boss.AICounter3 = 0.5f;
                }
                else {
                    boss._ai5 -= 0.5f;

                    boss.AICounter3 -= 0.005f;
                    if (boss.AICounter3 < 0f)
                        boss.AICounter3 = 0f;
                }

                npc.rotation += boss.AICounter3;

                if (boss._ai5 % 4 == 0 && progress <= 0.875f) {
                    boss.MakeTrailDust(velocityFactor: 1f);
                }

                npc.rotation = npc.rotation.AngleLerp(angleToTarget, ROTATIONLERP * boss.SmoothFactor);

                if (boss._ai5 % 20 == 0 && (boss.AICounter2 == 1f || boss._ai4 == 1f)) {
                    //AddGlow(20f, 0.5f, Color.BlueViolet);
                    SoundEngine.PlaySound(SoundID.NPCDeath43 with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f) }, npc.Center);
                    SoundEngine.PlaySound((Main.rand.NextBool(3) ? SoundID.NPCDeath23 : SoundID.NPCDeath22) with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f) }, npc.Center);

                    if (!Main.dedServ) {
                        for (int i = 0; i < 2; i++) {
                            if (i > 0 || Main.rand.NextBool()) {
                                Gore.NewGore(npc.GetSource_FromAI(), getPosition(), new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f) * 1.5f, ModContent.Find<ModGore>("Consolaria/EternalHorrorGore1").Type, Main.rand.NextFloat(0.9f, 1.4f));
                                Gore.NewGore(npc.GetSource_FromAI(), getPosition(), new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f) * 1.5f, ModContent.Find<ModGore>("Consolaria/EternalHorrorGore2").Type, Main.rand.NextFloat(0.9f, 1.4f));
                                Gore.NewGore(npc.GetSource_FromAI(), getPosition(), new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f) * 1.5f, ModContent.Find<ModGore>("Consolaria/EternalHorrorGore3").Type, Main.rand.NextFloat(0.9f, 1.4f));
                            }
                        }

                        //for (int index1 = 0; index1 < 14; ++index1) {
                        //    int index2 = Dust.NewDust(new Vector2(NPC.Center.X, NPC.Center.Y), 0, 0, DustID.Shadowflame, 0f, 0f, 100, default, Main.rand.NextFloat(1f, 2.5f));
                        //    Main.dust[index2].noGravity = true;
                        //    Main.dust[index2].noLight = false;
                        //    Main.dust[index2].fadeIn = Main.rand.NextFloat(0, 1f);
                        //    Main.dust[index2].position += new Vector2(Main.rand.Next(40, 120), 0).RotatedByRandom(rad);
                        //    Main.dust[index2].velocity = Vector2.Normalize(NPC.Center - Main.dust[index2].position) * Main.rand.NextFloat(-10f, -1f);
                        //}
                    }
                }

                //if (Main.getGoodWorld)
                //    reflectsProjectiles = true;

                if (Main.netMode != 1) {
                    int num29 = 20;
                    //if (Main.getGoodWorld && life < lifeMax / 3)
                    num29 = 6;

                    if (boss._ai5 % (float)num29 == 0f) {
                        float num30 = 5f;
                        Vector2 vector5 = boss.GetEyePosition();
                        float num31 = Main.rand.Next(-200, 200);
                        float num32 = Main.rand.Next(-200, 200);
                        //if (Main.getGoodWorld) {
                        //    num31 *= 3f;
                        //    num32 *= 3f;
                        //}

                        num31 *= 3f;
                        num32 *= 3f;

                        float num33 = (float)Math.Sqrt(num31 * num31 + num32 * num32);
                        num33 = num30 / num33;
                        Vector2 vector6 = vector5;
                        Vector2 vector7 = default(Vector2);
                        vector7.X = num31 * num33;
                        vector7.Y = num32 * num33;
                        vector6.X += vector7.X * 10f;
                        vector6.Y += vector7.Y * 10f;

                        vector7 *= 5f;

                        int num34 = NPC.NewNPC(npc.GetSource_FromAI(), (int)vector6.X, (int)vector6.Y, ModContent.NPCType<EternalServant>(), ai2: 1f);
                        Main.npc[num34].velocity.X = vector7.X;
                        Main.npc[num34].velocity.Y = vector7.Y;
                        if (Main.netMode == 2 && num34 < 200)
                            NetMessage.SendData(23, -1, -1, null, num34);
                    }

                    //for (int n = 0; n < 10; n++) {
                    //    Dust.NewDust(vector6, 20, 20, 5, vector7.X * 0.4f, vector7.Y * 0.4f);
                    //}
                }

                if (boss._ai5 >= transitionTime) {
                    if (boss._ai4 == 1f) {
                        boss._ai4 = 0f;
                        boss._ai5 = 0f;
                    }
                    else {
                        boss.AICounter2 += 1f;
                        boss._ai5 = 0f;
                        if (boss.AICounter2 == 3f) {
                            boss.AICounter3 = 0f;
                        }
                        else {
                            SoundEngine.PlaySound(SoundID.NPCHit1 with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f) }, npc.Center);

                            //SoundEngine.PlaySound(3, (int)position.X, (int)position.Y);
                            if (!Main.dedServ) {
                                IEntitySource goreSource = npc.GetSource_FromAI();
                                for (int i = 0; i < 2; i++) {
                                    if (i > 0 || Main.rand.NextBool()) {
                                        for (int num35 = 0; num35 < 2; num35++) {
                                            Gore.NewGore(goreSource, getPosition(), new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f) * 1.5f, ModContent.Find<ModGore>("Consolaria/EternalHorrorGore1").Type);
                                            Gore.NewGore(goreSource, getPosition(), new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f) * 1.5f, ModContent.Find<ModGore>("Consolaria/EternalHorrorGore2").Type);
                                            Gore.NewGore(goreSource, getPosition(), new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f) * 1.5f, ModContent.Find<ModGore>("Consolaria/EternalHorrorGore3").Type);
                                        }
                                    }
                                }
                            }

                            //for (int num36 = 0; num36 < 20; num36++) {
                            //    Dust.NewDust(position, width, height, 5, (float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f);
                            //}

                            //SoundEngine.PlaySound(15, (int)position.X, (int)position.Y, 0);
                            SoundEngine.PlaySound(Phase2DashAttack.DashSound with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f) }, npc.position);
                        }
                    }
                }

                //Dust.NewDust(position, width, height, 5, (float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f);

                return;
            }

            npc.reflectsProjectiles = false;

            boss.DeactivateAllStates();

            boss.ResetCounters();

            boss.ResetLaserAttack(applyExtraIncreasedDelay: true);

            boss.ActivateState<Phase2MoveToPlayer>();
            boss.ActivateState<Phase2BurstLaserAttack>();

            npc.dontTakeDamage = false;

            npc.reflectsProjectiles = false;
        }

        void IAIState.OnStart(NPC npc, EternalHorror boss) {
            boss.DeactivateAllStates();
            boss.ResetCounters();

            boss.AICounter2 = 1f;

            boss.ResetSmoothFactor();

            //boss.ActivateState<Phase2MoveToPlayer>();
            //boss.ActivateState<Phase2BurstLaserAttack>();

            boss.OnIterateActiveCloneData((ref cloneInfo) => {
                //cloneInfo.ShouldUpdateVisualPosition = true;
                //cloneInfo.ShouldUpdateVisualPosition = true;
                //cloneInfo.Velocity *= 0f;
                //cloneInfo.DashOpacity *= 0f;
                cloneInfo.ShouldFade = true;
                //cloneInfo.ForceFade();
            });

            //boss.SpawnRays(startRaySpeed: 0.5f);
        }
        void IAIState.OnEnd(NPC npc, EternalHorror boss) { }
    }

    private readonly struct Phase2MoveToPlayer : IAIState {
        void IAIState.OnActiveUpdate(NPC npc, EternalHorror boss) {
            boss.TargetPlayer();
            npc.FaceTarget_New();

            bool shouldDash = boss.HasActiveState<Phase2DashAttack>(),
                 shouldSpamLasers = boss.HasActiveState<Phase2LaserSpamAttack>(),
                 shouldSpawnSummons = boss.HasActiveState<Phase1SummonSpawnAttack>();

            shouldSpawnSummons = false;

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
                    angleFactor = Utils.Remap(angleFactor, Phase2LaserSpamAttack.PPEPARATIONSLOWLASTFACTOR, 1f, 0f, 1f, true);
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
            //npc.Center = Vector2.Lerp(npc.Center, targetCenter, progress);
            //npc.rotation = Utils.AngleLerp(npc.rotation, 0f, progress);
        }

        void IAIState.OnStart(NPC npc, EternalHorror boss) {
            //boss.SpawnRays(startRaySpeed: 0.5f);
        }
        void IAIState.OnEnd(NPC npc, EternalHorror boss) { }
    }

    private readonly struct Phase2BurstLaserAttack : IAIState {
        public static float LASERATTACKTIME => Helper.SecondsToFrames(1.5f);
        public static byte LASERATTACKCOUNT => (byte)(Phase2DashAttack.LASERATTACKCOUNTNEEDED + 1);

        void IAIState.OnActiveUpdate(NPC npc, EternalHorror boss) {
            Player target = npc.GetTargetPlayer();
            Vector2 targetCenter = target.Center;

            boss._cloneTargetPosition = targetCenter;

            float laserProgress = boss.Phase1BurstLaserAttackProgress;
            float laserProgress_ForLaserRotation = laserProgress * Utils.GetLerpValue(1f, 0.5f, laserProgress, true);

            boss.OnIterateActiveCloneData((ref cloneInfo) => {
                cloneInfo.DashOpacity = Helper.Approach(cloneInfo.DashOpacity, 0f, 1f / 60f);
            });

            void shootLasers() {
                if ((boss.AICounter < 0f && laserProgress > 0f) || boss.AICounter >= LASERATTACKTIME * 0.5f) {
                    if (boss.AICounter % 2 == 0) {
                        boss._eyeEffectOpacity = 1f;

                        boss.ShootLaser(shootBasedOnRotation: true, reflectedFromStart: true);
                    }
                }
            }
            void prepareLasers() {
                ref float laserRotation = ref boss.Phase1LaserRotation;
                float newLaserRotation = npc.AngleTo(targetCenter);
                laserRotation = Utils.AngleLerp(laserRotation, newLaserRotation, laserProgress_ForLaserRotation);

                boss.AICounter += boss.AICounter < LASERATTACKTIME * 0.5f ? 1f : 0.5f;

                if (boss.AICounter >= -LASERATTACKTIME * 1.125f) {
                    if (boss.Phase2_WasDoingCloneAttack) {
                        boss.Phase2_WasDoingCloneAttack = false;
                    }
                }

                bool shotLaser = boss.AICounter >= LASERATTACKTIME * 1f;
                if (!shotLaser) {
                    return;
                }

                boss.ResetLaserAttack();

                boss.LaserAttackCount++;

                bool shotLasers = ++boss.AttackCount >= LASERATTACKCOUNT;
                if (!shotLasers) {
                    return;
                }

                boss.DeactivateState<Phase2BurstLaserAttack>();

                //bool shouldDash = boss.LaserAttackCount > Phase2DashAttack.LASERATTACKCOUNTNEEDED;
                //if (shouldDash)
                {

                    boss.LaserAttackCount = 0;
                    boss.ActivateState<Phase2DashAttack>();

                    boss.ResetCounters();

                    //return;
                }

                //boss.ActivateState<Phase1CloneSpawn>();
            }

            if (boss.Phase2_WasDoingCloneAttack && boss.AICounter % 1 == 0) {
                boss.MakeTrailDust();
            }

            prepareLasers();
            shootLasers();
        }

        void IAIState.OnStart(NPC npc, EternalHorror boss) {
            boss.ResetLaserAttack();

            boss.AICounter = LASERATTACKTIME * 0.5f * 0.5f;
        }
        void IAIState.OnEnd(NPC npc, EternalHorror boss) { }
    }

    private readonly struct Phase2DashAttack : IAIState {
        public static float DASHTIME => Helper.SecondsToFrames(0.75f * 0.75f * 0.75f);
        public static float DASHTIME_FIRSTDASH => Helper.SecondsToFrames(0.75f);
        public static float LASERATTACKCOUNTNEEDED => 1;
        public static float DASHATTACKCOUNT => 5;
        public static float DASHSTRENGTH => 40f;

        public static SoundStyle DashSound => SoundID.Roar with { PitchVariance = 0.15f, MaxInstances = 0 };

        void IAIState.OnActiveUpdate(NPC npc, EternalHorror boss) {
            Player target = npc.GetTargetPlayer();
            Vector2 targetCenter = target.Center;

            float smoothFactor = boss.SmoothFactor/*Ease.SineIn(boss.SmoothFactor)*/;
            smoothFactor = 1f;
            //targetCenter.X = MathHelper.Lerp(targetCenter.X, npc.Center.X, 0.75f * smoothFactor);
            //targetCenter.Y = MathHelper.Lerp(targetCenter.Y, npc.Center.Y, 0.5f * smoothFactor);

            float dashTime = DASHTIME;

            bool didAtLeastOneDash = boss.Phase2DashAttackCount > 0;

            if (!didAtLeastOneDash) {
                dashTime = DASHTIME_FIRSTDASH;
            }

            float dashProgress = boss.AICounter / dashTime;

            if (!didAtLeastOneDash) {
                if (dashProgress >= 0.5f) {
                    boss.OnIterateActiveCloneData((ref cloneInfo) => {
                        Vector2 clonePosition = cloneInfo.VisualPosition;
                        //dashDirection = clonePosition.DirectionTo(targetCenter);
                        //cloneInfo.Velocity = dashDirection * dashStrength;

                        if (!cloneInfo.ShouldFade) {
                            cloneInfo.ShouldFade = true;

                            cloneInfo.ExplosionTime = CloneInfo.EXPLOSIONTIME;
                        }
                    });
                }
            }

            didAtLeastOneDash = true;

            float dustProgress = dashProgress;
            dustProgress = Helper.YoYo(dustProgress);
            boss.SpawnPreparationDust_Shadow(dustProgress);

            //if (boss.Phase1LastDash) {
            //    boss.OnIterateActiveCloneData((ref cloneInfo) => {
            //        //if (cloneInfo.LerpVelocityValue == 0f) {
            //        //    Player target = npc.GetTargetPlayer();
            //        //    Vector2 targetCenter = target.Center;
            //        //    Vector2 clonePosition = boss.GetCloneSpawnPosition();
            //        //    cloneInfo.TargetPosition = Vector2.Lerp(cloneInfo.TargetPosition, targetCenter, 0.1f);
            //        //    cloneInfo.Position = Vector2.Lerp(cloneInfo.Position, clonePosition, 0.1f);
            //        //}

            //        //cloneInfo.LerpVelocityValue = Helper.Approach(cloneInfo.LerpVelocityValue, 1f, 0.05f);

            //        //cloneInfo.DashOpacity = Helper.Approach(cloneInfo.DashOpacity, 0f, 0.1f);
            //    });
            //    foreach (CloneInfo cloneInfo in boss._cloneData) {
            //        if (cloneInfo.Active) {
            //            Vector2 targetCenter2 = targetCenter;
            //            targetCenter = Vector2.Lerp(npc.Center, cloneInfo.VisualPosition, 0.875f);
            //            targetCenter = Vector2.Lerp(targetCenter, targetCenter2, 0.875f);
            //            break;
            //        }
            //    }
            //}
            //else {
            //    //boss.OnIterateActiveCloneData((ref cloneInfo) => {
            //    //    cloneInfo.LerpVelocityValue = Helper.Approach(cloneInfo.LerpVelocityValue, 0f, 0.05f);
            //    //    if (cloneInfo.LerpVelocityValue <= 0f) {
            //    //        cloneInfo.ShouldUpdateVisualPosition = false;
            //    //    }
            //    //});
            //}

            boss._cloneTargetPosition = targetCenter;

            Vector2 baseTargetCenter = targetCenter;

            targetCenter += targetCenter.DirectionTo(npc.Center) * 10f;

            float dashStrength = DASHSTRENGTH;

            ShakeStrength = Helper.Approach(ShakeStrength, dashProgress, 0.125f);

            boss.OnIterateActiveCloneData((ref cloneInfo) => {
                cloneInfo.DashOpacity = Helper.Approach(cloneInfo.DashOpacity, 0f, 1f / 60f);
            });

            float dashPreparationFactor = 0.25f;

            bool shouldResetState() {
                if (boss.Phase2DashAttackCount >= DASHATTACKCOUNT) {
                    boss.ActivateState<Phase2MoveToPlayer>();
                    boss.DeactivateState<Phase2DashAttack>();
                    boss.ActivateState<Phase2LaserSpamAttack>();
                    boss.ResetCounters();

                    boss.ResetSmoothFactor(value: 0.25f);
                    boss.Phase2DashAttackCount = 0;

                    boss._dashVelocity *= 0f;

                    //boss.OnIterateActiveCloneData((ref cloneInfo) => {
                    //    //cloneInfo.ShouldUpdateVisualPosition = true;
                    //    cloneInfo.ShouldFade = true;
                    //});

                    return true;
                }

                return false;
            }
            void lookAtTarget() {
                if (!boss.HasActiveState<Phase2MoveToPlayer>()) {
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
                bool shouldDash = boss.AICounter >= dashTime;

                boss._copiesIntensity = dashProgress;

                if (shouldDash) {
                    if (shouldResetState()) {
                        return;
                    }

                    if (boss.Phase2_WasDoingCloneAttack) {
                        boss.Phase2_WasDoingCloneAttack = false;
                    }

                    SoundEngine.PlaySound(DashSound with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f) }, npc.Center);

                    {
                        boss._eyeEffectOpacity = 1f;

                        Vector2[] scytheSpawnOffsets = [new Vector2(120, 50), new Vector2(160, 50), new Vector2(-120, -50), new Vector2(-160, -50), new Vector2(50, 120), new Vector2(50, 160), new Vector2(-50, -120), new Vector2(-50, -160)];
                        foreach (Vector2 spawnOffset in scytheSpawnOffsets) {
                            boss.SpawnScythe(spawnOffset * 1f, shootBasedOnRotation: true);
                        }
                    }

                    boss.AICounter = -dashTime;

                    boss.DeactivateState<Phase2MoveToPlayer>();

                    npc.ResetTrails();
                    Vector2 dashDirection = npc.DirectionTo(targetCenter);
                    boss._dashVelocity = dashDirection * dashStrength;

                    boss.Phase2DashAttackCount++;

                    //if (!boss.Phase1LastDash)
                    {
                        //boss.OnIterateActiveCloneData((ref cloneInfo) => {
                        //    Vector2 clonePosition = cloneInfo.VisualPosition;
                        //    dashDirection = clonePosition.DirectionTo(targetCenter);
                        //    //cloneInfo.Velocity = dashDirection * dashStrength;

                        //    if (!cloneInfo.ShouldUpdateVisualPosition) {
                        //        //cloneInfo.ShouldFade = true;
                        //        cloneInfo.ShouldUpdateVisualPosition = true;
                        //        //cloneInfo.ExplosionTime = CloneInfo.EXPLOSIONTIME;
                        //    }
                        //});
                    }

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
                //if (!boss.Phase1LastDash) 
                {
                    boss.OnIterateActiveCloneData((ref cloneInfo) => {
                        cloneInfo.Velocity *= velocityDeceleration;
                        //cloneInfo.DashOpacity = Helper.Approach(cloneInfo.DashOpacity, dashOpacity, 1f);
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
                //bool shouldDestroyClones = npc.velocity.Length() > dashStrength * MathHelper.Lerp(0.5f, 0.875f, 0.5f);
                //if (boss.Phase1AtLeast1Dash && shouldDestroyClones) {
                //    boss.DestroyClonesOnContact();
                //}
            }

            //if (boss.Phase2_WasDoingCloneAttack && boss.AICounter % 2 == 0) {
            //    boss.MakeTrailDust();
            //}

            destroyClones();

            prepareDash();

            if (shouldSlowDownAfterDash()) {
                prepareSelfAfterDash();
                slowDown();
                return;
            }

            boss.OnIterateActiveCloneData((ref cloneInfo) => {
                Vector2 position = cloneInfo.VisualPosition;
                float maxDistance = 600f;
                float distanceFactor = npc.Distance(position) / maxDistance;
                distanceFactor = Helper.Clamp01(distanceFactor);
                distanceFactor = 1f - distanceFactor;
                npc.Center += npc.DirectionFrom(position) * distanceFactor * 2.5f;
            });

            lookAtTarget();
            extraSlowDownAfterDash();
        }

        void IAIState.OnStart(NPC npc, EternalHorror boss) {
            boss._dashVelocity *= 0f;
            boss._dashOpacity = 0f;
        }
        void IAIState.OnEnd(NPC npc, EternalHorror boss) { }
    }

    private readonly struct Phase2LaserSpamAttack : IAIState {
        public static float BEFORESPAMTIME => Helper.SecondsToFrames(1.5f * 1.5f);
        public static float SPAMATTACKTIME => Helper.SecondsToFrames(2f * 2f);

        public static float PPEPARATIONSLOWLASTFACTOR => 0.75f;

        void IAIState.OnActiveUpdate(NPC npc, EternalHorror boss) {
            float preparationProgress = boss.Phase2LaserSpamPreparationSlowDown;
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
                boss.Phase2LaserSpamPreparationSlowDown = preparationSlowDown;
            }
            void spamLasers() {
                if (shouldStartAttack) {
                    float laserAttackFireRate = Utils.Remap(attackProgress_ForSlowDown, 0f, 1f, 0.25f, 1f, true);
                    boss.AICounter2 += laserAttackFireRate;
                    float rotationOffset = MathHelper.PiOver4 * 0.5f * laserAttackFireRate;
                    if (boss.AICounter2 >= 1f * 2f) {
                        boss._eyeEffectOpacity = 1f;

                        float waveStep = attackProgress_ForSlowDown * 10f;
                        if (npc.IsFacingLeft()) {
                            waveStep += MathHelper.TwoPi;
                        }
                        float max = 2f;

                        float randomAngleShift = max * Main.rand.NextFloatDirection() * attackProgress_ForSlowDown/*Helper.Wave(waveStep, -max, max, 2.5f, boss.WaveOffset)*/ * rotationOffset;
                        randomAngleShift *= Main.rand.NextFloat(0.75f, 1.25f);

                        float forcedRotationOffset = randomAngleShift * 0.25f;
                        forcedRotationOffset *= 1f - Utils.GetLerpValue(0.75f, 1f, attackProgress_ForSlowDown, true);
                        boss._forcedRotationOffset += forcedRotationOffset;
                        boss.ShootLaser(shootBasedOnRotation: false, angleShiftToPlayer: randomAngleShift, reflectedFromStart: true);
                        boss.AICounter2 = 0f;
                    }
                }
                else {
                    boss._eyeEffectOpacity = Ease.CubeIn(boss.Phase2LaserSpamAttackProgress);
                }
            }
            void stopAttacking() {
                bool shouldStopAttack = boss.AICounter >= SPAMATTACKTIME + BEFORESPAMTIME;
                if (shouldStopAttack) {
                    boss.ResetCounters();
                    boss.ResetSmoothFactor();
                    boss.DeactivateState<Phase2LaserSpamAttack>();

                    boss.ResetLaserAttack(applyIncreasedDelay: true);

                    boss.ActivateState<Phase2MoveToPlayer>();

                    boss.ActivateState<Phase2CloneSpawn>();

                    //boss.Phase1CloneSpawn_ShouldDashAfter = true;
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

    private readonly struct Phase2CloneSpawn : IAIState {
        public static float SHADOWSPAWNTIME => Helper.SecondsToFrames(1);

        void IAIState.OnActiveUpdate(NPC npc, EternalHorror boss) {
            void prepareAndSpawnClone() {
                bool justStarted = boss.AICounter == 0f;

                bool shadowSpawnProgress = ++boss.AICounter >= SHADOWSPAWNTIME;

                float dustProgress = boss.AICounter / SHADOWSPAWNTIME;

                float preparationSlowDown = Utils.Remap(1f - dustProgress, 0f, 1f, 0.75f, 1f, true);

                npc.velocity *= preparationSlowDown;

                ShakeStrength = Helper.Approach(ShakeStrength, dustProgress, 0.125f * 2f);

                boss._eyeEffectOpacity = Ease.CubeIn(dustProgress);

                dustProgress = Helper.YoYo(dustProgress);
                for (int i = 0; i < 2; i++) {
                    boss.SpawnPreparationDust_Shadow(dustProgress);
                }


                if (shadowSpawnProgress) {
                    boss.ResetCounters();

                    boss.SpawnClone(insta: true);

                    npc.ResetTrails();

                    Player target = npc.GetTargetPlayer();
                    Vector2 targetCenter = target.Center;
                    Vector2 dashDirection = npc.DirectionTo(targetCenter);
                    dashDirection *= -1;
                    boss._dashVelocity = dashDirection * Phase2DashAttack.DASHSTRENGTH;

                    boss._dashOpacity = 1f;

                    boss.OnIterateActiveCloneData((ref cloneInfo) => {
                        cloneInfo.DashOpacity = 1f;
                    });

                    boss._eyeEffectOpacity = 1f;

                    SoundEngine.PlaySound(Phase2DashAttack.DashSound with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f) }, npc.Center);

                    npc.velocity = boss._dashVelocity;

                    if (boss.Phase1CloneSpawn_ShouldDashAfter) {
                        boss.Phase1CloneSpawn_ShouldDashAfter = false;
                        boss.ActivateState<Phase2DashAttack>();
                        boss.Phase1DashAttack_ShouldBurstLaserAfter = true;

                        //boss.ResetSmoothFactor();
                    }
                    else {
                        boss.ActivateState<Phase2BurstLaserAttack>();

                        boss.ResetLaserAttack(applyIncreasedDelay: true, applyExtraIncreasedDelay: true);
                    }
                    boss.DeactivateState<Phase2CloneSpawn>();

                    boss.Phase2_WasDoingCloneAttack = true;
                }
            }

            prepareAndSpawnClone();
        }

        void IAIState.OnStart(NPC npc, EternalHorror boss) {
        }
        void IAIState.OnEnd(NPC npc, EternalHorror boss) { }
    }

    private Dictionary<Type, IAIState> _states = null;
    private HashSet<IAIState> _activeStates = null;

    public ref float Phase1LaserRotation => ref NPC.ai[2];

    public float Phase1BurstLaserAttackProgress => Helper.Clamp01(AICounter / Phase1BurstLaserAttack.LASERATTACKTIME);
    public float Phase1ShadowSpawnProgress => Helper.Clamp01(AICounter / Phase1CloneSpawn.SHADOWSPAWNTIME);
    public float Phase1LaserSpamAttackProgress => Ease.SineIn(Helper.Clamp01(AICounter / Phase1LaserSpamAttack.BEFORESPAMTIME));
    public bool Phase1LastDash => Phase1DashAttackCount >= Phase1DashAttack.DASHATTACKCOUNT - 1;
    public bool Phase1AtLeast1Dash => Phase1DashAttackCount > 1;

    public float Phase2BurstLaserAttackProgress => Helper.Clamp01(AICounter / Phase2BurstLaserAttack.LASERATTACKTIME);
    public float Phase2LaserSpamAttackProgress => Ease.SineIn(Helper.Clamp01(AICounter / Phase2LaserSpamAttack.BEFORESPAMTIME));

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

    private void DeactivateAllStates() {
        if (!Init) {
            return;
        }

        _activeStates.Clear();
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
