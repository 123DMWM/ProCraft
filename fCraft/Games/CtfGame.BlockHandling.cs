// ProCraft Copyright 2014-2018 Joseph Beauvais <123DMWM@gmail.com>
using System;
using fCraft;
using fCraft.Events;

namespace fCraft.Games {
    
    public static partial class CTF {

        static void PlayerPlacing(object sender, PlayerPlacingBlockEventArgs e) {
            if (e.Player.World != world || e.Result != CanPlaceResult.Allowed)
                return;
            
            if (e.NewBlock == Opposition(e.Player.Team).FlagBlock) {
                e.Player.Message("You cannot place {0}&S blocks, you are on the {1}&S Team.",
                                 Opposition(e.Player.Team).ClassyName, e.Player.Team.ClassyName);
                e.Result = CanPlaceResult.CTFDenied;
            } else if (e.NewBlock == Block.Lava || e.NewBlock == Block.Water) {
                if (e.Context == BlockChangeContext.Manual)
                    e.Player.Message("You are not allowed to place water or lava blocks in CTF.");
                e.Result = CanPlaceResult.CTFDenied;
            } else if (e.Context != BlockChangeContext.Manual) {
                e.Result = CanPlaceResult.CTFDenied;
            } else {
                if ((e.Coords == RedTeam.FlagPos || e.Coords == BlueTeam.FlagPos) &&
                    (e.NewBlock == RedTeam.FlagBlock || e.NewBlock == BlueTeam.FlagBlock || e.NewBlock == Block.Air))
                {
                    if (e.Coords == BlueTeam.FlagPos && e.Player.Team == RedTeam)
                        ClickOpposingFlag(e, BlueTeam);
                    else if (e.Coords == RedTeam.FlagPos && e.Player.Team == BlueTeam)
                        ClickOpposingFlag(e, RedTeam);
                    else if (e.Coords == RedTeam.FlagPos && e.Player.Team == RedTeam)
                        ClickOwnFlag(e, BlueTeam);
                    else if (e.Coords == BlueTeam.FlagPos && e.Player.Team == BlueTeam)
                        ClickOwnFlag(e, RedTeam);
                } else if (e.Coords == RedTeam.FlagPos || e.Coords == BlueTeam.FlagPos) {
                    e.Result = CanPlaceResult.CTFDenied;
                } else if (e.OldBlock == RedTeam.FlagBlock || e.OldBlock == BlueTeam.FlagBlock) {
                    DoClickEffect(99, 75, e, Block.CobbleSlab, Block.Stone);
                } else if (e.OldBlock == Block.Stone) {
                    DoClickEffect(99, 75, e, Block.Gravel, Block.Cobblestone);
                } else if (e.OldBlock == Block.Cobblestone) {
                    DoClickEffect(99, 75, e, Block.Air, Block.Gravel);
                } else if (e.OldBlock == Block.Gravel) {
                    DoClickEffect(75, 1000, e, Block.Air, Block.None);
                } else if (e.OldBlock == Block.Grass) {
                    DoClickEffect(25, 1000, e, Block.Dirt, Block.None);
                } else if (e.OldBlock == Block.Water) {
                    e.Result = CanPlaceResult.CTFDenied;
                } else if (e.NewBlock == Block.TNT) {
                    HandleTNT(e.Player, e.Coords);
                    e.Result = CanPlaceResult.CTFDenied;
                    world.Map.QueueUpdate(new BlockUpdate(Player.Console, e.Coords, Block.Air));
                    world.Map.ProcessUpdates();
                } else {
                    e.Result = CanPlaceResult.Allowed;
                }
            }

            if (world != null)
                world.Map.ProcessUpdates();
        }
        
        static void ClickOpposingFlag(PlayerPlacingBlockEventArgs e, CtfTeam opposition) {
            if (opposition.HasFlag) {
                e.Player.Message("You can't capture the opposing team's flag, " +
                                 "if you're not in posession of your own flag.");
                e.Result = CanPlaceResult.CTFDenied;
            } else {
                e.Result = CanPlaceResult.Allowed;
                e.Player.Team.HasFlag = true;
                e.Player.IsHoldingFlag = true;
                world.Players.Message("{0} &Sgot the {1}&S flag.",
                                      e.Player.ClassyName, opposition.ClassyName );
            }
        }
        
        static void ClickOwnFlag(PlayerPlacingBlockEventArgs e, CtfTeam opposition) {
            if (!e.Player.IsHoldingFlag) {
                e.Player.Message("You don't have the {0}&S flag.", opposition.ClassyName);
                e.Result = CanPlaceResult.CTFDenied;
            } else {
                world.Map.QueueUpdate(new BlockUpdate(Player.Console, opposition.FlagPos, opposition.FlagBlock));
                CtfTeam team = e.Player.Team;
                team.Score++;
                team.HasFlag = false;
                e.Player.IsHoldingFlag = false;
                
                world.Players.Message("{0} &Sscored a point for {1}&S team.",
                                      e.Player.ClassyName, team.ClassyName);
                e.Result = CanPlaceResult.CTFDenied;
                Check();
            }
        }
        
        static void HandleTNT(Player player, Vector3I coords) {
            for (int i = 0; i < 20; i++) {
                int rndX = rnd.Next(-2, 3), rndY = rnd.Next(-2, 3), rndZ = rnd.Next(-2, 3);
                Vector3I pos = new Vector3I(coords.X + rndX, coords.Y + rndY, coords.Z + rndZ);
                if (pos == RedTeam.FlagPos) continue;
                if (pos == BlueTeam.FlagPos) continue;
                Block block = world.map.GetBlock(pos);
                if (block == Block.Water || block == Block.None)
                    continue;
                
                if (i < 3) {
                    if (block == RedTeam.FlagBlock || block == BlueTeam.FlagBlock)
                        DoTntLavaEffect(99, 75, pos, Block.Stone);
                    else if (block == Block.Stone)
                        DoTntLavaEffect(99, 75, pos, Block.Cobblestone);
                    else if (block == Block.Cobblestone)
                        DoTntLavaEffect(99, 75, pos, Block.Gravel);
                    else if (block == Block.Gravel)
                        DoTntLavaEffect(99, 75, pos, Block.Air);
                    else if (block == Block.Grass)
                        DoTntLavaEffect(75, 25, pos, Block.Dirt);
                    else if (!(block == Block.Water || block == Block.Lava)) {
                        world.Map.QueueUpdate(new BlockUpdate(Player.Console, pos, Block.Lava));
                        lock (lavaLock)
                            Lava.Add(pos);
                    }
                } else {
                    if (block == RedTeam.FlagBlock || block == BlueTeam.FlagBlock)
                        DoTntEffect(99, pos, Block.Stone);
                    else if (block == Block.Stone)
                        DoTntEffect(99, pos, Block.Cobblestone);
                    else if (block == Block.Cobblestone)
                        DoTntEffect(99, pos, Block.Gravel);
                    else if (block == Block.Gravel)
                        DoTntEffect(99, pos, Block.Air);
                    else if (block == Block.Grass)
                        DoTntEffect(33, pos, Block.Dirt);
                }
            }
            KillExplosion(player, coords);
            world.Map.QueueUpdate(new BlockUpdate(Player.Console, coords, Block.Air));
            world.Map.ProcessUpdates();
        }
        
        static void DoTntLavaEffect(int prob1, int prob2, Vector3I pos, Block target) {
            int effect = rnd.Next(100);
            if (effect >= prob1) {
                map.QueueUpdate(new BlockUpdate(Player.Console, pos, Block.Lava));
                lock (lavaLock)
                    Lava.Add(pos);
            } else if (effect >= prob2) {
                map.QueueUpdate(new BlockUpdate(Player.Console, pos, target));
            }
        }
        
        static void DoTntEffect(int prob, Vector3I pos, Block target) {
            if (rnd.Next(100) >= prob)
                map.QueueUpdate(new BlockUpdate(Player.Console, pos, target));
        }
        
        static void DoClickEffect(int prob1, int prob2, PlayerPlacingBlockEventArgs e,
                                  Block target1, Block target2) {
            int effect = rnd.Next(100);
            if (effect >= prob1)
                map.QueueUpdate(new BlockUpdate(Player.Console, e.Coords, target1));
            else if (effect >= prob2)
                map.QueueUpdate(new BlockUpdate(Player.Console, e.Coords, target2));
            e.Result = CanPlaceResult.CTFDenied;
        }
    }
}