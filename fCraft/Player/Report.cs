// ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.IO;

namespace fCraft {
    public class Report {

        /// <summary> Report ID. </summary>
        public int Id;

        /// <summary> Player who sent in the report. </summary>
        public string Sender;

        /// <summary> The date/time of sending the report.  </summary>
        public DateTime Datesent;

        /// <summary> The report message. </summary>
        public string Message;
        
        public static List<Report> Reports = new List<Report>();
        

        /// <summary> Adds/Creates the report </summary>
        public void AddReport(int id, string sender, DateTime datesent, string message) {
            Id = id;
            Sender = sender;
            Datesent = datesent;
            Message = message;
            Reports.Add(this);
            SaveReport(this);
        }

        /// <summary> Completely removes the report and data of it. </summary>
        public void RemoveReport() {
            Reports.Remove(this);
            if (File.Exists("./Reports/" + Id + "-" + Sender + ".txt")) {
                File.Delete("./Reports/" + Id + "-" + Sender + ".txt");
            }
        }
        
        
        /// <summary> Saves the report to be read by the owner with /reports</summary>
        /// <param name="report">Report being saved</param>
        public static void SaveReport(Report report) {
            try {
                string[] data = { report.Sender, report.Datesent.ToBinary().ToString(), report.Message };
                if (!Directory.Exists("./Reports")) {
                    Directory.CreateDirectory("./Reports");
                }
                File.WriteAllLines("./Reports/" + report.Id + "-" + report.Sender + ".txt", data);
            } catch (Exception ex) {
                Logger.Log(LogType.Error, "Report.SaveReport: " + ex);
            }
        }
        
        public static void LoadAll() {
            try {
                if (!Directory.Exists("./Reports")) return;
                string[] files = Directory.GetFiles("./Reports");
                int count = 0;
                
                foreach (string filename in files) {
                    if (Path.GetExtension(filename) != ".txt") continue;
                    string idString = Path.GetFileNameWithoutExtension(filename).Split('-')[0];
                    int id;
                    if (!int.TryParse(idString, out id)) continue;
                    
                    string[] data = File.ReadAllLines(filename);
                    Report report = new Report();
                    report.Sender = data[0];
                    report.Message = data[2];
                    
                    long dateSentBinary;
                    if (long.TryParse(data[1], out dateSentBinary)) {
                        report.Datesent = DateTime.FromBinary(dateSentBinary);
                    }
                    
                    Reports.Add(report);
                    count++;
                }
                
                Logger.Log(LogType.SystemActivity, "Report.LoadAll: Loaded " + count + " reports");
            } catch (Exception ex) {
                Logger.Log(LogType.Error, "Report.LoadAll: " + ex);
            }
        }
    }
}