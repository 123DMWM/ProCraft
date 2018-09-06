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
using System.Collections;

namespace fCraft.Portals {
    internal static class PortalHandler {
        
        public static void Init() {
            Player.Moved += Player_Moved;
            Player.JoinedWorld += Player_JoinedWorld;
            Player.PlacedBlock += Player_PlacedBlock;
            PortalDB.StartSaveTask();
        }

        static void Player_PlacedBlock(object sender, Events.PlayerPlacedBlockEventArgs e) {
            if (e.Player == Player.Console || e.Context == BlockChangeContext.Portal) return;
            try {
                Portal portal = e.Player.World.Portals.Find(e.Coords);
                if (portal == null) return;
                
               BlockUpdate update = new BlockUpdate(null, e.Coords, e.OldBlock);
               e.Player.World.Map.QueueUpdate(update);
               e.Player.Message("You can not place a block inside portal: " + portal.Name);
            } catch (Exception ex) {
                Logger.Log(LogType.Error, "PortalHandler.Player_PlacedBlock: " + ex);
            }
        }

        static void Player_JoinedWorld(object sender, Events.PlayerJoinedWorldEventArgs e) {
            // Player can use portals again
            e.Player.CanUsePortal = true;
            e.Player.LastUsedPortal = DateTime.UtcNow;
        }

        static void Player_Moved(object sender, Events.PlayerMovedEventArgs e) {
            //abuse portal moved event and add in message blocks right here
            //Vector3I oldPos = e.OldPosition.ToBlockCoordsRaw(); //get positions as block coords
            //Vector3I newPos = e.NewPosition.ToBlockCoordsRaw();

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

            if (!e.Player.PortalsEnabled) return;            
            lock (e.Player.PortalLock) {
            	if (!e.Player.CanUsePortal) return;
                if (e.OldPosition.X == e.NewPosition.X && e.OldPosition.Y == e.NewPosition.Y && e.OldPosition.Z == e.NewPosition.Z) return;
                if (!e.Player.Can(Permission.Chat)) return;
                
                try {                    
            		Portal portal = e.Player.World.Portals.Find(e.Player);
                    if (portal == null || e.Player.StandingInPortal) {
            			e.Player.StandingInPortal = false;
            			return;
            		}
            		
                    if ((DateTime.UtcNow - e.Player.LastUsedPortal).TotalSeconds < 5) {
                        // To prevent portal loops
                        if ((DateTime.UtcNow - e.Player.LastWarnedPortal).TotalSeconds > 2) {
                            e.Player.LastWarnedPortal = DateTime.UtcNow;
                            e.Player.Message("You can not use portals within 5 seconds of joining a world.");
                        }
                        return;
                    }

                    e.Player.StandingInPortal = true;
                    World world = WorldManager.FindWorldExact(portal.World);
                    
                    if (world == e.Player.World) {
                    	e.Player.TeleportTo(portal.tpPosition());
                        e.Player.Message("You used portal: " + portal.Name);
                        return;
                    }
                    
                    // Teleport player, portal protection
                    switch (world.AccessSecurity.CheckDetailed(e.Player.Info)) {
                        case SecurityCheckResult.Allowed:
                        case SecurityCheckResult.WhiteListed:
                            if (world.IsFull) {
                                e.Player.Message("Cannot join {0}&S: world is full.", world.ClassyName);
                                return;
                            }
                            e.Player.StopSpectating();
                            
                            if (portal.TeleportPosX != 0 && portal.TeleportPosY != 0 && portal.TeleportPosZ != 0) {
                                e.Player.JoinWorld(world, WorldChangeReason.Portal, portal.tpPosition());
                            } else {
                                e.Player.JoinWorld(world, WorldChangeReason.Portal);
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
                } catch (Exception ex) {
                    Logger.Log(LogType.Error, "PortalHandler.Player_Moved: " + ex);
                }
            }
        }

        public static bool NearSpawn(Player player, World world, Vector3I block) {
            if (player.Info.Rank == RankManager.HighestRank)
                return false;
            
            int dx = world.Map.Spawn.BlockX - block.X;
            int dy = world.Map.Spawn.BlockY - block.Y;
            int dz = world.Map.Spawn.BlockZ - block.Z;
            return Math.Abs(dx) <= 10 && Math.Abs(dy) <= 10 && Math.Abs(dz) <= 10;
        }
    }
}