using System.Collections.Generic;

namespace OctoPrintLib
{
    public class HistoricTemperatureDataPoint
    {
        public int? time { get; set; }
        public TemperatureData tool0 { get; set; }
        public TemperatureData bed { get; set; }
        public TemperatureData chamber { get; set; }
    }

    public class TemperatureData
    {
        public float? actual { get; set; }
        public float? target { get; set; }
        public float? offset { get; set; }
    }

    
}