// ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.IO;
using ServiceStack.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace fCraft {

    public sealed class Entity {

        // Entity
        public string Name { get; set; }
        public string Skin { get; set; }
        public string World { get; set; }
        public short X { get; set; }
        public short Y { get; set; }
        public short Z { get; set; }
        public byte R { get; set; }
        public byte L { get; set; }
        public sbyte ID { get; set; }
        public string Model { get; set; }



        public static List<Entity> Entities = new List<Entity>();

        public static World getWorld(Entity entity) {
            return WorldManager.FindWorldExact(entity.World);
        }

        public static Position getPos(Entity entity) {
            return new Position(entity.X, entity.Y, entity.Z, entity.R, entity.L);
        }
        public static void setPos(Entity entity, Position pos) {
            entity.X = pos.X;
            entity.Y = pos.Y; 
            entity.Z = pos.Z; 
            entity.R = pos.R; 
            entity.L = pos.L;
        }

        public static Entity Find(World world, string name) {
            foreach (Entity entity in Entities.Where(w => w.World == world.Name)) {
                if (entity.Name.ToLower().Equals(name.ToLower())) {
                    return entity;
                }
            }
            return null;
        }

        public static bool exists(World world, string name) {
            foreach(Entity e in Entities.Where(w => w.World == world.Name)) {
                if (e.Name.ToLower() == name.ToLower()) {
                    return true;
                }
            }
            return false;
        }

        public static bool existsAny(World world) {
            foreach (Entity e in Entities) {
                if (e.World.ToLower() == world.Name.ToLower()) {
                    return true;
                }
            }
            return false;
        }

        public static Entity CreateEntity(string name, string skin, string modelName, World world, Position pos, sbyte entityID) {
            Entity entity = new Entity();
            entity.Name = name;
            entity.Skin = skin;
            entity.Model = modelName;
            entity.World = world.Name;
            setPos(entity, pos);
            entity.ID = entityID;
            Entities.Add(entity);
            ShowEntity(entity);
            SaveAll(false);
            return entity;
        }

        public static void ShowEntity(Entity entity) {
            foreach (Player sendTo in getWorld(entity).Players) {
                if (sendTo.Supports(CpeExt.ExtPlayerList2)) {
                    sendTo.Send(Packet.MakeExtAddEntity2(entity.ID, entity.Name, entity.Skin, getPos(entity), sendTo));
                } else {
                    sendTo.Send(Packet.MakeAddEntity(entity.ID, entity.Name, getPos(entity)));
                }
                if (sendTo.Supports(CpeExt.ChangeModel)) {
                    sendTo.Send(Packet.MakeChangeModel((byte)entity.ID, entity.Model));
                }
            }
        }

        public static void ShowAll() {
            foreach (Entity entity in Entities) {
                foreach (Player sendTo in Server.Players.Where(p => p.World == getWorld(entity))) {
                    if (sendTo.Supports(CpeExt.ExtPlayerList2)) {
                        sendTo.Send(Packet.MakeExtAddEntity2(entity.ID, entity.Name, entity.Skin, getPos(entity), sendTo));
                    } else {
                        sendTo.Send(Packet.MakeAddEntity(entity.ID, entity.Name, getPos(entity)));
                    }
                    if (sendTo.Supports(CpeExt.ChangeModel)) {
                        sendTo.Send(Packet.MakeChangeModel((byte)entity.ID, entity.Model));
                    }
                }
            }
            SaveAll(false);
        }

        public static void TeleportEntity(Entity entity, Position p) {
            getWorld(entity).Players.Send(Packet.MakeTeleport(entity.ID, p));
            setPos(entity, p);
            SaveAll(false);
        }

        public static void UpdateEntityWorld(Entity entity, string newWorld) {
            entity.World = newWorld;
            SaveAll(false);
            ShowEntity(entity);
        }

        public static void ChangeEntityModel(Entity entity, string botModel) {
            getWorld(entity).Players.Send(Packet.MakeRemoveEntity(entity.ID));
            entity.Model = botModel;
            ShowEntity(entity);
            SaveAll(false);
        }

        public static void ChangeEntitySkin(Entity entity, string skin) {
            getWorld(entity).Players.Send(Packet.MakeRemoveEntity(entity.ID));
            entity.Skin = skin;
            ShowEntity(entity);
            SaveAll(false);
        }

        public static void RemoveEntity(Entity entity) {
            getWorld(entity).Players.Send(Packet.MakeRemoveEntity(entity.ID));
            Entities.Remove(entity);
            SaveAll(false);
        }

        public static void RemoveAll(World world) {
            foreach (Entity e in Entities.Where(e => getWorld(e) == world)) {
                world.Players.Send(Packet.MakeRemoveEntity(e.ID));
            }
            Entities.RemoveAll(w => getWorld(w) == world);
            SaveAll(false);
        }

        public static void ReloadAll() {
            foreach (Entity entity in Entities) {
                getWorld(entity).Players.Send(Packet.MakeRemoveEntity(entity.ID));
            }
            Entities.Clear();
            LoadAll();
            ShowAll();
        }

        public static void SaveAll(bool verbose) {
            try {
                Stopwatch sw = Stopwatch.StartNew();
                using (Stream s = File.Create(Paths.EntitiesFileName)) {
                    JsonSerializer.SerializeToStream(Entities.ToArray(), s);
                }
                sw.Stop();
                if (verbose) {
                    Logger.Log(LogType.Debug, "Entities.Save: Saved entities in {0}ms", sw.ElapsedMilliseconds);
                }
            } catch (Exception ex) {
                Logger.Log(LogType.Error, "Entities.Save: " + ex);
            }
        }

        public static void LoadAll() {
            if (Directory.Exists("Entities")) {
                OldLoad();
                Directory.Delete("Entities", true);
                SaveAll(false);
            }
            if (!File.Exists(Paths.EntitiesFileName)) return;

            try {
                using (Stream s = File.OpenRead(Paths.EntitiesFileName)) {
                    Entities = (List<Entity>)
                        JsonSerializer.DeserializeFromStream(typeof(List<Entity>), s);
                }
                int count = 0;
                for (int i = 0; i < Entities.Count; i++) {
                    if (Entities[i] == null)
                        continue;
                    // fixup for servicestack not writing out null entries
                    if (Entities[i].Name == null) {
                        Entities[i] = null; continue;
                    }
                    count++;
                }
                Logger.Log(LogType.SystemActivity, "Entity.Load: Loaded " + count + " entities");
                SaveAll(true);
            } catch (Exception ex) {
                Entities = null;
                Logger.Log(LogType.Error, "Entity.Load: " + ex);
            }
        }
        public static void OldLoad() {
            string[] EntityFileList = Directory.GetFiles("./Entities");
            foreach (string filename in EntityFileList) {
                Entity entity = new Entity();
                if (Path.GetExtension("./Entities/" + filename) == ".txt") {
                    string[] entityData = File.ReadAllLines(filename);
                    entity.Model = CpeCommands.ParseModel(null, entityData[2]) ?? "humanoid";
                    entity.World = WorldManager.FindWorldExact(entityData[4]).Name ?? null;
                    World world = getWorld(entity);
                    if (entity.World == null) continue;
                    sbyte id;
                    if (!sbyte.TryParse(entityData[3], out id)) { id = CpeCommands.getNewID(world); }
                    entity.ID = id;
                    entity.Name = entityData[0] ?? entity.ID.ToString();
                    entity.Skin = entityData[1] ?? entity.Name;
                    Position pos;
                    if (!short.TryParse(entityData[5], out pos.X)) {
                        pos.X = world.map.Spawn.X;
                    }
                    if (!short.TryParse(entityData[6], out pos.Y)) {
                        pos.Y = world.map.Spawn.Y;
                    }
                    if (!short.TryParse(entityData[7], out pos.Z)) {
                        pos.Z = world.map.Spawn.Z;
                    }
                    if (!byte.TryParse(entityData[8], out pos.L)) {
                        pos.L = world.map.Spawn.L;
                    }
                    if (!byte.TryParse(entityData[9], out pos.R)) {
                        pos.R = world.map.Spawn.R;
                    }
                    setPos(entity, pos);
                    Entities.Add(entity);
                }
            }
        }
    }
}