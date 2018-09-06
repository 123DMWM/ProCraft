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
using System.Diagnostics;
using System.IO;
using ServiceStack.Text;

namespace fCraft.Portals {
    public class PortalDB {
        static TimeSpan SaveInterval = TimeSpan.FromSeconds(120);
        static readonly object IOLock = new object();

        public static void Save() {
            try {
                lock (IOLock) {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    int worlds = 0, portals = 0;

                    using (StreamWriter w = new StreamWriter(Paths.PortalDBFileName, false)) {
                        World[] cache = WorldManager.Worlds;
                        foreach (World world in cache) {
                            if (world.Portals.Count == 0) continue;
                            
                            lock (world.Portals.locker) {
                                worlds++;
                                portals += world.Portals.Count;

                                foreach (Portal portal in world.Portals.entries) {
                                    w.WriteLine(JsonSerializer.SerializeToString(portal));
                                }
                            }
                        }
                    }

                    stopwatch.Stop();
                    Logger.Log(LogType.Debug, "PortalDB.Save: Saved {0} portal(s) of {1} world(s) in {2}ms", portals, worlds, stopwatch.ElapsedMilliseconds);
                }
            } catch (Exception ex) {
                Logger.Log(LogType.Error, "PortalDB.Save: " + ex);
            }
        }

        public static void Load() {
            try {
                lock (IOLock) {
                    using (StreamReader r = new StreamReader(Paths.PortalDBFileName)) {
                        string line;
                        int count = 0;

                        while ((line = r.ReadLine()) != null) {
                            Portal portal = (Portal)JsonSerializer.DeserializeFromString(line, typeof(Portal));
                            World world = WorldManager.FindWorldExact(portal.Place);
                            if (world == null) continue;

                            world.Portals.Add(portal);
                            count++;
                        }

                        if (count > 0) {
                            Logger.Log(LogType.SystemActivity, "PortalDB.Load: Loaded " + count + " portals");
                        }
                    }
                }
            } catch (FileNotFoundException) {
                Logger.Log(LogType.Warning, "PortalDB file does not exist. Ignore this message if you have not created any portals yet.");
            } catch (Exception ex) {
                Logger.Log(LogType.Error, "PortalDB.Load: " + ex);
            }
        }

        static void SaveCallback( SchedulerTask task ) { Save(); }
        public static void StartSaveTask() {
            Scheduler.NewBackgroundTask( SaveCallback ).RunForever(SaveInterval, SaveInterval + TimeSpan.FromSeconds(15));
        }
    }
}