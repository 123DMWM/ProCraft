// ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.Linq;

namespace fCraft {
    /// <summary> Handlers for commands and PM from IRC. </summary>
    internal static class IRCHandlers {
        
        internal static DateTime lastIrcCommand;
        const string Reset = "\u211C", Bold = "\u212C";
        
        static string Formatter(Player p) {
            string value = p.Info.Rank.Color + p.Info.Name;
            if (p.World != null)
                value += " &S[" + p.World.ClassyName + "&S]" + Reset;
            return value;
        }
        
        public static bool HandleCommand(string nick, string userNick, string rawMessage) {
            nick = nick.ToLower();
            if (!(rawMessage[0] == '!' || rawMessage.StartsWith(nick, StringComparison.OrdinalIgnoreCase)))
                return false;
            
            string cmd = rawMessage.ToLower();
            bool elapsed = DateTime.UtcNow.Subtract(lastIrcCommand).TotalSeconds > 5;
            
            if (cmd == "!players" || cmd == ".who" || cmd == ".players" || cmd == nick + " players") {
                if (!elapsed) return true;
                var visiblePlayers = Server.Players.Where(p => !p.Info.IsHidden)
                    .OrderBy(p => p, PlayerListSorter.Instance).ToArray();
                
                if (visiblePlayers.Any()) {
                    IRC.SendChannelMessage(Bold + "Players online: " + Reset +
                                       visiblePlayers.JoinToString(Formatter));
                    lastIrcCommand = DateTime.UtcNow;
                } else {
                    IRC.SendChannelMessage(Bold + "There are no players online.");
                    lastIrcCommand = DateTime.UtcNow;
                }
                return true;
            } else if (cmd.StartsWith("!st") || cmd.StartsWith(nick + " st")) {
                if (!elapsed) return true;
                int start = cmd[0] == '!' ? 4 : nick.Length + 4;
                if (cmd.Length > start) {
                    Chat.IRCSendStaff(userNick, rawMessage.Remove(0, start));
                    lastIrcCommand = DateTime.UtcNow;
                }
                return true;
            } else if (cmd.StartsWith("!seen") || cmd.StartsWith(nick + " seen")) {
                if (!elapsed) return true;
                int start = cmd[0] == '!' ? 6 : nick.Length + 6;
                if (cmd.Length > start) {
                    string findPlayer = rawMessage.Remove(0, start);
                    PlayerInfo info = PlayerDB.FindPlayerInfoExact(findPlayer);
                    if (info != null) {
                        Player target = info.PlayerObject;
                        if (target != null) {
                            IRC.SendChannelMessage("Player " + Bold + "{0}" + Reset + " has been " +
                                                   Bold + "&aOnline" + Reset + " for " + Bold + "{1}",
                                                   target.Info.Rank.Color + target.Name, target.Info.TimeSinceLastLogin.ToMiniString());
                            if (target.World != null) {
                                IRC.SendChannelMessage("They are currently on world " + Bold + "{0}",
                                                       target.World.ClassyName);
                            }
                        } else {
                            IRC.SendChannelMessage("Player " + Bold + "{0}" + Reset + " is " + Bold + "&cOffline",
                                                   info.ClassyName);
                            IRC.SendChannelMessage(
                                "They were last seen " + Bold + "{0}" + Reset + " ago on world " + Bold + "{1}",
                                info.TimeSinceLastSeen.ToMiniString(),
                                info.LastWorld);
                        }
                    } else {
                        IRC.SendChannelMessage("No player found with name \"" + Bold + findPlayer + Reset + "\"");
                    }
                } else {
                    IRC.SendChannelMessage("Please specify a player name");
                }
                lastIrcCommand = DateTime.UtcNow;
                return true;
            } else if (cmd.StartsWith("!bd") || cmd.StartsWith(nick + " bd")) {
                if (!elapsed) return true;
                int start = cmd[0] == '!' ? 4 : nick.Length + 4;
                if (cmd.Length > start) {
                    string findPlayer = rawMessage.Remove(0, start);
                    PlayerInfo info = PlayerDB.FindPlayerInfoExact(findPlayer);
                    if (info != null) {
                        IRC.SendChannelMessage("Player " + Bold + "{0}" + Reset +
                                               " has Built: " + Bold + "{1}" + Reset +
                                               " blocks Deleted: " + Bold + "{2}" + Reset + " blocks{3}",
                                               info.ClassyName, info.BlocksBuilt, info.BlocksDeleted,
                                               (info.Can(Permission.Draw) ? " Drawn: " + Bold + info.BlocksDrawnString + Reset + " blocks." : ""));
                    } else {
                        IRC.SendChannelMessage("No player found with name \"" + Bold + findPlayer + Reset + "\"");
                    }
                } else {
                    IRC.SendChannelMessage("Please specify a player name.");
                }
                lastIrcCommand = DateTime.UtcNow;
                return true;
            } else if (cmd.StartsWith("!time") || cmd.StartsWith(nick + " time")) {
                if (!elapsed) return true;
                int start = cmd[0] == '!' ? 6 : nick.Length + 6;
                if (cmd.Length > start) {
                    string findPlayer = rawMessage.Remove(0, start);
                    PlayerInfo info = PlayerDB.FindPlayerInfoExact(findPlayer);
                    if (info != null && info.IsOnline) {
                        TimeSpan idle = info.PlayerObject.IdleTime;
                        IRC.SendChannelMessage("Player " + Bold + "{0}" + Reset + " has spent a total of: " + Bold + "{1:F1}" + Reset +
                                               " hours (" + Bold + "{2:F1}" + Reset + " hours this session{3}",
                                               info.ClassyName,
                                               (info.TotalTime + info.TimeSinceLastLogin).TotalHours,
                                               info.TimeSinceLastLogin.TotalHours,
                                               idle > TimeSpan.FromMinutes(1) ?  ", been idle for " + Bold +
                                               string.Format("{0:F2}", idle.TotalMinutes) + Reset + " minutes)" : ")");
                    } else if (info != null) {
                        IRC.SendChannelMessage("Player " + Bold + "{0}" + Reset + " has spent a total of: "
                                               + Bold + "{1:F1}" + Reset + " hours",
                                               info.ClassyName,
                                               info.TotalTime.TotalHours);
                    } else {
                        IRC.SendChannelMessage("No player found with name \"" + Bold + findPlayer + Reset + "\"");
                    }
                } else {
                    IRC.SendChannelMessage("Server has been up for: " + Bold + DateTime.UtcNow.Subtract(Server.StartTime).ToMiniString());
                }
                lastIrcCommand = DateTime.UtcNow;
                return true;
            } else if (cmd.StartsWith("!clients") || cmd.StartsWith(nick + " clients")) {
                if (!elapsed) return true;
                
                var visiblePlayers = Server.Players.Where(p => !p.Info.IsHidden)
                    .OrderBy(p => p, PlayerListSorter.Instance).ToArray();

                Dictionary<string, List<Player>> clients = new Dictionary<string, List<Player>>();
                foreach (var p in visiblePlayers) {
                    string appName = p.ClientName;
                    if (string.IsNullOrEmpty(appName))
                        appName = "(unknown)";

                    List<Player> usingClient;
                    if (!clients.TryGetValue(appName, out usingClient)) {
                        usingClient = new List<Player>();
                        clients[appName] = usingClient;
                    }
                    usingClient.Add(p);
                }
                IRC.SendChannelMessage(Bold + "Players using:");
                foreach (var kvp in clients) {
                    IRC.SendChannelMessage("  " + Bold + "{0}" + Reset + ": {1}",
                                           kvp.Key, kvp.Value.JoinToClassyString());
                }
                lastIrcCommand = DateTime.UtcNow;
                return true;
            } else if (cmd == "!commands" || cmd == nick + " commands") {
                if (!elapsed) return true;
                IRC.SendChannelMessage(Bold + "List of commands: " + Reset + "BD, Commands, Clients, Players, Seen, St, Time");
                lastIrcCommand = DateTime.UtcNow;
                return true;
            }
            return false; //IRC Fantasy commands
        }
        
        static char[] trimChars = {' '};
        public static bool HandlePM(string nick, string userNick, string rawMessage) {
            nick = nick.ToLower();
            if (!(rawMessage[0] == '@' || rawMessage.StartsWith(nick + " @", StringComparison.OrdinalIgnoreCase)))
                return false;
            if (DateTime.UtcNow.Subtract(lastIrcCommand).TotalSeconds <= 5) return true;
            
            int start = rawMessage[0] == '@' ? 1 : nick.Length + 2;
            rawMessage = rawMessage.Substring(start);
            string[] parts = rawMessage.Split(trimChars, 2);
            if (parts.Length == 1) {
                IRC.SendChannelMessage("Please specify a message to send."); return true;
            }            
            string name = parts[0], contents = parts[1];

            // first, find ALL players (visible and hidden)
            Player[] matches = Server.FindPlayers(name, SearchOptions.IncludeHidden);

            // if there is more than 1 target player, exclude hidden players
            if (matches.Length > 1)
                matches = Server.FindPlayers(name, SearchOptions.Default);

            if (matches.Length == 1) {
                Player target = matches[0];
                if (target.Info.ReadIRC && !target.IsDeaf) {
                    Chat.IRCSendPM(userNick, target, contents);
                    lastIrcCommand = DateTime.UtcNow;
                }

                if (target.Info.IsHidden) {
                    // message was sent to a hidden player
                    IRC.SendChannelMessage("No players found matching \"" +
                                       Bold + name + Reset + "\"");
                    lastIrcCommand = DateTime.UtcNow;
                } else {
                    // message was sent normally
                    if (!target.Info.ReadIRC) {
                        if (!target.Info.IsHidden) {
                            IRC.SendChannelMessage("&WCannot PM " + Bold +
                                               target.ClassyName + Reset +
                                               "&W: they have IRC ignored.");
                        }
                    } else if (target.IsDeaf) {
                        IRC.SendChannelMessage("&WCannot PM " + Bold +
                                           target.ClassyName +
                                           Reset + "&W: they are currently deaf.");
                    } else {
                        IRC.SendChannelMessage("to " + Bold + target.Name + Reset + ": " +
                                           contents);
                    }
                    lastIrcCommand = DateTime.UtcNow;
                }

            } else if (matches.Length == 0) {
                IRC.SendChannelMessage("No players found matching \"" + Bold + name + Reset + "\"");
            } else {
                string list = matches.Take(15).JoinToString(", ", p => p.ClassyName);
                if (matches.Length > 15) {
                    IRC.SendChannelMessage("More than " + Bold + matches.Length + Reset +
                                       " players matched: " + list);
                } else {
                    IRC.SendChannelMessage("More than one player matched: " + list);
                }
                lastIrcCommand = DateTime.UtcNow;
            }
            return true;
        }
    }
}