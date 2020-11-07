using System;
using System.Collections.Generic;
using System.Text;

namespace OctoPrintLib.History
{
    public class HistoryResponse
    {
        public History history { get; set; }

    }

    public class History
    {
        public State state { get; set; }
        public Job job { get; set; }
        public double? currentZ { get; set; }
        public Progress progress { get; set; }
        public Offsets offsets { get; set; }
        public List<Temp> temps { get; set; }
        public List<string> logs { get; set; }
        public List<string> messages { get; set; }
        public double serverTime { get; set; }
    }
    
    

    public class Job
    {
        public WSFile file { get; set; }
        public int estimatedPrintTime { get; set; }
        public long lastPrintTime { get; set; }
        public Filament filament { get; set; }
        public string user { get; set; }
    }

    
}