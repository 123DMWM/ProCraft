// ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
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

        public static void Add(int id, string word, string replacement) {
            ChatFilter filter = new ChatFilter();
            filter.Id = id;
            filter.Word = word;
            filter.Replacement = replacement;
            Filters.Add(filter);
            Save(false);
        }

        public static void Remove(string id) {
            ChatFilter filter = Find(id);
            if (filter != null) {
                Filters.Remove(filter);
                Save(false);
            }
        }
        

        public static string Apply(string msg) {
            foreach (ChatFilter filter in Filters) {
                if (msg.CaselessContains(filter.Word)) {
                    msg = msg.ReplaceString(filter.Word, filter.Replacement, StringComparison.InvariantCultureIgnoreCase);
                }
            }
            return msg;
        }
        
        public static void Reload() {
            Filters.Clear();
            Load();
        }

        public static void Save(bool verbose) {
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

        public static void Load() {
            if (!File.Exists(Paths.FiltersFileName)) return;

            try {
                using (Stream s = File.OpenRead(Paths.FiltersFileName)) {
                    Filters = (List<ChatFilter>)
                        JsonSerializer.DeserializeFromStream(typeof(List<ChatFilter>), s);
                }
                int count = 0;
                for (int i = 0; i < Filters.Count; i++) {
                    if (Filters[i] == null) continue;
                    // fixup for servicestack not writing out null entries
                    if (Filters[i].Word == null) {
                        Filters[i] = null; continue;
                    }
                    count++;
                }
                Logger.Log(LogType.SystemActivity, "ChatFilter.Load: Loaded " + count + " filters");
                Save(true);
            } catch (Exception ex) {
                Filters = null;
                Logger.Log(LogType.Error, "ChatFilter.Load: " + ex);
            }
        }
    }
}