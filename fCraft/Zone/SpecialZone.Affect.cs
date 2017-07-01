//  ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.IO;
using JetBrains.Annotations;

namespace fCraft {
    public static partial class SpecialZone {
        
        internal static bool CheckAffectZone(Player p, Zone zone, Vector3I coord) {
            if (zone.Name.CaselessStarts(Door)) {
                p.RevertBlockNow(coord);
                HandleDoor(p, zone); return true;
            } else if (zone.Name.CaselessStarts(Sign)) {
                p.RevertBlockNow(coord);
                HandleSign(p, zone); return true;
            } else if (zone.Name.CaselessStarts(Command) || zone.Name.CaselessStarts(ConsoleCommand)) {
                p.RevertBlockNow(coord);
                HandleCommandBlock(p, zone); return true;
            }
            return false;
        }
        
        static void HandleDoor(Player p, Zone zone) {
            lock (ZoneCommands.openDoorsLock) {
                if (!ZoneCommands.openDoors.Contains(zone)) {
                    ZoneCommands.openDoor(zone, p);
                    ZoneCommands.openDoors.Add(zone);
                }
            }
        }
        
        static void HandleSign(Player p, Zone zone) {
            p.LastSignClicked = zone.Name;
            if (TextCooldown(p)) return;
            
            if (zone.Sign == null) {
                string path = SignPath(p, zone);
                if (File.Exists(path)) {
                    p.SignLines = File.ReadAllLines(path);
                    p.Message(String.Join("&N", p.SignLines));
                    Logger.Log(LogType.Debug, "[Signs] {0} clicked on sign [{1}] On map [{2}]", p.Name, zone.Name, p.World.Name);
                } else {
                    p.Message("&WThis zone, {0}&W, is marked as a signpost, but no text is added to the sign!", zone.ClassyName);
                    Logger.Log(LogType.Debug, "[Signs] {0} clicked on an empty sign [{1}] On map: [{2}]", p.Name, zone.Name, p.World.Name);
                }
            } else {
                p.Message("&WThis zone, {0}&W, is marked as a signpost, but no text is added to the sign!", zone.ClassyName);
                Logger.Log(LogType.Debug, "[Signs] {0} clicked on an empty sign [{1}] On map: [{2}]", p.Name, zone.Name, p.World.Name);
            }
            p.LastZoneNotification = DateTime.UtcNow;
        }
        
        static void HandleCommandBlock(Player p, Zone zone) {
            p.LastSignClicked = zone.Name;
            if (p.IsCommandBlockRunning || TextCooldown(p)) return;
            
            if (zone.Sign == null) {
                string path = SignPath(p, zone);
                if (File.Exists(path)) {
                    p.SignLines = File.ReadAllLines(path);
                    
                    if (p.SignLines.Length >= 1) {
                        if (!zone.Name.CaselessStarts(ConsoleCommand)) {
                            Scheduler.NewTask(CommandBlock).RunRepeating(p, new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 1), p.SignLines.Length);
                        } else {
                            Scheduler.NewTask(CommandBlock).RunRepeating(p, new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 0, 0, 1), p.SignLines.Length);
                        }
                    }
                    Logger.Log(LogType.Debug, "[Signs] {0} clicked on command block [{1}] On map [{2}]", p.Name, zone.Name, p.World.Name);
                } else {
                    p.Message("&WThis zone, {0}&W, is marked as a command block, but no text is added to the sign!", zone.ClassyName);
                    Logger.Log(LogType.Debug, "[Signs] {0} clicked on an empty command block [{1}] On map: [{2}]", p.Name, zone.Name, p.World.Name);
                }
            } else {
                p.Message("&WThis zone, {0}&W, is marked as a command block, but no text is added to the sign!", zone.ClassyName);
                Logger.Log(LogType.Debug, "[Signs] {0} clicked on an empty command block [{1}] On map: [{2}]", p.Name, zone.Name, p.World.Name);
            }
            p.LastZoneNotification = DateTime.UtcNow;
        }
        
        static bool TextCooldown(Player p) {
            return p.SignLines != null && (DateTime.UtcNow - p.LastZoneNotification).Seconds <= p.SignLines.Length;
        }
        
        static void CommandBlock(SchedulerTask task) {
            Player p = (Player)task.UserState;
            p.IsCommandBlockRunning = true;
            if (p.SignLines[p.SignPos] != null) {
                Player dst = task.Interval == new TimeSpan(0, 0, 0, 0, 1) ? Player.Console : p;
                
                try {
                    dst.ParseMessage(p.SignLines[p.SignPos], false);
                } catch (Exception ex) {
                    p.Message("Command produces error: \"" + p.SignLines[p.SignPos] + "\"");
                    Logger.Log(LogType.Error, ex.ToString());
                }
            }
            
            p.LastZoneNotification = DateTime.UtcNow;
            p.SignPos++;
            if (p.SignPos >= p.SignLines.Length) {
                p.SignLines = null;
                p.SignPos = 0;
                p.IsCommandBlockRunning = false;
            }
        }
    }
}