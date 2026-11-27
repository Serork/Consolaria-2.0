using Consolaria.Content.Crossmod.Thorium;
using Consolaria.Content.Crossmod.Thorium.Weapons;
using Consolaria.Content.Items.Consumables;
using Consolaria.Content.Items.Materials;
using Consolaria.Content.Items.Pets;
using Consolaria.Content.Items.Placeable;
using Consolaria.Content.Items.Vanity;
using Consolaria.Content.Items.Weapons.Magic;
using Consolaria.Content.Items.Weapons.Melee;
using Consolaria.Content.Items.Weapons.Ranged;
using Consolaria.Content.Items.Weapons.Summon;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed partial class EternalHorror : ModNPC {
    public override void ModifyNPCLoot(NPCLoot npcLoot) {
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<OcramTrophy>(), 10));

        Conditions.NotExpert notExpert = new Conditions.NotExpert();
        npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<OcramBag>()));
        npcLoot.Add(ItemDropRule.MasterModeCommonDrop(ModContent.ItemType<OcramRelic>()));
        npcLoot.Add(ItemDropRule.MasterModeDropOnAllPlayers(ModContent.ItemType<CursedFang>(), 4));

        LeadingConditionRule notExpertRule = new LeadingConditionRule(new Conditions.NotExpert());

        if (ModLoader.HasMod(ThoriumCompat.THORIUMMODNAME)) {
            AddThoriumLoot(notExpertRule);
        }
        else {
            notExpertRule.OnSuccess(new OneFromOptionsDropRule(1, 1,
                ModContent.ItemType<EternityStaff>(),
                ModContent.ItemType<DragonBreath>(),
                ModContent.ItemType<OcramsEye>(),
                ModContent.ItemType<Tizona>()));
        }

        npcLoot.Add(notExpertRule);
        npcLoot.Add(ItemDropRule.ByCondition(notExpert, ModContent.ItemType<SoulofBlight>(), 1, 25, 40));
        npcLoot.Add(ItemDropRule.ByCondition(notExpert, ModContent.ItemType<OcramMask>(), 7));
    }

    [JITWhenModsEnabled(ThoriumCompat.THORIUMMODNAME)]
    private void AddThoriumLoot(LeadingConditionRule notExpertRule) {
        notExpertRule.OnSuccess(new OneFromOptionsDropRule(1, 1,
            ModContent.ItemType<EternityStaff>(),
            ModContent.ItemType<DragonBreath>(),
            ModContent.ItemType<OcramsEye>(),
            ModContent.ItemType<Tizona>(),
            ModContent.ItemType<ScytheFantasma>(),
            ModContent.ItemType<SpineCracker>()));
    }
}
