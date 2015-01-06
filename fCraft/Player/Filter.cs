using System;
using System.IO;

namespace fCraft {
    public class Filter {

        /// <summary>
        /// Filter ID. 
        /// </summary>
        public int Id;

        /// <summary>
        /// Word being replaced. 
        /// </summary>
        public String Word;

        /// <summary>
        /// Word replacement. 
        /// </summary>
        public String Replacement;

        /// <summary>
        /// Sets a bot, as well as the bot values. Must be called before any other bot classes.
        /// </summary>
        public void addFilter(int id, String word, String replacement) {
            Id = id;
            Word = word;
            Replacement = replacement;
            Chat.Filters.Add(this);
            Chat.SaveFilter(this);
        }

        /// <summary>
        /// Completely removes the entity and data of the bot.
        /// </summary>
        public void removeFilter() {
            Chat.Filters.Remove(this);
            if (File.Exists("./Filters/" + Id + ".txt")) {
                File.Delete("./Filters/" + Id + ".txt");
            }
        }
    }
}