using System;
using System.IO;

namespace fCraft {
    public class Report {

        /// <summary>
        /// Report ID. 
        /// </summary>
        public int Id;

        /// <summary>
        /// Player who sent in the report. 
        /// </summary>
        public String Sender;

        /// <summary>
        /// The date/time of sending the report. 
        /// </summary>
        public DateTime Datesent;

        /// <summary>
        /// The report message. 
        /// </summary>
        public String Message;

        /// <summary>
        /// Adds/Creates the report
        /// </summary>
        public void addReport(int id, String sender, DateTime datesent, String message) {
            Id = id;
            Sender = sender;
            Datesent = datesent;
            Message = message;
            Chat.Reports.Add(this);
            Chat.SaveReport(this);
        }

        /// <summary>
        /// Completely removes the report and data of it.
        /// </summary>
        public void removeFilter() {
            Chat.Reports.Remove(this);
            if (File.Exists("./Reports/" + Id + "-" + Sender + ".txt")) {
                File.Delete("./Reports/" + Id + "-" + Sender + ".txt");
            }
        }
    }
}