using System;
using System.Collections.Generic;
using System.Text;

namespace OctoPrintLib
{
    public class HistoryMessage
    {
        public History history { get; set; }

    }

    public class History
    {
        public State state { get; set; }
        public FileInHistoryMessage job { get; set; }
        public float? currentZ { get; set; }
        public Progress progress { get; set; }
        public Offsets offsets { get; set; }
        public List<HistoricTemperatureDataPoint> temps { get; set; }
        public List<string> logs { get; set; }
        public List<string> messages { get; set; }
        public double? serverTime { get; set; }
    }
    
    

    

    
}