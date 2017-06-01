// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Packet struct, just a wrapper for a byte array. </summary>
    public partial struct Packet {
        /// <summary> ID byte used in the protocol to indicate that an action should apply to self.
        /// When used in AddEntity packet, sets player's own respawn point.
        /// When used in Teleport packet, teleports the player. </summary>
        public const sbyte SelfId = -1;

        /// <summary> Raw bytes of this packet. </summary>
        public readonly byte[] Bytes;

        /// <summary> OpCode (first byte) of this packet. </summary>
        public OpCode OpCode { get { return (OpCode)Bytes[0]; } }


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

        Packet( OpCode opCode, int size ) {
            Bytes = new byte[size];
            Bytes[0] = (byte)opCode;
        }

        #region Making regular packets

        /// <summary> Creates a new Handshake (0x00) packet. </summary>
        /// <param name="serverName"> Server name, to be shown on recipient's loading screen. May not be null. </param>
        /// <param name="player"> Player to whom this packet is being sent.
        /// Used to determine DeleteAdmincrete permission, for client-side checks. May not be null. </param>
        /// <param name="motd"> Message-of-the-day (text displayed below the server name). May not be null. </param>
        /// <param name="hasCP437"> Whether client supports extended code page 437 characters. </param>
        /// <exception cref="ArgumentNullException"> player, serverName, or motd is null </exception>
        public static Packet MakeHandshake( [NotNull] Player player, [NotNull] string serverName, [NotNull] string motd, bool hasCP437 ) {
            if( serverName == null ) throw new ArgumentNullException( "serverName" );
            if( motd == null ) throw new ArgumentNullException( "motd" );

            Packet packet = new Packet( OpCode.Handshake );
            //Logger.Log(LogType.Debug, "Send: MakeHandshake({0}, {1}, {2})", player, serverName, motd);
            packet.Bytes[1] = Config.ProtocolVersion;
            PacketWriter.WriteString( serverName, packet.Bytes, 2, hasCP437 );
            PacketWriter.WriteString( motd, packet.Bytes, 66, hasCP437 );
            packet.Bytes[130] = (byte)(player.Can( Permission.DeleteAdmincrete ) ? 100 : 0);
            return packet;
        }


        /// <summary> Creates a new SetBlockServer (0x06) packet. </summary>
        /// <param name="coords"> Coordinates of the block. </param>
        /// <param name="type"> Block type to set at given coordinates. </param>
        public static Packet MakeSetBlock( Vector3I coords, Block type ) {
            Packet packet = new Packet( OpCode.SetBlockServer );
            //Logger.Log(LogType.Debug, "Send: MakeSetBlock({0})({1})", coords, type);
            WriteU16( (ushort)coords.X, packet.Bytes, 1 );
            WriteU16( (ushort)coords.Z, packet.Bytes, 3 );
            WriteU16( (ushort)coords.Y, packet.Bytes, 5 );
            packet.Bytes[7] = (byte)type;
            return packet;
        }


        /// <summary> Creates a new AddEntity (0x07) packet. </summary>
        /// <param name="id"> Entity ID. Negative values refer to "self". </param>
        /// <param name="name"> Entity name. May not be null. </param>
        /// <param name="spawn"> Spawning position for the player. </param>
        /// <param name="hasCP437"> If packet contains characters from CodePage 437 </param>
        /// <param name="extPos"> If player supports Extended Positions </param>
        /// <exception cref="ArgumentNullException"> name is null </exception>
        public static Packet MakeAddEntity( sbyte id, [NotNull] string name, Position spawn, 
                                           bool hasCP437, bool extPos ) {
            if (name == null) throw new ArgumentNullException("name");
            
            int size = PacketSizes[(byte)OpCode.AddEntity];
            if (extPos) size += 6;
            Packet packet = new Packet( OpCode.AddEntity, size );
            //Logger.Log(LogType.Debug, "Send: MakeAddEntity({0}, {1}, {2})", id, name, spawnPosition);
            packet.Bytes[1] = (byte)id;
            PacketWriter.WriteString( name, packet.Bytes, 2, hasCP437 );
            
            int posSize = WritePos( spawn, packet.Bytes, 66, extPos );
            packet.Bytes[66 + posSize] = spawn.R;
            packet.Bytes[67 + posSize] = spawn.L;
            return packet;
        }


        /// <summary> Creates a new Teleport (0x08) packet. </summary>
        /// <param name="id"> Entity ID. Negative values refer to "self". </param>
        /// <param name="newPosition"> Position to teleport the entity to. </param>
        /// <param name="extPos"> If player supports Extended Positions </param>
        public static Packet MakeTeleport( sbyte id, Position newPosition, bool extPos ) {
            int size = PacketSizes[(byte)OpCode.Teleport];
            if (extPos) size += 6;
            Packet packet = new Packet( OpCode.Teleport, size );
            
            //Logger.Log(LogType.Debug, "Send: MakeTeleport({0}, {1})", id, newPosition);
            packet.Bytes[1] = (byte)id;
            int posSize = WritePos( newPosition, packet.Bytes, 2, extPos );
            packet.Bytes[2 + posSize] = newPosition.R;
            packet.Bytes[3 + posSize] = newPosition.L;
            return packet;
        }


        /// <summary> Creates a new Teleport (0x08) packet, and sets ID to -1 ("self"). </summary>
        /// <param name="newPosition"> Position to teleport player to. </param>
        /// <param name="extPos"> If player supports Extended Positions </param>
        public static Packet MakeSelfTeleport( Position newPosition, bool extPos ) {
            return MakeTeleport( -1, newPosition.GetFixed(), extPos );
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
        /// <param name="useFallbacks"> whether or not to use color fallback codes. </param>
        /// <param name="hasCP437"> Whether client supports extended code page 437 characters. </param>
        public static Packet Message(byte type, string message, bool useFallbacks, bool hasCP437) {
            Packet packet = new Packet(OpCode.Message);
            packet.Bytes[1] = type;
            message = Color.Sys + Color.SubstituteSpecialColors(message, useFallbacks);
            PacketWriter.WriteString(message, packet.Bytes, 2, hasCP437);
            return packet;
        }
        
        // avoid redundantly typing FallbackColors and HasCP437 all the time
        public static Packet Message(byte type, string message, Player player) {
            return Message(type, message, player.FallbackColors, player.HasCP437);
        }

        /// <summary> Creates a new Kick (0x0E) packet. </summary>
        /// <param name="reason"> Given reason. Only first 64 characters will be sent. May not be null. </param>
        /// <param name="hasCP437"> Whether client supports extended code page 437 characters. </param>
        /// <exception cref="ArgumentNullException"> reason is null </exception>
        public static Packet MakeKick( [NotNull] string reason, bool hasCP437 ) {
            if( reason == null ) throw new ArgumentNullException( "reason" );
            reason = Color.SubstituteSpecialColors(reason, true);
            Packet packet = new Packet( OpCode.Kick );
            PacketWriter.WriteString( reason, packet.Bytes, 1, hasCP437 );
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
        
        internal static int WritePos( Position pos, [NotNull] byte[] arr, int offset, bool extPos ) {
            if (!extPos) {
                WriteI16( (short)pos.X, arr, offset + 0 );
                WriteI16( (short)pos.Z, arr, offset + 2 );
                WriteI16( (short)pos.Y, arr, offset + 4 );
            } else {
                WriteI32( (int)pos.X, arr, offset + 0 );
                WriteI32( (int)pos.Z, arr, offset + 4 );
                WriteI32( (int)pos.Y, arr, offset + 8 );
            }
            return extPos ? 12 : 6;
        }

        internal static void WriteI16( short number, [NotNull] byte[] arr, int offset ) {
            arr[offset] =     (byte)((number & 0xff00) >> 8);
            arr[offset + 1] = (byte)( number & 0x00ff);
        }
        
        internal static void WriteU16( ushort number, [NotNull] byte[] arr, int offset ) {
            arr[offset] =     (byte)((number & 0xff00) >> 8);
            arr[offset + 1] = (byte)( number & 0x00ff);
        }

        internal static void WriteI32( int number, [NotNull] byte[] arr, int offset ) {
            arr[offset] =     (byte)((number & 0xff000000) >> 24);
            arr[offset + 1] = (byte)((number & 0x00ff0000) >> 16);
            arr[offset + 2] = (byte)((number & 0x0000ff00) >> 8);
            arr[offset + 3] = (byte)( number & 0x000000ff);
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
        public ushort X, Y, Z;
        public byte Block;
        
        public SetBlockData(int x, int y, int z, byte block) {
            X = (ushort)x; Y = (ushort)y; Z = (ushort)z;
            Block = block;
        }
        
        public SetBlockData(Vector3I p, byte block) {
            X = (ushort)p.X; Y = (ushort)p.Y; Z = (ushort)p.Z;
            Block = block;
        }
    }
}