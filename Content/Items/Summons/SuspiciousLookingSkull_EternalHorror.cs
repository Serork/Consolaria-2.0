using Consolaria.Content.NPCs.Bosses.EternalHorror;

using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Consolaria.Content.Items.Summons {
    public class SuspiciousLookingSkull_EternalHorror : ModItem {
        public static Condition ThisItemCondition => new Condition(Language.GetOrRegister("Mods.Consolaria.EternalHorrorSummonCondition"), () => !EternalHorror.CanEternalHorrorExistInAWorld);

        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 3;
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 33;

            //ItemVariants.AddVariant(Type, ItemVariants.EnabledVariant, ThisItemCondition);
        }

        public override void Load() {
            On_Recipe.PlayerMeetsEnvironmentConditions += On_Recipe_PlayerMeetsEnvironmentConditions;
            On_Recipe.AddToAvailableRecipes += On_Recipe_AddToAvailableRecipes;
            On_Recipe.CollectGuideRecipes += On_Recipe_CollectGuideRecipes;
        }

        private void On_Recipe_CollectGuideRecipes(On_Recipe.orig_CollectGuideRecipes orig) {
            orig();

            int numAvailableRecipes = 0;

            for (int i = 0; i < Main.availableRecipe.Length; i++) {
                int j = Main.availableRecipe[i];
                if (j <= 0) {
                    continue;
                }
                Recipe recipe = Main.recipe[j];
                if (recipe.createItem.type == ModContent.ItemType<SuspiciousLookingSkull_EternalHorror>() && !EternalHorror.CanEternalHorrorExistInAWorld) {
                    continue;
                }
                if (recipe.createItem.type == ModContent.ItemType<SuspiciousLookingSkull>() && EternalHorror.CanEternalHorrorExistInAWorld) {
                    continue;
                }

                Main.availableRecipe[numAvailableRecipes++] = j;
            }

            Main.numAvailableRecipes = numAvailableRecipes;
        }

        private void On_Recipe_AddToAvailableRecipes(On_Recipe.orig_AddToAvailableRecipes orig, int recipeIndex) {
            orig(recipeIndex);
        }

        private bool On_Recipe_PlayerMeetsEnvironmentConditions(On_Recipe.orig_PlayerMeetsEnvironmentConditions orig, Player player, Recipe tempRec) {
            bool result = orig(player, tempRec);

            if (tempRec.createItem.type == ModContent.ItemType<SuspiciousLookingSkull_EternalHorror>()) {
                return result && EternalHorror.CanEternalHorrorExistInAWorld;
            }
            if (tempRec.createItem.type == ModContent.ItemType<SuspiciousLookingSkull>()) {
                return result && !EternalHorror.CanEternalHorrorExistInAWorld;
            }

            return result;
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

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            if (!EternalHorror.CanEternalHorrorExistInAWorld) {
                int index = tooltips.FindIndex(x => x.Name == "Tooltip0");
                if (index != -1) {
                    tooltips[index].Text = Language.GetTextValue("Mods.Consolaria.CantUseSusSkull");
                    index = tooltips.FindIndex(x => x.Name == "Consumable");
                    if (index != -1) {
                        tooltips[index].Hide();
                    }
                }
            }
        }

        public override bool CanUseItem(Player player)
            => EternalHorror.CanEternalHorrorExistInAWorld && !EternalHorrorSummonHandler.EternalHorrorSummonStarted && !EternalHorrorSummonHandler.EternalHorrorSummonEnded && !NPC.AnyNPCs(ModContent.NPCType<EternalHorror>());

        public override bool? UseItem(Player player) {
            int type = ModContent.NPCType<EternalHorror>();

            int targetPlayerIndex = player.whoAmI;

            if (!Helper.IsClient()) {
                int num = 200;
                num = NPC.NewNPC(player.GetSource_ItemUse(Item), (int)player.Center.X, (int)player.Center.Y, type, 1);

                if (num != 200) {
                    Main.npc[num].target = targetPlayerIndex;
                    Main.npc[num].timeLeft *= 20;
                    string typeName = Main.npc[num].TypeName;
                    if (Main.netMode == 2 && num < 200)
                        NetMessage.SendData(23, -1, -1, null, num);
                }
            }

            //if (Main.netMode != NetmodeID.MultiplayerClient) {
            //    //int num9 = NPC.NewNPC(NPC.GetBossSpawnSource(player.whoAmI), (int)player.Center.X, (int)player.Center.Y, type);
            //    //Main.npc[num9].target = player.whoAmI;
            //    //string typeName2 = Main.npc[num9].TypeName;
            //    //if (Main.netMode == 0)
            //    //    Main.NewText(Language.GetTextValue("Announcement.HasAwoken", typeName2), 175, 75);
            //    //else if (Main.netMode == 2)
            //    //    ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Announcement.HasAwoken", Main.npc[num9].GetTypeNetName()), new Color(175, 75, 255));
            //}

            return base.UseItem(player);
        }
    }
}