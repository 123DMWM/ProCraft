// ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using fCraft;
using fCraft.Events;

namespace fCraft.Games {
	
	public static partial class CTF {
		
		public static CtfTeam RedTeam = new CtfTeam( "&4", "Red", "AA0000", Block.Red );
		public static CtfTeam BlueTeam = new CtfTeam( "&1", "Blue", "0000AA", Block.Blue );
		public static bool GameRunning;
		
		static List<Vector3I> Lava = new List<Vector3I>();
		static World world;
		internal static Map map;
		static bool lavaTaskStarted;
		static readonly object lavaLock = new object();
		static Random rnd = new Random();
		
		static CtfTeam Opposition(CtfTeam team) {
			return team == RedTeam ? BlueTeam : RedTeam;
		}
		
		static void PlayerMoved(object sender, PlayerMovedEventArgs e) {
			Player p = e.Player;
			if (p.World != world || !p.IsPlayingCTF) return;
			
			foreach (Player other in world.Players) {
				if (!other.IsPlayingCTF) continue;
				if (p.Team != other.Team && p.Bounds.Intersects(other.Bounds)) {
					if (p.Team.TaggingBounds.Intersects(other.Bounds))
						Kill(p, other, " &stagged ");
					else if(other.Team.TaggingBounds.Intersects(p.Bounds))
						Kill(other, p, " &stagged " );
				}
			}
		}

		static void KillExplosion(Player player, Vector3I coords) {
			foreach (Player p in world.Players) {
				if (p == player)
					continue;
				Vector3I cpPos = p.Position.ToBlockCoords();
				Vector3I cpPosMax = new Vector3I(cpPos.X + 2, cpPos.Y + 2, cpPos.Z + 2);
				Vector3I cpPosMin = new Vector3I(cpPos.X - 2, cpPos.Y - 2, cpPos.Z - 2);
				BoundingBox bounds = new BoundingBox(cpPosMin, cpPosMax);
				
				if (bounds.Contains(coords) && p.Team != player.Team)
					Kill(player, p, " &cexploded ");
			}
		}
		
		static void Kill(Player killer, Player p, string message) {
			DateTime now = DateTime.UtcNow;
			if( (now - p.LastKilled).TotalSeconds < 1)
				return;
			
			world.Players.Message(killer.Team.Color + killer.Name +
			                      message + p.Team.Color + p.Name);
			p.LastKilled = DateTime.UtcNow;
			p.TeleportTo(p.Team.Spawn);
			
			if (p.IsHoldingFlag) {
				world.Players.Message("{0} &cdropped the flag for the {1}&c team!", 
				                      p.ClassyName, p.Team.ClassyName);
				p.Team.HasFlag = false;
				p.IsHoldingFlag = false;
				world.Map.QueueUpdate(new BlockUpdate(Player.Console,
				                                      killer.Team.FlagPos, killer.Team.FlagBlock));
			}
		}

		public static void Check() {
			if (RedTeam.Score > 4 || BlueTeam.Score > 4) {
				UpdateRoundScores();
				RedTeam.Score = 0;
				BlueTeam.Score = 0;
				
				world.Players.Message("&sStarting the next round.");
				foreach (Player player in world.Players)
					player.TeleportTo(player.Team.Spawn);
			}
			
			string op = "&S<=>";
			if (RedTeam.TotalScore > BlueTeam.TotalScore) {
				op = "&S-->";
			} else if (BlueTeam.TotalScore > RedTeam.TotalScore) {
				op = "&S<--";
			}
			world.Players.Message("&SScores so far: {0}{1} &a{2}{0}:&f{3} {4} {5}{6} &a{7}{5}:&f{8}",
			                      RedTeam.Color, RedTeam.Name, RedTeam.RoundsWon, RedTeam.Score, op,
			                      BlueTeam.Color, BlueTeam.Name, BlueTeam.RoundsWon, BlueTeam.Score);
		}
		
		public static void PrintCtfState(Player player) {
			if (player.IsPlayingCTF && player.Supports(CpeExt.MessageType)) {
				player.Send(Packet.Message((byte)MessageType.BottomRight1, ""));
				string op = "&d<=>";
				if (CTF.RedTeam.TotalScore > CTF.BlueTeam.TotalScore) {
					op = "&S-->";
				} else if (CTF.RedTeam.TotalScore < CTF.BlueTeam.TotalScore) {
					op = "&S<--";
				}
				player.Message((byte)MessageType.BottomRight3, "{0}{1} &a{2}{0}:&f{3} {4} {5}{6} &a{7}{5}:&f{8}",
				               CTF.RedTeam.Color, CTF.RedTeam.Name, CTF.RedTeam.RoundsWon, CTF.RedTeam.Score, op,
				               CTF.BlueTeam.Color, CTF.BlueTeam.Name, CTF.BlueTeam.RoundsWon, CTF.BlueTeam.Score);
				
				var flagholder = player.World.Players.Where(p => p.IsHoldingFlag);
				if (flagholder.FirstOrDefault() == null) {
					player.Send(Packet.Message((byte)MessageType.BottomRight2, "&sNo one has the flag!"));
				} else if (CTF.RedTeam.HasFlag) {
					player.Message((byte)MessageType.BottomRight2, "{0} &shas the {1}&s flag!",
					               flagholder.First().ClassyName, CTF.BlueTeam.ClassyName);
				} else if (CTF.BlueTeam.HasFlag) {
					player.Message((byte)MessageType.BottomRight2,"{0} &shas the {1}&s flag!",
					               flagholder.First().ClassyName, CTF.RedTeam.ClassyName);
				}
				
				if (player.Team != null) {
					player.Send(Packet.Message((byte)MessageType.Status3,
					                           "&sTeam: " + player.Team.ClassyName));
				} else {
					player.Send(Packet.Message((byte)MessageType.Status3, "&sTeam: &0None"));
				}
			}
			
			if (player.IsPlayingCTF && player.Supports(CpeExt.EnvColors)) {
				string color = null;
				if (CTF.RedTeam.Score > CTF.BlueTeam.Score) {
					color = CTF.RedTeam.EnvColor;
				} else if (CTF.BlueTeam.Score > CTF.RedTeam.Score) {
					color = CTF.BlueTeam.EnvColor;
				} else {
					color = Mix(CTF.RedTeam.EnvColor, CTF.BlueTeam.EnvColor);
				}
				player.Send(Packet.MakeEnvSetColor((byte)EnvVariable.SkyColor, color));
				player.Send(Packet.MakeEnvSetColor((byte)EnvVariable.FogColor, color));
			}
		}
		
		static string Mix(string a, string b) {
			int colA = int.Parse(a, NumberStyles.HexNumber);
			int colB = int.Parse(b, NumberStyles.HexNumber);
			int aR = colA & 0xFF0000, bR = colB & 0xFF0000;
			int aG = colA & 0x00FF00, bG = colB & 0x00FF00;
			int aB = colA & 0x0000FF, bB = colB & 0x0000FF;
			
			int mix = ((aR + bR) / 2) | ((aG + bG) / 2) | ((aB + bB) / 2);
			return mix.ToString( "X6" );
		}

		static void PlayerChangedWorld(object sender, PlayerJoinedWorldEventArgs e) {
			if (e.OldWorld == null) return;
			
			if (e.NewWorld == world) {
				if (!BlueTeam.Has(e.Player) && !RedTeam.Has(e.Player)) {
					ChooseTeamFor(e.Player, e.NewWorld);
				}
			} else if (e.OldWorld == world) {
				RemovePlayer(e.Player, e.OldWorld);
				
				if (BlueTeam.Count + RedTeam.Count == 0) {
					e.Player.Message("&sYou were the last player in the game, and thus, the game has ended.");
					Stop();
				}
			}
		}

		static void UpdateRoundScores() {
			CtfTeam winner = null;
			if (BlueTeam.Score > RedTeam.Score) {
				BlueTeam.RoundsWon++;
				winner = BlueTeam;			
			} else if (RedTeam.Score > BlueTeam.Score) {
				RedTeam.RoundsWon++;
				winner = RedTeam;
			}
			if(winner == null) // should not happen
				return;
			
			CtfTeam loser = Opposition(winner);
			world.Players.Message("&SThe {0}{1}&S team won that round: {0}{2} &S- {3}{4}", 
			                      winner.Color, winner.Name, winner.Score, loser.Color, loser.Score);
		}

		static void ChooseTeamFor(Player p, World world) {
			if (BlueTeam.Has(p) || RedTeam.Has(p))
				return;
			
			if (BlueTeam.Count > RedTeam.Count)
				AddPlayerToTeam(RedTeam, p);
			else if (BlueTeam.Count < RedTeam.Count)
				AddPlayerToTeam(BlueTeam, p);
			else
				AddPlayerToTeam(RedTeam, p);
		}
		
		static void AddPlayerToTeam(CtfTeam team, Player p) {
			team.Players.Add(p);
			p.Message("&SAdding you to the " + team.ClassyName + " team");
			p.TeleportTo(team.Spawn);
			p.Team = team;
			
			p.IsPlayingCTF = true;
			if (p.Supports(CpeExt.HeldBlock))
				p.Send(Packet.MakeHoldThis(Block.TNT, false));
		}
		
		public static void SwitchTeamTo(Player player, CtfTeam newTeam, bool unbalanced) {
			CtfTeam oldTeam = newTeam == RedTeam ? BlueTeam : RedTeam;
			player.Message("You have switched to the {0}&s team.", newTeam.ClassyName);
			if (unbalanced)
				player.Message( "&sThe teams are now unbalanced!");
			
			oldTeam.Players.Remove(player);
			if (!newTeam.Has(player))
				newTeam.Players.Add(player);
			
			if (player.IsHoldingFlag) {
				world.Players.Message("&cFlag holder {0} &cswitched to the {1}&c team, " +
				                      "thus dropping the flag for the {2}&c team!",
				                      player.Name, newTeam.ClassyName, oldTeam.ClassyName );
				oldTeam.HasFlag = false;
				player.IsHoldingFlag = false;
				world.Map.QueueUpdate(new BlockUpdate(Player.Console, newTeam.FlagPos, newTeam.FlagBlock));
			}
			player.Team = newTeam;
			player.TeleportTo(newTeam.Spawn);
		}

		public static void RemovePlayer(Player player, World world) {
			if (player.Supports(CpeExt.MessageType))
				player.Send(Packet.Message((byte)MessageType.Status3, ""));
			
			if (BlueTeam.Has(player))
				RemovePlayerFromTeam(player, RedTeam);
			else if (RedTeam.Has(player))
				RemovePlayerFromTeam(player, BlueTeam);
			
			foreach (Player p in world.Players) {
				if (!p.Supports(CpeExt.ExtPlayerList) && !p.Supports(CpeExt.ExtPlayerList2))
					continue;
				p.Send(Packet.MakeExtRemovePlayerName(player.NameID));
			}
		}
		
		static void RemovePlayerFromTeam(Player p, CtfTeam opposingTeam) {
			p.Team.Players.Remove(p);
			p.Message("&SRemoving you from the game");
			if (p.IsHoldingFlag) {
				world.Players.Message("&cFlag holder " + p.ClassyName + " &cleft CTF, " +
				                      "thus dropping the flag for the " + p.Team.ClassyName + " team!");
				p.Team.HasFlag = false;
				p.IsHoldingFlag = false;
				world.Map.QueueUpdate(new BlockUpdate(Player.Console, opposingTeam.FlagPos,
				                                      opposingTeam.FlagBlock));
			}	
			
			p.IsPlayingCTF = false;
			p.Team = null;		
			if (p.Supports(CpeExt.HeldBlock))
				p.Send(Packet.MakeHoldThis(Block.Stone, false));
		}
		
		public static void Start(Player player, World world) {
			Server.Players.Message("{0}&S Started a game of CTF on world {1}",
			                       player.ClassyName, world.ClassyName);
			world.Players.Message("&SThe game will start in ten seconds.");
			GameRunning = true;
			
			Scheduler.NewTask(t => world.Players.Message("&SGame Starting: 3"))
				.RunOnce(TimeSpan.FromSeconds(7));
			Scheduler.NewTask(t => world.Players.Message("&SGame Starting: 2"))
				.RunOnce(TimeSpan.FromSeconds(8));
			Scheduler.NewTask(t => world.Players.Message("&SGame Starting: 1"))
				.RunOnce(TimeSpan.FromSeconds(9));
			Scheduler.NewTask(StartGame, player)
				.RunOnce(TimeSpan.FromSeconds(10));
			
			if (!lavaTaskStarted)
				Scheduler.NewTask(LavaCallback)
					.RunForever(TimeSpan.FromSeconds(0.5));
			lavaTaskStarted = true;
		}
		
		static void StartGame(SchedulerTask task) {
			if (!GameRunning && world != null) {
				world.Players.Message("&SCTF game was aborted.");
				return;
			}
			
			Player player = (Player)task.UserState;
			Player[] cache = player.World.Players;
			Init(player);
			
			foreach (Player p in cache)
				ChooseTeamFor(p, world);
		}
		
		static void Init(Player player) {
			world = player.World;
			if (!GameRunning)
				return;
			
			//bool foundBackup = true; //not used
			try {
				Map backupMap = MapConversion.MapUtility.Load("./maps/CTFBackup.fcm");
				world.MapChangedBy = player.Name;
				world = world.ChangeMap(backupMap);
			} catch (Exception ex) {
				player.Message( "Could not load CTF backup map.");
				Logger.Log(LogType.Error,
				           "Error loading CTF backup map: {0}", ex.Message);
				//foundBackup = false;
				map = world.LoadMap();
			}
			
			InitProperties();
			Player.PlacingBlock += PlayerPlacing;
			Player.JoinedWorld += PlayerChangedWorld;
			Player.Moved += PlayerMoved;
			RedTeam.RoundsWon = 0;
			BlueTeam.RoundsWon = 0;
			InitZones();
		}
		
		static void InitProperties() {
			if (map.Metadata.ContainsGroup("CTF_data")) {
				// TODO: Load from metadata, perhaps using ; as a separator.
			} else {
				RedTeam.Spawn = new Position((short)(((map.Bounds.XMin + 5) * 32)),
				                             (short)((map.Bounds.Length / 2) * 32),
				                             (short)(3 * 32), 128, 0);
				BlueTeam.Spawn = new Position((short)(((map.Bounds.XMax - 5) * 32)),
				                              (short)((map.Bounds.Length / 2) * 32),
				                              (short)(3 * 32), 0, 0);
				
				RedTeam.FlagPos = new Vector3I(map.Bounds.XMin + 5, map.Bounds.Length / 2, 10);
				CTF.map.QueueUpdate(new BlockUpdate(Player.Console,
				                                    CTF.RedTeam.FlagPos, CTF.RedTeam.FlagBlock));
				BlueTeam.FlagPos = new Vector3I(map.Bounds.XMax - 5, map.Bounds.Length / 2, 10);
				CTF.map.QueueUpdate(new BlockUpdate(Player.Console,
				                                    CTF.BlueTeam.FlagPos, CTF.BlueTeam.FlagBlock));
			}
		}
		
		static void InitZones() {
			Zone zoneRed = new Zone();
			zoneRed.Name = RedTeam.Name;
			Vector3I redFirst = new Vector3I(map.Bounds.XMin, map.Bounds.YMin, map.Bounds.ZMin);
			Vector3I redSecond = new Vector3I(map.Bounds.Width / 2 - 2, map.Bounds.Length, map.Bounds.ZMax);
			zoneRed.Create(new BoundingBox(redFirst, redSecond), Player.Console.Info);
			map.Zones.Remove(zoneRed.Name);
			map.Zones.Add(zoneRed);
			RedTeam.SetBounds(redFirst, redSecond);

			Zone zoneBlue = new Zone();
			zoneBlue.Name = BlueTeam.Name;
			Vector3I blueFirst = new Vector3I(map.Bounds.XMax, map.Bounds.YMin, map.Bounds.ZMin);
			Vector3I blueSecond = new Vector3I((map.Bounds.Width / 2) + 2, map.Bounds.Length, map.Bounds.ZMax);
			zoneBlue.Create(new BoundingBox(blueFirst, blueSecond), Player.Console.Info);
			map.Zones.Remove(zoneBlue.Name);
			map.Zones.Add(zoneBlue);
			BlueTeam.SetBounds(blueFirst, blueSecond);
		}

		public static void Stop() {
			if (world == null) return;
			
			if (world.IsLoaded)
				world.Players.Message(
					"&SThe game has ended! The scores are: &n" + "{0}{1} &7{2}{0}:&f{3} &S- {4}{5} &7{6}{4}:&f{7}",
					RedTeam.Color, RedTeam.Name, RedTeam.RoundsWon, RedTeam.Score,
					BlueTeam.Color, BlueTeam.Name, BlueTeam.RoundsWon, BlueTeam.Score);
			
			GameRunning = false;
			Player.PlacingBlock -= PlayerPlacing;
			Player.JoinedWorld -= PlayerChangedWorld;
			Player.Moved -= PlayerMoved;
			
			Player[] cache = world.Players;
			foreach (Player p in cache) {
				p.IsPlayingCTF = false;
				p.IsHoldingFlag = false;
				
				if (p.Supports(CpeExt.MessageType))
					p.Send(Packet.Message((byte)MessageType.Status3, " "));
				if (p.Supports(CpeExt.HeldBlock))
					p.Send(Packet.MakeHoldThis(Block.Stone, false));
			}
			
			BlueTeam.ClearStats();
			RedTeam.ClearStats();
			world = null;
		}
		
		static void LavaCallback(SchedulerTask task) {
			lock (lavaLock) {
				foreach (Vector3I p in Lava)
					map.QueueUpdate(new BlockUpdate(Player.Console, p, Block.Air));
				Lava.Clear();
			}
		}
	}
}