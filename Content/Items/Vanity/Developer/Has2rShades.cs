using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Content.Items.Vanity.Developer;

[AutoloadEquip(EquipType.Head)]
sealed class Has2rShades : ModItem {
    public static Lazy<Asset<Texture2D>> helmetGlowmask;

    public override void SetStaticDefaults() {
        ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = true;
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;

        if (!Main.dedServ) {
            helmetGlowmask = new(() => ModContent.Request<Texture2D>(Texture + "_Glow"));
            HeadGlowmask.RegisterData(Item.headSlot, new DrawLayerData() {
                Texture = ModContent.Request<Texture2D>(Texture + "_Head_Glow")
            });
        }
    }

    public override void SetDefaults() {
        int width = 28; int height = 14;
        Item.Size = new Vector2(width, height);

        Item.rare = ItemRarityID.Cyan;
        Item.value = Item.buyPrice(gold: 5);
        Item.vanity = true;
    }

    //public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
    //    => Item.BasicInWorldGlowmask(spriteBatch, helmetGlowmask.Value.Value, new Color(255, 255, 255, 0) * 0.8f, rotation, scale);

    public override bool IsVanitySet(int head, int body, int legs)
       => (head == EquipLoader.GetEquipSlot(Mod, nameof(Has2rMask), EquipType.Head) || head == EquipLoader.GetEquipSlot(Mod, nameof(Has2rShades), EquipType.Head)) &&
          body == EquipLoader.GetEquipSlot(Mod, nameof(Has2rJacket), EquipType.Body) &&
          legs == EquipLoader.GetEquipSlot(Mod, nameof(Has2rPants), EquipType.Legs);

    public override void UpdateVanitySet(Player player) => player.yoraiz0rDarkness = true;

    public override void UpdateArmorSet(Player player) => player.yoraiz0rDarkness = true;
}
