//Copyright (C) <2012>  <Jon Baker, Glenn Mariën and Lao Tszy>

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>.

//Copyright (C) <2012> Glenn Mariën (http://project-vanilla.com)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Text;
using System.Collections;

namespace fCraft.Portals {
    class PortalHandler {
        private static PortalHandler instance;

        private PortalHandler() {
            // Empty, singleton
        }

        public static PortalHandler GetInstance() {
            if (instance == null) {
                instance = new PortalHandler();
                Player.Moved += new EventHandler<Events.PlayerMovedEventArgs>(Player_Moved);
                Player.JoinedWorld += new EventHandler<Events.PlayerJoinedWorldEventArgs>(Player_JoinedWorld);
                Player.PlacedBlock += new EventHandler<Events.PlayerPlacedBlockEventArgs>(Player_PlacedBlock);
                PortalDB.StartSaveTask();
            }

            return instance;
        }

        static void Player_PlacedBlock(object sender, Events.PlayerPlacedBlockEventArgs e) {
            if (e.Player == Player.Console) return;
            try {
                if (e.Player.World.Portals != null && e.Player.World.Portals.Count > 0 && e.Context != BlockChangeContext.Portal) {
                    lock (e.Player.World.Portals.SyncRoot) {
                        foreach (Portal portal in e.Player.World.Portals) {
                            if (portal.IsInRange(e.Coords)) {
                                BlockUpdate update = new BlockUpdate(null, e.Coords, e.OldBlock);
                                e.Player.World.Map.QueueUpdate(update);
                                e.Player.Message("You can not place a block inside portal: " + portal.Name);
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Log(LogType.Error, "PortalHandler.Player_PlacedBlock: " + ex);
            }
        }

        static void Player_JoinedWorld(object sender, Events.PlayerJoinedWorldEventArgs e) {
            try {
                // Player can use portals again
                e.Player.CanUsePortal = true;
                e.Player.LastUsedPortal = DateTime.UtcNow;
            } catch (Exception ex) {
                Logger.Log(LogType.Error, "PortalHandler.Player_JoinedWorld: " + ex);
            }
        }

        static void Player_Moved(object sender, Events.PlayerMovedEventArgs e) {
            //abuse portal moved event and add in message blocks right here
            Vector3I oldPos = e.OldPosition.ToBlockCoordsRaw(); //get positions as block coords
            Vector3I newPos = e.NewPosition.ToBlockCoordsRaw();

            /*if (oldPos.X != newPos.X || oldPos.Y != newPos.Y || oldPos.Z != newPos.Z) //check if player has moved at least one block
            {
                //loop through all message blocks and check if we triggered one
                foreach (var messageBlock in e.Player.World.MessageBlocks)
                {
                    Tuple<Vector3I, string> tuple = messageBlock.Value;

                    //player is sitting on the message block, play the message
                    if (e.Player.Position.ToBlockCoords() == tuple.Item1)
                    {
                        e.Player.Message("__" + messageBlock.Key + "__");
                        e.Player.Message(messageBlock.Value.Item2);
                        e.Player.Message(e.Player.Position.ToBlockCoords().ToString());
                        e.Player.Message(tuple.Item1.ToString());
                    }
                }
            }*/

            try {
                if (e.Player.PortalsEnabled) {
                    lock (e.Player.PortalLock) {
                        if (e.Player.CanUsePortal) {
                            if ((e.OldPosition.X != e.NewPosition.X) || (e.OldPosition.Y != e.NewPosition.Y) || (e.OldPosition.Z != (e.NewPosition.Z))) {
                                if (e.Player.Can(Permission.Chat)) {
                                    if (PortalHandler.GetInstance().GetPortal(e.Player) != null && !e.Player.StandingInPortal) {
                                        if ((DateTime.UtcNow - e.Player.LastUsedPortal).TotalSeconds < 5) {
                                            // To prevent portal loops
                                            if ((DateTime.UtcNow - e.Player.LastWarnedPortal).TotalSeconds > 2) {
                                                e.Player.LastWarnedPortal = DateTime.UtcNow;
                                                e.Player.Message("You can not use portals within 5 seconds of joining a world.");
                                            }

                                            return;
                                        }

                                        e.Player.StandingInPortal = true;
                                        Portal portal = PortalHandler.GetInstance().GetPortal(e.Player);

                                        World world = WorldManager.FindWorldExact(portal.World);
                                        // Teleport player, portal protection
                                        if (world != e.Player.World) {
                                            switch (world.AccessSecurity.CheckDetailed(e.Player.Info)) {
                                                case SecurityCheckResult.Allowed:
                                                case SecurityCheckResult.WhiteListed:
                                                    if (world.IsFull) {
                                                        e.Player.Message("Cannot join {0}&S: world is full.", world.ClassyName);
                                                        return;
                                                    }
                                                    e.Player.StopSpectating();
                                                    if (portal.TeleportPosX != 0 && portal.TeleportPosY != 0 && portal.TeleportPosZ != 0) {
                                                        e.Player.JoinWorld(WorldManager.FindWorldExact(portal.World), WorldChangeReason.Portal, 
                                                            portal.tpPosition() == Position.RandomSpawn 
                                                            ? WorldManager.FindWorldExact(portal.World).map.getSpawnIfRandom() 
                                                            : portal.tpPosition());
                                                    } else {
                                                        e.Player.JoinWorld(WorldManager.FindWorldExact(portal.World), WorldChangeReason.Portal);
                                                    }
                                                    e.Player.Message("You used portal: " + portal.Name);
                                                    if (e.Player.WorldMap.Spawn == Position.RandomSpawn) {
                                                        e.Player.Message("Randomized Spawn!");
                                                    }

                                                    // Make sure this method isn't called twice
                                                    e.Player.CanUsePortal = false;
                                                    break;

                                                case SecurityCheckResult.BlackListed:
                                                    if ((DateTime.UtcNow - e.Player.LastWarnedPortal).TotalSeconds < 2) {
                                                        e.Player.LastWarnedPortal = DateTime.UtcNow;
                                                        e.Player.Message("Cannot join world {0}&S: you are blacklisted.",
                                                            world.ClassyName);
                                                    }
                                                    break;
                                                case SecurityCheckResult.RankTooLow:
                                                    if ((DateTime.UtcNow - e.Player.LastWarnedPortal).TotalSeconds > 2) {
                                                        e.Player.LastWarnedPortal = DateTime.UtcNow;
                                                        e.Player.Message("Cannot join world {0}&S: must be {1}+",
                                                                     world.ClassyName, world.AccessSecurity.MinRank.ClassyName);
                                                    }
                                                    break;
                                            }
                                        } else {
                                            e.Player.TeleportTo(portal.tpPosition());
                                            e.Player.Message("You used portal: " + portal.Name);
                                        }
                                    } else {
                                        e.Player.StandingInPortal = false;
                                    }
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Log(LogType.Error, "PortalHandler.Player_Moved: " + ex);
            }
        }

        public Portal GetPortal(Player player) {
            Portal portal = null;

            try {
                if (player.World.Portals != null && player.World.Portals.Count > 0) {
                    lock (player.World.Portals.SyncRoot) {
                        foreach (Portal possiblePortal in player.World.Portals) {
                            if (possiblePortal.IsInRange(player)) {
                                return possiblePortal;
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Log(LogType.Error, "PortalHandler.GetPortal: " + ex);
            }

            return portal;
        }

        public Portal GetPortal(World world, Vector3I block) {
            Portal portal = null;

            try {
                if (world.Portals != null && world.Portals.Count > 0) {
                    lock (world.Portals.SyncRoot) {
                        foreach (Portal possiblePortal in world.Portals) {
                            if (possiblePortal.IsInRange(block)) {
                                return possiblePortal;
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Log(LogType.Error, "PortalHandler.GetPortal: " + ex);
            }

            return portal;
        }

        public static void CreatePortal(Portal portal, World source) {
            World world = WorldManager.FindWorldExact(portal.World);

            if (source.Portals == null) {
                source.Portals = new ArrayList();
            }

            lock (source.Portals.SyncRoot) {
                source.Portals.Add(portal);
            }

            PortalDB.Save();
        }

        public static bool IsInRangeOfSpawnpoint(Player player, World world, Vector3I block) {
            if (player.Info.Rank == RankManager.HighestRank)
                return false;
            
            int dx = world.Map.Spawn.BlockX - block.X;
            int dy = world.Map.Spawn.BlockY - block.Y;
            int dz = world.Map.Spawn.BlockZ - block.Z;
            return Math.Abs(dx) <= 10 && Math.Abs(dy) <= 10 && Math.Abs(dz) <= 10;
        }
    }
}