using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

[AutoloadBossHead]
sealed partial class EternalHorror : ModNPC {
    public static string EERIEOCRAM_MUSICPATH => "Assets/Music/EerieOcram";

    private ushort[] _customImmunityFrames;

    private void InitializeCustomImmunityFrames() {
        _customImmunityFrames = new ushort[Main.player.Length];
    }

    private void UpdateCustomImmunityFrames() {
        for (int i = 0; i < _customImmunityFrames.Length; i++) {
            _customImmunityFrames[i] = (ushort)Helper.Approach(_customImmunityFrames[i], 0, 1);
        }
    }

    public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone) {
        if (Init && player.whoAmI >= 0) {
            if (_customImmunityFrames[player.whoAmI] <= 0) {
                _customImmunityFrames[player.whoAmI] = 10;
            }
        }
    }

    public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone) {
        if (Init && projectile.owner >= 0) {
            if (_customImmunityFrames[projectile.owner] <= 0) {
                _customImmunityFrames[projectile.owner] = 10;
            }
        }
    }

    public override bool? CanBeHitByProjectile(Projectile projectile) {
        if (Init && projectile.owner >= 0) {
            if (_customImmunityFrames[projectile.owner] <= 0) {
                return base.CanBeHitByProjectile(projectile);
            }
        }

        return false;
    }

    public override bool? CanBeHitByItem(Player player, Item item) {
        if (Init && player.whoAmI >= 0) {
            if (_customImmunityFrames[player.whoAmI] <= 0) {
                return base.CanBeHitByItem(player, item);
            }
        }

        return false;
    }

    public static ushort SelfType => (ushort)ModContent.NPCType<EternalHorror>();

    public EternalHorror Self => NPC.As<EternalHorror>();

    public override void Load() {
        Load_BackgroundHooks();

        if (!Main.dedServ) {
            Load_Textures();
        }

        Load_ApplyShaderEffects();

        Load_BlinkingDrawing();
    }

    public override void Unload() {
        Unload_Caches();
    }

    private partial void Unload_Caches();

    private partial void Load_BackgroundHooks();

    private partial void Load_Textures();

    private partial void Load_ApplyShaderEffects();

    private partial void Load_BlinkingDrawing();

    public override void SetStaticDefaults() {
        NPC.SetMaxFrames(count: 6);
        NPC.SetTrail(trailingMode: 7, length: 10);

        SetMiscellaneousProperties();

        SetDebuffImmuneData();

        SetBestiaryInfo();
    }

    private partial void SetBestiaryInfo();

    private void SetMiscellaneousProperties() {
        NPCID.Sets.MPAllowedEnemies[Type] = true;
        NPCID.Sets.MustAlwaysDraw[Type] = true;
    }

    private void SetDebuffImmuneData() {
        NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
        NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.ShadowFlame] = true;
    }

    public override void SetDefaults() {
        NPC.SetHitboxSizeValues(width: 314, height: 216);

        NPC.SetDefaultsToEnemy(lifeMax: 54000,
                               damage: 55, 
                               defense: 36, 
                               spawnSlots: 10f,
                               boss: true);

        NPC.SetHitSounds(hitSound: SoundID.NPCHit18 with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f) },
                         deathSound: SoundID.NPCDeath18 with { Pitch = -MathHelper.Lerp(0.125f, 0.25f, 0.5f) });

        NPC.SetMiscellaneousProperties(dropCoins: Item.buyPrice(gold: 15),
                                       lavaImmune: true);

        NPC.SpawnWithHigherTime(timeMult: 30);

        SetMusic();
    }

    private void SetMusic() {
        Music = MusicLoader.GetMusicSlot(Mod, EERIEOCRAM_MUSICPATH);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit) {
        
    }

    public override void HitEffect(NPC.HitInfo hit) {
        float hitDirection = hit.HitDirection;
        int lifeMax = NPC.lifeMax;
        float dmg = hit.Damage;
        if (NPC.life > 0) {
            for (int num163 = 0; (double)num163 < dmg / (double)lifeMax * 100.0; num163++) {
                if (Main.rand.NextBool()) {
                    continue;
                }

                int dust2 = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CorruptTorch, hitDirection, -1f);
                Main.dust[dust2].position = NPC.Center + Main.rand.NextVector2Circular(NPC.width, NPC.height) / 3f;
                Main.dust[dust2].noLightEmittence = true;
                if (Main.rand.Next(2) == 0) {
                    Dust dust43 = Main.dust[Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Shadowflame)];
                    dust43.color = EternalHorror.MainPurpleColor;
                    dust43.position = NPC.Center + Main.rand.NextVector2Circular(NPC.width, NPC.height) / 3f;
                    dust43.noGravity = true;
                    dust43.scale = 1.5f;
                    dust43.fadeIn = 1f;
                    dust43.noLightEmittence = true;
                    Dust dust = dust43;
                    dust.velocity *= 3f;
                }
            }
        }
        else {

        }
    }
}
