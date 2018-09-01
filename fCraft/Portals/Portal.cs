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
using fCraft.Drawing;

namespace fCraft.Portals {
    public class Portal {
        public string Name { get; set; }
        public string Creator { get; set; }
        public DateTime Created { get; set; }
        public string World { get; set; }
        public PortalRange Range { get; set; }
        public string Place { get; set; }
        public int TeleportPosX { get; set; }
        public int TeleportPosY { get; set; }
        public int TeleportPosZ { get; set; }
        public byte TeleportPosR { get; set; }
        public byte TeleportPosL { get; set; }

        public Portal(string world, PortalRange range,  string name, 
                      string creator, string place, Position teleportPos) {
            World = world;
            Range = range;
            Name = name;
            Creator = creator;
            Created = DateTime.Now;
            Place = place;
            TeleportPosX = teleportPos.X;
            TeleportPosY = teleportPos.Y;
            TeleportPosZ = teleportPos.Z;
            TeleportPosR = teleportPos.R;
            TeleportPosL = teleportPos.L;
        }
        
        public Position position() {
            return new Position(TeleportPosX, TeleportPosY, TeleportPosZ, TeleportPosR, TeleportPosL);
        }
        public Position tpPosition() {
            return new Position(TeleportPosX, TeleportPosY, TeleportPosZ + Player.CharacterHeight, TeleportPosR, TeleportPosL);
        }

        public bool IsInRange(Player player) {
            if ((player.Position.BlockX) <= Range.Xmax && (player.Position.BlockX) >= Range.Xmin) {
                if ((player.Position.BlockY) <= Range.Ymax && (player.Position.BlockY) >= Range.Ymin) {
                    if (((player.Position.Z / 32) - 1) <= Range.Zmax && ((player.Position.Z / 32) - 1) >= Range.Zmin) {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsInRange(Vector3I vector) {
            if (vector.X <= Range.Xmax && vector.X >= Range.Xmin) {
                if (vector.Y <= Range.Ymax && vector.Y >= Range.Ymin) {
                    if (vector.Z <= Range.Zmax && vector.Z >= Range.Zmin) {
                        return true;
                    }
                }
            }

            return false;
        }

        public static string GenerateName(World world) {
            if (world.Portals != null) {
                if (world.Portals.Count > 0) {
                    bool found = false;


                    while (!found) {
                        bool taken = false;

                        foreach (Portal portal in world.Portals) {
                            if (portal.Name.Equals("portal" + world.portalID)) {
                                taken = true;
                                break;
                            }
                        }

                        if (!taken) {
                            found = true;
                        } else {
                            world.portalID++;
                        }
                    }

                    return "portal" + world.portalID;
                }
            }

            return "portal1";
        }

        public static bool Exists(World world, string name) {
            if (world.Portals != null) {
                if (world.Portals.Count > 0) {
                    foreach (Portal portal in world.Portals) {
                        if (portal.Name.Equals(name)) return true;
                    }
                }
            }

            return false;
        }

        public void Remove(Player requester, World pWorld) {
            NormalBrush brush = new NormalBrush(Block.Air, Block.Air);
            DrawOperation removeOperation = new CuboidDrawOperation(requester);
            removeOperation.AnnounceCompletion = false;
            removeOperation.Brush = brush;
            removeOperation.Context = BlockChangeContext.Portal;

            Vector3I[] affectedBlocks = {
                new Vector3I(Range.Xmin, Range.Ymin, Range.Zmin),
                new Vector3I(Range.Xmax, Range.Ymax, Range.Zmax),
            };
            if (!removeOperation.Prepare(affectedBlocks))
                throw new PortalException("Unable to remove portal.");

            removeOperation.Begin();
            lock (pWorld.Portals.SyncRoot)
                pWorld.Portals.Remove(this);
            PortalDB.Save();
        }
    }
}