// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | Copyright 2014 123DMWM <shmo1joe2@gmail.com>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using fCraft.MapConversion;
using JetBrains.Annotations;
using fCraft.Drawing;
using System.Text;
using System.Threading;
using fCraft.Events;
using System.Collections;
using ServiceStack.Text;

namespace fCraft {
    /// <summary> Contains commands related to world management. </summary>
    static class WorldCommands {
        const int WorldNamesPerPage = 30;

        internal static void Init() {
            CommandManager.RegisterCommand( CdBlockDB );
            CommandManager.RegisterCommand( CdBlockInfo );
            CommandManager.RegisterCommand( CdEnv );
            CdGenerate.Help = "Generates a new map. If no dimensions are given, uses current world's dimensions. " +
                              "If no file name is given, loads generated world into current world.\n" +
                              "Available themes: Grass, " + Enum.GetNames( typeof( MapGenTheme ) ).JoinToString() + '\n' +
                              "Available terrain types: Empty, Ocean, " + Enum.GetNames( typeof( MapGenTemplate ) ).JoinToString() + '\n' +
                              "Note: You do not need to specify a theme with \"Empty\" and \"Ocean\" templates.";
            CommandManager.RegisterCommand( CdGenerate );
            CommandManager.RegisterCommand( CdJoin );
            CommandManager.RegisterCommand( CdWorldLock );
            CommandManager.RegisterCommand( CdWorldUnlock );
            CommandManager.RegisterCommand( CdSpawn );
            CommandManager.RegisterCommand( CdWorlds );
            CommandManager.RegisterCommand( CdWorldAccess );
            CommandManager.RegisterCommand( CdWorldBuild );
            CommandManager.RegisterCommand( CdWorldFlush );
            CommandManager.RegisterCommand( CdWorldInfo );
            CommandManager.RegisterCommand( CdWorldLoad );
            CommandManager.RegisterCommand( CdWorldMain );
            CommandManager.RegisterCommand( CdWorldRename );
            CommandManager.RegisterCommand( CdWorldSave );
            CommandManager.RegisterCommand( CdWorldClearSave );
            CommandManager.RegisterCommand( CdWorldSet );
            CommandManager.RegisterCommand( CdWorldUnload );
            CommandManager.RegisterCommand( CdCTF );
            CommandManager.RegisterCommand( CdWorldClear );
            CommandManager.RegisterCommand( Cdclickdistance );
            CommandManager.RegisterCommand( CdEntity );
            CommandManager.RegisterCommand( Cdtex );
            CommandManager.RegisterCommand( CdSuicide );
            CommandManager.RegisterCommand( Cdweather );
            CommandManager.RegisterCommand( CdReJoin );
            CommandManager.RegisterCommand( CdSLE );
        }
        #region BlockDB

        static readonly CommandDescriptor CdBlockDB = new CommandDescriptor {
            Name = "BlockDB",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageBlockDB },
            Usage = "/BlockDB <WorldName> <Operation>",
            Help = "Manages BlockDB on a given world. " +
                   "Operations are: On, Off, Clear, Limit, TimeLimit, Preload. " +
                   "See &H/Help BlockDB <Operation>&S for operation-specific help. " +
                   "If no operation is given, world's BlockDB status is shown. " +
                   "If no WorldName is given, prints status of all worlds.",
            HelpSections = new Dictionary<string, string>{
                { "auto",       "/BlockDB <WorldName> Auto\n&S" +
                                "Allows BlockDB to decide whether it should be enabled or disabled based on each world's permissions (default)." },
                { "on",         "/BlockDB <WorldName> On\n&S" +
                                "Enables block tracking. Information will only be available for blocks that changed while BlockDB was enabled." },
                { "off",        "/BlockDB <WorldName> Off\n&S" +
                                "Disables block tracking. Block changes will NOT be recorded while BlockDB is disabled. " +
                                "Note that disabling BlockDB does not delete the existing data. Use &Hclear&S for that." },
                { "clear",      "/BlockDB <WorldName> Clear\n&S" +
                                "Clears all recorded data from the BlockDB. Erases all changes from memory and deletes the .fbdb file." },
                { "limit",      "/BlockDB <WorldName> Limit <#>|None\n&S" +
                                "Sets the limit on the maximum number of changes to store for a given world. " +
                                "Oldest changes will be deleted once the limit is reached. " +
                                "Put \"None\" to disable limiting. " +
                                "Unless a Limit or a TimeLimit it specified, all changes will be stored indefinitely." },
                { "timelimit",  "/BlockDB <WorldName> TimeLimit <Time>/None\n&S" +
                                "Sets the age limit for stored changes. " +
                                "Oldest changes will be deleted once the limit is reached. " +
                                "Use \"None\" to disable time limiting. " +
                                "Unless a Limit or a TimeLimit it specified, all changes will be stored indefinitely." },
                { "preload",    "/BlockDB <WorldName> Preload On/Off\n&S" +
                                "Enabled or disables preloading. When BlockDB is preloaded, all changes are stored in memory as well as in a file. " +
                                "This reduces CPU and disk use for busy maps, but may not be suitable for large maps due to increased memory use." },
            },
            Handler = BlockDBHandler
        };

        static void BlockDBHandler( Player player, CommandReader cmd ) {
            if( !BlockDB.IsEnabledGlobally ) {
                player.Message( "&WBlockDB is disabled on this server." );
                return;
            }

            string worldName = cmd.Next();
            if( worldName == null ) {
                int total = 0;
                World[] autoEnabledWorlds = WorldManager.Worlds.Where( w => (w.BlockDB.EnabledState == YesNoAuto.Auto) && w.BlockDB.IsEnabled ).ToArray();
                if( autoEnabledWorlds.Length > 0 ) {
                    total += autoEnabledWorlds.Length;
                    player.Message( "BlockDB is auto-enabled on: {0}",
                                    autoEnabledWorlds.JoinToClassyString() );
                }

                World[] manuallyEnabledWorlds = WorldManager.Worlds.Where( w => w.BlockDB.EnabledState == YesNoAuto.Yes ).ToArray();
                if( manuallyEnabledWorlds.Length > 0 ) {
                    total += manuallyEnabledWorlds.Length;
                    player.Message( "BlockDB is manually enabled on: {0}",
                                    manuallyEnabledWorlds.JoinToClassyString() );
                }

                World[] manuallyDisabledWorlds = WorldManager.Worlds.Where( w => w.BlockDB.EnabledState == YesNoAuto.No ).ToArray();
                if( manuallyDisabledWorlds.Length > 0 ) {
                    player.Message( "BlockDB is manually disabled on: {0}",
                                    manuallyDisabledWorlds.JoinToClassyString() );
                }

                if( total == 0 ) {
                    player.Message( "BlockDB is not enabled on any world." );
                }
                return;
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;
            BlockDB db = world.BlockDB;

            using( db.GetWriteLock() ) {
                string op = cmd.Next();
                if( op == null ) {
                    if( !db.IsEnabled ) {
                        if( db.EnabledState == YesNoAuto.Auto ) {
                            player.Message( "BlockDB is disabled (auto) on world {0}", world.ClassyName );
                        } else {
                            player.Message( "BlockDB is disabled on world {0}", world.ClassyName );
                        }
                    } else {
                        if( db.IsPreloaded ) {
                            if( db.EnabledState == YesNoAuto.Auto ) {
                                player.Message( "BlockDB is enabled (auto) and preloaded on world {0}", world.ClassyName );
                            } else {
                                player.Message( "BlockDB is enabled and preloaded on world {0}", world.ClassyName );
                            }
                        } else {
                            if( db.EnabledState == YesNoAuto.Auto ) {
                                player.Message( "BlockDB is enabled (auto) on world {0}", world.ClassyName );
                            } else {
                                player.Message( "BlockDB is enabled on world {0}", world.ClassyName );
                            }
                        }
                        player.Message( "    Change limit: {0}    Time limit: {1}",
                                        db.Limit == 0 ? "none" : db.Limit.ToStringInvariant(),
                                        db.TimeLimit == TimeSpan.Zero ? "none" : db.TimeLimit.ToMiniString() );
                    }
                    return;
                }

                switch( op.ToLower() ) {
                    case "on":
                        // enables BlockDB
                        if( db.EnabledState == YesNoAuto.Yes ) {
                            player.Message( "BlockDB is already manually enabled on world {0}", world.ClassyName );

                        } else if( db.EnabledState == YesNoAuto.Auto && db.IsEnabled ) {
                            db.EnabledState = YesNoAuto.Yes;
                            WorldManager.SaveWorldList();
                            player.Message( "BlockDB was auto-enabled, and is now manually enabled on world {0}", world.ClassyName );

                        } else {
                            Logger.Log( LogType.UserActivity,
                                        "BlockDB: {0} {1} &senabled BlockDB on world {2} (was {3})",
                                        player.Info.Rank.Name, player.Name, world.Name, db.EnabledState );
                            db.EnabledState = YesNoAuto.Yes;
                            WorldManager.SaveWorldList();
                            player.Message( "BlockDB is now manually enabled on world {0}", world.ClassyName );
                        }
                        break;

                    case "off":
                        // disables BlockDB
                        if( db.EnabledState == YesNoAuto.No ) {
                            player.Message( "BlockDB is already manually disabled on world {0}", world.ClassyName );

                        } else if( db.IsEnabled ) {
                            if( cmd.IsConfirmed ) {
                                db.EnabledState = YesNoAuto.No;
                                WorldManager.SaveWorldList();
                                player.Message( "BlockDB is now manually disabled on world {0}&S. Use &H/BlockDB {1} clear&S to delete all the data.",
                                                world.ClassyName, world.Name );
                            } else {
                                Logger.Log( LogType.UserActivity,
                                            "BlockDB: Asked {0} to confirm disabling BlockDB on world {1}",
                                            player.Name, world.Name );
                                player.Confirm( cmd,
                                                "Disable BlockDB on world {0}&S? Block changes will stop being recorded.",
                                                world.ClassyName );
                            }
                        } else {
                            Logger.Log( LogType.UserActivity,
                                        "BlockDB: {0} {1} &sdisabled BlockDB on world {2} (was {3})",
                                        player.Info.Rank.Name, player.Name, world.Name, db.EnabledState );
                            db.EnabledState = YesNoAuto.No;
                            WorldManager.SaveWorldList();
                            player.Message( "BlockDB was auto-disabled, and is now manually disabled on world {0}&S.",
                                            world.ClassyName );
                        }
                        break;

                    case "auto":
                        if( db.EnabledState == YesNoAuto.Auto ) {
                            player.Message( "BlockDB is already set to automatically enable/disable itself on world {0}", world.ClassyName );
                        } else {
                            Logger.Log( LogType.UserActivity,
                                        "BlockDB: {0} {1} set BlockDB state on world {2} to Auto (was {3})",
                                        player.Info.Rank.Name, player.Name, world.Name, db.EnabledState );
                            db.EnabledState = YesNoAuto.Auto;
                            WorldManager.SaveWorldList();
                            if( db.IsEnabled ) {
                                player.Message( "BlockDB is now auto-enabled on world {0}",
                                                world.ClassyName );
                            } else {
                                player.Message( "BlockDB is now auto-disabled on world {0}",
                                                world.ClassyName );
                            }
                        }
                        break;

                    case "limit":
                        // sets or resets limit on the number of changes to store
                        if( db.IsEnabled ) {
                            string limitString = cmd.Next();
                            int limitNumber;

                            if( limitString == null ) {
                                player.Message( "BlockDB: Limit for world {0}&S is {1}",
                                                world.ClassyName,
                                                ( db.Limit == 0 ? "none" : db.Limit.ToStringInvariant() ) );
                                return;
                            }

                            if( limitString.Equals( "none", StringComparison.OrdinalIgnoreCase ) ) {
                                limitNumber = 0;

                            } else if( !Int32.TryParse( limitString, out limitNumber ) ) {
                                CdBlockDB.PrintUsage( player );
                                return;

                            } else if( limitNumber < 0 ) {
                                player.Message( "BlockDB: Limit must be non-negative." );
                                return;
                            }

                            string limitDisplayString = ( limitNumber == 0 ? "none" : limitNumber.ToStringInvariant() );
                            if( db.Limit == limitNumber ) {
                                player.Message( "BlockDB: Limit for world {0}&S is already set to {1}",
                                               world.ClassyName, limitDisplayString );

                            } else if( !cmd.IsConfirmed && limitNumber != 0 ) {
                                Logger.Log( LogType.UserActivity,
                                            "BlockDB: Asked {0} to confirm changing BlockDB limit on world {1}",
                                            player.Name, world.Name );
                                player.Confirm( cmd, "BlockDB: Change limit? Some old data for world {0}&S may be discarded.", world.ClassyName );

                            } else {
                                db.Limit = limitNumber;
                                WorldManager.SaveWorldList();
                                player.Message( "BlockDB: Limit for world {0}&S set to {1}",
                                               world.ClassyName, limitDisplayString );
                            }

                        } else {
                            player.Message( "Block tracking is disabled on world {0}", world.ClassyName );
                        }
                        break;

                    case "timelimit":
                        // sets or resets limit on the age of changes to store
                        if( db.IsEnabled ) {
                            string limitString = cmd.Next();

                            if( limitString == null ) {
                                if( db.TimeLimit == TimeSpan.Zero ) {
                                    player.Message( "BlockDB: There is no time limit for world {0}",
                                                    world.ClassyName );
                                } else {
                                    player.Message( "BlockDB: Time limit for world {0}&S is {1}",
                                                    world.ClassyName, db.TimeLimit.ToMiniString() );
                                }
                                return;
                            }

                            TimeSpan limit;
                            if( limitString.Equals( "none", StringComparison.OrdinalIgnoreCase ) ) {
                                limit = TimeSpan.Zero;

                            } else if( !limitString.TryParseMiniTimespan( out limit ) ) {
                                CdBlockDB.PrintUsage( player );
                                return;
                            }
                            if( limit > DateTimeUtil.MaxTimeSpan ) {
                                player.MessageMaxTimeSpan();
                                return;
                            }

                            if( db.TimeLimit == limit ) {
                                if( db.TimeLimit == TimeSpan.Zero ) {
                                    player.Message( "BlockDB: There is already no time limit for world {0}",
                                                    world.ClassyName );
                                } else {
                                    player.Message( "BlockDB: Time limit for world {0}&S is already set to {1}",
                                                    world.ClassyName, db.TimeLimit.ToMiniString() );
                                }

                            } else if( !cmd.IsConfirmed && limit != TimeSpan.Zero ) {
                                Logger.Log( LogType.UserActivity,
                                            "BlockDB: Asked {0} to confirm changing BlockDB time limit on world {1}",
                                            player.Name, world.Name );
                                player.Confirm( cmd, "BlockDB: Change time limit? Some old data for world {0}&S may be discarded.", world.ClassyName );

                            } else {
                                db.TimeLimit = limit;
                                WorldManager.SaveWorldList();
                                if( db.TimeLimit == TimeSpan.Zero ) {
                                    player.Message( "BlockDB: Time limit removed for world {0}",
                                                    world.ClassyName );
                                } else {
                                    player.Message( "BlockDB: Time limit for world {0}&S set to {1}",
                                                    world.ClassyName, db.TimeLimit.ToMiniString() );
                                }
                            }

                        } else {
                            player.Message( "Block tracking is disabled on world {0}", world.ClassyName );
                        }
                        break;

                    case "clear":
                        // wipes BlockDB data
                        if (player.Can(Permission.ShutdownServer) == false)
                        {
                            player.Message("You must be {0}&s to clear the block DataBase", RankManager.GetMinRankWithAllPermissions(Permission.ShutdownServer).ClassyName);
                            return;
                        }
                        bool hasData = (db.IsEnabled || File.Exists( db.FileName ));
                        if( hasData ) {
                            if( cmd.IsConfirmed ) {
                                db.Clear();
                                Logger.Log( LogType.UserActivity,
                                            "BlockDB: {0} {1} cleared BlockDB data world {2}",
                                            player.Info.Rank.Name, player.Name, world.Name );
                                player.Message( "BlockDB: Cleared all data for {0}", world.ClassyName );
                            } else {
                                Logger.Log( LogType.UserActivity,
                                            "BlockDB: Asked {0} to confirm clearing BlockDB data world {1}",
                                            player.Name, world.Name );
                                player.Confirm( cmd, "Clear BlockDB data for world {0}&S? This cannot be undone.",
                                                world.ClassyName );
                            }
                        } else {
                            player.Message( "BlockDB: No data to clear for world {0}", world.ClassyName );
                        }
                        break;

                    case "preload":
                        // enables/disables BlockDB preloading
                        if( db.IsEnabled ) {
                            string param = cmd.Next();
                            if( param == null ) {
                                // shows current preload setting
                                player.Message( "BlockDB preloading is {0} for world {1}",
                                                (db.IsPreloaded ? "ON" : "OFF"),
                                                world.ClassyName );

                            } else if( param.Equals( "on", StringComparison.OrdinalIgnoreCase ) ) {
                                // turns preload on
                                if( db.IsPreloaded ) {
                                    player.Message( "BlockDB preloading is already enabled on world {0}", world.ClassyName );
                                } else {
                                    db.IsPreloaded = true;
                                    WorldManager.SaveWorldList();
                                    player.Message( "BlockDB preloading is now enabled on world {0}", world.ClassyName );
                                }

                            } else if( param.Equals( "off", StringComparison.OrdinalIgnoreCase ) ) {
                                // turns preload off
                                if( !db.IsPreloaded ) {
                                    player.Message( "BlockDB preloading is already disabled on world {0}", world.ClassyName );
                                } else {
                                    db.IsPreloaded = false;
                                    WorldManager.SaveWorldList();
                                    player.Message( "BlockDB preloading is now disabled on world {0}", world.ClassyName );
                                }

                            } else {
                                CdBlockDB.PrintUsage( player );
                            }
                        } else {
                            player.Message( "Block tracking is disabled on world {0}", world.ClassyName );
                        }
                        break;

                    default:
                        // unknown operand
                        CdBlockDB.PrintUsage( player );
                        return;
                }
            }
        }

        #endregion
        #region BlockInfo

        static readonly CommandDescriptor CdBlockInfo = new CommandDescriptor {
            Name = "BInfo",
            Category = CommandCategory.World,
            Aliases = new[] { "b", "bi", "whodid", "about" },
            Permissions = new[] { Permission.ViewOthersInfo },
            RepeatableSelection = true,
            Usage = "/BInfo [X Y Z]",
            Help = "Checks edit history for a given block.",
            Handler = BlockInfoHandler
        };

        static void BlockInfoHandler( Player player, CommandReader cmd ) {
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );

            // Make sure BlockDB is usable
            if( !BlockDB.IsEnabledGlobally ) {
                player.Message( "&WBlockDB is disabled on this server." );
                return;
            }
            if( !playerWorld.BlockDB.IsEnabled ) {
                player.Message( "&WBlockDB is disabled in this world." );
                return;
            }

            int x, y, z;
            if( cmd.NextInt( out x ) && cmd.NextInt( out y ) && cmd.NextInt( out z ) ) {
                // If block coordinates are given, run the BlockDB query right away
                if( cmd.HasNext ) {
                    CdBlockInfo.PrintUsage( player );
                    return;
                }
                Vector3I coords = new Vector3I( x, y, z );
                Map map = player.WorldMap;
                coords.X = Math.Min( map.Width - 1, Math.Max( 0, coords.X ) );
                coords.Y = Math.Min( map.Length - 1, Math.Max( 0, coords.Y ) );
                coords.Z = Math.Min( map.Height - 1, Math.Max( 0, coords.Z ) );
                BlockInfoSelectionCallback( player, new[] { coords }, null );

            } else {
                // Otherwise, start a selection
                player.Message( "BInfo: Click a block to look it up." );
                player.SelectionStart( 1, BlockInfoSelectionCallback, null, CdBlockInfo.Permissions );
            }
        }

        static void BlockInfoSelectionCallback( Player player, Vector3I[] marks, object tag ) {
            var args = new BlockInfoLookupArgs {
                Player = player,
                World = player.World,
                Coordinate = marks[0]
            };

            Scheduler.NewBackgroundTask( BlockInfoSchedulerCallback, args ).RunOnce();
        }


        sealed class BlockInfoLookupArgs {
            public Player Player;
            public World World;
            public Vector3I Coordinate;
        }

        const int MaxBlockChangesToList = 15;
        static void BlockInfoSchedulerCallback( SchedulerTask task ) {
            BlockInfoLookupArgs args = (BlockInfoLookupArgs)task.UserState;
            if( !args.World.BlockDB.IsEnabled ) {
                args.Player.Message( "&WBlockDB is disabled in this world." );
                return;
            }
            BlockDBEntry[] results = args.World.BlockDB.Lookup( MaxBlockChangesToList, args.Coordinate );
            if( results.Length > 0 ) {
                Array.Reverse( results );
                foreach( BlockDBEntry entry in results ) {
                    string date = DateTime.UtcNow.Subtract( DateTimeUtil.ToDateTime( entry.Timestamp ) ).ToMiniString();

                    PlayerInfo info = PlayerDB.FindPlayerInfoByID( entry.PlayerID );
                    string playerName;
                    if( info == null ) {
                        playerName = "?";
                    } else {
                        Player target = info.PlayerObject;
                        if( target != null && args.Player.CanSee( target ) ) {
                            playerName = info.ClassyName;
                        } else {
                            playerName = info.ClassyName + "&S (offline)";
                        }
                    }
                    string contextString;
                    if( entry.Context == BlockChangeContext.Manual ) {
                        contextString = "";
                    } else if( entry.Context == ( BlockChangeContext.Manual | BlockChangeContext.Replaced ) ) {
                        contextString = "(Painted)";
                    } else if( ( entry.Context & BlockChangeContext.Drawn ) == BlockChangeContext.Drawn &&
                               entry.Context != BlockChangeContext.Drawn ) {
                        if( entry.Context ==
                            ( BlockChangeContext.Drawn | BlockChangeContext.UndoneSelf | BlockChangeContext.Redone ) ) {
                            contextString = " (Redone)";
                        } else {
                            contextString = " (" + ( entry.Context & ~BlockChangeContext.Drawn ) + ")";
                        }
                    } else {
                        contextString = " (" + entry.Context + ")";
                    }

                    if( entry.OldBlock == (byte)Block.Air ) {
                        args.Player.Message( "&S  {0} ago: {1}&S placed {2}{3}",
                                             date, playerName, entry.NewBlock, contextString);
                    } else if( entry.NewBlock == (byte)Block.Air ) {
                        args.Player.Message("&S  {0} ago: {1}&S deleted {2}{3}",
                                             date, playerName, entry.OldBlock, contextString);
                    } else {
                        args.Player.Message("&S  {0} ago: {1}&S replaced {2} with {3}{4}",
                                             date, playerName, entry.OldBlock, entry.NewBlock, contextString);
                    }
                }
            } else {
                args.Player.Message( "BlockInfo: No results for {0}",
                                     args.Coordinate );
            }
        }

        #endregion
        #region Env

        static readonly CommandDescriptor CdEnv = new CommandDescriptor {
            Name = "Env",
            Category = CommandCategory.New,
            Permissions = new[] { Permission.ManageWorlds },
            Help = "Prints or changes the environmental variables for a given world. " +
                   "Variables are: clouds, fog, sky, level, edge. " +
                   "See &H/Help env <Variable>&S for details about each variable. " +
                   "Type &H/Env <WorldName> normal&S to reset everything for a world.",
            HelpSections = new Dictionary<string, string>{
                { "normal",     "&H/Env <WorldName> normal\n&S" +
                                "Resets all environment settings to their defaults for the given world." },
                { "clouds",     "&H/Env <WorldName> clouds <Color>\n&S" +
                                "Sets color of the clouds. Use \"normal\" instead of color to reset." },
                { "fog",        "&H/Env <WorldName> fog <Color>\n&S" +
                                "Sets color of the fog. Sky color blends with fog color in the distance. " +
                                "Use \"normal\" instead of color to reset." },
                { "shadow",     "&H/Env <WorldName> shadow <Color>\n&S" +
                                "Sets color of the shadowed areas. Use \"normal\" instead of color to reset." },
                { "sunlight",   "&H/Env <WorldName> sunlight <Color>\n&S" +
                                "Sets color of the lighted areas. Use \"normal\" instead of color to reset." },
                { "sky",        "&H/Env <WorldName> sky <Color>\n&S" +
                                "Sets color of the sky. Sky color blends with fog color in the distance. " +
                                "Use \"normal\" instead of color to reset." },
                { "level",      "&H/Env <WorldName> level <#>\n&S" +
                                "Sets height of the map edges/water level, in terms of blocks from the bottom of the map. " +
                                "Use \"normal\" instead of a number to reset to default (middle of the map)." },
                { "edge",       "&H/Env <WorldName> edge <BlockType>\n&S" +
                                "Changes the type of block that's visible beyond the map boundaries. "+
                                "Use \"normal\" instead of a number to reset to default (water)." },
                { "border",     "&H/Env <WorldName> border <BlockType>\n&S" +
                                "Changes the type of block that's visible on sides the map boundaries. "+
                                "Use \"normal\" instead of a number to reset to default (bedrock)." },
                { "texture",    "&H/Env <WorldName> texture <Texture .PNG Url>\n&S" +
                                "Changes the texture for all visible blocks on a map. "+
                                "Use \"normal\" instead of a number to reset to default (http://i.imgur.com/httEvfx.png)." }
            },
            Usage = "/Env <WorldName> <Variable>",
            IsConsoleSafe = true,
            Handler = EnvHandler
        };

        static void EnvHandler( Player player, CommandReader cmd ) {
            string worldName = cmd.Next();
            World world;
            if( worldName == null ) {
                world = player.World;
                if( world == null ) {
                    player.Message( "When used from console, /Env requires a world name." );
                    return;
                }
            } else {
                world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                if( world == null ) return;
            }
            if (world.EdgeLevel == -1) {
                world.EdgeLevel = (short) (world.map.Height/2);
            }

            string variable = cmd.Next();
            string valueText = cmd.Next();
            string maybe = cmd.Next();
            if( variable == null ) {
                player.Message( "Environment settings for world {0}&S:", world.ClassyName );
                player.Message( "  Cloud: {0}   Fog: {1}   Sky: {2}",
                                world.CloudColor == null ? "normal" : '#' + world.CloudColor,
                                world.FogColor == null ? "normal" : '#' + world.FogColor,
                                world.SkyColor == null ? "normal" : '#' + world.SkyColor);
                player.Message("  Shadow: {0}   Sunlight: {1}  Edge level: {2}",
                                world.ShadowColor == null ? "normal" : '#' + world.ShadowColor,
                                world.LightColor == null ? "normal" : '#' + world.LightColor,
                                world.EdgeLevel == -1 ? "normal" : world.EdgeLevel + " blocks");
                player.Message( "  Water block: {1}  Bedrock block: {0}",
                                world.EdgeBlock, world.HorizonBlock );
                player.Message("  Texture: {0}",
                                world.Texture == "http://i.imgur.com/httEvfx.png" ? "normal" : world.Texture);
                if( !player.IsUsingWoM ) {
                    player.Message( "  You need ClassiCube client to see the changes." );
                }
                return;
            }

            if( variable.Equals( "normal", StringComparison.OrdinalIgnoreCase ) ) {
                if( cmd.IsConfirmed ) {
                    world.FogColor = null;
                    world.CloudColor = null;
                    world.SkyColor = null;
                    world.ShadowColor = null;
                    world.LightColor = null; 
                    world.EdgeLevel = -1;
                    world.EdgeBlock = Block.Admincrete;
                    world.HorizonBlock = Block.Water;
                    world.Texture = "http://i.imgur.com/httEvfx.png";
                    Logger.Log( LogType.UserActivity,
                                "Env: {0} {1} reset environment settings for world {2}",
                                player.Info.Rank.Name, player.Name, world.Name );
                    player.Message( "Enviroment settings for world {0} &swere reset.", world.ClassyName );
                    WorldManager.SaveWorldList();
                } else {
                    Logger.Log( LogType.UserActivity,
                                "Env: Asked {0} to confirm resetting enviroment settings for world {1}",
                                player.Name, world.Name );
                    player.Confirm( cmd, "Reset enviroment settings for world {0}&S?", world.ClassyName );
                }
                return;
            }

            if( valueText == null ) {
                CdEnv.PrintUsage( player );
                return;
            }
            if (valueText.StartsWith("#"))
            {
                valueText = valueText.Remove(0, 1);
            }

            bool isValid = true;

            switch( variable.ToLower() ) {
                case "fog":
                    if (valueText.Equals("-1") || valueText.Equals("normal", StringComparison.OrdinalIgnoreCase) || valueText.Equals("reset", StringComparison.OrdinalIgnoreCase) || valueText.Equals("default", StringComparison.OrdinalIgnoreCase))
                    {
                        player.Message("Reset fog color for {0}&S to normal", world.ClassyName);
                        world.FogColor = null;
                    }
                    else
                    {
                        isValid = IsValidHex(valueText);
                        if (!isValid)
                        {
                            player.Message("Env: \"#{0}\" is not a valid HEX color code.", valueText);
                            return;
                        } else {
                            world.FogColor = valueText;
                            player.Message("Set fog color for {0}&S to #{1}", world.ClassyName, valueText);
                        }
                    }
                    if (player.SupportsEnvColors) {
                        player.Send(Packet.MakeEnvSetColor(2, world.FogColor));
                    }
                    break;

                case "cloud":
                case "clouds":
                    if (valueText.Equals("-1") || valueText.Equals("normal", StringComparison.OrdinalIgnoreCase) || valueText.Equals("reset", StringComparison.OrdinalIgnoreCase) || valueText.Equals("default", StringComparison.OrdinalIgnoreCase))
                    {
                        player.Message("Reset cloud color for {0}&S to normal", world.ClassyName);
                        world.CloudColor = null;
                    }
                    else
                    {
                        isValid = IsValidHex(valueText);
                        if (!isValid)
                        {
                            player.Message("Env: \"#{0}\" is not a valid HEX color code.", valueText);
                            return;
                        } else {
                            world.CloudColor = valueText;
                            player.Message("Set cloud color for {0}&S to #{1}", world.ClassyName, valueText);

                        }
                    }
                    if (player.SupportsEnvColors) {
                        player.Send(Packet.MakeEnvSetColor(1, world.CloudColor));
                    }
                    break;

                case "sky":
                    if (valueText.Equals("-1") || valueText.Equals("normal", StringComparison.OrdinalIgnoreCase) || valueText.Equals("reset", StringComparison.OrdinalIgnoreCase) || valueText.Equals("default", StringComparison.OrdinalIgnoreCase))
                    {
                        player.Message("Reset sky color for {0}&S to normal", world.ClassyName);
                        world.SkyColor = null;
                    }
                    else
                    {
                        isValid = IsValidHex(valueText);
                        if (!isValid)
                        {
                            player.Message("Env: \"#{0}\" is not a valid HEX color code.", valueText);
                            return;
                        } else {
                            world.SkyColor = valueText;
                            player.Message("Set sky color for {0}&S to #{1}", world.ClassyName, valueText);
                        }
                    }
                    if (player.SupportsEnvColors) {
                        player.Send(Packet.MakeEnvSetColor(0, world.SkyColor));
                    }
                    break;

                case "dark":
                case "shadow":
                    if (valueText.Equals("-1") || valueText.Equals("normal", StringComparison.OrdinalIgnoreCase) || valueText.Equals("reset", StringComparison.OrdinalIgnoreCase) || valueText.Equals("default", StringComparison.OrdinalIgnoreCase))
                    {
                        player.Message("Reset shadow color for {0}&S to normal", world.ClassyName);
                        world.ShadowColor = null;
                    }
                    else
                    {
                        isValid = IsValidHex(valueText);
                        if (!isValid)
                        {
                            player.Message("Env: \"#{0}\" is not a valid HEX color code.", valueText);
                            return;
                        } else {
                            world.ShadowColor = valueText;
                            player.Message("Set shadow color for {0}&S to #{1}", world.ClassyName, valueText);
                        }
                    }
                    if (player.SupportsEnvColors) {
                        player.Send(Packet.MakeEnvSetColor(3, world.ShadowColor));
                    }
                    break;

                case "sun":
                case "light":
                case "sunlight":
                    if (valueText.Equals("-1") || valueText.Equals("normal", StringComparison.OrdinalIgnoreCase) || valueText.Equals("reset", StringComparison.OrdinalIgnoreCase) || valueText.Equals("default", StringComparison.OrdinalIgnoreCase))
                    {
                        player.Message("Reset sunlight color for {0}&S to normal", world.ClassyName);
                        world.LightColor = null;
                    }
                    else
                    {
                        isValid = IsValidHex(valueText);
                        if (!isValid)
                        {
                            player.Message("Env: \"#{0}\" is not a valid HEX color code.", valueText);
                            return;
                        } else {
                            world.LightColor = valueText;
                            player.Message("Set sunlight color for {0}&S to #{1}", world.ClassyName, valueText);
                        }
                    }
                    if (player.SupportsEnvColors) {
                        player.Send(Packet.MakeEnvSetColor(4, world.LightColor));
                    }
                    break;

                case "level":
                    short level;
                    if (valueText.Equals("normal", StringComparison.OrdinalIgnoreCase) || valueText.Equals("reset", StringComparison.OrdinalIgnoreCase) || valueText.Equals("default", StringComparison.OrdinalIgnoreCase) || valueText.Equals("middle", StringComparison.OrdinalIgnoreCase) || valueText.Equals("center", StringComparison.OrdinalIgnoreCase))
                    {
                        player.Message("Reset water level for {0}&S to normal", world.ClassyName);
                        world.EdgeLevel = (short)(world.map.Height/2);
                    }
                    else
                    {
                        if (!short.TryParse(valueText, out level))
                        {
                            player.Message("Env: \"{0}\" is not a valid integer.", valueText);
                            return;
                        }
                        else
                        {
                            world.EdgeLevel = level;
                            player.Message("Set water level for {0}&S to {1}", world.ClassyName, level);
                        }
                    }
                    if (player.SupportsEnvColors) {
                        player.Send(Packet.MakeEnvSetMapAppearance(world.Texture, world.EdgeBlock, world.HorizonBlock, world.EdgeLevel));
                    }
                    break;

                case "horizon":
                case "edge":
                case "water":
                    Block block;
                    if( !Map.GetBlockByName( valueText, false, out block ) && !(valueText.Equals("normal", StringComparison.OrdinalIgnoreCase) || valueText.Equals("default", StringComparison.OrdinalIgnoreCase))) 
                    {
                        CdEnv.PrintUsage( player );
                        return;
                    }
                    if (block == Block.Water || valueText.Equals("normal", StringComparison.OrdinalIgnoreCase) || valueText.Equals("default", StringComparison.OrdinalIgnoreCase))
                    {
                        player.Message( "Reset water block for {0}&S to normal (Water)", world.ClassyName );
                        world.HorizonBlock = Block.Water;
                    } else {
                        if( block == Block.Air || block == Block.Sapling || block == Block.Glass || block == Block.YellowFlower || block == Block.RedFlower || block == Block.BrownMushroom || block == Block.RedMushroom || block == Block.Rope || block == Block.Fire  ) 
                        {
                            player.Message( "Env: Cannot use {0} for water textures.", block );
                            return;
                        } else {
                            world.HorizonBlock = block;
                            player.Message("Set water block for {0}&S to {1}", world.ClassyName, block);
                        }
                    }
                    if (player.SupportsEnvColors) {
                        player.Send(Packet.MakeEnvSetMapAppearance(world.Texture, world.EdgeBlock, world.HorizonBlock, world.EdgeLevel));
                    }
                    break;

                case "side":
                case "border":
                case "bedrock":
                    Block blockhorizon;
                    if (!Map.GetBlockByName(valueText, false, out blockhorizon) && !(valueText.Equals("normal", StringComparison.OrdinalIgnoreCase) || valueText.Equals("default", StringComparison.OrdinalIgnoreCase)))
                    {
                        CdEnv.PrintUsage(player);
                        return;
                    }
                    if (blockhorizon == Block.Admincrete || valueText.Equals("normal", StringComparison.OrdinalIgnoreCase) || valueText.Equals("default", StringComparison.OrdinalIgnoreCase))
                    {
                        player.Message("Reset bedrock block for {0}&S to normal (Bedrock)", world.ClassyName);
                        world.EdgeBlock = Block.Admincrete;
                    }
                    else
                    {
                        if (blockhorizon == Block.Air || blockhorizon == Block.Sapling || blockhorizon == Block.Glass || blockhorizon == Block.YellowFlower || blockhorizon == Block.RedFlower || blockhorizon == Block.BrownMushroom || blockhorizon == Block.RedMushroom || blockhorizon == Block.Rope || blockhorizon == Block.Fire)
                        {
                            player.Message("Env: Cannot use {0} for bedrock textures.", blockhorizon);
                            return;
                        }
                        else
                        {
                            world.EdgeBlock = blockhorizon;
                            player.Message("Set bedrock block for {0}&S to {1}", world.ClassyName, blockhorizon);
                        }
                    }
                    if (player.SupportsEnvColors) {
                        player.Send(Packet.MakeEnvSetMapAppearance(world.Texture, world.EdgeBlock, world.HorizonBlock, world.EdgeLevel));
                    }
                    break;

                case "tex":
                case "texture":
                    if (valueText == "http://i.imgur.com/httEvfx.png" || valueText == "normal")
                    {
                        player.Message("Reset texture for {0}&S to normal", world.ClassyName);
                        valueText = "http://i.imgur.com/httEvfx.png";
                    }
                    if (!valueText.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        player.Message("Env Texture: Invalid image type. Please use a \".png\" type image.", world.ClassyName);
                        return;
                    }
                    else if (!valueText.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                    {
                        player.Message("Env Texture: Invalid URL. Please use a \"http://\" type url.", world.ClassyName);
                        return;
                    }
                    else
                    {
                        player.Message("Set texture for {0}&S to {1}", world.ClassyName, valueText);
                    }
                    world.Texture = valueText;
                    if (player.SupportsEnvColors) {
                        player.Send(Packet.MakeEnvSetMapAppearance(world.Texture, world.EdgeBlock, world.HorizonBlock, world.EdgeLevel));
                    }
                    break;

                default:
                    CdEnv.PrintUsage( player );
                    return;
            }

            WorldManager.SaveWorldList();
            if (player.World == world) {
                player.Message("Env: Rejoin the world to see the changes.");
            }
        }

        /// <summary> Ensures that the hex color has the correct length (1-6 characters)
        /// and character set (alphanumeric chars allowed). </summary>
        public static bool IsValidHex( [NotNull] string hex ) {
            if( hex == null ) throw new ArgumentNullException( "hex" );
            if (hex.StartsWith("#")) hex = hex.Remove(0, 1);
            if( hex.Length < 1 || hex.Length > 6 ) return false;
            for( int i = 0; i < hex.Length; i++ ) {
                char ch = hex[i];
                if( ch < '0' || ch > '9' && 
                    ch < 'A' || ch > 'Z' && 
                    ch < 'a' || ch > 'z' ) {
                    return false;
                }
            }
            return true;
        }

        #endregion
        #region Gen

        static readonly CommandDescriptor CdGenerate = new CommandDescriptor {
            Name = "Gen",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/Gen Theme Template [Width Length Height] [FileName]",
            //Help is assigned by WorldCommands.Init
            Handler = GenHandler
        };

        static void GenHandler( Player player, CommandReader cmd ) {
            World playerWorld = player.World;
            string themeName = cmd.Next();
            bool genOcean = false;
            bool genEmpty = false;
            bool noTrees = false;

            if( themeName == null ) {
                CdGenerate.PrintUsage( player );
                return;
            }
            MapGenTheme theme = MapGenTheme.Forest;
            MapGenTemplate template = MapGenTemplate.Flat;

            // parse special template names (which do not need a theme)
            if( themeName.Equals( "ocean" ) ) {
                genOcean = true;

            } else if( themeName.Equals( "empty" ) ) {
                genEmpty = true;

            } else {
                string templateName = cmd.Next();
                if( templateName == null ) {
                    CdGenerate.PrintUsage( player );
                    return;
                }

                // parse theme
                bool swapThemeAndTemplate = false;
                if( themeName.Equals( "grass", StringComparison.OrdinalIgnoreCase ) ) {
                    theme = MapGenTheme.Forest;
                    noTrees = true;

                } else if( templateName.Equals( "grass", StringComparison.OrdinalIgnoreCase ) ) {
                    theme = MapGenTheme.Forest;
                    noTrees = true;
                    swapThemeAndTemplate = true;

                } else if( EnumUtil.TryParse( themeName, out theme, true ) ) {
                    noTrees = (theme != MapGenTheme.Forest);

                } else if( EnumUtil.TryParse( templateName, out theme, true ) ) {
                    noTrees = (theme != MapGenTheme.Forest);
                    swapThemeAndTemplate = true;

                } else {
                    player.Message( "Gen: Unrecognized theme \"{0}\". Available themes are: Grass, {1}",
                                    themeName,
                                    Enum.GetNames( typeof( MapGenTheme ) ).JoinToString() );
                    return;
                }

                // parse template
                if( swapThemeAndTemplate ) {
                    if( !EnumUtil.TryParse( themeName, out template, true ) ) {
                        player.Message( "Unrecognized template \"{0}\". Available terrain types: Empty, Ocean, {1}",
                                        themeName,
                                        Enum.GetNames( typeof( MapGenTemplate ) ).JoinToString() );
                        return;
                    }
                } else {
                    if( !EnumUtil.TryParse( templateName, out template, true ) ) {
                        player.Message( "Unrecognized template \"{0}\". Available terrain types: Empty, Ocean, {1}",
                                        templateName,
                                        Enum.GetNames( typeof( MapGenTemplate ) ).JoinToString() );
                        return;
                    }
                }
            }

            // parse map dimensions
            int mapWidth, mapLength, mapHeight;
            if( cmd.HasNext ) {
                int offset = cmd.Offset;
                if( !(cmd.NextInt( out mapWidth ) && cmd.NextInt( out mapLength ) && cmd.NextInt( out mapHeight )) ) {
                    if( playerWorld != null ) {
                        Map oldMap = player.WorldMap;
                        // If map dimensions were not given, use current map's dimensions
                        mapWidth = oldMap.Width;
                        mapLength = oldMap.Length;
                        mapHeight = oldMap.Height;
                    } else {
                        player.Message( "When used from console, /Gen requires map dimensions." );
                        CdGenerate.PrintUsage( player );
                        return;
                    }
                    cmd.Offset = offset;
                }
            } else if( playerWorld != null ) {
                Map oldMap = player.WorldMap;
                // If map dimensions were not given, use current map's dimensions
                mapWidth = oldMap.Width;
                mapLength = oldMap.Length;
                mapHeight = oldMap.Height;
            } else {
                player.Message( "When used from console, /Gen requires map dimensions." );
                CdGenerate.PrintUsage( player );
                return;
            }

            // Check map dimensions
            const string dimensionRecommendation = "Dimensions must be between 16 and 2047. " +
                                                   "Recommended values: 16, 32, 64, 128, 256, 512, and 1024.";
            if( !Map.IsValidDimension( mapWidth ) ) {
                player.Message( "Cannot make map with width {0}. {1}", mapWidth, dimensionRecommendation );
                return;
            } else if( !Map.IsValidDimension( mapLength ) ) {
                player.Message( "Cannot make map with length {0}. {1}", mapLength, dimensionRecommendation );
                return;
            } else if( !Map.IsValidDimension( mapHeight ) ) {
                player.Message( "Cannot make map with height {0}. {1}", mapHeight, dimensionRecommendation );
                return;
            }
            long volume = (long)mapWidth * mapLength * mapHeight;
            if( volume > Int32.MaxValue ) {
                player.Message( "Map volume may not exceed {0}", Int32.MaxValue );
                return;
            }

            if( !cmd.IsConfirmed && (!Map.IsRecommendedDimension( mapWidth ) || !Map.IsRecommendedDimension( mapLength ) || mapHeight % 16 != 0) ) {
                player.Message( "&WThe map will have non-standard dimensions. " +
                                "You may see glitched blocks or visual artifacts. " +
                                "The only recommended map dimensions are: 16, 32, 64, 128, 256, 512, and 1024." );
            }

            // figure out full template name
            bool genFlatgrass = (theme == MapGenTheme.Forest && noTrees && template == MapGenTemplate.Flat);
            string templateFullName;
            if( genEmpty ) {
                templateFullName = "Empty";
            } else if( genOcean ) {
                templateFullName = "Ocean";
            } else if( genFlatgrass ) {
                templateFullName = "Flatgrass";
            } else {
                if( theme == MapGenTheme.Forest && noTrees ) {
                    templateFullName = "Grass " + template;
                } else {
                    templateFullName = theme + " " + template;
                }
            }

            // check file/world name
            string fileName = cmd.Next();
            string fullFileName = null;
            if( fileName == null ) {
                // replacing current world
                if( playerWorld == null ) {
                    player.Message( "When used from console, /Gen requires FileName." );
                    CdGenerate.PrintUsage( player );
                    return;
                }
                if( !cmd.IsConfirmed ) {
                    Logger.Log( LogType.UserActivity,
                                "Gen: Asked {0} to confirm replacing the map of world {1} (\"this map\"). Request Denied because of Security Precautions In Place.",
                                player.Name, playerWorld.Name );
                    player.Confirm( cmd, "Replace THIS MAP with a generated one ({0})?", templateFullName );
                    return;
                }

            } else {
                if( cmd.HasNext ) {
                    CdGenerate.PrintUsage( player );
                    return;
                }
                // saving to file
                fileName = fileName.Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar );
                if( !fileName.EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase ) ) {
                    fileName += ".fcm";
                }
                if( !Paths.IsValidPath( fileName ) ) {
                    player.Message( "Invalid file name." );
                    return;
                }
                fullFileName = Path.Combine( Paths.MapPath, fileName );
                if( !Paths.Contains( Paths.MapPath, fullFileName ) ) {
                    player.MessageUnsafePath();
                    return;
                }
                string dirName = fullFileName.Substring( 0, fullFileName.LastIndexOf( Path.DirectorySeparatorChar ) );
                if( !Directory.Exists( dirName ) ) {
                    Directory.CreateDirectory( dirName );
                }
                if( !cmd.IsConfirmed && File.Exists( fullFileName ) ) {
                    Logger.Log( LogType.UserActivity,
                                "Gen: Asked {0} to confirm overwriting map file \"{1}\"",
                                player.Name, fileName );
                    player.Confirm( cmd, "The mapfile \"{0}\" already exists. Overwrite?", fileName );
                    return;
                }
            }

            // generate the map
            Map map;
            player.MessageNow( "Generating {0}...", templateFullName );

            if( genEmpty ) {
                map = MapGenerator.GenerateEmpty( mapWidth, mapLength, mapHeight );

            } else if( genOcean ) {
                map = MapGenerator.GenerateOcean( mapWidth, mapLength, mapHeight );

            } else if( genFlatgrass ) {
                map = MapGenerator.GenerateFlatgrass( mapWidth, mapLength, mapHeight );

            } else {
                MapGeneratorArgs args = MapGenerator.MakeTemplate( template );
                if( theme == MapGenTheme.Desert ) {
                    args.AddWater = false;
                }
                float ratio = mapHeight / (float)args.MapHeight;
                args.MapWidth = mapWidth;
                args.MapLength = mapLength;
                args.MapHeight = mapHeight;
                args.MaxHeight = (int)Math.Round( args.MaxHeight * ratio );
                args.MaxDepth = (int)Math.Round( args.MaxDepth * ratio );
                args.SnowAltitude = (int)Math.Round( args.SnowAltitude * ratio );
                args.Theme = theme;
                args.AddTrees = !noTrees;

                MapGenerator generator = new MapGenerator( args );
                map = generator.Generate();
            }

            Server.RequestGC();

            // save map to file, or load it into a world
            if( fileName != null ) {
                if( map.Save( fullFileName ) ) {
                    player.Message( "Generation done. Saved to {0}", fileName );
                } else {
                    player.Message( "&WAn error occurred while saving generated map to {0}", fileName );
                }
            } else {
                player.MessageNow( "Generation done. Changing map..." );
                playerWorld.MapChangedBy = player.Name;
                playerWorld.ChangeMap( map );
            }
        }

        #endregion       
        #region Join

        static readonly CommandDescriptor CdJoin = new CommandDescriptor {
            Name = "Join",
            Aliases = new[] {"j", "load", "goto", "map"},
            Category = CommandCategory.World,
            Usage = "/Join WorldName",
            Help =
                "Teleports the player to a specified world. You can see the list of available worlds by using &H/Worlds",
            Handler = JoinHandler
        };

        static void JoinHandler( [NotNull] Player player, [NotNull] CommandReader cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                CdJoin.PrintUsage( player );
                return;
            }

            if( worldName == "-" ) {
                if( player.LastUsedWorldName != null ) {
                    worldName = player.LastUsedWorldName;
                } else {
                    player.Message( "Cannot repeat world name: you haven't used any names yet." );
                    return;
                }
            }

            World[] worlds = WorldManager.FindWorlds( player, worldName );

            if( worlds.Length > 1 ) {
                player.MessageManyMatches( "world", worlds );
            } else if( worlds.Length == 1 ) {
                World world = worlds[0];
                player.LastUsedWorldName = world.Name;
                switch( world.AccessSecurity.CheckDetailed( player.Info ) ) {
                    case SecurityCheckResult.Allowed:
                    case SecurityCheckResult.WhiteListed:
                        if( world.IsFull ) {
                            player.Message( "Cannot join {0}&S: world is full.", world.ClassyName );
                            return;
                        }
                        if (cmd.IsConfirmed)
                        {
                            player.JoinWorldNow(world, true, WorldChangeReason.ManualJoin);
                            return;
                        }
                        if (player.World.Name.ToLower() == "tutorial" && player.Info.HasRTR == false)
                        {
                            player.Confirm(cmd, "&sYou are choosing to skip the rules, if you continue you will spawn here the next time you log in.");
                            return;
                        }
                        player.StopSpectating();
                        if( !player.JoinWorldNow( world, true, WorldChangeReason.ManualJoin ) ) {
                            player.Message( "ERROR: Failed to join world. See log for details." );
                        }
                        break;
                    case SecurityCheckResult.BlackListed:
                        player.Message( "Cannot join world {0}&S: you are blacklisted.",
                                        world.ClassyName );
                        break;
                    case SecurityCheckResult.RankTooLow:
                        player.Message( "Cannot join world {0}&S: must be {1}+",
                                        world.ClassyName,
                                        world.AccessSecurity.MinRank.ClassyName );
                        break;
                }
            } else {
                player.MessageNoWorld( worldName );
            }
        }

        #endregion
        #region WLock, WUnlock

        static readonly CommandDescriptor CdWorldLock = new CommandDescriptor {
            Name = "WLock",
            Aliases = new[] { "lock" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Lock },
            Usage = "/WLock [*|WorldName]",
            Help = "Puts the world into a locked, read-only mode. " +
                   "No one can place or delete blocks during lockdown. " +
                   "By default this locks the world you're on, but you can also lock any world by name. " +
                   "Put an asterisk (*) for world name to lock ALL worlds at once. " +
                   "Call &H/WUnlock&S to release lock on a world.",
            Handler = WorldLockHandler
        };

        static void WorldLockHandler( Player player, CommandReader cmd ) {
            string worldName = cmd.Next();

            World world;
            if( worldName != null ) {
                if( worldName == "*" ) {
                    int locked = 0;
                    World[] worldListCache = WorldManager.Worlds;
                    for( int i = 0; i < worldListCache.Length; i++ ) {
                        if( !worldListCache[i].IsLocked ) {
                            worldListCache[i].Lock( player );
                            locked++;
                        }
                    }
                    player.Message( "Locked {0} worlds.", locked );
                    return;
                } else {
                    world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                    if( world == null ) return;
                }

            } else if( player.World != null ) {
                world = player.World;

            } else {
                player.Message( "When called from console, /WLock requires a world name." );
                return;
            }

            if( !world.Lock( player ) ) {
                player.Message( "The world is already locked." );
            } else if( player.World != world ) {
                player.Message( "Locked world {0}", world );
            }
        }


        static readonly CommandDescriptor CdWorldUnlock = new CommandDescriptor {
            Name = "WUnlock",
            Aliases = new[] { "unlock" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Lock },
            Usage = "/WUnlock [*|WorldName]",
            Help = "Removes the lockdown set by &H/WLock&S. See &H/Help WLock&S for more information.",
            Handler = WorldUnlockHandler
        };

        static void WorldUnlockHandler( Player player, CommandReader cmd ) {
            string worldName = cmd.Next();

            World world;
            if( worldName != null ) {
                if( worldName == "*" ) {
                    World[] worldListCache = WorldManager.Worlds;
                    int unlocked = 0;
                    for( int i = 0; i < worldListCache.Length; i++ ) {
                        if( worldListCache[i].IsLocked ) {
                            worldListCache[i].Unlock( player );
                            unlocked++;
                        }
                    }
                    player.Message( "Unlocked {0} worlds.", unlocked );
                    return;
                } else {
                    world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                    if( world == null ) return;
                }

            } else if( player.World != null ) {
                world = player.World;

            } else {
                player.Message( "When called from console, /WUnlock requires a world name." );
                return;
            }

            if( !world.Unlock( player ) ) {
                player.Message( "The world is already unlocked." );
            } else if( player.World != world ) {
                player.Message( "Unlocked world {0}", world );
            }
        }

        #endregion
        #region Spawn

        static readonly CommandDescriptor CdSpawn = new CommandDescriptor {
            Name = "Spawn",
            Category = CommandCategory.World,
            Help = "Teleports you to the current map's spawn.",
            Handler = SpawnHandler
        };

        static void SpawnHandler( Player player, CommandReader cmd ) {
            if( player.World == null ) PlayerOpException.ThrowNoWorld( player );
            player.TeleportTo( player.World.LoadMap().Spawn );
        }

        #endregion
        #region Suicide
        
        static readonly CommandDescriptor CdSuicide = new CommandDescriptor
        {
            Name = "Suicide",
            Category = CommandCategory.New,
            Aliases = new[] { "Kill", "DoABackFlip", "ThanksObama", "ErraBodyDoTheFlop" },
            Help = "Tells the world of your sad ending. Now with the ability to add a note!",
            Usage = "/Suicide <Note>",
            Handler = SuicideHandler
        };

        static void SuicideHandler(Player player, CommandReader cmd)
        {
            string note = cmd.NextAll();
            if (player.World == null) PlayerOpException.ThrowNoWorld(player);
            if (player.Info.TimeSinceLastServerMessage.TotalSeconds < 10) {
                player.Info.getLeftOverTime(10, cmd);
                return;
            }
            if (note.Length > 64)
            {
                player.Message("&sProbably bad timing, but your suicide note can't be {0} characters long. Max is 64.", note.Length);
                return;
            }
            if (note == "")
            {
                Server.Message("&s{0}&s took the easy way out", player.ClassyName);
                player.TeleportTo(player.World.LoadMap().Spawn);
                player.Info.LastServerMessageDate = DateTime.Now;
                return;
            }
            else
            {
                Server.Message("&s{0}&s took the easy way out and left a note", player.ClassyName);
                Server.Message("&s[&fNote&s] {0}", note);
                player.TeleportTo(player.World.LoadMap().Spawn);
                player.Info.LastServerMessageDate = DateTime.Now;
                return;
            }
        }

        #endregion
        #region ClickDistance

        static readonly CommandDescriptor Cdclickdistance = new CommandDescriptor
        {
            Name = "ReachDistance",
            Aliases = new[] { "Reach", "rd" },
            Permissions = new[] { Permission.EditPlayerDB },
            Category = CommandCategory.New,
            Help = "Changes player reach distance. Every 32 is one block. Default: 160",
            Usage = "/reach [Player] [distance or reset]",
            Handler = ClickDistanceHandler
        };

        static void ClickDistanceHandler(Player player, CommandReader cmd)
        {
            short distance;
            string first = cmd.Next();
            string second = cmd.Next();
            if (first == null)
            {
                player.Message(Cdclickdistance.Usage);
                return;
            }
            PlayerInfo p = PlayerDB.FindPlayerInfoOrPrintMatches(player, first, SearchOptions.IncludeHidden);
            if (p == null)
            {
                return;
            }
            else
            {
                if (!short.TryParse(second, out distance))
                {
                    if (second != "reset")
                    {
                        player.Message("Please try something inbetween 0 and 32767");
                        return;
                    }
                    else
                    {
                        distance = 160;
                    }
                }
            }
            if (distance >= 32767 && distance <= 0)
            {
                player.Message("Please try something inbetween 0 and 32767");
                return;
                
            }
            if (distance != p.ReachDistance)
            {
                if (p != player.Info)
                {
                    if (p.IsOnline == true)
                    {
                        if (p.PlayerObject.SupportsClickDistance)
                        {
                            p.PlayerObject.Message("{0} set your reach distance from {0} to {1} blocks [Units: {2}]", player.Name, p.ReachDistance / 32, distance / 32, distance);
                            player.Message("Set reach distance for {0} from {1} to {2} blocks [Units: {3}]", p.Name, p.ReachDistance / 32, distance / 32, distance);
                            p.ReachDistance = distance;
                            p.PlayerObject.Send(Packet.MakeSetClickDistance(distance));
                        }
                        else
                        {
                            player.Message("This player does not support ReachDistance packet");
                        }
                    }
                    else
                    {
                        player.Message("Set reach distance for {0} from {1} to {2} blocks [Units: {3}]", p.Name, p.ReachDistance / 32, distance / 32, distance);
                        p.ReachDistance = distance;
                    }
                }
                else
                {
                    if (player.SupportsClickDistance)
                    {
                        player.Message("Set own reach distance from {0} to {1} blocks [Units: {2}]", player.Info.ReachDistance / 32, distance / 32, distance);
                        player.Info.ReachDistance = distance;
                        player.Send(Packet.MakeSetClickDistance(distance));
                    }
                    else
                    {
                        player.Message("You don't support ReachDistance packet");
                    }
                }
            }
            else
            {
                if (p != player.Info)
                {
                    player.Message("{0}'s reach distance is already set to {1}", p.ClassyName, p.ReachDistance);
                }
                else
                {
                    player.Message("Your reach distance is already set to {0}", p.ReachDistance);
                }
                return;
            }
        }

        #endregion
        #region AddEntity


        public static string[] validEntities = 
            {
                "chicken",
                "creeper",
                "croc",
                "humanoid",
                "humanoid.armor",
                "human",
                "pig",
                "printer",
                "sheep",
                "sheep.fur",
                "skeleton",
                "spider",
                "zombie"
            };
        static readonly CommandDescriptor CdEntity = new CommandDescriptor
        {
            Name = "Entity",
            Aliases = new[] { "AddEntity", "AddEnt", "Ent" },
            Permissions = new[] { Permission.BringAll },
            Category = CommandCategory.New,
            IsConsoleSafe = false,
            Usage = "/ent <create / remove / removeAll / model / list / bring>",
            Help = "Commands for manipulating entities. For help and usage for the individual options, use /help ent <option>.",
            HelpSections = new Dictionary<string, string>{
                { "create", "&H/Ent create <entity name> <model>\n&S" +
                                "Creates a new entity with the given name. Valid models are chicken, creeper, croc, human, pig, printer, sheep, skeleton, spider, zombie, or any block ID/Name." },
                { "remove", "&H/Ent remove <entity name>\n&S" +
                                "Removes the given entity." },
                { "removeall", "&H/Ent removeAll\n&S" +
                                "Removes all entities from the server."},  
                { "model", "&H/Ent model <entity name> <model>\n&S" +
                                "Changes the model of an entity to the given model. Valid models are chicken, creeper, croc, human, pig, printer, sheep, skeleton, spider, zombie, or any block ID/Name."},
                { "list", "&H/Ent list\n&S" +
                                "Prints out a list of all the entites on the server."},
                 { "bring", "&H/Ent bring <entity name>\n&S" +
                                "Brings the given entity to you."}
            },
            Handler = BotHandler,
        };

        private static void BotHandler(Player player, CommandReader cmd) {
            string option = cmd.Next();
            if (string.IsNullOrEmpty(option)) {
                CdEntity.PrintUsage(player);
                return;
            }

            if (option.ToLower() == "list") {
                player.Message("_Entities on {0}_", ConfigKey.ServerName.GetString());
                foreach (Bot botCheck in World.Bots) {
                    player.Message(botCheck.Name + " on " + botCheck.World.Name);
                }
                return;
            }
            if (option.ToLower() == "removeall") {
                tryagain:
                World.Bots.ForEach(b => b.removeBot(player));
                if (World.Bots.Count != 0) {
                    goto tryagain;
                }
                player.Message("All entities removed from the world.");
                return;
            }

            //finally away from the special cases
            string botName = cmd.Next();
            if (string.IsNullOrEmpty(botName)) {
                CdEntity.PrintUsage(player);
                return;
            }

            Bot bot = new Bot();
            if (option != "create" && option != "add") {
                bot = World.FindBot(botName.ToLower());
                if (bot == null) {
                    player.Message(
                        "Could not find {0}! Please make sure you spelled the entities name correctly. To view all the entities, type /ent list.",
                        botName);
                    return;
                }
            }
            Block blockmodel;

            switch (option.ToLower()) {
                case "create":
                case "add":
                    string requestedModel = "humanoid";
                    if (cmd.HasNext) {
                        requestedModel = cmd.Next().ToLower();
                    }
                    if (!validEntities.Contains(requestedModel)) {
                        if (Map.GetBlockByName(requestedModel, false, out blockmodel)) {
                            requestedModel = blockmodel.GetHashCode().ToString();
                        } else {
                            player.Message(
                                "That wasn't a valid entity model! Valid models are chicken, creeper, croc, human, pig, printer, sheep, skeleton, spider, zombie, or any block ID/Name.");
                            return;
                        }
                    }

                    //if a botname has already been chosen, ask player for a new name
                    var matchingNames = from b in World.Bots where b.Name.ToLower() == botName.ToLower() select b;

                    if (matchingNames.Count() > 0) {
                        player.Message("An entity with that name already exists! To view all entities, type /ent list.");
                        return;
                    }


                    Bot botCreate = new Bot();
                    botCreate.setBot(botName, player.World, player.Position, getNewID());
                    botCreate.createBot();
                    botCreate.changeBotModel(requestedModel);
                    player.Message("Successfully created entity {0}&s with id:{1}.", botCreate.Name, botCreate.ID);
                    break;
                case "remove":
                    player.Message("{0} was removed from the server.", bot.Name);
                    bot.removeBot(player);
                    break;
                case "model":
                    if (cmd.HasNext) {
                        string model = cmd.Next().ToLower();
                        if (string.IsNullOrEmpty(model)) {
                            player.Message(
                                "Usage is /Ent model <bot> <model>. Valid models are chicken, creeper, croc, human, pig, printer, sheep, skeleton, spider, zombie, or any block ID/Name.");
                            break;
                        }

                        if (model == "human") {
                            model = "humanoid";
                        }
                        if (!validEntities.Contains(model)) {
                            if (Map.GetBlockByName(model, false, out blockmodel)) {
                                model = blockmodel.GetHashCode().ToString();
                            } else {
                                player.Message(
                                    "That wasn't a valid entity model! Valid models are chicken, creeper, croc, human, pig, printer, sheep, skeleton, spider, zombie, or any block ID/Name.");
                                break;
                            }
                        }

                        player.Message("Changed entity model to {0}.", model);
                        bot.changeBotModel(model);
                    } else
                    player.Message(
                        "Usage is /Ent model <bot> <model>. Valid models are chicken, creeper, croc, human, pig, printer, sheep, skeleton, spider, zombie, or any block ID/Name.");
                    break;
                case "bring":
                    bot.teleportBot(player.Position);
                    break;
                default:
                    CdEntity.PrintUsage(player);
                    break;
            }
        }

        public static sbyte getNewID() {
            sbyte i = 1;
            go:
            foreach (Bot bot in World.Bots) {
                if (bot.ID == i) {
                    i++;
                    goto go;
                }
            }
            return i;
        }

        #endregion
        #region Texture

        static readonly CommandDescriptor Cdtex = new CommandDescriptor
        {
            Name = "texture",
            Aliases = new[] { "texturepack", "tex" },
            Permissions = new[] { Permission.Chat },
            Category = CommandCategory.New,
            Help = "Tells you information about our custom texture pack.",
            Handler = textureHandler
        };

        static void textureHandler(Player player, CommandReader cmd)
        {
            player.Message("If you havn't already noticed we have our own texture pack\n" + 
                           "but we are only allowed to force the terrain onto you.");
            player.Message("So if you want the full experience that this HD Default 64x pack has to offer...");
            player.Message("(Including beautiful Gui and Font)");
            player.Message( "ClassiCube texturepacks: http://173.48.22.66/texturepacks/" );
            player.Message( "Made and converted by 123DMWM^" );
        }

        #endregion
        #region Rejoin

        static readonly CommandDescriptor CdReJoin = new CommandDescriptor
        {
            Name = "rejoin",
            Aliases = new[] { "rj" },
            Permissions = new[] { Permission.Chat },
            Category = CommandCategory.New,
            Help = "Forces you to rejoin the world. Some commands require this if certain things change.",
            Handler = rejoinHandler
        };

        static void rejoinHandler(Player player, CommandReader cmd)
        {
            player.JoinWorld(player.World, WorldChangeReason.Rejoin, player.Position);
            return;
        }

        #endregion
        #region Block Hunt Map Settings

        /*static readonly CommandDescriptor CdGameSet = new CommandDescriptor
        {
            Name = "GameSettings",
            Aliases = new[] { "GameSet", "GSet", "GS" },
            Permissions = new[] { Permission.EditPlayerDB },
            Category = CommandCategory.New,
            Help = "&sAllows direct editing of game settings per world.\n " + 
                   "&sList of editable options: HiderSpawn, SeekerSpawn, Blocks.\n" + 
                   "&sFor detailed help see &h/Help GSet <Option>",
            HelpSections = new Dictionary<string, string>{
                { "hiderspawn",  "&H/GSet <WorldName> HiderSpawn <Action>\n" +
                                 "&SChanges the spawn for the hiders. Actions: Set, Reset, Display " },
                { "seekerspawn", "&H/GSet <WorldName> SeekerSpawn <Action>\n" +
                                 "&SChanges the spawn for the seeker. Actions: Set, Reset, Display" },
                { "gameblocks",  "&H/GSet <WorldName> GameBlocks <Action> <Block Name/ID>\n" +
                                 "&SChanges usable blocks in the game. Actions: Add, Remove, Reset, List" }
            },
            Usage = "/GSet <WorldName> <Option> <Action> <block>",
            Handler = GameSetHandler
        };

        static void GameSetHandler(Player player, CommandReader cmd)
        {
            string worldName = cmd.Next();
            World world;
            if (worldName == null)
            {
                world = player.World;
                if (world == null)
                {
                    player.Message("When used from console, /GSet requires a world name.");
                    return;
                }
            }
            else
            {
                world = WorldManager.FindWorldOrPrintMatches(player, worldName);
                if (world == null) return;
            }
            string option = cmd.Next();
            if (option == null)
            {
                player.Message("&sGame settings for world (&a{0}&s)", world.Name);
                player.Message("  &sHiders spawn: {0}", new Position(world.HiderPosX, world.HiderPosY, world.HiderPosZ).ToBlockCoords().ToString());
                player.Message("  &sSeeker spawn: {0}", new Position(world.SeekerPosX, world.SeekerPosY, world.SeekerPosZ).ToBlockCoords().ToString());
                player.Message("  &sGame Blocks: {0}", world.GameBlocks.JoinToString(", "));
                return;
            }
            string action = cmd.Next();
            if (action == null)
            {
                player.Message(CdGameSet.Usage);
                return;
            }
            string blocks = cmd.Next();
            if (option.ToLower().Equals("hiderspawn") || option.ToLower().Equals("hspawn") || option.ToLower().Equals("hider") || option.ToLower().Equals("hs"))
            {
                if (action.ToLower().Equals("set"))
                {
                    if (world == player.World)
                    {
                        world.HiderPosX = player.Position.X;
                        world.HiderPosY = player.Position.Y;
                        world.HiderPosZ = player.Position.Z;
                        player.Message("Hider Spawn for world (&a{0}&s) set to your location.", world.Name);
                        return;
                    }
                    else
                    {
                        player.Message("You must be in the world (&a{0}&s) to set the spawn.", world.Name);
                        return;
                    }
                }
                else if (action.ToLower().Equals("reset"))
                {
                    world.HiderPosX = world.map.Spawn.X;
                    world.HiderPosY = world.map.Spawn.Y;
                    world.HiderPosZ = world.map.Spawn.Z;
                    player.Message("Hider Spawn for world (&a{0}&s) has reset to world spawn.", world.Name);
                    return;
                }
                else if (action.ToLower().Equals("display"))
                {
                    player.Message("Hider Spawn for world (&a{0}&s) is: {1}", world.Name, new Position(world.HiderPosX, world.HiderPosY, world.HiderPosZ).ToBlockCoords().ToString());
                    return;
                }
                else
                {
                    player.Message(CdGameSet.Usage);
                    return;
                }
            }
            else if (option.ToLower().Equals("seekerspawn") || option.ToLower().Equals("sspawn") || option.ToLower().Equals("seeker") || option.ToLower().Equals("ss"))
            {
                if (action.ToLower().Equals("set"))
                {
                    if (world == player.World)
                    {
                        world.SeekerPosX = player.Position.X;
                        world.SeekerPosY = player.Position.Y;
                        world.SeekerPosZ = player.Position.Z;
                        player.Message("Seeker Spawn for world (&a{0}&s) set to your location.", world.Name);
                        return;
                    }
                    else
                    {
                        player.Message("You must be in the world (&a{0}&s) to set the spawn.", world.Name);
                        return;
                    }
                }
                else if (action.ToLower().Equals("reset"))
                {
                    world.SeekerPosX = world.map.Spawn.X;
                    world.SeekerPosY = world.map.Spawn.Y;
                    world.SeekerPosZ = world.map.Spawn.Z;
                    player.Message("Seeker Spawn for world (&a{0}&s) has reset to world spawn.", world.Name);
                    return;
                }
                else if (action.ToLower().Equals("display"))
                {
                    player.Message("Seeker Spawn for world (&a{0}&s) is: {1}", world.Name, new Position(world.SeekerPosX, world.SeekerPosY, world.SeekerPosZ).ToBlockCoords().ToString());
                    return;
                }
                else
                {
                    player.Message(CdGameSet.Usage);
                    return;
                }
            }
            else if (option.ToLower().Equals("gameblocks") || option.ToLower().Equals("gblocks") || option.ToLower().Equals("blocks") || option.ToLower().Equals("gb"))
            {
                if (action.ToLower().Equals("add"))
                {
                    Block gBlock;
                    if (Map.GetBlockByName(blocks, false, out gBlock))
                    {
                        if (world.GameBlocks.Contains(gBlock))
                        {
                            player.Message("World ({0}) already contains Block({1})", world.Name, gBlock);
                            return;
                        }
                        else
                        {
                            world.GameBlocks.Add(gBlock);
                            player.Message("Added Block({1}) to World ({0})", world.Name, gBlock);
                            return;
                        }
                    }
                    else
                    {
                        player.Message("({0}) is not a valid block.", blocks);
                    }
                }
                else if (action.ToLower().Equals("remove"))
                {
                    Block gBlock;
                    if (Map.GetBlockByName(blocks, false, out gBlock))
                    {
                        if (!world.GameBlocks.Contains(gBlock))
                        {
                            player.Message("World ({0}) not not contain Block({1})", world.Name, gBlock);
                            return;
                        }
                        else
                        {
                            world.GameBlocks.Remove(gBlock);
                            player.Message("Block ({0}) has been remove.", gBlock);
                            return;
                        }
                    }
                    else
                    {
                        player.Message("({0}) is not a valid block.", blocks);
                        return;
                    }
                }
                else if (action.ToLower().Equals("reset"))
                {
                    if (cmd.IsConfirmed)
                    {
                        world.GameBlocks.Clear();
                        player.Message("All game blocks for world ({0}) have been removed", world.Name);
                        return;
                    }
                    player.Confirm(cmd, "This will remove all game blocks for world ({0}). Are you sure?", world.Name);
                    return;
                }
                else if (action.ToLower().Equals("list") || action.ToLower().Equals("display"))
                {
                    player.Message("All game block id's for world({0})", world.Name);
                    player.Message(world.GameBlocks.JoinToString(", "));
                    return;
                }
                else
                {
                    player.Message(CdGameSet.Usage);
                    return;
                }
            }
            else
            {
                player.Message(CdGameSet.Usage);
                return;
            }
            return;
        }*/

        #endregion
        #region weather

        static readonly CommandDescriptor Cdweather = new CommandDescriptor
        {
            Name = "weather",
            Permissions = new[] { Permission.EditPlayerDB },
            Category = CommandCategory.New,
            Help = "Changes player weather ingame",
            Usage = "/weather [Player] [weather]",
            Handler = WeatherHandler
        };

        static void WeatherHandler(Player player, CommandReader cmd)
        {            
            string name = cmd.Next();
            PlayerInfo p = PlayerDB.FindPlayerInfoOrPrintMatches(player, name, SearchOptions.IncludeSelf);
            if (p == null)
            {
                return;
            }
            int weather;
            if (!cmd.NextInt(out weather))
            {
                weather = 0;
            }
            if (weather != 0 && weather != 1 && weather != 2)
            {
                player.Message("Please try something inbetween 0 and 2");
                return;

            }
            if (p != player.Info)
            {
                if (p.IsOnline == true)
                {
                    if (p.PlayerObject.SupportsEnvWeatherType)
                    {
                        p.PlayerObject.Message("{0} set your weather to {1}", player.Name, weather);
                        player.Message("Set weather for {0} to {1}", p.Name, weather);
                        p.PlayerObject.Send(Packet.SetWeather((byte)weather));
                    }
                    else
                    {
                        player.Message("That player does not support WeatherType packet");
                    }
                }
                else if (p.IsOnline == false || !player.CanSee(p.PlayerObject))
                {
                    player.Message("That player is not online!");
                }
            }
            else
            {
                if (player.SupportsEnvWeatherType)
                {
                    player.Message("Set weather to {0}", weather);
                    player.Send(Packet.SetWeather((byte)weather));
                }
                else
                {
                    player.Message("You don't support WeatherType packet");
                }
            }
        }

        #endregion  
        #region Worlds

        static readonly CommandDescriptor CdWorlds = new CommandDescriptor {
            Name = "Worlds",
            Category = CommandCategory.World | CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Aliases = new[] { "maps", "levels" },
            Usage = "/Worlds [all/hidden/populated/@Rank]",
            Help = "Shows a list of available worlds. To join a world, type &H/Join WorldName&S. " +
                   "If the optional \"all\" is added, also shows inaccessible or hidden worlds. " +
                   "If \"hidden\" is added, shows only inaccessible and hidden worlds. " +
                   "If \"populated\" is added, shows only worlds with players online. " +
                   "If a rank name is given, shows only worlds where players of that rank can build.",
            Handler = WorldsHandler
        };

        static void WorldsHandler( Player player, CommandReader cmd ) {
            string param = cmd.Next();
            World[] worlds;

            string listName;
            string extraParam;
            int offset = 0;

            if( param == null || Int32.TryParse( param, out offset ) ) {
                listName = "available worlds";
                extraParam = "";
                worlds = WorldManager.Worlds.Where( player.CanSee ).ToArray();

            } else {
                switch( Char.ToLower( param[0] ) ) {
                    case 'a':
                        listName = "worlds";
                        extraParam = "all ";
                        worlds = WorldManager.Worlds;
                        break;
                    case 'h':
                        listName = "hidden worlds";
                        extraParam = "hidden ";
                        worlds = WorldManager.Worlds.Where( w => !player.CanSee( w ) ).ToArray();
                        break;
                    case 'p':
                        listName = "populated worlds";
                        extraParam = "populated ";
                        worlds = WorldManager.Worlds.Where( w => w.Players.Any( player.CanSee ) ).ToArray();
                        break;
                    case '@':
                        if( param.Length == 1 ) {
                            CdWorlds.PrintUsage( player );
                            return;
                        }
                        string rankName = param.Substring( 1 );
                        Rank rank = RankManager.FindRank( rankName );
                        if( rank == null ) {
                            player.MessageNoRank( rankName );
                            return;
                        }
                        listName = String.Format( "worlds where {0}&S+ can build", rank.ClassyName );
                        extraParam = "@" + rank.Name + " ";
                        worlds = WorldManager.Worlds.Where( w => (w.BuildSecurity.MinRank <= rank) && player.CanSee( w ) )
                                                    .ToArray();
                        break;
                    default:
                        CdWorlds.PrintUsage( player );
                        return;
                }
                if( cmd.HasNext && !cmd.NextInt( out offset ) ) {
                    CdWorlds.PrintUsage( player );
                    return;
                }
            }

            if( worlds.Length == 0 ) {
                player.Message( "There are no {0}.", listName );

            } else if( worlds.Length <= WorldNamesPerPage || player.IsSuper ) {
                player.MessagePrefixed( "&S  ", "&SThere are {0} {1}: {2}",
                                        worlds.Length, listName, worlds.JoinToClassyString() );

            } else {
                if( offset >= worlds.Length ) {
                    offset = Math.Max( 0, worlds.Length - WorldNamesPerPage );
                }
                World[] worldsPart = worlds.Skip( offset ).Take( WorldNamesPerPage ).ToArray();
                player.MessagePrefixed( "&S   ", "&S{0}: {1}",
                                        listName.UppercaseFirst(), worldsPart.JoinToClassyString() );

                if( offset + worldsPart.Length < worlds.Length ) {
                    player.Message( "Showing {0}-{1} (out of {2}). Next: &H/Worlds {3}{1}",
                                    offset + 1, offset + worldsPart.Length, worlds.Length, extraParam );
                } else {
                    player.Message( "Showing worlds {0}-{1} (out of {2}).",
                                    offset + 1, offset + worldsPart.Length, worlds.Length );
                }
            }
        }

        #endregion
        #region WorldAccess

        static readonly CommandDescriptor CdWorldAccess = new CommandDescriptor {
            Name = "WAccess",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WAccess [WorldName [RankName]]",
            Help = "Shows access permission for player's current world. " +
                   "If optional WorldName parameter is given, shows access permission for another world. " +
                   "If RankName parameter is also given, sets access permission for specified world." +
                   "To include individuals, use \"+PlayerName\" in place of rank. To exclude, use \"-PlayerName\". " +
                   "To clear whitelist, use \"-*\". To clear blacklist use \"+*\"",
            Handler = WorldAccessHandler
        };

        static void WorldAccessHandler( [NotNull] Player player, CommandReader cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            string worldName = cmd.Next();

            // Print information about the current world
            if( worldName == null ) {
                if( player.World == null ) {
                    player.Message( "When calling /WAccess from console, you must specify a world name." );
                } else {
                    player.Message( player.World.AccessSecurity.GetDescription( player.World, "world", "accessed" ) );
                }
                return;
            }

            // Find a world by name
            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;

            // If no parameters were given, print info
            string nextToken = cmd.Next();
            if( nextToken == null ) {
                player.Message( world.AccessSecurity.GetDescription( world, "world", "accessed" ) );
                return;
            }

            // Deny adding access restrictions to main world(s)
            if( world == WorldManager.MainWorld ) {
                player.Message( "The main world cannot have access restrictions." );
                return;
            }

            bool changesWereMade = false;
            do {
                // Clear whitelist
                if( nextToken.Equals( "-*" ) ) {
                    PlayerInfo[] oldWhitelist = world.AccessSecurity.ExceptionList.Included.ToArray();
                    world.AccessSecurity.ResetIncludedList();
                    player.Message( "Access whitelist of {0}&S cleared: {1}",
                                    world.ClassyName, oldWhitelist.JoinToClassyString() );
                    Logger.Log( LogType.UserActivity,
                                "{0} {1} &scleared access whitelist of world {2}: {3}",
                                player.Info.Rank.Name, player.Name, world.Name, oldWhitelist.JoinToString( pi => pi.Name ) );
                    continue;
                }

                // Clear blacklist
                if( nextToken.Equals( "+*" ) ) {
                    PlayerInfo[] oldBlacklist = world.AccessSecurity.ExceptionList.Excluded.ToArray();
                    world.AccessSecurity.ResetExcludedList();
                    player.Message( "Access blacklist of {0}&S cleared: {1}",
                                    world.ClassyName, oldBlacklist.JoinToClassyString() );
                    Logger.Log( LogType.UserActivity,
                                "{0} {1} &scleared access blacklist of world {2}: {3}",
                                player.Info.Rank.Name, player.Name, world.Name, oldBlacklist.JoinToString( pi => pi.Name ) );
                    continue;
                }

                // Whitelisting individuals
                if( nextToken.StartsWith( "+" ) ) {
                    PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches( player, nextToken.Substring( 1 ), SearchOptions.Default );
                    if( info == null ) return;

                    // prevent players from whitelisting themselves to bypass protection
                    if (player.Info == info && !player.Info.Rank.AllowSecurityCircumvention)
                    {
                        switch( world.AccessSecurity.CheckDetailed( player.Info ) ) {
                            case SecurityCheckResult.RankTooLow:
                                player.Message( "&WYou must be {0}&W+ to add yourself to the access whitelist of {1}",
                                                world.AccessSecurity.MinRank.ClassyName,
                                                world.ClassyName );
                                continue;
                            case SecurityCheckResult.BlackListed:
                                player.Message( "&WYou cannot remove yourself from the access blacklist of {0}",
                                                world.ClassyName );
                                continue;
                        }
                    }

                    if( world.AccessSecurity.CheckDetailed( info ) == SecurityCheckResult.Allowed ) {
                        player.Message( "{0}&S is already allowed to access {1}&S (by rank)",
                                        info.ClassyName, world.ClassyName );
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if( target == player ) target = null; // to avoid duplicate messages

                    switch( world.AccessSecurity.Include( info ) ) {
                        case PermissionOverride.Deny:
                            if( world.AccessSecurity.Check( info ) ) {
                                player.Message( "{0}&S is no longer barred from accessing {1}",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "You can now access world {0}&S (removed from blacklist by {1}&S).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            } else {
                                player.Message( "{0}&S was removed from the access blacklist of {1}&S. " +
                                                "Player is still NOT allowed to join (by rank).",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "You were removed from the access blacklist of world {0}&S by {1}&S. " +
                                                    "You are still NOT allowed to join (by rank).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            }
                            Logger.Log( LogType.UserActivity,
                                        "{0} removed {1} from the access blacklist of {2}",
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.None:
                            player.Message( "{0}&S is now allowed to access {1}",
                                            info.ClassyName, world.ClassyName );
                            if( target != null ) {
                                target.Message( "You can now access world {0}&S (whitelisted by {1}&S).",
                                                world.ClassyName, player.ClassyName );
                            }
                            Logger.Log( LogType.UserActivity,
                                        "{0} added {1} to the access whitelist on world {2}",
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            player.Message( "{0}&S is already on the access whitelist of {1}",
                                            info.ClassyName, world.ClassyName );
                            break;
                    }

                    // Blacklisting individuals
                } else if( nextToken.StartsWith( "-" ) ) {
                    PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches(player, nextToken.Substring(1), SearchOptions.Default);
                    if( info == null ) return;

                    if( world.AccessSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooLow ) {
                        player.Message( "{0}&S is already barred from accessing {1}&S (by rank)",
                                        info.ClassyName, world.ClassyName );
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if( target == player ) target = null; // to avoid duplicate messages

                    switch( world.AccessSecurity.Exclude( info ) ) {
                        case PermissionOverride.Deny:
                            player.Message( "{0}&S is already on access blacklist of {1}",
                                            info.ClassyName, world.ClassyName );
                            break;

                        case PermissionOverride.None:
                            player.Message( "{0}&S is now barred from accessing {1}",
                                            info.ClassyName, world.ClassyName );
                            if( target != null ) {
                                target.Message( "&WYou were barred by {0}&W from accessing world {1}",
                                                player.ClassyName, world.ClassyName );
                            }
                            Logger.Log( LogType.UserActivity,
                                        "{0} added {1} to the access blacklist on world {2}",
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            if( world.AccessSecurity.Check( info ) ) {
                                player.Message( "{0}&S is no longer on the access whitelist of {1}&S. " +
                                                "Player is still allowed to join (by rank).",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "You were removed from the access whitelist of world {0}&S by {1}&S. " +
                                                    "You are still allowed to join (by rank).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            } else {
                                player.Message( "{0}&S is no longer allowed to access {1}",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "&WYou can no longer access world {0}&W (removed from whitelist by {1}&W).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            }
                            Logger.Log( LogType.UserActivity,
                                        "{0} removed {1} from the access whitelist on world {2}",
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;
                    }

                    // Setting minimum rank
                } else {
                    Rank rank = RankManager.FindRank( nextToken );
                    if( rank == null ) {
                        player.MessageNoRank( nextToken );

                    } else if( !player.Info.Rank.AllowSecurityCircumvention &&
                               world.AccessSecurity.MinRank > rank &&
                               world.AccessSecurity.MinRank > player.Info.Rank ) {
                        player.Message( "&WYou must be ranked {0}&W+ to lower the access rank for world {1}",
                                        world.AccessSecurity.MinRank.ClassyName, world.ClassyName );

                    } else {
                        // list players who are redundantly blacklisted
                        var exceptionList = world.AccessSecurity.ExceptionList;
                        PlayerInfo[] noLongerExcluded = exceptionList.Excluded.Where( excludedPlayer => excludedPlayer.Rank < rank ).ToArray();
                        if( noLongerExcluded.Length > 0 ) {
                            player.Message( "Following players no longer need to be blacklisted to be barred from {0}&S: {1}",
                                            world.ClassyName,
                                            noLongerExcluded.JoinToClassyString() );
                        }

                        // list players who are redundantly whitelisted
                        PlayerInfo[] noLongerIncluded = exceptionList.Included.Where( includedPlayer => includedPlayer.Rank >= rank ).ToArray();
                        if( noLongerIncluded.Length > 0 ) {
                            player.Message( "Following players no longer need to be whitelisted to access {0}&S: {1}",
                                            world.ClassyName,
                                            noLongerIncluded.JoinToClassyString() );
                        }

                        // apply changes
                        world.AccessSecurity.MinRank = rank;
                        changesWereMade = true;
                        if( world.AccessSecurity.MinRank == RankManager.LowestRank ) {
                            Server.Message( "{0}&S made the world {1}&S accessible to everyone.",
                                              player.ClassyName, world.ClassyName );
                        } else {
                            Server.Message( "{0}&S made the world {1}&S accessible only by {2}+",
                                              player.ClassyName, world.ClassyName,
                                              world.AccessSecurity.MinRank.ClassyName );
                        }
                        Logger.Log( LogType.UserActivity,
                                    "{0} set access rank for world {1} to {2}+",
                                    player.Name, world.Name, world.AccessSecurity.MinRank.Name );
                    }
                }
            } while( (nextToken = cmd.Next()) != null );

            if( changesWereMade ) {
                var playersWhoCantStay = world.Players.Where( p => !p.CanJoin( world ) );
                foreach( Player p in playersWhoCantStay ) {
                    p.Message( "&WYou are no longer allowed to join world {0}", world.ClassyName );
                    p.JoinWorld( WorldManager.FindMainWorld( p ), WorldChangeReason.PermissionChanged );
                }
                WorldManager.SaveWorldList();
            }
        }

        #endregion
        #region WorldBuild

        static readonly CommandDescriptor CdWorldBuild = new CommandDescriptor {
            Name = "WBuild",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WBuild [WorldName [RankName]]",
            Help = "Shows build permissions for player's current world. " +
                   "If optional WorldName parameter is given, shows build permission for another world. " +
                   "If RankName parameter is also given, sets build permission for specified world. " +
                   "To include individuals, use \"+PlayerName\" in place of rank. To exclude, use \"-PlayerName\". " +
                   "To clear whitelist, use \"-*\". To clear blacklist use \"+*\"",
            Handler = WorldBuildHandler
        };

        static void WorldBuildHandler( [NotNull] Player player, CommandReader cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            string worldName = cmd.Next();

            // Print information about the current world
            if( worldName == null ) {
                if( player.World == null ) {
                    player.Message( "When calling /WBuild from console, you must specify a world name." );
                } else {
                    player.Message( player.World.BuildSecurity.GetDescription( player.World, "world", "modified" ) );
                }
                return;
            }

            // Find a world by name
            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;

            // If no parameters were given, print info
            string nextToken = cmd.Next();
            if( nextToken == null ) {
                player.Message( world.BuildSecurity.GetDescription( world, "world", "modified" ) );
                return;
            }

            bool changesWereMade = false;
            do {
                // Clear whitelist
                if( nextToken.Equals( "-*" ) ) {
                    PlayerInfo[] oldWhitelist = world.BuildSecurity.ExceptionList.Included.ToArray();
                    if( oldWhitelist.Length > 0 ) {
                        world.BuildSecurity.ResetIncludedList();
                        player.Message( "Build whitelist of world {0}&S cleared: {1}",
                                        world.ClassyName, oldWhitelist.JoinToClassyString() );
                        Logger.Log( LogType.UserActivity,
                                    "{0} {1} &scleared build whitelist of world {2}: {3}",
                                    player.Info.Rank.Name, player.Name, world.Name, oldWhitelist.JoinToString( pi => pi.Name ) );
                    } else {
                        player.Message( "Build whitelist of world {0}&S is empty.",
                                        world.ClassyName );
                    }
                    continue;
                }

                // Clear blacklist
                if( nextToken.Equals( "+*" ) ) {
                    PlayerInfo[] oldBlacklist = world.BuildSecurity.ExceptionList.Excluded.ToArray();
                    if( oldBlacklist.Length > 0 ) {
                        world.BuildSecurity.ResetExcludedList();
                        player.Message( "Build blacklist of world {0}&S cleared: {1}",
                                        world.ClassyName, oldBlacklist.JoinToClassyString() );
                        Logger.Log( LogType.UserActivity,
                                    "{0} {1} &scleared build blacklist of world {2}: {3}",
                                    player.Info.Rank.Name, player.Name, world.Name, oldBlacklist.JoinToString( pi => pi.Name ) );
                    } else {
                        player.Message( "Build blacklist of world {0}&S is empty.",
                                        world.ClassyName );
                    }
                    continue;
                }

                // Whitelisting individuals
                if( nextToken.StartsWith( "+" ) ) {
                    PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches(player, nextToken.Substring(1), SearchOptions.Default);
                    if( info == null ) return;

                    // prevent players from whitelisting themselves to bypass protection
                    if (player.Info == info && !player.Info.Rank.AllowSecurityCircumvention)
                    {
                        switch( world.BuildSecurity.CheckDetailed( player.Info ) ) {
                            case SecurityCheckResult.RankTooLow:
                                player.Message( "&WYou must be {0}&W+ to add yourself to the build whitelist of {1}",
                                                world.BuildSecurity.MinRank.ClassyName,
                                                world.ClassyName );
                                continue;
                            case SecurityCheckResult.BlackListed:
                                player.Message( "&WYou cannot remove yourself from the build blacklist of {0}",
                                                world.ClassyName );
                                continue;
                        }
                    }

                    if( world.BuildSecurity.CheckDetailed( info ) == SecurityCheckResult.Allowed ) {
                        player.Message( "{0}&S is already allowed to build in {1}&S (by rank)",
                                        info.ClassyName, world.ClassyName );
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if( target == player ) target = null; // to avoid duplicate messages

                    switch( world.BuildSecurity.Include( info ) ) {
                        case PermissionOverride.Deny:
                            if( world.BuildSecurity.Check( info ) ) {
                                player.Message( "{0}&S is no longer barred from building in {1}",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "You can now build in world {0}&S (removed from blacklist by {1}&S).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            } else {
                                player.Message( "{0}&S was removed from the build blacklist of {1}&S. " +
                                                "Player is still NOT allowed to build (by rank).",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "You were removed from the build blacklist of world {0}&S by {1}&S. " +
                                                    "You are still NOT allowed to build (by rank).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            }
                            Logger.Log( LogType.UserActivity,
                                        "{0} removed {1} from the build blacklist of {2}",
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.None:
                            player.Message( "{0}&S is now allowed to build in {1}",
                                            info.ClassyName, world.ClassyName );
                            if( target != null ) {
                                target.Message( "You can now build in world {0}&S (whitelisted by {1}&S).",
                                                world.ClassyName, player.ClassyName );
                            }
                            Logger.Log( LogType.UserActivity,
                                        "{0} added {1} to the build whitelist on world {2}",
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            player.Message( "{0}&S is already on the build whitelist of {1}",
                                            info.ClassyName, world.ClassyName );
                            break;
                    }

                    // Blacklisting individuals
                } else if( nextToken.StartsWith( "-" ) ) {
                    PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches(player, nextToken.Substring(1), SearchOptions.Default);
                    if( info == null ) return;

                    if( world.BuildSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooLow ) {
                        player.Message( "{0}&S is already barred from building in {1}&S (by rank)",
                                        info.ClassyName, world.ClassyName );
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if( target == player ) target = null; // to avoid duplicate messages

                    switch( world.BuildSecurity.Exclude( info ) ) {
                        case PermissionOverride.Deny:
                            player.Message( "{0}&S is already on build blacklist of {1}",
                                            info.ClassyName, world.ClassyName );
                            break;

                        case PermissionOverride.None:
                            player.Message( "{0}&S is now barred from building in {1}",
                                            info.ClassyName, world.ClassyName );
                            if( target != null ) {
                                target.Message( "&WYou were barred by {0}&W from building in world {1}",
                                                player.ClassyName, world.ClassyName );
                            }
                            Logger.Log( LogType.UserActivity,
                                        "{0} added {1} to the build blacklist on world {2}",
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            if( world.BuildSecurity.Check( info ) ) {
                                player.Message( "{0}&S is no longer on the build whitelist of {1}&S. " +
                                                "Player is still allowed to build (by rank).",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "You were removed from the build whitelist of world {0}&S by {1}&S. " +
                                                    "You are still allowed to build (by rank).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            } else {
                                player.Message( "{0}&S is no longer allowed to build in {1}",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "&WYou can no longer build in world {0}&W (removed from whitelist by {1}&W).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            }
                            Logger.Log( LogType.UserActivity,
                                        "{0} removed {1} from the build whitelist on world {2}",
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;
                    }

                    // Setting minimum rank
                } else {
                    Rank rank = RankManager.FindRank( nextToken );
                    if( rank == null ) {
                        player.MessageNoRank( nextToken );
                    } else if( !player.Info.Rank.AllowSecurityCircumvention &&
                               world.BuildSecurity.MinRank > rank &&
                               world.BuildSecurity.MinRank > player.Info.Rank ) {
                        player.Message( "&WYou must be ranked {0}&W+ to lower build restrictions for world {1}",
                                        world.BuildSecurity.MinRank.ClassyName, world.ClassyName );
                    } else {
                        // list players who are redundantly blacklisted
                        var exceptionList = world.BuildSecurity.ExceptionList;
                        PlayerInfo[] noLongerExcluded = exceptionList.Excluded.Where( excludedPlayer => excludedPlayer.Rank < rank ).ToArray();
                        if( noLongerExcluded.Length > 0 ) {
                            player.Message( "Following players no longer need to be blacklisted on world {0}&S: {1}",
                                            world.ClassyName,
                                            noLongerExcluded.JoinToClassyString() );
                        }

                        // list players who are redundantly whitelisted
                        PlayerInfo[] noLongerIncluded = exceptionList.Included.Where( includedPlayer => includedPlayer.Rank >= rank ).ToArray();
                        if( noLongerIncluded.Length > 0 ) {
                            player.Message( "Following players no longer need to be whitelisted on world {0}&S: {1}",
                                            world.ClassyName,
                                            noLongerIncluded.JoinToClassyString() );
                        }

                        // apply changes
                        world.BuildSecurity.MinRank = rank;
                        if( BlockDB.IsEnabledGlobally && world.BlockDB.AutoToggleIfNeeded() ) {
                            if( world.BlockDB.IsEnabled ) {
                                player.Message( "BlockDB is now auto-enabled on world {0}",
                                                world.ClassyName );
                            } else {
                                player.Message( "BlockDB is now auto-disabled on world {0}",
                                                world.ClassyName );
                            }
                        }
                        changesWereMade = true;
                        if( world.BuildSecurity.MinRank == RankManager.LowestRank ) {
                            Server.Message( "{0}&S allowed anyone to build on world {1}",
                                              player.ClassyName, world.ClassyName );
                        } else {
                            Server.Message( "{0}&S allowed only {1}+&S to build in world {2}",
                                              player.ClassyName, world.BuildSecurity.MinRank.ClassyName, world.ClassyName );
                        }
                        Logger.Log( LogType.UserActivity,
                                    "{0} set build rank for world {1} to {2}+",
                                    player.Name, world.Name, world.BuildSecurity.MinRank.Name );
                    }
                }
            } while( (nextToken = cmd.Next()) != null );

            if( changesWereMade ) {
                WorldManager.SaveWorldList();
            }
        }

        #endregion
        #region WorldFlush

        static readonly CommandDescriptor CdWorldFlush = new CommandDescriptor {
            Name = "WFlush",
            Aliases = new[] { "Flush" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.FlushWorlds },
            Usage = "/WFlush [WorldName]",
            Help = "Flushes the update buffer on specified map by causing players to rejoin. " +
                   "Makes cuboids and other draw commands finish REALLY fast.",
            Handler = WorldFlushHandler
        };

        static void WorldFlushHandler( Player player, CommandReader cmd ) {
            string worldName = cmd.Next();
            World world = player.World;

            if( worldName != null ) {
                world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                if( world == null ) return;

            } else if( world == null ) {
                player.Message( "When using /WFlush from console, you must specify a world name." );
                return;
            }

            Map map = world.Map;
            if( map == null ) {
                player.MessageNow( "WFlush: {0}&S has no updates to process.",
                                   world.ClassyName );
            } else {
                player.MessageNow( "WFlush: Flushing {0}&S ({1} blocks)...",
                                   world.ClassyName,
                                   map.UpdateQueueLength + map.DrawQueueBlockCount );
                world.Flush();
            }
        }

        #endregion
        #region WorldInfo

        static readonly CommandDescriptor CdWorldInfo = new CommandDescriptor {
            Name = "WInfo",
            Aliases = new[] { "mapinfo" },
            Category = CommandCategory.World | CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Usage = "/WInfo [WorldName]",
            Help = "Shows information about a world: player count, map dimensions, permissions, etc." +
                   "If no WorldName is given, shows info for current world.",
            Handler = WorldInfoHandler
        };

        static void WorldInfoHandler( Player player, CommandReader cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                if( player.World == null ) {
                    player.Message( "Please specify a world name when calling /WInfo from console." );
                    return;
                } else {
                    worldName = player.World.Name;
                }
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;

            player.Message( "World {0}&S has {1} player(s) on.",
                            world.ClassyName,
                            world.CountVisiblePlayers( player ) );

            Map map = world.Map;

            // If map is not currently loaded, grab its header from disk
            if( map == null ) {
                try {
                    map = MapUtility.LoadHeader( Path.Combine( Paths.MapPath, world.MapFileName ) );
                } catch( Exception ex ) {
                    player.Message( "  Map information could not be loaded: {0}: {1}",
                                    ex.GetType().Name, ex.Message );
                }
            }

            if( map != null ) {
                player.Message( "  Map dimensions are {0} x {1} x {2}",
                                map.Width, map.Length, map.Height );
            }

            // Print access/build limits
            player.Message( "  " + world.AccessSecurity.GetDescription( world, "world", "accessed" ) );
            player.Message( "  " + world.BuildSecurity.GetDescription( world, "world", "modified" ) );

            // Print lock/unlock information
            if( world.IsLocked ) {
                player.Message( "  {0}&S was locked {1} ago by {2}",
                                world.ClassyName,
                                DateTime.UtcNow.Subtract( world.LockedOn ).ToMiniString(),
                                world.LockedBy );
            } else if( world.UnlockedBy != null ) {
                player.Message( "  {0}&S was unlocked {1} ago by {2}",
                                world.ClassyName,
                                DateTime.UtcNow.Subtract( world.UnlockedOn ).ToMiniString(),
                                world.UnlockedBy );
            }

            if( !String.IsNullOrEmpty( world.LoadedBy ) && world.LoadedOn != DateTime.MinValue ) {
                player.Message( "  {0}&S was created/loaded {1} ago by {2}",
                                world.ClassyName,
                                DateTime.UtcNow.Subtract( world.LoadedOn ).ToMiniString(),
                                world.LoadedByClassy );
            }

            if( !String.IsNullOrEmpty( world.MapChangedBy ) && world.MapChangedOn != DateTime.MinValue ) {
                player.Message( "  Map was last changed {0} ago by {1}",
                                DateTime.UtcNow.Subtract( world.MapChangedOn ).ToMiniString(),
                                world.MapChangedByClassy );
            }

            if( world.BlockDB.IsEnabled ) {
                if( world.BlockDB.EnabledState == YesNoAuto.Auto ) {
                    player.Message( "  BlockDB is enabled (auto) on {0}", world.ClassyName );
                } else {
                    player.Message( "  BlockDB is enabled on {0}", world.ClassyName );
                }
            } else {
                player.Message( "  BlockDB is disabled on {0}", world.ClassyName );
            }

            player.Message( "  " + GetBackupSettingsString( world ) );
        }

        #endregion
        #region WorldLoad

        static readonly CommandDescriptor CdWorldLoad = new CommandDescriptor {
            Name = "WLoad",
            Aliases = new[] { "wadd" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WLoad FileName [WorldName [BuildRank [AccessRank]]]",
            Help = "If WorldName parameter is not given, replaces the current world's map with the specified map. The old map is overwritten. " +
                   "If the world with the specified name exists, its map is replaced with the specified map file. " +
                   "Otherwise, a new world is created using the given name and map file. " +
                   "NOTE: For security reasons, you may only load files from the map folder. " +
                   "For a list of supported formats, see &H/Help WLoad Formats",
            HelpSections = new Dictionary<string, string>{
                { "formats",    "WLoad supported formats: fCraft FCM (versions 2, 3, and 4), MCSharp/MCZall/MCLawl (.lvl), " +
                                "D3 (.map), Classic (.dat), InDev (.mclevel), MinerCPP/LuaCraft (.dat), " +
                                "JTE (.gz), iCraft/Myne (directory-based), Opticraft (.save)." }
            },
            Handler = WorldLoadHandler
        };


        static void WorldLoadHandler( Player player, CommandReader cmd ) {
            string fileName = cmd.Next();
            string worldName = cmd.Next();

            if( worldName == null && player.World == null ) {
                player.Message( "When using /WLoad from console, you must specify the world name." );
                return;
            }

            if( fileName == null ) {
                // No params given at all
                CdWorldLoad.PrintUsage( player );
                return;
            }

            string fullFileName = WorldManager.FindMapFile( player, fileName );
            if( fullFileName == null ) return;

            // Loading map into current world
            if( worldName == null ) {
                if( !cmd.IsConfirmed ) {
                    Logger.Log( LogType.UserActivity,
                                "WLoad: Asked {0} to confirm replacing the map of world {1} (\"this map\")",
                                player.Name, player.World.Name );
                    player.Confirm( cmd, "Replace THIS MAP with \"{0}\"?", fileName );
                    return;
                }
                Map map;
                try {
                    map = MapUtility.Load( fullFileName );
                } catch( Exception ex ) {
                    player.MessageNow( "Could not load specified file: {0}: {1}", ex.GetType().Name, ex.Message );
                    return;
                }
                World world = player.World;

                // Loading to current world
                try {
                    world.MapChangedBy = player.Name;
                    world.ChangeMap( map );
                } catch( WorldOpException ex ) {
                    Logger.Log( LogType.Error,
                                "Could not complete WorldLoad operation: {0}", ex.Message );
                    player.Message( "&WWLoad: {0}", ex.Message );
                    return;
                }

                world.Players.Message( player, "{0}&S loaded a new map for this world.",
                                              player.ClassyName );
                player.MessageNow( "New map loaded for the world {0}", world.ClassyName );

                Logger.Log( LogType.UserActivity,
                            "{0} {1} &sloaded new map for world \"{1}\" from \"{2}\"",
                            player.Info.Rank.Name, player.Name, world.Name, fileName );


            } else {
                // Loading to some other (or new) world
                if( !World.IsValidName( worldName ) ) {
                    player.MessageInvalidWorldName( worldName );
                    return;
                }

                string buildRankName = cmd.Next();
                string accessRankName = cmd.Next();
                Rank buildRank = RankManager.DefaultBuildRank;
                Rank accessRank = null;
                if( buildRankName != null ) {
                    buildRank = RankManager.FindRank( buildRankName );
                    if( buildRank == null ) {
                        player.MessageNoRank( buildRankName );
                        return;
                    }
                    if( accessRankName != null ) {
                        accessRank = RankManager.FindRank( accessRankName );
                        if( accessRank == null ) {
                            player.MessageNoRank( accessRankName );
                            return;
                        }
                    }
                }

                // Retype world name, if needed
                if( worldName == "-" ) {
                    if( player.LastUsedWorldName != null ) {
                        worldName = player.LastUsedWorldName;
                    } else {
                        player.Message( "Cannot repeat world name: you haven't used any names yet." );
                        return;
                    }
                }

                lock( WorldManager.SyncRoot ) {
                    World world = WorldManager.FindWorldExact( worldName );
                    if( world != null ) {
                        player.LastUsedWorldName = world.Name;
                        // Replacing existing world's map
                        if( !cmd.IsConfirmed ) {
                            Logger.Log( LogType.UserActivity,
                                        "WLoad: Asked {0} to confirm replacing the map of world {1}",
                                        player.Name, world.Name );
                            player.Confirm( cmd, "Replace map for {0}&S with \"{1}\"?",
                                            world.ClassyName, fileName );
                            return;
                        }

                        Map map;
                        try {
                            map = MapUtility.Load( fullFileName );
                        } catch( Exception ex ) {
                            player.MessageNow( "Could not load specified file: {0}: {1}", ex.GetType().Name, ex.Message );
                            return;
                        }

                        try {
                            world.MapChangedBy = player.Name;
                            world.ChangeMap( map );
                        } catch( WorldOpException ex ) {
                            Logger.Log( LogType.Error,
                                        "Could not complete WorldLoad operation: {0}", ex.Message );
                            player.Message( "&WWLoad: {0}", ex.Message );
                            return;
                        }

                        world.Players.Message( player, "{0}&S loaded a new map for the world {1}",
                                               player.ClassyName, world.ClassyName );
                        player.MessageNow( "New map for the world {0}&S has been loaded.", world.ClassyName );
                        Logger.Log( LogType.UserActivity,
                                    "{0} {1} &sloaded new map for world \"{2}\" from \"{3}\"",
                                    player.Info.Rank.Name, player.Name, world.Name, fullFileName );

                    } else {
                        // Adding a new world
                        string targetFullFileName = Path.Combine( Paths.MapPath, worldName + ".fcm" );
                        if( !cmd.IsConfirmed &&
                            File.Exists( targetFullFileName ) && // target file already exists
                            !Paths.Compare( targetFullFileName, fullFileName ) ) {
                            // and is different from sourceFile
                            Logger.Log( LogType.UserActivity,
                                        "WLoad: Asked {0} to confirm replacing map file \"{1}\"",
                                        player.Name, fullFileName );
                            player.Confirm( cmd,
                                            "A map named \"{0}\" already exists, and will be overwritten with \"{1}\".",
                                            Path.GetFileName( fullFileName ), Path.GetFileName( fullFileName ) );
                            return;
                        }

                        Map map;
                        try {
                            map = MapUtility.Load( fullFileName );
                        } catch( Exception ex ) {
                            player.MessageNow( "Could not load \"{0}\": {1}: {2}",
                                               fileName, ex.GetType().Name, ex.Message );
                            return;
                        }

                        World newWorld;
                        try {
                            newWorld = WorldManager.AddWorld( player, worldName, map, false );
                        } catch( WorldOpException ex ) {
                            player.Message( "WLoad: {0}", ex.Message );
                            return;
                        }

                        player.LastUsedWorldName = worldName;
                        newWorld.BuildSecurity.MinRank = buildRank;
                        if( accessRank == null ) {
                            newWorld.AccessSecurity.ResetMinRank();
                        } else {
                            newWorld.AccessSecurity.MinRank = accessRank;
                        }
                        newWorld.BlockDB.AutoToggleIfNeeded();
                        if( BlockDB.IsEnabledGlobally && newWorld.BlockDB.IsEnabled ) {
                            player.Message( "BlockDB is now auto-enabled on world {0}", newWorld.ClassyName );
                        }
                        newWorld.LoadedBy = player.Name;
                        newWorld.LoadedOn = DateTime.UtcNow;
                        Server.Message( "{0}&S created a new world named {1}",
                                        player.ClassyName, newWorld.ClassyName );
                        Logger.Log( LogType.UserActivity,
                                    "{0} {1} &screated a new world named \"{2}\" (loaded from \"{3}\")",
                                    player.Info.Rank.Name, player.Name, worldName, fileName );
                        WorldManager.SaveWorldList();
                        player.MessageNow( "Access is {0}+&S, and building is {1}+&S on {2}",
                                           newWorld.AccessSecurity.MinRank.ClassyName,
                                           newWorld.BuildSecurity.MinRank.ClassyName,
                                           newWorld.ClassyName );
                    }
                }
            }

            Server.RequestGC();
        }

        #endregion
        #region WorldClear

        static readonly CommandDescriptor CdWorldClear = new CommandDescriptor
        {
            Name = "WorldClear",
            Aliases = new[] { "wclear" },
            Category = CommandCategory.New,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WClear",
            Help = "Used to clear a map",
            Handler = WorldClearHandler
        };


        static void WorldClearHandler(Player player, CommandReader cmd)
        {
            string fullFileName = WorldManager.FindMapClearFile(player, player.World + "clear");

            if (fullFileName == null) return;

            // Loading map into current world
            if (player.World == null)
            {
                if (!cmd.IsConfirmed)
                {
                    Logger.Log(LogType.UserActivity,
                                "WLoad: Asked {0} to confirm clearing the map of world {1} (\"this map\")",
                                player.Name, player.World.Name);
                    player.Confirm(cmd, "Clear \"{0}\"?", player.World);
                    return;
                }
                Map map;
                try
                {
                    map = MapUtility.Load(fullFileName);
                }
                catch (Exception ex)
                {
                    player.MessageNow("Could not load specified file: {0}: {1}", ex.GetType().Name, ex.Message);
                    player.MessageNow("Please use &h/WCS &sfirst on an empty map to create a backup for clearing.", ex.GetType().Name, ex.Message);
                    return;
                }
                World world = player.World;

                // Loading to current world
                try
                {
                    world.MapChangedBy = player.Name;
                    world.ChangeMap(map);
                }
                catch (WorldOpException ex)
                {
                    Logger.Log(LogType.Error,
                                "Could not complete WorldLoad operation: {0}", ex.Message);
                    player.Message("&WWClear: {0}", ex.Message);
                    return;
                }

                world.Players.Message(player, "{0}&S cleared this world.",
                                              player.ClassyName);
                player.MessageNow("New clear map loaded for {0}", world.ClassyName);

                Logger.Log(LogType.UserActivity,
                            "{0} {1} &scleared map for world \"{1}\" from \"{2}\"",
                            player.Info.Rank.Name, player.Name, world.Name, player.World);


            }
            else
            {             
                // Retype world name, if needed
                
                lock (WorldManager.SyncRoot)
                {
                    World world = player.World;
                    if (world != null)
                    {
                        player.LastUsedWorldName = world.Name;
                        // Replacing existing world's map
                        if (!cmd.IsConfirmed)
                        {
                            Logger.Log(LogType.UserActivity,
                                        "WClear: Asked {0} to confirm replacing the map of world {1}",
                                        player.Name, world.Name);
                            player.Confirm(cmd, "Clear {0}&S map?",
                                            world.ClassyName, player.World);
                            return;
                        }

                        Map map;
                        try
                        {
                            map = MapUtility.Load(fullFileName);
                        }
                        catch (Exception ex)
                        {
                            player.MessageNow("Could not load specified file: {0}: {1}", ex.GetType().Name, ex.Message);
                            return;
                        }

                        try
                        {
                            world.MapChangedBy = player.Name;
                            world.ChangeMap(map);
                        }
                        catch (WorldOpException ex)
                        {
                            Logger.Log(LogType.Error,
                                        "Could not complete WorldClear operation: {0}", ex.Message);
                            player.Message("&WWClear: {0}", ex.Message);
                            return;
                        }

                        world.Players.Message(player, "{0}&S cleared the map for world {1}",
                                               player.ClassyName, world.ClassyName);
                        player.MessageNow("New map for the world {0}&S has been loaded.", world.ClassyName);
                        Logger.Log(LogType.UserActivity,
                                    "{0} {1} &sloaded new map for world \"{2}\"",
                                    player.Info.Rank.Name, player.Name, world.Name, fullFileName);

                    }
                }
            }

            Server.RequestGC();
        }

        #endregion
        #region WorldMain

        static readonly CommandDescriptor CdWorldMain = new CommandDescriptor {
            Name = "WMain",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WMain [@RankName] [WorldName]",
            Help = "Sets the specified world as the new main world. " +
                   "Main world is what newly-connected players join first. " +
                   "You can specify a rank name to set a different starting world for that particular rank.",
            Handler = WorldMainHandler
        };

        static void WorldMainHandler( Player player, CommandReader cmd ) {
            string param = cmd.Next();
            if( param == null ) {
                player.Message( "Main world is {0}", WorldManager.MainWorld.ClassyName );
                var mainedRanks = RankManager.Ranks
                                             .Where( r => r.MainWorld != null && r.MainWorld != WorldManager.MainWorld )
                                             .ToArray();
                if( mainedRanks.Length > 0 ) {
                    player.Message( "Rank mains: {0}",
                                    mainedRanks.JoinToString( r => String.Format( "{0}&S for {1}&S",
                                                                                  r.MainWorld.ClassyName,
                                                                                  r.ClassyName ) ) );
                }
                return;
            }

            if( param.StartsWith( "@" ) ) {
                string rankName = param.Substring( 1 );
                Rank rank = RankManager.FindRank( rankName );
                if( rank == null ) {
                    player.MessageNoRank( rankName );
                    return;
                }
                string worldName = cmd.Next();
                if( worldName == null ) {
                    if( rank.MainWorld != null ) {
                        player.Message( "Main world for rank {0}&S is {1}",
                                        rank.ClassyName,
                                        rank.MainWorld.ClassyName );
                    } else {
                        player.Message( "Main world for rank {0}&S is {1}&S (default)",
                                        rank.ClassyName,
                                        WorldManager.MainWorld.ClassyName );
                    }
                } else {
                    World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                    if( world != null ) {
                        SetRankMainWorld( player, rank, world );
                    }
                }

            } else {
                World world = WorldManager.FindWorldOrPrintMatches( player, param );
                if( world != null ) {
                    SetMainWorld( player, world );
                }
            }
        }


        static void SetRankMainWorld( Player player, Rank rank, World world ) {
            if( world == rank.MainWorld ) {
                player.Message( "World {0}&S is already set as main for {1}&S.",
                                world.ClassyName, rank.ClassyName );
                return;
            }

            if( world == WorldManager.MainWorld ) {
                if( rank.MainWorld == null ) {
                    player.Message( "The main world for rank {0}&S is already {1}&S (default).",
                                    rank.ClassyName, world.ClassyName );
                } else {
                    rank.MainWorld = null;
                    WorldManager.SaveWorldList();
                    Server.Message( "&S{0}&S has reset the main world for rank {1}&S.",
                                    player.ClassyName, rank.ClassyName );
                    Logger.Log( LogType.UserActivity,
                                "{0} reset the main world for rank {1}.",
                                player.Name, rank.Name );
                }
                return;
            }

            if( world.AccessSecurity.MinRank > rank ) {
                player.Message( "World {0}&S requires {1}+&S to join, so it cannot be used as the main world for rank {2}&S.",
                                world.ClassyName, world.AccessSecurity.MinRank, rank.ClassyName );
                return;
            }

            rank.MainWorld = world;
            WorldManager.SaveWorldList();
            Server.Message( "{0}&S designated {1}&S to be the main world for rank {2}",
                            player.ClassyName, world.ClassyName, rank.ClassyName );
            Logger.Log( LogType.UserActivity,
                        "{0} set {1} to be the main world for rank {2}.",
                        player.Name, world.Name, rank.Name );
        }


        static void SetMainWorld( Player player, World world ) {
            if( world == WorldManager.MainWorld ) {
                player.Message( "World {0}&S is already set as main.", world.ClassyName );

            } else if( !player.Info.Rank.AllowSecurityCircumvention && !player.CanJoin( world ) ) {
                // Prevent players from exploiting /WMain to gain access to restricted maps
                switch( world.AccessSecurity.CheckDetailed( player.Info ) ) {
                    case SecurityCheckResult.RankTooLow:
                        player.Message( "You are not allowed to set {0}&S as the main world (by rank).", world.ClassyName );
                        return;
                    case SecurityCheckResult.BlackListed:
                        player.Message( "You are not allowed to set {0}&S as the main world (blacklisted).", world.ClassyName );
                        return;
                }

            } else {
                if( world.AccessSecurity.HasRestrictions ) {
                    world.AccessSecurity.Reset();
                    player.Message( "The main world cannot have access restrictions. " +
                                    "All access restrictions were removed from world {0}",
                                    world.ClassyName );
                }

                try {
                    WorldManager.MainWorld = world;
                } catch( WorldOpException ex ) {
                    player.Message( ex.Message );
                    return;
                }

                Server.Message( "{0}&S set {1}&S to be the main world.",
                                  player.ClassyName, world.ClassyName );
                Logger.Log( LogType.UserActivity,
                            "{0} set {1} to be the main world.",
                            player.Name, world.Name );
            }
        }

        #endregion
        #region WorldRename

        static readonly CommandDescriptor CdWorldRename = new CommandDescriptor {
            Name = "WRename",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WRename OldName NewName",
            Help = "Changes the name of a world. Does not require any reloading.",
            Handler = WorldRenameHandler
        };

        static void WorldRenameHandler( Player player, CommandReader cmd ) {
            string oldName = cmd.Next();
            string newName = cmd.Next();
            if( oldName == null || newName == null ) {
                CdWorldRename.PrintUsage( player );
                return;
            }

            World oldWorld = WorldManager.FindWorldOrPrintMatches( player, oldName );
            if( oldWorld == null ) return;
            oldName = oldWorld.Name;

            if( !World.IsValidName( newName ) ) {
                player.MessageInvalidWorldName( newName );
                return;
            }

            World newWorld = WorldManager.FindWorldExact( newName );
            if( !cmd.IsConfirmed && newWorld != null && newWorld != oldWorld ) {
                Logger.Log( LogType.UserActivity,
                            "WRename: Asked {0} to confirm replacing world \"{1}\"",
                            player.Name, newWorld.Name );
                player.Confirm( cmd, "A world named {0}&S already exists. Replace it?", newWorld.ClassyName );
                return;
            }

            if( !cmd.IsConfirmed && Paths.FileExists( Path.Combine( Paths.MapPath, newName + ".fcm" ), true ) ) {
                Logger.Log( LogType.UserActivity,
                            "WRename: Asked {0} to confirm overwriting map file \"{1}.fcm\"",
                            player.Name, newName );
                player.Confirm( cmd, "Renaming this world will overwrite an existing map file \"{0}.fcm\".", newName );
                return;
            }

            try {
                WorldManager.RenameWorld( oldWorld, newName, true, true );
            } catch( WorldOpException ex ) {
                switch( ex.ErrorCode ) {
                    case WorldOpExceptionCode.NoChangeNeeded:
                        player.MessageNow( "WRename: World is already named \"{0}\"", oldName );
                        return;
                    case WorldOpExceptionCode.DuplicateWorldName:
                        player.MessageNow( "WRename: Another world named \"{0}\" already exists.", newName );
                        return;
                    case WorldOpExceptionCode.InvalidWorldName:
                        player.MessageNow( "WRename: Invalid world name: \"{0}\"", newName );
                        return;
                    case WorldOpExceptionCode.MapMoveError:
                        player.MessageNow( "WRename: World \"{0}\" was renamed to \"{1}\", but the map file could not be moved due to an error: {2}",
                                            oldName, newName, ex.InnerException );
                        return;
                    default:
                        player.MessageNow( "&WWRename: Unexpected error renaming world \"{0}\": {1}", oldName, ex.Message );
                        Logger.Log( LogType.Error,
                                    "WorldCommands.Rename: Unexpected error while renaming world {0} to {1}: {2}",
                                    oldWorld.Name, newName, ex );
                        return;
                }
            }

            player.LastUsedWorldName = newName;
            Logger.Log( LogType.UserActivity,
                        "{0} renamed the world \"{1}\" to \"{2}\".",
                        player.Name, oldName, newName );
            Server.Message( "{0}&S renamed the world \"{1}\" to \"{2}\"",
                              player.ClassyName, oldName, newName );
        }

        #endregion
        #region WorldSave

        static readonly CommandDescriptor CdWorldSave = new CommandDescriptor {
            Name = "WorldSave",
            Aliases = new[] { "wsave" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WSave FileName &Sor&H /WSave WorldName FileName",
            Help = "Saves a map copy to a file with the specified name. " +
                   "The \".fcm\" file extension can be omitted. " +
                   "If a file with the same name already exists, it will be overwritten.",
            Handler = WorldSaveHandler
        };

        static void WorldSaveHandler( Player player, CommandReader cmd ) {
            string p1 = cmd.Next(), p2 = cmd.Next();
            if( p1 == null ) {
                CdWorldSave.PrintUsage( player );
                return;
            }

            World world = player.World;
            string fileName;
            if( p2 == null ) {
                fileName = p1;
                if( world == null ) {
                    player.Message( "When called from console, /wsave requires WorldName. See \"/Help wsave\" for details." );
                    return;
                }
            } else {
                world = WorldManager.FindWorldOrPrintMatches( player, p1 );
                if( world == null ) return;
                fileName = p2;
            }

            // normalize the path
            fileName = fileName.Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar );
            if( fileName.EndsWith( "/" ) && fileName.EndsWith( @"\" ) ) {
                fileName += world.Name + ".fcm";
            } else if( !fileName.ToLower().EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase ) ) {
                fileName += ".fcm";
            }
            if( !Paths.IsValidPath( fileName ) ) {
                player.Message( "Invalid file name." );
                return;
            }
            string fullFileName = Path.Combine( Paths.MapPath, fileName );
            if( !Paths.Contains( Paths.MapPath, fullFileName ) ) {
                player.MessageUnsafePath();
                return;
            }

            // Ask for confirmation if overwriting
            if( File.Exists( fullFileName ) ) {
                FileInfo targetFile = new FileInfo( fullFileName );
                FileInfo sourceFile = new FileInfo( world.MapFileName );
                if( !targetFile.FullName.Equals( sourceFile.FullName, StringComparison.OrdinalIgnoreCase ) ) {
                    if( !cmd.IsConfirmed ) {
                        Logger.Log( LogType.UserActivity,
                                    "WSave: Asked {0} to confirm overwriting map file \"{1}\"",
                                    player.Name, targetFile.FullName );
                        player.Confirm( cmd, "Target file \"{0}\" already exists, and will be overwritten.", targetFile.Name );
                        return;
                    }
                }
            }

            // Create the target directory if it does not exist
            string dirName = fullFileName.Substring( 0, fullFileName.LastIndexOf( Path.DirectorySeparatorChar ) );
            if( !Directory.Exists( dirName ) ) {
                Directory.CreateDirectory( dirName );
            }

            player.MessageNow( "Saving map to {0}", fileName );

            const string mapSavingErrorMessage = "Map saving failed. See server logs for details.";
            Map map = world.Map;
            if( map == null ) {
                if( File.Exists( world.MapFileName ) ) {
                    try {
                        File.Copy( world.MapFileName, fullFileName, true );
                    } catch( Exception ex ) {
                        Logger.Log( LogType.Error,
                                    "WorldCommands.WorldSave: Error occurred while trying to copy an unloaded map: {0}", ex );
                        player.Message( mapSavingErrorMessage );
                    }
                } else {
                    Logger.Log( LogType.Error,
                                "WorldCommands.WorldSave: Map for world \"{0}\" is unloaded, and file does not exist.",
                                world.Name );
                    player.Message( mapSavingErrorMessage );
                }
            } else if( map.Save( fullFileName ) ) {
                player.Message( "Map saved succesfully." );
            } else {
                Logger.Log( LogType.Error,
                            "WorldCommands.WorldSave: Saving world \"{0}\" failed.", world.Name );
                player.Message( mapSavingErrorMessage );
            }
        }

        #endregion
        #region WorldClearSave

        static readonly CommandDescriptor CdWorldClearSave = new CommandDescriptor
        {
            Name = "WorldClearSave",
            Aliases = new[] { "wcs" },
            Category = CommandCategory.New,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WCS",
            Help = "Saves a map copy to be used with /WClear",
            Handler = WorldClearSaveHandler
        };

        static void WorldClearSaveHandler(Player player, CommandReader cmd)
        {
            string WorldCSave = player.World.Name;
            if (player.World.Name == null)
            {
                CdWorldSave.PrintUsage(player);
                return;
            }

            World world = player.World;
            string fileName;
            fileName = WorldCSave;
            {
                world = WorldManager.FindWorldOrPrintMatches(player, WorldCSave);
                if (world == null) return;
                fileName = WorldCSave;
            }

            // normalize the path
            fileName = fileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            fileName = "World(" + WorldCSave + ")clear.fcm";
            if (!Paths.IsValidPath(fileName))
            {
                player.Message("Invalid file name.");
                return;
            }
            string fullFileName = Path.Combine(Paths.WClearPath, fileName);
            if (!Paths.Contains(Paths.WClearPath, fullFileName))
            {
                player.MessageUnsafePath();
                return;
            }

            // Ask for confirmation if overwriting
            if (File.Exists(fullFileName))
            {
                FileInfo targetFile = new FileInfo(fullFileName);
                FileInfo sourceFile = new FileInfo(world.MapFileName);
                if (!targetFile.FullName.Equals(sourceFile.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!cmd.IsConfirmed)
                    {
                        Logger.Log(LogType.UserActivity,
                                    "WCSave: Asked {0} to confirm overwriting map file \"{1}\"",
                                    player.Name, targetFile.FullName);
                        player.Confirm(cmd, "Target file \"{0}\" already exists, and will be overwritten.", targetFile.Name);
                        return;
                    }
                }
            }

            // Create the target directory if it does not exist
            string dirName = fullFileName.Substring(0, fullFileName.LastIndexOf(Path.DirectorySeparatorChar));
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

            player.MessageNow("Saving map to {0}", fileName);

            const string mapSavingErrorMessage = "Map saving failed. See server logs for details.";
            Map map = world.Map;
            if (map == null)
            {
                if (File.Exists(world.MapFileName))
                {
                    try
                    {
                        File.Copy(world.MapFileName, fullFileName, true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogType.Error,
                                    "WorldCommands.WorldCSave: Error occurred while trying to copy an unloaded map: {0}", ex);
                        player.Message(mapSavingErrorMessage);
                    }
                }
                else
                {
                    Logger.Log(LogType.Error,
                                "WorldCommands.WorldCSave: Map for world \"{0}\" is unloaded, and file does not exist.",
                                world.Name);
                    player.Message(mapSavingErrorMessage);
                }
            }
            else if (map.Save(fullFileName))
            {
                player.Message("Map saved succesfully.");
            }
            else
            {
                Logger.Log(LogType.Error,
                            "WorldCommands.WorldCSave: Saving world \"{0}\" failed.", world.Name);
                player.Message(mapSavingErrorMessage);
            }
        }

        #endregion
        #region WorldSet

        static readonly CommandDescriptor CdWorldSet = new CommandDescriptor {
            Name = "WSet",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WSet <World> <Variable> <Value>",
            Help = "Sets a world variable. Variables are: hide, backups, greeting",
            HelpSections = new Dictionary<string, string>{
                { "hide",       "&H/WSet <WorldName> Hide On/Off\n&S" +
                                "When a world is hidden, it does not show up on the &H/Worlds&S list. It can still be joined normally." },
                { "backups",    "&H/WSet <World> Backups Off&S, &H/WSet <World> Backups Default&S, or &H/WSet <World> Backups <Time>\n&S" +
                                "Enables or disables periodic backups. Time is given in the compact format." },
                { "greeting",   "&H/WSet <WorldName> Greeting <Text>\n&S" +
                                "Sets a greeting message. Message is shown whenever someone joins the map, and can also be viewed in &H/WInfo" }
            },
            Handler = WorldSetHandler
        };

        static void WorldSetHandler( Player player, CommandReader cmd ) {
            string worldName = cmd.Next();
            string varName = cmd.Next();
            string value = cmd.NextAll();
            if( worldName == null || varName == null ) {
                CdWorldSet.PrintUsage( player );
                return;
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;

            switch( varName.ToLower() ) {
                case "hide":
                case "hidden":
                    if( String.IsNullOrEmpty( value ) ) {
                        player.Message( "World {0}&S is current {1}hidden.",
                                        world.ClassyName,
                                        world.IsHidden ? "" : "NOT " );
                    } else if( value.Equals( "on", StringComparison.OrdinalIgnoreCase ) ||
                               value.Equals( "true", StringComparison.OrdinalIgnoreCase ) ||
                               value == "1" ) {
                        if( world.IsHidden ) {
                            player.Message( "World {0}&S is already hidden.", world.ClassyName );
                        } else {
                            player.Message( "World {0}&S is now hidden.", world.ClassyName );
                            world.IsHidden = true;
                            WorldManager.SaveWorldList();
                        }
                    } else if( value.Equals( "off", StringComparison.OrdinalIgnoreCase ) ||
                               value.Equals( "false", StringComparison.OrdinalIgnoreCase ) ||
                               value == "0" ) {
                        if( world.IsHidden ) {
                            player.Message( "World {0}&S is no longer hidden.", world.ClassyName );
                            world.IsHidden = false;
                            WorldManager.SaveWorldList();
                        } else {
                            player.Message( "World {0}&S is not hidden.", world.ClassyName );
                        }
                    } else {
                        CdWorldSet.PrintUsage( player );
                    }
                    break;

                case "backup":
                case "backups":
                    TimeSpan backupInterval;
                    string oldDescription = world.BackupSettingDescription;
                    if( String.IsNullOrEmpty( value ) ) {
                        player.Message( GetBackupSettingsString( world ) );
                        return;

                    } else if( value.Equals( "off", StringComparison.OrdinalIgnoreCase ) ||
                               value.StartsWith( "disable", StringComparison.OrdinalIgnoreCase ) ) {
                        // Disable backups on the world
                        if( world.BackupEnabledState == YesNoAuto.No ) {
                            MessageSameBackupSettings( player, world );
                            return;
                        } else {
                            world.BackupEnabledState = YesNoAuto.No;
                        }

                    } else if( value.Equals( "default", StringComparison.OrdinalIgnoreCase ) ||
                               value.Equals( "auto", StringComparison.OrdinalIgnoreCase ) ) {
                        // Set world to use default settings
                        if( world.BackupEnabledState == YesNoAuto.Auto ) {
                            MessageSameBackupSettings( player, world );
                            return;
                        } else {
                            world.BackupEnabledState = YesNoAuto.Auto;
                        }

                    } else if( value.TryParseMiniTimespan( out backupInterval ) ) {
                        if( backupInterval == TimeSpan.Zero ) {
                            // Set world's backup interval to 0, which is equivalent to disabled
                            if( world.BackupEnabledState == YesNoAuto.No ) {
                                MessageSameBackupSettings( player, world );
                                return;
                            } else {
                                world.BackupEnabledState = YesNoAuto.No;
                            }
                        } else if( world.BackupEnabledState != YesNoAuto.Yes ||
                                   world.BackupInterval != backupInterval ) {
                            // Alter world's backup interval
                            world.BackupInterval = backupInterval;
                        } else {
                            MessageSameBackupSettings( player, world );
                            return;
                        }

                    } else {
                        CdWorldSet.PrintUsage( player );
                        return;
                    }
                    player.Message( "Backup setting for world {0}&S changed from \"{1}\" to \"{2}\"",
                                    world.ClassyName, oldDescription, world.BackupSettingDescription );
                    WorldManager.SaveWorldList();
                    break;

                case "description":
                case "greeting":
                    if( String.IsNullOrEmpty( value ) ) {
                        if (world.Greeting == null)
                        {
                            if (!Directory.Exists("./WorldGreeting/")) Directory.CreateDirectory("./WorldGreeting/");
                            if (File.Exists("./WorldGreeting/" + player.World.Name + ".txt"))
                            {
                                world.Greeting = File.ReadAllText("./WorldGreeting/" + player.World.Name + ".txt");
                                if (world.Greeting.Length == 0) player.Message("No greeting message is set for world {0}", world.ClassyName);
                                else player.Message("&SGreeting message for world {0}&s is: {1}", world.ClassyName, world.Greeting);
                                world.Greeting = null;
                            }
                            else player.Message("No greeting message is set for world {0}", world.ClassyName);
                        }
                    } else {
                        if (value.ToLower() == "remove")
                        {
                            player.Message("Greeting message removed for world {0}", world.ClassyName);
                            if (!Directory.Exists("./WorldGreeting/")) Directory.CreateDirectory("./WorldGreeting/");
                            if (File.Exists("./WorldGreeting/" + player.World.Name + ".txt")) File.Delete("./WorldGreeting/" + player.World.Name + ".txt");
                            world.Greeting = null;
                        }
                        else
                        {
                            world.Greeting = value.Replace("%n", "/n");
                            player.Message("Greeting message for world {0}&S set to: {1}", world.ClassyName, world.Greeting);
                            if (!Directory.Exists("./WorldGreeting/")) Directory.CreateDirectory("./WorldGreeting/");
                            File.WriteAllText("./WorldGreeting/" + player.World.Name + ".txt", world.Greeting);
                            world.Greeting = null;
                        }
                    }
                    break;

                default:
                    CdWorldSet.PrintUsage( player );
                    break;
            }
        }


        static void MessageSameBackupSettings( Player player, World world ) {
            player.Message( "Backup settings for {0}&S are already \"{1}\"",
                            world.ClassyName, world.BackupSettingDescription );
        }


        static string GetBackupSettingsString( World world ) {
            switch( world.BackupEnabledState ) {
                case YesNoAuto.Yes:
                    return String.Format( "World {0}&S is backed up every {1}",
                                          world.ClassyName,
                                          world.BackupInterval.ToMiniString() );
                case YesNoAuto.No:
                    return String.Format( "Backups are manually disabled on {0}&S",
                                          world.ClassyName );
                case YesNoAuto.Auto:
                    if( World.DefaultBackupsEnabled ) {
                        return String.Format( "World {0}&S is backed up every {1} (default)",
                                              world.ClassyName,
                                              World.DefaultBackupInterval.ToMiniString() );
                    } else {
                        return String.Format( "Backups are disabled on {0}&S (default)",
                                              world.ClassyName );
                    }
                default:
                    // never happens
                    throw new Exception( "Unexpected BackupEnabledState value: " + world.BackupEnabledState );
            }
        }

        #endregion
        #region WorldUnload

        static readonly CommandDescriptor CdWorldUnload = new CommandDescriptor {
            Name = "WUnload",
            Aliases = new[] { "wremove", "wdelete" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WUnload WorldName",
            Help = "Removes the specified world from the world list, and moves all players from it to the main world. " +
                   "The main world itself cannot be removed with this command. You will need to delete the map file manually.",
            Handler = WorldUnloadHandler
        };

        static void WorldUnloadHandler( Player player, CommandReader cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                CdWorldUnload.PrintUsage( player );
                return;
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;

            try {
                WorldManager.RemoveWorld( world );
            } catch( WorldOpException ex ) {
                switch( ex.ErrorCode ) {
                    case WorldOpExceptionCode.CannotDoThatToMainWorld:
                        player.MessageNow( "&WWorld {0}&W is set as the main world. " +
                                           "Assign a new main world before deleting this one.",
                                           world.ClassyName );
                        return;
                    case WorldOpExceptionCode.WorldNotFound:
                        player.MessageNow( "&WWorld {0}&W is already unloaded.",
                                           world.ClassyName );
                        return;
                    default:
                        player.MessageNow( "&WUnexpected error occurred while unloading world {0}&W: {1}",
                                           world.ClassyName, ex.GetType().Name );
                        Logger.Log( LogType.Error,
                                    "WorldCommands.WorldUnload: Unexpected error while unloading world {0}: {1}",
                                    world.Name, ex );
                        return;
                }
            }

            Server.Message( player,
                            "{0}&S removed {1}&S from the world list.",
                            player.ClassyName, world.ClassyName );
            player.Message( "Removed {0}&S from the world list. You can now delete the map file ({1}.fcm) manually.",
                            world.ClassyName, world.Name );
            Logger.Log( LogType.UserActivity,
                        "{0} removed \"{1}\" from the world list.",
                        player.Name, worldName );

            Server.RequestGC();
        }

        #endregion        
        #region CTF
        static readonly CommandDescriptor CdCTF = new CommandDescriptor
        {
            Name = "ctf",
            Category = CommandCategory.New,
            Permissions = new Permission[] { Permission.ReadStaffChat },
            IsConsoleSafe = false,
            IsHidden = true,
            Usage = "/ctf <start / stop / redspawn / bluespawn / redflag / blueflag / swapteam>",
            Help = "Allows starting CTF / editing CTF properties. List of properties:\n" +
                "Start, Stop, RedSpawn, BlueSpawn, RedFlag, BlueFlag, SwapTeam\n" +
                "For detailed help see &H/Help CTF <Property>",
            HelpSections = new Dictionary<string, string>{
				{ "start",     	    "&H/CTF start\n&S" +
						"Starts a CTF game on the current world of the player." },
				{ "stop",     		"&H/CTF stop\n&S" +
						"Stops the current CTF game. You needn't be in the same world. " +
						"Original kick reason is preserved in the logs." },
				{ "redspawn",   	"&H/CTF redspawn\n&S" +
						"Sets the spawn of red team to your current position.\n" +
						"Note that spawns are reset after the game it stopped.)"},
				{ "bluespawn",   	"&H/CTF bluespawn\n&S" +
						"Sets the spawn of blue team to your current position.\n" +
						"Note that spawns are reset after the game it stopped.)"},
				{ "redflag",   		"&H/CTF redflag\n&S" +
						"Sets the position of the red flag to your current position.\n" +
						"Note that flag positions are reset after the game it stopped.)"},
				{ "blueflag",   	"&H/CTF blueflag\n&S" +
						"Sets the position of the blue flag to your current position.\n" +
						"Note that flag positions are reset after the game it stopped.)"},
                { "swapteam",       "&H/CTF swapteam\n&S" +
                        "Switches your team in the CTF Match."}
			},
            Handler = CTFHandler
        };

        private static void CTFHandler(Player player, CommandReader cmd)
        {
            if (!cmd.HasNext)
            {
                CdCTF.PrintUsage(player);
                return;
            }
            string Options = cmd.Next();
            World world = player.World;
            if (world == WorldManager.MainWorld)
            {
                player.Message("/ctf cannot be used on the main world");
                return;
            }
            switch (Options.ToLower())
            {
                case "start":
                    {
                        if (player.Can(Permission.ReadStaffChat))
                        {
                            if (CTF.instances == 0)
                            {
                                if (player.World.Name.ToLower() != "ctf")
                                {
                                    player.Message("You can only start a game on the CTF map.");
                                    break;
                                }
                                else
                                {
                                    player.Message("Started CTF game on world {0}", player.World.ClassyName);
                                    CTF.Start(player, player.World);
                                    break;
                                }
                            }
                            else
                            {
                                player.Message("Game is already in progress. Type /ctf stop to stop the game.");
                                break;
                            }
                        }
                        else
                        {
                            player.Message("You need to be a Moderator to start CTF Games.");
                            break;
                        }
                    }

                case "stop":
                    {
                        if (player.Can(Permission.ReadStaffChat))
                        {
                            if (CTF.instances == 1)
                            {
                                player.Message("Stopped CTF game.");
                                CTF.Stop();
                                break;
                            }
                            else
                            {
                                player.Message("No CTF game running.");
                                break;
                            }
                        }
                        else
                        {
                            player.Message("You need to be a Moderator to stop CTF Games.");
                            break;
                        }
                    }

                case "redspawn":
                    {
                        if (player.Can(Permission.ReadStaffChat))
                        {
                            player.Message("Red teams spawn set to {0},{1},{2}.", player.Position.ToBlockCoords().X,
                                           player.Position.ToBlockCoords().Y, player.Position.Z);
                            CTF.redSpawn = new Position(player.Position.X,
                                                        player.Position.Y,
                                                        player.Position.Z);
                            break;
                        }
                        else
                        {
                            player.Message("You need to be a Moderator to adjust CTF Spawns.");
                            break;
                        }
                    }
                case "bluespawn":
                    {
                        if (player.Can(Permission.ReadStaffChat))
                        {
                            player.Message("Blue teams spawn set to {0},{1},{2}.", player.Position.ToBlockCoords().X,
                                           player.Position.ToBlockCoords().Y, player.Position.Z);
                            CTF.blueSpawn = new Position(player.Position.X,
                                                        player.Position.Y,
                                                        player.Position.Z);
                            break;
                        }
                        else
                        {
                            player.Message("You need to be a Moderator to adjust CTF Spawns.");
                            break;
                        }
                    }
                case "redflag":
                    {
                        if (player.Can(Permission.ReadStaffChat))
                        {
                            CTF.world.Map.QueueUpdate(new BlockUpdate(Player.Console, CTF.redFlag, Block.Air));
                            CTF.redFlag = player.Position.ToBlockCoords();
                            CTF.world.Map.QueueUpdate(new BlockUpdate(Player.Console, CTF.redFlag, Block.Red));
                            player.Message("Red flag positon set to {0},{1},{2}",
                                           player.Position.X, player.Position.Y, player.Position.Z);
                            break;
                        }
                        else
                        {
                            player.Message("You need to be a Moderator to adjust CTF Flags.");
                            break;
                        }
                    }
                case "blueflag":
                    {
                        if (player.Can(Permission.ReadStaffChat))
                        {
                            CTF.world.Map.QueueUpdate(new BlockUpdate(Player.Console, CTF.blueFlag, Block.Air));
                            CTF.blueFlag = player.Position.ToBlockCoords();
                            CTF.world.Map.QueueUpdate(new BlockUpdate(Player.Console, CTF.blueFlag, Block.Blue));
                            player.Message("Blue flag positon set to {0},{1},{2}",
                                           player.Position.X, player.Position.Y, player.Position.Z);
                            break;
                        }
                        else
                        {
                            player.Message("You need to be a Moderator to adjust CTF Flags.");
                            break;
                        }
                    }
                case "swapteam":
                case "switchteam":
                case "swap":
                case "changeteam":
                case "changesides":
                case "switch":
                    {
                        if (player.Team == "Blue")
                        {
                            if ((CTF.blueTeam.ToArray().Length) - (CTF.redTeam.ToArray().Length) >= 1)
                            {
                                player.Message("You have switched to the &cRed&s team");
                                if (CTF.blueTeam.Contains(player))
                                {
                                    CTF.blueTeam.Remove(player);
                                    CTF.redTeam.Add(player);
                                }
                                if (player.IsHoldingFlag)
                                {
                                    world.Players.Message("&cFlag holder &1" + player.Name + " &cswitched to the &4Red&c team, thus dropping the flag for the &1Blue&c team!");
                                    CTF.blueHasFlag = false;
                                    player.IsHoldingFlag = false;
                                    world.Map.QueueUpdate(new BlockUpdate(Player.Console, CTF.redFlag, Block.Red));
                                }
                                player.Team = "Red";
                                player.TeleportTo(CTF.redSpawn);
                                break;
                            }
                            else
                            {
                                if (player.Can(Permission.ReadStaffChat))
                                {
                                    player.Message("You have switched to the &cRed&s team. The teams are now unbalanced!");
                                    if (CTF.blueTeam.Contains(player))
                                    {
                                        CTF.blueTeam.Remove(player);
                                        CTF.redTeam.Add(player);
                                    }
                                    if (player.IsHoldingFlag)
                                    {
                                        world.Players.Message("&cFlag holder &1" + player.Name + " &cswitched to the &4Red&c team, thus dropping the flag for the &1Blue&c team!");
                                        CTF.blueHasFlag = false;
                                        player.IsHoldingFlag = false;
                                        world.Map.QueueUpdate(new BlockUpdate(Player.Console, CTF.redFlag, Block.Red));
                                    }
                                    player.Team = "Red";
                                    player.TeleportTo(CTF.redSpawn);
                                    break;
                                }
                                else
                                {
                                    player.Message("You cannot switch teams. The teams would become too unbalanced!");
                                    break;
                                }
                            }
                        }
                        else if (player.Team == "Red")
                        {
                            if ((CTF.redTeam.ToArray().Length) - (CTF.blueTeam.ToArray().Length) >= 1)
                            {
                                player.Message("You have switched to the &9Blue&s team");
                                if (CTF.redTeam.Contains(player))
                                {
                                    CTF.redTeam.Remove(player);
                                    CTF.blueTeam.Add(player);
                                }
                                if (player.IsHoldingFlag)
                                {
                                    world.Players.Message("&cFlag holder &4" + player.Name + " &cswitched to the &1Blue&c team, thus dropping the flag for the &4Red&c team!");
                                    CTF.redHasFlag = false;
                                    player.IsHoldingFlag = false;
                                    world.Map.QueueUpdate(new BlockUpdate(Player.Console, CTF.blueFlag, Block.Blue));
                                }
                                player.Team = "Blue";
                                player.TeleportTo(CTF.blueSpawn);
                                break;
                            }
                            else
                            {
                                if (player.Can(Permission.ReadStaffChat))
                                {
                                    player.Message("You have switched to the &9Blue&s team. The teams are now unbalanced!");
                                    if (CTF.redTeam.Contains(player))
                                    {
                                        CTF.redTeam.Remove(player);
                                        CTF.blueTeam.Add(player);
                                    }
                                    if (player.IsHoldingFlag)
                                    {
                                        world.Players.Message("&cFlag holder &4" + player.Name + " &cswitched to the &1Blue&c team, thus dropping the flag for the &4Red&c team!");
                                        CTF.redHasFlag = false;
                                        player.IsHoldingFlag = false;
                                        world.Map.QueueUpdate(new BlockUpdate(Player.Console, CTF.blueFlag, Block.Blue));
                                    }
                                    player.Team = "Blue";
                                    player.TeleportTo(CTF.blueSpawn);
                                    break;
                                }
                                else
                                {
                                    player.Message("You cannot switch teams. The teams would become too unbalanced!");
                                    break;
                                }
                            }
                        }
                        break;
                    }

                default:
                    CdCTF.PrintUsage(player);
                    break;
            }

        }
        #endregion
        #region SkyLightEmulator
        static readonly CommandDescriptor CdSLE = new CommandDescriptor {
            Name = "SkyLightEmulator",
            Aliases = new[] { "SLE" },
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.ManageWorlds },
            Help =
                "Toggles whether or not to emulate sky color based on time in a world",
            Usage = "/SLE [World] [on/off]",
            Handler = SLEHandler
        };

        static void SLEHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            string worldtest = cmd.Next();
            World world;
            if (worldtest != null && !worldtest.Equals("0") && !worldtest.Equals("1") && !worldtest.Equals("on") && !worldtest.Equals("off") && !worldtest.Equals("true") && !worldtest.Equals("false")) {
                world = WorldManager.FindWorldOrPrintMatches(player, worldtest);
            } else {
                world = player.World;
                cmd.Rewind();
            }
            if (world == null) { return; }
            bool turnSkyOn = !world.SkyLightEmulator;
            if (cmd.HasNext && !cmd.NextOnOff(out turnSkyOn)) {
                if (world != null) {
                    turnSkyOn = !world.SkyLightEmulator;
                }
            }
            if (turnSkyOn != world.SkyLightEmulator) {
                if (turnSkyOn) {
                    world.SkyLightEmulator = true;
                    player.Message(
                        "&sSkylight Emulator for world {0}&s: &2ON&e. Sky will now change color to emulate time.",
                        world.ClassyName);
                    foreach (Player p in world.Players.Where(p => p.SupportsEnvColors)) {
                        string hex;
                        if (Server.SkyColorHex.TryGetValue(Server.ColorTime, out hex)) {
                            p.Send(Packet.MakeEnvSetColor(0, hex));
                        }
                        if (Server.CloudAndFogColorHex.TryGetValue(Server.ColorTime, out hex)) {
                            p.Send(Packet.MakeEnvSetColor(1, hex));
                            p.Send(Packet.MakeEnvSetColor(2, hex));
                        }
                        p.Message("SkyLight Emulator is now enabled!");
                    }
                } else {
                    world.SkyLightEmulator = false;
                    player.Message("&sSkylight Emulator for world {0}&s: &4OFF&e.", world.ClassyName);
                    foreach (Player p in world.Players.Where(p => p.SupportsEnvColors && p.World != null)) {
                        p.Send(Packet.MakeEnvSetColor(0, p.World.SkyColor));
                        p.Send(Packet.MakeEnvSetColor(1, p.World.CloudColor));
                        p.Send(Packet.MakeEnvSetColor(2, p.World.FogColor));
                        p.Message("SkyLight Emulator is now disabled!");

                    }
                }
            }
        }
        #endregion
    }
}