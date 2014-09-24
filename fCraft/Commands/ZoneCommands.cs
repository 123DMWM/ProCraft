// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | Copyright 2014 123DMWM <shmo1joe2@gmail.com>
using System;
using System.IO;
using fCraft.MapConversion;

namespace fCraft {
    /// <summary> Contains commands related to zone management. </summary>
    static class ZoneCommands {

        internal static void Init() {
            CommandManager.RegisterCommand( CdZoneAdd );
            CommandManager.RegisterCommand( CdSignAdd );
            CommandManager.RegisterCommand( CdZoneEdit );
            CommandManager.RegisterCommand( CdZoneInfo );
            CommandManager.RegisterCommand( CdZoneList );
            CommandManager.RegisterCommand( CdZoneMark );
            CommandManager.RegisterCommand( CdZoneRemove );
            CommandManager.RegisterCommand( CdSignRemove );
            CommandManager.RegisterCommand( CdZoneRename );
            CommandManager.RegisterCommand( CdZoneTest );
            CommandManager.RegisterCommand( CdZoneShow );

        }
        #region ZoneAdd

        static readonly CommandDescriptor CdZoneAdd = new CommandDescriptor {
            Name = "ZAdd",
            Category = CommandCategory.Zone,
            Aliases = new[] { "zone" },
            Permissions = new[] { Permission.ManageZones },
            Usage = "/ZAdd ZoneName RankName",
            Help = "Create a zone that overrides build permissions. " +
                   "This can be used to restrict access to an area (by setting RankName to a high rank) " +
                   "or to designate a guest area (by lowering RankName).",
            Handler = ZoneAddHandler
        };

        static void ZoneAddHandler( Player player, CommandReader cmd ) {
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );

            string givenZoneName = cmd.Next();
            if( givenZoneName == null ) {
                CdZoneAdd.PrintUsage( player );
                return;
            }

            if( !player.Info.Rank.AllowSecurityCircumvention ) {
                SecurityCheckResult buildCheck = playerWorld.BuildSecurity.CheckDetailed( player.Info );
                switch( buildCheck ) {
                    case SecurityCheckResult.BlackListed:
                        player.Message( "Cannot add zones to world {0}&S: You are barred from building here.",
                                        playerWorld.ClassyName );
                        return;
                    case SecurityCheckResult.RankTooLow:
                        player.Message( "Cannot add zones to world {0}&S: You are not allowed to build here.",
                                        playerWorld.ClassyName );
                        return;
                }
            }

            Zone newZone = new Zone();
            ZoneCollection zoneCollection = player.WorldMap.Zones;
            if (givenZoneName.ToLower().StartsWith("deny_") || givenZoneName.ToLower().StartsWith("respawn_") || givenZoneName.ToLower().StartsWith("message_") || givenZoneName.ToLower().StartsWith("death_") || givenZoneName.ToLower().StartsWith("checkpoint_"))
            {
                if (player.Can(Permission.ManageSpecialZones) == false)
                {
                    player.Message("You cannot affect special zones.");
                    return;
                }
            }

            if( givenZoneName.StartsWith( "+" ) ) {
                // personal zone (/ZAdd +Name)
                givenZoneName = givenZoneName.Substring( 1 );

                // Find the target player
                PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches( player, givenZoneName, SearchOptions.IncludeHidden );
                if( info == null ) return;

                // Make sure that the name is not taken already.
                // If a zone named after the player already exists, try adding a number after the name (e.g. "Notch2")
                newZone.Name = info.Name;
                for( int i = 2; zoneCollection.Contains( newZone.Name ); i++ ) {
                    newZone.Name = givenZoneName + i;
                }

                newZone.Controller.MinRank = info.Rank.NextRankUp ?? info.Rank;
                newZone.Controller.Include( info );
                player.Message( "ZoneAdd: Creating a {0}+&S zone for player {1}&S. Click or &H/Mark&S 2 blocks.",
                                newZone.Controller.MinRank.ClassyName, info.ClassyName );
                player.SelectionStart( 2, ZoneAddCallback, newZone, CdZoneAdd.Permissions );

            } else {
                // Adding an ordinary, rank-restricted zone.
                if( !World.IsValidName( givenZoneName ) ) {
                    player.Message( "\"{0}\" is not a valid zone name", givenZoneName );
                    return;
                }

                if( zoneCollection.Contains( givenZoneName ) ) {
                    player.Message( "A zone with this name already exists. Use &H/ZEdit&S to edit." );
                    return;
                }

                newZone.Name = givenZoneName;

                string rankName = cmd.Next();
                if( rankName == null ) {
                    player.Message( "No rank was specified. See &H/Help zone" );
                    return;
                }

                Rank minRank = RankManager.FindRank( rankName );
                if( minRank == null ) {
                    player.MessageNoRank( rankName );
                    return;
                }

                string name;
                while( ( name = cmd.Next() ) != null ) {
                    if( name.Length < 1 ) {
                        CdZoneAdd.PrintUsage( player );
                        return;
                    }
                    PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches(player, name.Substring(1), SearchOptions.Default);
                    if( info == null ) return;

                    if( name.StartsWith( "+" ) ) {
                        newZone.Controller.Include( info );
                    } else if( name.StartsWith( "-" ) ) {
                        newZone.Controller.Exclude( info );
                    }
                }

                newZone.Controller.MinRank = minRank;
                player.SelectionStart( 2, ZoneAddCallback, newZone, CdZoneAdd.Permissions );
                player.Message( "ZoneAdd: Creating zone {0}&S. Click or &H/Mark&S 2 blocks.",
                                newZone.ClassyName );
            }
        }
        static void ZoneAddCallback( Player player, Vector3I[] marks, object tag ) {
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );

            if( !player.Info.Rank.AllowSecurityCircumvention ) {
                SecurityCheckResult buildCheck = playerWorld.BuildSecurity.CheckDetailed( player.Info );
                switch( buildCheck ) {
                    case SecurityCheckResult.BlackListed:
                        player.Message( "Cannot add zones to world {0}&S: You are barred from building here.",
                                        playerWorld.ClassyName );
                        return;
                    case SecurityCheckResult.RankTooLow:
                        player.Message( "Cannot add zones to world {0}&S: You are not allowed to build here.",
                                        playerWorld.ClassyName );
                        return;
                }
            }

            Zone zone = (Zone)tag;
            if (zone.Name.ToLower().StartsWith("checkpoint"))
            {
                if (marks[0].X != marks[1].X || marks[0].Y != marks[1].Y || marks[1].Z - marks[0].Z != 1)
                {
                    player.Message("Checkpoints must be 1x1x2 (Size of a player)");
                    return;
                }
            }
            var zones = player.WorldMap.Zones;
            lock( zones.SyncRoot ) {
                Zone dupeZone = zones.FindExact( zone.Name );
                if( dupeZone != null ) {
                    player.Message( "A zone named \"{0}\" has just been created by {1}",
                                    dupeZone.Name, dupeZone.CreatedBy );
                    return;
                }

                zone.Create( new BoundingBox( marks[0], marks[1] ), player.Info );

                player.Message( "Zone \"{0}\" created, {1} blocks total.",
                                zone.Name, zone.Bounds.Volume );
                Logger.Log( LogType.UserActivity,
                            "{0} {1} &screated a new zone \"{2}\" containing {3} blocks.",
                            player.Info.Rank.Name,
                            player.Name,
                            zone.Name,
                            zone.Bounds.Volume );

                zones.Add( zone );
            }
        }
        #endregion
        #region SignAdd
        static readonly CommandDescriptor CdSignAdd = new CommandDescriptor
        {
            Name = "SAdd",
            Category = CommandCategory.Zone,
            Aliases = new[] { "SignAdd", "sign" },
            Permissions = new[] { Permission.ManageSigns },
            Usage = "/SAdd [Sign name or Number] [Message]",
            Help = "Create a Sign that displays a message when you break that block. ",
            Handler = SignAddHandler
        };

        static void SignAddHandler(Player player, CommandReader cmd)
        {
            World playerWorld = player.World;
            if (playerWorld == null) PlayerOpException.ThrowNoWorld(player);

            string givenZoneName = "Sign" + cmd.Next();
            if (givenZoneName == null || !cmd.HasNext)
            {
                CdSignAdd.PrintUsage(player);
                return;
            }

            if (!player.Info.Rank.AllowSecurityCircumvention)
            {
                SecurityCheckResult buildCheck = playerWorld.BuildSecurity.CheckDetailed(player.Info);
                switch (buildCheck)
                {
                    case SecurityCheckResult.BlackListed:
                        player.Message("Cannot add zones to world {0}&S: You are barred from building here.",
                                        playerWorld.ClassyName);
                        return;
                    case SecurityCheckResult.RankTooLow:
                        player.Message("Cannot add zones to world {0}&S: You are not allowed to build here.",
                                        playerWorld.ClassyName);
                        return;
                }
            }

            Zone newZone = new Zone();
            ZoneCollection zoneCollection = player.WorldMap.Zones;
            if (givenZoneName.ToLower().StartsWith("deny_") || givenZoneName.ToLower().StartsWith("respawn_") || givenZoneName.ToLower().StartsWith("message_") || givenZoneName.ToLower().StartsWith("death_") || givenZoneName.ToLower().StartsWith("checkpoint_"))
            {
                if (player.Can(Permission.ManageSpecialZones) == false)
                {
                    player.Message("You cannot affect special zones.");
                    return;
                }
            }

            // Adding an ordinary, rank-restricted zone.
            if (!World.IsValidName(givenZoneName))
            {
                player.Message("\"{0}\" is not a valid Sign/Zone name", givenZoneName);
                return;
            }

            if (zoneCollection.Contains(givenZoneName))
            {
                player.Message("A Sign/Zone with this name already exists.");
                return;
            }

            newZone.Name = givenZoneName;

            Rank minRank = RankManager.HighestRank;

            if (cmd.HasNext)
            {
                newZone.Sign = cmd.NextAll();
                if (newZone.Sign.Length == 0)
                {
                    CdSignAdd.PrintUsage(player); 
                    return;
                }
                else
                {
                    if (!Directory.Exists("./Signs/")) Directory.CreateDirectory("./Signs/");
                    if (!Directory.Exists("./Signs/" + player.World.Name + "/")) Directory.CreateDirectory("./Signs/" + player.World.Name + "/");
                    File.WriteAllText("./Signs/" + player.World.Name + "/" + newZone.Name + ".txt", cmd.NextAll().Replace("%n", "/n"));
                    player.Message("&SMessage for sign {0}&S is: {1}", newZone.ClassyName, newZone.Sign);
                    newZone.Sign = null;
                }
            }

            newZone.Controller.MinRank = minRank;
            player.SelectionStart(1, SignAddCallback, newZone, CdSignAdd.Permissions);
            player.Message("SignAdd: Creating sign {0}&S. Click or &H/Mark&S 1 block.",
                            newZone.ClassyName);
        }

        static void SignAddCallback(Player player, Vector3I[] marks, object tag)
        {
            World playerWorld = player.World;
            if (playerWorld == null) PlayerOpException.ThrowNoWorld(player);

            if (!player.Info.Rank.AllowSecurityCircumvention)
            {
                SecurityCheckResult buildCheck = playerWorld.BuildSecurity.CheckDetailed(player.Info);
                switch (buildCheck)
                {
                    case SecurityCheckResult.BlackListed:
                        player.Message("Cannot add Sign to world {0}&S: You are barred from building here.",
                                        playerWorld.ClassyName);
                        return;
                    case SecurityCheckResult.RankTooLow:
                        player.Message("Cannot add Sign to world {0}&S: You are not allowed to build here.",
                                        playerWorld.ClassyName);
                        return;
                }
            }

            Zone zone = (Zone)tag;
            var zones = player.WorldMap.Zones;
            lock (zones.SyncRoot)
            {
                Zone dupeZone = zones.FindExact(zone.Name);
                if (dupeZone != null)
                {
                    player.Message("A Sign named \"{0}\" has just been created by {1}",
                                    dupeZone.Name, dupeZone.CreatedBy);
                    return;
                }

                zone.Create(new BoundingBox(marks[0], marks[0]), player.Info);

                player.Message("Sign \"{0}\" created, {1} blocks total.",
                                zone.Name, zone.Bounds.Volume);
                Logger.Log(LogType.UserActivity,
                            "{0} {1} &screated a new Sign \"{2}\" containing {3} blocks.",
                            player.Info.Rank.Name,
                            player.Name,
                            zone.Name,
                            zone.Bounds.Volume);

                zones.Add(zone);
            }
        }

        #endregion
        #region ZoneEdit

        static readonly CommandDescriptor CdZoneEdit = new CommandDescriptor {
            Name = "ZEdit",
            Category = CommandCategory.Zone,
            Permissions = new[] { Permission.ManageZones },
            Usage = "/ZEdit ZoneName [RankName] [+IncludedName] [-ExcludedName]",
            Help = "Allows editing the zone permissions after creation. " +
                   "You can change the rank restrictions, and include or exclude individual players. " +
                   "To include individuals, use \"+PlayerName\". To exclude, use \"-PlayerName\". " +
                   "To clear whitelist, use \"-*\". To clear blacklist use \"+*\"",
            Handler = ZoneEditHandler
        };

        static void ZoneEditHandler( Player player, CommandReader cmd ) {
            if( player.World == null ) PlayerOpException.ThrowNoWorld( player );
            bool changesWereMade = false;
            string zoneName = cmd.Next();
            if( zoneName == null ) {
                player.Message( "No zone name specified. See &H/Help ZEdit" );
                return;
            }

            Zone zone = player.WorldMap.Zones.Find( zoneName );
            if( zone == null ) {
                player.MessageNoZone( zoneName );
                return;
            }
            if (zone.Name.ToLower().StartsWith("deny_") || zone.Name.ToLower().StartsWith("respawn_") || zone.Name.ToLower().StartsWith("message_") || zone.Name.ToLower().StartsWith("death_") || zone.Name.ToLower().StartsWith("checkpoint_"))
            {
                if (player.Can(Permission.ManageSpecialZones) == false)
                {
                    player.Message("You cannot affect special zones.");
                    return;
                }
            }

            //player.Message(cmd.RawMessage);
            //player.Message(cmd.RawMessage.Substring(cmd.Offset));
            if (cmd.RawMessage.Substring(cmd.Offset + 1).StartsWith("#"))
                {
                    zone.Sign = cmd.NextAll().Substring(1);
                    if (zone.Sign.Length == 0 || zone.Sign == null)
                    {
                        if (!Directory.Exists("./Signs/")) Directory.CreateDirectory("./Signs/");
                        if (!Directory.Exists("./Signs/" + player.World.Name + "/")) Directory.CreateDirectory("./Signs/" + player.World.Name + "/"); 
                        if (File.Exists("./Signs/" + player.World.Name + "/" + zone.Name + ".txt")) File.Delete("./Signs/" + player.World.Name + "/" + zone.Name + ".txt");
                        player.Message("&SSign Text for zone {0}&S was removed.", zone.ClassyName);
                        zone.Sign = null;
                    }
                    else
                    {
                        if (!Directory.Exists("./Signs/")) Directory.CreateDirectory("./Signs/");
                        if (!Directory.Exists("./Signs/" + player.World.Name + "/")) Directory.CreateDirectory("./Signs/" + player.World.Name + "/"); 
                        File.WriteAllText("./Signs/" + player.World.Name + "/" + zone.Name + ".txt", cmd.NextAll().Substring(1).Replace("%n", "/n"));
                        player.Message("&SSign Text for zone {0}&S changed to {1}", zone.ClassyName, zone.Sign);
                        zone.Sign = null;
                    }
                    return;
                }

            string nextToken;
            while( (nextToken = cmd.Next()) != null ) {
                // Clear whitelist
                if( nextToken.Equals( "-*" ) ) {
                    PlayerInfo[] oldWhitelist = zone.Controller.ExceptionList.Included;
                    if( oldWhitelist.Length > 0 ) {
                        zone.Controller.ResetIncludedList();
                        player.Message( "Whitelist of zone {0}&S cleared: {1}",
                                        zone.ClassyName, oldWhitelist.JoinToClassyString() );
                        Logger.Log( LogType.UserActivity,
                                    "{0} {1} &scleared whitelist of zone {2} on world {3}: {4}",
                                    player.Info.Rank.Name, player.Name, zone.Name, player.World.Name,
                                    oldWhitelist.JoinToString( pi => pi.Name ) );
                    } else {
                        player.Message( "Whitelist of zone {0}&S is empty.",
                                        zone.ClassyName );
                    }
                    continue;
                }

                // Clear blacklist
                if( nextToken.Equals( "+*" ) ) {
                    PlayerInfo[] oldBlacklist = zone.Controller.ExceptionList.Excluded;
                    if( oldBlacklist.Length > 0 ) {
                        zone.Controller.ResetExcludedList();
                        player.Message( "Blacklist of zone {0}&S cleared: {1}",
                                        zone.ClassyName, oldBlacklist.JoinToClassyString() );
                        Logger.Log( LogType.UserActivity,
                                    "{0} {1} &scleared blacklist of zone {2} on world {3}: {4}",
                                    player.Info.Rank.Name, player.Name, zone.Name, player.World.Name,
                                    oldBlacklist.JoinToString( pi => pi.Name ) );
                    } else {
                        player.Message( "Blacklist of zone {0}&S is empty.",
                                        zone.ClassyName );
                    }
                    continue;
                }

                if( nextToken.StartsWith( "+" ) ) {
                    PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches( player, nextToken.Substring( 1 ), SearchOptions.IncludeSelf );
                    if( info == null ) return;

                    // prevent players from whitelisting themselves to bypass protection
                    if (!player.Info.Rank.AllowSecurityCircumvention && player.Info == info)
                    {
                        switch( zone.Controller.CheckDetailed( info ) ) {
                            case SecurityCheckResult.BlackListed:
                                player.Message( "You are not allowed to remove yourself from the blacklist of zone {0}",
                                                zone.ClassyName );
                                continue;
                            case SecurityCheckResult.RankTooLow:
                                player.Message( "You must be {0}+&S to add yourself to the whitelist of zone {1}",
                                                zone.Controller.MinRank.ClassyName, zone.ClassyName );
                                continue;
                        }
                    }

                    switch( zone.Controller.Include( info ) ) {
                        case PermissionOverride.Deny:
                            player.Message( "{0}&S is no longer excluded from zone {1}",
                                            info.ClassyName, zone.ClassyName );
                            changesWereMade = true;
                            break;
                        case PermissionOverride.None:
                            player.Message( "{0}&S is now included in zone {1}",
                                            info.ClassyName, zone.ClassyName );
                            changesWereMade = true;
                            break;
                        case PermissionOverride.Allow:
                            player.Message( "{0}&S is already included in zone {1}",
                                            info.ClassyName, zone.ClassyName );
                            break;
                    }

                } else if( nextToken.StartsWith( "-" ) ) {
                    PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches(player, nextToken.Substring(1), SearchOptions.Default);
                    if( info == null ) return;

                    if (!player.Info.Rank.AllowSecurityCircumvention && player.Info == info)
                    {
                        switch (zone.Controller.CheckDetailed(info))
                        {
                            case SecurityCheckResult.WhiteListed:
                                player.Message("You are not allowed to remove yourself from the whitelist of zone {0}",
                                                zone.ClassyName);
                                continue;
                            case SecurityCheckResult.RankTooLow:
                                player.Message("You must be {0}+&S to add yourself to the blacklist of zone {1}",
                                                zone.Controller.MinRank.ClassyName, zone.ClassyName);
                                continue;
                        }
                    }

                    switch( zone.Controller.Exclude( info ) ) {
                        case PermissionOverride.Deny:
                            player.Message( "{0}&S is already excluded from zone {1}",
                                            info.ClassyName, zone.ClassyName );
                            break;
                        case PermissionOverride.None:
                            player.Message( "{0}&S is now excluded from zone {1}",
                                            info.ClassyName, zone.ClassyName );
                            changesWereMade = true;
                            break;
                        case PermissionOverride.Allow:
                            player.Message( "{0}&S is no longer included in zone {1}",
                                            info.ClassyName, zone.ClassyName );
                            changesWereMade = true;
                            break;
                    }

                } else {
                    Rank minRank = RankManager.FindRank( nextToken );

                    if( minRank != null ) {
                        // prevent players from lowering rank so bypass protection
                        if( !player.Info.Rank.AllowSecurityCircumvention &&
                            zone.Controller.MinRank > player.Info.Rank && minRank <= player.Info.Rank ) {
                            player.Message( "You are not allowed to lower the zone's rank." );
                            continue;
                        }

                        if( zone.Controller.MinRank != minRank ) {
                            zone.Controller.MinRank = minRank;
                            player.Message( "Permission for zone \"{0}\" changed to {1}+",
                                            zone.Name,
                                            minRank.ClassyName );
                            changesWereMade = true;
                        }
                    } else {
                        player.MessageNoRank( nextToken );
                    }
                }

                if( changesWereMade ) {
                    zone.Edit( player.Info );
                } else {
                    player.Message( "No changes were made to the zone." );
                }
            }
        }

        #endregion ZoneEdit
        #region ZoneInfo

        static readonly CommandDescriptor CdZoneInfo = new CommandDescriptor {
            Name = "ZInfo",
            Aliases = new[] { "ZoneInfo" },
            Category = CommandCategory.Zone | CommandCategory.Info,
            Help = "Shows detailed information about a zone.",
            Usage = "/ZInfo ZoneName",
            UsableByFrozenPlayers = true,
            Handler = ZoneInfoHandler
        };

        static void ZoneInfoHandler( Player player, CommandReader cmd ) {
            string zoneName = cmd.Next();
            if( zoneName == null ) {
                player.Message( "No zone name specified. See &H/Help ZInfo" );
                return;
            }

            Zone zone = player.WorldMap.Zones.Find( zoneName );
            if( zone == null ) {
                player.MessageNoZone( zoneName );
                return;
            }

            player.Message( "About zone \"{0}\": size {1} x {2} x {3}, contains {4} blocks, editable by {5}+.",
                            zone.Name,
                            zone.Bounds.Width, zone.Bounds.Length, zone.Bounds.Height,
                            zone.Bounds.Volume,
                            zone.Controller.MinRank.ClassyName );

            player.Message( "  Zone center is at ({0},{1},{2}).",
                            (zone.Bounds.XMin + zone.Bounds.XMax) / 2,
                            (zone.Bounds.YMin + zone.Bounds.YMax) / 2,
                            (zone.Bounds.ZMin + zone.Bounds.ZMax) / 2 );

            if( zone.CreatedBy != null ) {
                player.Message( "  Zone created by {0}&S on {1:MMM d} at {1:h:mm} ({2} ago).",
                                zone.CreatedByClassy,
                                zone.CreatedDate,
                                DateTime.UtcNow.Subtract( zone.CreatedDate ).ToMiniString() );
            }

            if( zone.EditedBy != null ) {
                player.Message( "  Zone last edited by {0}&S on {1:MMM d} at {1:h:mm} ({2}d {3}h ago).",
                zone.EditedByClassy,
                zone.EditedDate,
                DateTime.UtcNow.Subtract( zone.EditedDate ).Days,
                DateTime.UtcNow.Subtract( zone.EditedDate ).Hours );
            }

            PlayerExceptions zoneExceptions = zone.ExceptionList;

            if( zoneExceptions.Included.Length > 0 ) {
                player.Message( "  Zone whitelist includes: {0}",
                                zoneExceptions.Included.JoinToClassyString() );
            }

            if( zoneExceptions.Excluded.Length > 0 ) {
                player.Message( "  Zone blacklist excludes: {0}",
                                zoneExceptions.Excluded.JoinToClassyString() );
            }
        }

        #endregion
        #region ZoneList

        static readonly CommandDescriptor CdZoneList = new CommandDescriptor {
            Name = "Zones",
            Category = CommandCategory.Zone | CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Usage = "/Zones [WorldName]",
            Help = "Lists all zones defined on the current map/world.",
            Handler = ZoneListHandler
        };

        static void ZoneListHandler( Player player, CommandReader cmd ) {
            World world = player.World;
            string worldName = cmd.Next();
            if( worldName != null ) {
                world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                if( world == null ) return;
                player.Message( "List of zones on {0}&S:",
                                world.ClassyName );

            } else if( world != null ) {
                player.Message( "List of zones on this world:" );

            } else {
                player.Message( "When used from console, &H/Zones&S command requires a world name." );
                return;
            }

            Map map = world.Map;
            if( map == null ) {
                if( !MapUtility.TryLoadHeader( world.MapFileName, out map ) ) {
                    player.Message( "&WERROR:Could not load mapfile for world {0}.",
                                    world.ClassyName );
                    return;
                }
            }

            Zone[] zones = map.Zones.Cache;
            if( zones.Length > 0 ) {
                foreach( Zone zone in zones ) {
                    player.Message( "   {0} ({1}&S) - {2} x {3} x {4}",
                                    zone.Name,
                                    zone.Controller.MinRank.ClassyName,
                                    zone.Bounds.Width,
                                    zone.Bounds.Length,
                                    zone.Bounds.Height );
                }
                player.Message( "   Type &H/ZInfo ZoneName&S for details." );
            } else {
                player.Message( "   No zones defined." );
            }
        }

        #endregion
        #region ZoneMark

        static readonly CommandDescriptor CdZoneMark = new CommandDescriptor {
            Name = "ZMark",
            Category = CommandCategory.Zone | CommandCategory.Building,
            Usage = "/ZMark ZoneName",
            Help = "Uses zone boundaries to make a selection.",
            Handler = ZoneMarkHandler
        };

        static void ZoneMarkHandler( Player player, CommandReader cmd ) {
            if( player.SelectionMarksExpected == 0 ) {
                player.MessageNow( "Cannot use ZMark - no selection in progress." );
            } else if( player.SelectionMarksExpected == 2 ) {
                string zoneName = cmd.Next();
                if( zoneName == null ) {
                    CdZoneMark.PrintUsage( player );
                    return;
                }

                Zone zone = player.WorldMap.Zones.Find( zoneName );
                if( zone == null ) {
                    player.MessageNoZone( zoneName );
                    return;
                }

                player.SelectionResetMarks();
                player.SelectionAddMark( zone.Bounds.MinVertex, false, false );
                player.SelectionAddMark( zone.Bounds.MaxVertex, true, false );
            } else {
                player.MessageNow( "ZMark can only be used for 2-block selection." );
            }
        }

        #endregion
        #region ZoneRemove

        static readonly CommandDescriptor CdZoneRemove = new CommandDescriptor {
            Name = "ZRemove",
            Aliases = new[] { "zdelete" },
            Category = CommandCategory.Zone,
            Permissions = new[] { Permission.ManageZones },
            Usage = "/ZRemove ZoneName",
            Help = "Removes a zone with the specified name from the map.",
            Handler = ZoneRemoveHandler
        };

        static void ZoneRemoveHandler( Player player, CommandReader cmd ) {
            if( player.World == null ) PlayerOpException.ThrowNoWorld( player );

            string zoneName = cmd.Next();
            if( zoneName == null || cmd.HasNext ) {
                CdZoneRemove.PrintUsage( player );
                return;
            }

            if( zoneName == "*" ) {
                if( !cmd.IsConfirmed ) {
                    Logger.Log( LogType.UserActivity,
                                "ZRemove: Asked {0} to confirm removing all zones on world {1}",
                                player.Name, player.World.Name );
                    player.Confirm( cmd,
                                    "&WRemove ALL zones on this world ({0}&W)? This cannot be undone.&S",
                                    player.World.ClassyName );
                    return;
                }
                player.WorldMap.Zones.Clear();
                Logger.Log( LogType.UserActivity,
                            "{0} {1} &sremoved all zones on world {2}",
                            player.Info.Rank.Name, player.Name, player.World.Name );
                Server.Message( "{0} {1}&S removed all zones on world {2}",
                                player.Info.Rank.Name, player.ClassyName, player.World.ClassyName );
                return;
            }

            ZoneCollection zones = player.WorldMap.Zones;
            Zone zone = zones.Find( zoneName );
            if (zone.Name.ToLower().StartsWith("deny_") || zone.Name.ToLower().StartsWith("respawn_") || zone.Name.ToLower().StartsWith("message_") || zone.Name.ToLower().StartsWith("death_") || zone.Name.ToLower().StartsWith("checkpoint_"))
            {
                if (player.Can(Permission.ManageSpecialZones) == false)
                {
                    player.Message("You cannot affect special zones.");
                    return;
                }
            }
            if( zone != null ) {
                if( !player.Info.Rank.AllowSecurityCircumvention ) {
                    switch( zone.Controller.CheckDetailed( player.Info ) ) {
                        case SecurityCheckResult.BlackListed:
                            player.Message( "You are not allowed to remove zone {0}: you are blacklisted.", zone.ClassyName );
                            return;
                        case SecurityCheckResult.RankTooLow:
                            player.Message( "You are not allowed to remove zone {0}.", zone.ClassyName );
                            return;
                    }
                }
                if( !cmd.IsConfirmed ) {
                    Logger.Log( LogType.UserActivity,
                                "ZRemove: Asked {0} to confirm removing zone {1} from world {2}",
                                player.Name, zone.Name, player.World.Name );
                    player.Confirm( cmd, "Remove zone {0}&S?", zone.ClassyName );
                    return;
                }

                if( zones.Remove( zone.Name ) ) {
                    Logger.Log( LogType.UserActivity,
                                "{0} {1} &sremoved zone {2} from world {3}",
                                player.Info.Rank.Name, player.Name, zone.Name, player.World.Name );
                    player.Message( "Zone \"{0}\" removed.", zone.Name );
                }

            } else {
                player.MessageNoZone( zoneName );
            }
        }

        #endregion
        #region SignRemove

        static readonly CommandDescriptor CdSignRemove = new CommandDescriptor
        {
            Name = "SRemove",
            Aliases = new[] { "Sdelete" },
            Category = CommandCategory.Zone,
            Permissions = new[] { Permission.ManageSigns },
            Usage = "/SRemove [Sign Name or number]",
            Help = "Removes a sign with the specified name from the map.",
            Handler = SignRemoveHandler
        };

        static void SignRemoveHandler(Player player, CommandReader cmd)
        {
            if (player.World == null) PlayerOpException.ThrowNoWorld(player);

            string zoneName = "Sign" + cmd.Next();
            if (zoneName == "Sign")
            {
                CdSignRemove.PrintUsage(player);
                return;
            }

            ZoneCollection zones = player.WorldMap.Zones;
            Zone zone = zones.Find(zoneName);
            if (zone != null)
            {
                if (!cmd.IsConfirmed)
                {
                    Logger.Log(LogType.UserActivity,
                                "SRemove: Asked {0} to confirm removing Sign {1} from world {2}",
                                player.Name, zone.Name, player.World.Name);
                    player.Confirm(cmd, "Remove Sign {0}&S?", zone.ClassyName);
                    return;
                }

                if (zones.Remove(zone.Name))
                {
                    Logger.Log(LogType.UserActivity,
                                "{0} {1} &sremoved Sign {2} from world {3}",
                                player.Info.Rank.Name, player.Name, zone.Name, player.World.Name);
                    player.Message("Sign \"{0}\" removed.", zone.Name);
                }

            }
            else
            {
                player.MessageNoZone(zoneName);
            }
        }

        #endregion
        #region ZoneRename

        static readonly CommandDescriptor CdZoneRename = new CommandDescriptor {
            Name = "ZRename",
            Category = CommandCategory.Zone,
            Permissions = new[] { Permission.ManageZones },
            Help = "Renames a zone",
            Usage = "/ZRename OldName NewName",
            Handler = ZoneRenameHandler
        };

        static void ZoneRenameHandler( Player player, CommandReader cmd ) {
            World playerWorld = player.World;
            if(playerWorld==null)PlayerOpException.ThrowNoWorld( player );

            // make sure that both parameters are given
            string oldName = cmd.Next();
            string newName = cmd.Next();
            if( oldName == null || newName == null ) {
                CdZoneRename.PrintUsage( player );
                return;
            }

            // make sure that the new name is valid
            if( !World.IsValidName( newName ) ) {
                player.Message( "\"{0}\" is not a valid zone name", newName );
                return;
            }

            // find the old zone
            var zones = player.WorldMap.Zones;
            Zone oldZone = zones.Find( oldName );
            if( oldZone == null ) {
                player.MessageNoZone( oldName );
                return;
            }
            if (oldZone.Name.ToLower().StartsWith("deny_") || oldZone.Name.ToLower().StartsWith("respawn_") || oldZone.Name.ToLower().StartsWith("message_") || oldZone.Name.ToLower().StartsWith("death_") || oldZone.Name.ToLower().StartsWith("checkpoint_"))
            {
                if (player.Can(Permission.ManageSpecialZones) == false)
                {
                    player.Message("You cannot affect special zones.");
                    return;
                }
            }

            // Check if a zone with "newName" name already exists
            Zone newZone = zones.FindExact( newName );
            if( newZone != null && newZone != oldZone ) {
                player.Message( "A zone with the name \"{0}\" already exists.", newName );
                return;
            }

            // check if any change is needed
            string fullOldName = oldZone.Name;
            if( fullOldName == newName ) {
                player.Message( "The zone is already named \"{0}\"", fullOldName );
                return;
            }

            // actually rename the zone
            zones.Rename( oldZone, newName );

            // announce the rename
            playerWorld.Players.Message( "&SZone \"{0}\" was renamed to \"{1}&S\" by {2}",
                                         fullOldName, oldZone.ClassyName, player.ClassyName );
            Logger.Log( LogType.UserActivity,
                        "{0} {1} &srenamed zone \"{2}\" to \"{3}\" on world {4}",
                        player.Info.Rank.Name, player.Name, fullOldName, newName, playerWorld.Name );
        }

        #endregion
        #region ZoneTest

        static readonly CommandDescriptor CdZoneTest = new CommandDescriptor {
            Name = "ZTest",
            Category = CommandCategory.Zone | CommandCategory.Info,
            RepeatableSelection = true,
            Help = "Allows to test exactly which zones affect a particular block. Can be used to find and resolve zone overlaps.",
            Handler = ZoneTestHandler
        };

        static void ZoneTestHandler( Player player, CommandReader cmd ) {
            player.SelectionStart( 1, ZoneTestCallback, null );
            player.Message( "Click the block that you would like to test." );
        }

        static void ZoneTestCallback( Player player, Vector3I[] marks, object tag ) {
            Zone[] allowed, denied;
            if( player.WorldMap.Zones.CheckDetailed( marks[0], player, out allowed, out denied ) ) {
                foreach( Zone zone in allowed ) {
                    SecurityCheckResult status = zone.Controller.CheckDetailed( player.Info );
                    player.Message( "> Zone {0}&S: {1}{2}", zone.ClassyName, Color.Lime, status );
                }
                foreach( Zone zone in denied ) {
                    SecurityCheckResult status = zone.Controller.CheckDetailed( player.Info );
                    player.Message( "> Zone {0}&S: {1}{2}", zone.ClassyName, Color.Red, status );
                }
            } else {
                player.Message( "No zones affect this block." );
            }
        }

        #endregion
        #region ZoneShow
        static readonly CommandDescriptor CdZoneShow = new CommandDescriptor
        {
            Name = "ZoneSelection",
            Aliases = new[] { "zselection", "zbox", "zshow", "zs" },
            Permissions = new[] { Permission.ManageZones },
            Category = CommandCategory.New,
            Help = "Lets you configure zone selections.",
            Usage = "/ZShow [Zone Name] [Color or On/Off] [Alpha] [On/Off]",
            Handler = zshowHandler
        };

        private static void zshowHandler(Player player, CommandReader cmd) {
            string zonea = cmd.Next();
            string color = cmd.Next();
            string alp = cmd.Next();
            string bol = cmd.Next();
            short alpha;
            Zone zone = player.World.Map.Zones.Find(zonea);
            if (zone == null) {
                player.Message("Error: Zone not found");
                return;
            }
            if (color == null) {
                player.Message("Error: Missing a Hex Color code");
                player.Message(CdZoneShow.Usage);
                return;
            } else {
                color = color.ToUpper();
            }
            if (color.StartsWith("#")) {
                color = color.ToUpper().Remove(0, 1);
            }
            if (!IsValidHex(color)) {
                if (color.ToLower().Equals("on") || color.ToLower().Equals("true") || color.ToLower().Equals("yes")) {
                    zone.ShowZone = true;
                    if (zone.Color != null) {
                        player.Message("Zone ({0}&s) will now show its bounderies", zone.ClassyName);
                        player.World.Players.Send(Packet.MakeRemoveSelection(zone.ZoneID));
                        player.World.Players.Send(Packet.MakeMakeSelection(zone.ZoneID, zone.Name, zone.Bounds,
                            zone.Color, zone.Alpha));
                    }
                    return;
                } else if (color.ToLower().Equals("off") || color.ToLower().Equals("false") || color.ToLower().Equals("no")) {
                    zone.ShowZone = false;
                    player.Message("Zone ({0}&s) will no longer show its bounderies", zone.ClassyName);
                    player.World.Players.Send(Packet.MakeRemoveSelection(zone.ZoneID));
                    return;
                } else {
                    player.Message("Error: \"#{0}\" is not a valid HEX color code.", color);
                    return;
                }
            } else {
                zone.Color = color.ToUpper();
            }

            if (alp == null) {
                player.Message("Error: Missing an Alpha integer");
                player.Message(CdZoneShow.Usage);
                return;
            }
            if (!short.TryParse(alp, out alpha)) {
                player.Message("Error: \"{0}\" is not a valid integer for Alpha.", alp);
                return;
            } else {
                zone.Alpha = alpha;
            }
            if (bol != null) {
                if (!bol.ToLower().Equals("on") && !bol.ToLower().Equals("off") && !bol.ToLower().Equals("true") &&
                    !bol.ToLower().Equals("false") && !bol.ToLower().Equals("0") && !bol.ToLower().Equals("1") &&
                    !bol.ToLower().Equals("yes") && !bol.ToLower().Equals("no")) {
                    zone.ShowZone = false;
                    player.Message("({0}) is not a valid bool statement", bol);
                } else if (bol.ToLower().Equals("on") || bol.ToLower().Equals("true") || bol.ToLower().Equals("1") ||
                           bol.ToLower().Equals("yes")) {
                    zone.ShowZone = true;
                    player.Message("Zone ({0}&s) color set! Bounderies: ON", zone.ClassyName);
                } else if (bol.ToLower().Equals("off") || bol.ToLower().Equals("false") || bol.ToLower().Equals("0") ||
                           bol.ToLower().Equals("no")) {
                    zone.ShowZone = false;
                    player.Message("Zone ({0}&s) color set! Bounderies: OFF", zone.ClassyName);
                }
            } else {
                if (zone.ShowZone == null) {
                    zone.ShowZone = false;
                }
                player.Message("Zone ({0}&s) color set!", zone.ClassyName);
            }
            if (zone != null) {
                foreach (Player p in player.World.Players) {
                    if (p.SupportsSelectionCuboid) {
                        if (zone.ShowZone == true) {

                            player.World.Players.Send(Packet.MakeRemoveSelection(zone.ZoneID));
                            p.Send(Packet.MakeMakeSelection(zone.ZoneID, zone.Name, zone.Bounds, zone.Color, alpha));
                        }
                    }
                }
            }
        }

        #endregion
        /// <summary> Ensures that the hex color has the correct length (1-6 characters)
        /// and character set (alphanumeric chars allowed). </summary>
        public static bool IsValidHex( string hex ) {
            if( hex == null ) throw new ArgumentNullException( "hex" );
            if (hex.StartsWith("#")) hex = hex.Remove(0, 1);
            if( hex.Length < 1 || hex.Length > 6 ) return false;
            for( int i = 0; i < hex.Length; i++ ) {
                char ch = hex[i];
                if( ch < '0' || ch > '9' && 
                    ch < 'A' || ch > 'F' && 
                    ch < 'a' || ch > 'f' ) {
                    return false;
                }
            }
            return true;
        }        
    }
}