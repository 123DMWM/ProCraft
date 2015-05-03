//Copyright (C) <2012> Someone
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using fCraft;
using fCraft.Events;
using System.Threading;
using System.IO;

namespace fCraft
{
    public class CTF
    {
        public static List<Player> redTeam = new List<Player>();
        public static List<Player> blueTeam = new List<Player>();
        public static List<Vector3I> Lava = new List<Vector3I>();
        //List of team players, not often used for checking. Player.Team() is used more.
        public static int redScore = 0, blueScore = 0;
        public static Thread GameThread;
        public static Thread LavaThread;
        public static World world;
        public static int redRoundsWon, blueRoundsWon;
        public static Vector3I redFlag, blueFlag;
        public static Position blueSpawn, redSpawn;
        //Position of flags and spawns.
        public static bool redHasFlag = false;
        public static bool blueHasFlag = false;
        public static int instances;

        /// <summary> Initiates a game of CTF on the specified world. </summary>
        /// <param name="World"> World to begin CTF on. Cannot be null. </param>
        /// <param name="Player">Player who called the command. Used to send back error messages.</param>
        /// <remarks> Make sure to check how many instances are running before starting.</remarks>
        /// <example><code>ctf.Init(player.World)</code></example>
        public static void Init(Player player, World worldCTF)
        {
            world = worldCTF; //World used for all things.
            if (instances > 1) //Failsafe if more then one game is running.
            {
                player.Message("Game already running.");
                return;
            }
            instances = 1;

            //TODO: Add savable properties.
            if (!File.Exists("ctfdata.txt"))
            {
                //No data found, use default.
                if (world != null)
                {
                    redSpawn = new Position(short.Parse(((world.Map.Bounds.XMin + 5) * 32).ToString()), short.Parse(((world.Map.Bounds.Length / 2) * 32).ToString()), short.Parse((3 * 32).ToString()), (byte)64, (byte)0);
                    blueSpawn = new Position(short.Parse(((world.Map.Bounds.XMax - 5) * 32).ToString()), short.Parse(((world.Map.Bounds.Length / 2) * 32).ToString()), short.Parse((3 * 32).ToString()), (byte)196, (byte)0);
                    redFlag = new Vector3I((world.Map.Bounds.XMin + 5), world.Map.Bounds.Length / 2, 10);
                    blueFlag = new Vector3I((world.Map.Bounds.XMax - 5), world.Map.Bounds.Length / 2, 10);
                    //Create flags.
                    world.Map.QueueUpdate(new BlockUpdate(Player.Console, redFlag, Block.Red));
                    world.Map.QueueUpdate(new BlockUpdate(Player.Console, blueFlag, Block.Blue));
                }
            }

            //Is never used.
            else
            {
                string[] ctfData = File.ReadAllLines("ctfdata.txt");
                //Load all the data, split it later.
                string[] redSData = ctfData[0].Split(',');
                string[] blueSData = ctfData[1].Split(',');
                string[] redFData = ctfData[2].Split(',');
                string[] blueFData = ctfData[3].Split(',');
                //Now declare all flags. Don't try and check if the file is invalid or not.
                redSpawn = new Position(
                    Convert.ToInt32(redSData[0]), Convert.ToInt32(redSData[1]), Convert.ToInt32(redSData[2]));
                blueSpawn = new Position(
                    Convert.ToInt32(blueSData[0]), Convert.ToInt32(blueSData[1]), Convert.ToInt32(blueSData[2]));
                redFlag = new Vector3I(
                    Convert.ToInt32(redFData[0]), Convert.ToInt32(redFData[1]), Convert.ToInt32(redFData[2]));
                blueFlag = new Vector3I(
                    Convert.ToInt32(blueFData[0]), Convert.ToInt32(blueFData[1]), Convert.ToInt32(blueFData[2]));

            }
            //Initiate all events to be the current class, for checking.
            Player.PlacingBlock += PlayerPlacing;
            Player.JoinedWorld += PlayerChangedWorld;
            Player.Moved += PlayerMoved;
            redRoundsWon = 0;
            blueRoundsWon = 0;

            
            //Replace This World with The Backup!
            bool flushWorld = true;
            bool exception = false;
            if (flushWorld)
            {
                Console.WriteLine("Flushing CTF...");
                Map map;
                try
                {
                    map = MapConversion.MapUtility.Load("./maps/CTFBackup.fcm");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error Flushing CTF.");
                    Console.WriteLine("Could not load specified file: {0}: {1}", ex.GetType().Name, ex.Message);
                    exception = true;
                    map = world.Map;
                }
                if (!exception)
                {
                    try
                    {
                        Console.WriteLine("Changing The Map For CTF...");
                        if (world != null) world.MapChangedBy = player.Name;
                        if (world != null) world = world.ChangeMap(map);
                        Console.WriteLine("Map Changed For CTF.");
                    }
                    catch (WorldOpException ex)
                    {
                        Logger.Log(LogType.Error,
                                    "Could not complete WorldLoad operation: {0}", ex.Message);
                        Console.WriteLine("&WWLoad: {0}", ex.Message);
                    }

                    //world.Players.Message(player, "{0}&S loaded a new map for the world {1}", player.ClassyName, world.ClassyName);
                    //player.Message("New map for the world {0}&S has been loaded.", world.ClassyName);
                    try
                    {
                        Logger.Log(LogType.UserActivity,
                                    "Player {0} loaded new map for world \"{1}\" from \"{2}\"",
                                    player.Name, world.Name, "./maps/CTFBackup.fcm");
                    }
                    catch
                    {
                        Console.WriteLine("Some Error...");
                    }
                }
            }
            //*/
            Console.WriteLine("All Should Be Okay!");
            //Remove zones if they already exist. Fixes errors.
            if (world.Map == null)
            {
                Console.WriteLine("CTF World Map reports Null...");
                Console.WriteLine(world.MapFileName);
                Console.WriteLine(world.Name);
            }
            try
            {
                if (world.Map.Zones.Contains("Red"))
                {
                    world.Map.Zones.Remove("Red");
                }
                if (world.Map.Zones.Contains("Blue"))
                {
                    world.Map.Zones.Remove("Blue");
                }
                Console.WriteLine("Cleaned Up Old Zones!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Zone Cleanup Broke!");
                Console.WriteLine("ZoneCleanUp: " + ex.Message);
            }
            //Red side zone handling
            try
            {
                Zone zoneRed = new Zone();
                zoneRed.Name = "Red";
                Vector3I redFirst = new Vector3I(world.Map.Bounds.XMin, world.Map.Bounds.YMin, world.Map.Bounds.ZMin);
                Vector3I redSecond = new Vector3I(world.Map.Bounds.Width / 2 - 2, world.Map.Bounds.Length, world.Map.Bounds.ZMax);
                zoneRed.Create(new BoundingBox(redFirst, redSecond), Player.Console.Info);
                world.Map.Zones.Add(zoneRed);

                //Blue side zone handling
                Zone zoneBlue = new Zone();
                zoneBlue.Name = "Blue";
                Vector3I blueFirst = new Vector3I(world.Map.Bounds.XMax, world.Map.Bounds.YMin, world.Map.Bounds.ZMin);
                int xBlue = (world.Map.Bounds.Width - world.Map.Bounds.Width / 2) + 2;
                Vector3I blueSecond = new Vector3I(xBlue, world.Map.Bounds.Length, world.Map.Bounds.ZMax);
                zoneBlue.Create(new BoundingBox(blueFirst, blueSecond), Player.Console.Info);
                world.Map.Zones.Add(zoneBlue);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in the Zone Thing...");
                Console.WriteLine("ZoneCleanUp2: " + ex.Message);
            }
            LavaThread.Start();
            Console.WriteLine("All Should Be DONE!");
        }

        /// <summary> Occurs when a player moves.</summary>
        /// <param name="PlayerMovedEventArgs">Player who raised this event.</param>
        /// <remarks> Checks for collisions using CheckForCollision(e.Player).</remarks>
        private static void PlayerMoved(object sender, PlayerMovedEventArgs e)
        {
            if (e.Player.World == world)
            {
                CheckForCollision(e.Player);
            }
        }

        /// <summary> Checks if any players should be exploded.</summary>
        /// <param name="Player">Player who placed the TNT.</param>
        /// <param name="coords">Coordinates at which the TNT was placed.</param>
        /// <remarks> Add a cooldown later. Works 95% of the time.</remarks>		
        private static void Explode(Player player, Vector3I coords)
        {
            try
            {
                bool matchFound = false;
                foreach (Player p in world.Players)
                {
                    if (p == player) continue;
                    Vector3I cpPos = p.Position.ToBlockCoords();
                    Vector3I cpPosMax = new Vector3I(cpPos.X + 2, cpPos.Y + 2, cpPos.Z + 2);
                    Vector3I cpPosMin = new Vector3I(cpPos.X - 2, cpPos.Y - 2, cpPos.Z - 2);
                    for (; cpPos.X <= cpPosMax.X; cpPos.X++)
                    {
                        for (; cpPos.Y <= cpPosMax.Y; cpPos.Y++)
                        {
                            for (; cpPos.Z <= cpPosMax.Z; cpPos.Z++)
                            {
                                //BoundingBox doesn't check if it's outside the world, 
                                //Which caused it to fail with explosions on world edges. >.>
                                if (world.Map.InBounds(cpPos) & cpPos == coords)
                                {
                                    matchFound = true;
                                    break;
                                }
                            }
                            cpPos.Z = cpPosMin.Z;
                        }
                        cpPos.Y = cpPosMin.Y;
                    }

                    //Only go here if someone should explode.
                    if (matchFound && p.Team != player.Team)
                    {
                        if (p.Team == "Blue")
                        {
                            world.Players.Message("&4" + player.Name + " &cexploded &1" + p.Name);
                            p.TeleportTo(blueSpawn);
                            if (p.IsHoldingFlag)
                            {
                                world.Players.Message("&1" + p.Name + " &cdropped the flag for the &1Blue&c team!");
                                blueHasFlag = false;
                                p.IsHoldingFlag = false;
                                world.Map.QueueUpdate(new BlockUpdate(Player.Console, redFlag, Block.Red));
                            }
                        }
                        else if (p.Team == "Red")
                        {
                            world.Players.Message("&1" + player.Name + " &cexploded &4" + p.Name);
                            p.TeleportTo(redSpawn);
                            if (p.IsHoldingFlag)
                            {
                                world.Players.Message("&4" + p.Name + " &cdropped the flag for the &4Red&c team!");
                                redHasFlag = false;
                                p.IsHoldingFlag = false;
                                world.Map.QueueUpdate(new BlockUpdate(Player.Console, blueFlag, Block.Blue));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in the exploder");
                Console.WriteLine("ExplodeError: " + ex.Message);
            }
        }

        /// <summary> Handles the exploding of TNT.</summary>
        /// <param name="Player">Player who placed the TNT.</param>
        /// <param name="coords">Coordinates at which the block is being placed.</param>
        /// <remarks> Add a cooldown later, so no TNT spamming.</remarks>
        private static void HandleTNT(Player player, Vector3I coords)
        {
            try
            {
                Random rnd = new Random();
                List<Vector3I> positions = new List<Vector3I>();
                //Look into making an array later.

                int[] temppos = new int[60];
                for (int i = 0; i < 60; i++)
                {
                    //Fill the array with 60 different possible coordinates.
                    temppos[i] = rnd.Next(-2, 2);
                }
                for (int i = 0; i < 20; i++)
                {
                    //Use first 20 for X, 20 - 40 for Y, 40 - 60 for Z.
                    Vector3I temp = new Vector3I(coords.X + temppos[i], coords.Y + temppos[i + 20], coords.Z + temppos[i + 40]);
                    positions.Add(temp);
                }
                for (int i = 0; i < positions.Count; i++)
                {
                    if (positions[i] == redFlag) continue;
                    if (positions[i] == blueFlag) continue;
                    if (world.Map.GetBlock(positions[i]) == Block.Water) continue;
                    //Don't explode the flags.
                    //Skip water...
                    else
                    {
                        int effect;
                        if (i < 3 & world.Map.InBounds(positions[i].X, positions[i].Y, positions[i].Z))
                        {
                            //Only place if inside the map.
                            if (world.Map.GetBlock(positions[i]) == Block.Red || world.Map.GetBlock(positions[i]) == Block.Blue)
                            {
                                effect = rnd.Next(100);
                                if (effect >= 99)
                                {
                                    world.Map.QueueUpdate(new BlockUpdate(Player.Console, positions[i], Block.Lava));
                                    Lava.Add(positions[i]);
                                }
                                else if (effect >= 75) world.Map.QueueUpdate(new BlockUpdate(Player.Console, positions[i], Block.Stone));
                            }
                            else if (world.Map.GetBlock(positions[i]) == Block.Stone)
                            {
                                effect = rnd.Next(100);
                                if (effect >= 99)
                                {
                                    world.Map.QueueUpdate(new BlockUpdate(Player.Console, positions[i], Block.Lava));
                                    Lava.Add(positions[i]);
                                }
                                else if (effect >= 75) world.Map.QueueUpdate(new BlockUpdate(Player.Console, positions[i], Block.Cobblestone));
                            }
                            else if (world.Map.GetBlock(positions[i]) == Block.Cobblestone)
                            {
                                effect = rnd.Next(100);
                                if (effect >= 99)
                                {
                                    world.Map.QueueUpdate(new BlockUpdate(Player.Console, positions[i], Block.Lava));
                                    Lava.Add(positions[i]);
                                }
                                else if (effect >= 75) world.Map.QueueUpdate(new BlockUpdate(Player.Console, positions[i], Block.Gravel));
                            }
                            else if (world.Map.GetBlock(positions[i]) == Block.Gravel)
                            {
                                effect = rnd.Next(100);
                                if (effect >= 99)
                                {
                                    world.Map.QueueUpdate(new BlockUpdate(Player.Console, positions[i], Block.Lava));
                                    Lava.Add(positions[i]);
                                }
                                else if (effect >= 75) world.Map.QueueUpdate(new BlockUpdate(Player.Console, positions[i], Block.Air));
                            }
                            else if (world.Map.GetBlock(positions[i]) == Block.Grass)
                            {
                                effect = rnd.Next(100);
                                if (effect >= 75)
                                {
                                    world.Map.QueueUpdate(new BlockUpdate(Player.Console, positions[i], Block.Lava));
                                    Lava.Add(positions[i]);
                                }
                                else if (effect >= 25) world.Map.QueueUpdate(new BlockUpdate(Player.Console, positions[i], Block.Dirt));
                            }
                            else if (world.Map.GetBlock(positions[i]) == Block.Water || world.Map.GetBlock(positions[i]) == Block.Lava)
                            {
                                //Don't blow up pre-existing water or lava.
                            }
                            else
                            {
                                world.Map.QueueUpdate(new BlockUpdate(Player.Console, positions[i], Block.Lava));
                                Lava.Add(positions[i]);
                            }
                        }
                        //Fill the rest with air.
                        else if (i >= 3 & world.Map.InBounds(positions[i].X, positions[i].Y, positions[i].Z))
                        {
                            //Only place if inside the map.
                            if (world.Map.GetBlock(positions[i]) == Block.Red || world.Map.GetBlock(positions[i]) == Block.Blue)
                            {
                                effect = rnd.Next(100);
                                if (effect >= 99) world.Map.QueueUpdate(new BlockUpdate(Player.Console, positions[i], Block.Stone));
                            }
                            else if (world.Map.GetBlock(positions[i]) == Block.Stone)
                            {
                                effect = rnd.Next(100);
                                if (effect >= 99) world.Map.QueueUpdate(new BlockUpdate(Player.Console, positions[i], Block.Cobblestone));
                            }
                            else if (world.Map.GetBlock(positions[i]) == Block.Cobblestone)
                            {
                                effect = rnd.Next(100);
                                if (effect >= 99) world.Map.QueueUpdate(new BlockUpdate(Player.Console, positions[i], Block.Gravel));
                            }
                            else if (world.Map.GetBlock(positions[i]) == Block.Gravel)
                            {
                                effect = rnd.Next(100);
                                if (effect >= 99) world.Map.QueueUpdate(new BlockUpdate(Player.Console, positions[i], Block.Air));
                            }
                            else if (world.Map.GetBlock(positions[i]) == Block.Grass)
                            {
                                effect = rnd.Next(100);
                                if (effect >= 33) world.Map.QueueUpdate(new BlockUpdate(Player.Console, positions[i], Block.Dirt));
                            }
                            else if (world.Map.GetBlock(positions[i]) == Block.Water || world.Map.GetBlock(positions[i]) == Block.Lava)
                            {
                                //Don't blow up pre-existing water or lava.
                            }
                        }
                    }
                }
                //Remove original TNT block, check if someone should explode.
                Explode(player, coords);
                world.Map.QueueUpdate(new BlockUpdate(Player.Console, coords, Block.Air));
                world.Map.ProcessUpdates();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in the TNT Handler...");
                Console.WriteLine("TNTHandler: " + ex.Message);
            }
        }

        /// <summary> Checks if anyone collided with the source player. Accounts for teams.</summary>
        /// <param name="Player">Player with which we check for collisions.</param>
        /// <remarks> Look at optimizing later. Ugly if / foreach statement.</remarks>		
        public static bool CheckForCollision(Player p)
        {
            try
            {
                foreach (Zone z in world.Map.Zones)
                {
                    if (z.Name.ToLower() == p.Team.ToLower() & z.Bounds.Contains(p.Position.ToBlockCoords()))
                    {
                        //ToLower is more safe.
                        foreach (Player player in world.Players)
                        {
                            if (p.Position.ToBlockCoords().X == player.Position.ToBlockCoords().X &&
                               p.Position.ToBlockCoords().Y == player.Position.ToBlockCoords().Y)
                            {

                                if (p.Team != player.Team)
                                {                                    
                                    if (player.Team == "Blue")
                                    {
                                        world.Players.Message("&4" + p.Name + " &stagged &1" + player.Name);
                                        player.TeleportTo(blueSpawn);
                                        if (player.IsHoldingFlag)
                                        {
                                            world.Players.Message("&1" + player.Name + " &cdropped the flag for the &1Blue&c team!");
                                            blueHasFlag = false;
                                            player.IsHoldingFlag = false;
                                            world.Map.QueueUpdate(new BlockUpdate(Player.Console, redFlag, Block.Red));
                                        }
                                        return true;

                                    }
                                    else if (player.Team == "Red")
                                    {
                                        world.Players.Message("&1" + p.Name + " &stagged &4" + player.Name);
                                        player.TeleportTo(redSpawn);
                                        if (player.IsHoldingFlag)
                                        {
                                            world.Players.Message("&4" + player.Name + " &cdropped the flag for the &4Red&c team!");
                                            redHasFlag = false;
                                            player.IsHoldingFlag = false;
                                            world.Map.QueueUpdate(new BlockUpdate(Player.Console, blueFlag, Block.Blue));
                                        }
                                        return true;
                                    }

                                }

                            }
                        }
                    }
                }
                //If no collision, reutrn false. This could be a void instead, since we never use the bool result.
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in the Collsion Detector...");
                Console.WriteLine("CollsionDetector: " + ex.Message);
                return false;
            }
        }

        /// <summary> Checks the scores of teams.</summary>
        /// <remarks> This is much more effective then using a while loop.</remarks>		
        public static void Check()
        {
            try
            {
                if (redScore > 4 || blueScore > 4)
                {
                    scoreCounter();
                    redScore = 0;
                    blueScore = 0;
                    //Reset scores.
                    world.Players.Message("Starting a new round.");
                    foreach (Player player in world.Players)
                    {
                        //Send players to their respective spawns.
                        if (blueTeam.Contains(player))
                        {
                            player.TeleportTo(blueSpawn);
                        }

                        else if (redTeam.Contains(player))
                        {
                            player.TeleportTo(redSpawn);
                        }
                    }
                }
                if (((redRoundsWon * 5) + redScore) > ((blueRoundsWon * 5) + blueScore)) {
                    world.Players.Message("&SScores so far: &4Red &a{0}&4:&f{2} &c<-- &1Blue &a{1}&1:&f{3}", redRoundsWon, blueRoundsWon, redScore, blueScore);
                } else if (((redRoundsWon * 5) + redScore) < ((blueRoundsWon * 5) + blueScore)) {
                    world.Players.Message("&SScores so far: &4Red &a{0}&4:&f{2} &9--> &1Blue &a{1}&1:&f{3}", redRoundsWon, blueRoundsWon, redScore, blueScore);
                } else {
                    world.Players.Message("&SScores so far: &4Red &a{0}&4:&f{2} &d<=> &1Blue &a{1}&1:&f{3}", redRoundsWon, blueRoundsWon, redScore, blueScore);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in the Check...");
                Console.WriteLine("Check: " + ex.Message);
            }
        }

        /// <summary> Occurs when someone placed a flag. Also, scores are raised here.</summary>
        /// <param name="sender">Player who raised the event.</param>
        /// <remarks> Lot of ifs. Look at reducing them later.</remarks>		
        private static void PlayerPlacing(object sender, PlayerPlacingBlockEventArgs e)
        {
            try
            {
                if (e.Player.World != world)
                {
                    //For everyone else on the server.
                    return;
                }
                //Only do this if we are placing, not cubodiding and undoing.
                //e.Player.Message(e.Context.ToString());
                if (!(e.OldBlock == Block.Air || e.NewBlock == Block.Air))
                {
                    e.Result = CanPlaceResult.CTFDenied;
                    //Unable to use Paint!
                }
                else if (e.NewBlock == Block.Blue && e.Player.Team == "Red")
                {
                    e.Player.Message("&sYou cannot place &9Blue&s blocks, you are on the &cRed&s Team!");
                    world.Map.QueueUpdate(new BlockUpdate(Player.Console, e.Coords, e.OldBlock));
                    world.Map.ProcessUpdates();
                    e.Result = CanPlaceResult.CTFDenied;
                }
                else if (e.NewBlock == Block.Red && e.Player.Team == "Blue")
                {
                    e.Player.Message("&sYou cannot place &cRed&s blocks, you are on the &9Blue&s Team!");
                    world.Map.QueueUpdate(new BlockUpdate(Player.Console, e.Coords, e.OldBlock));
                    world.Map.ProcessUpdates();
                    e.Result = CanPlaceResult.CTFDenied;
                }
                else if (e.Player.World == world)
                {
                    if (e.Context == BlockChangeContext.Manual || e.Context == BlockChangeContext.Unknown)
                    {
                        #region Is on the flags and block is air blue or red.
                        if ((e.Coords == redFlag || e.Coords == blueFlag) && (e.NewBlock == Block.Red
                            || e.NewBlock == Block.Blue || e.NewBlock == Block.Air))
                        {
                            #region Blue Flag Being Capped By Red Team.
                            if (e.Coords == blueFlag && e.Player.Team == "Red") //Red got flag.
                            {
                                if (blueHasFlag == true)
                                {
                                    e.Player.Message("You can't capture if you're not in posession of your own flag.");
                                    e.Result = CanPlaceResult.CTFDenied;
                                }
                                else
                                {
                                    e.Result = CanPlaceResult.Allowed;
                                    redHasFlag = true;
                                    e.Player.IsHoldingFlag = true;
                                    world.Players.Message("&4" + e.Player.Name + " &sgot the &1Blue&s flag.");
                                }
                            }
                            #endregion

                            #region Red Flag Being Capped By Blue Team.
                            if (e.Coords == redFlag && e.Player.Team == "Blue") //Blue got flag.
                            {
                                if (redHasFlag == true)
                                {
                                    e.Player.Message("You can't capture if you're not in posession of your own flag.");
                                    e.Result = CanPlaceResult.CTFDenied;
                                }
                                else
                                {
                                    e.Result = CanPlaceResult.Allowed;
                                    blueHasFlag = true;
                                    e.Player.IsHoldingFlag = true;
                                    world.Players.Message("&1" + e.Player.Name + " &sgot the &4Red&s flag.");
                                }
                            }
                            #endregion

                            #region Blue Flag Brought To Red Base.
                            if (e.Coords == redFlag && e.Player.Team == "Red") //Revert if no flag.
                            {
                                if (!e.Player.IsHoldingFlag)
                                {
                                    e.Player.Message("You don't have the &1Blue&s flag.");
                                    e.Result = CanPlaceResult.CTFDenied;
                                }
                                else
                                {
                                    world.Map.QueueUpdate(new BlockUpdate(Player.Console, blueFlag, Block.Blue));
                                    redScore++;
                                    redHasFlag = false;
                                    e.Player.IsHoldingFlag = false;
                                    world.Players.Message("&4" + e.Player.Name + " &sscored a point for &4Red&s team.");
                                    e.Result = CanPlaceResult.CTFDenied;
                                    Check();
                                }
                            }
                            #endregion

                            #region Red Flag Brought To Blue Base
                            if (e.Coords == blueFlag && e.Player.Team == "Blue") //Revert if no flag.
                            {
                                if (!e.Player.IsHoldingFlag)
                                {
                                    e.Player.Message("You don't have the flag.");
                                    e.Result = CanPlaceResult.CTFDenied;
                                }
                                else
                                {
                                    world.Map.QueueUpdate(new BlockUpdate(Player.Console, redFlag, Block.Red));//e.Player, redFlag, Block.Red));
                                    blueScore++;
                                    blueHasFlag = false;
                                    e.Player.IsHoldingFlag = false;
                                    world.Players.Message("&1" + e.Player.Name + " &sscored a point for the &1Blue&s team");
                                    e.Result = CanPlaceResult.CTFDenied;
                                    Check();
                                }
                            }
                            #endregion
                        }
                        #endregion

                        #region Handle TNT, but NOT if on the flags!
                        else if (e.NewBlock == Block.TNT)
                        {
                            //Don't blow up if you're on the flags.
                            if (e.Coords != redFlag && e.Coords != blueFlag)
                            {
                                HandleTNT(e.Player, e.Coords);
                                e.Result = CanPlaceResult.CTFDenied;
                                world.Map.QueueUpdate(new BlockUpdate(Player.Console, e.Coords, Block.Air));
                                world.Map.ProcessUpdates();
                            }
                        }
                        #endregion

                        #region Deny Block changes to flags!
                        else if (e.Coords == redFlag || e.Coords == blueFlag)
                        {
                            e.Result = CanPlaceResult.CTFDenied;
                        }
                        #endregion

                        #region Degrade blocks when hit.
                        else
                        {
                            //NOT AT A FLAG, NORMAL PROCEDURE:
                            Random rnd = new Random();
                            int effect;
                            if (e.NewBlock == Block.Lava || e.NewBlock == Block.Water)
                            {
                                e.Player.Message("You are not able to place water or lava blocks in CTF!");
                                e.Result = CanPlaceResult.CTFDenied;
                                world.Map.QueueUpdate(new BlockUpdate(Player.Console, e.Coords, e.OldBlock));
                            }
                            else if (e.OldBlock == Block.Red || e.OldBlock == Block.Blue)
                            {
                                effect = rnd.Next(100);
                                if (effect >= 99) world.Map.QueueUpdate(new BlockUpdate(Player.Console, e.Coords, Block.Cobblestone));
                                else if (effect >= 75) world.Map.QueueUpdate(new BlockUpdate(Player.Console, e.Coords, Block.Stone));
                                e.Result = CanPlaceResult.CTFDenied;
                            }
                            else if (e.OldBlock == Block.Stone)
                            {
                                effect = rnd.Next(100);
                                if (effect >= 99) world.Map.QueueUpdate(new BlockUpdate(Player.Console, e.Coords, Block.Gravel));
                                else if (effect >= 75) world.Map.QueueUpdate(new BlockUpdate(Player.Console, e.Coords, Block.Cobblestone));
                                else world.Map.QueueUpdate(new BlockUpdate(Player.Console, e.Coords, e.OldBlock));
                                e.Result = CanPlaceResult.CTFDenied;
                            }
                            else if (e.OldBlock == Block.Cobblestone)
                            {
                                effect = rnd.Next(100);
                                if (effect >= 99) world.Map.QueueUpdate(new BlockUpdate(Player.Console, e.Coords, Block.Air));
                                else if (effect >= 75) world.Map.QueueUpdate(new BlockUpdate(Player.Console, e.Coords, Block.Gravel));
                                else world.Map.QueueUpdate(new BlockUpdate(Player.Console, e.Coords, e.OldBlock));
                                e.Result = CanPlaceResult.CTFDenied;
                            }
                            else if (e.OldBlock == Block.Gravel)
                            {
                                effect = rnd.Next(100);
                                if (effect >= 75) world.Map.QueueUpdate(new BlockUpdate(Player.Console, e.Coords, Block.Air));
                                else world.Map.QueueUpdate(new BlockUpdate(Player.Console, e.Coords, e.OldBlock));
                                e.Result = CanPlaceResult.CTFDenied;
                            }
                            else if (e.OldBlock == Block.Grass)
                            {
                                effect = rnd.Next(100);
                                if (effect >= 25) world.Map.QueueUpdate(new BlockUpdate(Player.Console, e.Coords, Block.Dirt));
                                else world.Map.QueueUpdate(new BlockUpdate(Player.Console, e.Coords, e.OldBlock));
                                e.Result = CanPlaceResult.CTFDenied;
                            }
                            else if (e.OldBlock == Block.Water)
                            {
                                world.Map.QueueUpdate(new BlockUpdate(Player.Console, e.Coords, e.OldBlock));
                                e.Result = CanPlaceResult.CTFDenied;
                            }
                            else world.Map.QueueUpdate(new BlockUpdate(Player.Console, e.Coords, e.NewBlock));
                        }
                        #endregion
                    }
                    #region Deny draw or paint over flags.
                    else if (e.Coords == redFlag || e.Coords == blueFlag)
                    {
                        e.Result = CanPlaceResult.CTFDenied;
                    }
                    #endregion

                    #region Deny Lava and Water Placing.
                    else if (e.NewBlock == Block.Lava || e.NewBlock == Block.Water)
                    {
                        e.Result = CanPlaceResult.CTFDenied;
                    }
                    #endregion
                }

                //Deny if not on this world!
                else e.Result = CanPlaceResult.CTFDenied;

                //Make the changes physically happen.
                if (world != null) world.Map.ProcessUpdates();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in the Player Placing");
                Console.WriteLine("PlayerPlacing: " + ex.Message);
            }
        }

        /// <summary> Occurs when someone moved worlds.</summary>
        /// <param name="sender">Player who raised the event.</param>
        /// <remarks> Chooses or removes players from teams.</remarks>			
        public static void PlayerChangedWorld(object sender, PlayerJoinedWorldEventArgs e)
        {
            if (e.OldWorld != null) //Prevent null exception.
            {
                if (e.NewWorld == world)
                {
                    if (!blueTeam.Contains(e.Player) && !redTeam.Contains(e.Player))
                    {
                        //Add player to a team.
                        ChooseTeam(e.Player, e.NewWorld);
                    }
                }
                else if (e.OldWorld == world && e.NewWorld != world)
                {
                    if (e.OldWorld.Name == "CTF" && e.NewWorld.Name == "CTF") {
                        //Do Nothing.
                    } else {
                        //Remove player.
                        RemovePlayer(e.Player, e.OldWorld);
                        if (blueTeam.Count + redTeam.Count == 0) {
                            e.Player.Message("&sYou were the last player in the game, and thus, the game has ended.");
                            Stop();
                        }
                    }
                }
            }
        }

        /// <summary> Occurs at the end of a round.</summary>
        /// <remarks> No checking if it was a draw, since that's impossible.</remarks>					
        public static void scoreCounter()
        {
            if (blueScore > redScore)
            {
                blueRoundsWon++;
                if (world != null) world.Players.Message("&SThe &1Blue&S team won that round: &1{0} &S- &4{1}", blueScore, redScore);
            }
            if (redScore > blueScore)
            {
                redRoundsWon++;
                if (world != null) world.Players.Message("&SThe &4Red&S team won that round: &4{0} &S- &1{1}", redScore, blueScore);
            }
        }


        /// <summary> Adds a player to the team.</summary>
        /// <remarks> Remembers to check if a player is in a team or not.</remarks>
        public static void ChooseTeam(Player player, World world)
        {
            try
            {
                if (!blueTeam.Contains(player) && !redTeam.Contains(player))
                {
                    if (blueTeam.Count > redTeam.Count)
                    {
                        redTeam.Add(player);
                        player.Message("&SAdding you to the &4Red Team");
                        player.TeleportTo(redSpawn);
                        player.Position.R = (byte)100;
                        player.Team = "Red";
                        player.IsPlayingCTF = true;
                        if (player.Supports(CpeExtension.HeldBlock))
                        {
                            player.Send(Packet.MakeHoldThis(Block.TNT, false));
                            player.Send(Packet.MakeHoldThis(Block.TNT, true));
                        }
                    }
                    else if (blueTeam.Count < redTeam.Count)
                    {
                        blueTeam.Add(player);
                        player.Message("&SAdding you to the &1Blue Team");
                        player.TeleportTo(blueSpawn);
                        player.Team = "Blue";
                        player.IsPlayingCTF = true;
                        if (player.Supports(CpeExtension.HeldBlock))
                        {
                            player.Send(Packet.MakeHoldThis(Block.TNT, false));
                        }
                    }
                    else
                    {
                        redTeam.Add(player);
                        player.Message("&SAdding you to the &4Red Team");
                        player.TeleportTo(redSpawn);
                        player.Position.R = (byte)100;
                        player.Team = "Red";
                        player.IsPlayingCTF = true;
                        if (player.Supports(CpeExtension.HeldBlock))
                        {
                            player.Send(Packet.MakeHoldThis(Block.TNT, false));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Team Selector...");
                Console.WriteLine("TeamSelector: " + ex.Message);
            }
        }

        /// <summary> Removes a player from a team.</summary>
        /// <remarks> Remembers to check if a player is in a team or not.</remarks>				
        public static void RemovePlayer(Player player, World world)
        {
            try
            {
                if (player.Supports(CpeExtension.MessageType)) {
                    player.Send(Packet.Message((byte)MessageType.Status3, ""));
                }
                if (blueTeam.Contains(player))
                {
                    blueTeam.Remove(player);
                    player.Message("&SRemoving you from the game");
                    player.IsPlayingCTF = false;
                    if (player.IsHoldingFlag)
                    {
                        world.Players.Message("&cFlag holder &1" + player.Name + " &cquit CTF, thus dropping the flag for the &1Blue&c team!");
                        blueHasFlag = false;
                        player.IsHoldingFlag = false;
                        world.Map.QueueUpdate(new BlockUpdate(Player.Console, redFlag, Block.Red));
                    }
                    if (player.Supports(CpeExtension.HeldBlock))
                    {
                        player.Send(Packet.MakeHoldThis(Block.Stone, false));
                    }
                }
                else if (redTeam.Contains(player))
                {
                    redTeam.Remove(player);
                    player.Message("&SRemoving you from the game");
                    player.IsPlayingCTF = false;
                    if (player.IsHoldingFlag)
                    {
                        world.Players.Message("&cFlag holder &c" + player.Name + " &cquit CTF, thus dropping the flag for the &4Red&c team!");
                        redHasFlag = false;
                        player.IsHoldingFlag = false;
                        world.Map.QueueUpdate(new BlockUpdate(Player.Console, blueFlag, Block.Blue));
                    }
                    if (player.Supports(CpeExtension.HeldBlock))
                    {
                        player.Send(Packet.MakeHoldThis(Block.Stone, false));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in the Player Remover");
                Console.WriteLine("PlayerRemover: " + ex.Message);
            }
        }


        /// <summary> Starts a game of CTF on the specified world. </summary>
        /// <param name="World"> World to begin CTF on. Cannot be null. </param>
        /// <param name="Player">Player who called the command. Used to send back error messages.</param>
        /// <remarks> Should be pretty reliable.</remarks>
        public static void Start(Player player, World world)
        {
            try
            {
                GameThread = new Thread(new ThreadStart(delegate
                {
                    Server.Players.Message("{0}&S Started a game of CTF on world {1}",
                                           player.ClassyName, world.ClassyName);

                    bool immediatestart = false;
                    if (immediatestart)
                    {
                        if (world != null) world.Players.Message("&SThe game will start immediatly.");
                    }
                    else
                    {
                        if (world != null) world.Players.Message("&SThe game will start in ten seconds.");
                        Thread.Sleep(7000);
                        if (world != null) world.Players.Message("&SGame Starting: 3");
                        Thread.Sleep(1000);
                        if (world != null) world.Players.Message("&SGame Starting: 2");
                        Thread.Sleep(1000);
                        if (world != null) world.Players.Message("&SGame Starting: 1");
                        Thread.Sleep(1000);
                    }
                    Player[] cache = world.Players;
                    Init(player, world);
                    foreach (Player p in cache)
                    {
                        ChooseTeam(p, world);
                        //We assign the teams first, THEN We flush the world and stuff!
                    }

                }));
                GameThread.Start();
                LavaThread = new Thread(new ThreadStart(delegate
                    {
                        while (true)
                        {
                            try
                            {

                                Thread.Sleep(500);
                                while (Lava.Count > 0)
                                {
                                    WorldManager.FindWorldOrPrintMatches(Player.Console, "CTF").Map.QueueUpdate(new BlockUpdate(Player.Console, Lava[0], Block.Air));
                                    Lava.RemoveAt(0);
                                }

                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Error In Lava Thread: {0}: {1} {2}", ex.GetType().Name, ex.Message, ex.TargetSite);
                                Console.WriteLine(world.Name);
                                Console.WriteLine(world.Map);
                                Console.WriteLine(ex.StackTrace);
                                Console.WriteLine(ex.InnerException);
                                break;
                            }
                        }
                    }));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error starting CTF");
                Console.WriteLine("StartCTF: " + ex.Message);
            }
        }

        /// <summary> Stops the game of CTF.</summary>
        /// <remarks> Also wipes scores.</remarks>		
        public static void Stop() {
            try {
                if (world != null)
                    world.Players.Message(
                        "&SThe game has ended! The scores are: \n" + "&4Red &7{0}&4:&f{2} &S- &1Blue &7{1}&1:&f{3}",
                        redRoundsWon, blueRoundsWon, redScore, blueScore);
                instances = 0;
                Player.PlacingBlock -= PlayerPlacing;
                Player.JoinedWorld -= PlayerChangedWorld;
                Player.Moved -= PlayerMoved;
                //Remove event handlers.
                Player[] cache = world.Players;
                foreach (Player p in cache) {
                    p.IsPlayingCTF = false;
                    p.IsHoldingFlag = false;
                    if (p.Supports(CpeExtension.MessageType)) {
                        p.Send(Packet.Message((byte)MessageType.Status3, " "));
                    }
                    if (p.Supports(CpeExtension.HeldBlock)) {
                        p.Send(Packet.MakeHoldThis(Block.Stone, false));
                    }
                }
                blueTeam.Clear();
                redTeam.Clear();
                blueScore = 0;
                redScore = 0;
                blueRoundsWon = 0;
                redRoundsWon = 0;
                GameThread.Abort();
                LavaThread.Abort();
            } catch (Exception ex) {
                Console.WriteLine("Error stopping CTF...");
                Console.WriteLine("StopCTF: " + ex.Message);
            }
        }
    }
}