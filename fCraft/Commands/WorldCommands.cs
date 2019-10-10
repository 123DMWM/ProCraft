﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using fCraft.MapConversion;
using JetBrains.Annotations;
using fCraft.Drawing;
using System.Text;
using fCraft.Games;
using fCraft.Portals;
using System.Diagnostics;
using System.Net;
using System.Drawing;

namespace fCraft {
    /// <summary> Contains commands related to world management. </summary>
    static class WorldCommands {
        const int WorldNamesPerPage = 30;

        internal static void Init() {
            CommandManager.RegisterCommand( CdBlockDB );
            CommandManager.RegisterCommand( CdBlockInfo );
            CdGenerate.Help = "Generates a new map. If no dimensions are given, uses current world's dimensions. " +
                              "If no file name is given, loads generated world into current world.&N" +
                              "Available themes: Grass, " + Enum.GetNames( typeof( MapGenTheme ) ).JoinToString() + "&N" +
                              "Available terrain types: Empty, Ocean, " + Enum.GetNames( typeof( MapGenTemplate ) ).JoinToString() + "&N" +
                              "Note: You do not need to specify a theme with \"Empty\" and \"Ocean\" templates.";
            CommandManager.RegisterCommand( CdGenerate );
            CommandManager.RegisterCommand( CdGenerateHeightMap );
            CommandManager.RegisterCommand( CdJoin );
            CommandManager.RegisterCommand( CdJoinr );
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
            CommandManager.RegisterCommand( CdReJoin );
            CommandManager.RegisterCommand( CdMyWorld );
            CommandManager.RegisterCommand( CdMaxPW );
            CommandManager.RegisterCommand( CdPortal );
            CommandManager.RegisterCommand( CdBlockInfoList );
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
                { "auto",       "/BlockDB <WorldName> Auto&N&S" +
                                "Allows BlockDB to decide whether it should be enabled or disabled based on each world's permissions (default)." },
                { "on",         "/BlockDB <WorldName> On&N&S" +
                                "Enables block tracking. Information will only be available for blocks that changed while BlockDB was enabled." },
                { "off",        "/BlockDB <WorldName> Off&N&S" +
                                "Disables block tracking. Block changes will NOT be recorded while BlockDB is disabled. " +
                                "Note that disabling BlockDB does not delete the existing data. Use &Hclear&S for that." },
                { "clear",      "/BlockDB <WorldName> Clear&N&S" +
                                "Clears all recorded data from the BlockDB. Erases all changes from memory and deletes the .fbdb file." },
                { "limit",      "/BlockDB <WorldName> Limit <#>|None&N&S" +
                                "Sets the limit on the maximum number of changes to store for a given world. " +
                                "Oldest changes will be deleted once the limit is reached. " +
                                "Put \"None\" to disable limiting. " +
                                "Unless a Limit or a TimeLimit it specified, all changes will be stored indefinitely." },
                { "timelimit",  "/BlockDB <WorldName> TimeLimit <Time>/None&N&S" +
                                "Sets the age limit for stored changes. " +
                                "Oldest changes will be deleted once the limit is reached. " +
                                "Use \"None\" to disable time limiting. " +
                                "Unless a Limit or a TimeLimit it specified, all changes will be stored indefinitely." },
                { "preload",    "/BlockDB <WorldName> Preload On/Off&N&S" +
                                "Enabled or disables preloading. When BlockDB is preloaded, all changes are stored in memory as well as in a file. " +
                                "This reduces CPU and disk use for busy maps, but may not be suitable for large maps due to increased memory use." },
            },
            Handler = BlockDBHandler
        };

        static void BlockDBHandler( Player player, CommandReader cmd ) {
            if( !BlockDB.IsEnabledGlobally ) {
                player.Message("&WBlockDB is disabled on this server.");
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
                                        "BlockDB: {0} {1} &Senabled BlockDB on world {2} (was {3})",
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
                                        "BlockDB: {0} {1} &Sdisabled BlockDB on world {2} (was {3})",
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

                            if( limitString.CaselessEquals( "none" ) ) {
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
                            if( limitString.CaselessEquals( "none" ) ) {
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
                        if (!player.Can(Permission.ShutdownServer))
                        {
                            player.Message("You must be {0}&S to clear the block DataBase", RankManager.GetMinRankWithAllPermissions(Permission.ShutdownServer).ClassyName);
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

                            } else if( param.CaselessEquals( "on" ) ) {
                                // turns preload on
                                if( db.IsPreloaded ) {
                                    player.Message( "BlockDB preloading is already enabled on world {0}", world.ClassyName );
                                } else {
                                    db.IsPreloaded = true;
                                    WorldManager.SaveWorldList();
                                    player.Message( "BlockDB preloading is now enabled on world {0}", world.ClassyName );
                                }

                            } else if( param.CaselessEquals( "off" ) ) {
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
                coords = map.Bounds.Clamp( coords );
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
            if( results.Length == 0 ) {
                args.Player.Message( "BlockInfo: No results for {0}", args.Coordinate );
                return;
            }
            
            // iterate from oldest to newest
            for( int i = results.Length - 1; i >= 0; i-- ) {
                BlockDBEntry entry = results[i];
                string date = DateTime.UtcNow.Subtract( DateTimeUtil.ToDateTime( entry.Timestamp ) ).ToMiniString();

                PlayerInfo info = PlayerDB.FindPlayerInfoByID( entry.PlayerID );
                string playerName;
                if( info == null ) {
                    playerName = "?&S";
                } else {
                    Player target = info.PlayerObject;
                    if( target != null && args.Player.CanSee( target ) ) {
                        playerName = info.Rank.Color + info.Name + "&S(&aOn&S)";
                    } else {
                        playerName = info.Rank.Color + info.Name + "&S(&7Off&S)";
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
                        contextString = "(Redone)";
                    } else {
                        contextString = "(" + ( entry.Context & ~BlockChangeContext.Drawn ) + ")";
                    }
                } else {
                    contextString = "(" + entry.Context + ")";
                }

                if( entry.OldBlock == Block.Air ) {
                    args.Player.Message(" {0} ago {1} placed {2} {3}",
                                        date, playerName, Map.GetBlockName(args.World, entry.NewBlock),
                                        contextString);
                } else if( entry.NewBlock == Block.Air ) {
                    args.Player.Message(" {0} ago {1} deleted {2} {3}",
                                        date, playerName, Map.GetBlockName(args.World, entry.OldBlock),
                                        contextString);
                } else {
                    args.Player.Message(" {0} ago {1} replaced {2} with {3} {4}",
                                        date, playerName, Map.GetBlockName(args.World, entry.OldBlock),
                                        Map.GetBlockName(args.World, entry.NewBlock), contextString);
                }
            }
        }

        #endregion
        #region BlockInfoList

        static readonly CommandDescriptor CdBlockInfoList = new CommandDescriptor {
            Name = "BInfoList",
            Category = CommandCategory.World | CommandCategory.New,
            IsConsoleSafe = true,
            Aliases = new[] { "bilist", "blist", "bl", "bil" },
            Permissions = new[] { Permission.ViewOthersInfo },
            RepeatableSelection = true,
            Usage = "/BInfoList [Player]",
            Help = "Checks edit history for a given player.",
            Handler = BlockInfoListHandler
        };

        static void BlockInfoListHandler(Player player, CommandReader cmd) {
            PlayerInfo info = InfoCommands.FindPlayerInfo(player, cmd);
            if (info == null) return;
            var args = new BlockInfoListLookupArgs {
                Player = info,
                Sender = player
            };
            Scheduler.NewBackgroundTask(BlockInfoListSchedulerCallback, args).RunOnce();
        }

        sealed class BlockInfoListLookupArgs {
            public PlayerInfo Player;
            public Player Sender;
        }


        static void BlockInfoListSchedulerCallback(SchedulerTask task) {
            BlockInfoListLookupArgs args = (BlockInfoListLookupArgs)task.UserState;
            string playerName = args.Player.Rank.Color + args.Player.Name;
            bool noChanges = true;
            int worldsListed = 0, worldsNotListed = 0;
            Stopwatch sw = Stopwatch.StartNew();
            BlockDBCounterProcessor counter = new BlockDBCounterProcessor();

            foreach (World world in WorldManager.Worlds.Where(w => w.BlockDB.IsEnabled 
                                                              && File.GetLastAccessTimeUtc(w.MapFileName) >= args.Player.FirstLoginDate)) {
                if (worldsListed >= 10) {
                    worldsNotListed++;
                    continue;
                }
                
                counter.Update(args.Player);
                world.BlockDB.Traverse(counter);
                if (counter.Placed == 0 && counter.Deleted == 0 && counter.Drawn == 0) continue;
                
                if (noChanges) {
                    args.Sender.Message("{0}&S has commited block changes on...", playerName);
                    noChanges = false;
                }
                
                args.Sender.Message("  {0}&S: Built &F{1}&S, Deleted &F{2}&S, Drew &F{3}", 
                                    world.ClassyName, counter.Placed, counter.Deleted, counter.Drawn);
                worldsListed++;
            }

            sw.Stop();

            if (noChanges) {
                args.Sender.Message("{0}&S has not commited any block changes on the currently loaded worlds", playerName);
            } else {
                args.Sender.Message("Showing &F{0}&S of &F{1}&S (Done in &F{2}&Sms)", worldsListed, worldsListed + worldsNotListed, sw.ElapsedMilliseconds);
            }
        }
        
        class BlockDBCounterProcessor : IBlockDBQueryProcessor {
            
            long ticks;
            int playerId;
            public int Placed, Deleted, Drawn;
            
            public void Update(PlayerInfo player) {
                ticks = player.FirstLoginDate.ToUnixTime();
                playerId = player.ID;
                Placed = 0; Deleted = 0; Drawn = 0;
            }
            
            public bool ProcessEntry(BlockDBEntry e) {
                if (e.Timestamp < ticks) return false;
                if (e.PlayerID != playerId) return true;
                
                if (e.Context == BlockChangeContext.Drawn) Drawn++;
                if (e.Context == BlockChangeContext.Manual && e.OldBlock == Block.Air) Placed++;
                if (e.Context == BlockChangeContext.Manual && e.OldBlock != Block.Air && e.NewBlock == Block.Air) Deleted++;
                return true;
            }
            
            public BlockDBEntry[] GetResults() { return null; }
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

        private static void GenHandler(Player player, CommandReader cmd) {
            World playerWorld = player.World;
            string themeName = cmd.Next();
            bool genOcean = false;
            bool genEmpty = false;
            bool noTrees = false;

            if (string.IsNullOrEmpty(themeName)) {
                CdGenerate.PrintUsage(player);
                return;
            } else {
                themeName = themeName.ToLower();
            }
            MapGenTheme theme = MapGenTheme.Forest;
            MapGenTemplate template = MapGenTemplate.Flat;

            // parse special template names (which do not need a theme)
            if (themeName.Equals("ocean")) {
                genOcean = true;

            } else if (themeName.Equals("empty")) {
                genEmpty = true;

            } else {
                string templateName = cmd.Next();
                if (templateName == null) {
                    CdGenerate.PrintUsage(player);
                    return;
                }

                // parse theme
                bool swapThemeAndTemplate = false;
                if (themeName.CaselessEquals("grass")) {
                    theme = MapGenTheme.Forest;
                    noTrees = true;

                } else if (templateName.CaselessEquals("grass")) {
                    theme = MapGenTheme.Forest;
                    noTrees = true;
                    swapThemeAndTemplate = true;

                } else if (EnumUtil.TryParse(themeName, out theme, true)) {
                    noTrees = (theme != MapGenTheme.Forest);

                } else if (EnumUtil.TryParse(templateName, out theme, true)) {
                    noTrees = (theme != MapGenTheme.Forest);
                    swapThemeAndTemplate = true;

                } else {
                    player.Message("Gen: Unrecognized theme \"{0}\". Available themes are: Grass, {1}", themeName,
                        Enum.GetNames(typeof (MapGenTheme)).JoinToString());
                    return;
                }

                // parse template
                if (swapThemeAndTemplate) {
                    if (!EnumUtil.TryParse(themeName, out template, true)) {
                        player.Message("Unrecognized template \"{0}\". Available terrain types: Empty, Ocean, {1}",
                            themeName, Enum.GetNames(typeof (MapGenTemplate)).JoinToString());
                        return;
                    }
                } else {
                    if (!EnumUtil.TryParse(templateName, out template, true)) {
                        player.Message("Unrecognized template \"{0}\". Available terrain types: Empty, Ocean, {1}",
                            templateName, Enum.GetNames(typeof (MapGenTemplate)).JoinToString());
                        return;
                    }
                }
            }

            // parse map dimensions
            int mapWidth, mapLength, mapHeight;
            if (cmd.HasNext) {
                int offset = cmd.Offset;
                if (!(cmd.NextInt(out mapWidth) && cmd.NextInt(out mapLength) && cmd.NextInt(out mapHeight))) {
                    if (playerWorld != null) {
                        Map oldMap = player.WorldMap;
                        // If map dimensions were not given, use current map's dimensions
                        mapWidth = oldMap.Width;
                        mapLength = oldMap.Length;
                        mapHeight = oldMap.Height;
                    } else {
                        player.Message("When used from console, /Gen requires map dimensions.");
                        CdGenerate.PrintUsage(player);
                        return;
                    }
                    cmd.Offset = offset;
                }
            } else if (playerWorld != null) {
                Map oldMap = player.WorldMap;
                // If map dimensions were not given, use current map's dimensions
                mapWidth = oldMap.Width;
                mapLength = oldMap.Length;
                mapHeight = oldMap.Height;
            } else {
                player.Message("When used from console, /Gen requires map dimensions.");
                CdGenerate.PrintUsage(player);
                return;
            }

            // Check map dimensions
            const string dimensionRecommendation =
                "Dimensions must be between 16 and 16384. " + "Recommended values: 16, 32, 64, 128, 256, 512, and 1024.";
            if (!Map.IsValidDimension(mapWidth)) {
                player.Message("Cannot make map with width {0}. {1}", mapWidth, dimensionRecommendation);
                return;
            } else if (!Map.IsValidDimension(mapLength)) {
                player.Message("Cannot make map with length {0}. {1}", mapLength, dimensionRecommendation);
                return;
            } else if (!Map.IsValidDimension(mapHeight)) {
                player.Message("Cannot make map with height {0}. {1}", mapHeight, dimensionRecommendation);
                return;
            }
            long volume = (long) mapWidth*mapLength*mapHeight;
            if (volume > Int32.MaxValue) {
                player.Message("Map volume may not exceed {0}", Int32.MaxValue);
                return;
            }

            if (!cmd.IsConfirmed &&
                (!Map.IsRecommendedDimension(mapWidth) || !Map.IsRecommendedDimension(mapLength) || mapHeight%16 != 0)) {
                player.Message("&WThe map will have non-standard dimensions. " +
                               "You may see glitched blocks or visual artifacts. " +
                               "The only recommended map dimensions are: 16, 32, 64, 128, 256, 512, and 1024.");
            }

            // figure out full template name
            bool genFlatgrass = (theme == MapGenTheme.Forest && noTrees && template == MapGenTemplate.Flat);
            string templateFullName;
            if (genEmpty) {
                templateFullName = "Empty";
            } else if (genOcean) {
                templateFullName = "Ocean";
            } else if (genFlatgrass) {
                templateFullName = "Flatgrass";
            } else {
                if (theme == MapGenTheme.Forest && noTrees) {
                    templateFullName = "Grass " + template;
                } else {
                    templateFullName = theme + " " + template;
                }
            }

            // check file/world name
            string fileName = cmd.Next();
            string fullFileName = null;
            if (fileName == null) {
                // replacing current world
                if (playerWorld == null) {
                    player.Message("When used from console, /Gen requires FileName.");
                    CdGenerate.PrintUsage(player);
                    return;
                }
                if (!cmd.IsConfirmed) {
                    Logger.Log(LogType.UserActivity,
                        "Gen: Asked {0} to confirm replacing the map of world {1} (\"this map\").",
                        player.Name, playerWorld.Name);

                    player.Confirm(cmd, "Replace THIS MAP with a generated one ({0})?", templateFullName);
                    return;
                }
            } else {
                if (cmd.HasNext) { CdGenerate.PrintUsage(player); return; }
                // saving to file
                if (!ExpandFilename(player, cmd, ref fileName, ref fullFileName)) return;
            }

            // generate the map
            Map map;
            player.Message("Generating {0}...", templateFullName);

            if (genEmpty) {
                map = MapGenerator.GenerateEmpty(mapWidth, mapLength, mapHeight);

            } else if (genOcean) {
                map = MapGenerator.GenerateOcean(mapWidth, mapLength, mapHeight);

            } else if (genFlatgrass) {
                map = MapGenerator.GenerateFlatgrass(mapWidth, mapLength, mapHeight);

            } else {
                MapGeneratorArgs args = MapGenerator.MakeTemplate(template);
                if (theme == MapGenTheme.Desert) {
                    args.AddWater = false;
                }
                float ratio = mapHeight/(float) args.MapHeight;
                args.MapWidth = mapWidth;
                args.MapLength = mapLength;
                args.MapHeight = mapHeight;
                args.MaxHeight = (int) Math.Round(args.MaxHeight*ratio);
                args.MaxDepth = (int) Math.Round(args.MaxDepth*ratio);
                args.SnowAltitude = (int) Math.Round(args.SnowAltitude*ratio);
                args.Theme = theme;
                args.AddTrees = !noTrees;

                MapGenerator generator = new MapGenerator(args);
                map = generator.Generate();
            }

            Server.RequestGC();

            // save map to file, or load it into a world
            if (fileName != null) {
                if (map.Save(fullFileName)) {
                    player.Message("Generation done. Saved to {0}", fileName);
                } else {
                    player.Message("&WAn error occurred while saving generated map to {0}", fileName);
                }
            } else {
                player.Message("Generation done. Changing map...");
                playerWorld.MapChangedBy = player.Name;
                playerWorld.ChangeMap(map);
            }
        }
        
        static bool ExpandFilename(Player player, CommandReader cmd, ref string fileName, ref string fullFileName) {
            if (fileName.CaselessStarts("pw_") && player.Info.Rank != RankManager.HighestRank) {
                player.Message("You cannot make fake personal worlds");
                return false;
            }
            
            // saving to file
            fileName = fileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (!fileName.CaselessEnds(".fcm")) fileName += ".fcm";
            
            if (!Paths.IsValidPath(fileName)) {
                player.Message("Invalid file name.");
                return false;
            }
            
            fullFileName = Path.Combine(Paths.MapPath, fileName);
            if (!Paths.Contains(Paths.MapPath, fullFileName)) {
                player.MessageUnsafePath();
                return false;
            }
            
            string dirName = fullFileName.Substring(0, fullFileName.LastIndexOf(Path.DirectorySeparatorChar));
            if (!Directory.Exists(dirName)) {
                Directory.CreateDirectory(dirName);
            }
            
            if (!cmd.IsConfirmed && File.Exists(fullFileName)) {
                Logger.Log(LogType.UserActivity, "Gen: Asked {0} to confirm overwriting map file \"{1}\"",
                           player.Name, fileName);
                player.Confirm(cmd, "The mapfile \"{0}\" already exists. Overwrite?", fileName);
                return false;
            }
            return true;
        }

        #endregion
        #region GenHeightMap
        
        static readonly CommandDescriptor CdGenerateHeightMap = new CommandDescriptor {
            Name = "GenHeightMap",
            Aliases = new[] { "genhm" },
            Category = CommandCategory.World | CommandCategory.New,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/GenHeightMap [URL to heightmap image file] [FileName]",
            Help = "Generates a new map based on a heightmap image.&N" +
                              "If no file name is given, loads generated world into current world.&N" +
                              "If no file theme is given, generates default Grass theme.&N" +
                              "Available themes: Grass, " + Enum.GetNames(typeof(MapGenTheme)).JoinToString(),
            Handler = GenHMHandler
        };

        private static void GenHMHandler(Player player, CommandReader cmd) {
            World playerWorld = player.World;
            bool noTrees = true;
            string themeName = cmd.Next() ?? "grass";
            string url = null;
            string[] themes = { "arctic", "desert", "forest", "grass", "hell", "swamp" };

            if (themes.Contains(themeName.ToLower())) {
                if (themeName.CaselessEquals("forest") || themeName.CaselessEquals("swamp")) {
                    noTrees = false;
                }
                url = cmd.Next();
            } else {
                cmd.Rewind();
                url = cmd.Next();
            }

            if (!parseUrl(ref url, player)) return;

            Bitmap heightmap = null;

            // check file/world name
            string fileName = cmd.Next();
            string fullFileName = null;
            if (string.IsNullOrEmpty(fileName)) {
                // replacing current world
                if (playerWorld == null) {
                    player.Message("When used from console, /GenHeightMap requires FileName.");
                    CdGenerateHeightMap.PrintUsage(player);
                    return;
                }
                if (!cmd.IsConfirmed) {
                    Logger.Log(LogType.UserActivity,
                        "GenHM: Asked {0} to confirm replacing the map of world {1} (\"this map\").",
                        player.Name, playerWorld.Name);
                    
                    player.Confirm(cmd, "Replace THIS MAP with a generated one (HeightMap: &9{0}&S)?", url);
                    return;
                }

            } else {
                if (cmd.HasNext) { CdGenerateHeightMap.PrintUsage(player); return; }
                if (!ExpandFilename(player, cmd, ref fileName, ref fullFileName)) return;
            }

            // generate the map
            int mapWidth = 0, mapLength = 0;
            player.SendNow(Packet.Message(0, "Downloading file from: &9" + url, player));
            heightmap = DownloadImage(url, player);
            if (heightmap == null) return;
            mapWidth = heightmap.Width;
            mapLength = heightmap.Height;
            if (!Map.IsValidDimension(mapWidth) || !Map.IsValidDimension(mapLength)) {
                player.Message("Invalid image size along {0} &S(Must be inbetween 16 and 1024)", Map.IsValidDimension(mapWidth) ? "height: &F" + mapLength : "width: &F" + mapWidth);
                return;
            }
            player.SendNow(Packet.Message(0, "Generating HeightMap...", player));
            Map map = MapGenerator.GenerateEmpty(mapWidth, mapLength, 256);
            Generate(ref map, heightmap, themeName.ToLower());
            if (!noTrees) {
                Block stone = Block.Stone, dirt = Block.Dirt, grass = Block.Grass;
                ApplyTheme(themeName.ToLower(), ref stone, ref dirt, ref grass);
                GenerateTrees(map, grass);
            }
            Server.RequestGC();

            // save map to file, or load it into a world
            if (fileName != null) {
                if (map.Save(fullFileName)) {
                    player.Message("Generation done. Saved to {0}", fileName);
                } else {
                    player.Message("&WAn error occurred while saving generated map to {0}", fileName);
                }
            } else {
                player.Message("Generation done. Changing map...");
                playerWorld.MapChangedBy = player.Name;
                playerWorld.ChangeMap(map);
            }
        }

        public static void Generate(ref Map map, Bitmap image, string theme) {
            // Modified McGalaxy Code
            Block stone = Block.Stone, dirt = Block.Dirt, grass = Block.Grass;
            ApplyTheme(theme, ref stone, ref dirt, ref grass);
            int index = 0, oneY = map.Width * map.Length;
            int imageWidth = image.Width, imageHeight = image.Height;
            
            using (image) {
                for (int z = 0; z < imageHeight; z++)
                    for (int x = 0; x < imageWidth; x++) 
                {
                    System.Drawing.Color col = image.GetPixel(x, z);
                    int height = (int)Math.Floor(((col.R + col.G + col.B) / 3) * (col.A / 255.00));
                    
                    for (int y = 0; y < height - 5; y++) {
                        map.Blocks[index + oneY * y] = (byte)stone;
                    }
                    for (int y = height - 5; y < height - 1; y++) {
                        if (y >= 0) map.Blocks[index + oneY * y] = (byte)dirt;
                    }
                    if (height > 0) {
                        map.Blocks[index + oneY * (height - 1)] = (byte)grass;
                    }
                    index++;
                }
            }
        }

        public static void GenerateTrees([NotNull] Map map, Block grass) {
            if (map == null) throw new ArgumentNullException("map");
            int minHeight = 5;
            int maxHeight = 7;
            int minTrunkPadding = 7;
            int maxTrunkPadding = 11;
            const int topLayers = 2;
            const double odds = 0.618;

            Random rn = new Random();

            short[,] shadows = map.ComputeHeightmap();

            for (int x = 0; x < map.Width; x += rn.Next(minTrunkPadding, maxTrunkPadding + 1)) {
                for (int y = 0; y < map.Length; y += rn.Next(minTrunkPadding, maxTrunkPadding + 1)) {
                    int nx = x + rn.Next(-(minTrunkPadding / 2), (maxTrunkPadding / 2) + 1);
                    int ny = y + rn.Next(-(minTrunkPadding / 2), (maxTrunkPadding / 2) + 1);
                    if (nx < 0 || nx >= map.Width || ny < 0 || ny >= map.Length) continue;
                    int nz = shadows[nx, ny];

                    if ((map.GetBlock(nx, ny, nz) == grass)) {
                        int nh;
                        if ((nh = rn.Next(minHeight, maxHeight + 1)) + nz + nh / 2 > map.Height)
                            continue;

                        for (int z = 1; z <= nh; z++)
                            map.SetBlock(nx, ny, nz + z, Block.Log);

                        for (int i = -1; i < nh / 2; i++) {
                            int radius = (i >= (nh / 2) - topLayers) ? 1 : 2;
                            for (int xoff = -radius; xoff < radius + 1; xoff++) {
                                for (int yoff = -radius; yoff < radius + 1; yoff++) {
                                    if (rn.NextDouble() > odds && Math.Abs(xoff) == Math.Abs(yoff) && Math.Abs(xoff) == radius)
                                        continue;
                                    if (map.GetBlock(nx + xoff, ny + yoff, nz + nh + i) == Block.Air)
                                        map.SetBlock(nx + xoff, ny + yoff, nz + nh + i, Block.Leaves);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void ApplyTheme(string theme, ref Block stone, ref Block dirt, ref Block grass) {
            switch (theme) {
                case "arctic":
                    grass = Block.Snow;
                    dirt = Block.Ice;
                    break;
                case "desert":
                    grass = Block.Sand;
                    dirt = Block.Sand;
                    stone = Block.Sandstone;
                    break;
                case "hell":
                    grass = Block.Obsidian;
                    dirt = Block.Magma;
                    stone = Block.StillLava;
                    break;
                case "swamp":
                    grass = Block.Dirt;
                    stone = Block.MossyCobble;
                    break;
                default:
                    break;

            }
        }

        public static Bitmap DownloadImage(string url, Player player) {
            if (url == null) {
                throw new InvalidOperationException(
                    "ImageUrl must be set before calling DownloadImage()");
            }

            int ContentLength;
            HttpWebRequest reqLen = (HttpWebRequest)WebRequest.Create(url);
            reqLen.Timeout = (int)TimeSpan.FromSeconds(6).TotalMilliseconds;
            reqLen.Method = "HEAD";
            using (WebResponse resp = reqLen.GetResponse()) {
                int.TryParse(resp.Headers.Get("Content-Length"), out ContentLength);
            }
            if (ContentLength > 5000000) {
                player.Message("&WImage size is too large {0}MB > 5MB", ContentLength / 1000000);
                return null;
            }

            HttpWebRequest request = HttpUtil.CreateRequest(new Uri(url), TimeSpan.FromSeconds(6));
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                if ((response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Moved ||
                     response.StatusCode == HttpStatusCode.Redirect) &&
                    response.ContentType.CaselessStarts("image")) {
                    // if the remote file was found, download it
                    using (Stream inputStream = response.GetResponseStream()) {
                        // TODO: check file size limit?
                        return new Bitmap(inputStream);
                    }
                } else {
                    player.Message("&WFailed to download the image from the given url.");
                    throw new Exception("Error downloading image: " + response.StatusCode);
                }
            }

        }

        public static bool parseUrl(ref string urlString, Player player) {
            if (urlString.NullOrWhiteSpace()) {
                player.Message("You must provide a url to a heightmap image.");
                return false;
            }

            if (urlString.StartsWith("http://imgur.com/")) urlString = "http://i.imgur.com/" + urlString.Substring("http://imgur.com/".Length) + ".png";
            if (urlString.StartsWith("++")) urlString = "http://i.imgur.com/" + urlString.Substring(2) + ".png";
            if (!urlString.CaselessStarts("http://") && !urlString.CaselessStarts("https://")) urlString = "http://" + urlString;

            if (!urlString.CaselessStarts("http://i.imgur.com/") && !urlString.CaselessStarts("http://123DMWM.com/")) {
                player.Message("For safety reasons we only accept images uploaded to &9http://imgur.com/ &SSorry for this inconvenience.");
                player.Message("    You cannot use: &9" + urlString);
                return false;
            }

            if (!urlString.CaselessEnds(".png") && !urlString.CaselessEnds(".jpg") && !urlString.CaselessEnds(".bmp")) {
                player.Message("URL must be a link to an image (.png/.jpg/.bmp");
                return false;
            }

            return true;
        }

        #endregion       
        #region Join

        static readonly CommandDescriptor CdJoin = new CommandDescriptor {
            Name = "Join",
            Aliases = new[] {"g", "j", "load", "goto", "map"},
            Category = CommandCategory.World,
            Usage = "/Join WorldName",
            Help =
                "Teleports the player to a specified world. You can see the list of available worlds by using &H/Worlds",
            Handler = JoinHandler
        };

        private static void JoinHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            string worldName = cmd.Next();
            if (worldName == null) {
                CdJoin.PrintUsage(player);
                return;
            }

            if (worldName == "-") {
                if (player.LastUsedWorldName != null) {
                    worldName = player.LastUsedWorldName;
                } else {
                    player.Message("Cannot repeat world name: you haven't used any names yet.");
                    return;
                }
            }
            World[] worlds = WorldManager.FindWorlds(player, worldName);
            foreach (World w in worlds) {
                if (w.Name.StartsWith("PW_")) {
                    player.Message("You must use &a/PW Join &Sto access personal worlds.");
                    return;
                }
            }

            if (worlds.Length > 1) {
                player.MessageManyMatches("world", worlds);
            } else if (worlds.Length == 1) {
                World world = worlds[0];
                player.LastUsedWorldName = world.Name;
                switch (world.AccessSecurity.CheckDetailed(player.Info)) {
                    case SecurityCheckResult.Allowed:
                    case SecurityCheckResult.WhiteListed:
                        if (world.IsFull) {
                            player.Message("Cannot join {0}&S: world is full.", world.ClassyName);
                            return;
                        }
                        if (cmd.IsConfirmed) {
                            player.JoinWorldNow(world, true, WorldChangeReason.ManualJoin);
                            return;
                        }
                        if (player.World.Name.CaselessEquals("tutorial") && !player.Info.HasRTR) {
                            player.Confirm(cmd,
                                "&SYou are choosing to skip the rules, if you continue you will spawn here the next time you log in.");
                            return;
                        }
                        player.StopSpectating();
                        if (!player.JoinWorldNow(world, true, WorldChangeReason.ManualJoin)) {
                            player.Message("ERROR: Failed to join world. See log for details.");
                        }
                        break;
                    case SecurityCheckResult.BlackListed:
                        player.Message("Cannot join world {0}&S: you are blacklisted.", world.ClassyName);
                        break;
                    case SecurityCheckResult.RankTooLow:
                        player.Message("Cannot join world {0}&S: must be {1}+", world.ClassyName,
                            world.AccessSecurity.MinRank.ClassyName);
                        break;
                }
            } else {
                player.MessageNoWorld(worldName);
            }
        }

        #endregion
        #region Join Random

        static readonly CommandDescriptor CdJoinr = new CommandDescriptor
        {
            Name = "JoinRandom",
            Aliases = new[] { "jr", "gotorandom", "gr" },
            Category = CommandCategory.World | CommandCategory.New,
            Usage = "/JoinRandom [@minrank]",
            Help = "Teleports the player to a random world." +
            "If a rank is specified it chooses from only the worlds that rank can access.",
            Handler = JoinrHandler
        };

        private static void JoinrHandler([NotNull] Player player, [NotNull] CommandReader cmd) {
            string rankStr = cmd.Next() ?? player.Info.Rank.Name;
            Rank rank = RankManager.FindRank(rankStr.Replace("@", ""));
            if (rank == null) {
                player.MessageNoRank(rankStr.Replace("@", ""));
                return;
            }
            World[] worlds = WorldManager.Worlds.Where(w => w.AccessSecurity.MinRank <= rank).ToArray();
            World world = worlds[new Random(Environment.TickCount).Next(0, worlds.Length)];
            if (world != null) { //'should' never be null, but whatever, null checks are always good
                switch (world.AccessSecurity.CheckDetailed(player.Info)) {
                    case SecurityCheckResult.Allowed:
                    case SecurityCheckResult.WhiteListed:
                        if (world.IsFull) {
                            player.Message("Cannot join {0}&S: world is full.", world.ClassyName);
                            return;
                        }
                        player.StopSpectating();
                        if (!player.JoinWorldNow(world, true, WorldChangeReason.ManualJoin)) {
                            player.Message("ERROR: Failed to join world. See log for details.");
                        }
                        break;
                    case SecurityCheckResult.BlackListed:
                        player.Message("Cannot join world {0}&S: you are blacklisted.", world.ClassyName);
                        break;
                    case SecurityCheckResult.RankTooLow:
                        player.Message("Cannot join world {0}&S: must be {1}+", world.ClassyName,
                            world.AccessSecurity.MinRank.ClassyName);
                        break;
                }
            } else {
                player.Message("World was null, shouldn't happen, Why'd you break it?");
                return;
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
            Aliases = new[] { "respawn", "suicide" },
            Category = CommandCategory.World,
            Help = "Teleports you to the current map's spawn.",
            Handler = SpawnHandler
        };

        static void SpawnHandler( Player player, CommandReader cmd ) {
            if( player.World == null ) PlayerOpException.ThrowNoWorld( player );
            if (player.World != null) {
                player.LastWorld = player.World;
                player.LastPosition = player.Position;
            }
            player.TeleportTo( player.World.LoadMap().getSpawnIfRandom());
            if (player.WorldMap.Spawn == Position.RandomSpawn) {
                player.Message("Randomized Spawn!");
            }
        }

        #endregion
        #region Rejoin

        static readonly CommandDescriptor CdReJoin = new CommandDescriptor
        {
            Name = "rejoin",
            Aliases = new[] { "rj" },
            Permissions = new[] { Permission.Chat },
            Category = CommandCategory.New | CommandCategory.World,
            Help = "Forces you to rejoin the world. Some commands require this if certain things change.",
            Handler = rejoinHandler
        };

        static void rejoinHandler(Player player, CommandReader cmd)
        {
            player.JoinWorld(player.World, WorldChangeReason.Rejoin, player.Position);
        }

        #endregion
        #region Block Hunt Map Settings

        /*static readonly CommandDescriptor CdGameSet = new CommandDescriptor
        {
            Name = "GameSettings",
            Aliases = new[] { "GameSet", "GSet", "GS" },
            Permissions = new[] { Permission.EditPlayerDB },
            Category = CommandCategory.New,
            Help = "&SAllows direct editing of game settings per world.&N " + 
                   "&SList of editable options: HiderSpawn, SeekerSpawn, Blocks.&N" + 
                   "&SFor detailed help see &H/Help GSet <Option>",
            HelpSections = new Dictionary<string, string>{
                { "hiderspawn",  "&H/GSet <WorldName> HiderSpawn <Action>&N" +
                                 "&SChanges the spawn for the hiders. Actions: Set, Reset, Display " },
                { "seekerspawn", "&H/GSet <WorldName> SeekerSpawn <Action>&N" +
                                 "&SChanges the spawn for the seeker. Actions: Set, Reset, Display" },
                { "gameblocks",  "&H/GSet <WorldName> GameBlocks <Action> <Block Name/ID>&N" +
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
                player.Message("Game settings for world (&a{0}&S)", world.Name);
                player.Message("  &SHiders spawn: {0}", new Position(world.HiderPosX, world.HiderPosY, world.HiderPosZ).ToBlockCoords().ToString());
                player.Message("  &SSeeker spawn: {0}", new Position(world.SeekerPosX, world.SeekerPosY, world.SeekerPosZ).ToBlockCoords().ToString());
                player.Message("  &SGame Blocks: {0}", world.GameBlocks.JoinToString(", "));
                return;
            }
            string action = cmd.Next();
            if (action == null)
            {
                player.Message(CdGameSet.Usage);
                return;
            }
            string blocks = cmd.Next();
            if (option.CaselessEquals("hiderspawn") || option.CaselessEquals("hspawn") || option.CaselessEquals("hider") || option.CaselessEquals("hs"))
            {
                if (action.CaselessEquals("set"))
                {
                    if (world == player.World)
                    {
                        world.HiderPosX = player.Position.X;
                        world.HiderPosY = player.Position.Y;
                        world.HiderPosZ = player.Position.Z;
                        player.Message("Hider Spawn for world (&a{0}&S) set to your location.", world.Name);
                        return;
                    }
                    else
                    {
                        player.Message("You must be in the world (&a{0}&S) to set the spawn.", world.Name);
                        return;
                    }
                }
                else if (action.CaselessEquals("reset"))
                {
                    world.HiderPosX = world.map.Spawn.X;
                    world.HiderPosY = world.map.Spawn.Y;
                    world.HiderPosZ = world.map.Spawn.Z;
                    player.Message("Hider Spawn for world (&a{0}&S) has reset to world spawn.", world.Name);
                    return;
                }
                else if (action.CaselessEquals("display"))
                {
                    player.Message("Hider Spawn for world (&a{0}&S) is: {1}", world.Name, new Position(world.HiderPosX, world.HiderPosY, world.HiderPosZ).ToBlockCoords().ToString());
                    return;
                }
                else
                {
                    player.Message(CdGameSet.Usage);
                    return;
                }
            }
            else if (option.CaselessEquals("seekerspawn") || option.CaselessEquals("sspawn") || option.CaselessEquals("seeker") || option.CaselessEquals("ss"))
            {
                if (action.CaselessEquals("set"))
                {
                    if (world == player.World)
                    {
                        world.SeekerPosX = player.Position.X;
                        world.SeekerPosY = player.Position.Y;
                        world.SeekerPosZ = player.Position.Z;
                        player.Message("Seeker Spawn for world (&a{0}&S) set to your location.", world.Name);
                        return;
                    }
                    else
                    {
                        player.Message("You must be in the world (&a{0}&S) to set the spawn.", world.Name);
                        return;
                    }
                }
                else if (action.CaselessEquals("reset"))
                {
                    world.SeekerPosX = world.map.Spawn.X;
                    world.SeekerPosY = world.map.Spawn.Y;
                    world.SeekerPosZ = world.map.Spawn.Z;
                    player.Message("Seeker Spawn for world (&a{0}&S) has reset to world spawn.", world.Name);
                    return;
                }
                else if (action.CaselessEquals("display"))
                {
                    player.Message("Seeker Spawn for world (&a{0}&S) is: {1}", world.Name, new Position(world.SeekerPosX, world.SeekerPosY, world.SeekerPosZ).ToBlockCoords().ToString());
                    return;
                }
                else
                {
                    player.Message(CdGameSet.Usage);
                    return;
                }
            }
            else if (option.CaselessEquals("gameblocks") || option.CaselessEquals("gblocks") || option.CaselessEquals("blocks") || option.CaselessEquals("gb"))
            {
                if (action.CaselessEquals("add"))
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
                else if (action.CaselessEquals("remove"))
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
                else if (action.CaselessEquals("reset"))
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
                else if (action.CaselessEquals("list") || action.CaselessEquals("display"))
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
                   "If a rank name is given, shows only worlds where players of that rank can build." +
                   "You can also search by world name using \"*\" as a wildcard(needed).",
            Handler = WorldsHandler
        };

        static void WorldsHandler(Player player, CommandReader cmd) {
            string param = cmd.Next();
            World[] worlds;

            string listName;
            string extraParam;
            int offset = 0;

            if (param == null || Int32.TryParse(param, out offset)) {
                listName = "available worlds";
                extraParam = "";
                worlds = WorldManager.Worlds.Where(player.CanSee).ToArray();

            } else if (param[0].Equals('@')) {
                if (param.Length == 1) {
                    CdWorlds.PrintUsage(player);
                    return;
                }
                string rankName = param.Substring(1);
                Rank rank = RankManager.FindRank(rankName);
                if (rank == null) {
                    player.MessageNoRank(rankName);
                    return;
                }
                listName = String.Format("worlds where {0}&S+ can build", rank.ClassyName);
                extraParam = "@" + rank.Name + " ";
                worlds = WorldManager.Worlds.Where(w => (w.BuildSecurity.MinRank <= rank) && player.CanSee(w)).ToArray();
            } else if (param.EndsWith("*") && param.StartsWith("*")) {
                listName = "worlds containing \"" + param.ToLower().Replace("*", "") + "\"";
                extraParam = param.ToLower();
                worlds = WorldManager.Worlds.Where(w => w.Name.CaselessContains(param.Replace("*", ""))).ToArray();
            } else if (param.EndsWith("*")) {
                listName = "worlds starting with \"" + param.ToLower().Replace("*", "") + "\"";
                extraParam = param.ToLower();
                worlds = WorldManager.Worlds.Where(w => w.Name.CaselessStarts(param.Replace("*", ""))).ToArray();
            } else if (param.StartsWith("*")) {
                listName = "worlds ending with \"" + param.ToLower().Replace("*", "") + "\"";
                extraParam = param.ToLower();
                worlds = WorldManager.Worlds.Where(w => w.Name.CaselessEnds(param.Replace("*", ""))).ToArray();
            } else {
                switch (param) {
                    case "a":
                    case "all":
                        listName = "worlds";
                        extraParam = "all ";
                        worlds = WorldManager.Worlds;
                        break;
                    case "h":
                    case "hidden":
                        listName = "hidden worlds";
                        extraParam = "hidden ";
                        worlds = WorldManager.Worlds.Where(w => !player.CanSee(w)).ToArray();
                        break;
                    case "p":
                    case "popular":
                    case "populated":
                        listName = "populated worlds";
                        extraParam = "populated ";
                        worlds = WorldManager.Worlds.Where(w => w.Players.Any(player.CanSee)).ToArray();
                        break;
                    default:
                        CdWorlds.PrintUsage(player);
                        return;
                }
            }
            if (cmd.HasNext && !cmd.NextInt(out offset)) {
                CdWorlds.PrintUsage(player);
                return;
            }

            if (worlds.Length == 0) {
                player.Message("There are no {0}.", listName);

            } else if (worlds.Length <= WorldNamesPerPage || player.IsSuper) {
                player.Message("  There are {0} {1}: {2}",
                                        worlds.Length, listName, worlds.JoinToClassyString());

            } else {
                if (offset < 0) offset = 0;
                if (offset >= worlds.Length) {
                    offset = Math.Max(0, worlds.Length - WorldNamesPerPage);
                }
                World[] worldsPart = worlds.Skip(offset).Take(WorldNamesPerPage).ToArray();
                player.Message("  {0}: {1}",
                                        listName.UppercaseFirst(), worldsPart.JoinToClassyString());

                if (offset + worldsPart.Length < worlds.Length) {
                    player.Message("Showing {0}-{1} (out of {2}). Next: &H/Worlds {3}{1}",
                                    offset + 1, offset + worldsPart.Length, worlds.Length, extraParam);
                } else {
                    player.Message("Showing worlds {0}-{1} (out of {2}).",
                                    offset + 1, offset + worldsPart.Length, worlds.Length);
                }
            }
        }

        #endregion
        #region WorldAccess / WorldBuild

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
            HandleWorldPerms(player, cmd, true, false);
        }

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
            HandleWorldPerms(player, cmd, true, true);
        }
        
        static void HandleWorldPerms([NotNull] Player player, CommandReader cmd, bool checkSelf, bool build) {
            if (player == null) throw new ArgumentNullException( "player" );
            string worldName = cmd.Next();
            string verb = build ? "modified" : "accessed";

            // Print information about the current world
            if (worldName == null) {
                if (player.World == null) {
                    player.Message("When calling /{0} from console, you must specify a world name.", build ? "WBuild" : "WAccess");
                } else {
                    SecurityController controller = build ? player.World.BuildSecurity : player.World.AccessSecurity;
                    player.Message(controller.GetDescription(player.World, "world", verb));
                }
                return;
            }

            // Find a world by name
            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if (world == null) return;

            // If no parameters were given, print info
            string token = cmd.Next();
            if (token == null) {
                SecurityController controller = build ? world.BuildSecurity : world.AccessSecurity;
                player.Message(controller.GetDescription(world, "world", verb));
                return;
            }

            // Deny adding access restrictions to main world(s)
            if (!build && world == WorldManager.MainWorld) {
                player.Message("The main world cannot have access restrictions.");
                return;
            }

            bool changesWereMade = false;
            do {
                bool madeChange = false;
                if (token.Equals("-*")) {
                    madeChange = ClearWhitelist(player, world, build);
                } else if (token.Equals( "+*")) {
                    madeChange = ClearBlacklist(player, world, build);
                } else if (token.StartsWith("+")) {
                    madeChange = WhitelistPlayer(token.Substring(1), checkSelf, player, world, build);
                } else if( token.StartsWith( "-" ) ) {
                    madeChange = BlacklistPlayer(token.Substring(1), player, world, build);
                } else {
                    madeChange = MinRank(token, player, world, build);
                }
                
                changesWereMade |= madeChange;
            } while( (token = cmd.Next()) != null );

            if (!changesWereMade) return;
            WorldManager.SaveWorldList();

            if (build) {
                if (BlockDB.IsEnabledGlobally && world.BlockDB.AutoToggleIfNeeded()) {
                    if (world.BlockDB.IsEnabled) {
                        player.Message("BlockDB is now auto-enabled on world {0}", world.ClassyName);
                    } else {
                        player.Message("BlockDB is now auto-disabled on world {0}", world.ClassyName);
                    }
                }
            } else {
                var playersWhoCantStay = world.Players.Where(p => !p.CanJoin(world));
                foreach(Player p in playersWhoCantStay ) {
                    p.Message( "&WYou are no longer allowed to join world {0}", world.ClassyName );
                    p.JoinWorld( WorldManager.FindMainWorld( p ), WorldChangeReason.PermissionChanged );
                }
            }
        }

        static bool ClearWhitelist(Player player, World world, bool build) {
            SecurityController controller = build ? world.BuildSecurity : world.AccessSecurity;
            PlayerInfo[] whitelist = controller.ExceptionList.Included.ToArray();
            string type = build ? "Build" : "Access";
            
            if (whitelist.Length > 0) {
                controller.ResetIncludedList();
                player.Message("{2} whitelist of world {0}&S cleared: {1}",
                               world.ClassyName, whitelist.JoinToClassyString(), type);
                Logger.Log(LogType.UserActivity,
                           "{0} {1} &Scleared {4} whitelist of world {2}: {3}",
                           player.Info.Rank.Name, player.Name, world.Name, 
                           whitelist.JoinToString(pi => pi.Name), type.ToLower());
                return true;
            } else {
                player.Message("{1} whitelist of world {0}&S is empty.", world.ClassyName, type);
                return false;
            }
        }
        
        static bool ClearBlacklist(Player player, World world, bool build) {
            SecurityController controller = build ? world.BuildSecurity : world.AccessSecurity;
            PlayerInfo[] blacklist = controller.ExceptionList.Excluded.ToArray();
            string type = build ? "Build" : "Access";
            
            if (blacklist.Length > 0) {
                controller.ResetExcludedList();
                player.Message("{2} blacklist of world {0}&S cleared: {1}",
                               world.ClassyName, blacklist.JoinToClassyString(), type);
                Logger.Log(LogType.UserActivity,
                           "{0} {1} &Scleared {4} blacklist of world {2}: {3}",
                           player.Info.Rank.Name, player.Name, world.Name, 
                           blacklist.JoinToString(pi => pi.Name), type.ToLower());
                return true;
            } else {
                player.Message("Build blacklist of world {0}&S is empty.", world.ClassyName, type);
                return false;
            }
        }
        
        static bool WhitelistPlayer(string token, bool checkSelf, Player player, World world, bool build) {
            PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches(player, token, SearchOptions.IncludeSelf);
            if (info == null) return false;
            SecurityController controller = build ? world.BuildSecurity : world.AccessSecurity;
            string type = build ? "build" : "access";
            string action = build ? "build in" : "access";
            string actionIng = build ? "building in" : "accessing";

            // prevent players from whitelisting themselves to bypass protection
            if (player.Info == info && checkSelf && !player.Info.Rank.AllowSecurityCircumvention) {
                switch (controller.CheckDetailed(player.Info)) {
                    case SecurityCheckResult.RankTooLow:
                        player.Message("&WYou must be {0}&W+ to add yourself to the {2} whitelist of {1}",
                                       controller.MinRank.ClassyName, world.ClassyName, type);
                        return false;
                    case SecurityCheckResult.BlackListed:
                        player.Message("&WYou cannot remove yourself from the {1} blacklist of {0}",
                                       world.ClassyName, type);
                        return false;
                }
            }

            if (controller.CheckDetailed( info ) == SecurityCheckResult.Allowed ) {
                player.Message("{0}&S is already allowed to {2} {1}&S (by rank)",
                               info.ClassyName, world.ClassyName, action);
                return false;
            }

            Player target = info.PlayerObject;
            if (target == player) target = null; // to avoid duplicate messages

            switch (controller.Include(info)) {
                case PermissionOverride.Deny:
                    if (controller.Check(info)) {
                        player.Message("{0}&S is no longer barred from {2} {1}",
                                       info.ClassyName, world.ClassyName, actionIng);
                        if (target != null) {
                            target.Message("You can now {2} world {0}&S (removed from blacklist by {1}&S).",
                                           world.ClassyName, player.ClassyName, action);
                        }
                    } else {
                        player.Message("{0}&S was removed from the {2} blacklist of {1}&S. " +
                                       "Player is still NOT allowed to {2} (by rank).",
                                       info.ClassyName, world.ClassyName, type);
                        if (target != null) {
                            target.Message("You were removed from the {2} blacklist of world {0}&S by {1}&S. " +
                                           "You are still NOT allowed to {2} (by rank).",
                                           world.ClassyName, player.ClassyName, type);
                        }
                    }
                    
                    Logger.Log(LogType.UserActivity, "{0} removed {1} from the {3} blacklist of {2}",
                               player.Name, info.Name, world.Name, type);
                    return true;

                case PermissionOverride.None:
                    player.Message("{0}&S is now allowed to {2} {1}",
                                   info.ClassyName, world.ClassyName, action);
                    if (target != null) {
                        target.Message("You can now {2} world {0}&S (whitelisted by {1}&S).",
                                       world.ClassyName, player.ClassyName, action);
                    }
                    
                    Logger.Log(LogType.UserActivity, "{0} added {1} to the {3} whitelist on world {2}",
                               player.Name, info.Name, world.Name, type);
                    return true;

                case PermissionOverride.Allow:
                    player.Message("{0}&S is already on the {2} whitelist of {1}",
                                   info.ClassyName, world.ClassyName, type);
                    return false;
            }
            return false;
        }
        
        static bool BlacklistPlayer(string token, Player player, World world, bool build) {
            PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches(player, token, SearchOptions.IncludeSelf);
            if (info == null) return false;
            SecurityController controller = build ? world.BuildSecurity : world.AccessSecurity;
            string type = build ? "build" : "access";
            string action = build ? "build in" : "access";
            string actionIng = build ? "building in" : "accessing";

            if (controller.CheckDetailed( info ) == SecurityCheckResult.RankTooLow) {
                player.Message( "{0}&S is already barred from {2} {1}&S (by rank)",
                               info.ClassyName, world.ClassyName, actionIng);
                return false;
            }

            Player target = info.PlayerObject;
            if (target == player) target = null; // to avoid duplicate messages

            switch (controller.Exclude(info)) {
                case PermissionOverride.Deny:
                    player.Message("{0}&S is already on {2} blacklist of {1}",
                                   info.ClassyName, world.ClassyName, type);
                    return false;

                case PermissionOverride.None:
                    player.Message("{0}&S is now barred from {2} {1}",
                                   info.ClassyName, world.ClassyName, actionIng);
                    if (target != null) {
                        target.Message("&WYou were barred by {0}&W from {2} world {1}",
                                       player.ClassyName, world.ClassyName, actionIng);
                    }
                    
                    Logger.Log(LogType.UserActivity,
                               "{0} added {1} to the {3} blacklist on world {2}",
                               player.Name, info.Name, world.Name, type);
                    return true;

                case PermissionOverride.Allow:
                    if (controller.Check(info)) {
                        player.Message("{0}&S is no longer on the {2} whitelist of {1}&S. " +
                                       "Player is still allowed to {2} (by rank).",
                                       info.ClassyName, world.ClassyName, type);
                        if (target != null) {
                            target.Message("You were removed from the {2} whitelist of world {0}&S by {1}&S. " +
                                           "You are still allowed to {2} (by rank).",
                                           world.ClassyName, player.ClassyName, type);
                        }
                    } else {
                        player.Message("{0}&S is no longer allowed to {2} {1}",
                                       info.ClassyName, world.ClassyName, action);
                        if (target != null) {
                            target.Message("&WYou can no longer {2} world {0}&W (removed from whitelist by {1}&W).",
                                           world.ClassyName, player.ClassyName, action);
                        }
                    }
                    
                    Logger.Log(LogType.UserActivity,
                               "{0} removed {1} from the {3} whitelist on world {2}",
                               player.Name, info.Name, world.Name, type);
                    return true;
            }
            return false;
        }
        
        static bool MinRank(string token, Player player, World world, bool build) {
            Rank rank = RankManager.FindRank(token);
            if (rank == null) { player.MessageNoRank(token); return false; }
            
            SecurityController controller = build ? world.BuildSecurity : world.AccessSecurity;
            string type = build ? "build" : "access";
            string action = build ? "build in" : "access";
            
            if (!player.Info.Rank.AllowSecurityCircumvention && controller.MinRank > rank 
                && controller.MinRank > player.Info.Rank ) {
                player.Message("&WYou must be ranked {0}&W+ to lower {2} restrictions for world {1}",
                               controller.MinRank.ClassyName, world.ClassyName, type);
                return false;
            }
            
            // list players who are redundantly blacklisted
            var exceptionList = controller.ExceptionList;
            PlayerInfo[] noLongerExcluded = exceptionList.Excluded.Where(excluded => excluded.Rank < rank).ToArray();
            if (noLongerExcluded.Length > 0) {
                player.Message("Following players no longer need to be blacklisted on world {0}&S: {1}",
                               world.ClassyName, noLongerExcluded.JoinToClassyString());
            }

            // list players who are redundantly whitelisted
            PlayerInfo[] noLongerIncluded = exceptionList.Included.Where(included => included.Rank >= rank ).ToArray();
            if (noLongerIncluded.Length > 0) {
                player.Message("Following players no longer need to be whitelisted on world {0}&S: {1}",
                               world.ClassyName, noLongerIncluded.JoinToClassyString() );
            }

            controller.MinRank = rank;
            if (controller.MinRank == RankManager.LowestRank ) {
                Server.Message("{0}&S allowed anyone to {2} world {1}",
                               player.ClassyName, world.ClassyName, action);
            } else {
                Server.Message("{0}&S allowed only {1}+&S to {3} world {2}",
                               player.ClassyName, controller.MinRank.ClassyName, world.ClassyName, action);
            }
            
            Logger.Log(LogType.UserActivity,
                       "{0} set {3} rank for world {1} to {2}+",
                       player.Name, world.Name, controller.MinRank.Name, type);
            return true;
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
                player.Message( "WFlush: {0}&S has no updates to process.",
                                   world.ClassyName );
            } else {
                player.Message( "WFlush: Flushing {0}&S ({1} blocks)...",
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
            
            if( world.Buildable && world.Deletable ) return;
            player.Message( "  Buildable: {0}   &SDeletable: {1}",
                           world.Buildable ? "&aYes" : "&cNo",
                           world.Deletable ? "&aYes" : "&cNo");
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


        private static void WorldLoadHandler(Player player, CommandReader cmd) {
            string fileName = cmd.Next();
            string worldName = cmd.Next();

            if (worldName == null && player.World == null) {
                player.Message("When using /WLoad from console, you must specify the world name.");
                return;
            }

            if (fileName == null) {
                // No params given at all
                CdWorldLoad.PrintUsage(player);
                return;
            }

            string fullFileName = WorldManager.FindMapFile(player, fileName);
            if (fullFileName == null) return;

            // Loading map into current world
            if (worldName == null) {
                WorldLoadReplace( cmd, player, player.World, fileName, fullFileName, false );
            } else {
                // Loading to some other (or new) world
                if (!World.IsValidName(worldName)) {
                    player.MessageInvalidWorldName(worldName);
                    return;
                }

                string buildRankName = cmd.Next();
                string accessRankName = cmd.Next();
                Rank buildRank = RankManager.DefaultBuildRank;
                Rank accessRank = null;
                if (buildRankName != null) {
                    buildRank = RankManager.FindRank(buildRankName);
                    if (buildRank == null) {
                        player.MessageNoRank(buildRankName);
                        return;
                    }
                    if (accessRankName != null) {
                        accessRank = RankManager.FindRank(accessRankName);
                        if (accessRank == null) {
                            player.MessageNoRank(accessRankName);
                            return;
                        }
                    }
                }

                // Retype world name, if needed
                if (worldName == "-") {
                    if (player.LastUsedWorldName != null) {
                        worldName = player.LastUsedWorldName;
                    } else {
                        player.Message("Cannot repeat world name: you haven't used any names yet.");
                        return;
                    }
                }

                lock (WorldManager.SyncRoot) {
                    World world = WorldManager.FindWorldExact(worldName);
                    if (world != null) {
                        player.LastUsedWorldName = world.Name;
                        WorldLoadReplace( cmd, player, world, fileName, fullFileName, false );
                    } else {
                        // Adding a new world
                        string targetFullFileName = Path.Combine(Paths.MapPath, worldName + ".fcm");
                        if (!cmd.IsConfirmed && File.Exists(targetFullFileName) && // target file already exists
                            !Paths.Compare(targetFullFileName, fullFileName)) { // and is different from sourceFile
                            Logger.Log(LogType.UserActivity,
                                       "WLoad: Asked {0} to confirm replacing map file \"{1}\" with \"{2}\"",
                                       player.Name,
                                       targetFullFileName,
                                       fullFileName);
                            player.Confirm(cmd,
                                           "A map named \"{0}\" already exists, and will be overwritten with \"{1}\".",
                                           Path.GetFileName(targetFullFileName),
                                           Path.GetFileName(fullFileName));
                            return;
                        }

                        Map map;
                        try {
                            map = MapUtility.Load(fullFileName);
                        } catch (Exception ex) {
                            player.Message("Could not load \"{0}\": {1}: {2}", fileName, ex.GetType().Name,
                                ex.Message);
                            return;
                        }

                        World newWorld = WorldLoadAdd( cmd, player, map, worldName, fileName, accessRank, buildRank );
                        if( newWorld == null ) return;
                        
                        Server.Message("{0}&S created a new world named {1}", player.ClassyName, newWorld.ClassyName);                     
                        WorldManager.SaveWorldList();
                        player.Message("Access is {0}+&S, and building is {1}+&S on {2}",
                            newWorld.AccessSecurity.MinRank.ClassyName, newWorld.BuildSecurity.MinRank.ClassyName,
                            newWorld.ClassyName);
                    }
                }
            }
            Server.RequestGC();
        }

        static World WorldLoadAdd( CommandReader cmd, Player player, Map map, string name, 
                                  string fileName, Rank accessRank, Rank buildRank) {
            World world;
            try {
                world = WorldManager.AddWorld(player, name, map, false);
            } catch (WorldOpException ex) {
                player.Message("WLoad: {0}", ex.Message);
                return null;
            }
            player.LastUsedWorldName = name;
            
            world.BuildSecurity.MinRank = buildRank;
            if (accessRank == null) {
                world.AccessSecurity.ResetMinRank();
            } else {
                world.AccessSecurity.MinRank = accessRank;
            }
            
            world.BlockDB.AutoToggleIfNeeded();
            if (BlockDB.IsEnabledGlobally && world.BlockDB.IsEnabled) {
                player.Message("BlockDB is now auto-enabled on world {0}", world.ClassyName);
            }
            
            world.LoadedBy = player.Name;
            world.LoadedOn = DateTime.UtcNow;
            
            Logger.Log(LogType.UserActivity,
                       "{0} {1} &Screated a new world named \"{2}\" (loaded from \"{3}\")",
                       player.Info.Rank.Name, player.Name, world.Name, fileName);
            return world;
        }
        
        static void WorldLoadReplace( CommandReader cmd, Player player, World world, 
                                     string fileName, string fullFileName, bool clear ) {
            // Replacing existing world's map
            string mapName = player.World == world ? "THIS MAP" : "map for " + world.ClassyName;
            if (!cmd.IsConfirmed) {
                Logger.Log(LogType.UserActivity, "WLoad: Asked {0} to confirm {2} the map of world {1}", 
                           player.Name, world.Name, clear ? "clearing" : "replacing");
                
                string actionMsg = clear ? "Clear {0}&S?" : "Replace {0}&S with \"{1}\"?";
                player.Confirm(cmd, actionMsg, mapName, fileName);
                return;
            }

            Map map;
            try {
                map = MapUtility.Load(fullFileName);
            } catch (Exception ex) {
                player.Message("Could not load specified file: {0}: {1}", ex.GetType().Name, ex.Message);
                if (clear) player.Message("Please use &H/WCS &Sfirst on an empty map to create a backup for clearing.");
                return;
            }

            try {
                world.MapChangedBy = player.Name;
                world.ChangeMap(map);
            } catch (WorldOpException ex) {
                Logger.Log(LogType.Error, "Could not complete WorldLoad operation: {0}", ex.Message);
                player.Message("&W{1}: {0}", ex.Message, clear ? "WClear" : "WLoad");
                return;
            }

            string action = clear ? "cleared" : "loaded a new";
            world.Players.Message(player, "{0}&S {1} map for this world", player.ClassyName, action);
            player.Message("{1} map for the world {0}", world.ClassyName, action.UppercaseFirst());
            Logger.Log(LogType.UserActivity, "{0} {1} &S{4} map for world \"{2}\" from \"{3}\"",
                       player.Info.Rank.Name, player.Name, world.Name, fileName, action);
        }

        #endregion
        #region WorldClear

        static readonly CommandDescriptor CdWorldClear = new CommandDescriptor
        {
            Name = "WorldClear",
            Aliases = new[] { "wclear" },
            Category = CommandCategory.New | CommandCategory.World,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WClear",
            Help = "Used to clear a map",
            Handler = WorldClearHandler
        };


        static void WorldClearHandler(Player player, CommandReader cmd)
        {
            string fullFileName = WorldManager.FindMapClearFile(player, player.World + "clear");
            if (fullFileName == null) return;
            WorldLoadReplace( cmd, player, player.World, 
                                Path.GetFileName(fullFileName), fullFileName, true );
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
                                world.ClassyName, world.AccessSecurity.MinRank.ClassyName, rank.ClassyName );
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

        private static void WorldRenameHandler(Player player, CommandReader cmd) {
            string oldName = cmd.Next();
            string newName = cmd.Next();
            if (oldName == null || newName == null) {
                CdWorldRename.PrintUsage(player);
                return;
            }

            World oldWorld = WorldManager.FindWorldOrPrintMatches(player, oldName);
            if (oldWorld == null) return;
            oldName = oldWorld.Name;
            if (oldName.CaselessStarts("pw_")) {
                player.Message("You cannot change playerworld names");
                return;
            }
            if (!World.IsValidName(newName)) {
                player.MessageInvalidWorldName(newName);
                return;
            }

            World newWorld = WorldManager.FindWorldExact(newName);

            if (newName.CaselessStarts("pw_")) {
                player.Message("You cannot make fake personal worlds.");
                return;
            }
            if (!cmd.IsConfirmed && newWorld != null && newWorld != oldWorld) {
                Logger.Log(LogType.UserActivity, "WRename: Asked {0} to confirm replacing world \"{1}\"", player.Name,
                    newWorld.Name);
                player.Confirm(cmd, "A world named {0}&S already exists. Replace it?", newWorld.ClassyName);
                return;
            }

            if (!cmd.IsConfirmed && Paths.FileExists(Path.Combine(Paths.MapPath, newName + ".fcm"), true)) {
                Logger.Log(LogType.UserActivity, "WRename: Asked {0} to confirm overwriting map file \"{1}.fcm\"",
                    player.Name, newName);
                player.Confirm(cmd, "Renaming this world will overwrite an existing map file \"{0}.fcm\".", newName);
                return;
            }

            try {
                WorldManager.RenameWorld(oldWorld, newName, true, true);
            } catch (WorldOpException ex) {
                switch (ex.ErrorCode) {
                    case WorldOpExceptionCode.NoChangeNeeded:
                        player.Message("WRename: World is already named \"{0}\"", oldName);
                        return;
                    case WorldOpExceptionCode.DuplicateWorldName:
                        player.Message("WRename: Another world named \"{0}\" already exists.", newName);
                        return;
                    case WorldOpExceptionCode.InvalidWorldName:
                        player.Message("WRename: Invalid world name: \"{0}\"", newName);
                        return;
                    case WorldOpExceptionCode.MapMoveError:
                        player.Message(
                            "WRename: World \"{0}\" was renamed to \"{1}\", but the map file could not be moved due to an error: {2}",
                            oldName, newName, ex.InnerException);
                        return;
                    default:
                        player.Message("&WWRename: Unexpected error renaming world \"{0}\": {1}", oldName, ex.Message);
                        Logger.Log(LogType.Error,
                            "WorldCommands.Rename: Unexpected error while renaming world {0} to {1}: {2}", oldWorld.Name,
                            newName, ex);
                        return;
                }
            }

            player.LastUsedWorldName = newName;
            Logger.Log(LogType.UserActivity, "{0} renamed the world \"{1}\" to \"{2}\".", player.Name, oldName, newName);
            Server.Message("{0}&S renamed the world \"{1}\" to \"{2}\"", player.ClassyName, oldName, newName);
            
            foreach (Entity e in Entity.AllIn(oldName)) {
                e.ChangeWorld(newName);
            }
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
            if( fileName.EndsWith( "/" ) || fileName.EndsWith( @"\" ) ) {
                fileName += world.Name + ".fcm";
            } else if( !fileName.CaselessEnds( ".fcm" ) ) {
                fileName += ".fcm";
            }
            
            string fullFileName = WorldSaveFullFileName( cmd, player, world, 
                                                        Paths.MapPath, "WorldSave", fileName );
            if( fullFileName == null ) return;

            player.Message( "Saving map to {0}", fileName );
            WorldSave( player, world, "WorldSave", fullFileName );
        }
        
        static string WorldSaveFullFileName( CommandReader cmd, Player player, World world, 
                                            string basePath, string cmdName, string fileName ) {
            if( !Paths.IsValidPath( fileName ) ) {
                player.Message( "Invalid file name." );
                return null;
            }
            
            string fullFileName = Path.Combine( basePath, fileName );
            if( !Paths.Contains( basePath, fullFileName ) ) {
                player.MessageUnsafePath();
                return null;
            }

            // Ask for confirmation if overwriting
            if( !File.Exists( fullFileName ) ) return fullFileName;
            
            FileInfo target = new FileInfo( fullFileName );
            FileInfo source = new FileInfo( world.MapFileName );
            if ( target.FullName.CaselessEquals( source.FullName ) ) return fullFileName;
            if ( cmd.IsConfirmed ) return fullFileName;
            
            Logger.Log(LogType.UserActivity,
                       cmdName + ": Asked {0} to confirm overwriting map file \"{1}\"",
                       player.Name, target.FullName);
            player.Confirm(cmd, "Target file \"{0}\" already exists, and will be overwritten.", target.Name);
            return null;
        }
        
        static void WorldSave( Player player, World world, string cmd, string fullFileName ) {
            // Create the target directory if it does not exist
            string dirName = fullFileName.Substring( 0, fullFileName.LastIndexOf( Path.DirectorySeparatorChar ) );
            if( !Directory.Exists( dirName ) ) {
                Directory.CreateDirectory( dirName );
            }
            
            const string mapSavingErrorMessage = "Map saving failed. See server logs for details.";
            Map map = world.Map;
            if( map == null ) {
                if( File.Exists( world.MapFileName ) ) {
                    try {
                        File.Copy( world.MapFileName, fullFileName, true );
                    } catch( Exception ex ) {
                        Logger.Log( LogType.Error,
                                    "WorldCommands." + cmd + ": Error occurred while trying to copy an unloaded map: {0}", ex );
                        player.Message( mapSavingErrorMessage );
                    }
                } else {
                    Logger.Log( LogType.Error,
                                "WorldCommands." + cmd + ": Map for world \"{0}\" is unloaded, and file does not exist.",
                                world.Name );
                    player.Message( mapSavingErrorMessage );
                }
            } else if( map.Save( fullFileName ) ) {
                player.Message( "Map saved succesfully." );
            } else {
                Logger.Log( LogType.Error,
                            "WorldCommands." + cmd + ": Saving world \"{0}\" failed.", world.Name );
                player.Message( mapSavingErrorMessage );
            }            
        }

        #endregion
        #region WorldClearSave

        static readonly CommandDescriptor CdWorldClearSave = new CommandDescriptor
        {
            Name = "WorldClearSave",
            Aliases = new[] { "wcs" },
            Category = CommandCategory.New | CommandCategory.World,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WCS",
            Help = "Saves a map copy to be used with /WClear",
            IsConsoleSafe = false,
            Handler = WorldClearSaveHandler
        };

        static void WorldClearSaveHandler(Player player, CommandReader cmd) {
            if (player.World == null) { CdWorldSave.PrintUsage(player); return; }

            World world = player.World;
            string fileName = world.Name;

            // normalize the path
            fileName = fileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            fileName = "World(" + fileName + ")clear.fcm";
            if (!Paths.IsValidPath(fileName)) {
                player.Message("Invalid file name.");
                return;
            }
            
            string fullFileName = WorldSaveFullFileName( cmd, player, world, 
                                                        Paths.WClearPath, "WClearSave", fileName );
            if( fullFileName == null ) return;

            player.Message("Saving map to {0}", fileName);
            WorldSave(player, world, "WClearSave", fullFileName);
        }

        #endregion
        #region WorldSet

        static readonly CommandDescriptor CdWorldSet = new CommandDescriptor {
            Name = "WSet",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/WSet <World> <Variable> <Value>",
            Help = "Sets a world variable. Variables are: hide, backups, greeting, motd, buildable, deletable",
            HelpSections = new Dictionary<string, string>{
                { "hide",       "&H/WSet <WorldName> Hide On/Off&N&S" +
                                "When a world is hidden, it does not show up on the &H/Worlds&S list. It can still be joined normally." },
                { "backups",    "&H/WSet <World> Backups Off&S, &H/WSet <World> Backups Default&S, or &H/WSet <World> Backups <Time>&N&S" +
                                "Enables or disables periodic backups. Time is given in the compact format." },
                { "greeting",   "&H/WSet <WorldName> Greeting <Text>&N&S" +
                                "Sets a greeting message. Message is shown whenever someone joins the map, and can also be viewed in &H/WInfo" },
                { "motd",        "&H/WSet <WorldName> Motd <Text>&N&S" +
                                "Sets a message to be shown when joining/Loading a map." },
                { "buildable",  "&H/WSet <WorldName> Buildable On/Off&N&S" +
                                "Whether any blocks can be placed by players in the world." },
                { "deletable",  "&H/WSet <WorldName> Deletable On/Off&N&S" +
                                "Whether any blocks can be deleted by players in the world." },
                { "maxreach",  "&H/WSet <WorldName> MaxReach <Distance>/reset&N&S" +
                                "Sets maximum reach distance players may click/reach up to." },
            },
            Handler = WorldSetHandler
        };

        static void WorldSetHandler( Player player, CommandReader cmd ) {
            string worldName = cmd.Next(), varName = cmd.Next();
            string value = cmd.NextAll();
            if (worldName == null || varName == null) {
                CdWorldSet.PrintUsage(player);
                return;
            }

            World world = WorldManager.FindWorldOrPrintMatches(player, worldName);
            if (world == null) return;

            switch (varName.ToLower()) {
                case "hide":
                case "hidden":
                    SetWorldBool(player, world, value, world.IsHidden, "hidden",
                                 v => world.IsHidden = v);
                    break;
                case "build":
                case "buildable":
                    SetWorldBool(player, world, value, world.Buildable, "buildable",
                                 v => { world.Buildable = v; UpdateBlockPerms(world); } );
                    break;
                case "delete":
                case "deletable":
                    SetWorldBool(player, world, value, world.Deletable, "deletable",
                                 v => { world.Deletable = v; UpdateBlockPerms(world); } );
                    break;
                    
                case "backup":
                case "backups":
                    SetBackupSettings(player, world, value); break;
                case "description":
                case "greeting":
                    SetGreeting(player, world, value); break;
                case "messageoftheday":
                case "motd":
                    SetMOTD(player, world, value); break;
                case "mrd":
                case "maxreach":
                case "maxreachdistance":
                    SetMaxReach(player, world, value); break;
                default:
                    CdWorldSet.PrintUsage(player); break;
            }
        }
        
        static void UpdateBlockPerms(World world) {
            Player[] players = world.Players;
            foreach (Player pl in players) {
                if (!pl.Supports(CpeExt.BlockPermissions)) continue;
                pl.SendBlockPermissions();
            }
        }

        static void SetWorldBool(Player player, World world, string value,
                                 bool curValue, string type, Action<bool> setter) {
            if (String.IsNullOrEmpty(value)) {
                player.Message("World {0}&S is currently {1}{2}.",
                               world.ClassyName, curValue ? "" : "NOT ", type);
            } else if (value.CaselessEquals("on") || value.CaselessEquals("true") || value == "1") {
                if (curValue) {
                    player.Message("World {0}&S is already {1}.", world.ClassyName, type);
                } else {
                    player.Message("World {0}&S is now {1}.", world.ClassyName, type);
                    setter(true);
                    WorldManager.SaveWorldList();
                }
            } else if (value.CaselessEquals("off") || value.CaselessEquals("false") || value == "0") {
                if (curValue) {
                    player.Message("World {0}&S is no longer {1}.", world.ClassyName, type);
                    setter(false);
                    WorldManager.SaveWorldList();
                } else {
                    player.Message( "World {0}&S is not {1}.", world.ClassyName, type);
                }
            } else {
                CdWorldSet.PrintUsage( player );
            }
        }
        
        static void SetBackupSettings(Player player, World world, string value) {
            TimeSpan backupInterval;
            string oldDescription = world.BackupSettingDescription;
            if (String.IsNullOrEmpty(value)) {
                player.Message(GetBackupSettingsString(world));
                return;
            } else if (value.CaselessEquals("off") || value.CaselessStarts("disable")) {
                // Disable backups on the world
                if(world.BackupEnabledState == YesNoAuto.No) {
                    MessageSameBackupSettings(player, world);
                    return;
                }
                world.BackupEnabledState = YesNoAuto.No;
            } else if (value.CaselessEquals("default") || value.CaselessEquals("auto")) {
                // Set world to use default settings
                if (world.BackupEnabledState == YesNoAuto.Auto) {
                    MessageSameBackupSettings(player, world);
                    return;
                }
                world.BackupEnabledState = YesNoAuto.Auto;
            } else if (value.TryParseMiniTimespan(out backupInterval)) {
                if (backupInterval == TimeSpan.Zero ) {
                    // Set world's backup interval to 0, which is equivalent to disabled
                    if (world.BackupEnabledState == YesNoAuto.No) {
                        MessageSameBackupSettings(player, world);
                        return;
                    }
                    world.BackupEnabledState = YesNoAuto.No;
                } else if(world.BackupEnabledState != YesNoAuto.Yes ||
                          world.BackupInterval != backupInterval) {
                    // Alter world's backup interval
                    world.BackupInterval = backupInterval;
                } else {
                    MessageSameBackupSettings(player, world);
                    return;
                }
            } else {
                CdWorldSet.PrintUsage(player);
                return;
            }
            player.Message("Backup setting for world {0}&S changed from \"{1}\" to \"{2}\"",
                           world.ClassyName, oldDescription, world.BackupSettingDescription);
            WorldManager.SaveWorldList();
        }
        
        static void MessageSameBackupSettings(Player player, World world) {
            player.Message("Backup settings for {0}&S are already \"{1}\"",
                            world.ClassyName, world.BackupSettingDescription);
        }

        static string GetBackupSettingsString(World world) {
            switch (world.BackupEnabledState) {
                case YesNoAuto.Yes:
                    return String.Format("World {0}&S is backed up every {1}",
                                          world.ClassyName,
                                          world.BackupInterval.ToMiniString());
                case YesNoAuto.No:
                    return String.Format("Backups are manually disabled on {0}&S",
                                          world.ClassyName);
                case YesNoAuto.Auto:
                    if (World.DefaultBackupsEnabled) {
                        return String.Format("World {0}&S is backed up every {1} (default)",
                                              world.ClassyName,
                                              World.DefaultBackupInterval.ToMiniString());
                    } else {
                        return String.Format("Backups are disabled on {0}&S (default)",
                                              world.ClassyName);
                    }
                default:
                    // never happens
                    throw new Exception("Unexpected BackupEnabledState value: " + world.BackupEnabledState);
            }
        }
        
        static void SetMOTD(Player player, World world, string value) {
            if (string.IsNullOrEmpty(value)) {
                if (string.IsNullOrEmpty(world.MOTD)) {
                    player.Message("World \"{0}\" does not have a custom MOTD", world.Name);
                } else {
                    player.Message("MOTD for \"{0}\" is: ", world.Name);
                    player.Message("  " + world.MOTD);
                }
                return;
            }            
            if (value.Length > Packet.StringSize)
                value = value.Substring(0, Packet.StringSize);
            
            if (value.CaselessEquals("remove") || value.CaselessEquals("delete") || value.CaselessEquals("reset")) {
                player.Message("MOTD for \"{0}\" has been removed", world.Name);
                world.MOTD = null;
            } else {
                player.Message("MOTD for \"{0}\" has been set to:", world.Name);
                player.Message("  " + value);
                world.MOTD = value;
            }
            
            Player[] players = world.Players;
            foreach (Player pl in players) {
                if (pl.Supports(CpeExt.HackControl)) {
                    pl.Send(PlayerHacks.MakePacket(pl, world.MOTD));
                }
                if (pl.Supports(CpeExt.InstantMOTD)) {
                    string motd = world.MOTD ?? "";
                    pl.Send(Packet.MakeHandshake(pl, ConfigKey.ServerName.GetString(), motd));
                }
            }
            WorldManager.SaveWorldList();
        }
        
        static void SetGreeting(Player player, World world, string value) {
            if (!Directory.Exists(Paths.WGreetingsPath))
                Directory.CreateDirectory(Paths.WGreetingsPath);            
            string greetingsPath = Path.Combine(Paths.WGreetingsPath, world.Name + ".txt");
            
            if (String.IsNullOrEmpty(value)) {
                if (world.Greeting == null) {
                    if (File.Exists(greetingsPath)) {
                        world.Greeting = File.ReadAllText(greetingsPath);
                        if (world.Greeting.Length == 0)
                            player.Message("No greeting message is set for world {0}", world.ClassyName);
                        else
                            player.Message("Greeting message for world {0}&S is: {1}", world.ClassyName, world.Greeting);
                        world.Greeting = null;
                    } else {
                        player.Message("No greeting message is set for world {0}", world.ClassyName);
                    }
                }
            } else if (value.CaselessEquals("remove")) {
                player.Message("Greeting message removed for world {0}", world.ClassyName);
                if (File.Exists(greetingsPath))
                    File.Delete(greetingsPath);
                world.Greeting = null;
            } else {
                world.Greeting = value;
                player.Message("Greeting message for world {0}&S set to: {1}", world.ClassyName, world.Greeting);
                File.WriteAllText(greetingsPath, world.Greeting);
                world.Greeting = null;
            }
        }
        
        static void SetMaxReach(Player player, World world, string value) {
            short dist = world.MaxReach;
            if (String.IsNullOrEmpty(value)) {
                if (dist == -1) {
                    player.Message("Max reach distance for world {0}&S currently not set", world.ClassyName);
                } else {
                    player.Message("Max reach distance for world {0}&S currently &f{1} &S(&f{2}&S blocks)",
                                   world.ClassyName, dist, dist / 32);
                }
                return;
            }
            
            if (value.CaselessEquals("normal") || value.CaselessEquals("reset") || value.CaselessEquals("default")) {
                dist = -1;
            } else if (!short.TryParse(value, out dist)) {
                player.Message("Invalid distance!");
                return;
            }
            
            player.Message("Max reach distance for world {0}&S set to &f{1} &S(&f{2}&S blocks)", 
                           world.ClassyName, dist, dist / 32);
            world.MaxReach = dist;
            WorldManager.SaveWorldList();
            
            foreach (Player p in world.Players) {
                if (!p.Supports(CpeExt.ClickDistance)) continue;
                p.Send(Packet.MakeSetClickDistance(p.ReachDistance));
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
                        player.Message( "&WWorld {0}&W is set as the main world. " +
                                           "Assign a new main world before deleting this one.",
                                           world.ClassyName );
                        return;
                    case WorldOpExceptionCode.WorldNotFound:
                        player.Message( "&WWorld {0}&W is already unloaded.",
                                           world.ClassyName );
                        return;
                    default:
                        player.Message( "&WUnexpected error occurred while unloading world {0}&W: {1}",
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
            Category = CommandCategory.New | CommandCategory.World,
            Permissions = new Permission[] { Permission.ReadStaffChat },
            IsConsoleSafe = false,
            IsHidden = true,
            Usage = "/ctf <start / stop / redspawn / bluespawn / redflag / blueflag / swapteam>",
            Help = "Allows starting CTF / editing CTF properties. List of properties:&N" +
                "Start, Stop, RedSpawn, BlueSpawn, RedFlag, BlueFlag, SwapTeam&N" +
                "For detailed help see &H/Help CTF <Property>",
            HelpSections = new Dictionary<string, string>{
                { "start",             "&H/CTF start&N&S" +
                        "Starts a CTF game on the current world of the player." },
                { "stop",             "&H/CTF stop&N&S" +
                        "Stops the current CTF game. You needn't be in the same world." },
                { "redspawn",       "&H/CTF redspawn&N&S" +
                        "Sets the spawn of red team to your current position.&N" +
                        "Note that spawn positions are reset after the game is stopped."},
                { "bluespawn",       "&H/CTF bluespawn&N&S" +
                        "Sets the spawn of blue team to your current position.&N" +
                        "Note that spawn positions are reset after the game is stopped."},
                { "redflag",           "&H/CTF redflag&N&S" +
                        "Sets the position of the red flag to your current position.&N" +
                        "Note that flag positions are reset after the game is stopped."},
                { "blueflag",       "&H/CTF blueflag&N&S" +
                        "Sets the position of the blue flag to your current position.&N" +
                        "Note that flag positions are reset after the game is stopped."},
                { "swapteam",       "&H/CTF swapteam&N&S" +
                        "Switches your team in the CTF game."}
            },
            Handler = CTFHandler
        };
        
        static void CtfGuestHandler(Player player, string options) {
            switch (options) {
                case "start":
                    player.Message("You need to be a Moderator to start CTF Games.");
                    break;
                case "stop":
                    player.Message("You need to be a Moderator to stop CTF Games.");
                    break;
                case "redspawn":
                    player.Message("You need to be a Moderator to adjust CTF Spawns.");
                    break;
                case "bluespawn":
                    player.Message("You need to be a Moderator to adjust CTF Spawns.");
                    break;
                case "redflag":
                    player.Message("You need to be a Moderator to adjust CTF Flags.");
                    break;
                case "blueflag":
                    player.Message("You need to be a Moderator to adjust CTF Flags.");
                    break;
                    
                case "swapteam":
                case "switchteam":
                case "swap":
                case "changeteam":
                case "changesides":
                case "switch":
                    if (player.Team == CTF.BlueTeam) {
                        if ((CTF.BlueTeam.Count - CTF.RedTeam.Count) >= 1)
                            CTF.SwitchTeamTo(player, CTF.RedTeam, false);
                        else
                            player.Message("You cannot switch teams. The teams would become too unbalanced!");
                    } else if (player.Team == CTF.RedTeam) {
                        
                        if ((CTF.RedTeam.Count - CTF.BlueTeam.Count) >= 1)
                            CTF.SwitchTeamTo(player, CTF.BlueTeam, false);
                        else
                            player.Message("You cannot switch teams. The teams would become too unbalanced!");
                    }
                    break;
                default:
                    CdCTF.PrintUsage(player);
                    break;
            }
        }
        
        static void CtfStaffHandler(Player player, string options) {
            switch (options) {
                case "start":
                    if (!CTF.GameRunning) {
                        if (!player.World.Name.CaselessEquals("ctf")) {
                            player.Message("You can only start a game on the CTF map.");
                        } else {
                            player.Message("Started CTF game on world {0}", player.World.ClassyName);
                            CTF.Start(player, player.World);
                        }
                    } else {
                        player.Message("Game is already in progress. Type /ctf stop to stop the game.");
                    }
                    break;

                case "stop":
                    if (CTF.GameRunning) {
                        player.Message("Stopped CTF game.");
                        CTF.Stop();
                    } else {
                        player.Message("No CTF game running.");
                    }
                    break;

                case "redspawn":
                    player.Message("Red team's spawn set to {0}.", player.Position.ToBlockCoordsRaw());
                    CTF.RedTeam.Spawn = player.Position;
                    break;
                case "bluespawn":
                    player.Message("Blue team's spawn set to {0}", player.Position.ToBlockCoordsRaw());
                    CTF.BlueTeam.Spawn = player.Position;
                    break;
                case "redflag":
                    CTF.RedTeam.FlagPos = player.Position.ToBlockCoordsRaw();
                    CTF.map.QueueUpdate(new BlockUpdate(Player.Console,
                                                        CTF.RedTeam.FlagPos, CTF.RedTeam.FlagBlock));
                    player.Message("Red flag positon set to {0}", player.Position.ToBlockCoordsRaw());
                    break;
                case "blueflag":
                    CTF.BlueTeam.FlagPos = player.Position.ToBlockCoordsRaw();
                    CTF.map.QueueUpdate(new BlockUpdate(Player.Console,
                                                        CTF.BlueTeam.FlagPos, CTF.BlueTeam.FlagBlock));
                    player.Message("Blue flag positon set to {0}", player.Position.ToBlockCoordsRaw());
                    break;
                case "swapteam":
                case "switchteam":
                case "swap":
                case "changeteam":
                case "changesides":
                case "switch":
                    if (player.Team == CTF.BlueTeam) {
                        bool unbalanced = (CTF.BlueTeam.Count - CTF.RedTeam.Count) >= 1;
                        CTF.SwitchTeamTo(player, CTF.RedTeam, unbalanced);
                    } else if (player.Team == CTF.RedTeam) {
                        bool unbalanced = (CTF.RedTeam.Count - CTF.BlueTeam.Count) >= 1;
                        CTF.SwitchTeamTo(player, CTF.BlueTeam, unbalanced);
                    }
                    break;
                default:
                    CdCTF.PrintUsage(player);
                    break;
            }
        }

        static void CTFHandler(Player player, CommandReader cmd) {
            if (!cmd.HasNext)  {
                CdCTF.PrintUsage(player);
                return;
            }
            string Options = cmd.Next().ToLower();
            World world = player.World;
            if (world == WorldManager.MainWorld) {
                player.Message("/ctf cannot be used on the main world");
                return;
            }
            
            if (player.IsStaff)
                CtfStaffHandler(player, Options);
            else
                CtfGuestHandler(player, Options);
        }
        
        #endregion
        #region MyPersonalWorld

        static readonly CommandDescriptor CdMyWorld = new CommandDescriptor {
            Name = "PersonalWorld",
            Aliases = new[] { "pw" },
            Category = CommandCategory.World | CommandCategory.New,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Chat },
            Usage = "/PW [option] [args]",
            Help = "Allows players to have their own personal world. List of options:&N" +
                "BuildAccess, Create, Delete, Join, JoinAccess, List, Reset&N" +
                "For detailed help see &H/Help PW <Option>",
            HelpSections = new Dictionary<string, string>{
                { "create",     "&H/PW Create [size]&N" +
                                "&SCreates a personal world with a specified size:&N" +
                                "&bTiny/64 &f- &aNormal/128 &f- &SLarge/256 &f- &cHuge/512" },
                { "reset",      "&H/PW reset [number]&N" +
                                "&SResets your specified world back to when you created it.&N" +
                                "&cCan't be undone!" },
                { "delete",     "&H/PW delete [number]&N" +
                                "&SDeleted your specified world so you can make a new one.&N" +
                                "&cCan't be undone!" },
                { "join",       "&H/PW join [number] [player]&N" +
                                "&SJoins your specified world or the specified world of the specified player if you have joining rights."},
                { "buildaccess","&H/PW buildaccess +/-[Player] [number]&N" +
                                "&SAdds/Removed a specified players building rights on your specified world."},
                { "joinaccess", "&H/PW joinaccess +/-[Player] [number]&N" +
                                "&SAdds/Removed a specified players joining rights on your specified world."},
                { "list",       "&H/PW list&N" +
                                "&SLists all your personal worlds. And the ones you have access to."}
            },
            Handler = MWHandler
        };

        static void MWHandler(Player player, CommandReader cmd) {
            switch (cmd.Next()) {
                case "create":
                case "c":
                    MWCreate(player, cmd); break;
                case "reset":
                case "r":
                    MWReset(player, cmd); break;
                case "delete":
                case "d":
                case "remove":
                    MWDelete(player, cmd); break;
                case "j":
                case "join":                    
                    MWJoin(player, cmd); break;
                case "buildaccess":
                case "ba":
                    MWAccess(player, cmd, true); break;
                case "ja":
                case "joinaccess":
                    MWAccess(player, cmd, false); break;
                case "l":
                case "list":
                    MWList(player, cmd); break;
                default:
                    CdMyWorld.PrintUsage(player); break;
            }
        }
        
        static void MWCreate(Player player, CommandReader cmd) {
            string sizeName = cmd.Next();
            int size;
            if (sizeName == null)
                sizeName = "Normal";
            switch (sizeName.ToLower()) {
                case "64":
                case "tiny":
                    size = 64; sizeName = "Tiny"; break;
                case "128":
                case "normal":
                    size = 128; sizeName = "Normal"; break;
                case "256":
                case "large":
                    size = 256; sizeName = "Large"; break;
                case "512":
                case "huge":
                    size = 512; sizeName = "Huge"; break;
                default:
                    size = 128; sizeName = "Normal"; break;
            }
            
            World[] curRealms = WorldManager.FindWorlds(Player.Console, "PW_" + player.Name + "_");
            if (curRealms.Length >= player.Info.Rank.MaxPersonalWorlds && player.Info.Rank != RankManager.HighestRank) {
                player.Message("You can only have a maximum of {0} personal worlds. Sorry!",
                               player.Info.Rank.MaxPersonalWorlds);
                return;
            }

            string mapName = "PW_" + player.Name + "_" + (curRealms.Length + 1);
            player.Message("Creating your {0}({1}) personal world: {2}", sizeName, size, mapName);
            Map map = MapGenerator.GenerateFlatgrass(size, size, size);
            Server.RequestGC();
            if (map.Save(Path.Combine(Paths.MapPath, mapName + ".fcm")))
                player.Message("Done! Saved to {0}.fcm", mapName);
            else
                player.Message("&WAn error occurred while saving generated map to {0}.fcm", mapName);
                
            Rank rank = RankManager.HighestRank;
            lock (WorldManager.SyncRoot) {
                World newWorld = WorldLoadAdd(cmd, player, map, mapName, mapName + ".fcm", rank, rank );
                if (newWorld == null) return;
                
                newWorld.IsHidden = true;
                newWorld.AccessSecurity.Include(player.Info);
                newWorld.BuildSecurity.Include(player.Info);
                WorldManager.SaveWorldList();
            }
            Server.RequestGC();
        }
        
        static void MWReset(Player player, CommandReader cmd) {
            int num = 0; ReadMWNumber(cmd, out num);
            string mapName = "PW_" + player.Name + "_" + num;
            string map = WorldManager.FindMapFile(Player.Console, mapName);
            if (map == null) {
                player.Message("You have no personal worlds by that number: {0}", num); return;
            }
            if (!cmd.IsConfirmed) {
                player.Confirm(cmd, "This will reset your personal world:   " + mapName +
                               "&N&cThis cannot be undone!");
                return;
            }
            
            World world = WorldManager.FindWorldExact(mapName);
            Vector3I dims = world.GetOrLoadDimensions();
            
            Map newMap = MapGenerator.GenerateFlatgrass(dims.X, dims.Y, dims.Z);
            world.MapChangedBy = player.Name;
            world.ChangeMap(newMap);
            player.Message("Your personal world({0}) has been reset to flatgrass!", num);
            Server.RequestGC();
        }
        
        static void MWDelete(Player player, CommandReader cmd) {
            int num = 0; ReadMWNumber(cmd, out num);
            string mapName = "PW_" + player.Name + "_" + num;
            string map = WorldManager.FindMapFile(Player.Console, mapName);
            if (map == null) {
                player.Message("You have no personal worlds by that number: {0}", num); return;
            }
            if (!cmd.IsConfirmed) {
                player.Confirm(cmd, "This will delete your personal world:   " + mapName +
                               "&N&cThis cannot be undone!");
                return;
            }
            World world = WorldManager.FindWorldExact(mapName);
            if (world != null) WorldManager.RemoveWorld(world);
            
            mapName = Path.Combine(Paths.MapPath, mapName + ".fcm");
            if (File.Exists(mapName)) File.Delete(mapName);
            player.Message("Your personal world({0}) has been deleted!", num);
            Server.RequestGC();
        }
        
        static void MWJoin(Player player, CommandReader cmd) {
            PlayerInfo info = player.Info;
            // Args are supposed to be "<map number> <player>", but handle if user provides <player> first
            if (cmd.HasNext) {
                string name = cmd.Next();
                int temp = 0;
                
                if (!int.TryParse(name, out temp)) {
                    info = PlayerDB.FindPlayerInfoOrPrintMatches(player, name, SearchOptions.Default);
                    if (info == null) return;
                } else {
                    cmd.Rewind(); // user provided number, rewind
                    cmd.Next(); // Skip 'join' arg
                }
            }
            
            int num = 0; ReadMWNumber(cmd, out num);
            if (info == player.Info && cmd.HasNext) {
                info = PlayerDB.FindPlayerInfoOrPrintMatches(player, cmd.Next(), SearchOptions.Default);
                if (info == null) return;
            }
            
            string mapName = "PW_" + info.Name + "_" + num;
            string map = WorldManager.FindMapFile(Player.Console, mapName);
            if (map == null) {
                player.Message("{0} no personal worlds by that number: {1}", (info == player.Info) ? "You have" : "There are", num);
                return;
            }
            
            World world = WorldManager.FindWorldExact(mapName);
            if (world != null && player.CanJoin(world)) {
                player.JoinWorld(world, WorldChangeReason.ManualJoin);
            } else {
                player.Message("You cannot join that world!");
            }
        }
        
        static void MWList(Player player, CommandReader cmd) {
            string mapName = "PW_" + player.Name + "_";
            World[] own = WorldManager.Worlds.Where(w => w.Name.StartsWith(mapName)).ToArray();
            World[] others =
                WorldManager.Worlds.Where( w =>
                    w.Name.StartsWith("PW_") && w.AccessSecurity.Check(player.Info) &&
                    !w.Name.StartsWith(mapName)).ToArray();
            
            if (own.Length > 0) {
                player.Message("Your personal worlds: {0}", own.JoinToClassyString());
            }
            if (others.Length > 0) {
                player.Message("Player personal worlds you have access to: {0}",
                               others.JoinToClassyString());
            }
            if (own.Length == 0 && others.Length == 0) {
                player.Message("You do not have access to any personal worlds.");
            }
        }
        
        static void MWAccess(Player player, CommandReader cmd, bool build) {
            int num = 0;
            if (!ReadMWNumber(cmd, out num))
                cmd.Rewind();
            string mapName = "PW_" + player.Name + "_" + num;
            
            if (build) {
                cmd = new CommandReader("/" + CdWorldBuild.Name + " " + mapName + " " + cmd.NextAll());
                HandleWorldPerms(player, cmd, false, true);
            } else {
                cmd = new CommandReader("/" + CdWorldAccess.Name + " " + mapName + " " + cmd.NextAll());
                HandleWorldPerms(player, cmd, false, false);
            }
        }
        
        static bool ReadMWNumber(CommandReader cmd, out int number) {
            if (!int.TryParse(cmd.Next(), out number)) {
                number = 1;
                return false;
            }
            return true;
        }

        #endregion       
        #region MaxPersonalWorlds

        static readonly CommandDescriptor CdMaxPW = new CommandDescriptor {
            Name = "MaxPWorlds",
            Aliases = new[] { "MPW" },
            Permissions = new[] { Permission.Chat },
            Category = CommandCategory.New | CommandCategory.Moderation | CommandCategory.Chat,
            Help = "Changes/Displays the max amount of personal worlds a rank may have.",
            Usage = "/MPW <Rank> <Amount>",
            Handler = MaxPWHandler
        };

        private static void MaxPWHandler(Player player, CommandReader cmd) {
            string rname = cmd.Next();
            string rmax = cmd.Next();
            Rank prank = player.Info.Rank;
            if (player.Info.Rank == RankManager.HighestRank) {
                if (rname == null) {
                    player.Message("Rank ({0}&S) has a max of {1} personal worlds", prank.ClassyName,
                        prank.MaxPersonalWorlds);
                    return;
                }
                Rank rank = RankManager.FindRank(rname);
                if (rank == null) {
                    player.MessageNoRank(rname);
                    return;
                }
                if (rmax == null) {
                    player.Message("Rank ({0}&S) has a max of {1} personal worlds.", rank.ClassyName,
                        rank.MaxPersonalWorlds);
                    return;
                }
                int max;
                if (!int.TryParse(rmax, out max)) {
                    CdMaxPW.PrintUsage(player);
                    return;
                }
                if (rank != null) {
                    rank.MaxPersonalWorlds = max;
                    player.Message("Set MaxPersonalWorlds for rank ({0}&S) to {1} personal worlds.", rank.ClassyName,
                        rank.MaxPersonalWorlds);
                    Config.Save();
                }
            } else {
                player.Message("Rank ({0}&S) has a max of {1} personal worlds.", prank.ClassyName,
                    prank.MaxPersonalWorlds);
                return;
            }
        }

        #endregion
        #region portals
        static Block[] validPBlocks = {
            Block.Sapling, Block.Water, Block.StillWater,
            Block.Lava, Block.StillLava, Block.YellowFlower,
            Block.RedFlower, Block.BrownMushroom, Block.RedMushroom,
            Block.Rope, Block.Fire, Block.Air
        };

        static readonly CommandDescriptor CdPortal = new CommandDescriptor {
            Name = "portal",
            Aliases = new[] { "portals" },
            Category = CommandCategory.World | CommandCategory.New,
            Permissions = new Permission[] { Permission.Chat },
            IsConsoleSafe = false,
            Usage = "/portal [create | remove | info | list | enable | disable ]",
            Help = "Controls portals, options are: create, remove, list, info, enable, disable&N&S" +
                "See &H/Help portal <option>&S for details about each option.",
            HelpSections = new Dictionary<string, string>() {
                { "create",     "&H/portal create [world] [liquid] [portal name] ([x y z r l] or [#ZoneName])&N&S" +
                        "Creates a portal with specified options"},
                { "remove",     "&H/portal remove [portal name]&N&S" +
                        "Removes specified portal."},
                { "list",       "&H/portal list&N&S" +
                        "Gives you a list of portals in the current world."},
                { "info",       "&H/portal info [portal name]&N&S" +
                        "Gives you information of the specified portal."},
                { "enable",     "&H/portal enable&N&S" +
                        "Enables the use of portals, this is player specific."},
                { "disable",     "&H/portal disable&N&S" +
                        "Disables the use of portals, this is player specific."},
            },
            Handler = PortalH
        };

        private static void PortalH(Player player, CommandReader cmd) {
            string option = cmd.Next();
            if (string.IsNullOrEmpty(option)) {
                CdPortal.PrintUsage(player);
                return;
            }
            
            switch (option.ToLower()) {
                case "create":
                case "add":
                    PortalCreate(player, cmd);
                    break;
                case "remove":
                case "delete":
                    PortalRemove(player, cmd);
                    break;
                case "info":
                case "i":
                    PortalInfo(player, cmd);
                    break;
                case "list":
                case "l":
                    PortalList(player, cmd);
                    break;
                case "enable":
                case "on":
                    player.PortalsEnabled = true;
                    player.Message("You enabled the use of portals.");
                    break;
                case "disable":
                case "off":
                    player.PortalsEnabled = false;
                    player.Message("You disabled the use of portals, type /portal enable to re-enable portals.");
                    break;
                default:
                    CdPortal.PrintUsage(player);
                    break;
            }
        }
        
        static void PortalCreate(Player player, CommandReader cmd) {
            if (!player.Can(Permission.CreatePortals)) {
                player.MessageNoAccess(Permission.CreatePortals);
                return;
            }
            
            string worldName = cmd.Next();
            if (string.IsNullOrEmpty(worldName)) {
                player.Message("No world specified.");
                return;
            }
            World world = WorldManager.FindWorldExact(worldName);
            if (world == null) {
                player.MessageNoWorld(worldName);
                return;
            }
            
            Block block = Block.Water;
            if (cmd.HasNext && !cmd.NextBlock(player, false, out block)) return;
            if ((!validPBlocks.Contains(block) && block <= Block.StoneBrick) || (block == Block.Air && player.Info.Rank != RankManager.HighestRank)) {
                player.Message("Invalid block, choose a non-solid block");
                return;
            }
            
            player.PortalName = null;
            string name = cmd.Next();
            if (string.IsNullOrEmpty(name)) {
            } else if (player.World.Portals.Find(name) == null) {
                player.PortalName = name;
            } else {
                player.Message("A portal named {0} already exists in this world.", name);
                return;
            }
            
            player.PortalTPPos = world.map == null ? default(Position) : world.map.Spawn;
            if (cmd.HasNext) {
                int x, y, z, rot = player.Position.R, lot = player.Position.L;
                string next = cmd.Next();
                if (next != null && next.StartsWith("#")) {
                    bool needsLoading = world.Map == null;
                    if (needsLoading) world.LoadMap();
                    
                    Zone zone = world.map.Zones.FindExact(next.Remove(0,1));                   
                    if (zone == null) {
                        player.MessageNoZone(next.Remove(0, 1));
                        return;
                    }
                    
                    player.PortalTPPos = new Position(zone.Bounds.XCentre * 32 + 16,
                                                      zone.Bounds.YCentre * 32 + 16, zone.Bounds.ZCentre * 32 + Player.CharacterHeight,
                                                      (byte)rot, (byte)lot);
                    player.Message("Players will be teleported to zone: " + zone.Name);
                    player.Message("At: " + player.PortalTPPos.ToString());
                    player.Message("On: " + world.Name);
                    if (needsLoading) world.UnloadMap(false);
                } else if (int.TryParse(next, out x) && cmd.NextInt(out y) && cmd.NextInt(out z)) {
                    if (cmd.CountRemaining >= 2 && cmd.NextInt(out rot) && cmd.NextInt(out lot)) {
                        if (rot > 255 || rot < 0) {
                            player.Message("R must be inbetween 0 and 255. Set to player R");
                            rot = player.Position.R;
                        }

                        if (lot > 255 || lot < 0) {
                            player.Message("L must be inbetween 0 and 255. Set to player L");
                            lot = player.Position.L;
                        }
                    }
                    player.PortalTPPos = new Position(x * 32, y * 32, z * 32, (byte)rot, (byte)lot);
                }
            }
            
            DrawOperation op = new CuboidDrawOperation(player);
            op.Brush = new NormalBrush(block, block);
            player.PortalWorld = worldName;
            player.SelectionStart(op.ExpectedMarks, PortalCreateCallback, op, Permission.CreatePortals);
            player.Message("Click {0} blocks or use &H/Mark&S to mark the area of the portal.", op.ExpectedMarks);
        }
        
        static void PortalRemove(Player player, CommandReader cmd) {
            if (!player.Can(Permission.CreatePortals)) {
                player.MessageNoAccess(Permission.CreatePortals);
                return;
            }
            
            string name = cmd.Next();
            if (string.IsNullOrEmpty(name)) {
                player.Message("No portal name specified.");
                return;
            }
            
            string worldName = cmd.Next();
            World world = player.World;
            if (!string.IsNullOrEmpty(worldName)) {
                world = WorldManager.FindWorldOrPrintMatches(player, worldName);
            }
            if (world == null) return;
            
            if (world.Portals.Count == 0) {
                player.Message("There are no portals in {0}", world.ClassyName);
                return;
            }
            
            Portal portal = world.Portals.Find(name);
            if (portal == null) {
                 player.Message("Portal {0} does not exist in {1}", name, world.ClassyName);
            } else {
                portal.Remove(player, world);
                player.Message("Portal was removed.");
            }
        }
        
        static void PortalInfo(Player player, CommandReader cmd) {
            string name = cmd.Next();
            if (string.IsNullOrEmpty(name)) {
                player.Message("No portal name specified.");
                return;
            }
            
            string worldName = cmd.Next();
            World world = player.World;
            if (!string.IsNullOrEmpty(worldName)) {
                world = WorldManager.FindWorldOrPrintMatches(player, worldName);
            }
            if (world == null) return;
            
            if (world.Portals.Count == 0) {
                player.Message("There are no portals in {0}", world.ClassyName);
                return;
            }
            
            Portal portal = world.Portals.Find(name);
            if (portal == null) {
                player.Message("Portal {0} does not exist in {1}", name, world.ClassyName);
            } else {
                string creator = PlayerDB.FindPlayerInfoExact(portal.Creator).ClassyName;
                World exit = WorldManager.FindWorldExact(portal.World);
                player.Message("Portal {0}&S was created by {1}&S at {2} and teleports to world {3} at {4}&S.",
                                portal.Name, creator, portal.Created, exit.ClassyName, portal.position().ToString());
            }
        }
        
        static void PortalList(Player player, CommandReader cmd) {
            string worldName = cmd.Next();
            World world = player.World;
            if (!string.IsNullOrEmpty(worldName)) {
                world = WorldManager.FindWorldOrPrintMatches(player, worldName);
            }
            if (world == null) return;
            
            if (world.Portals.Count == 0) {
                player.Message("There are no portals in {0}", world.ClassyName);
                return;
            }
            
            string list;
            lock (world.Portals.locker) {
                list = world.Portals.entries.JoinToString(", ", portal => portal.Name);
            }
            
            player.Message("There are {0} portals in {1}&S: {2}",
                           world.Portals.Count, world.ClassyName, list);
        }
        
        static void PortalCreateCallback(Player player, Vector3I[] marks, object tag) {
            try {
                World world = WorldManager.FindWorldExact(player.PortalWorld);
                if (world == null) {
                    player.MessageInvalidWorldName(player.PortalWorld);
                    return;
                }
                
                DrawOperation op = (DrawOperation)tag;
                if (!op.Prepare(marks)) return;
                PortalsList portals = player.World.Portals;
                
                if (!player.CanDraw(op.BlocksTotalEstimate)) {
                    player.Message("You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.Info.Rank.DrawLimit,
                                   op.Bounds.Volume);
                    op.Cancel();
                    return;
                }

                int Xmin = Math.Min(marks[0].X, marks[1].X);
                int Xmax = Math.Max(marks[0].X, marks[1].X);
                int Ymin = Math.Min(marks[0].Y, marks[1].Y);
                int Ymax = Math.Max(marks[0].Y, marks[1].Y);
                int Zmin = Math.Min(marks[0].Z, marks[1].Z);
                int Zmax = Math.Max(marks[0].Z, marks[1].Z);

                for (int x = Xmin; x <= Xmax; x++) {
                    for (int y = Ymin; y <= Ymax; y++) {
                        for (int z = Zmin; z <= Zmax; z++) {
                            if (PortalHandler.NearSpawn(player, player.World, new Vector3I(x, y, z))) {
                                player.Message("You cannot build a portal near the spawnpoint.");
                                return;
                            }

                            if (portals.Find(new Vector3I(x, y, z)) != null) {
                                player.Message("You cannot build a portal inside a portal.");
                                return;
                            }
                        }
                    }
                }

                string name = player.PortalName;
                if (name == null) name = player.World.Portals.GenAutoName();

                Portal portal = new Portal(player.PortalWorld, new PortalRange(Xmin, Xmax, Ymin, Ymax, Zmin, Zmax),
                                           name, player.Name, player.World.Name, player.PortalTPPos);
                portals.Add(portal);
                PortalDB.Save();
                
                op.AnnounceCompletion = false;
                op.Context = BlockChangeContext.Portal;
                op.Begin();

                player.Message("Successfully created portal with name " + portal.Name + ".");
            } catch (Exception ex) {
                player.Message("Failed to create portal.");
                Logger.Log(LogType.Error, "WorldCommands.PortalCreateCallback: " + ex);
            }
        }
        #endregion
    }
}
