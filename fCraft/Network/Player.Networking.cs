// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2015 Joseph Beauvais <123DMWM@gmail.com>
//#define DEBUG_MOVEMENT
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using fCraft.AutoRank;
using fCraft.Drawing;
using fCraft.Events;
using fCraft.MapConversion;
using JetBrains.Annotations;
using System.Collections.Concurrent;
using ServiceStack.Text;

namespace fCraft {
    /// <summary> Represents a connection to a Minecraft client. Handles low-level interactions (e.g. networking). </summary>
    public sealed partial class Player {
        public static int SocketTimeout { get; set; }
        public static bool RelayAllUpdates { get; set; }
        const int SleepDelay = 5; // milliseconds
        const int SocketPollInterval = 200; // multiples of SleepDelay, approx. 1 second
        const int PingInterval = 3; // multiples of SocketPollInterval, approx. 3 seconds
        public DateTime LastZoneNotification = DateTime.UtcNow;

        const string NoSmpMessage = "Only ClassiCube clients work!";


        static Player() {
            MaxBlockPlacementRange = 32767;
            SocketTimeout = 10000;
        }


        /// <summary> Reason that this player is about to leave / has left the server. Set by Kick. 
        /// This value is undefined until the player is about to disconnect. </summary>
        public LeaveReason LeaveReason { get; private set; }

        /// <summary> Remote IP address of this player. </summary>
        public IPAddress IP { get; private set; }


        bool canReceive = true,
             canSend = true,
             canQueue = true;

        readonly Thread ioThread;
        readonly TcpClient client;
        readonly NetworkStream stream;
        readonly PacketReader reader;
        readonly PacketWriter writer;

        readonly ConcurrentQueue<Packet> outputQueue = new ConcurrentQueue<Packet>(),
                                         priorityOutputQueue = new ConcurrentQueue<Packet>();


        internal static Player StartSession( [NotNull] TcpClient tcpClient ) {
            if( tcpClient == null ) throw new ArgumentNullException( "tcpClient" );
            return new Player( tcpClient );
        }


        Player( [NotNull] TcpClient tcpClient ) {
            if( tcpClient == null ) throw new ArgumentNullException( "tcpClient" );
            State = SessionState.Connecting;
            LoginTime = DateTime.UtcNow;
            LastActiveTime = DateTime.UtcNow;
            LastPatrolTime = DateTime.UtcNow;
            LeaveReason = LeaveReason.Unknown;
            LastUsedBlockType = Block.None;
            BlocksDeletedThisSession = 0;
            BlocksPlacedThisSession = 0;

            client = tcpClient;
            client.SendTimeout = SocketTimeout;
            client.ReceiveTimeout = SocketTimeout;

            BrushReset();
            Metadata = new MetadataCollection<object>();

            try {
                IP = ( (IPEndPoint)( client.Client.RemoteEndPoint ) ).Address;
                if( Server.RaiseSessionConnectingEvent( IP ) ) return;

                stream = client.GetStream();
                reader = new PacketReader( stream );
                writer = new PacketWriter( stream );

                ioThread = new Thread( IoLoop ) {
                    Name = "ProCraft.Session",
                    CurrentCulture = new CultureInfo( "en-US" )
                };
                ioThread.Start();

            } catch( SocketException ) {
                // Mono throws SocketException when accessing Client.RemoteEndPoint on disconnected sockets
                Disconnect();

            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Session failed to start", "ProCraft", ex, false );
                Disconnect();
            }
        }


        #region I/O Loop

        void IoLoop() {
            try {
                Server.RaiseSessionConnectedEvent( this );

                // try to log the player in, otherwise die.
                if( !LoginSequence() ) {
                    return;
                }

                BandwidthUseMode = Info.BandwidthUseMode;

                // set up some temp variables
                Packet packet = new Packet();

                int pollCounter = 0,
                    pingCounter = 0;

                // main i/o loop
                while( canSend ) {
                    int packetsSent = 0;

                    // detect player disconnect
                    if( pollCounter > SocketPollInterval ) {
                        if( !client.Connected ||
                            ( client.Client.Poll( 1000, SelectMode.SelectRead ) && client.Client.Available == 0 ) ) {
                            if( Info != null ) {
                                Logger.Log( LogType.Debug,
                                            "Player.IoLoop: Lost connection to player {0} ({1}).", Name, IP );
                            } else {
                                Logger.Log( LogType.Debug,
                                            "Player.IoLoop: Lost connection to unidentified player at {0}.", IP );
                            }
                            LeaveReason = LeaveReason.ClientQuit;
                            return;
                        }
                        if( pingCounter > PingInterval ) {
                            writer.Write( OpCode.Ping );
                            BytesSent++;
                            pingCounter = 0;
                            MeasureBandwidthUseRates();
                        }
                        pingCounter++;
                        pollCounter = 0;                        
                    }
                    pollCounter++;

                    if( DateTime.UtcNow.Subtract( lastMovementUpdate ) > movementUpdateInterval ) {
                        UpdateVisibleEntities();
                        lastMovementUpdate = DateTime.UtcNow;
                    }

                    // send output to player
                    while (canSend && packetsSent < Server.MaxSessionPacketsPerTick)
                    {
                        if (!priorityOutputQueue.TryDequeue(out packet))
                        {
                            if (!outputQueue.TryDequeue(out packet))
                            {
                                // nothing more to send!
                                break;
                            }
                        }

                        if( IsDeaf && packet.OpCode == OpCode.Message ) continue;

                        writer.Write( packet.Bytes );
                        BytesSent += packet.Bytes.Length;
                        packetsSent++;

                        if( packet.OpCode == OpCode.Kick ) {
                            writer.Flush();
                            if( LeaveReason == LeaveReason.Unknown ) LeaveReason = LeaveReason.Kick;
                            return;
                        }

                        if( DateTime.UtcNow.Subtract( lastMovementUpdate ) > movementUpdateInterval ) {
                            UpdateVisibleEntities();
                            lastMovementUpdate = DateTime.UtcNow;
                        }
                    }

                    // check if player needs to change worlds
                    if( canSend ) {
                        lock( joinWorldLock ) {
                            if( forcedWorldToJoin != null ) {
                                while( priorityOutputQueue.TryDequeue( out packet ) ) {
                                    writer.Write( packet.Bytes );
                                    BytesSent += packet.Bytes.Length;
                                    packetsSent++;
                                    if( packet.OpCode == OpCode.Kick ) {
                                        writer.Flush();
                                        if( LeaveReason == LeaveReason.Unknown ) LeaveReason = LeaveReason.Kick;
                                        return;
                                    }
                                }
                                if( !JoinWorldNow( forcedWorldToJoin, useWorldSpawn, worldChangeReason ) ) {
                                    Logger.Log( LogType.Warning,
                                                "Player.IoLoop: Player was asked to force-join a world, but it was full." );
                                    KickNow( "World is full.", LeaveReason.ServerFull );
                                }
                                forcedWorldToJoin = null;
                            }
                        }

                        if( DateTime.UtcNow.Subtract( lastMovementUpdate ) > movementUpdateInterval ) {
                            UpdateVisibleEntities();
                            lastMovementUpdate = DateTime.UtcNow;
                        }
                    }


                    // get input from player
                    while( canReceive && stream.DataAvailable ) {
                        byte opcode = reader.ReadByte();
                        switch( (OpCode)opcode ) {

                            case OpCode.Message:
                                if( !ProcessMessagePacket() ) return;
                                break;

                            case OpCode.Teleport:
                                ProcessMovementPacket();
                                break;

                            case OpCode.SetBlockClient:
                                ProcessSetBlockPacket();
                                break;

                            case OpCode.PlayerClick:
                                ProcessPlayerClickPacket();
                                break;

                            case OpCode.Ping:
                                BytesReceived++;
                                continue;

                            default:
                                Logger.Log( LogType.SuspiciousActivity,
                                            "Player {0} was kicked after sending an invalid opcode ({1}).",
                                            Name, opcode );
                                KickNow( "Unknown packet opcode " + opcode,
                                         LeaveReason.InvalidOpcodeKick );
                                return;
                        }

                        if( DateTime.UtcNow.Subtract( lastMovementUpdate ) > movementUpdateInterval ) {
                            UpdateVisibleEntities();
                            lastMovementUpdate = DateTime.UtcNow;
                        }
                    }

                    Thread.Sleep( SleepDelay );
                }

            } catch( IOException ) {
                LeaveReason = LeaveReason.ClientQuit;

            } catch( SocketException ) {
                LeaveReason = LeaveReason.ClientQuit;
#if !DEBUG
            } catch( Exception ex ) {
                LeaveReason = LeaveReason.ServerError;
                Logger.LogAndReportCrash( "Error in Player.IoLoop", "ProCraft", ex, false );
#endif
            } finally {
                canQueue = false;
                canSend = false;
                Disconnect();
            }
        }
        #endregion

        bool ProcessMessagePacket() {
            BytesReceived += 66;
            ResetIdBotTimer();
            reader.ReadByte();
            string message = reader.ReadString();

            if( !IsSuper && message.StartsWith( "/womid " ) ) {
                IsUsingWoM = true;
                return true;
            }
            foreach (Char c in message) {
                if (c < ' ' || c > '~') {
                    message = message.Replace(c, ' ');
                }
            }

            /*if( message.Any( t => t < ' ' || t > '~' ) ) {
                Logger.Log( LogType.SuspiciousActivity,
                            "Player.ParseMessage: {0} attempted to write illegal characters in chat and was kicked.",
                            Name );
                Server.Message( "{0}&W was kicked for sending invalid chat.", ClassyName );
                KickNow( "Illegal characters in chat.", LeaveReason.InvalidMessageKick );
                return false;
            }*/

            if((message.IndexOf('&') != -1) && (!(Can(Permission.UseColorCodes)))) {
                message = Color.StripColors( message );
            }
#if DEBUG
            ParseMessage( message, false );
#else
            try {
                ParseMessage( message, false );
            } catch( IOException ) {
                throw;
            } catch( SocketException ) {
                throw;
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Error while parsing player's message", "ProCraft", ex, false );
                Message( "&WError while handling your message ({0}: {1})." +
                            "It is recommended that you reconnect to the server.",
                            ex.GetType().Name, ex.Message );
            }
#endif
            return true;
        }

        Queue<Position> posLog = new Queue<Position>();
        DateTime lastSpamTime = DateTime.MinValue;
        DateTime lastMoveTime = DateTime.MinValue;

        void ProcessMovementPacket() {
            BytesReceived += 10;
            byte id = reader.ReadByte();
            Block failsafe;
            if (Supports(CpeExtension.HeldBlock)) {
                if (Map.GetBlockByName(id.ToString(), false, out failsafe)) {
                    if (Info.heldBlock != failsafe) {
                        Info.heldBlock = failsafe;
                        if (Supports(CpeExtension.MessageType) && !IsPlayingCTF) {
                            Send(Packet.Message((byte)MessageType.BottomRight1, "&sBlock:&f" + failsafe.ToString() + " &sID:&f" + failsafe.GetHashCode()));
                        }
                    }
                } else {
                    Info.heldBlock = Block.Stone;
                }
            } else {
                Info.heldBlock = Block.None;
            }
            Position newPos = new Position {
                X = reader.ReadInt16(),
                Z = reader.ReadInt16(),
                Y = reader.ReadInt16(),
                R = reader.ReadByte(),
                L = reader.ReadByte()
            };

            Position oldPos = Position;

            /* string[] hexRandom = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f" };
             Random RandomHexString = new Random();
             string sky = hexRandom[RandomHexString.Next(0, hexRandom.Length)] + hexRandom[RandomHexString.Next(0, hexRandom.Length)] + hexRandom[RandomHexString.Next(0, hexRandom.Length)] + hexRandom[RandomHexString.Next(0, hexRandom.Length)] + hexRandom[RandomHexString.Next(0, hexRandom.Length)] + hexRandom[RandomHexString.Next(0, hexRandom.Length)];
             string cloud = hexRandom[RandomHexString.Next(0, hexRandom.Length)] + hexRandom[RandomHexString.Next(0, hexRandom.Length)] + hexRandom[RandomHexString.Next(0, hexRandom.Length)] + hexRandom[RandomHexString.Next(0, hexRandom.Length)] + hexRandom[RandomHexString.Next(0, hexRandom.Length)] + hexRandom[RandomHexString.Next(0, hexRandom.Length)];
             string fog = hexRandom[RandomHexString.Next(0, hexRandom.Length)] + hexRandom[RandomHexString.Next(0, hexRandom.Length)] + hexRandom[RandomHexString.Next(0, hexRandom.Length)] + hexRandom[RandomHexString.Next(0, hexRandom.Length)] + hexRandom[RandomHexString.Next(0, hexRandom.Length)] + hexRandom[RandomHexString.Next(0, hexRandom.Length)];
             this.Send(Packet.MakeEnvSetColor((byte)EnvVariable.SkyColor, sky));
             this.Send(Packet.MakeEnvSetColor((byte)EnvVariable.CloudColor, cloud));
             this.Send(Packet.MakeEnvSetColor((byte)EnvVariable.FogColor, fog));
             string[] hexRandom = { "ff5555", "aa0000", "ffaa00", "ffff55", "55ff55", "555555", "55ffff", "00aaaa", "5555ff", "0000aa", "ff55ff", "aa00aa"};
             int totalhex = 0;
             for (; totalhex < hexRandom.Count(); totalhex++)
             {
                 this.Send(Packet.MakeEnvSetColor((byte)EnvVariable.SkyColor, hexRandom[totalhex]));
             }
             if (totalhex == 11) totalhex = 0;*/

            // calculate difference between old and new positions
            Position delta = new Position {
                X = (short)( newPos.X - oldPos.X ),
                Y = (short)( newPos.Y - oldPos.Y ),
                Z = (short)( newPos.Z - oldPos.Z ),
                R = (byte)Math.Abs( newPos.R - oldPos.R ),
                L = (byte)Math.Abs( newPos.L - oldPos.L )
            };

            // skip everything if player hasn't moved
            if( delta.IsZero ) return;

            bool posChanged = (delta.X != 0) || (delta.Y != 0) || (delta.Z != 0);
            bool rotChanged = (delta.R != 0) || (delta.L != 0);
            
            //if(rotChanged && !this.isSolid) ResetIdBotTimer();
            //if(posChanged && this.isSolid) ResetIdBotTimer();
            if (rotChanged) ResetIdBotTimer();
            /*if (posChanged)
            {
                posLog.Enqueue(newPos);
                lastMoveTime = DateTime.UtcNow;
            }
            else
            {
                if (lastMoveTime > lastSpamTime && DateTime.UtcNow.Subtract(lastMoveTime) > TimeSpan.FromSeconds(0.5))
                {
                    SpamMovementStats();
                    lastSpamTime = DateTime.UtcNow;
                }
            }*/

            bool deniedzone = false;

            foreach (Zone zone in World.Map.Zones.Cache)
            {
                #region special zones

                #region DenyLower
                //Player.Console.Message("&sTesting Zone: {0}", zone.Name);
                if (zone.Name.ToLower().StartsWith("deny_"))
                {
                    //Player.Console.Message("&sGot a Zone: {0}", zone.Name);
                    //Player.Console.Message("&sTesting Ranks: {0} -> {1}", zone.Controller.MinRank.Name, Info.Rank.Name);
                    //Player.Console.Message("&sTesting Ranks: {0} < {1}", RankManager.GetIndex(zone.Controller.MinRank).ToString(), RankManager.GetIndex(Info.Rank).ToString());
                    if ((zone.Controller.MinRank > Info.Rank) && (zone.Controller.ExceptionList.Included.Contains(Info) == false) || (zone.Controller.ExceptionList.Excluded.Contains(Info) == true))
                    {
                        lastValidPosition = Position;                            
                        //Player.Console.Message("&sFound A Zone That Would Deny {0}: {1}", Info.Name, zone.Name);
                        //Player.Console.Message("&sTesting Co-Ords {0},{1},{2}: {3}{4}", newPos.X.ToString(), newPos.Y.ToString(), newPos.Z.ToString(), zone.Bounds.MinVertex.ToString(), zone.Bounds.MaxVertex.ToString());
                        if (zone.Bounds.Contains(newPos.X / 32, newPos.Y /32, newPos.Z /32))
                        {
                            deniedzone = true;
                            if (zone.Sign == null)
                            {
                                FileInfo SignInfo = new FileInfo("./signs/" + World.Name + "/" + zone.Name + ".txt");
                                string SignMessage = "";
                                if (SignInfo.Exists)
                                {
                                    string[] SignList = File.ReadAllLines("./signs/" + World.Name + "/" + zone.Name + ".txt");
                                    foreach (string line in SignList)
                                    {
                                        SignMessage += line + "&n";
                                    }
                                    
                                }
                                //else Message("&WSignFile for this signpost not found!&n.Looking For: &s./signs/" + World.Name + "/" + deniedZone.Name + "&w.");
                                else SignMessage = "&WYou must be atleast rank " + zone.Controller.MinRank.Name + "&w to enter this area.";
                                if ((DateTime.UtcNow - LastZoneNotification).Seconds > 5)
                                {
                                    Message(SignMessage);
                                    LastZoneNotification = DateTime.UtcNow;
                                }
                                if (!Info.IsFrozen)
                                {
                                    SendNow(Packet.MakeSelfTeleport(new Position
                                    {
                                        X = (short)((lastValidPosition.X / 32) * 32),
                                        Y = (short)((lastValidPosition.Y / 32) * 32),
                                        Z = (short)(lastValidPosition.Z + 22),
                                        R = lastValidPosition.R,
                                        L = lastValidPosition.L
                                    }));
                                }
                            }
                            break;
                        }
                    }
                }
                #endregion                

                #region Message
                //Player.Console.Message("&sTesting Zone: {0}", zone.Name);
                if (zone.Name.ToLower().StartsWith("text_"))
                {
                    //Player.Console.Message("&sGot a Zone: {0}", zone.Name);
                    //Player.Console.Message("&sTesting Ranks: {0} -> {1}", zone.Controller.MinRank.Name, Info.Rank.Name);
                    //Player.Console.Message("&sTesting Ranks: {0} < {1}", RankManager.GetIndex(zone.Controller.MinRank).ToString(), RankManager.GetIndex(Info.Rank).ToString());
                    if ((zone.Controller.MinRank > Info.Rank) && (zone.Controller.ExceptionList.Included.Contains(Info) == false) || (zone.Controller.ExceptionList.Excluded.Contains(Info) == true))
                    {
                        //Player.Console.Message("&sFound A Zone That Would Deny {0}: {1}", Info.Name, zone.Name);
                        //Player.Console.Message("&sTesting Co-Ords {0},{1},{2}: {3}{4}", newPos.X.ToString(), newPos.Y.ToString(), newPos.Z.ToString(), zone.Bounds.MinVertex.ToString(), zone.Bounds.MaxVertex.ToString());
                        if (zone.Bounds.Contains(newPos.X / 32, newPos.Y / 32, newPos.Z / 32))
                        {
                            if (zone.Sign == null)
                            {
                                FileInfo SignInfo = new FileInfo("./signs/" + World.Name + "/" + zone.Name + ".txt");
                                string SignMessage = "";
                                if (SignInfo.Exists)
                                {
                                    string[] SignList = File.ReadAllLines("./signs/" + World.Name + "/" + zone.Name + ".txt");
                                    foreach (string line in SignList)
                                    {
                                        SignMessage += line + "&n";
                                    }

                                }
                                //else Message("&WSignFile for this signpost not found!&n.Looking For: &s./signs/" + World.Name + "/" + deniedZone.Name + "&w.");
                                else SignMessage = "&WThis zone is marked as a text area, but no text is added to the message!";
                                if ((DateTime.UtcNow - LastZoneNotification).Seconds > 5)
                                {
                                    Message(SignMessage);
                                    LastZoneNotification = DateTime.UtcNow;
                                }
                            }
                            break;
                        }
                    }
                }
                #endregion

                #region RespawnLower
                //Player.Console.Message("&sTesting Zone: {0}", zone.Name);
                if (zone.Name.ToLower().StartsWith("respawn_"))
                {
                    //Player.Console.Message("&sGot a Zone: {0}", zone.Name);
                    //Player.Console.Message("&sTesting Ranks: {0} -> {1}", zone.Controller.MinRank.Name, Info.Rank.Name);
                    //Player.Console.Message("&sTesting Ranks: {0} < {1}", RankManager.GetIndex(zone.Controller.MinRank).ToString(), RankManager.GetIndex(Info.Rank).ToString());
                    if ((zone.Controller.MinRank > Info.Rank) && (zone.Controller.ExceptionList.Included.Contains(Info) == false) || (zone.Controller.ExceptionList.Excluded.Contains(Info) == true))
                    {
                        //Player.Console.Message("&sFound A Zone That Would Deny {0}: {1}", Info.Name, zone.Name);
                        //Player.Console.Message("&sTesting Co-Ords {0},{1},{2}: {3}{4}", newPos.X.ToString(), newPos.Y.ToString(), newPos.Z.ToString(), zone.Bounds.MinVertex.ToString(), zone.Bounds.MaxVertex.ToString());
                        if (zone.Bounds.Contains(newPos.X / 32, newPos.Y / 32, newPos.Z / 32))
                        {
                            deniedzone = true;
                            if (zone.Sign == null)
                            {
                                FileInfo SignInfo = new FileInfo("./signs/" + World.Name + "/" + zone.Name + ".txt");
                                string SignMessage = "";
                                if (SignInfo.Exists)
                                {
                                    string[] SignList = File.ReadAllLines("./signs/" + World.Name + "/" + zone.Name + ".txt");
                                    foreach (string line in SignList)
                                    {
                                        SignMessage += line + "&n";
                                    }

                                }
                                //else Message("&WSignFile for this signpost not found!&n.Looking For: &s./signs/" + World.Name + "/" + deniedZone.Name + "&w.");
                                else SignMessage = "&WThis zone is marked as a deny area, but no text is added to the deny message! Regardless, you are not permitted to enter this area.";
                                if ((DateTime.UtcNow - LastZoneNotification).Seconds > 5)
                                {
                                    Message(SignMessage);
                                    LastZoneNotification = DateTime.UtcNow;
                                }
                                if (!Info.IsFrozen)
                                {
                                    TeleportTo(this.WorldMap.Spawn);
                                }
                            }
                            break;
                        }
                    }
                }
                #endregion

                #region CheckPoint
                if (zone.Name.ToLower().StartsWith("checkpoint_") && this.Info.CheckPoint != new Position(((zone.Bounds.XMin + zone.Bounds.XMax) / 2) * 32 + 16, ((zone.Bounds.YMin + zone.Bounds.YMax) / 2) * 32 + 16, ((zone.Bounds.ZMin + zone.Bounds.ZMax) / 2) * 32 + 64))
                {
                    if (zone.Controller.ExceptionList.Excluded.Contains(Info) == false)
                    {
                        if (zone.Bounds.Contains(newPos.X / 32, newPos.Y / 32, newPos.Z / 32))
                        {
                            if (zone.Sign == null)
                            {
                                FileInfo SignInfo = new FileInfo("./signs/" + World.Name + "/" + zone.Name + ".txt");
                                string SignMessage = "";
                                if (SignInfo.Exists)
                                {
                                    string[] SignList = File.ReadAllLines("./signs/" + World.Name + "/" + zone.Name + ".txt");
                                    foreach (string line in SignList)
                                    {
                                        SignMessage += line + "&n";
                                    }

                                }
                                else SignMessage = "&aCheckPoint &sreached! This is now your respawn point.";
                                Message(SignMessage);
                                LastZoneNotification = DateTime.UtcNow;
                                this.Info.CheckPoint = new Position(((zone.Bounds.XMin + zone.Bounds.XMax) / 2) * 32 + 16, ((zone.Bounds.YMin + zone.Bounds.YMax) / 2) * 32 + 16, ((zone.Bounds.ZMin + zone.Bounds.ZMax) / 2) * 32 + 64);
                            }
                            break;
                        }
                    }
                }
                #endregion

                #region Death
                if (zone.Name.ToLower().StartsWith("death_"))
                {
                    if (this.Info.CheckPoint != null)
                    {
                        if (zone.Bounds.Contains(newPos.X / 32, newPos.Y / 32, newPos.Z / 32))
                        {
                            deniedzone = true;
                            if (zone.Sign == null)
                            {
                                FileInfo SignInfo = new FileInfo("./signs/" + World.Name + "/" + zone.Name + ".txt");
                                string SignMessage = "";
                                if (SignInfo.Exists)
                                {
                                    string[] SignList = File.ReadAllLines("./signs/" + World.Name + "/" + zone.Name + ".txt");
                                    foreach (string line in SignList)
                                    {
                                        SignMessage += line + "&n";
                                    }

                                }
                                else SignMessage = "&WYou Died!";
                                if ((DateTime.UtcNow - LastZoneNotification).Seconds > 1)
                                {
                                    Message(SignMessage);
                                    LastZoneNotification = DateTime.UtcNow;
                                }
                                if (!Info.IsFrozen)
                                {
                                    if (this.Info.CheckPoint.X != 0 && this.Info.CheckPoint.Y != 0 && this.Info.CheckPoint.Z != 0)
                                    {
                                        TeleportTo(new Position(this.Info.CheckPoint.X, this.Info.CheckPoint.Y, this.Info.CheckPoint.Z, this.Position.R, this.Position.L));
                                    }
                                    else
                                    {
                                        TeleportTo(this.WorldMap.Spawn);
                                    }
                                }

                            }
                            break;
                        }
                    }
                }
                #endregion

                #endregion
            }           

            if( Info.IsFrozen || deniedzone) {
                // special handling for frozen players
                if( delta.X * delta.X + delta.Y * delta.Y > AntiSpeedMaxDistanceSquared ||
                    Math.Abs( delta.Z ) > 40 ) {
                    SendNow( Packet.MakeSelfTeleport( Position ) );
                }
                newPos.X = Position.X;
                newPos.Y = Position.Y;
                newPos.Z = Position.Z;

                // recalculate deltas
                delta.X = 0;
                delta.Y = 0;
                delta.Z = 0;

            }

            else if( !Can( Permission.UseSpeedHack ) || !Info.AllowSpeedhack) {
                int distSquared = delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z;
                // speedhack detection
                if( DetectMovementPacketSpam() ) {
                    return;

                } else if( ( distSquared - delta.Z * delta.Z > AntiSpeedMaxDistanceSquared ||
                             delta.Z > AntiSpeedMaxJumpDelta ) &&
                           speedHackDetectionCounter >= 0 ) {

                    if( speedHackDetectionCounter == 0 ) {
                        lastValidPosition = Position;
                    } else if( speedHackDetectionCounter > 1 ) {
                        DenyMovement();
                        speedHackDetectionCounter = 0;
                        return;
                    }
                    speedHackDetectionCounter++;

                } else {
                    speedHackDetectionCounter = 0;
                }
            }

            if( RaisePlayerMovingEvent( this, newPos ) ) {
                DenyMovement();
                return;
            }

            Position = newPos;
            RaisePlayerMovedEvent( this, oldPos );
        }

        void SpamMovementStats()
        {
            short[] zs = posLog.Select(pos => pos.Z).ToArray();
            int minDelta = int.MaxValue,
                maxDelta = int.MinValue,
                totalDelta = 0,
                totalDisplacement = 0,
                minZ = zs.Min(),
                maxZ = zs.Max(),
                maxDisplacement = Math.Abs(minZ - maxZ);
            for (int i = 1; i < zs.Length; i++)
            {
                int deltaZ = zs[i] - zs[i - 1];
                minDelta = Math.Min(minDelta, deltaZ);
                maxDelta = Math.Max(maxDelta, deltaZ);
                totalDelta += Math.Abs(deltaZ);
                totalDisplacement += deltaZ;
            }
            Message("{0:HH:mm:ss} &FHeight: Min={1} Max={2} &CJumpHeight={3}",
                DateTime.UtcNow, minZ, maxZ, maxDisplacement);
            Message("&FZ-Velocity: Min={0} Max={1} | Dist: {2} | Displ: {3}",
                minDelta, maxDelta, totalDelta, totalDisplacement);
            Message("");
            posLog.Clear();
        }


        void ProcessSetBlockPacket() {
            BytesReceived += 9;
            if( World == null || World.Map == null ) return;
            ResetIdBotTimer();
            short x = reader.ReadInt16();
            short z = reader.ReadInt16();
            short y = reader.ReadInt16();
            ClickAction action = ( reader.ReadByte() == 1 ) ? ClickAction.Build : ClickAction.Delete;
            byte type = reader.ReadByte();

            // if a player is using InDev or SurvivalTest client, they may try to
            // place blocks that are not found in MC Classic. Convert them!
            if( type > 68 ) {
                type = MapDat.MapBlock( type );
            }

            Vector3I coords = new Vector3I( x, y, z );

            // If block is in bounds, count the click.
            // Sometimes MC allows clicking out of bounds,
            // like at map transitions or at the top layer of the world.
            // Those clicks should be simply ignored.
            if( World.Map.InBounds( coords ) ) {
                var e = new PlayerClickingEventArgs( this, coords, action, (Block)type );
                if( RaisePlayerClickingEvent( e ) ) {
                    RevertBlockNow( coords );
                } else {
                    RaisePlayerClickedEvent( this, coords, e.Action, e.Block );
                    PlaceBlock( coords, e.Action, e.Block );
                    Info.LastWorld = this.World.ClassyName;
                    Info.LastWorldPos = this.Position.ToString();
                }
            }
        }

        void ProcessPlayerClickPacket() {
            BytesReceived += 15;
            byte button = reader.ReadByte();
            byte action = reader.ReadByte();
            short pitch = reader.ReadInt16();
            short yaw = reader.ReadInt16();
            byte targetEntityID = reader.ReadByte();
            short targetBlockX = reader.ReadInt16();
            short targetBlockY = reader.ReadInt16();
            short targetBlockZ = reader.ReadInt16();
            byte targetBlockFace = reader.ReadByte();
        }


        void Disconnect() {
            State = SessionState.Disconnected;
            Server.RaiseSessionDisconnectedEvent( this, LeaveReason );

            if( HasRegistered ) {
                lock( kickSyncLock ) {
                    if( useSyncKick ) {
                        syncKickWaiter.Set();
                    } else {
                        Server.UnregisterPlayer( this );
                    }
                }
                RaisePlayerDisconnectedEvent( this, LeaveReason );
            }

            if( stream != null ) stream.Close();
            if( client != null ) client.Close();
        }

        bool LoginSequence()
        {
            byte opCode = reader.ReadByte();

#if DEBUG_NETWORKING
            Logger.Log( LogType.Trace, "from {0} [{1}] {2}", IP, outPacketNumber++, (OpCode)opCode );
#endif

            switch (opCode)
            {
                case (byte)OpCode.Handshake:
                    break;

                case 2:
                case 250:
                    GentlyKickSMPClients();
                    return false;

                case 15:
                case 254:
                    // ignore SMP pings
                    return false;

                case (byte)'G':
                    return false;

                default:
                    Logger.Log(LogType.Error,
                                "Player.LoginSequence: Unexpected op code in the first packet from {0}: {1}.",
                                IP,
                                opCode);
                    KickNow("Incompatible client, or a network error.", LeaveReason.ProtocolViolation);
                    return false;
            }

            // Check protocol version
            int clientProtocolVersion = reader.ReadByte();
            if (clientProtocolVersion != Config.ProtocolVersion)
            {
                Logger.Log(LogType.Error,
                            "Player.LoginSequence: Wrong protocol version: {0}.",
                            clientProtocolVersion);
                KickNow("Incompatible protocol version!", LeaveReason.ProtocolViolation);
                return false;
            }

            string givenName = reader.ReadString();
            string verificationCode = reader.ReadString();
            bool supportsCpe = (reader.ReadByte() == 0x42);
            BytesReceived += 131;

            bool isEmailAccount = false;
            if (IsValidEmail(givenName)) {
                isEmailAccount = true;
            }
            else if (!IsValidAccountName(givenName)) {
                // Neither Mojang nor a normal account -- kick it!
                Logger.Log(LogType.SuspiciousActivity,
                            "Player.LoginSequence: Unacceptable player name: {0} ({1})",
                            givenName,
                            IP);
                KickNow("Unacceptable player name!", LeaveReason.ProtocolViolation);
                return false;
            }

            
            if (Server.VerifyName(givenName, verificationCode, Heartbeat.KickSalt))
            {
                KickNow("Please go to " + Heartbeat.HeartbeatServerUrl.Host + " to join our server!", LeaveReason.ClientQuit);
                return false;
            }

            Info = PlayerDB.FindOrCreateInfoForPlayer( givenName, IP );
            ResetAllBinds();
            if (isEmailAccount) {
                Logger.Log( LogType.SystemActivity,
                            "Mojang account <{0}> connected as {1}",
                            givenName,
                            Info.Name );
            }
            if (Server.VerifyName(givenName, verificationCode, Heartbeat.Salt))
            {
                IsVerified = true;
                // update capitalization of player's name
                if (!Info.Name.Equals(givenName, StringComparison.Ordinal))
                {
                    Info.Name = givenName;
                }

            }
            else
            {
                NameVerificationMode nameVerificationMode = ConfigKey.VerifyNames.GetEnum<NameVerificationMode>();

                string standardMessage =
                    String.Format("Player.LoginSequence: Could not verify player name for {0} ({1}).",
                                   Name, IP);
                if (IP.Equals(IPAddress.Loopback) && nameVerificationMode != NameVerificationMode.Always)
                {
                    Logger.Log(LogType.SuspiciousActivity,
                                "{0} Player was identified as connecting from localhost and allowed in.",
                                standardMessage);
                    IsVerified = true;

                }
                else if (IP.IsLocal() && ConfigKey.AllowUnverifiedLAN.Enabled())
                {
                    Logger.Log(LogType.SuspiciousActivity,
                                "{0} Player was identified as connecting from LAN and allowed in.",
                                standardMessage);
                    IsVerified = true;

                }
                else if (Info.TimesVisited > 1 && Info.LastIP.Equals(IP))
                {
                    switch (nameVerificationMode)
                    {
                        case NameVerificationMode.Always:
                            Info.ProcessFailedLogin(this);
                            Logger.Log(LogType.SuspiciousActivity,
                                        "{0} IP matched previous records for that name. " +
                                        "Player was kicked anyway because VerifyNames is set to Always.",
                                        standardMessage);
                            KickNow("Could not verify player name!", LeaveReason.UnverifiedName);
                            return false;

                        case NameVerificationMode.Balanced:
                        case NameVerificationMode.Never:
                            Logger.Log(LogType.SuspiciousActivity,
                                        "{0} IP matched previous records for that name. Player was allowed in.",
                                        standardMessage);
                            IsVerified = true;
                            break;
                    }

                }
                else
                {
                    switch (nameVerificationMode)
                    {
                        case NameVerificationMode.Always:
                        case NameVerificationMode.Balanced:
                            Info.ProcessFailedLogin(this);
                            Logger.Log(LogType.SuspiciousActivity,
                                        "{0} IP did not match. Player was kicked.",
                                        standardMessage);
                            KickNow("Could not verify player name!", LeaveReason.UnverifiedName);
                            return false;

                        case NameVerificationMode.Never:
                            Logger.Log(LogType.SuspiciousActivity,
                                        "{0} IP did not match. Player was allowed in anyway because VerifyNames is set to Never.",
                                        standardMessage);
                            Message("&WYour name could not be verified.");
                            break;
                    }
                }
            }

            // Check if player is banned
            if (Info.IsBanned)
            {
                Info.ProcessFailedLogin(this);
                Logger.Log(LogType.SuspiciousActivity,
                            "Banned player {0} tried to log in from {1}",
                            Name, IP);
                string bannedMessage;
                if (Info.BannedBy != null)
                {
                    if (Info.BanReason != null)
                    {
                        bannedMessage = String.Format("Banned {0} ago by {1}: {2}",
                                                       Info.TimeSinceBan.ToMiniString(),
                                                       Info.BannedBy,
                                                       Info.BanReason);
                    }
                    else
                    {
                        bannedMessage = String.Format("Banned {0} ago by {1}",
                                                       Info.TimeSinceBan.ToMiniString(),
                                                       Info.BannedBy);
                    }
                }
                else
                {
                    if (Info.BanReason != null)
                    {
                        bannedMessage = String.Format("Banned {0} ago: {1}",
                                                       Info.TimeSinceBan.ToMiniString(),
                                                       Info.BanReason);
                    }
                    else
                    {
                        bannedMessage = String.Format("Banned {0} ago",
                                                       Info.TimeSinceBan.ToMiniString());
                    }
                }
                KickNow(bannedMessage, LeaveReason.LoginFailed);
                return false;
            }


            // Check if player's IP is banned
            IPBanInfo ipBanInfo = IPBanList.Get(IP);
            if (ipBanInfo != null && Info.BanStatus != BanStatus.IPBanExempt)
            {
                Info.ProcessFailedLogin(this);
                ipBanInfo.ProcessAttempt(this);
                Logger.Log(LogType.SuspiciousActivity,
                            "{0} tried to log in from a banned IP.", Name);
                string bannedMessage = String.Format("IP-banned {0} ago by {1}: {2}",
                                                      DateTime.UtcNow.Subtract(ipBanInfo.BanDate).ToMiniString(),
                                                      ipBanInfo.BannedBy,
                                                      ipBanInfo.BanReason);
                KickNow(bannedMessage, LeaveReason.LoginFailed);
                return false;
            }


            // Check if player is paid (if required)
            if (ConfigKey.PaidPlayersOnly.Enabled() && Info.AccountType != AccountType.Paid)
            {
                SendNow(Packet.MakeHandshake(this,
                                               ConfigKey.ServerName.GetString(),
                                               "Please wait; Checking paid status..."));
                writer.Flush();

                Info.AccountType = CheckPaidStatus(Name);
                if (Info.AccountType != AccountType.Paid)
                {
                    Logger.Log(LogType.SystemActivity,
                                "Player {0} was kicked because their account is not paid, and PaidPlayersOnly setting is enabled.",
                                Name);
                    KickNow("Paid players allowed only.", LeaveReason.LoginFailed);
                    return false;
                }
            }
            else
            {
                Info.CheckAccountType();
            }


            // Any additional security checks should be done right here
            if (RaisePlayerConnectingEvent(this)) return false;


            // ----==== beyond this point, player is considered connecting (allowed to join) ====----


            // negotiate protocol extensions
            if (supportsCpe && !NegotiateProtocolExtension())
            {
                return false;
            }


            // Register player for future block updates
            if (!Server.RegisterPlayer(this))
            {
                Logger.Log(LogType.SystemActivity,
                            "Player {0} was kicked because server is full.", Name);
                string kickMessage = String.Format("Sorry, server is full ({0}/{1})",
                                                    Server.Players.Length, ConfigKey.MaxPlayers.GetInt());
                KickNow(kickMessage, LeaveReason.ServerFull);
                return false;
            }
            Info.ProcessLogin(this);
            State = SessionState.LoadingMain;


            // ----==== Beyond this point, player is considered connected (authenticated and registered) ====----
            Logger.Log(LogType.UserActivity, "{0} &sconnected from {1}.", Name, IP);


            // Figure out what the starting world should be
            World startingWorld = WorldManager.FindMainWorld(this);
            startingWorld = RaisePlayerConnectedEvent(this, startingWorld);
            Position = startingWorld.LoadMap().Spawn;

            // Send server information
            string serverName = ConfigKey.ServerName.GetString();
            string motd;
            FileInfo MOTDInfo = new FileInfo("./MOTDList.txt");
            if (MOTDInfo.Exists)
            {
                string[] MOTDlist = File.ReadAllLines("./MOTDList.txt");
                Array.Sort(MOTDlist);
                Random random = new Random();
                int index = random.Next(0, MOTDlist.Length);
                motd = MOTDlist[index];
                string sName = ConfigKey.ServerName.ToString();
                if (motd.Length > 64) motd = "Welcome to " + (sName.Length > 52 ? sName.Remove(52) : sName) + "!";
                else motd = "&0=&c=&e= " + motd + " &e=&c=&0=";
                //motd = ConfigKey.MOTD.GetString();
            }
            else motd = ConfigKey.MOTD.GetString();
            SendNow(Packet.MakeHandshake(this, serverName, motd));

            // AutoRank
            if (ConfigKey.AutoRankEnabled.Enabled())
            {
                Rank newRank = AutoRankManager.Check(Info);
                if (newRank != null)
                {
                    try
                    {
                        Info.ChangeRank(AutoRank, newRank, "~AutoRank", true, true, true);
                    }
                    catch (PlayerOpException ex)
                    {
                        Logger.Log(LogType.Error,
                                    "AutoRank failed on player {0}: {1}",
                                    ex.Player.Name, ex.Message);
                    }
                }
            }

            bool firstTime = (Info.TimesVisited == 1);
            if (!JoinWorldNow(startingWorld, true, WorldChangeReason.FirstWorld))
            {
                Logger.Log(LogType.Warning,
                            "Could not load main world ({0}) for connecting player {1} (from {2}): " +
                            "Either main world is full, or an error occurred.",
                            startingWorld.Name, Name, IP);
                KickNow("Either main world is full, or an error occurred.", LeaveReason.WorldFull);
                return false;
            }


            // ==== Beyond this point, player is considered ready (has a world) ====

            var canSee = Server.Players.CanSee(this).ToArray();

            // Announce join
            if (ConfigKey.ShowConnectionMessages.Enabled())
            {
                string message = Server.MakePlayerConnectedMessage(this, firstTime, World);
                canSee.Message(message);
            }

            if (!IsVerified)
            {
                canSee.Message("&WName and IP of {0}&W are unverified!", ClassyName);
            }

            if (Info.IsHidden)
            {
                if (Can(Permission.Hide))
                {
                    canSee.Message("&8Player {0}&8 logged in hidden.", ClassyName);
                }
                else
                {
                    Info.IsHidden = false;
                }
            }
            Info.GeoipLogin();

            // Check if other banned players logged in from this IP
            PlayerInfo[] bannedPlayerNames = PlayerDB.FindPlayers(IP, 25)
                                                     .Where(playerFromSameIP => playerFromSameIP.IsBanned)
                                                     .ToArray();
            if (bannedPlayerNames.Length > 0)
            {
                canSee.Message("&WPlayer {0}&W logged in from an IP shared by banned players: {1}",
                                ClassyName, bannedPlayerNames.JoinToClassyString());
                Logger.Log(LogType.SuspiciousActivity,
                            "Player {0} logged in from an IP shared by banned players: {1}",
                            ClassyName, bannedPlayerNames.JoinToString(info => info.Name));
            }

            // check if player is still muted
            if (Info.MutedUntil > DateTime.UtcNow)
            {
                Message("&WYou were previously muted by {0}&W, {1} left.",
                         Info.MutedByClassy, Info.TimeMutedLeft.ToMiniString());
                canSee.Message("&WPlayer {0}&W was previously muted by {1}&W, {2} left.",
                                ClassyName, Info.MutedByClassy, Info.TimeMutedLeft.ToMiniString());
            }

            // check if player is still frozen
            if (Info.IsFrozen)
            {
                if (Info.FrozenOn != DateTime.MinValue)
                {
                    Message("&WYou were previously frozen {0} ago by {1}. This means you can not move or place/delete blocks. Seek Moderator Assistance.",
                             Info.TimeSinceFrozen.ToMiniString(),
                             Info.FrozenByClassy);
                    canSee.Message("&WPlayer {0}&W was previously frozen {1} ago by {2}",
                                    ClassyName,
                                    Info.TimeSinceFrozen.ToMiniString(),
                                    Info.FrozenByClassy);
                }
                else
                {
                    Message("&WYou were previously frozen by {0}. This means you can not move or place/delete blocks. Seek Moderator Assistance.",
                             Info.FrozenByClassy);
                    canSee.Message("&WPlayer {0}&W was previously frozen by {1}.",
                                    ClassyName, Info.FrozenByClassy);
                }
            }

            // Welcome message
            if (File.Exists(Paths.GreetingFileName))
            {
                string[] greetingText = File.ReadAllLines(Paths.GreetingFileName);
                foreach (string greetingLine in greetingText)
                {
                    Message(Chat.ReplaceTextKeywords(this, greetingLine));
                }
            }
            else
            {
                if (firstTime)
                {
                    Message("Welcome to {0}", ConfigKey.ServerName.GetString());
                }
                else
                {
                    Message("Welcome back to {0}", ConfigKey.ServerName.GetString());
                }

                Message("Your rank is {0}&S. Type &H/Help&S for help.",
                            Info.Rank.ClassyName);
            }
			if (Info.Rank == RankManager.HighestRank) {
				if (Chat.Reports.Count() >= 1) {
					Message(Chat.Reports.Count() + " unread /Reports");
				}
			}

            // A reminder for first-time users
            if (PlayerDB.Size == 1 && Info.Rank != RankManager.HighestRank)
            {
                Message("Type &H/Rank {0} {1}&S in konsole to promote yourself",
                         Name, RankManager.HighestRank.Name);
            }
            
            MaxCopySlots = Info.Rank.CopySlots;

            HasFullyConnected = true;
            State = SessionState.Online;

            // Add player to the userlist
            lock (syncKickWaiter)
            {
                if (!useSyncKick)
                {
                    Server.UpdatePlayerList();
                }
            }            

            RaisePlayerReadyEvent(this);

			if (Supports(CpeExtension.MessageType)) {
				Send(Packet.Message((byte)MessageType.Status1, "&f" + ConfigKey.ServerName.GetString()));
			}

            short NID = 1;
            this.NameID = NID;
            retry:
            foreach (Player player in Server.Players)
            {
                if (this.NameID == player.NameID && this.Info.PlayerObject != player.Info.PlayerObject)
                {
                    this.NameID++;
                    goto retry;
                }
            }
            if (Info.skinName == null) {
                Info.oldskinName = Info.skinName;
                Info.skinName = Name;
            }
            Server.UpdateTabList();
            System.Console.Beep();
            System.Console.Beep();
            return true;
        }


        void GentlyKickSMPClients() {
            // This may be someone connecting with an SMP client
            int strLen = reader.ReadInt16();

            if( strLen >= 2 && strLen <= 16 ) {
                string smpPlayerName = Encoding.BigEndianUnicode.GetString( reader.ReadBytes( strLen * 2 ) );

                Logger.Log( LogType.Warning,
                            "Player.LoginSequence: Player \"{0}\" tried connecting with Minecraft Beta client from {1}. " +
                            "ProCraft does not support Minecraft Beta.",
                            smpPlayerName, IP );

                // send SMP KICK packet
                writer.Write( (byte)255 );
                byte[] stringData = Encoding.BigEndianUnicode.GetBytes( NoSmpMessage );
                writer.Write( (short)NoSmpMessage.Length );
                writer.Write( stringData );
                BytesSent += ( 1 + stringData.Length );
                writer.Flush();

            } else {
                // Not SMP client (invalid player name length)
                Logger.Log( LogType.Error,
                            "Player.LoginSequence: Unexpected opcode in the first packet from {0}: 2.", IP );
                KickNow( "Unexpected handshake message - possible protocol mismatch!", LeaveReason.ProtocolViolation );
            }
        }


        #region Joining Worlds

        readonly object joinWorldLock = new object();

        [CanBeNull] World forcedWorldToJoin;
        WorldChangeReason worldChangeReason;
        Position postJoinPosition;
        bool useWorldSpawn;


        public void JoinWorld( [NotNull] World newWorld, WorldChangeReason reason ) {
            if( newWorld == null ) throw new ArgumentNullException( "newWorld" );
            lock( joinWorldLock ) {
                useWorldSpawn = true;
                postJoinPosition = Position.Zero;
                forcedWorldToJoin = newWorld;
                worldChangeReason = reason;
            }
        }


        public void JoinWorld( [NotNull] World newWorld, WorldChangeReason reason, Position position ) {
            if( newWorld == null ) throw new ArgumentNullException( "newWorld" );
            if( !Enum.IsDefined( typeof( WorldChangeReason ), reason ) ) {
                throw new ArgumentOutOfRangeException( "reason" );
            }
            lock( joinWorldLock ) {
                useWorldSpawn = false;
                postJoinPosition = position;
                forcedWorldToJoin = newWorld;
                worldChangeReason = reason;
            }
        }


        internal bool JoinWorldNow([NotNull] World newWorld, bool doUseWorldSpawn, WorldChangeReason reason)
        {
            if (newWorld == null) throw new ArgumentNullException("newWorld");
            if (!Enum.IsDefined(typeof(WorldChangeReason), reason))
            {
                throw new ArgumentOutOfRangeException("reason");
            }
            /*if (Thread.CurrentThread != ioThread)
            {
                throw new InvalidOperationException(
                    "Player.JoinWorldNow may only be called from player's own thread. " +
                    "Use Player.JoinWorld instead.");
            }*/

            string textLine1 = ConfigKey.ServerName.GetString();
            string textLine2;

            if ((newWorld.Name.ToLower() == "ctf" || newWorld.Name.ToLower() == "maze" || newWorld.Name.ToLower() == "parkour") && this.Info.Rank != RankManager.HighestRank)
            {
                textLine2 = "Loading world " + newWorld.ClassyName + " &f-hax -respawn";
            }
            else textLine2 = "Loading world " + newWorld.ClassyName;

            if (RaisePlayerJoiningWorldEvent(this, newWorld, reason, textLine1, textLine2))
            {
                Logger.Log(LogType.Warning,
                            "Player.JoinWorldNow: Player {0} was prevented from joining world {1} by an event callback.",
                            Name, newWorld.Name);
                return false;
            }

            World oldWorld = World;

            // remove player from the old world
            if (oldWorld != null && oldWorld != newWorld)
            {
                if (!oldWorld.ReleasePlayer(this))
                {
                    Logger.Log(LogType.Error,
                                "Player.JoinWorldNow: Player asked to be released from its world, " +
                                "but the world did not contain the player.");
                }
            }

            ResetVisibleEntities();

            ClearQueue(outputQueue);

            Map map;

            // try to join the new world
            if (oldWorld != newWorld)
            {
                bool announce = (oldWorld != null) && (oldWorld.Name != newWorld.Name);
                map = newWorld.AcceptPlayer(this, announce);
                if (map == null)
                {
                    return false;
                }
            }
            else
            {
                map = newWorld.LoadMap();
            }
            World = newWorld;

            // Set spawn point
            if (doUseWorldSpawn)
            {
                Position = map.Spawn;
            }
            else
            {
                Position = postJoinPosition;
            }

            // Start sending over the level copy
            if (oldWorld != null)
            {
                SendNow(Packet.MakeHandshake(this, textLine1, textLine2));
            }

            writer.Write(OpCode.MapBegin);
            BytesSent++;

            // enable Nagle's algorithm (in case it was turned off by LowLatencyMode)
            // to avoid wasting bandwidth for map transfer
            client.NoDelay = false;

            // Fetch compressed map copy
            byte[] buffer = new byte[1024];
            int mapBytesSent = 0;
            byte[] unGzipBlockData = map.Blocks.ToArray();
            int count = 0;
            foreach (var get in unGzipBlockData) {
                if ((int) get > (int) Map.MaxLegalBlockType) {
                    if (Supports(CpeExtension.CustomBlocks) != true) {
                        unGzipBlockData[count] = (byte) Fallback.GetFallBack((Block) get);
                    }
                }
                count++;
            }
            byte[] blockData = map.GetCompressedCopy(unGzipBlockData);
            Logger.Log(LogType.Debug,
                        "Player.JoinWorldNow: Sending compressed map ({0} bytes) to {1}.",
                        blockData.Length, Name);

            // Transfer the map copy
            while (mapBytesSent < blockData.Length) {
                int chunkSize = blockData.Length - mapBytesSent;
                if (chunkSize > 1024) {
                    chunkSize = 1024;
                } else {
                    // CRC fix for ManicDigger
                    for (int i = 0; i < buffer.Length; i++) {
                        buffer[i] = 0;
                    }
                }
                Array.Copy(blockData, mapBytesSent, buffer, 0, chunkSize);
                byte progress = (byte) (100*mapBytesSent/blockData.Length);

                // write in chunks of 1024 bytes or less
                writer.Write(OpCode.MapChunk);
                writer.Write((short) chunkSize);
                writer.Write(buffer, 0, 1024);
                writer.Write(progress);
                BytesSent += 1028;
                mapBytesSent += chunkSize;
            }


            // Turn off Nagel's algorithm again for LowLatencyMode
            client.NoDelay = ConfigKey.LowLatencyMode.Enabled();

            // Done sending over level copy
            writer.Write(OpCode.MapEnd);
            writer.Write((short)map.Width);
            writer.Write((short)map.Height);
            writer.Write((short)map.Length);
            BytesSent += 7;

            if (Supports(CpeExtension.ExtPlayerList2)) {
                Send(Packet.MakeExtAddEntity2(Packet.SelfId, Info.Rank.Color + Name, (Info.skinName ?? Name), Position, this));
            } else {
                Send(Packet.MakeAddEntity(Packet.SelfId, Info.Rank.Color + Name, Position));
            }

            if (Supports(CpeExtension.ChangeModel)) {
                Send(Packet.MakeChangeModel(255, !Info.IsAFK ? Info.Mob : "Chicken"));
            }
            // Teleport player to the target location
            // This allows preserving spawn rotation/look, and allows
            // teleporting player to a specific location (e.g. /TP or /Bring)
            writer.Write(Packet.MakeTeleport(Packet.SelfId, Position).Bytes);
            BytesSent += 10;

            #region EnvSetMapAppearance

            if (Supports(CpeExtension.EnvMapAppearance)) {
                Send(Packet.MakeEnvSetMapAppearance(World.Texture, World.EdgeBlock, World.HorizonBlock,
                        (short)((World.EdgeLevel == -1) ? (WorldMap.Height / 2) : World.EdgeLevel)));
            }

            #endregion
            #region EnvColors
            if (Supports(CpeExtension.EnvColors))
            {
                Send(Packet.MakeEnvSetColor((byte)EnvVariable.SkyColor, World.SkyColor));
                Send(Packet.MakeEnvSetColor((byte)EnvVariable.CloudColor, World.CloudColor));
                Send(Packet.MakeEnvSetColor((byte)EnvVariable.FogColor, World.FogColor));
                Send(Packet.MakeEnvSetColor((byte)EnvVariable.Shadow, World.ShadowColor));
                Send(Packet.MakeEnvSetColor((byte)EnvVariable.Sunlight, World.LightColor));
            }
			if (Supports(CpeExtension.EnvWeatherType)) {
				Send(Packet.SetWeather((byte)WeatherType.Sunny));
                Send(Packet.SetWeather(World.Weather));
            }
            if (Supports(CpeExtension.EnvColors) && World != null && World.SkyLightEmulator) {
                string hex;
                if (Server.SkyColorHex.TryGetValue(Server.ColorTime, out hex)) {
                    Send(Packet.MakeEnvSetColor((byte)EnvVariable.SkyColor, hex));
                }
                if (Server.CloudAndFogColorHex.TryGetValue(Server.ColorTime, out hex)) {
                    if (World.CloudColor != null)
                        Send(Packet.MakeEnvSetColor((byte)EnvVariable.CloudColor, hex));
                    if (World.FogColor != null)
                        Send(Packet.MakeEnvSetColor((byte)EnvVariable.FogColor, hex));
                }
            }
            #endregion
            #region HackControls
            if (Supports(CpeExtension.HackControl))
            {
                Send(Packet.HackControl(Info.AllowFlying, Info.AllowNoClip, Info.AllowSpeedhack, Info.AllowRespawn, Info.AllowThirdPerson, Info.JumpHeight));
            }
            #endregion
            #region Reach Distance

            if (Supports(CpeExtension.ClickDistance)) {
                Send(Packet.MakeSetClickDistance((World.maxReach < Info.ReachDistance && !Can(Permission.ReadStaffChat) ) ? World.maxReach : Info.ReachDistance));
            }

            #endregion
            #region Block Permissions

            if (Supports(CpeExtension.BlockPermissions)) {
                if (!Can(Permission.PlaceAdmincrete)) {
                    Send(Packet.MakeSetBlockPermission(Block.Admincrete, false, false));
                }
                if (!Can(Permission.PlaceWater)) {
                    Send(Packet.MakeSetBlockPermission(Block.Water, false, true));
                    Send(Packet.MakeSetBlockPermission(Block.StillWater, false, true));
                }
                if (!Can(Permission.PlaceLava)) {
                    Send(Packet.MakeSetBlockPermission(Block.Lava, false, true));
                    Send(Packet.MakeSetBlockPermission(Block.StillLava, false, true));
                }
                if (!Can(Permission.PlaceGrass))
                {
                    Send(Packet.MakeSetBlockPermission(Block.Grass, false, true));
                }
            }

            #endregion

            foreach (Bot bot in World.Bots) {
                Send(Packet.MakeRemoveEntity(bot.ID));
                if (bot.World == World) {
                    if (Supports(CpeExtension.ExtPlayerList2)) {
                        Send(Packet.MakeExtAddEntity2(bot.ID, bot.Name, (bot.SkinName ?? bot.Name), bot.Position, this));
                    } else {
                        Send(Packet.MakeAddEntity(bot.ID, bot.Name, bot.Position));
                    }
                    if (bot.Model != "humanoid" && Supports(CpeExtension.ChangeModel)) {
                        Send(Packet.MakeChangeModel((byte) bot.ID, bot.Model));
                    }
                }
            }
            if (oldWorld == newWorld)
            {
                Message("&sRejoined world {0}", newWorld.ClassyName);
            }
            else
            {
                Message("&sJoined world {0}", newWorld.ClassyName);
                string greeting = newWorld.Greeting;
                if (greeting != null)
                {
                    greeting = Chat.ReplaceTextKeywords(this, greeting);
                    Message(greeting);
                }
                else
                {
                    FileInfo GreetingInfo = new FileInfo("./WorldGreeting/" + World.Name + ".txt");
                    if (GreetingInfo.Exists)
                    {
                        string[] Greeting = File.ReadAllLines("./WorldGreeting/" + World.Name + ".txt");
                        string GreetingMessage = "";
                        foreach (string line in Greeting)
                        {
                            GreetingMessage += line + "&n";
                        }
                        Message(GreetingMessage);
                    }
                }
            }

            RaisePlayerJoinedWorldEvent(this, oldWorld, reason);

            if (Supports(CpeExtension.SelectionCuboid)) {
                foreach (Zone z in WorldMap.Zones) {
                    if (z.ShowZone) {
                        Send(Packet.MakeMakeSelection(z.ZoneID, z.Name, z.Bounds, z.Color, z.Alpha));
                    }
                }
            }

			if (this.Supports(CpeExtension.MessageType)) {
				Send(Packet.Message((byte)MessageType.Status1, Color.White + ConfigKey.ServerName.GetString()));
			}

            Server.UpdateTabList();
            Server.RequestGC();
            return true;
        }

        #endregion


        #region Sending

        /// <summary> Send packet to player (not thread safe, sync, immediate).
        /// Should NEVER be used from any thread other than this session's ioThread.
        /// Not thread-safe (for performance reason). </summary>
        public void SendNow( Packet packet ) {
            if( Thread.CurrentThread != ioThread ) {
                throw new InvalidOperationException( "SendNow may only be called from player's own thread." );
            } 
            writer.Write( packet.Bytes );
            BytesSent += packet.Bytes.Length;
        }


        /// <summary> Send packet (thread-safe, async, priority queue).
        /// This is used for most packets (movement, chat, etc). </summary>
        public void Send(Packet packet)
        {
            if (packet.OpCode == OpCode.SetBlockServer) {
                ProcessOutgoingSetBlock( packet);
            }
            if( canQueue ) priorityOutputQueue.Enqueue( packet );
        }


        /// <summary> Send packet (thread-safe, asynchronous, delayed queue).
        /// This is currently only used for block updates. </summary>
        public void SendLowPriority(Packet packet)
        {
            if (packet.OpCode == OpCode.SetBlockServer) {
                ProcessOutgoingSetBlock( packet);
            }
            if( canQueue ) outputQueue.Enqueue( packet );
        }

        #endregion


        static void ClearQueue([NotNull] ConcurrentQueue<Packet> queue)
        {
            if (queue == null) throw new ArgumentNullException("queue");
            Packet ignored;
            while (queue.TryDequeue(out ignored)) { }
        }           


        #region Kicking

        /// <summary> Kick (asynchronous). Immediately blocks all client input, but waits
        /// until client thread has sent the kick packet. </summary>
        public void Kick( [NotNull] string message, LeaveReason leaveReason ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( !Enum.IsDefined( typeof( LeaveReason ), leaveReason ) ) {
                throw new ArgumentOutOfRangeException( "leaveReason" );
            }
            State = SessionState.PendingDisconnect;
            LeaveReason = leaveReason;

            canReceive = false;
            canQueue = false;

            // clear all pending output to be written to client (it won't matter after the kick)
            ClearQueue(outputQueue);
            ClearQueue(priorityOutputQueue);

            // bypassing Send() because canQueue is false
            priorityOutputQueue.Enqueue( Packet.MakeKick( message ) );
        }


        bool useSyncKick;
        readonly ManualResetEvent syncKickWaiter = new ManualResetEvent( false );
        readonly object kickSyncLock = new object();


        internal void KickSynchronously( [NotNull] string message, LeaveReason reason ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            lock( kickSyncLock ) {
                useSyncKick = true;
                Kick( message, reason );
            }
            syncKickWaiter.WaitOne();
            Server.UnregisterPlayer( this );
        }


        /// <summary> Kick (synchronous). Immediately sends the kick packet.
        /// Can only be used from IoThread (this is not thread-safe). </summary>
        void KickNow( [NotNull] string message, LeaveReason leaveReason ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( Thread.CurrentThread != ioThread ) {
                throw new InvalidOperationException( "KickNow may only be called from player's own thread." );
            }
            State = SessionState.PendingDisconnect;
            LeaveReason = leaveReason;

            canQueue = false;
            canReceive = false;
            canSend = false;
            SendNow( Packet.MakeKick( message ) );
            writer.Flush();
        }


        /// <summary> Blocks the calling thread until this session disconnects. </summary>
        public void WaitForDisconnect() {
            if( Thread.CurrentThread == ioThread ) {
                throw new InvalidOperationException( "Cannot call WaitForDisconnect from IoThread." );
            }
            if( ioThread != null && ioThread.IsAlive ) {
                try {
                    ioThread.Join();
                } catch( NullReferenceException ) {
                } catch( ThreadStateException ) {}
            }
        }

        #endregion


        #region Movement

        // visible entities
        public readonly Dictionary<Player, VisibleEntity> entities = new Dictionary<Player, VisibleEntity>();
        readonly Stack<Player> playersToRemove = new Stack<Player>( 127 );
        readonly Stack<sbyte> freePlayerIDs = new Stack<sbyte>( 127 );

        // movement optimization
        int fullUpdateCounter;
        public const int FullPositionUpdateIntervalDefault = 20;
        public static int FullPositionUpdateInterval = FullPositionUpdateIntervalDefault;

        const int SkipMovementThresholdSquared = 64,
                  SkipRotationThresholdSquared = 1500;

        // anti-speedhack vars
        int speedHackDetectionCounter;

        const int AntiSpeedMaxJumpDelta = 25,
                  // 16 for normal client, 25 for WoM
                  AntiSpeedMaxDistanceSquared = 1024,
                  // 32 * 32
                  AntiSpeedMaxPacketCount = 200,
                  AntiSpeedMaxPacketInterval = 5;

        // anti-speedhack vars: packet spam
        readonly Queue<DateTime> antiSpeedPacketLog = new Queue<DateTime>();
        DateTime antiSpeedLastNotification = DateTime.UtcNow;

        void ResetVisibleEntities() {
            foreach( var pos in entities.Values ) {
                SendNow( Packet.MakeRemoveEntity( pos.Id ) );
            }
            freePlayerIDs.Clear();
            for( int i = 1; i <= sbyte.MaxValue; i++ ) {
                freePlayerIDs.Push( (sbyte)i );
            }
            playersToRemove.Clear();
            entities.Clear();
        }


        void UpdateVisibleEntities() {
            if( World == null ) PlayerOpException.ThrowNoWorld( this );

            // handle following the spectatee
            if( spectatedPlayer != null ) {
                if( !spectatedPlayer.IsOnline || !CanSee( spectatedPlayer ) ) {
                    Message( "Stopped spectating {0}&S (disconnected)", spectatedPlayer.ClassyName );
                    spectatedPlayer = null;
                } else {
                    Position spectatePos = spectatedPlayer.Position;
                    World spectateWorld = spectatedPlayer.World;
                    if( spectateWorld == null ) {
                        throw new InvalidOperationException( "Trying to spectate player without a world." );
                    }
                    if( spectateWorld != World ) {
                        if( CanJoin( spectateWorld ) ) {
                            postJoinPosition = spectatePos;
                            if( JoinWorldNow( spectateWorld, false, WorldChangeReason.SpectateTargetJoined ) ) {
                                Message( "Joined {0}&S to continue spectating {1}",
                                         spectateWorld.ClassyName,
                                         spectatedPlayer.ClassyName );
                            } else {
                                Message( "Stopped spectating {0}&S (cannot join {1}&S)",
                                         spectatedPlayer.ClassyName,
                                         spectateWorld.ClassyName );
                                spectatedPlayer = null;
                            }
                        } else {
                            Message( "Stopped spectating {0}&S (cannot join {1}&S)",
                                     spectatedPlayer.ClassyName,
                                     spectateWorld.ClassyName );
                            spectatedPlayer = null;
                        }
                    } else if( spectatePos != Position ) {
                        SendNow( Packet.MakeSelfTeleport( spectatePos ) );
                    }
                    if (SpectatedPlayer.Info.heldBlock != this.Info.heldBlock)
                    {
                        SendNow(Packet.MakeHoldThis(SpectatedPlayer.Info.heldBlock, false));
                    }
                }
            }

            // check every player on the current world
            Player[] worldPlayerList = World.Players;
            Position pos = Position;
            foreach (Bot bot in World.Bots.Where(b => b.World == World)) {
                if (!bot.oldModel.ToLower().Equals(bot.Model.ToLower()) && Supports(CpeExtension.ChangeModel)) {
                    Send(Packet.MakeChangeModel((byte)bot.ID, bot.Model));
                    bot.oldModel = bot.Model;
                }
                if (bot.oldSkinName != bot.SkinName && Supports(CpeExtension.ExtPlayerList2)) {
                    Send(Packet.MakeRemoveEntity(bot.ID));
                    Send(Packet.MakeExtAddEntity2(bot.ID, bot.Name, bot.SkinName ?? bot.Name, bot.Position, this));
                    Send(Packet.MakeChangeModel((byte)bot.ID, bot.Model));
                    bot.oldSkinName = bot.SkinName;
                }
                
            }
            for( int i = 0; i < worldPlayerList.Length; i++ ) {
                Player otherPlayer = worldPlayerList[i];
                // Fetch or create a VisibleEntity object for the player
                VisibleEntity entity;
                if (!otherPlayer.CanSee(this))
                    goto skip;
                if (otherPlayer != this) {
                    if (otherPlayer.entities.TryGetValue(this, out entity)) {
                        entity.MarkedForRetention = true;
                    } else {
                        entity = otherPlayer.AddEntity(this);
                    }
                } else {
                    entity = new VisibleEntity(Position, -1, Info.Rank);
                }
                if (Info.oldskinName != Info.skinName && otherPlayer.Supports(CpeExtension.ExtPlayerList2)) {
                    otherPlayer.Send(Packet.MakeExtAddEntity2(entity.Id, Info.Rank.Color + Name, Info.skinName ?? Name, WorldMap.Spawn, otherPlayer));
                    if (otherPlayer == this) {
                        otherPlayer.Send(Packet.MakeTeleport(entity.Id, Position));
                    }

                }
                if ((Info.oldMob != Info.Mob || Info.oldafkMob != Info.afkMob) && otherPlayer.Supports(CpeExtension.ChangeModel)) {
                    otherPlayer.Send(Packet.MakeChangeModel((byte)entity.Id,
                        !Info.IsAFK ? Info.Mob : Info.afkMob));
                }
            skip:

                if (otherPlayer == this) {
                    continue;
                }
                if (!CanSee(otherPlayer))
                    continue;
                if (entities.TryGetValue(otherPlayer, out entity)) {
                    entity.MarkedForRetention = true;
                } else {
                    entity = AddEntity(otherPlayer);
                }

                Position otherPos = otherPlayer.Position;
                int distance = pos.DistanceSquaredTo( otherPos );

                // Re-add player if their rank changed (to maintain correct name colors/prefix)
                if( entity.LastKnownRank != otherPlayer.Info.Rank ) {
                    ReAddEntity( entity, otherPlayer );
                    entity.LastKnownRank = otherPlayer.Info.Rank;
                }

                if( entity.Hidden ) {
                    if( distance < entityShowingThreshold && CanSeeMoving( otherPlayer ) ) {
                        ShowEntity( entity, otherPos );
                    }

                } else {
                    if( distance > entityHidingThreshold || !CanSeeMoving( otherPlayer ) ) {
                        HideEntity( entity );

                    } else if( entity.LastKnownPosition != otherPos ) {
                        MoveEntity( entity, otherPos );
                    }
                }
            }
            Info.oldskinName = Info.skinName;
            Info.oldMob = Info.Mob;
            Info.oldafkMob = Info.afkMob;

            // Find entities to remove (not marked for retention).
            foreach( var pair in entities ) {
                if( pair.Value.MarkedForRetention ) {
                    pair.Value.MarkedForRetention = false;
                } else {
                    playersToRemove.Push( pair.Key );
                }
            }

            // Remove non-retained entities
            while( playersToRemove.Count > 0 ) {
                RemoveEntity( playersToRemove.Pop() );
            }

            fullUpdateCounter++;
            if( fullUpdateCounter >= FullPositionUpdateInterval ) {
                fullUpdateCounter = 0;
            }
        }


        VisibleEntity AddEntity( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if (freePlayerIDs.Count > 0) {
                var newEntity = new VisibleEntity(VisibleEntity.HiddenPosition, freePlayerIDs.Pop(), player.Info.Rank);
                entities.Add(player, newEntity);
#if DEBUG_MOVEMENT
                Logger.Log( LogType.Debug, "AddEntity: {0} added {1} ({2})", Name, newEntity.Id, player.Name );
#endif
                if (Supports(CpeExtension.ExtPlayerList2)) {
                    Send(Packet.MakeExtAddEntity2(newEntity.Id, player.Info.Rank.Color + player.Name,
                        (player.Info.skinName ?? player.Name), player.WorldMap.Spawn, this));
                    Send(Packet.MakeTeleport(newEntity.Id, player.Position));
                } else {
                    Send(Packet.MakeAddEntity(newEntity.Id, player.Info.Rank.Color + player.Name,
                        player.WorldMap.Spawn));
                    Send(Packet.MakeTeleport(newEntity.Id, player.Position));
                }
                if (Supports(CpeExtension.ChangeModel)) {
                    Send(Packet.MakeChangeModel((byte)newEntity.Id, !player.Info.IsAFK ? player.Info.Mob : "Chicken"));
                }
                return newEntity;
            } else {
                throw new InvalidOperationException("Player.AddEntity: Ran out of entity IDs.");
            }
        }        

        void HideEntity( [NotNull] VisibleEntity entity ) {
            if( entity == null ) throw new ArgumentNullException( "entity" );
#if DEBUG_MOVEMENT
            Logger.Log( LogType.Debug, "HideEntity: {0} no longer sees {1}", Name, entity.Id );
#endif
            entity.Hidden = true;
            entity.LastKnownPosition = VisibleEntity.HiddenPosition;
            SendNow( Packet.MakeTeleport( entity.Id, VisibleEntity.HiddenPosition ) );
        }


        void ShowEntity( [NotNull] VisibleEntity entity, Position newPos ) {
            if( entity == null ) throw new ArgumentNullException( "entity" );
#if DEBUG_MOVEMENT
            Logger.Log( LogType.Debug, "ShowEntity: {0} now sees {1}", Name, entity.Id );
#endif
            entity.Hidden = false;
            entity.LastKnownPosition = newPos;
            SendNow( Packet.MakeTeleport( entity.Id, newPos ) );
        }


        void ReAddEntity( [NotNull] VisibleEntity entity, [NotNull] Player player ) {
            if( entity == null ) throw new ArgumentNullException( "entity" );
            if( player == null ) throw new ArgumentNullException( "player" );
#if DEBUG_MOVEMENT
            Logger.Log( LogType.Debug, "ReAddEntity: {0} re-added {1} ({2})", Name, entity.Id, player.Name );
#endif
            SendNow( Packet.MakeRemoveEntity( entity.Id ) );
            if (Supports(CpeExtension.ExtPlayerList2)) {
                SendNow(Packet.MakeExtAddEntity2(entity.Id, player.Info.Rank.Color + player.Name, (player.Info.skinName ?? player.Name),
                    player.WorldMap.Spawn, this));
                Send(Packet.MakeTeleport(entity.Id, player.Position));
            } else {
                SendNow(Packet.MakeAddEntity(entity.Id, player.Info.Rank.Color + player.Name, player.WorldMap.Spawn));
                Send(Packet.MakeTeleport(entity.Id, player.Position));
            }

            if (Supports(CpeExtension.ChangeModel)) {
                SendNow(Packet.MakeChangeModel((byte)entity.Id, !player.Info.IsAFK ? player.Info.Mob : "Chicken"));
            }
        }


        void RemoveEntity( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
#if DEBUG_MOVEMENT
            Logger.Log( LogType.Debug, "RemoveEntity: {0} removed {1} ({2})", Name, entities[player].Id, player.Name );
#endif
            SendNow( Packet.MakeRemoveEntity( entities[player].Id ) );
            freePlayerIDs.Push( entities[player].Id );
            entities.Remove( player );
        }


        void MoveEntity( [NotNull] VisibleEntity entity, Position newPos ) {
            if( entity == null ) throw new ArgumentNullException( "entity" );
            Position oldPos = entity.LastKnownPosition;

            // calculate difference between old and new positions
            Position delta = new Position {
                X = (short)( newPos.X - oldPos.X ),
                Y = (short)( newPos.Y - oldPos.Y ),
                Z = (short)( newPos.Z - oldPos.Z ),
                R = (byte)Math.Abs( newPos.R - oldPos.R ),
                L = (byte)Math.Abs( newPos.L - oldPos.L )
            };

            bool posChanged = ( delta.X != 0 ) || ( delta.Y != 0 ) || ( delta.Z != 0 );
            bool rotChanged = ( delta.R != 0 ) || ( delta.L != 0 );

            if( skipUpdates ) {
                int distSquared = delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z;
                // movement optimization
                if( distSquared < SkipMovementThresholdSquared &&
                    ( delta.R * delta.R + delta.L * delta.L ) < SkipRotationThresholdSquared &&
                    !entity.SkippedLastMove ) {

                    entity.SkippedLastMove = true;
                    return;
                }
                entity.SkippedLastMove = false;
            }

            Packet packet;
            // create the movement packet
            if( partialUpdates && delta.FitsIntoMoveRotatePacket && fullUpdateCounter < FullPositionUpdateInterval ) {
                if( posChanged && rotChanged ) {
                    // incremental position + absolute rotation update
                    packet = Packet.MakeMoveRotate( entity.Id, new Position {
                        X = delta.X,
                        Y = delta.Y,
                        Z = delta.Z,
                        R = newPos.R,
                        L = newPos.L
                    } );

                } else if( posChanged ) {
                    // incremental position update
                    packet = Packet.MakeMove( entity.Id, delta );

                } else if( rotChanged ) {
                    // absolute rotation update
                    packet = Packet.MakeRotate( entity.Id, newPos );
                } else {
                    return;
                }

            } else {
                // full (absolute position + absolute rotation) update
                packet = Packet.MakeTeleport( entity.Id, newPos );
            }

            entity.LastKnownPosition = newPos;
            SendNow( packet );
        }

        public sealed class VisibleEntity {
            public static readonly Position HiddenPosition = new Position( 0, 0, short.MinValue );


            public VisibleEntity( Position newPos, sbyte newId, Rank newRank ) {
                Id = newId;
                LastKnownPosition = newPos;
                MarkedForRetention = true;
                Hidden = true;
                LastKnownRank = newRank;
            }


            public readonly sbyte Id;
            public Position LastKnownPosition;
            public Rank LastKnownRank;
            public bool Hidden;
            public bool MarkedForRetention;
            public bool SkippedLastMove;
        }


        Position lastValidPosition; // used in speedhack detection


        bool DetectMovementPacketSpam() {
            if( antiSpeedPacketLog.Count >= AntiSpeedMaxPacketCount ) {
                DateTime oldestTime = antiSpeedPacketLog.Dequeue();
                double spamTimer = DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds;
                if( spamTimer < AntiSpeedMaxPacketInterval ) {
                    DenyMovement();
                    return true;
                }
            }
            antiSpeedPacketLog.Enqueue( DateTime.UtcNow );
            return false;
        }


        void DenyMovement() {
            SendNow( Packet.MakeSelfTeleport(new Position
                {
                    X = (short)(lastValidPosition.X),
                    Y = (short)(lastValidPosition.Y),
                    Z = (short)(lastValidPosition.Z + 22),
                    R = lastValidPosition.R,
                    L = lastValidPosition.L
                }));
            if( DateTime.UtcNow.Subtract( antiSpeedLastNotification ).Seconds > 1 ) {
                Message( "&WYou are not allowed to speedhack." );
                antiSpeedLastNotification = DateTime.UtcNow;
            }
        }

        #endregion


        #region Bandwidth Use Tweaks

        BandwidthUseMode bandwidthUseMode;
        int entityShowingThreshold, entityHidingThreshold;
        bool partialUpdates, skipUpdates;

        DateTime lastMovementUpdate;
        TimeSpan movementUpdateInterval;


        public BandwidthUseMode BandwidthUseMode {
            get { return bandwidthUseMode; }

            set {
                bandwidthUseMode = value;
                BandwidthUseMode actualValue = value;
                if( value == BandwidthUseMode.Default ) {
                    actualValue = ConfigKey.BandwidthUseMode.GetEnum<BandwidthUseMode>();
                }
                switch( actualValue ) {
                    case BandwidthUseMode.VeryLow:
                        entityShowingThreshold = ( 40 * 32 ) * ( 40 * 32 );
                        entityHidingThreshold = ( 42 * 32 ) * ( 42 * 32 );
                        partialUpdates = true;
                        skipUpdates = true;
                        movementUpdateInterval = TimeSpan.FromMilliseconds( 100 );
                        break;

                    case BandwidthUseMode.Low:
                        entityShowingThreshold = ( 50 * 32 ) * ( 50 * 32 );
                        entityHidingThreshold = ( 52 * 32 ) * ( 52 * 32 );
                        partialUpdates = true;
                        skipUpdates = true;
                        movementUpdateInterval = TimeSpan.FromMilliseconds( 50 );
                        break;

                    case BandwidthUseMode.Normal:
                        entityShowingThreshold = ( 68 * 32 ) * ( 68 * 32 );
                        entityHidingThreshold = ( 70 * 32 ) * ( 70 * 32 );
                        partialUpdates = true;
                        skipUpdates = false;
                        movementUpdateInterval = TimeSpan.FromMilliseconds( 50 );
                        break;

                    case BandwidthUseMode.High:
                        entityShowingThreshold = ( 128 * 32 ) * ( 128 * 32 );
                        entityHidingThreshold = ( 130 * 32 ) * ( 130 * 32 );
                        partialUpdates = true;
                        skipUpdates = false;
                        movementUpdateInterval = TimeSpan.FromMilliseconds( 50 );
                        break;

                    case BandwidthUseMode.VeryHigh:
                        entityShowingThreshold = int.MaxValue;
                        entityHidingThreshold = int.MaxValue;
                        partialUpdates = false;
                        skipUpdates = false;
                        movementUpdateInterval = TimeSpan.FromMilliseconds( 25 );
                        break;
                }
            }
        }

        #endregion


        #region Bandwidth Use Metering

        DateTime lastMeasurementDate = DateTime.UtcNow;
        int lastBytesSent, lastBytesReceived;


        /// <summary> Total bytes sent (to the client) this session. </summary>
        public int BytesSent { get; private set; }

        /// <summary> Total bytes received (from the client) this session. </summary>
        public int BytesReceived { get; private set; }

        /// <summary> Bytes sent (to the client) per second, averaged over the last several seconds. </summary>
        public double BytesSentRate { get; private set; }

        /// <summary> Bytes received (from the client) per second, averaged over the last several seconds. </summary>
        public double BytesReceivedRate { get; private set; }


        void MeasureBandwidthUseRates() {
            int sentDelta = BytesSent - lastBytesSent;
            int receivedDelta = BytesReceived - lastBytesReceived;
            TimeSpan timeDelta = DateTime.UtcNow.Subtract( lastMeasurementDate );
            BytesSentRate = sentDelta / timeDelta.TotalSeconds;
            BytesReceivedRate = receivedDelta / timeDelta.TotalSeconds;
            lastBytesSent = BytesSent;
            lastBytesReceived = BytesReceived;
            lastMeasurementDate = DateTime.UtcNow;
        }
        #endregion                      
    }
}