// ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;

namespace fCraft.Games {
    
    public class CtfTeam {
        
        /// <summary> List of all players that are currently part of the team. </summary>
        public List<Player> Players = new List<Player>();
        
        /// <summary> Total number of players in this team. </summary>
        public int Count {
            get { return Players.Count; }
        }
        
        /// <summary> Returns whether this team currently has the given player as a member. </summary>
        public bool Has(Player p) {
            return Players.Contains(p);
        }
        
        /// <summary> Total score of this team for the current round. </summary>
        public int Score;
        
        /// <summary> Total number of rounds this team has won for the current game. </summary>
        public int RoundsWon;
        
        /// <summary> Total score of this team across all rounds </summary>
        public int TotalScore {
            get { return RoundsWon * 5 + Score; }
        }
        
        /// <summary> Integer coordinates of the position of this team's flag. </summary>
        public Vector3I FlagPos;
        
        /// <summary> The type of block this team's flag is. </summary>
        public Block FlagBlock;
        
        /// <summary> Position that players of this team should teleport to upon death or a new round. </summary>
        public Position Spawn;
        
        /// <summary> Whether a player of this team currently has the flag of the opposing team. </summary>
        public bool HasFlag;
        
        /// <summary> Name of the color of this team. </summary>
        public string Name;
        
        /// <summary> Unique color code for this team. </summary>
        public string Color;
        
        /// <summary> Color code + Name of this team. </summary>
        public string ClassyName;
        
        /// <summary> Area that belongs to this team for tagging purposes. </summary>
        public BoundingBox TaggingBounds;
        
        /// <summary> Sky and fog color used when this team is winning the current round. </summary>
        public string EnvColor;
        
        public CtfTeam(string color, string name, string envColor, Block flagBlock) {
            Color = color;
            Name = name;
            ClassyName = Color + Name;
            FlagBlock = flagBlock;
            EnvColor = envColor;
        }
        
        public void ClearStats() {
            Players.Clear();
            Score = 0;
            RoundsWon = 0;
        }
        
        public void SetBounds(Vector3I p1, Vector3I p2) {
            TaggingBounds = new BoundingBox(p1.X * 32, p1.Y * 32, p1.Z * 32,
                                     p2.X * 32, p2.Y * 32, p2.Z * 32);
        }
    }
}