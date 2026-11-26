using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Consolaria.Content.Items.Placeable;

public abstract class TopperItem : ModItem {
    public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Language.GetTextValue("CommonItemTooltip.PlaceableOnXmasTree"));

    public enum TopperItemType {
        Topper505,
        Topper4
    }

    public override void SetStaticDefaults()
        => Item.ResearchUnlockCount = 1;

    public override void SetDefaults()
        => Item.CloneDefaults(ItemID.StarTopper1);

    public virtual TopperItemType TopperType { get; }

    public override bool? UseItem(Player player) {
        if (player.ItemAnimationJustStarted) {
            Item item = player.inventory[player.selectedItem];
            if (!(player.position.X / 16f - (float)Player.tileRangeX - (float)item.tileBoost - (float)player.blockRange <= (float)Player.tileTargetX) || !((player.position.X + (float)player.width) / 16f + (float)Player.tileRangeX + (float)item.tileBoost - 1f + (float)player.blockRange >= (float)Player.tileTargetX) || !(player.position.Y / 16f - (float)Player.tileRangeY - (float)item.tileBoost - (float)player.blockRange <= (float)Player.tileTargetY) || !((player.position.Y + (float)player.height) / 16f + (float)Player.tileRangeY + (float)item.tileBoost - 2f + (float)player.blockRange >= (float)Player.tileTargetY))
                return base.UseItem(player);

            TopperItem topperItem = item.ModItem as TopperItem;

            bool CheckOtherConditions(int x, int y) {
                int num = x;
                int num2 = y;
                if (Main.tile[x, y].TileFrameX < 10) {
                    num -= Main.tile[x, y].TileFrameX;
                    num2 -= Main.tile[x, y].TileFrameY;
                }

                Tile tile = Main.tile[num, num2];
                if (tile.TileFrameY == 5 && topperItem.TopperType == TopperItem.TopperItemType.Topper505) {
                    return false;
                }
                if (tile.TileFrameY == 6 && topperItem.TopperType == TopperItem.TopperItemType.Topper4) {
                    return false;
                }

                return true;
            }

            if (CheckOtherConditions(Player.tileTargetX, Player.tileTargetY)) {
                WorldGen.dropXmasTree(Player.tileTargetX, Player.tileTargetY, 0);

                void setXmasTree(int x, int y) {
                    int num = x;
                    int num2 = y;
                    if (Main.tile[x, y].TileFrameX < 10) {
                        num -= Main.tile[x, y].TileFrameX;
                        num2 -= Main.tile[x, y].TileFrameY;
                    }

                    Main.tile[num, num2].TileFrameY = (short)(topperItem.TopperType == TopperItem.TopperItemType.Topper505 ? 5 : 6);
                }
                setXmasTree(Player.tileTargetX, Player.tileTargetY);

                int x = Player.tileTargetX;
                int y = Player.tileTargetY;
                if (Main.tile[Player.tileTargetX, Player.tileTargetY].TileFrameX < 10) {
                    x -= Main.tile[Player.tileTargetX, Player.tileTargetY].TileFrameX;
                    y -= Main.tile[Player.tileTargetX, Player.tileTargetY].TileFrameY;
                }
                if (Main.netMode == 1) {
                    NetMessage.SendTileSquare(-1, x, y);
                }

                //Player.ApplyItemTime(selectedItem);
                player.ConsumeItem(item.type);
            }
        }

        return base.UseItem(player);
    }
}

public class Topper505 : TopperItem {
    public override TopperItemType TopperType
        => TopperItemType.Topper505;
}

public class StarTopper4 : TopperItem {
    public override TopperItemType TopperType
        => TopperItemType.Topper4;
}

//public class CustomChristmsTreeTopPlacer : ModPlayer {
//    public override void PostUpdate() {
//        bool isInMouse = !Main.mouseItem.IsAir;
//        Item selectedItem = isInMouse ? Main.mouseItem : Player.inventory[Player.selectedItem];

//        bool isItemCustomTopper = selectedItem.ModItem is TopperItem,
//             isTileAChristmasTree = Main.tile[Player.tileTargetX, Player.tileTargetY].HasTile && Main.tile[Player.tileTargetX, Player.tileTargetY].TileType == 171,
//             isTileInRange = !(!(Player.position.X / 16f - (float)Player.tileRangeX - (float)selectedItem.tileBoost - (float)Player.blockRange <= (float)Player.tileTargetX) || !((Player.position.X + (float)Player.width) / 16f + (float)Player.tileRangeX + (float)selectedItem.tileBoost - 1f + (float)Player.blockRange >= (float)Player.tileTargetX) || !(Player.position.Y / 16f - (float)Player.tileRangeY - (float)selectedItem.tileBoost - (float)Player.blockRange <= (float)Player.tileTargetY) || !((Player.position.Y + (float)Player.height) / 16f + (float)Player.tileRangeY + (float)selectedItem.tileBoost - 2f + (float)Player.blockRange >= (float)Player.tileTargetY)),
//             canUseItem = Player.ItemAnimationJustStarted;

//        if (!isItemCustomTopper || !isTileAChristmasTree || !isTileInRange || !canUseItem) {
//            return;
//        }

//        bool CheckOtherConditions(int x, int y) {
//            int num = x;
//            int num2 = y;
//            if (Main.tile[x, y].TileFrameX < 10) {
//                num -= Main.tile[x, y].TileFrameX;
//                num2 -= Main.tile[x, y].TileFrameY;
//            }

//            Tile tile = Main.tile[num, num2];
//            TopperItem topperItem = selectedItem.ModItem as TopperItem;
//            if (tile.TileFrameY == 5 && topperItem.TopperType == TopperItem.TopperItemType.Topper505) {
//                return false;
//            }
//            if (tile.TileFrameY == 6 && topperItem.TopperType == TopperItem.TopperItemType.Topper4) {
//                return false;
//            }

//            return true;
//        }

//        if (CheckOtherConditions(Player.tileTargetX, Player.tileTargetY)) {
//            WorldGen.dropXmasTree(Player.tileTargetX, Player.tileTargetY, 0);

//            void setXmasTree(int x, int y) {
//                int num = x;
//                int num2 = y;
//                if (Main.tile[x, y].TileFrameX < 10) {
//                    num -= Main.tile[x, y].TileFrameX;
//                    num2 -= Main.tile[x, y].TileFrameY;
//                }

//                Main.tile[num, num2].TileFrameY = (short)((selectedItem.ModItem as TopperItem).TopperType == TopperItem.TopperItemType.Topper505 ? 5 : 6);
//            }
//            setXmasTree(Player.tileTargetX, Player.tileTargetY);

//            int x = Player.tileTargetX;
//            int y = Player.tileTargetY;
//            if (Main.tile[Player.tileTargetX, Player.tileTargetY].TileFrameX < 10) {
//                x -= Main.tile[Player.tileTargetX, Player.tileTargetY].TileFrameX;
//                y -= Main.tile[Player.tileTargetX, Player.tileTargetY].TileFrameY;
//            }
//            if (Main.netMode == 1) {
//                NetMessage.SendTileSquare(-1, x, y);
//            }

//            //Player.ApplyItemTime(selectedItem);
//            if (isInMouse) {
//                if (Main.mouseItem.stack > 0) {
//                    if (ItemLoader.ConsumeItem(Main.mouseItem, Player)) {
//                        Main.mouseItem.stack--;

//                        Main.NewText(1);
//                    }

//                    if (Main.mouseItem.stack <= 0)
//                        Main.mouseItem.SetDefaults();
//                }

//                return;
//            }
//            //Player.ConsumeItem(selectedItem.type);
//        }
//    }
//}