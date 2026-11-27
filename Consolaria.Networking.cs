using Consolaria.Content.Items.Armor.Ranged;
using Consolaria.Content.NPCs.Bosses.EternalHorror;
using Consolaria.Content.NPCs.Bosses.Lepus;
using System.IO;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria {
    public partial class Consolaria : Mod {
        internal enum MessageType : byte {
            TitanPower,
            EternalHorrorSummon,
            SpawnChocolateEgg
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI) {
            MessageType msgType = (MessageType)reader.ReadByte();

            switch (msgType) {
                // This message syncs ExampleStatIncreasePlayer.exampleLifeFruits and ExampleStatIncreasePlayer.exampleManaCrystals
                case MessageType.TitanPower: {
                        byte playerNumber = reader.ReadByte();
                        TitanPlayer examplePlayer = Main.player[playerNumber].GetModPlayer<TitanPlayer>();
                        examplePlayer.ReceivePlayerSync(reader);
                        if (Main.netMode == NetmodeID.Server) {
                            // Forward the changes to the other clients
                            examplePlayer.SyncPlayer(-1, whoAmI, false);
                        }
                    }
                    break;
                case MessageType.EternalHorrorSummon: {
                        byte playerNumber = reader.ReadByte();

                        EternalHorrorSummonHandler.PlayerIndexWhoSummon = playerNumber;

                        EternalHorrorSummonHandler.StartSummoningEternalHorror();
                    }
                    break;
                case MessageType.SpawnChocolateEgg: {
                        byte playerNumber = reader.ReadByte();
                        float x = reader.ReadSingle();
                        float y = reader.ReadSingle();

                        int npc = NPC.NewNPC(Main.player[playerNumber].GetSource_FromThis(), (int)x, (int)y, ModContent.NPCType<ChocolateEgg>());
                        NetMessage.SendData(MessageID.SyncNPC, number: whoAmI);
                        if (Main.netMode == NetmodeID.Server && whoAmI < Main.maxNPCs) {
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc, 0f, 0f, 0f, 0, 0, 0);
                        }
                    }
                    break;
                default:
                    Logger.WarnFormat("ExampleMod: Unknown Message type: {0}", msgType);
                    break;
            }
        }
    }
}