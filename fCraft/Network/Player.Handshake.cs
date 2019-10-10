// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace fCraft {
    /// <summary> Represents a connection to a Minecraft client. Handles initial handshake between a client and a server. </summary>
    public sealed partial class Player {

        bool HandleOpcode(byte opcode) {
            switch (opcode) {
                case (byte)OpCode.Handshake:
                    return true;

                case 0xFE: // SMP ping packet id
                    // 1.4 - 1.6
                    string name = Chat.ReplacePercentColorCodes(ConfigKey.ServerName.GetString(), false).Replace('&', '§');
                    string data = "§1\078\00.30c\0§E" + name + '\0' + Server.CountPlayers(false) + '\0' + ConfigKey.MaxPlayers.GetInt();
                    SendOldSMPKick(data);
                    return false;

                case 0x02:
                case 0xFA:
                    SendOldSMPKick("§EPlease join us at §9http://classicube.net/");
                    Logger.Log(LogType.Warning, "Player.LoginSequence: A player tried connecting with Minecraft Beta client from {0}.", IP);
                    // ignore SMP pings
                    return false;

                case (byte)'G': // WoM GET requests
                    return false;

                default:
                    if (CheckModernSMP(opcode)) return false;
                    Logger.Log(LogType.Error, "Player.LoginSequence: Unexpected op code in the first packet from {0}: {1}.", IP, opcode);
                    KickNow("Incompatible client, or a network error.", LeaveReason.ProtocolViolation);
                    return false;
            }
        }
        
        #region SMP
        
        bool CheckModernSMP(byte length) {
            // Ensure that we have enough bytes for packet data
            if (client.Available < length || reader.ReadByte() != 0) return false;
            
            int protocolVer = ReadVarInt(); // protocol version
            int hostLen = ReadVarInt();
            if (protocolVer == -1 || hostLen == -1) return false;
            
            if (client.Available < hostLen) return false;
            reader.ReadBytes(hostLen); // hostname
            
            if (client.Available < 3) return false;
            reader.ReadInt16(); // port
            byte nextState = reader.ReadByte();
            
            string data = null;
            if (nextState == 1) { // status state
                string name = Chat.ReplacePercentColorCodes(ConfigKey.ServerName.GetString(), false).Replace('&', '§');
                
                data = @"{""version"": { ""name"": ""0.30c"", ""protocol"": " + protocolVer + @" }, ""players"": " + 
                    ServiceStack.Text.JsonSerializer.SerializeToString(new Players()) + @", ""description"": {""text"": ""§6" + 
                    name + "\n§EPlease join us at §9http://ClassiCube.net/" + @"""},""favicon"": """ + CheckForFavicon() + @"""}";
            } else if (nextState == 2) { // game state
                data = @"{""text"": ""§EPlease join us at §9http://classicube.net/""}";
                Logger.Log(LogType.Warning, "Player.LoginSequence: A player tried connecting with Minecraft premium client from {0}.", IP);
            }
            if (data == null) return false;
            
            int strLength = Encoding.UTF8.GetByteCount(data);
            int strLength_S = VarIntBytes(strLength);
            int dataLength = 1 + strLength_S + strLength;
            int dataLength_S = VarIntBytes(dataLength);
            
            byte[] packet = new byte[dataLength_S + dataLength];
            WriteVarInt(dataLength, packet, 0);
            packet[dataLength_S] = 0; // opcode
            WriteVarInt(strLength, packet, dataLength_S + 1);
            Encoding.UTF8.GetBytes(data, 0, data.Length, packet, strLength_S + dataLength_S + 1);
            
            writer.Write(packet);
            BytesSent += packet.Length;
            if (IP.IsLocal()) System.Threading.Thread.Sleep(10); //localhost gets disconnected too quickly causing connection error client side
            writer.Flush();
            return true;
        }


        public string CheckForFavicon() {
            string favicon = ImageFromFavicon();
            if (favicon != null) return favicon;
            
            // Base64 encoding of http://123DMWM.com/I/299.png;
            return "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAAABGdBTUEAALGPC/xhBQAAAAlwSFlzAAAOwgAADsI" +
                   "BFShKgAAAABl0RVh0U29mdHdhcmUAcGFpbnQubmV0IDQuMC4xMK0KCsAAAAoJSURBVHhe7ZoJcFVXHcYf27CWpZ2OzrS2Tkft1KpTR6u12mrV" +
                   "Wu02ShdrVSwudFoHqZbF0BSEkLAGSMIOBcK+rwUKlFUoYMAGyjphCWEPi8O+c/x+592TuWYCPW8LCeN/5jdvybnnnO87/7Pc+xIxHhGJRCqdm" +
                   "jVrmrp165rGjRubhg0bBj1JflRZAyqifv36pl69ekGvkhPVygBHnTp1TO3atYPeJRbV0gBHMrKhWhsAZEMiUe0NgERMuCUMgHhNSIoBqQjqrV" +
                   "GjhqV8e9eDsrFGlTUgHAirVatWhW2HuWUNcME5oKL2w8RqQrUygGCuV9QHxy1vAMfjivoQJpaodgYQyTShWhrADVJF/QjjG9XSAKKifoTxjf8" +
                   "bELzeMJqqQmgsGomGAQ0CbkY0U7vX61N94RteJT+nCuEu8VnxGXFniJsRN+rTHcI3vEo+oArhS+IL4j7xeXFvwM2IL6tdKN+ne8Tdwje8Sn5T" +
                   "FcLXxdfEVwSN3x9Q2dG8USPzsNoN9+lBwSDRH4zwDa+S31eF8D3xiPiWcI1DZUeL224zP1C7j4tHxbeF689DgmnhG14lf6YK4SfiR+IJQePfD" +
                   "ajseFbH3WfU7k/FjwVmMDjfEQxQ0jPgBVUIPxfPiaeFMwMqM9o1aWJeUpuuP8+KpwT9wAgyNZbwKv1bVQqvil+KF8UvxPMBlRVdmjUzv1N78B" +
                   "vxiqAv9IFBIUtZC2IJr9KtVCn8QbwmWgg68OuAyohet99u3lBb8LqgLxjBoLgBgVjD64q3VDG0Fm8K1xHe/1mkMvo3bWo66hb4b2oH/ipcPxg" +
                   "UBgQTfiXYDWINryvSVDG0F28HtBMdxN9FP92c5GhlztH2FAu5ugbyAviuT4MGJkOC39FCR5sdxTvBK23RLkb8RTAIZEJLwboQT3hd1VWVwz/E" +
                   "uyJddAo+831GgCuXDFydrl7aok2MwAQykuxzU+JJEU94XdVblUNP0V1kBa89gu9SCW3QXjeBCZhP5pGFbmo+JuINrysHqAHIFf1FvxB8Thauz" +
                   "r4iW/QRvQQmZIouggx0U7Kt4Bxw9erVuPEy4D01AsPFUDFEDBaDxMAkgsl5AjOcCRhAtmEA0wET4GVx5syZhPEyYLwagzEiX4wSGIIZGFGRmH" +
                   "hAfI5APKMfnnqAGX8SzcXmzZuTQlwGjBSId51lxHjPFEkE6mAKMPLg3jPfOe2NHz++jAkTJiQFLwNmqHGYJqaICYIsYArQabYl5iOrc7ywrf1" +
                   "ekNoccdnX58yZY4YMGWKys7NNr169TO/eve37fv36mZycnKTgZcA8dQbep1MCM8iIEYJMSEZcu3bNXLx40Rw7dsxs2rTJzJ492wwePNj07NnT" +
                   "ZGZmWnifm5trRo0aZUdv8uTJCePV++USCUvFh+IDMVOQCRiRaPiKHzRokJk+fbpZs2aN2bJli9mxY0fCePV+jUQ6VgvMwAQ3NRKJTxPfrVu3M" +
                   "vGzZs0yhYWF5siRI3YFP3/+fMJ49f5jiYQNYr3ACLLBTY14I1bxGzduNEePHrXXsIdzfaJ49X6bRMJWsUUUio8E0wHiCRqPVzzXJiu8er9HIh" +
                   "27xQ5BRqwKiDWqinjCq/eHJBIOBuwVZENBQCxRVcRTF3j1/j8SGaZUkA2fBPhGIuKTNeeBui5fvuy/CJ6XyDCnBJlQFOATNHw98QiH7t27m7y" +
                   "8PLvVbdiwwRw6dMicPXvWXLp0yXY4EajjwoUL5ty5c+bkyZN2J9m9e7efAaaGijkk+JI4IUoCPi18xJMBnPJGjx5tlixZYrZu3Wr2799vDh8+" +
                   "bDubCNRBXXv27LF7P1vpypUr7UnTz4A7VQzuEE1lQBMZ0FjiA24UPuIhKyvL9O3b157yKIMJK1assB1NlOXLl5vFixfbeqdMmWLGjh1rhg8fb" +
                   "k+VfgZ8UcXgPnGvDLhHBtwt8XdFuV74igcygO85n1Nm2LBhFjqaKNRDnQju37+/vZdw9xN+BnxDxeAh8VUZ8KAMeEDi749SUdxIvFvweGXkmf" +
                   "vQo0ePspueeOnTp4+FbHJCEY14FlfMcPcSM2fO9DTgCRWDx8SjMuARGfCwxMsUKB8+4hHMnB8wYIC94xs6dGjCuIwZOXKkGTdunJk4caK94Zk" +
                   "6dardVRYuXGinw9q1a22/du7c6WnAcyoGz4inZMCTMuCHEv94lHD4iOc9whmF+fPnm6VLl9qOJQPmPALZQrdt22YXvaKiIrviu0X1+PHj5tSp" +
                   "U/7boHlVxeAV8ZIMaC4Dnpf4p6O48BVPKpJ+BQUFdmU+ePCg7VgyYNV3AtnyEMn2B247vXLlStm5ws+A11UM/ihayoAWMkCGlLwchYhFvDvkl" +
                   "JaW2k4mY58PExZYEeHwM+BtFYM2orUMeEMGyIyS16JQaaziU3HCc8QSfgZ0UjFIF2kyoJ0MeEvi34wSr/hYO5uK8DOgh4pBpsiQAZ1lgIwoUV" +
                   "ZAdRVP+BmQp2KQI/rKgN4yoJvEvxsFYQhEKHs5wjMyMuz7gQMH2gWP4ydznsUoFWkfJpbwM+A9FYPhYqgMGCgDsiU+K0pVEg+xhJ8Bk1UMJor" +
                   "xMiBfBgyT+Nwo5cVzuuPkxQFk/fr15sCBA2X7LukPbmtK9DM7SHlTYwk/A95XMZgjZsmAaTJgnMQrI8CJ79q1a9noczKbN2+eHX1OXBxGeC0p" +
                   "KbFwMOEMsG/fPrN37177t127dtm/uc+uPJ8pX1xcbM3kGsrymfUHI8ImxBJ+BixVMfhQLJIB82XADIlXNoAbefeKESx+I0aMsFNgxowZ9tccj" +
                   "qZz5861MG0WLFhg79J4z98nTZpkb1F5HjBmzBh7UuQziyv1cJRdtWqVWbRokZk2bZr9zEmP7GLvT50B61QM1oo1MmClDFgo8coGQPTRrRFz+W" +
                   "P9XUa1bds2umtweNIRmh2BZwk8TeJJEsdgnibzmwI/iGIc/+3BPz7xT05paWn/83+/ZFT4M2d9bnAwavv27eb06dOpNQBhF+HfYkPEnJERpcs" +
                   "ipnhBxOz9INpBDGDeY0CbNm2sAVa4DOjYsaM1AGGI5aYFAxhZDMAwDMBIDGjZsqUV2rp1a9OhQwd7jf0VR98tW7bMPjTJz8+3Z34WV9aElE6B" +
                   "059o9GTAcYmH0n9p5P8ZMTsltnhFxD7GchnQpUsX0759e2sA0wADEIEB6enpdnrQeZcBmIcx4Qxo1apV2WhjCrev69ats5+54cEMjODmpvz8T" +
                   "4kBiNtfqNGWeCgukHhNhaKPZIQ+M7cpwysZYIXLAHYDDLCGyIDOnTubTp06RRdIiWHOkwE8A8AAjMQATEQs9/KsBatXr7aLId/xpIg7SB6ZlU" +
                   "/91BmwO2IObJf4LQGbZYKyokTvDxdF7GKEASxuGECKhtcABGGAWwM4H2AA2yQGkBEYwKMqDGCOIxaDGHFW+xMnTtjv3O0ud5DlUz92A4z5L9j" +
                   "2II10x3D2AAAAAElFTkSuQmCC";
        }
        
        static string ImageFromFavicon() {
            if (!File.Exists("server-icon.png")) return null;
            
            using (Image image = Image.FromFile("server-icon.png")) {
                if (image.Width != 64 || image.Height != 64) return null;
                
                using (MemoryStream ms = new MemoryStream()) {
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    byte[] data = ms.ToArray();
                    return "data:image/png;base64," + Convert.ToBase64String(data);
                }
            }
        }

        public class Sample {
            public string name { get; set; }
            public string id { get; set; }

            public Sample(string Name, string Id) {
                name = Name;
                id = Id;
            }
        }

        public class Players {
            public int max { get { return ConfigKey.MaxPlayers.GetInt(); } }
            public int online { get { return Server.CountPlayers(false); } }
            public List<Sample> sample {
                get {
                    List<Sample> players = new List<Sample>();
                    foreach (Player p in Server.Players.Where(p => !p.Info.IsHidden)) {
                        players.Add(new Sample(p.Name, "00000000-0000-3000-0000-000000000000"));
                    }
                    return players;
                }
            }
        }

        void SendOldSMPKick(string data) {
            // send SMP KICK packet
            byte[] packet = new byte[3 + data.Length * 2];
            packet[0] = 255; // kick opcode
            Packet.WriteI16((short)data.Length, packet, 1);
            Encoding.BigEndianUnicode.GetBytes(data, 0, data.Length, packet, 3);
            
            writer.Write(packet);
            BytesSent += packet.Length;
            writer.Flush();
        }
        
        
        int ReadVarInt() {
            int shift = 0, result = 0;
            while (shift < 32) {
                if (client.Available == 0) return -1; // out of data
                
                byte part = reader.ReadByte();
                result |= (part & 0x7F) << shift;
                if ((part & 0x80) == 0) return result;
                shift += 7;
            }
            return -1; // varint too big
        }
        
        static int VarIntBytes(int value) {
            int count = 1;
            while ((value >>= 7) > 0) count++;
            return count;
        }
                
        static void WriteVarInt(int value, byte[] buffer, int offset) {
            do {
                byte part = (byte)(value & 0x7F);
                value >>= 7;
                if (value > 0) part |= 0x80;

                buffer[offset] = part; offset++;
            } while (value > 0);
        }

        #endregion


        #region CPE
        
        bool NegotiateProtocolExtension() {
            // write our ExtInfo and ExtEntry packets
            writer.Write(Packet.MakeExtInfo("ProCraft", 30).Bytes);
            
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
            writer.Write(Packet.MakeExtEntry(ExtPlayerPositionsExtName, 1).Bytes);
            
            writer.Write(Packet.MakeExtEntry(EntityPropertyExtName, 1).Bytes);
            writer.Write(Packet.MakeExtEntry(TwoWayPingExtName, 1).Bytes);
            writer.Write(Packet.MakeExtEntry(InventoryOrderExtName, 1).Bytes);
            
            writer.Write(Packet.MakeExtEntry(InstantMOTDExtName, 1).Bytes);
            writer.Write(Packet.MakeExtEntry(FastMapExtName, 1).Bytes);
            
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
                int version = reader.ReadInt32();
                CpeExt ext = CpeExt.None;
                
                switch (extName) {
                    case CustomBlocksExtName:
                        if (version == 1) ext = CpeExt.CustomBlocks;
                        break;
                    case BlockPermissionsExtName:
                        if (version == 1) ext = CpeExt.BlockPermissions;
                        break;
                    case ClickDistanceExtName:
                        if (version == 1) ext = CpeExt.ClickDistance;
                        break;
                    case EnvColorsExtName:
                        if (version == 1) ext = CpeExt.EnvColors;
                        break;
                    case ChangeModelExtName:
                        if (version == 1) ext = CpeExt.ChangeModel;
                        break;                        
                    case EnvMapAppearanceExtName:
                        if (version == 1) ext = CpeExt.EnvMapAppearance;
                        if (version == 2) ext = CpeExt.EnvMapAppearance2;
                        break;                      
                    case EnvWeatherTypeExtName:
                        if (version == 1) ext = CpeExt.EnvWeatherType;
                        break;
                    case HeldBlockExtName:
                        if (version == 1) ext = CpeExt.HeldBlock;
                        break;
                        
                    case ExtPlayerListExtName:
                        if (version == 1) {
                            ext = CpeExt.ExtPlayerList;
                            if (Supports(CpeExt.ExtPlayerList2)) {
                                ext = CpeExt.ExtPlayerList2;
                            }
                        } else if (version == 2) {
                            ext = CpeExt.ExtPlayerList2;
                            if (Supports(CpeExt.ExtPlayerList)) {
                                supportedExts.Remove(CpeExt.ExtPlayerList);
                            }
                        }
                        break;
                    case SelectionCuboidExtName:
                        if (version == 1) ext = CpeExt.SelectionCuboid;
                        break;
                    case MessageTypesExtName:
                        if (version == 1) ext = CpeExt.MessageType;
                        break;
                    case HackControlExtName:
                        if (version == 1) ext = CpeExt.HackControl;
                        break;
                    case EmoteFixExtName:
                        if (version == 1) ext = CpeExt.EmoteFix;
                        break;
                    case TextHotKeyExtName:
                        if (version == 1) ext = CpeExt.TextHotKey;
                        break;
                    case PlayerClickExtName:
                        if (version == 1) ext = CpeExt.PlayerClick;
                        break;
                    case LongerMessagesExtName:
                        if (version == 1) ext = CpeExt.LongerMessages;
                        break;
                    case FullCP437ExtName:
                        if (version == 1) ext = CpeExt.FullCP437;
                        break;
                    case BlockDefinitionsExtName:
                        if (version == 1) ext = CpeExt.BlockDefinitions;
                        break;
                    case BlockDefinitionsExtExtName:
                        if (version == 1) ext = CpeExt.BlockDefinitionsExt;
                        if (version == 2) ext = CpeExt.BlockDefinitionsExt2;
                        break;
                    case BulkBlockUpdateExtName:
                        if (version == 1) ext = CpeExt.BulkBlockUpdate;
                        break;
                    case TextColorsExtName:
                        if (version == 1) ext = CpeExt.TextColors;
                        break;
                    case EnvMapAspectExtName:
                        if (version == 1) ext = CpeExt.EnvMapAspect;
                        break;
                    case ExtPlayerPositionsExtName:
                        if (version == 1) ext = CpeExt.ExtPlayerPositions;
                        supportsExtPositions = true;
                        break;
                    case EntityPropertyExtName:
                        if (version == 1) ext = CpeExt.EntityProperty;
                        break;
                    case TwoWayPingExtName:
                        if (version == 1) ext = CpeExt.TwoWayPing;
                        break;
                    case InventoryOrderExtName:
                        if (version == 1) ext = CpeExt.InventoryOrder;
                        break;
                    case InstantMOTDExtName:
                        if (version == 1) ext = CpeExt.InstantMOTD;
                        break;
                    case FastMapExtName:
                        if (version == 1) ext = CpeExt.FastMap;
                        break;
                }
                if (ext != CpeExt.None)
                    supportedExts.Add(ext);
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

            return true;
        }
        #endregion
    }
}