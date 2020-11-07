using System;
using System.Collections.Generic;
using System.Text;

namespace OctoPrintLib.DataClasses
{
    public class JobBase
    {
        public int? estimatedPrintTime { get; set; }
        public int? lastPrintTime { get; set; }
        public Filament filament { get; set; }

    }

    public class JobInCurrentMessage:JobBase
    {
        public FileInCurrentMessage file { get; set; }
        public string user { get; set; }
    }

    public class JobInHistoryMessage:JobBase
    {
        public FileInHistoryMessage file { get; set; }
    }
}
