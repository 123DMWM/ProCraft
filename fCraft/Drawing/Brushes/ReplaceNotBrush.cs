﻿// Copyright 2009-2013 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> Constructs ReplaceNotBrush. </summary>
    public sealed class ReplaceNotBrushFactory : IBrushFactory {
        /// <summary> Singleton instance of the ReplaceNotBrushFactory. </summary>
        public static readonly ReplaceNotBrushFactory Instance = new ReplaceNotBrushFactory();

        ReplaceNotBrushFactory() {
            Aliases = new[] { "rn" };
        }

        public string Name {
            get { return "ReplaceNot"; }
        }

        public string[] Aliases { get; private set; }

        const string HelpString = "ReplaceNot brush: Replaces all blocks except the given type(s) with another type. " +
                                  "Usage similar to &H/ReplaceNot&S command.";
        public string Help {
            get { return HelpString; }
        }


        [CanBeNull]
        public IBrush MakeBrush( Player player, CommandReader cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );

            Stack<Block> blocks = new Stack<Block>();
            while( cmd.HasNext ) {
                Block block;
                if( !cmd.NextBlock( player, false, out block ) ) return null;
                blocks.Push( block );
            }
            if( blocks.Count == 0 ) {
                return new ReplaceNotBrush();
            } else if( blocks.Count == 1 ) {
                return new ReplaceNotBrush( blocks.ToArray(), Block.None );
            } else {
                Block replacement = blocks.Pop();
                return new ReplaceNotBrush( blocks.ToArray(), replacement );
            }
        }
    }


    /// <summary> Brush that replaces all blocks EXCEPT those of given type(s) with a replacement blocktype. </summary>
    public sealed class ReplaceNotBrush : IBrushInstance, IBrush {
        public Block[] Blocks { get; private set; }
        public Block Replacement { get; private set; }

        public ReplaceNotBrush() { }

        public ReplaceNotBrush( Block[] blocks, Block replacement ) {
            Blocks = blocks;
            Replacement = replacement;
        }


        public ReplaceNotBrush( [NotNull] ReplaceNotBrush other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            Blocks = other.Blocks;
            Replacement = other.Replacement;
        }


        #region IBrush members

        public IBrushFactory Factory {
            get { return ReplaceNotBrushFactory.Instance; }
        }


        public string Description {
            get {
                if( Blocks == null ) {
                    return Factory.Name;
                } else if( Replacement == Block.None ) {
                    return String.Format( "{0}({1} -> ?)",
                                          Factory.Name,
                                          Blocks.JoinToString() );
                } else {
                    return String.Format( "{0}({1} -> {2})",
                                          Factory.Name,
                                          Blocks.JoinToString(),
                                          Replacement );
                }
            }
        }


        [CanBeNull]
        public IBrushInstance MakeInstance( Player player, CommandReader cmd, DrawOperation op ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( op == null ) throw new ArgumentNullException( "op" );

            Stack<Block> blocks = new Stack<Block>();
            while( cmd.HasNext ) {
                Block block;
                if( !cmd.NextBlock( player, false, out block ) ) return null;
                blocks.Push( block );
            }

            if( blocks.Count == 0 && Blocks == null ) {
                player.Message( "ReplaceNot brush requires at least 1 block." );
                return null;
            }

            if( blocks.Count > 0 ) {
                if( blocks.Count > 1 ) Replacement = blocks.Pop();
                Blocks = blocks.ToArray();
            }

            return new ReplaceNotBrush( this );
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
            if( Replacement == Block.None ) {
                if( player.LastUsedBlockType == Block.None ) {
                    player.Message( "Cannot deduce desired replacement block. Click a block or type out the block name." );
                    return false;
                } else {
                    Replacement = player.GetBind( player.LastUsedBlockType );
                }
            }
            op.Context |= BlockChangeContext.Replaced;
            return true;
        }


        public Block NextBlock( DrawOperation op ) {
            if( op == null ) throw new ArgumentNullException( "op" );
            Block block = op.Map.GetBlock( op.Coords );
            for( int i = 0; i < Blocks.Length; i++ ) {
                if( block == Blocks[i] ) {
                    return Block.None;
                }
            }
            return Replacement;
        }


        public void End() { }

        #endregion
    }
}