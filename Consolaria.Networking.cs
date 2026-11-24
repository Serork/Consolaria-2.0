using Consolaria.Content.Items.Armor.Ranged;
using Consolaria.Content.NPCs.Bosses.EternalHorror;
using System.IO;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria {
    public partial class Consolaria : Mod {
        internal enum MessageType : byte {
            TitanPower,
            EternalHorrorSummon
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
                default:
                    Logger.WarnFormat("ExampleMod: Unknown Message type: {0}", msgType);
                    break;
            }
        }
    }
}