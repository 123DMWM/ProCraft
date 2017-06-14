// ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ServiceStack.Text;

namespace fCraft {

    /// <summary> Represents a non-player entity in a world. </summary>
    public sealed class Entity {

        public string Name { get; set; }
        public string Skin { get; set; }
        public string World { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public byte R { get; set; }
        public byte L { get; set; }
        public sbyte ID { get; set; }
        public string Model { get; set; }
        
        
        /// <summary> Gets the position of this entity. </summary>
        public Position GetPos() {
            return new Position(X, Y, Z, R, L);
        }
        
        /// <summary> Sets the position of this entity. </summary>
        public void SetPos(Position pos) {
            X = pos.X; Y = pos.Y; Z = pos.Z;
            R = pos.R; L = pos.L;
        }
        
        /// <summary> Gets the world this entity is in. Can be null. </summary>
        /// <returns></returns>
        public World WorldIn() {
            return WorldManager.FindWorldExact(World);
        }
        
        /// <summary> Despawns this entity to all players in the entity's world. </summary>
        public void Despawn() {
            World world = WorldIn();
            if (world != null) world.Players.Send(Packet.MakeRemoveEntity(ID));
        }

        /// <summary> Spawns this entity to all players in the entity's world. </summary>
        public void Spawn() {
            World world = WorldIn();
            if (world == null) return;
            foreach (Player pl in world.Players) { Entities.Spawn(pl, false, this); }
        }
        
        
        /// <summary> Moves this entity to the given position,
        /// and updates this entity's position to all players in the entity's world. </summary>
        public void TeleportTo(Position p) {
            Player[] players = WorldIn().Players;
            foreach (Player pl in players) { pl.Send(pl.TeleportPacket(ID, p)); }
            SetPos(p);
            SaveAll(false);
        }

        /// <summary> Changes the world this entity is in,
        /// and spawns this entity to all players in the new world. </summary>
        public void ChangeWorld(string newWorld) {
            World = newWorld;
            Spawn();
            SaveAll(false);
        }

        /// <summary> Changes the model of this entity to the given model
        /// and updates this entity's model to all players in the entity's world. </summary>
        public void ChangeModel(string model) {
            Despawn();
            Model = model;
            Spawn();
            SaveAll(false);
        }

        /// <summary> Changes the skin of this entity to the given skin,
        /// and updates this entity's skin to all players in the entity's world. </summary>
        public void ChangeSkin(string skin) {
            Despawn();
            Skin = skin;
            Spawn();
            SaveAll(false);
        }
        

        /// <summary> List of all entities across all worlds. </summary>
        public static List<Entity> EntityList = new List<Entity>();
        
        /// <summary> Enumerable of all entities that are in the given world. </summary>
        public static IEnumerable<Entity> AllIn(string worldName) {
            return EntityList.Where(e => e.World.CaselessEquals(worldName));
        }
        
        /// <summary> Enumerable of all entities that are in the given world. </summary>
        public static IEnumerable<Entity> AllIn(World world) { return AllIn(world.Name); }

        /// <summary> Returns whether any entities exist in the given world. </summary>
        public static bool AnyIn(World world) { return AllIn(world).Any(); }

        /// <summary> Retrieves the entity who is located in the given world,
        /// and whose name caselessly matches the input. </summary>
        public static Entity Find(World world, string name) {
            foreach (Entity e in AllIn(world)) {
                if (e.Name.CaselessEquals(name)) return e;
            }
            return null;
        }

        /// <summary> Returns whether there is an entity located in the given world,
        /// whose name caselessly matches the input. </summary>
        public static bool Exists(World world, string name) { return Find(world, name) != null; }

        /// <summary> Creates a new entity and adds it to the list of entities. </summary>
        public static Entity Create(string name, string skin, string modelName,
                                    World world, Position pos, sbyte entityID) {
            Entity entity = new Entity();
            entity.Name = name;
            entity.Skin = skin;
            entity.Model = modelName;
            entity.World = world.Name;
            entity.SetPos(pos);
            entity.ID = entityID;
            
            EntityList.Add(entity);
            entity.Spawn();
            SaveAll(false);
            return entity;
        }

        public static void SpawnAll() {
        	foreach (Entity e in EntityList) { e.Spawn(); }
            SaveAll(false);
        }

        public static void Remove(Entity entity) {
            entity.Despawn();
            EntityList.Remove(entity);
            SaveAll(false);
        }

        public static void RemoveAllIn(World world) {
            foreach (Entity e in AllIn(world)) { e.Despawn(); }
            
            EntityList.RemoveAll(e => e.World.CaselessEquals(world.Name));
            SaveAll(false);
        }

        public static void ReloadAll() {
            foreach (Entity e in EntityList) { e.Despawn(); }
            
            EntityList.Clear();
            LoadAll();
            SpawnAll();
        }

        public static void SaveAll(bool verbose) {
            try {
                Stopwatch sw = Stopwatch.StartNew();
                using (Stream s = File.Create(Paths.EntitiesFileName)) {
                    JsonSerializer.SerializeToStream(EntityList.ToArray(), s);
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
                    EntityList = (List<Entity>)
                        JsonSerializer.DeserializeFromStream(typeof(List<Entity>), s);
                }
                int count = 0;
                for (int i = 0; i < EntityList.Count; i++) {
                    if (EntityList[i] == null)
                        continue;
                    // fixup for servicestack not writing out null entries
                    if (EntityList[i].Name == null) {
                        EntityList[i] = null; continue;
                    }
                    count++;
                }
                Logger.Log(LogType.SystemActivity, "Entity.Load: Loaded " + count + " entities");
                SaveAll(true);
            } catch (Exception ex) {
                EntityList = null;
                Logger.Log(LogType.Error, "Entity.Load: " + ex);
            }
        }
        
        public static void OldLoad() {
            string[] EntityFileList = Directory.GetFiles("./Entities");
            foreach (string filename in EntityFileList) {
                Entity entity = new Entity();
                if (Path.GetExtension(filename) != ".txt") continue;
                
                string[] entityData = File.ReadAllLines(filename);
                entity.Model = CpeCommands.ParseModel(null, entityData[2]) ?? "humanoid";
                entity.World = WorldManager.FindWorldExact(entityData[4]).Name ?? null;
                World world = entity.WorldIn();
                if (entity.World == null) continue;
                
                sbyte id;
                if (!sbyte.TryParse(entityData[3], out id)) { id = CpeCommands.getNewID(world); }
                entity.ID = id;
                entity.Name = entityData[0] ?? entity.ID.ToString();
                entity.Skin = entityData[1] ?? entity.Name;
                Position pos;
                
                
                if (!int.TryParse(entityData[5], out pos.X)) {
                    pos.X = world.map.Spawn.X;
                }
                if (!int.TryParse(entityData[6], out pos.Y)) {
                    pos.Y = world.map.Spawn.Y;
                }
                if (!int.TryParse(entityData[7], out pos.Z)) {
                    pos.Z = world.map.Spawn.Z;
                }
                if (!byte.TryParse(entityData[8], out pos.L)) {
                    pos.L = world.map.Spawn.L;
                }
                if (!byte.TryParse(entityData[9], out pos.R)) {
                    pos.R = world.map.Spawn.R;
                }
                
                entity.SetPos(pos);
                EntityList.Add(entity);
            }
        }
    }
}