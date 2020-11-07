using System;
using System.Collections.Generic;
using System.Text;

namespace OctoPrintLib
{
    class CurrentResponse
    {
        public Current current { get; set; }
    }
    public class Current
    {
        public State state { get; set; }
        public Job job { get; set; }
        public float? currentZ { get; set; }
        public Progress progress { get; set; }
        public Offsets offsets { get; set; }
        public double serverTime { get; set; }
        public List<Temp> temps { get; set; }
        public List<string> logs { get; set; }
        public List<string> messages { get; set; }
        public List<object> busyFiles { get; set; }
    }
   

    public class Job
    {
        public WSFile file { get; set; }
        public object estimatedPrintTime { get; set; }
        public string lastPrintTime { get; set; }
        public Filament filament { get; set; }
        public string user { get; set; }
    }
}

   
