// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Linq;
using System.Text;

namespace fCraft {
    /// <summary> Represents a connection to a Minecraft client. Handles initial handshake between a client and a server. </summary>
    public sealed partial class Player {

        void GentlyKickSMPClients() {
            // This may be someone connecting with an SMP client
            string premiumKickMessage = "§EPlease join us at §9http://classicube.net/";
            // send SMP KICK packet
            writer.Write((byte)255);
            byte[] stringData = Encoding.BigEndianUnicode.GetBytes(premiumKickMessage);
            writer.Write((short)premiumKickMessage.Length);
            writer.Write(stringData);
            BytesSent += (1 + stringData.Length);
            writer.Flush();
            Logger.Log(LogType.Warning, "Player.LoginSequence: A player tried connecting with Minecraft Beta client from {0}.", IP);
        }

        void SMPPing() {
            string servername = Chat.ReplacePercentColorCodes(ConfigKey.ServerName.GetString(), false).Replace('&', '§');
            string premiumPingMotd = "§1" + '\0' + "78" + '\0' + "0.30c" + '\0' + "§E" + servername + '\0' + Server.CountPlayers(false) + '\0' + ConfigKey.MaxPlayers.GetInt();
            // send SMP KICK packet
            writer.Write((byte)255);
            byte[] stringData = Encoding.BigEndianUnicode.GetBytes(premiumPingMotd);
            writer.Write((short)premiumPingMotd.Length);
            writer.Write(stringData);
            BytesSent += (1 + stringData.Length);
            writer.Flush();
        }


        bool NegotiateProtocolExtension() {
            // write our ExtInfo and ExtEntry packets
            writer.Write(Packet.MakeExtInfo("ProCraft", 24).Bytes);
            writer.Write(Packet.MakeExtEntry(ClickDistanceExtName, 1).Bytes);
            writer.Write(Packet.MakeExtEntry(CustomBlocksExtName, 1).Bytes);
            writer.Write(Packet.MakeExtEntry(HeldBlockExtName, 1).Bytes);
            
            writer.Write(Packet.MakeExtEntry(TextHotKeyExtName, 1).Bytes);
            writer.Write(Packet.MakeExtEntry(ExtPlayerListExtName, 1).Bytes);
            writer.Write(Packet.MakeExtEntry(EnvColorsExtName, 1).Bytes);
            
            writer.Write(Packet.MakeExtEntry(SelectionCuboidExtName, 1).Bytes);
            writer.Write(Packet.MakeExtEntry(BlockPermissionsExtName, 1).Bytes);
            writer.Write(Packet.MakeExtEntry(ChangeModelExtName, 1).Bytes);
            
            writer.Write(Packet.MakeExtEntry(EnvMapAppearanceExtName, 1).Bytes);
            writer.Write(Packet.MakeExtEntry(EnvWeatherTypeExtName, 1).Bytes);
            writer.Write(Packet.MakeExtEntry(HackControlExtName, 1).Bytes);
            
            writer.Write(Packet.MakeExtEntry(ExtPlayerListExtName, 2).Bytes);
            writer.Write(Packet.MakeExtEntry(PlayerClickExtName, 1).Bytes);
            writer.Write(Packet.MakeExtEntry(MessageTypesExtName, 1).Bytes);
            
            writer.Write(Packet.MakeExtEntry(EmoteFixExtName, 1).Bytes);
            writer.Write(Packet.MakeExtEntry(LongerMessagesExtName, 1).Bytes);
            writer.Write(Packet.MakeExtEntry(FullCP437ExtName, 1).Bytes);
            
            writer.Write(Packet.MakeExtEntry(BlockDefinitionsExtName, 1).Bytes);
            writer.Write(Packet.MakeExtEntry(BlockDefinitionsExtExtName, 2).Bytes);
            writer.Write(Packet.MakeExtEntry(BulkBlockUpdateExtName, 1).Bytes);
            
            writer.Write(Packet.MakeExtEntry(TextColorsExtName, 1).Bytes);
            writer.Write(Packet.MakeExtEntry(EnvMapAspectExtName, 1).Bytes);
            // Fix for ClassiCube Client which violates the spec -
            // If server supports version > 1 but client version 1, client should reply with version 1.
            // ClassiCube just doesn't reply at all in that case.
            writer.Write(Packet.MakeExtEntry(EnvMapAppearanceExtName, 2).Bytes);
            
            // Expect ExtInfo reply from the client
            OpCode extInfoReply = reader.ReadOpCode();
            //Logger.Log(LogType.Debug, "Expected: {0} / Received: {1}", OpCode.ExtInfo, extInfoReply );
            if (extInfoReply != OpCode.ExtInfo) {
                Logger.Log(LogType.Debug, "Player {0}: Unexpected ExtInfo reply ({1})", Info.Name, extInfoReply);
                return false;
            }
            ClientName = reader.ReadString();
            int expectedEntries = reader.ReadInt16();

            // wait for client to send its ExtEntries
            for (int i = 0; i < expectedEntries; i++) {
                // Expect ExtEntry replies (0 or more)
                OpCode extEntryReply = reader.ReadOpCode();
                if (extEntryReply != OpCode.ExtEntry) {
                    Logger.Log(LogType.Warning, "Player {0} from {1}: Unexpected ExtEntry reply ({2})", Name, IP,
                               extInfoReply);
                    return false;
                }
                string extName = reader.ReadString();
                int extVersion = reader.ReadInt32();
                CpeExt addedExt = CpeExt.None;
                
                switch (extName) {
                    case CustomBlocksExtName:
                        if (extVersion == 1)
                            addedExt = CpeExt.CustomBlocks;
                        break;
                    case BlockPermissionsExtName:
                        if (extVersion == 1)
                            addedExt = CpeExt.BlockPermissions;
                        break;
                    case ClickDistanceExtName:
                        if (extVersion == 1) {
                            addedExt = CpeExt.ClickDistance;
                        }
                        break;
                    case EnvColorsExtName:
                        if (extVersion == 1)
                            addedExt = CpeExt.EnvColors;
                        break;
                    case ChangeModelExtName:
                        if (extVersion == 1)
                            addedExt = CpeExt.ChangeModel;
                        break;
                    case EnvMapAppearanceExtName:
                        if (extVersion == 1) {
                            addedExt = CpeExt.EnvMapAppearance;
                        } else if (extVersion == 2) {
                            addedExt = CpeExt.EnvMapAppearance2;
                        }
                        break;
                    case EnvWeatherTypeExtName:
                        if (extVersion == 1)
                            addedExt = CpeExt.EnvWeatherType;
                        break;
                    case HeldBlockExtName:
                        if (extVersion == 1)
                            addedExt = CpeExt.HeldBlock;
                        break;
                    case ExtPlayerListExtName:
                        if (extVersion == 1) {
                            addedExt = CpeExt.ExtPlayerList;
                            if (Supports(CpeExt.ExtPlayerList2)) {
                                addedExt = CpeExt.ExtPlayerList2;
                            }
                        } else if (extVersion == 2) {
                            addedExt = CpeExt.ExtPlayerList2;
                            if (Supports(CpeExt.ExtPlayerList)) {
                                supportedExts.Remove(CpeExt.ExtPlayerList);
                            }
                        }
                        break;
                    case SelectionCuboidExtName:
                        if (extVersion == 1)
                            addedExt = CpeExt.SelectionCuboid;
                        break;
                    case MessageTypesExtName:
                        if (extVersion == 1)
                            addedExt = CpeExt.MessageType;
                        break;
                    case HackControlExtName:
                        if (extVersion == 1)
                            addedExt = CpeExt.HackControl;
                        break;
                    case EmoteFixExtName:
                        if (extVersion == 1)
                            addedExt = CpeExt.EmoteFix;
                        break;
                    case TextHotKeyExtName:
                        if (extVersion == 1)
                            addedExt = CpeExt.TextHotKey;
                        break;
                    case PlayerClickExtName:
                        if (extVersion == 1)
                            addedExt = CpeExt.PlayerClick;
                        break;
                    case LongerMessagesExtName:
                        if (extVersion == 1)
                            addedExt = CpeExt.LongerMessages;
                        break;
                    case FullCP437ExtName:
                        if (extVersion == 1)
                            addedExt = CpeExt.FullCP437;
                        break;
                    case BlockDefinitionsExtName:
                        if (extVersion == 1)
                            addedExt = CpeExt.BlockDefinitions;
                        break;
                    case BlockDefinitionsExtExtName:
                        if (extVersion == 1)
                            addedExt = CpeExt.BlockDefinitionsExt;
                        else if (extVersion == 2)
                            addedExt = CpeExt.BlockDefinitionsExt2;
                        break;
                    case BulkBlockUpdateExtName:
                        if (extVersion == 1)
                            addedExt = CpeExt.BulkBlockUpdate;
                        break;
                    case TextColorsExtName:
                        if (extVersion == 1)
                            addedExt = CpeExt.TextColors;
                        break;
                    case EnvMapAspectExtName:
                        if (extVersion == 1)
                            addedExt = CpeExt.EnvMapAspect;
                        break;
                }
                if (addedExt != CpeExt.None)
                    supportedExts.Add(addedExt);
            }
            supportsCustomBlocks = Supports(CpeExt.CustomBlocks);
            supportsBlockDefs = Supports(CpeExt.BlockDefinitions);

            // log client's capabilities
            if (supportedExts.Count > 0) {
                Logger.Log(LogType.Debug, "Player {0} is using \"{1}\", supporting: {2}",
                           Info.Name, ClientName, supportedExts.JoinToString(", "));
            } else if (supportedExts.Count == 0) {
                Kick("Please use the ClassicalSharp client", LeaveReason.InvalidOpcodeKick);
            }

            // if client also supports CustomBlockSupportLevel, figure out what level to use

            // Send CustomBlockSupportLevel
            writer.Write(Packet.MakeCustomBlockSupportLevel(CustomBlocksLevel).Bytes);
            
            if (Supports(CpeExt.TextColors)) {
                for (int i = 0; i < Color.ExtColors.Length; i++) {
                    if (Color.ExtColors[i].Undefined) continue;
                    writer.Write(Packet.MakeSetTextColor(Color.ExtColors[i]).Bytes);
                }
            }

            // Expect CustomBlockSupportLevel reply
            OpCode customBlockSupportLevelReply = reader.ReadOpCode();
            //Logger.Log( LogType.Debug, "Expected: {0} / Received: {1}", OpCode.CustomBlockSupportLevel, customBlockSupportLevelReply );
            if (customBlockSupportLevelReply != OpCode.CustomBlockSupportLevel) {
                Logger.Log(LogType.Warning, "Player {0} from {1}: Unexpected CustomBlockSupportLevel reply ({2})",
                           Info.Name, IP, customBlockSupportLevelReply);
                return false;
            }
            byte clientLevel = reader.ReadByte();
            //UsesCustomBlocks = (clientLevel >= CustomBlocksLevel);
            return true;
        }
    }
}