// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Packet struct, just a wrapper for a byte array. </summary>
    public struct Packet {
        /// <summary> ID byte used in the protocol to indicate that an action should apply to self.
        /// When used in AddEntity packet, sets player's own respawn point.
        /// When used in Teleport packet, teleports the player. </summary>
        public const sbyte SelfId = -1;

        /// <summary> Raw bytes of this packet. </summary>
        public readonly byte[] Bytes;

        /// <summary> OpCode (first byte) of this packet. </summary>
        public OpCode OpCode {
            get { return (OpCode)Bytes[0]; }
        }


        /// <summary> Creates a new packet from given raw bytes. Data not be null. </summary>
        public Packet( [NotNull] byte[] rawBytes ) {
            if( rawBytes == null ) throw new ArgumentNullException( "rawBytes" );
            Bytes = rawBytes;
        }


        /// <summary> Creates a packet of correct size for a given opCode,
        /// and sets the first (opCode) byte. </summary>
        Packet( OpCode opCode ) {
            Bytes = new byte[PacketSizes[(int)opCode]];
            Bytes[0] = (byte)opCode;
        }

        #region Making regular packets

        /// <summary> Creates a new Handshake (0x00) packet. </summary>
        /// <param name="serverName"> Server name, to be shown on recipient's loading screen. May not be null. </param>
        /// <param name="player"> Player to whom this packet is being sent.
        /// Used to determine DeleteAdmincrete permission, for client-side checks. May not be null. </param>
        /// <param name="motd"> Message-of-the-day (text displayed below the server name). May not be null. </param>
        /// <exception cref="ArgumentNullException"> player, serverName, or motd is null </exception>
        public static Packet MakeHandshake( [NotNull] Player player, [NotNull] string serverName, [NotNull] string motd ) {
            if( serverName == null ) throw new ArgumentNullException( "serverName" );
            if( motd == null ) throw new ArgumentNullException( "motd" );

            Packet packet = new Packet( OpCode.Handshake );
            //Logger.Log(LogType.Debug, "Send: MakeHandshake({0}, {1}, {2})", player, serverName, motd);
            packet.Bytes[1] = Config.ProtocolVersion;
            Encoding.ASCII.GetBytes( serverName.PadRight( 64 ), 0, 64, packet.Bytes, 2 );
            Encoding.ASCII.GetBytes( motd.PadRight( 64 ), 0, 64, packet.Bytes, 66 );
            packet.Bytes[130] = (byte)(player.Can( Permission.DeleteAdmincrete ) ? 100 : 0);
            return packet;
        }


        /// <summary> Creates a new SetBlockServer (0x06) packet. </summary>
        /// <param name="coords"> Coordinates of the block. </param>
        /// <param name="type"> Block type to set at given coordinates. </param>
        public static Packet MakeSetBlock( Vector3I coords, Block type ) {
            Packet packet = new Packet( OpCode.SetBlockServer );
            //Logger.Log(LogType.Debug, "Send: MakeSetBlock({0})({1})", coords, type);
            ToNetOrder( (short)coords.X, packet.Bytes, 1 );
            ToNetOrder( (short)coords.Z, packet.Bytes, 3 );
            ToNetOrder( (short)coords.Y, packet.Bytes, 5 );
            packet.Bytes[7] = (byte)type;
            return packet;
        }


        /// <summary> Creates a new AddEntity (0x07) packet. </summary>
        /// <param name="id"> Entity ID. Negative values refer to "self". </param>
        /// <param name="name"> Entity name. May not be null. </param>
        /// <param name="spawnPosition"> Spawning position for the player. </param>
        /// <exception cref="ArgumentNullException"> name is null </exception>
        public static Packet MakeAddEntity( sbyte id, [NotNull] string name, Position spawnPosition ) {
            if (name == null) throw new ArgumentNullException("name");
            
            Packet packet = new Packet( OpCode.AddEntity );
            //Logger.Log(LogType.Debug, "Send: MakeAddEntity({0}, {1}, {2})", id, name, spawnPosition);
            packet.Bytes[1] = (byte)id;
            Encoding.ASCII.GetBytes( name.PadRight( 64 ), 0, 64, packet.Bytes, 2 );
            ToNetOrder( spawnPosition.X, packet.Bytes, 66 );
            ToNetOrder( spawnPosition.Z, packet.Bytes, 68 );
            ToNetOrder( spawnPosition.Y, packet.Bytes, 70 );
            packet.Bytes[72] = spawnPosition.R;
            packet.Bytes[73] = spawnPosition.L;
            return packet;
        }


        /// <summary> Creates a new Teleport (0x08) packet. </summary>
        /// <param name="id"> Entity ID. Negative values refer to "self". </param>
        /// <param name="newPosition"> Position to teleport the entity to. </param>
        public static Packet MakeTeleport( sbyte id, Position newPosition ) {
            Packet packet = new Packet( OpCode.Teleport );
            //Logger.Log(LogType.Debug, "Send: MakeTeleport({0}, {1})", id, newPosition);
            packet.Bytes[1] = (byte)id;
            ToNetOrder( newPosition.X, packet.Bytes, 2 );
            ToNetOrder( newPosition.Z, packet.Bytes, 4 );
            ToNetOrder( newPosition.Y, packet.Bytes, 6 );
            packet.Bytes[8] = newPosition.R;
            packet.Bytes[9] = newPosition.L;
            return packet;
        }


        /// <summary> Creates a new Teleport (0x08) packet, and sets ID to -1 ("self"). </summary>
        /// <param name="newPosition"> Position to teleport player to. </param>
        public static Packet MakeSelfTeleport( Position newPosition ) {
            return MakeTeleport( -1, newPosition.GetFixed() );
        }


        /// <summary> Creates a new MoveRotate (0x09) packet. </summary>
        /// <param name="id"> Entity ID. </param>
        /// <param name="positionDelta"> Positioning information.
        /// Coordinates (X/Y/Z) should be relative and between -128 and 127.
        /// Rotation (R/L) should be absolute. </param>
        public static Packet MakeMoveRotate( sbyte id, Position positionDelta ) {
            Packet packet = new Packet( OpCode.MoveRotate );
            packet.Bytes[1] = (byte)id;
            packet.Bytes[2] = (byte)(positionDelta.X & 0xFF);
            packet.Bytes[3] = (byte)(positionDelta.Z & 0xFF);
            packet.Bytes[4] = (byte)(positionDelta.Y & 0xFF);
            packet.Bytes[5] = positionDelta.R;
            packet.Bytes[6] = positionDelta.L;
            return packet;
        }


        /// <summary> Creates a new Move (0x0A) packet. </summary>
        /// <param name="id"> Entity ID. </param>
        /// <param name="positionDelta"> Positioning information.
        /// Coordinates (X/Y/Z) should be relative and between -128 and 127. Rotation (R/L) is not sent. </param>
        public static Packet MakeMove( sbyte id, Position positionDelta ) {
            Packet packet = new Packet( OpCode.Move );
            packet.Bytes[1] = (byte)id;
            packet.Bytes[2] = (byte)positionDelta.X;
            packet.Bytes[3] = (byte)positionDelta.Z;
            packet.Bytes[4] = (byte)positionDelta.Y;
            return packet;
        }


        /// <summary> Creates a new Rotate (0x0B) packet. </summary>
        /// <param name="id"> Entity ID. </param>
        /// <param name="newPosition"> Positioning information.
        /// Rotation (R/L) should be absolute. Coordinates (X/Y/Z) are not sent. </param>
        public static Packet MakeRotate( sbyte id, Position newPosition ) {
            Packet packet = new Packet( OpCode.Rotate );
            packet.Bytes[1] = (byte)id;
            packet.Bytes[2] = newPosition.R;
            packet.Bytes[3] = newPosition.L;
            return packet;
        }


        /// <summary> Creates a new RemoveEntity (0x0C) packet. </summary>
        /// <param name="id"> Entity ID. </param>
        public static Packet MakeRemoveEntity( sbyte id ) {
            //Logger.Log(LogType.Debug, "Send: MakeRemoveEntity({0})", id);
            Packet packet = new Packet( OpCode.RemoveEntity );
            packet.Bytes[1] = (byte)id;
            return packet;
        }

        /// <summary> Creates a new Message (0x0D) packet. </summary>
        /// <param name="type"> Message type. </param>
        /// <param name="message"> Message. </param>
        public static Packet Message(byte type, string message, bool useFallbacks) {
            Packet packet = new Packet(OpCode.Message);
            packet.Bytes[1] = type;
            message = Color.SubstituteSpecialColors(message, useFallbacks);
            Encoding.ASCII.GetBytes(message.PadRight(64), 0, 64, packet.Bytes, 2);
            return packet;
        }

        /// <summary> Creates a new Kick (0x0E) packet. </summary>
        /// <param name="reason"> Given reason. Only first 64 characters will be sent. May not be null. </param>
        /// <exception cref="ArgumentNullException"> reason is null </exception>
        public static Packet MakeKick( [NotNull] string reason ) {
            if( reason == null ) throw new ArgumentNullException( "reason" );
            reason = Color.SubstituteSpecialColors(reason, true);
            Packet packet = new Packet( OpCode.Kick );
            Encoding.ASCII.GetBytes( reason.PadRight( 64 ), 0, 64, packet.Bytes, 1 );
            return packet;
        }


        /// <summary> Creates a new SetPermission (0x0F) packet. </summary>
        /// <param name="player"> Player to whom this packet is being sent.
        /// Used to determine DeleteAdmincrete permission, for client-side checks. May not be null. </param>
        /// <exception cref="ArgumentNullException"> player is null </exception>
        public static Packet MakeSetPermission( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            Packet packet = new Packet( OpCode.SetPermission );
            //Logger.Log(LogType.Debug, "Send: MakeSetPermission({0})", player);
            packet.Bytes[1] = (byte)(player.Can( Permission.DeleteAdmincrete ) ? 100 : 0);
            return packet;
        }

        #endregion

        #region Making extended packets

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
            //Logger.Log(LogType.Debug, "Send: MakeSetClickDistance({0})", distance);
            if( distance < 0 ) throw new ArgumentOutOfRangeException( "distance" );
            Packet packet = new Packet( OpCode.SetClickDistance );
            ToNetOrder( distance, packet.Bytes, 1 );
            return packet;
        }


        [Pure]
        public static Packet MakeCustomBlockSupportLevel( byte level ) {
            //Logger.Log(LogType.Debug, "Send: CustomBlockSupportLevel({0})", level);
            Packet packet = new Packet(OpCode.CustomBlockSupportLevel);
            packet.Bytes[1] = level;
            return packet;
        }


        [Pure]
        public static Packet MakeHoldThis(Block block, bool preventChange)
        {
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
        public static Packet MakeExtRemovePlayerName(short nameId)
        {
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
                                               string color, short opacity)
        {
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
        public static Packet MakeExtAddEntity2(sbyte entityId, [NotNull] string inGameName, [NotNull] string skinName, Position spawnPosition, Player sentto) {
            if (inGameName == null)
                throw new ArgumentNullException("inGameName");
            if (skinName == null)
                throw new ArgumentNullException("skinName");
            Packet packet = new Packet(OpCode.ExtAddEntity2);
            //Logger.Log(LogType.Debug, "Send to {4}: MakeExtAddEntity2({0}, {1}, {2}, {3})", (byte)((byte)(entityId) - 128), inGameName, skinName, spawnPosition.ToString(), sentto.Name);
            packet.Bytes[1] = (byte) entityId;
            Encoding.ASCII.GetBytes(inGameName.PadRight(64), 0, 64, packet.Bytes, 2);
            Encoding.ASCII.GetBytes(skinName.PadRight(64), 0, 64, packet.Bytes, 66);
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
        public static Packet MakeEnvSetMapAppearance( EnvProperty prop, int value ) {
            Packet packet = new Packet( OpCode.SetEnvMapProperty );
            packet.Bytes[1] = (byte)prop;
            ToNetOrder( value, packet.Bytes, 2 );
            return packet;
        }

        #endregion

        internal static void ToNetOrder( short number, [NotNull] byte[] arr, int offset ) {
            arr[offset] = (byte)((number & 0xff00) >> 8);
            arr[offset + 1] = (byte)(number & 0x00ff);
        }

        internal static void ToNetOrder( int number, [NotNull] byte[] arr, int offset ) {
            arr[offset] = (byte)((number & 0xff000000) >> 24);
            arr[offset + 1] = (byte)((number & 0x00ff0000) >> 16);
            arr[offset + 2] = (byte)((number & 0x0000ff00) >> 8);
            arr[offset + 3] = (byte)(number & 0x000000ff);
        }

        static readonly int[] PacketSizes = {
            131, // Handshake
            1, // Ping
            1, // MapBegin
            1028, // MapChunk
            7, // MapEnd
            9, // SetBlockClient
            8, // SetBlockServer
            74, // AddEntity
            10, // Teleport
            7, // MoveRotate
            5, // Move
            4, // Rotate
            2, // RemoveEntity
            66, // Message
            65, // Kick
            2, // SetPermission

            67, // ExtInfo
            69, // ExtEntry

            3, // SetClickDistance
            2, // CustomBlockSupportLevel
            3, // HoldThis
            134, // SetTextHotKey
            196, // ExtAddPlayerName
            130, // ExtAddEntity
            3, // ExtRemovePlayerName
            8, // EnvSetColor
            86, // MakeSelection
            2, // RemoveSelection
            4, // SetBlockPermission
            66, // ChangeModel
            69, // EnvMapAppearance
            2, // EnvSetWeatherType
            8, // HackControl
            138, // ExtAddEntity2
            0,
            80, // DefineBlock
            2, // RemoveBlockDefinition
            85, // DefineBlockExt
            1282, // BulkBlockUpdate
            6, // SetTextColor
            65, // SetEnvMapUrl
            6, // SetEnvMapProperty
        };
    }
    
    public struct SetBlockData {
        public short X, Y, Z;
        public byte Block;
        
        public SetBlockData(int x, int y, int z, byte block) {
            X = (short)x; Y = (short)y; Z = (short)z;
            Block = block;
        }
        
        public SetBlockData(Vector3I p, byte block) {
            X = (short)p.X; Y = (short)p.Y; Z = (short)p.Z;
            Block = block;
        }
    }
}