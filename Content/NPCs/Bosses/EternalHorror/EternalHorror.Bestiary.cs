using System.Collections.Generic;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Consolaria.Content.NPCs.Bosses.EternalHorror;

sealed partial class EternalHorror : ModNPC {
    public static LocalizedText BestiaryText {
        get; private set;
    }

    private partial void SetBestiaryInfo() {
        NPCID.Sets.BossBestiaryPriority.Add(Type);
    }

    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
        bestiaryEntry.Info.AddRange([
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
                new FlavorTextBestiaryInfoElement(BestiaryText.ToString())
            ]);
    }
}
