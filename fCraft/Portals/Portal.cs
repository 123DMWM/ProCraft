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
        public Vector3I[] AffectedBlocks { get; set; }
        public PortalRange Range { get; set; }
        public string Place { get; set; }
        public short TeleportPosX { get; set; }
        public short TeleportPosY { get; set; }
        public short TeleportPosZ { get; set; }
        public byte TeleportPosR { get; set; }
        public byte TeleportPosL { get; set; }

        public Portal(string world, Vector3I[] affectedBlocks, string name, string creator, string place, Position teleportPos) {
            World = world;
            AffectedBlocks = affectedBlocks;
            Range = CalculateRange(this);
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

        public Portal(string world, Vector3I[] affectedBlocks, string name, string creator, DateTime created, string place, Position teleportPos) {
            World = world;
            AffectedBlocks = affectedBlocks;
            Range = CalculateRange(this);
            Name = Name;
            Creator = Creator;
            Created = Created;
            Place = Place;
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
            return new Position(TeleportPosX, TeleportPosY, (short)(TeleportPosZ + 64), TeleportPosR, TeleportPosL);
        }
        public static PortalRange CalculateRange(Portal portal) {
            PortalRange range = new PortalRange(0, 0, 0, 0, 0, 0);

            foreach (Vector3I block in portal.AffectedBlocks) {
                if (range.Xmin == 0) {
                    range.Xmin = block.X;
                } else {
                    if (block.X < range.Xmin) {
                        range.Xmin = block.X;
                    }
                }

                if (range.Xmax == 0) {
                    range.Xmax = block.X;
                } else {
                    if (block.X > range.Xmax) {
                        range.Xmax = block.X;
                    }
                }

                if (range.Ymin == 0) {
                    range.Ymin = block.Y;
                } else {
                    if (block.Y < range.Ymin) {
                        range.Ymin = block.Y;
                    }
                }

                if (range.Ymax == 0) {
                    range.Ymax = block.Y;
                } else {
                    if (block.Y > range.Ymax) {
                        range.Ymax = block.Y;
                    }
                }

                if (range.Zmin == 0) {
                    range.Zmin = block.Z;
                } else {
                    if (block.Z < range.Zmin) {
                        range.Zmin = block.Z;
                    }
                }

                if (range.Zmax == 0) {
                    range.Zmax = block.Z;
                } else {
                    if (block.Z > range.Zmax) {
                        range.Zmax = block.Z;
                    }
                }
            }

            return range;
        }

        public bool IsInRange(Player player) {
            if ((player.Position.X / 32) <= Range.Xmax && (player.Position.X / 32) >= Range.Xmin) {
                if ((player.Position.Y / 32) <= Range.Ymax && (player.Position.Y / 32) >= Range.Ymin) {
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

        public static String GenerateName(World world) {
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

        public static bool DoesNameExist(World world, String name) {
            if (world.Portals != null) {
                if (world.Portals.Count > 0) {
                    foreach (Portal portal in world.Portals) {
                        if (portal.Name.Equals(name)) {
                            return true;
                        }
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

            if (this.AffectedBlocks == null) {
                this.AffectedBlocks = new Vector3I[2];
                this.AffectedBlocks[0] = new Vector3I(Range.Xmin, Range.Ymin, Range.Zmin);
                this.AffectedBlocks[1] = new Vector3I(Range.Xmax, Range.Ymax, Range.Zmax);
            }

            if (!removeOperation.Prepare(this.AffectedBlocks)) {
                throw new PortalException("Unable to remove portal.");
            }

            removeOperation.Begin();

            lock (pWorld.Portals.SyncRoot) {
                pWorld.Portals.Remove(this);
            }

            PortalDB.Save();
        }
    }
}