using Terraria;
using Terraria.ModLoader;
using ThoriumMod.Empowerments;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed partial class EternalHorror : ModNPC {
    private float _bodyDamageFactor = 1f;

    public int HP => 140000;
    public int DEFENSE => 100;
    public int DAMAGE_BODY => 50;

    public static int HP_SERVANT => 1000;
    public static int DEFENSE_SERVANT => 50;
    public static int DAMAGE_BODY_SERVANT => 50;

    public static float DAMAGEFACTOR_BODY_ONDASH => 1.5f;
    public static float DAMAGEFACTOR_SKULLEXPLOSION => 0.75f;

    public static float DEFENSEFACTOR_PHASE2 => 0.75f;

    public int GetFinalBodyDamage(int defDamage) {
        bool flag2 = Main.expertMode;
        float num16 = flag2 ? 0.3f : 1f;

        num16 *= _bodyDamageFactor;

        return NPC.GetAttackDamage_ScaledByStrength((float)defDamage * num16);
    }

    public int GetLaserDamage_Phase1() => GetDamage(1f);
    public float GetLaserKnockback_Phase1() => 0f;
    public int GetLaserDamage_Phase2() => GetDamage(1f);
    public float GetLaserKnockback_Phase2() => 0f;

    public int GetBigLaserDamage() => GetDamage(2f);
    public float GetBigLaserKnockback() => 0f;

    public int GetExplosionDamage_Phase1() => GetDamage(1.5f);
    public float GetExplosionKnockback_Phase1() => 0f;
    public int GetExplosionDamage_Phase2() => GetDamage(1.5f);
    public float GetExplosionKnockback_Phase2() => 0f;

    public int GetScytheDamage() => GetDamage(1.25f);
    public float GetScytheKnockback() => 0f;

    public int GetDamage(float modifier = 1f) => NPC.GetAttackDamage_ForProjectiles((int)(50 * modifier * 2f), (int)(30 * modifier * 2f));

    public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment) {
        NPC.lifeMax = (int)((double)NPC.lifeMax * 0.7 * (double)balance * (double)bossAdjustment);
        NPC.damage = (int)((double)NPC.damage * 1.15);
    }
}
