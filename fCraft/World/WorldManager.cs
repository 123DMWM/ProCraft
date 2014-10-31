﻿// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | Copyright 2014 123DMWM <shmo1joe2@gmail.com>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using fCraft.Events;
using fCraft.MapConversion;
using JetBrains.Annotations;
using System.Text;

namespace fCraft {
    /// <summary> Manages the world list. Handles loading/unloading, renaming, map changes, and more. </summary>
    public static class WorldManager {
        public const string BuildSecurityXmlTagName = "BuildSecurity",
                            AccessSecurityXmlTagName = "AccessSecurity",
                            EnvironmentXmlTagName = "Environment",
                            RankMainXmlTagName = "RankMainWorld";

        /// <summary> List of worlds currently being managed by WorldManager. </summary>
        public static World[] Worlds { get; private set; }
        static readonly SortedDictionary<string, World> WorldIndex = new SortedDictionary<string, World>( StringComparer.OrdinalIgnoreCase );

        internal static readonly object SyncRoot = new object();


        /// <summary> Gets or sets the default main world.
        /// That's the world that players first join upon connecting.
        /// The map of the new main world is preloaded, and old one is unloaded, if needed. </summary>
        /// <exception cref="System.ArgumentNullException" />
        /// <exception cref="fCraft.WorldOpException" />
        [NotNull]
        public static World MainWorld {
            get { return mainWorld; }
            set {
                if( value == null ) throw new ArgumentNullException( "value" );
                if( value == mainWorld ) return;
                if( RaiseMainWorldChangingEvent( mainWorld, value ) ) {
                    throw new WorldOpException( value.Name, WorldOpExceptionCode.Cancelled );
                }
                World oldWorld;
                lock( SyncRoot ) {
                    value.Preload = true;
                    oldWorld = mainWorld;
                    if( oldWorld != null ) {
                        oldWorld.Preload = false;
                    }
                    mainWorld = value;
                }
                RaiseMainWorldChangedEvent( oldWorld, value );
                SaveWorldList();
            }
        }
        static World mainWorld;


        public static World FindMainWorld( Player player ) {
            World rankMain = player.Info.Rank.MainWorld;
            if ((player.Info.TimesVisited == 1 || player.Info.HasRTR == false) && FindWorldExact("Tutorial") != null) return FindWorldOrPrintMatches(player, "Tutorial");
            if( rankMain != null && player.CanJoin( rankMain ) && player.Info.JoinOnRankWorld == true ) {
                return rankMain;
            } else {
                return MainWorld;
            }
        }


        #region World List Saving/Loading

        static World firstWorld;
        internal static bool LoadWorldList() {
            World newMainWorld = null;
            Worlds = new World[0];
            if( File.Exists( Paths.WorldListFileName ) ) {
#if !DEBUG
                try {
#endif
                    XDocument doc = XDocument.Load( Paths.WorldListFileName );
                    XElement root = doc.Root;
                    if( root != null ) {
                        foreach( XElement el in root.Elements( "World" ) ) {
#if DEBUG
                            LoadWorldListEntry( el );
#else
                            try {
                                LoadWorldListEntry( el );
                            } catch( Exception ex ) {
                                Logger.LogAndReportCrash( "An error occurred while trying to parse one of the entries on the world list",
                                                          "ProCraft", ex, false );
                            }
#endif
                        }

                        XAttribute temp;
                        if( (temp = root.Attribute( "main" )) != null ) {
                            World suggestedMainWorld = FindWorldExact( temp.Value );

                            if( suggestedMainWorld != null ) {
                                newMainWorld = suggestedMainWorld;

                            } else if( firstWorld != null ) {
                                // if specified main world does not exist, use first-defined world
                                Logger.Log( LogType.Warning,
                                            "The specified main world \"{0}\" does not exist. " +
                                            "\"{1}\" was designated main instead. You can use /WMain to change it.",
                                            temp.Value, firstWorld.Name );
                                newMainWorld = firstWorld;
                            }
                            // if firstWorld was also null, LoadWorldList() should try creating a new mainWorld

                        } else if( firstWorld != null ) {
                            newMainWorld = firstWorld;
                        }
                    }
#if !DEBUG
                } catch( Exception ex ) {
                    Logger.LogAndReportCrash( "Error occurred while trying to load the world list.", "ProCraft", ex, true );
                    return false;
                }
#endif

                if( newMainWorld == null ) {
                    Logger.Log( LogType.Error,
                                "Server.Start: Could not load any of the specified worlds, or no worlds were specified. " +
                                "Creating default \"main\" world." );
                    newMainWorld = AddWorld( null, "main", MapGenerator.GenerateFlatgrass( 128, 128, 64 ), true );
                }

            } else {
                Logger.Log( LogType.SystemActivity,
                            "Server.Start: No world list found. Creating default \"main\" world." );
                newMainWorld = AddWorld( null, "main", MapGenerator.GenerateFlatgrass( 128, 128, 64 ), true );
            }

            // if there is no default world still, die.
            if( newMainWorld == null ) {
                throw new Exception( "Could not create any worlds." );

            } else if( newMainWorld.AccessSecurity.HasRestrictions ) {
                Logger.Log( LogType.Warning,
                            "Server.LoadWorldList: Main world cannot have any access restrictions. " +
                            "Access permission for \"{0}\" has been reset.",
                             newMainWorld.Name );
                newMainWorld.AccessSecurity.Reset();
            }

            MainWorld = newMainWorld;

            return true;
        }

        static void LoadWorldListEntry([NotNull] XElement el)
        {
            if (el == null) throw new ArgumentNullException("el");
            XAttribute tempAttr;
            if ((tempAttr = el.Attribute("name")) == null)
            {
                Logger.Log(LogType.Error, "WorldManager: World tag with no name skipped.");
                return;
            }
            string worldName = tempAttr.Value;

            bool neverUnload = (el.Attribute("noUnload") != null);

            World world;
            try
            {
                world = AddWorld(null, worldName, null, neverUnload);
            }
            catch (WorldOpException ex)
            {
                Logger.Log(LogType.Error,
                            "WorldManager: Error adding world \"{0}\": {1}",
                            worldName, ex.Message);
                return;
            }
            if ((tempAttr = el.Attribute("hidden")) != null)
            {
                bool isHidden;
                if (Boolean.TryParse(tempAttr.Value, out isHidden))
                {
                    world.IsHidden = isHidden;
                }
                else
                {
                    Logger.Log(LogType.Warning,
                                "WorldManager: Could not parse \"hidden\" attribute of world \"{0}\", assuming NOT hidden.",
                                worldName);
                }
            }
            if (firstWorld == null) firstWorld = world;

            XElement tempEl = el.Element("Greeting");
            if (tempEl != null && !String.IsNullOrEmpty(tempEl.Value)) world.Greeting = tempEl.Value;

            if ((tempEl = el.Element(AccessSecurityXmlTagName)) != null)
            {
                world.AccessSecurity = new SecurityController(tempEl, true);
            }
            else if ((tempEl = el.Element("accessSecurity")) != null)
            {
                world.AccessSecurity = new SecurityController(tempEl, true);
            }
            if ((tempEl = el.Element(BuildSecurityXmlTagName)) != null)
            {
                world.BuildSecurity = new SecurityController(tempEl, true);
            }
            else if ((tempEl = el.Element("buildSecurity")) != null)
            {
                world.BuildSecurity = new SecurityController(tempEl, true);
            }

            // load backup settings
            if ((tempAttr = el.Attribute("backup")) != null)
            {
                TimeSpan backupInterval;
                if (tempAttr.Value.ToTimeSpan(out backupInterval))
                {
                    if (backupInterval <= TimeSpan.Zero)
                    {
                        world.BackupEnabledState = YesNoAuto.No;
                    }
                    else
                    {
                        world.BackupInterval = backupInterval;
                    }
                }
                else
                {
                    world.BackupEnabledState = YesNoAuto.Auto;
                    Logger.Log(LogType.Warning,
                                "WorldManager: Could not parse \"backup\" attribute of world \"{0}\", assuming default ({1}).",
                                worldName,
                                world.BackupInterval.ToMiniString());
                }
            }
            else
            {
                world.BackupEnabledState = YesNoAuto.Auto;
            }

            // load BlockDB settings
            XElement blockEl = el.Element(BlockDB.XmlRootName);
            if (blockEl != null)
            {
                world.BlockDB.LoadSettings(blockEl);
            }

            // load environment settings
            XElement envEl = el.Element(EnvironmentXmlTagName);
            if (envEl != null)
            {
                if ((tempAttr = envEl.Attribute("cloud")) != null)
                {
                    try
                    {
                        world.CloudColor = tempAttr.Value;
                    }
                    catch
                    {
                        world.CloudColor = null;
                        Logger.Log(LogType.Warning,
                                    "WorldManager: Could not parse \"cloud\" attribute of Environment settings for world \"{0}\", assuming default (normal).",
                                    worldName);
                    }
                }
                if ((tempAttr = envEl.Attribute("fog")) != null)
                {
                    try
                    {
                        world.FogColor = tempAttr.Value;
                    }
                    catch
                    {
                        world.FogColor = null;
                        Logger.Log(LogType.Warning,
                                    "WorldManager: Could not parse \"fog\" attribute of Environment settings for world \"{0}\", assuming default (normal).",
                                    worldName);
                    }
                }
                if ((tempAttr = envEl.Attribute("sky")) != null)
                {
                    try
                    {
                        world.SkyColor = tempAttr.Value;
                    }
                    catch
                    {
                        world.SkyColor = null;
                        Logger.Log(LogType.Warning,
                                    "WorldManager: Could not parse \"sky\" attribute of Environment settings for world \"{0}\", assuming default (normal).",
                                    worldName);
                    }
                }
                if ((tempAttr = envEl.Attribute("shadow")) != null)
                {
                    try
                    {
                        world.ShadowColor = tempAttr.Value;
                    }
                    catch
                    {
                        world.ShadowColor = null;
                        Logger.Log(LogType.Warning,
                                    "WorldManager: Could not parse \"shadow\" attribute of Environment settings for world \"{0}\", assuming default (normal).",
                                    worldName);
                    }
                }
                if ((tempAttr = envEl.Attribute("light")) != null)
                {
                    try
                    {
                        world.LightColor = tempAttr.Value;
                    }
                    catch
                    {
                        world.LightColor = null;
                        Logger.Log(LogType.Warning,
                                    "WorldManager: Could not parse \"light\" attribute of Environment settings for world \"{0}\", assuming default (normal).",
                                    worldName);
                    }
                }
                if ((tempAttr = envEl.Attribute("water")) != null)
                {
                    Block block;
                    try
                    {
                        Map.GetBlockByName(tempAttr.Value, false, out block);
                        world.HorizonBlock = block;
                    }
                    catch
                    {
                        world.HorizonBlock = Block.Water;
                        Logger.Log(LogType.Warning,
                                    "WorldManager: Could not parse \"Water\" attribute of Environment settings for world \"{0}\", assuming default (water).",
                                    worldName);
                    }
                }
                if ((tempAttr = envEl.Attribute("bedrock")) != null)
                {
                    Block block;
                    try
                    {
                        Map.GetBlockByName(tempAttr.Value, false, out block);
                        world.EdgeBlock = block;
                    }
                    catch
                    {
                        world.EdgeBlock = Block.Admincrete;
                        Logger.Log(LogType.Warning,
                                    "WorldManager: Could not parse \"bedrock\" attribute of Environment settings for world \"{0}\", assuming default (admincrete).",
                                    worldName);
                    }
                }
                if ((tempAttr = envEl.Attribute("level")) != null)
                {
                    if (!short.TryParse(tempAttr.Value, out world.EdgeLevel))
                    {
                        world.EdgeLevel = (short)(world.map.Height / 2);
                        Logger.Log(LogType.Warning,
                                    "WorldManager: Could not parse \"level\" attribute of Environment settings for world \"{0}\", assuming default (normal).",
                                    worldName);
                    }
                }
            }

            // load loaded/map-changed information
            long timestamp;
            tempEl = el.Element("LoadedBy");
            if (tempEl != null) world.LoadedBy = tempEl.Value;
            tempEl = el.Element("LoadedOn");
            if (tempEl != null && Int64.TryParse(tempEl.Value, out timestamp))
            {
                world.LoadedOn = timestamp.ToDateTime();
            }
            tempEl = el.Element("MapChangedBy");
            if (tempEl != null) world.MapChangedBy = tempEl.Value;
            tempEl = el.Element("MapChangedOn");
            if (tempEl != null && Int64.TryParse(tempEl.Value, out timestamp))
            {
                world.MapChangedOn = timestamp.ToDateTime();
            }

            // load lock information
            if ((tempAttr = el.Attribute("locked")) != null)
            {
                bool isLocked;
                if (Boolean.TryParse(tempAttr.Value, out isLocked))
                {
                    world.IsLocked = isLocked;
                }
                tempEl = el.Element("LockedBy");
                if (tempEl != null) world.LockedBy = tempEl.Value;
                tempEl = el.Element("LockedOn");
                if (tempEl != null && Int64.TryParse(tempEl.Value, out timestamp))
                {
                    world.LockedOn = timestamp.ToDateTime();
                }
            }
            else
            {
                tempEl = el.Element("UnlockedBy");
                if (tempEl != null) world.UnlockedBy = tempEl.Value;
                tempEl = el.Element("UnlockedOn");
                if (tempEl != null && Int64.TryParse(tempEl.Value, out timestamp))
                {
                    world.UnlockedOn = timestamp.ToDateTime();
                }
            }
            /* load BlockHunt settings
            XElement tempBH = el.Element("BlockHunt");
            if (tempBH != null)
            {
                if ((tempAttr = tempBH.Attribute("HiderPosX")) != null)
                {
                    if (!short.TryParse(tempAttr.Value, out world.HiderPosX))
                    {
                        world.HiderPosX = world.map.Spawn.X;
                        Logger.Log(LogType.Warning,
                                    "WorldManager: Could not parse \"HiderPosX\" attribute of Block Hunt settings for world \"{0}\", assuming default (spawn).",
                                    worldName);
                    }
                }
                if ((tempAttr = tempBH.Attribute("HiderPosY")) != null)
                {
                    if (!short.TryParse(tempAttr.Value, out world.HiderPosY))
                    {
                        world.HiderPosY = world.map.Spawn.Y;
                        Logger.Log(LogType.Warning,
                                    "WorldManager: Could not parse \"HiderPosY\" attribute of Block Hunt settings for world \"{0}\", assuming default (spawn).",
                                    worldName);
                    }
                }
                if ((tempAttr = tempBH.Attribute("HiderPosZ")) != null)
                {
                    if (!short.TryParse(tempAttr.Value, out world.HiderPosZ))
                    {
                        world.HiderPosZ = world.map.Spawn.Z;
                        Logger.Log(LogType.Warning,
                                    "WorldManager: Could not parse \"HiderPosZ\" attribute of Block Hunt settings for world \"{0}\", assuming default (spawn).",
                                    worldName);
                    }
                }
                if ((tempAttr = tempBH.Attribute("SeekerPosX")) != null)
                {
                    if (!short.TryParse(tempAttr.Value, out world.SeekerPosX))
                    {
                        world.SeekerPosX = world.map.Spawn.X;
                        Logger.Log(LogType.Warning,
                                    "WorldManager: Could not parse \"SeekerPosX\" attribute of Block Hunt settings for world \"{0}\", assuming default (spawn).",
                                    worldName);
                    }
                }
                if ((tempAttr = tempBH.Attribute("SeekerPosY")) != null)
                {
                    if (!short.TryParse(tempAttr.Value, out world.SeekerPosY))
                    {
                        world.SeekerPosY = world.map.Spawn.Y;
                        Logger.Log(LogType.Warning,
                                    "WorldManager: Could not parse \"SeekerPosY\" attribute of Block Hunt settings for world \"{0}\", assuming default (spawn).",
                                    worldName);
                    }
                }
                if ((tempAttr = tempBH.Attribute("SeekerPosZ")) != null)
                {
                    if (!short.TryParse(tempAttr.Value, out world.SeekerPosZ))
                    {
                        world.SeekerPosZ = world.map.Spawn.Z;
                        Logger.Log(LogType.Warning,
                                    "WorldManager: Could not parse \"SeekerPosZ\" attribute of Block Hunt settings for world \"{0}\", assuming default (spawn).",
                                    worldName);
                    }
                }
                if ((tempAttr = tempBH.Attribute("GameBlocks")) != null)
                {
                    string test = tempAttr.Value;
                    List<Block> blocks = new List<Block>();
                    String[] blockSplit = test.Split(',');
                    try
                    {
                        for (int i = 0; i < blockSplit.Length; i++)
                        {
                            Block numParse;
                            if (Map.GetBlockByName(blockSplit[i], false, out numParse))
                            {
                                blocks.Add(numParse);
                            }
                        }
                        world.GameBlocks = blocks;
                    }
                    catch
                    {
                        world.GameBlocks = null;
                        Logger.Log(LogType.Warning,
                                    "WorldManager: Could not parse \"GameBlocks\" attribute of Block Hunt settings for world \"{0}\", assuming default (None).",
                                    worldName);
                    }
                }
            }*/



            foreach (XElement mainedRankEl in el.Elements(RankMainXmlTagName))
            {
                Rank rank = Rank.Parse(mainedRankEl.Value);
                if (rank != null)
                {
                    if (rank < world.AccessSecurity.MinRank)
                    {
                        world.AccessSecurity.MinRank = rank;
                        Logger.Log(LogType.Warning,
                                    "WorldManager: Lowered access MinRank of world {0} to allow it to be the main world for that rank.",
                                    rank.Name);
                    }
                    rank.MainWorld = world;
                }
            }

            CheckMapFile(world);
        }

        // Makes sure that the map file exists, is properly named, and is loadable.
        static void CheckMapFile( [NotNull] World world ) {
            if( world == null ) throw new ArgumentNullException( "world" );
            // Check the world's map file
            string fullMapFileName = world.MapFileName;
            string fileName = Path.GetFileName( fullMapFileName );

            if( Paths.FileExists( fullMapFileName, false ) ) {
                if( !Paths.FileExists( fullMapFileName, true ) ) {
                    // Map file has wrong capitalization
                    FileInfo[] matches = Paths.FindFiles( fullMapFileName );
                    if( matches.Length == 1 ) {
                        // Try to rename the map file to match world's capitalization
                        Paths.ForceRename( matches[0].FullName, fileName );
                        if( Paths.FileExists( fullMapFileName, true ) ) {
                            Logger.Log( LogType.Warning,
                                        "WorldManager.CheckMapFile: Map file for world \"{0}\" was renamed from \"{1}\" to \"{2}\"",
                                        world.Name, matches[0].Name, fileName );
                        } else {
                            Logger.Log( LogType.Error,
                                        "WorldManager.CheckMapFile: Failed to rename map file of \"{0}\" from \"{1}\" to \"{2}\"",
                                        world.Name, matches[0].Name, fileName );
                            return;
                        }
                    } else {
                        Logger.Log( LogType.Warning,
                                    "WorldManager.CheckMapFile: More than one map file exists matching the world name \"{0}\". " +
                                    "Please check the map directory and use /WLoad to load the correct file.",
                                    world.Name );
                        return;
                    }
                }
                // Try loading the map header
                try {
                    MapUtility.LoadHeader( world.MapFileName );
                } catch( Exception ex ) {
                    Logger.Log( LogType.Warning,
                                "WorldManager.CheckMapFile: Could not load map file for world \"{0}\": {1}",
                                world.Name, ex );
                }
            } else {
                Logger.Log( LogType.Warning,
                            "WorldManager.CheckMapFile: Map file for world \"{0}\" was not found.",
                            world.Name );
            }
        }


        /// <summary> Saves the current world list to worlds.xml. Thread-safe. </summary>
        public static void SaveWorldList() {
            const string worldListTempFileName = Paths.WorldListFileName + ".tmp";
            // Save world list
            lock( SyncRoot ) {
                XDocument doc = new XDocument();
                XElement root = new XElement( "fCraftWorldList" );

                foreach( World world in Worlds ) {
                    XElement temp = new XElement( "World" );
                    temp.Add( new XAttribute( "name", world.Name ) );

                    if( world.AccessSecurity.HasRestrictions ) {
                        temp.Add( world.AccessSecurity.Serialize( AccessSecurityXmlTagName ) );
                    }
                    if( world.BuildSecurity.HasRestrictions ) {
                        temp.Add( world.BuildSecurity.Serialize( BuildSecurityXmlTagName ) );
                    }

                    // save backup settings
                    switch( world.BackupEnabledState ) {
                        case YesNoAuto.Yes:
                            temp.Add( new XAttribute( "backup", world.BackupInterval.ToTickString() ) );
                            break;
                        case YesNoAuto.No:
                            temp.Add( new XAttribute( "backup", 0 ) );
                            break;
                    }

                    if( world.Preload ) {
                        temp.Add( new XAttribute( "noUnload", true ) );
                    }
                    if( world.IsHidden ) {
                        temp.Add( new XAttribute( "hidden", true ) );
                    }
                    temp.Add( world.BlockDB.SaveSettings() );

                    World world1 = world; // keeping ReSharper happy
                    foreach( Rank mainedRank in RankManager.Ranks.Where( r => r.MainWorld == world1 ) ) {
                        temp.Add( new XElement( RankMainXmlTagName, mainedRank.FullName ) );
                    }

                    // save loaded/map-changed information
                    if( !String.IsNullOrEmpty( world.LoadedBy ) ) {
                        temp.Add( new XElement( "LoadedBy", world.LoadedBy ) );
                    }
                    if( world.LoadedOn != DateTime.MinValue ) {
                        temp.Add( new XElement( "LoadedOn", world.LoadedOn.ToUnixTime() ) );
                    }
                    if( !String.IsNullOrEmpty( world.MapChangedBy ) ) {
                        temp.Add( new XElement( "MapChangedBy", world.MapChangedBy ) );
                    }
                    if( world.MapChangedOn != DateTime.MinValue ) {
                        temp.Add( new XElement( "MapChangedOn", world.MapChangedOn.ToUnixTime() ) );
                    }

                    // save environmental settings
                    XElement elEnv = new XElement( EnvironmentXmlTagName );
                    if( world.CloudColor != null ) elEnv.Add( new XElement( "cloud", world.CloudColor ) );
                    if (world.FogColor != null) elEnv.Add(new XAttribute("fog", world.FogColor));
                    if (world.SkyColor != null) elEnv.Add(new XAttribute("sky", world.SkyColor));
                    if (world.ShadowColor != null) elEnv.Add(new XAttribute("shadow", world.ShadowColor));
                    if (world.LightColor != null) elEnv.Add(new XAttribute("light", world.LightColor));
                    elEnv.Add(new XAttribute("level", world.EdgeLevel));
                    if (world.HorizonBlock != Block.Water) elEnv.Add(new XAttribute("water", world.HorizonBlock));
                    if (world.EdgeBlock != Block.Admincrete) elEnv.Add(new XAttribute("bedrock", world.EdgeBlock));
                    if( elEnv.HasAttributes ) {
                        temp.Add( elEnv );
                    }

                    // save lock information
                    if( world.IsLocked ) {
                        temp.Add( new XAttribute( "locked", true ) );
                        if( !String.IsNullOrEmpty( world.LockedBy ) ) {
                            temp.Add( new XElement( "LockedBy", world.LockedBy ) );
                        }
                        if( world.LockedOn != DateTime.MinValue ) {
                            temp.Add( new XElement( "LockedOn", world.LockedOn.ToUnixTime() ) );
                        }
                    } else {
                        if( !String.IsNullOrEmpty( world.UnlockedBy ) ) {
                            temp.Add( new XElement( "UnlockedBy", world.UnlockedBy ) );
                        }
                        if( world.UnlockedOn != DateTime.MinValue ) {
                            temp.Add( new XElement( "UnlockedOn", world.UnlockedOn.ToUnixTime() ) );
                        }
                    }

                    /*save BlockHunt settings
                    XElement BHunt = new XElement("BlockHunt");
                    if (world.HiderPosX > -1) BHunt.Add(new XAttribute("HiderPosX", world.HiderPosX));
                    if (world.HiderPosY > -1) BHunt.Add(new XAttribute("HiderPosY", world.HiderPosY));
                    if (world.HiderPosZ > -1) BHunt.Add(new XAttribute("HiderPosZ", world.HiderPosZ));
                    if (world.SeekerPosX > -1) BHunt.Add(new XAttribute("SeekerPosX", world.SeekerPosX));
                    if (world.SeekerPosY > -1) BHunt.Add(new XAttribute("SeekerPosY", world.SeekerPosY));
                    if (world.SeekerPosZ > -1) BHunt.Add(new XAttribute("SeekerPosZ", world.SeekerPosZ));
                    if (world.GameBlocks.Count > 0) BHunt.Add(new XAttribute("GameBlocks", world.GameBlocks.JoinToString(",")));
                    if (BHunt.HasAttributes)
                    {
                        temp.Add(BHunt);
                    }*/

                    root.Add( temp );
                }
                root.Add( new XAttribute( "main", MainWorld.Name ) );

                doc.Add( root );
                doc.Save( worldListTempFileName );
                Paths.MoveOrReplaceFile( worldListTempFileName, Paths.WorldListFileName );
            }
        }

        #endregion


        #region Finding Worlds

        /// <summary> Finds a world by full name.
        /// Target world is not guaranteed to have a loaded map. </summary>
        /// <returns> World if found, or null if not found. </returns>
        [CanBeNull]
        public static World FindWorldExact( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            return Worlds.FirstOrDefault( w => w.Name.Equals( name, StringComparison.OrdinalIgnoreCase ) );
        }


        /// <summary> Finds all worlds that match the given world name.
        /// Autocompletes. Does not raise SearchingForWorld event.
        /// Target worlds are not guaranteed to have a loaded map. </summary>
        public static World[] FindWorldsNoEvent( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            World[] worldListCache = Worlds;

            List<World> results = new List<World>();
            for( int i = 0; i < worldListCache.Length; i++ ) {
                if( worldListCache[i] != null ) {
                    if( worldListCache[i].Name.Equals( name, StringComparison.OrdinalIgnoreCase ) ) {
                        results.Clear();
                        results.Add( worldListCache[i] );
                        break;
                    } else if( worldListCache[i].Name.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
                        results.Add( worldListCache[i] );
                    }
                }
            }
            return results.ToArray();
        }


        /// <summary> Finds all worlds that match the given name.
        /// Autocompletes. Raises SearchingForWorld event.
        /// Target worlds are not guaranteed to have a loaded map.</summary>
        /// <param name="player"> Player who is calling the query. May be null. </param>
        /// <param name="name"> Full or partial world name. </param>
        /// <returns> An array of 0 or more worlds that matched the name. </returns>
        public static World[] FindWorlds( [CanBeNull] Player player, [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            World[] matches = FindWorldsNoEvent( name );
            var h = SearchingForWorld;
            if( h != null ) {
                SearchingForWorldEventArgs e = new SearchingForWorldEventArgs( player, name, matches.ToList() );
                h( null, e );
                matches = e.Matches.ToArray();
            }
            return matches;
        }


        /// <summary> Tries to find a single world by full or partial name.
        /// Returns null if zero or multiple worlds matched. </summary>
        /// <param name="player"> Player who will receive messages regarding zero or multiple matches. </param>
        /// <param name="worldName"> Full or partial world name. </param>
        [CanBeNull]
        public static World FindWorldOrPrintMatches( [NotNull] Player player, [NotNull] string worldName ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( worldName == null ) throw new ArgumentNullException( "worldName" );
            if( worldName == "-" ) {
                if( player.LastUsedWorldName != null ) {
                    worldName = player.LastUsedWorldName;
                } else {
                    player.Message( "Cannot repeat world name: you haven't used any names yet." );
                    return null;
                }
            }
            player.LastUsedWorldName = worldName;

            World[] matches = FindWorlds( player, worldName );

            if( matches.Length == 0 ) {
                player.MessageNoWorld( worldName );
                return null;
            }

            if( matches.Length > 1 ) {
                player.MessageManyMatches( "world", matches );
                return null;
            }
            return matches[0];
        }

        #endregion


        /// <summary> Creates a new world and adds it to the current list of worlds being managed by WorldManager. </summary>
        /// <param name="player"> Player who is adding the world. May be null. </param>
        /// <param name="name"> Name of the world being added. May NOT be null. </param>
        /// <param name="map"> Map to assign to the newly created world. May be null. </param>
        /// <param name="preload"> Whether or not the map should be preloaded. </param>
        /// <returns> Newly-created world. </returns>
        /// <exception cref="ArgumentNullException"> If name is null. </exception>
        /// <exception cref="WorldOpException"> If world name was invalid, a world with this name already exists,
        /// or if an event callback cancelled the addition. </exception>
        [NotNull]
        public static World AddWorld( [CanBeNull] Player player, [NotNull] string name, [CanBeNull] Map map, bool preload ) {
            if( name == null ) throw new ArgumentNullException( "name" );

            if( !World.IsValidName( name ) ) {
                throw new WorldOpException( name, WorldOpExceptionCode.InvalidWorldName );
            }

            lock( SyncRoot ) {
                if( WorldIndex.ContainsKey( name.ToLower() ) ) {
                    throw new WorldOpException( name, WorldOpExceptionCode.DuplicateWorldName );
                }

                if( RaiseWorldCreatingEvent( player, name, map ) ) {
                    throw new WorldOpException( name, WorldOpExceptionCode.Cancelled );
                }

                World newWorld = new World( name ) {
                    Map = map
                };

                if( preload ) {
                    newWorld.Preload = true;
                }

                if( map != null ) {
                    newWorld.SaveMap();
                }

                WorldIndex.Add( name.ToLower(), newWorld );
                UpdateWorldList();

                RaiseWorldCreatedEvent( player, newWorld );

                return newWorld;
            }
        }


        /// <summary> Changes the name of the given world. </summary>
        public static void RenameWorld( [NotNull] World world, [NotNull] string newName, bool moveMapFile, bool overwrite ) {
            if( newName == null ) throw new ArgumentNullException( "newName" );
            if( world == null ) throw new ArgumentNullException( "world" );

            if( !World.IsValidName( newName ) ) {
                throw new WorldOpException( newName, WorldOpExceptionCode.InvalidWorldName );
            }

            lock( world.SyncRoot ) {
                string oldName = world.Name;
                if( oldName == newName ) {
                    throw new WorldOpException( world.Name, WorldOpExceptionCode.NoChangeNeeded );
                }

                lock( SyncRoot ) {
                    World newWorld = FindWorldExact( newName );
                    if( newWorld != null && newWorld != world ) {
                        if( overwrite ) {
                            RemoveWorld( newWorld );
                        } else {
                            throw new WorldOpException( newName, WorldOpExceptionCode.DuplicateWorldName );
                        }
                    }

                    WorldIndex.Remove( world.Name.ToLower() );
                    world.Name = newName;
                    WorldIndex.Add( newName.ToLower(), world );
                    SaveWorldList();
                    UpdateWorldList();

                    if( moveMapFile ) {
                        string oldMapFile = Path.Combine( Paths.MapPath, oldName + ".fcm" );
                        string newMapFile = newName + ".fcm";
                        if( File.Exists( oldMapFile ) ) {
                            try {
                                Paths.ForceRename( oldMapFile, newMapFile );
                            } catch( Exception ex ) {
                                throw new WorldOpException( world.Name,
                                                            WorldOpExceptionCode.MapMoveError,
                                                            ex );
                            }
                        }

                        using( world.BlockDB.GetWriteLock() ) {
                            string oldBlockDBFile = Path.Combine( Paths.BlockDBDirectory, oldName + ".fbdb" );
                            string newBockDBFile = newName + ".fbdb";
                            if( File.Exists( oldBlockDBFile ) ) {
                                try {
                                    Paths.ForceRename( oldBlockDBFile, newBockDBFile );
                                } catch( Exception ex ) {
                                    throw new WorldOpException( world.Name,
                                                                WorldOpExceptionCode.MapMoveError,
                                                                ex );
                                }
                            }
                        }
                    }
                }
            }
        }


        internal static void ReplaceWorld( [NotNull] World oldWorld, [NotNull] World newWorld ) {
            if( oldWorld == null ) throw new ArgumentNullException( "oldWorld" );
            if( newWorld == null ) throw new ArgumentNullException( "newWorld" );

            lock( SyncRoot ) {
                if( oldWorld == newWorld ) {
                    throw new WorldOpException( oldWorld.Name, WorldOpExceptionCode.NoChangeNeeded );
                }

                if( !WorldIndex.ContainsValue( oldWorld ) ) {
                    throw new WorldOpException( oldWorld.Name, WorldOpExceptionCode.WorldNotFound );
                }

                if( WorldIndex.ContainsValue( newWorld ) ) {
                    throw new InvalidOperationException( "New world already exists on the list." );
                }

                // cycle load/unload on the new world to save it under the new name
                newWorld.Name = oldWorld.Name;
                if( newWorld.Preload ) {
                    newWorld.SaveMap();
                } else {
                    newWorld.UnloadMap( false );
                }

                WorldIndex[oldWorld.Name.ToLower()] = newWorld;
                oldWorld.Map = null;

                // change the main world, if needed
                if( oldWorld == MainWorld ) {
                    MainWorld = newWorld;
                }

                SaveWorldList();
                UpdateWorldList();
            }
        }


        /// <summary> Removes the specified world from the list of worlds being managed by WorldManager. </summary>
        /// <param name="worldToDelete"> World to be deleted. </param>
        public static void RemoveWorld( [NotNull] World worldToDelete ) {
            if( worldToDelete == null ) throw new ArgumentNullException( "worldToDelete" );

            lock( SyncRoot ) {
                if( worldToDelete == MainWorld ) {
                    throw new WorldOpException( worldToDelete.Name, WorldOpExceptionCode.CannotDoThatToMainWorld );
                }

                foreach( Rank rank in RankManager.Ranks){
                    if( rank.MainWorld == worldToDelete ) {
                        Logger.Log( LogType.Warning,
                                    "Main world for rank {0} was reset because world {1} was deleted.",
                                    rank.Name, worldToDelete.Name );
                        rank.MainWorld = null;
                    }
                }

                Player[] worldPlayerList = worldToDelete.Players;
                worldToDelete.Players.Message( "&SYou have been moved to the main world." );
                foreach( Player player in worldPlayerList ) {
                    player.JoinWorld( FindMainWorld( player ), WorldChangeReason.WorldRemoved );
                }

                try {
                    worldToDelete.BlockDB.Clear();
                } catch( Exception ex ) {
                    Logger.Log( LogType.Error,
                                "WorldManager.RemoveWorld: Could not delete BlockDB file: {0}", ex );
                }

                WorldIndex.Remove( worldToDelete.Name.ToLower() );
                UpdateWorldList();
                SaveWorldList();
            }
        }


        /// <summary> Number of all worlds that are currently loaded. </summary>
        /// <returns> Number of all loaded worlds. </returns>
        public static int CountLoadedWorlds() {
            return Worlds.Count( world => world.IsLoaded );
        }


        /// <summary> Number of worlds that are currently loaded and can be seen by the specified observer. </summary>
        /// <param name="observer"> Player to observe as. </param>
        /// <returns> Number of worlds the specified player has permission to observe. </returns>
        public static int CountLoadedWorlds( [NotNull] Player observer ) {
            if( observer == null ) throw new ArgumentNullException( "observer" );
            return ListLoadedWorlds( observer ).Count();
        }


        /// <summary> List of all the worlds that are currently loaded. </summary>
        /// <returns> List of all loaded worlds. </returns>
        public static IEnumerable<World> ListLoadedWorlds() {
            return Worlds.Where( world => world.IsLoaded );
        }


        /// <summary> List of worlds that are currently loaded and can be seen by the specified observer. </summary>
        /// <param name="observer"> Player to observe as. </param>
        /// <returns> List of worlds the specified player has permission to observe. </returns>
        public static IEnumerable<World> ListLoadedWorlds( [NotNull] Player observer ) {
            if( observer == null ) throw new ArgumentNullException( "observer" );
            return Worlds.Where( w => w.Players.Any( observer.CanSee ) );
        }


        internal static void UpdateWorldList() {
            lock( SyncRoot ) {
                Worlds = WorldIndex.Values.ToArray();
            }
        }


        /// <summary> Searches for a map using the specified fileName. 
        /// Prints any errors/warnings directly to the player. </summary>
        /// <param name="player"> Player who is doing the search. </param>
        /// <param name="fileName"> FileName of the map to be searched for. </param>
        /// <returns> Full source file name.
        /// Null if file could not be found, or an error occurred. </returns>
        /// <exception cref="ArgumentNullException"> If player or fileName is null. </exception>
        [CanBeNull]
        public static string FindMapFile( [NotNull] Player player, [NotNull] string fileName ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            // Check if path contains missing drives or invalid characters
            if( !Paths.IsValidPath( fileName ) ) {
                player.Message( "Invalid file name or path." );
                return null;
            }

            // Look for the file
            string sourceFullFileName = Path.Combine( Paths.MapPath, fileName );
            if( !File.Exists( sourceFullFileName ) && !Directory.Exists( sourceFullFileName ) ) {

                if( File.Exists( sourceFullFileName + ".fcm" ) ) {
                    // Try with extension added
                    sourceFullFileName += ".fcm";

                } else if( MonoCompat.IsCaseSensitive ) {
                    try {
                        // If we're on a case-sensitive OS, try case-insensitive search
                        FileInfo[] candidates = Paths.FindFiles( sourceFullFileName + ".fcm" );
                        if( candidates.Length == 0 ) {
                            candidates = Paths.FindFiles( sourceFullFileName );
                        }

                        if( candidates.Length == 0 ) {
                            player.Message( "File/directory not found: {0}", fileName );

                        } else if( candidates.Length == 1 ) {
                            player.Message( "File names are case-sensitive! Did you mean to load \"{0}\"?", candidates[0].Name );

                        } else {
                            player.Message( "File names are case-sensitive! Did you mean to load one of these: {0}",
                                            String.Join( ", ", candidates.Select( c => c.Name ).ToArray() ) );
                        }
                    } catch( DirectoryNotFoundException ex ) {
                        player.Message( ex.Message );
                    }
                    return null;

                } else {
                    // Nothing found!
                    player.Message( "File/directory not found: {0}", fileName );
                    return null;
                }
            }

            // Make sure that the given file is within the map directory
            if( !Paths.Contains( Paths.MapPath, sourceFullFileName ) ) {
                player.MessageUnsafePath();
                return null;
            }

            return sourceFullFileName;
        }

        /// <summary> Searches for a map using the specified fileName. 
        /// Prints any errors/warnings directly to the player. </summary>
        /// <param name="player"> Player who is doing the search. </param>
        /// <param name="fileName"> FileName of the map to be searched for. </param>
        /// <returns> Full source file name.
        /// Null if file could not be found, or an error occurred. </returns>
        /// <exception cref="ArgumentNullException"> If player or fileName is null. </exception>
        [CanBeNull]
        public static string FindMapClearFile( [NotNull] Player player, [NotNull] string fileName ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            // Check if path contains missing drives or invalid characters
            if( !Paths.IsValidPath( fileName ) ) {
                player.Message( "Invalid file name or path." );
                return null;
            }

            // Look for the file
            string sourceFullFileName = Path.Combine( Paths.WClearPath, fileName );
            if( !File.Exists( sourceFullFileName ) && !Directory.Exists( sourceFullFileName ) ) {

                if( File.Exists( sourceFullFileName + ".fcm" ) ) {
                    // Try with extension added
                    sourceFullFileName += ".fcm";

                } else if( MonoCompat.IsCaseSensitive ) {
                    try {
                        // If we're on a case-sensitive OS, try case-insensitive search
                        FileInfo[] candidates = Paths.FindFiles( sourceFullFileName + ".fcm" );
                        if( candidates.Length == 0 ) {
                            candidates = Paths.FindFiles( sourceFullFileName );
                        }

                        if( candidates.Length == 0 ) {
                            player.Message( "File/directory not found: {0}", fileName );

                        } else if( candidates.Length == 1 ) {
                            player.Message( "File names are case-sensitive! Did you mean to load \"{0}\"?", candidates[0].Name );

                        } else {
                            player.Message( "File names are case-sensitive! Did you mean to load one of these: {0}",
                                            String.Join( ", ", candidates.Select( c => c.Name ).ToArray() ) );
                        }
                    } catch( DirectoryNotFoundException ex ) {
                        player.Message( ex.Message );
                    }
                    return null;

                } else {
                    // Nothing found!
                    player.Message( "File/directory not found: {0}", fileName );
                    return null;
                }
            }

            // Make sure that the given file is within the map directory
            if( !Paths.Contains( Paths.WClearPath, sourceFullFileName ) ) {
                player.MessageUnsafePath();
                return null;
            }

            return sourceFullFileName;
        }


        #region Events

        /// <summary> Occurs when the main world is being changed (cancelable). </summary>
        public static event EventHandler<MainWorldChangingEventArgs> MainWorldChanging;


        /// <summary> Occurs after the main world has been changed. </summary>
        public static event EventHandler<MainWorldChangedEventArgs> MainWorldChanged;


        /// <summary> Occurs when a player is searching for worlds (with autocompletion).
        /// The list of worlds in the search results may be replaced. </summary>
        public static event EventHandler<SearchingForWorldEventArgs> SearchingForWorld;


        /// <summary> Occurs before a new world is created/added (cancelable). </summary>
        public static event EventHandler<WorldCreatingEventArgs> WorldCreating;


        /// <summary> Occurs after a new world is created/added. </summary>
        public static event EventHandler<WorldCreatedEventArgs> WorldCreated;


        static bool RaiseMainWorldChangingEvent( World oldWorld, [NotNull] World newWorld ) {
            if( newWorld == null ) throw new ArgumentNullException( "newWorld" );
            var h = MainWorldChanging;
            if( h == null ) return false;
            var e = new MainWorldChangingEventArgs( oldWorld, newWorld );
            h( null, e );
            return e.Cancel;
        }

        static void RaiseMainWorldChangedEvent( [CanBeNull] World oldWorld, [NotNull] World newWorld ) {
            if( newWorld == null ) throw new ArgumentNullException( "newWorld" );
            var h = MainWorldChanged;
            if( h != null ) h( null, new MainWorldChangedEventArgs( oldWorld, newWorld ) );
        }

        static bool RaiseWorldCreatingEvent( [CanBeNull] Player player, [NotNull] string worldName, [CanBeNull] Map map ) {
            if( worldName == null ) throw new ArgumentNullException( "worldName" );
            var h = WorldCreating;
            if( h == null ) return false;
            var e = new WorldCreatingEventArgs( player, worldName, map );
            h( null, e );
            return e.Cancel;
        }

        static void RaiseWorldCreatedEvent( [CanBeNull] Player player, [NotNull] World world ) {
            if( world == null ) throw new ArgumentNullException( "world" );
            var h = WorldCreated;
            if( h != null ) h( null, new WorldCreatedEventArgs( player, world ) );
        }

        #endregion
    }
}