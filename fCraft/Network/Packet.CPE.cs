// ProCraft Copyright 2014-2018 Joseph Beauvais <123DMWM@gmail.com>

using System;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Packet struct, just a wrapper for a byte array. </summary>
    public partial struct Packet {
        
        [Pure]
        public static Packet MakeExtInfo( string sname, short extCount ) {
            //Logger.Log(LogType.Debug, "Send: ExtInfo({0} {1})", sname, extCount);
            Packet packet = new Packet( OpCode.ExtInfo );
            PacketWriter.WriteString( sname, packet.Bytes, 1, false );
            WriteI16( extCount, packet.Bytes, 65 );
            return packet;
        }


        [Pure]
        public static Packet MakeExtEntry( [NotNull] string name, int version ) {
            //Logger.Log(LogType.Debug, "Send: ExtEntry({0}, {1})", name, version);
            if( name == null ) throw new ArgumentNullException( "name" );
            Packet packet = new Packet( OpCode.ExtEntry );
            PacketWriter.WriteString( name, packet.Bytes, 1, false );
            WriteI32( version, packet.Bytes, 65 );
            return packet;
        }


        [Pure]
        public static Packet MakeSetClickDistance( short distance ) {
            if( distance < 0 ) throw new ArgumentOutOfRangeException( "distance" );
            Packet packet = new Packet( OpCode.SetClickDistance );
            WriteI16( distance, packet.Bytes, 1 );
            return packet;
        }


        [Pure]
        public static Packet MakeCustomBlockSupportLevel( byte level ) {
            Packet packet = new Packet(OpCode.CustomBlockSupportLevel);
            packet.Bytes[1] = level;
            return packet;
        }


        [Pure]
        public static Packet MakeHoldThis(Block block, bool preventChange) {
            Packet packet = new Packet(OpCode.HoldThis);
            packet.Bytes[1] = (byte)block;
            packet.Bytes[2] = (byte)(preventChange ? 1 : 0);
            return packet;
        }


        [Pure]
        public static Packet MakeSetTextHotKey( [NotNull] string label, [NotNull] string action, int keyCode,
                                                byte keyMods, bool hasCP437 ) {
            if( label == null ) throw new ArgumentNullException( "label" );
            if( action == null ) throw new ArgumentNullException( "action" );
            Packet packet = new Packet( OpCode.SetTextHotKey );
            PacketWriter.WriteString( label, packet.Bytes,   1, hasCP437 );
            PacketWriter.WriteString( action, packet.Bytes, 65, hasCP437 );
            WriteI32( keyCode, packet.Bytes, 129 );
            packet.Bytes[133] = keyMods;
            return packet;
        }


        [Pure]
        public static Packet MakeExtAddPlayerName(short nameId, string playerName, string listName, string groupName,
                                                   byte groupRank, bool useFallbacks, bool hasCP437 ) {
            if( playerName == null ) throw new ArgumentNullException( "playerName" );
            if( listName == null ) throw new ArgumentNullException( "listName" );
            if( groupName == null ) throw new ArgumentNullException( "groupName" );
            Packet packet = new Packet( OpCode.ExtAddPlayerName );
            //Logger.Log(LogType.Debug, "Send: MakeExtAddPlayerName({0}, {1}, {2}, {3}, {4})", nameId, playerName, listName, groupName, groupRank);
            WriteI16( nameId, packet.Bytes, 1 );
            PacketWriter.WriteString( Color.SubstituteSpecialColors(playerName, useFallbacks), packet.Bytes,   3, hasCP437 );
            PacketWriter.WriteString( Color.SubstituteSpecialColors(listName, useFallbacks),   packet.Bytes,  67, hasCP437 );
            PacketWriter.WriteString( Color.SubstituteSpecialColors(groupName, useFallbacks),  packet.Bytes, 131, hasCP437 );
            packet.Bytes[195] = groupRank;
            return packet;
        }


        [Pure]
        public static Packet MakeExtAddEntity( byte entityId, [NotNull] string inGameName, [NotNull] string skinName, bool hasCP437 ) {
            if( inGameName == null ) throw new ArgumentNullException( "inGameName" );
            if( skinName == null ) throw new ArgumentNullException( "skinName" );
            Packet packet = new Packet( OpCode.ExtAddEntity );
            //Logger.Log(LogType.Debug, "Send: MakeExtAddEntity({0}, {1}, {2})", entityId, inGameName, skinName);
            packet.Bytes[1] = entityId;
            PacketWriter.WriteString( inGameName, packet.Bytes, 2, hasCP437 );
            PacketWriter.WriteString( skinName, packet.Bytes, 66, hasCP437 );
            return packet;
        }


        [Pure]
        public static Packet MakeExtRemovePlayerName(short nameId) {
            Packet packet = new Packet( OpCode.ExtRemovePlayerName );
            //Logger.Log(LogType.Debug, "Send: MakeExtRemovePlayerName({0})", nameId);
            WriteI16( nameId, packet.Bytes, 1 );
            return packet;
        }


        [Pure]
        public static Packet MakeEnvSetColor(Byte variable, string color) {
            Packet packet = new Packet( OpCode.EnvSetColor );
            packet.Bytes[1] = (byte)variable;
            if (color != null) {
                System.Drawing.Color col = System.Drawing.ColorTranslator.FromHtml("#" + color.ToUpper());
                WriteI16(col.R, packet.Bytes, 2);
                WriteI16(col.G, packet.Bytes, 4);
                WriteI16(col.B, packet.Bytes, 6);
            } else {
                WriteI16(-1, packet.Bytes, 2);
                WriteI16(-1, packet.Bytes, 4);
                WriteI16(-1, packet.Bytes, 6);
            }
            return packet;
        }


        [Pure]
        public static Packet MakeMakeSelection(byte selectionId, [NotNull] string label, [NotNull] BoundingBox bounds,
                                               string color, short opacity, bool hasCP437) {
            if (label == null) throw new ArgumentNullException("label");
            if (bounds == null) throw new ArgumentNullException("bounds");
            Packet packet = new Packet(OpCode.MakeSelection);
            packet.Bytes[1] = selectionId;
            PacketWriter.WriteString(label, packet.Bytes, 2, hasCP437);
            
            WriteI16((short)bounds.XMin, packet.Bytes, 66);
            WriteI16((short)bounds.ZMin, packet.Bytes, 68);
            WriteI16((short)bounds.YMin, packet.Bytes, 70);
            WriteI16((short)(bounds.XMax + 1), packet.Bytes, 72);
            WriteI16((short)(bounds.ZMax + 1), packet.Bytes, 74);
            WriteI16((short)(bounds.YMax + 1), packet.Bytes, 76);
            
            CustomColor c = Color.ParseHex(color);
            WriteI16(c.R, packet.Bytes, 78);
            WriteI16(c.G, packet.Bytes, 80);
            WriteI16(c.B, packet.Bytes, 82);
            WriteI16(opacity, packet.Bytes, 84);
            return packet;
        }


        [Pure]
        public static Packet MakeRemoveSelection( byte selectionId ) {
            Packet packet = new Packet( OpCode.RemoveSelection );
            packet.Bytes[1] = selectionId;
            return packet;
        }


        [Pure]
        public static Packet MakeSetBlockPermission( Block block, bool canPlace, bool canDelete ) {
            Packet packet = new Packet( OpCode.SetBlockPermission );
            packet.Bytes[1] = (byte)block;
            packet.Bytes[2] = (byte)(canPlace ? 1 : 0);
            packet.Bytes[3] = (byte)(canDelete ? 1 : 0);
            return packet;
        }

        [Pure]
        public static Packet MakeChangeModel( sbyte id, [NotNull] string modelName, bool hasCP437) {
            if( modelName == null ) throw new ArgumentNullException( "modelName" );
            //Logger.Log(LogType.Debug, "Send: MakeChangeModel({0}, {1})", entityId, modelName);
            Packet packet = new Packet( OpCode.ChangeModel );
            packet.Bytes[1] = (byte)id;
            PacketWriter.WriteString( modelName, packet.Bytes, 2, hasCP437 );
            return packet;
        }

        [Pure]
        public static Packet MakeEnvSetMapAppearance( [NotNull] string textureUrl, byte sideBlock, byte edgeBlock,
                                                      short sideLevel, bool hasCP437 ) {
            if( textureUrl == null ) throw new ArgumentNullException( "textureUrl" );
            Packet packet = new Packet( OpCode.EnvMapAppearance );
            PacketWriter.WriteString( textureUrl, packet.Bytes, 1, hasCP437 );
            packet.Bytes[65] = sideBlock;
            packet.Bytes[66] = edgeBlock;
            WriteI16( sideLevel, packet.Bytes, 67 );
            return packet;
        }
        
        [Pure]
        public static Packet MakeEnvSetMapAppearance2( [NotNull] string textureUrl, byte sideBlock, byte edgeBlock,
                                                      short sideLevel, short cloudsHeight, short maxFog, bool hasCP437 ) {
            if( textureUrl == null ) throw new ArgumentNullException( "textureUrl" );
            int size = PacketSizes[(byte)OpCode.EnvMapAppearance] + 4;
            Packet packet = new Packet( OpCode.EnvMapAppearance, size );
            
            PacketWriter.WriteString( textureUrl, packet.Bytes, 1, hasCP437 );
            packet.Bytes[65] = sideBlock;
            packet.Bytes[66] = edgeBlock;
            WriteI16( sideLevel, packet.Bytes, 67 );
            WriteI16( cloudsHeight, packet.Bytes, 69 );
            WriteI16( maxFog, packet.Bytes, 71 );
            return packet;
        }

        [Pure]
        public static Packet SetWeather(byte forecast) {
            Packet packet = new Packet(OpCode.EnvWeatherType);
            packet.Bytes[1] = forecast;
            return packet;
        }

        [Pure]
        public static Packet HackControl(bool flying, bool noclip, bool speedhack, bool respawn, bool thirdperson, short jumpheight)
        {
            Packet packet = new Packet(OpCode.HackControl);
            packet.Bytes[1] = (byte)(flying ? 1 : 0);
            packet.Bytes[2] = (byte)(noclip ? 1 : 0);
            packet.Bytes[3] = (byte)(speedhack ? 1 : 0);
            packet.Bytes[4] = (byte)(respawn ? 1 : 0);
            packet.Bytes[5] = (byte)(thirdperson ? 1 : 0);
            WriteI16(jumpheight, packet.Bytes, 6);
            return packet;
        }


        [Pure]
        public static Packet MakeExtAddEntity2(sbyte entityId, string inGameName, string skin, Position spawn, 
                                               bool hasCP437, bool extPos) {
            if (inGameName == null) inGameName = entityId.ToString();
            if (skin == null) skin = inGameName;
            
            int size = PacketSizes[(byte)OpCode.ExtAddEntity2];
            if (extPos) size += 6;
            Packet packet = new Packet(OpCode.ExtAddEntity2, size);
            packet.Bytes[1] = (byte) entityId;
            PacketWriter.WriteString(inGameName, packet.Bytes, 2, hasCP437);
            PacketWriter.WriteString(skin, packet.Bytes, 66, hasCP437);
            
            int posSize = WritePos(spawn, packet.Bytes, 130, extPos);
            packet.Bytes[130 + posSize] = spawn.R;
            packet.Bytes[131 + posSize] = spawn.L;
            return packet;
        }
        
        [Pure]
        public static Packet MakeDefineBlock(BlockDefinition def, bool hasCP437) {
            Packet packet = new Packet(OpCode.DefineBlock);
            int index = 1;
            MakeDefineBlockStart(def, ref index, ref packet, false, hasCP437);
            packet.Bytes[index++] = def.Shape;
            MakeDefineBlockEnd(def, ref index, ref packet);
            return packet;
        }       
        
        [Pure]
        public static Packet MakeRemoveBlockDefinition(byte blockId) {
            Packet packet = new Packet(OpCode.RemoveBlockDefinition);
            packet.Bytes[1] = blockId;
            return packet;
        }
        
        [Pure]
        public static Packet MakeDefineBlockExt(BlockDefinition def, bool uniqueSideTexs, bool hasCP437) {
            byte[] bytes = new byte[uniqueSideTexs ? 88 : 85];
            Packet packet = new Packet(bytes);
            packet.Bytes[0] = (byte)OpCode.DefineBlockExt;
            int index = 1;
            
            MakeDefineBlockStart(def, ref index, ref packet, uniqueSideTexs, hasCP437);
            packet.Bytes[index++] = def.MinX;
            packet.Bytes[index++] = def.MinZ;
            packet.Bytes[index++] = def.MinY;
            packet.Bytes[index++] = def.MaxX;
            packet.Bytes[index++] = def.MaxZ;
            packet.Bytes[index++] = def.MaxY;
            MakeDefineBlockEnd(def, ref index, ref packet);
            return packet;
        }
        
        static void MakeDefineBlockStart(BlockDefinition def, ref int index, ref Packet packet, 
                                         bool uniqueSideTexs, bool hasCP437) {
            // speed = 2^((raw - 128) / 64);
            // therefore raw = 64log2(speed) + 128
            byte rawSpeed = (byte)(64 * Math.Log(def.Speed, 2) + 128);
            
            packet.Bytes[index++] = def.BlockID;
            PacketWriter.WriteString(def.Name, packet.Bytes, index, hasCP437);
            index += 64;
            packet.Bytes[index++] = def.CollideType;
            packet.Bytes[index++] = rawSpeed;
            packet.Bytes[index++] = def.TopTex;
            if (uniqueSideTexs) {
                packet.Bytes[index++] = def.LeftTex;
                packet.Bytes[index++] = def.RightTex;
                packet.Bytes[index++] = def.FrontTex;
                packet.Bytes[index++] = def.BackTex;
            } else {
                packet.Bytes[index++] = def.RightTex;
            }
            
            packet.Bytes[index++] = def.BottomTex;
            packet.Bytes[index++] = (byte)(def.BlocksLight ? 0 : 1);
            packet.Bytes[index++] = def.WalkSound;
            packet.Bytes[index++] = (byte)(def.FullBright ? 1 : 0);
        }
        
        static void MakeDefineBlockEnd(BlockDefinition def, ref int index, ref Packet packet) {
            packet.Bytes[index++] = def.BlockDraw;
            packet.Bytes[index++] = def.FogDensity;
            packet.Bytes[index++] = def.FogR;
            packet.Bytes[index++] = def.FogG;
            packet.Bytes[index++] = def.FogB;
        }
        
        public static Packet MakeSetTextColor(CustomColor col) {
            Packet packet = new Packet(OpCode.SetTextColor);
            packet.Bytes[1] = col.R;
            packet.Bytes[2] = col.G;
            packet.Bytes[3] = col.B;
            packet.Bytes[4] = col.A;
            packet.Bytes[5] = (byte)col.Code;
            return packet;
        }
        
        [Pure]
        public static Packet MakeEnvSetMapUrl( [NotNull] string textureUrl, bool hasCP437 ) {
            if( textureUrl == null ) throw new ArgumentNullException( "textureUrl" );
            Packet packet = new Packet( OpCode.SetEnvMapUrl );
            PacketWriter.WriteString( textureUrl, packet.Bytes, 1, hasCP437 );
            return packet;
        }
        
        [Pure]
        public static Packet MakeEnvSetMapProperty( EnvProp prop, int value ) {
            Packet packet = new Packet( OpCode.SetEnvMapProperty );
            packet.Bytes[1] = (byte)prop;
            WriteI32( value, packet.Bytes, 2 );
            return packet;
        }
        
        [Pure]
        public static Packet MakeEntityProperty( sbyte id, EntityProp prop, int value ) {
            Packet packet = new Packet( OpCode.SetEntityProperty );
            packet.Bytes[1] = (byte)id;
            packet.Bytes[2] = (byte)prop;
            WriteI32( value, packet.Bytes, 3 );
            return packet;
        }
        
        
        [Pure]
        public static Packet MakeTwoWayPing( bool serverToClient, ushort data ) {
            Packet packet = new Packet( OpCode.TwoWayPing );
            packet.Bytes[1] = (byte)(serverToClient ? 1 : 0);
            WriteU16( data, packet.Bytes, 2 );
            return packet;
        }
        
        [Pure]               
        public static Packet SetInventoryOrder( byte block, byte position ) {
            Packet packet = new Packet( OpCode.SetInventoryOrder );
            packet.Bytes[1] = block;
            packet.Bytes[2] = position;
            return packet;
        }
    }
}