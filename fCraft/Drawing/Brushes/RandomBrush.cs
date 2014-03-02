﻿// Copyright 2009-2013 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> Constructs RandomBrush. </summary>
    public sealed class RandomBrushFactory : IBrushFactory {
        public static readonly RandomBrushFactory Instance = new RandomBrushFactory();

        RandomBrushFactory() {
            Aliases = new[] { "rand" };
        }

        public string Name {
            get { return "Random"; }
        }

        public string[] Aliases { get; private set; }

        const string HelpString = "Random brush: Chaotic pattern of two or more random block types. " +
                                  "If only one block name is given, leaves every other block untouched.";
        public string Help {
            get { return HelpString; }
        }


        [CanBeNull]
        public IBrush MakeBrush( Player player, CommandReader cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );

            List<Block> blocks = new List<Block>();
            List<int> blockRatios = new List<int>();
            while( cmd.HasNext ) {
                int ratio;
                Block block;
                if( !cmd.NextBlockWithParam( player, true, out block, out ratio ) ) return null;
                if( ratio < 1 || ratio > RandomBrush.MaxRatio ) {
                    player.Message( "Random brush: Invalid block ratio ({0}). Must be between 1 and {1}.",
                                    ratio, RandomBrush.MaxRatio );
                    return null;
                }
                blocks.Add( block );
                blockRatios.Add( ratio );
            }

            if( blocks.Count == 0 ) {
                return new RandomBrush();
            } else if( blocks.Count == 1 ) {
                return new RandomBrush( blocks[0], blockRatios[0] );
            } else {
                return new RandomBrush( blocks.ToArray(), blockRatios.ToArray() );
            }
        }
    }


    /// <summary> Brush that creates a random pattern,
    /// with individually adjustable probabilities for each blocktype. </summary>
    public sealed class RandomBrush : IBrushInstance, IBrush {
        public const int MaxRatio = 10000;

        public Block[] Blocks { get; private set; }
        public int[] BlockRatios { get; private set; }
        readonly Block[] actualBlocks;
        readonly int seed = new Random().Next();

        public RandomBrush() {
            Blocks = new Block[0];
            BlockRatios = new int[0];
        }


        public RandomBrush( Block oneBlock, int ratio ) {
            Blocks = new[] { oneBlock, Block.None };
            BlockRatios = new[] { ratio, 1 };
            actualBlocks = new[] { oneBlock, Block.None };
        }


        public RandomBrush( Block[] blocks, int[] ratios ) {
            Blocks = blocks;
            BlockRatios = ratios;
            actualBlocks = new Block[BlockRatios.Sum()];
            int c = 0;
            for( int i = 0; i < Blocks.Length; i++ ) {
                for( int j = 0; j < BlockRatios[i]; j++ ) {
                    actualBlocks[c] = Blocks[i];
                    c++;
                }
            }
        }


        public RandomBrush( [NotNull] RandomBrush other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            Blocks = other.Blocks;
            BlockRatios = other.BlockRatios;
            actualBlocks = other.actualBlocks;
        }


        #region IBrush members

        public IBrushFactory Factory {
            get { return RandomBrushFactory.Instance; }
        }


        public string Description {
            get {
                if( Blocks.Length == 0 ) {
                    return Factory.Name;
                } else if( Blocks.Length == 1 || (Blocks.Length == 2 && Blocks[1] == Block.None) ) {
                    return String.Format( "{0}({1})", Factory.Name, Blocks[0] );
                } else {
                    StringBuilder sb = new StringBuilder();
                    sb.Append( Factory.Name );
                    sb.Append( '(' );
                    for( int i = 0; i < Blocks.Length; i++ ) {
                        if( i != 0 ) sb.Append( ',' ).Append( ' ' );
                        sb.Append( Blocks[i] );
                        if( BlockRatios[i] > 1 ) {
                            sb.Append( '/' );
                            sb.Digits( BlockRatios[i] );
                        }
                    }
                    sb.Append( ')' );
                    return sb.ToString();
                }
            }
        }


        [CanBeNull]
        public IBrushInstance MakeInstance( Player player, CommandReader cmd, DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( state == null ) throw new ArgumentNullException( "state" );

            List<Block> blocks = new List<Block>();
            List<int> blockRatios = new List<int>();
            while( cmd.HasNext ) {
                int ratio;
                Block block;
                if( !cmd.NextBlockWithParam( player, true, out block, out ratio ) ) return null;
                if( ratio < 1 || ratio > MaxRatio ) {
                    player.Message( "RandomBrush: Invalid block ratio ({0}). Must be between 1 and {1}.",
                                    ratio, MaxRatio );
                    return null;
                }
                blocks.Add( block );
                blockRatios.Add( ratio );
            }

            if( blocks.Count == 0 ) {
                if( Blocks.Length == 0 ) {
                    player.Message( "{0} brush: Please specify at least one block.", Factory.Name );
                    return null;
                } else {
                    return new RandomBrush( this );
                }
            } else if( blocks.Count == 1 ) {
                return new RandomBrush( blocks[0], blockRatios[0] );
            } else {
                return new RandomBrush( blocks.ToArray(), blockRatios.ToArray() );
            }
        }

        #endregion


        #region IBrushInstance members

        public IBrush Brush {
            get { return this; }
        }


        public int AlternateBlocks {
            get { return 1; }
        }


        public string InstanceDescription {
            get { return Description; }
        }


        public bool Begin( Player player, DrawOperation op ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( op == null ) throw new ArgumentNullException( "op" );
            if( Blocks == null || Blocks.Length == 0 ) {
                throw new InvalidOperationException( "No blocks given." );
            }
            return true;
        }


        public Block NextBlock( DrawOperation op ) {
            if( op == null ) throw new ArgumentNullException( "op" );
            int n = seed ^ (op.Coords.X + 1290 * op.Coords.Y + 1664510 * op.Coords.Z);
            n = (n << 13) ^ n;
            n = (n * (n * n * 15731 + 789221) + 1376312589) & 0x7FFFFFFF;
            double derp = (n / (double)0x7FFFFFFF) * actualBlocks.Length;
            return actualBlocks[(int)Math.Floor( derp )];
        }


        public void End() { }

        #endregion
    }
}