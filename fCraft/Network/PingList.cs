// ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;

namespace fCraft {

    public sealed class PingList {
        
        public struct PingEntry {
            public DateTime TimeSent, TimeReceived;
            public ushort Data;
        }
        public PingEntry[] Entries = new PingEntry[10];
        
        
        public ushort NextTwoWayPingData() {
            // Find free ping slot
            for (int i = 0; i < Entries.Length; i++) {
                if (Entries[i].TimeSent.Ticks != 0) continue;
                
                ushort prev = i > 0 ? Entries[i - 1].Data : (ushort)0;
                return SetTwoWayPing(i, prev);
            }
            
            // Remove oldest ping slot
            for (int i = 0; i < Entries.Length - 1; i++) {
                Entries[i] = Entries[i + 1];
            }
            int j = Entries.Length - 1;
            return SetTwoWayPing(j, Entries[j].Data);
        }
        
        ushort SetTwoWayPing(int i, ushort prev) {
             Entries[i].Data = (ushort)(prev + 1);
             Entries[i].TimeSent = DateTime.UtcNow;
             return (ushort)(prev + 1);
        }
        
        public void Update(ushort data) {
            for (int i = 0; i < Entries.Length; i++ ) {
                if (Entries[i].Data != data) continue;
                Entries[i].TimeReceived = DateTime.UtcNow;
                return;
            }
        }
        
        
        /// <summary> Gets average ping in milliseconds, or 0 if no ping measures. </summary>
        public double AveragePingMilliseconds() {
            double totalMs = 0;
            int measures = 0;
            
            foreach (PingEntry ping in Entries) {
                if (ping.TimeSent.Ticks == 0 || ping.TimeReceived.Ticks == 0) continue;
                
                totalMs += (ping.TimeReceived - ping.TimeSent).TotalMilliseconds;
                measures++;
            }
            return measures == 0 ? 0 : (totalMs / measures);
        }
        
        
        /// <summary> Gets worst ping in milliseconds, or 0 if no ping measures. </summary>
        public double WorstPingMilliseconds() {
            double totalMs = 0;
            
            foreach (PingEntry ping in Entries) {
                if (ping.TimeSent.Ticks == 0 || ping.TimeReceived.Ticks == 0) continue;
                
                double ms = (ping.TimeReceived - ping.TimeSent).TotalMilliseconds;
                totalMs = Math.Max(totalMs, ms);
            }
            return totalMs;
        }
        
        public string Format() {
            return String.Format("Worst ping {0}ms, average {1}ms",
                                 WorstPingMilliseconds().ToString("N0"),
                                 AveragePingMilliseconds().ToString("N0"));
        }
    }
}
