// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2018 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;

namespace fCraft {
    internal static class NameMatcher {
        
        internal static World FindWorldMatches(Player player, string name) {
            if (name == "-") {
                if (player.LastUsedWorldName != null) {
                    name = player.LastUsedWorldName;
                } else {
                    player.Message("Cannot repeat world name: you haven't used any names yet.");
                    return null;
                }
            }
            player.LastUsedWorldName = name;
            World[] matches = WorldManager.FindWorlds(player, name);

            if (matches.Length == 0) {
                player.MessageNoWorld(name);
                return null;
            } else if (matches.Length > 1) {
                player.MessageManyMatches("world", matches);
                return null;
            }
            return matches[0];
        }        
        
        internal static Player FindPlayerMatches(Player player, string namePart, SearchOptions options) {
            // Repeat last-used player name
            if (namePart == "-") {
                if (player.LastUsedPlayerName != null) {
                    namePart = player.LastUsedPlayerName;
                } else {
                    player.Message("Cannot repeat player name: you haven't used any names yet.");
                    return null;
                }
            }

            // in case someone tries to use the "!" prefix in an online-only search
            if (namePart.Length > 0 && namePart[0] == '!')
                namePart = namePart.Substring(1);

            // Make sure player name is valid
            if (!Player.ContainsValidCharacters(namePart)) {
                player.MessageInvalidPlayerName(namePart);
                return null;
            }

            Player[] matches = Server.FindPlayers(player, namePart, options);
            if (matches.Length == 0) {
                player.MessageNoPlayer(namePart);
                return null;
            } else if (matches.Length > 1) {
                player.MessageManyMatches("player", matches);
                return null;
            } else {
                player.LastUsedPlayerName = matches[0].Name;
                return matches[0];
            }
        }
        
        internal static List<T> Find<T>(T[] items, string name, Func<T, string> getName) {
            List<T> results = new List<T>();
            for (int i = 0; i < items.Length; i++) {
                if (items[i] == null) continue;
                string itemName = getName(items[i]);
                if (itemName == null) continue;
                
                if (itemName.CaselessEquals(name)) {
                    results.Clear();
                    results.Add(items[i]);
                    return results;
                } else if (itemName.CaselessStarts(name)) {
                    results.Add(items[i]);
                }
            }
            return results;
        }
    }
}
