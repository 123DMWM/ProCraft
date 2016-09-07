// ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>

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
            Encoding.ASCII.GetBytes( sname.PadRight(64), 0, 64, packet.Bytes, 1 );
            ToNetOrder( extCount, packet.Bytes, 65 );
            return packet;
        }


        [Pure]
        public static Packet MakeExtEntry( [NotNull] string name, int version ) {
            //Logger.Log(LogType.Debug, "Send: ExtEntry({0}, {1})", name, version);
            if( name == null ) throw new ArgumentNullException( "name" );
            Packet packet = new Packet( OpCode.ExtEntry );
            Encoding.ASCII.GetBytes( name.PadRight( 64 ), 0, 64, packet.Bytes, 1 );
            ToNetOrder( version, packet.Bytes, 65 );
            return packet;
        }


        [Pure]
        public static Packet MakeSetClickDistance( short distance ) {
            if( distance < 0 ) throw new ArgumentOutOfRangeException( "distance" );
            Packet packet = new Packet( OpCode.SetClickDistance );
            ToNetOrder( distance, packet.Bytes, 1 );
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
                                                byte keyMods ) {
            if( label == null ) throw new ArgumentNullException( "label" );
            if( action == null ) throw new ArgumentNullException( "action" );
            Packet packet = new Packet( OpCode.SetTextHotKey );
            Encoding.ASCII.GetBytes( label.PadRight( 64 ), 0, 64, packet.Bytes, 1 );
            Encoding.ASCII.GetBytes( action.PadRight( 64 ), 0, 64, packet.Bytes, 65 );
            ToNetOrder( keyCode, packet.Bytes, 129 );
            packet.Bytes[133] = keyMods;
            return packet;
        }


        [Pure]
        public static Packet MakeExtAddPlayerName(short nameId, string playerName, string listName, string groupName,
                                                   byte groupRank, bool useFallbacks ) {
            if( playerName == null ) throw new ArgumentNullException( "playerName" );
            if( listName == null ) throw new ArgumentNullException( "listName" );
            if( groupName == null ) throw new ArgumentNullException( "groupName" );
            Packet packet = new Packet( OpCode.ExtAddPlayerName );
            //Logger.Log(LogType.Debug, "Send: MakeExtAddPlayerName({0}, {1}, {2}, {3}, {4})", nameId, playerName, listName, groupName, groupRank);
            ToNetOrder( nameId, packet.Bytes, 1 );
            Encoding.ASCII.GetBytes( Color.SubstituteSpecialColors(playerName, useFallbacks).PadRight( 64 ), 0, 64, packet.Bytes, 3 );
            Encoding.ASCII.GetBytes( Color.SubstituteSpecialColors(listName, useFallbacks).PadRight( 64 ), 0, 64, packet.Bytes, 67 );
            Encoding.ASCII.GetBytes( Color.SubstituteSpecialColors(groupName, useFallbacks).PadRight( 64 ), 0, 64, packet.Bytes, 131 );
            packet.Bytes[195] = groupRank;
            return packet;
        }


        [Pure]
        public static Packet MakeExtAddEntity( byte entityId, [NotNull] string inGameName, [NotNull] string skinName ) {
            if( inGameName == null ) throw new ArgumentNullException( "inGameName" );
            if( skinName == null ) throw new ArgumentNullException( "skinName" );
            Packet packet = new Packet( OpCode.ExtAddEntity );
            //Logger.Log(LogType.Debug, "Send: MakeExtAddEntity({0}, {1}, {2})", entityId, inGameName, skinName);
            packet.Bytes[1] = entityId;
            Encoding.ASCII.GetBytes( inGameName.PadRight( 64 ), 0, 64, packet.Bytes, 2 );
            Encoding.ASCII.GetBytes( skinName.PadRight( 64 ), 0, 64, packet.Bytes, 66 );
            return packet;
        }


        [Pure]
        public static Packet MakeExtRemovePlayerName(short nameId) {
            Packet packet = new Packet( OpCode.ExtRemovePlayerName );
            //Logger.Log(LogType.Debug, "Send: MakeExtRemovePlayerName({0})", nameId);
            ToNetOrder( nameId, packet.Bytes, 1 );
            return packet;
        }


        [Pure]
        public static Packet MakeEnvSetColor(Byte variable, string color)
        {
            Packet packet = new Packet( OpCode.EnvSetColor );
            packet.Bytes[1] = (byte)variable;
            if (color != null) {
                System.Drawing.Color col = System.Drawing.ColorTranslator.FromHtml("#" + color.ToUpper());
                ToNetOrder((short) col.R, packet.Bytes, 2);
                ToNetOrder((short) col.G, packet.Bytes, 4);
                ToNetOrder((short) col.B, packet.Bytes, 6);
            } else {
                ToNetOrder((short)-1, packet.Bytes, 2);
                ToNetOrder((short)-1, packet.Bytes, 4);
                ToNetOrder((short)-1, packet.Bytes, 6);
            }
            return packet;
        }


        [Pure]
        public static Packet MakeMakeSelection(byte selectionId, [NotNull] string label, [NotNull] BoundingBox bounds,
                                               string color, short opacity) {
            if (label == null) throw new ArgumentNullException("label");
            if (bounds == null) throw new ArgumentNullException("bounds");
            Packet packet = new Packet(OpCode.MakeSelection);
            packet.Bytes[1] = selectionId;
            Encoding.ASCII.GetBytes(label.PadRight(64), 0, 64, packet.Bytes, 2);
            
            ToNetOrder((short)bounds.XMin, packet.Bytes, 66);
            ToNetOrder((short)bounds.ZMin, packet.Bytes, 68);
            ToNetOrder((short)bounds.YMin, packet.Bytes, 70);
            ToNetOrder((short)(bounds.XMax + 1), packet.Bytes, 72);
            ToNetOrder((short)(bounds.ZMax + 1), packet.Bytes, 74);
            ToNetOrder((short)(bounds.YMax + 1), packet.Bytes, 76);
            
            var col = System.Drawing.ColorTranslator.FromHtml("#" + color.ToUpper());
            ToNetOrder((short)col.R, packet.Bytes, 78);
            ToNetOrder((short)col.G, packet.Bytes, 80);
            ToNetOrder((short)col.B, packet.Bytes, 82);
            ToNetOrder(opacity, packet.Bytes, 84);
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
        public static Packet MakeChangeModel( byte entityId, [NotNull] string modelName) {
            if( modelName == null ) throw new ArgumentNullException( "modelName" );
            //Logger.Log(LogType.Debug, "Send: MakeChangeModel({0}, {1})", entityId, modelName);
            Packet packet = new Packet( OpCode.ChangeModel );
            packet.Bytes[1] = entityId;
            Encoding.ASCII.GetBytes( modelName.PadRight( 64 ), 0, 64, packet.Bytes, 2 );
            return packet;
        }

        [Pure]
        public static Packet MakeEnvSetMapAppearance( [NotNull] string textureUrl, byte sideBlock, byte edgeBlock,
                                                      short sideLevel ) {
            if( textureUrl == null ) throw new ArgumentNullException( "textureUrl" );
            Packet packet = new Packet( OpCode.EnvMapAppearance );
            Encoding.ASCII.GetBytes( textureUrl.PadRight( 64 ), 0, 64, packet.Bytes, 1 );
            packet.Bytes[65] = sideBlock;
            packet.Bytes[66] = edgeBlock;
            ToNetOrder( sideLevel, packet.Bytes, 67 );
            return packet;
        }
        
        [Pure]
        public static Packet MakeEnvSetMapAppearance2( [NotNull] string textureUrl, byte sideBlock, byte edgeBlock,
                                                      short sideLevel, short cloudsHeight, short maxFog ) {
            if( textureUrl == null ) throw new ArgumentNullException( "textureUrl" );
            byte[] packet = new byte[73];
            packet[0] = (byte)OpCode.EnvMapAppearance;
            Encoding.ASCII.GetBytes( textureUrl.PadRight( 64 ), 0, 64, packet, 1 );
            packet[65] = sideBlock;
            packet[66] = edgeBlock;
            ToNetOrder( sideLevel, packet, 67 );
            ToNetOrder( cloudsHeight, packet, 69 );
            ToNetOrder( maxFog, packet, 71 );
            return new Packet( packet );
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
            ToNetOrder(jumpheight, packet.Bytes, 6);
            return packet;
        }


        [Pure]
        public static Packet MakeExtAddEntity2(sbyte entityId, string inGameName, string skin, Position spawnPosition, Player sentto) {
            if (inGameName == null) inGameName = entityId.ToString();
            if (skin == null) skin = inGameName ;
            Packet packet = new Packet(OpCode.ExtAddEntity2);
            //Logger.Log(LogType.Debug, "Send to {4}: MakeExtAddEntity2({0}, {1}, {2}, {3})", (byte)((byte)(entityId) - 128), inGameName, skin, spawnPosition.ToString(), sentto.Name);
            packet.Bytes[1] = (byte) entityId;
            Encoding.ASCII.GetBytes(inGameName.PadRight(64), 0, 64, packet.Bytes, 2);
            Encoding.ASCII.GetBytes(skin.PadRight(64), 0, 64, packet.Bytes, 66);
            ToNetOrder(spawnPosition.X, packet.Bytes, 130);
            ToNetOrder(spawnPosition.Z, packet.Bytes, 132);
            ToNetOrder(spawnPosition.Y, packet.Bytes, 134);
            packet.Bytes[136] = spawnPosition.R;
            packet.Bytes[137] = spawnPosition.L;
            return packet;
        }
        
        [Pure]
        public static Packet MakeDefineBlock(BlockDefinition def) {
            Packet packet = new Packet(OpCode.DefineBlock);
            int index = 1;
            MakeDefineBlockStart(def, ref index, ref packet, false);
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
        public static Packet MakeDefineBlockExt(BlockDefinition def, bool uniqueSideTexs) {
        	byte[] bytes = new byte[uniqueSideTexs ? 88 : 85];
            Packet packet = new Packet(bytes);
            packet.Bytes[0] = (byte)OpCode.DefineBlockExt;
            int index = 1;
            
            MakeDefineBlockStart(def, ref index, ref packet, uniqueSideTexs);
            packet.Bytes[index++] = def.MinX;
            packet.Bytes[index++] = def.MinZ;
            packet.Bytes[index++] = def.MinY;
            packet.Bytes[index++] = def.MaxX;
            packet.Bytes[index++] = def.MaxZ;
            packet.Bytes[index++] = def.MaxY;
            MakeDefineBlockEnd(def, ref index, ref packet);
            return packet;
        }
        
        static void MakeDefineBlockStart(BlockDefinition def, ref int index, ref Packet packet, bool uniqueSideTexs) {
        	// speed = 2^((raw - 128) / 64);
            // therefore raw = 64log2(speed) + 128
            byte rawSpeed = (byte)(64 * Math.Log(def.Speed, 2) + 128);
            
            packet.Bytes[index++] = def.BlockID;
            Encoding.ASCII.GetBytes(def.Name.PadRight(64), 0, 64, packet.Bytes, index);
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
                packet.Bytes[index++] = def.SideTex;
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
        public static Packet MakeEnvSetMapUrl( [NotNull] string textureUrl ) {
            if( textureUrl == null ) throw new ArgumentNullException( "textureUrl" );
            Packet packet = new Packet( OpCode.SetEnvMapUrl );
            Encoding.ASCII.GetBytes( textureUrl.PadRight( 64 ), 0, 64, packet.Bytes, 1 );
            return packet;
        }
        
        [Pure]
        public static Packet MakeEnvSetMapProperty( EnvProp prop, int value ) {
            Packet packet = new Packet( OpCode.SetEnvMapProperty );
            packet.Bytes[1] = (byte)prop;
            ToNetOrder( value, packet.Bytes, 2 );
            return packet;
        }
    }
}