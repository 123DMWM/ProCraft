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
        /// Adds/Creats the filter
        /// </summary>
        public void addFilter(int id, String word, String replacement) {
            Id = id;
            Word = word;
            Replacement = replacement;
            Chat.Filters.Add(this);
            Chat.SaveFilter(this);
        }

        /// <summary>
        /// Completely removes the filter
        /// </summary>
        public void removeFilter() {
            Chat.Filters.Remove(this);
            if (File.Exists("./Filters/" + Id + ".txt")) {
                File.Delete("./Filters/" + Id + ".txt");
            }
        }
    }
}