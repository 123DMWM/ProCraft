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

        public bool Contains(Player p) {
            return
                p.Position.BlockX >= Range.Xmin && p.Position.BlockX <= Range.Xmax &&
                p.Position.BlockY >= Range.Ymin && p.Position.BlockY <= Range.Ymax && 
                ((p.Position.Z / 32) - 1) >= Range.Zmin && ((p.Position.Z / 32) - 1) <= Range.Zmax;
        }

        public bool Contains(Vector3I vector) {
            return 
                vector.X >= Range.Xmin && vector.X <= Range.Xmax &&
                vector.Y >= Range.Ymin && vector.Y <= Range.Ymax &&
                vector.Z >= Range.Zmin && vector.Z <= Range.Zmax;
        }

        public void Remove(Player requester, World world) {
            DrawOperation op = new CuboidDrawOperation(requester);
            op.AnnounceCompletion = false;
            op.Brush   = new NormalBrush(Block.Air, Block.Air);
            op.Context = BlockChangeContext.Portal;

            Vector3I[] bounds = {
                new Vector3I(Range.Xmin, Range.Ymin, Range.Zmin),
                new Vector3I(Range.Xmax, Range.Ymax, Range.Zmax),
            };
            if (!op.Prepare(bounds))
                throw new InvalidOperationException("Unable to remove portal.");

            op.Begin();
            world.Portals.Remove(this);
            PortalDB.Save();
        }
    }
    
    public class PortalRange {
        public int Xmin { get; set; }
        public int Xmax { get; set; }
        public int Ymin { get; set; }
        public int Ymax { get; set; }
        public int Zmin { get; set; }
        public int Zmax { get; set; }

        public PortalRange(int Xmin, int Xmax, int Ymin, int Ymax, int Zmin, int Zmax) {
            this.Xmin = Xmin;
            this.Xmax = Xmax;
            this.Ymin = Ymin;
            this.Ymax = Ymax;
            this.Zmin = Zmin;
            this.Zmax = Zmax;
        }
    }
    
    public class PortalsList {
        public readonly object locker = new object();
        public volatile int Count;
        public List<Portal> entries;
        
        public void Add(Portal portal) {
            lock (locker) {
                if (entries == null) entries = new List<Portal>();
                entries.Add(portal);
                Count = entries.Count;
            }
        }
        
        public void Remove(Portal portal) {
            lock (locker) {
                if (entries == null) return;
                entries.Remove(portal);
                Count = entries.Count;
            }
        }
        
        public string GenAutoName() {
            int id = 1;
            while (Find("portal" + id) != null) id++;
            return "portal" + id;
        }
        
        public Portal Find(string name) {
            if (Count == 0) return null;
            
            lock (locker) {
                foreach (Portal p in entries) {
                    if (p.Name.CaselessEquals(name)) return p;
                }
            }
            return null;
        }
        
        public Portal Find(Vector3I coords) {
            if (Count == 0) return null;
            
            lock (locker) {
                foreach (Portal p in entries) {
                    if (p.Contains(coords)) return p;
                }
            }
            return null;
        }
        
        public Portal Find(Player player) {
            if (Count == 0) return null;
            
            lock (locker) {
                foreach (Portal p in entries) {
                    if (p.Contains(player)) return p;
                }
            }
            return null;
        }
    }
}