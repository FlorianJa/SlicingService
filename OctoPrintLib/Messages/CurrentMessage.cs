using System;
using System.Collections.Generic;
using System.Text;

namespace OctoPrintLib
{
    class CurrentMessage
    {
        public Current current { get; set; }
    }
    public class Current
    {
        public State state { get; set; }
        public FileInCurrentMessage job { get; set; }
        public float? currentZ { get; set; }
        public Progress progress { get; set; }
        public Offsets offsets { get; set; }
        public double serverTime { get; set; }
        public List<HistoricTemperatureDataPoint> temps { get; set; }
        public List<string> logs { get; set; }
        public List<string> messages { get; set; }
        public List<object> busyFiles { get; set; }
    }
   

    
}

   
