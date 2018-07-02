// ProCraft Copyright 2014-2018 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ServiceStack.Text;

namespace fCraft {

    public sealed class ChatFilter {

        // ChatFilter
        public int Id { get; set; }
        public string Word { get; set; }
        public string Replacement { get; set; }

        public static List<ChatFilter> Filters = new List<ChatFilter>();
        

        public static ChatFilter Find(string sid) {
            int id;
            int.TryParse(sid, out id);
            foreach (ChatFilter filter in Filters) {
                if (filter.Id == id) return filter;
            }
            return null;
        }

        public static bool Exists(string word) {
            foreach (ChatFilter filter in Filters) {
                if (filter.Word.CaselessEquals(word)) return true;
            }
            return false;
        }

        public static void CreateFilter(int id, string word, string replacement) {
            ChatFilter filter = new ChatFilter();
            filter.Id = id;
            filter.Word = word;
            filter.Replacement = replacement;
            Filters.Add(filter);
            SaveAll(false);
        }

        public static void RemoveFilter(string id) {
            ChatFilter filter = Find(id);
            if (filter != null) {
                Filters.Remove(filter);
                SaveAll(false);
            }
        }
        

        public static void ReloadAll() {
            Filters.Clear();
            LoadAll();
        }

        public static void SaveAll(bool verbose) {
            try {
                Stopwatch sw = Stopwatch.StartNew();
                using (Stream s = File.Create(Paths.FiltersFileName)) {
                    JsonSerializer.SerializeToStream(Filters.ToArray(), s);
                }
                sw.Stop();
                if (verbose) {
                    Logger.Log(LogType.Debug, "ChatFilter.Save: Saved filters in {0}ms", sw.ElapsedMilliseconds);
                }
            } catch (Exception ex) {
                Logger.Log(LogType.Error, "ChatFilter.Save: " + ex);
            }
        }

        public static void LoadAll() {
            if (Directory.Exists("Filters")) {
                OldLoad();
                Directory.Delete("Filters", true);
                SaveAll(false);
            }
            if (!File.Exists(Paths.FiltersFileName)) return;

            try {
                using (Stream s = File.OpenRead(Paths.FiltersFileName)) {
                    Filters = (List<ChatFilter>)
                        JsonSerializer.DeserializeFromStream(typeof(List<ChatFilter>), s);
                }
                int count = 0;
                for (int i = 0; i < Filters.Count; i++) {
                    if (Filters[i] == null)
                        continue;
                    // fixup for servicestack not writing out null entries
                    if (Filters[i].Word == null) {
                        Filters[i] = null; continue;
                    }
                    count++;
                }
                Logger.Log(LogType.SystemActivity, "ChatFilter.Load: Loaded " + count + " filters");
                SaveAll(true);
            } catch (Exception ex) {
                Filters = null;
                Logger.Log(LogType.Error, "ChatFilter.Load: " + ex);
            }
        }
        
        public static void OldLoad() {
            string[] files = Directory.GetFiles("./Filters");
            foreach (string filename in files) {
                if (Path.GetExtension(filename) != ".txt") continue;
                string idString = Path.GetFileNameWithoutExtension(filename);
                int id;
                if (!int.TryParse(idString, out id)) continue;
                
                string[] data = File.ReadAllLines(filename);
                ChatFilter filter = new ChatFilter();
                filter.Id = id;
                filter.Word = data[0];
                filter.Replacement = data[1];
                Filters.Add(filter);
            }
        }
    }
}