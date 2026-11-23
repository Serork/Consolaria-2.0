using Consolaria.Common;
using Consolaria.Content.NPCs.Bosses.EternalHorror;

using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Content.Items.Summons {
    public class SuspiciousLookingSkull_EternalHorror : ModItem {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 3;
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 33;
        }

        public override void SetDefaults() {
            int width = 18; int height = 22;
            Item.Size = new Vector2(width, height);

            Item.maxStack = Item.CommonMaxStack;

            Item.value = Item.sellPrice(gold: 10);
            Item.rare = ItemRarityID.Red;

            Item.useAnimation = 45;
            Item.useTime = 45;

            Item.useStyle = ItemUseStyleID.HoldUp;
            //Item.consumable = true;
        }

        public override bool CanUseItem(Player player)
            => EternalHorror.CanEternalHorrorExistInAWorld && !EternalHorrorSummonHandler.EternalHorrorSummonStarted && !EternalHorrorSummonHandler.EternalHorrorSummonEnded && !NPC.AnyNPCs(ModContent.NPCType<EternalHorror>());

        public override bool? UseItem(Player player) {
            int type = ModContent.NPCType<EternalHorror>();

            if (Main.netMode != NetmodeID.MultiplayerClient) {
                int num9 = NPC.NewNPC(NPC.GetBossSpawnSource(player.whoAmI), (int)player.Center.X, (int)player.Center.Y, type);
                Main.npc[num9].target = player.whoAmI;
                string typeName2 = Main.npc[num9].TypeName;
                //if (Main.netMode == 0)
                //    Main.NewText(Language.GetTextValue("Announcement.HasAwoken", typeName2), 175, 75);
                //else if (Main.netMode == 2)
                //    ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Announcement.HasAwoken", Main.npc[num9].GetTypeNetName()), new Color(175, 75, 255));
            }

            return true;
        }
    }
}