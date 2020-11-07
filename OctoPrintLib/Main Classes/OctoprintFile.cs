using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace OctoPrintLib.File
{
    public class OctoprintFile
    {
        public int date { get; set; }
        public string display { get; set; }
        public GcodeAnalysis gcodeAnalysis { get; set; }
        public string hash { get; set; }
        public string name { get; set; }
        public string origin { get; set; }
        public string path { get; set; }
        public Refs refs { get; set; }
        public int size { get; set; }
        public string type { get; set; }
        public List<string> typePath { get; set; }
        public List<OctoprintFile> children { get; set; }

        public OctoprintFile()
        {
                
        }
    }

    public class Dimensions
    {
        public double depth { get; set; }
        public double height { get; set; }
        public double width { get; set; }
    }

    public class Tool0
    {
        public double length { get; set; }
        public double volume { get; set; }
    }

    public class Filament
    {
        public Tool0 tool0 { get; set; }
    }

    public class PrintingArea
    {
        public double maxX { get; set; }
        public double maxY { get; set; }
        public double maxZ { get; set; }
        public double minX { get; set; }
        public double minY { get; set; }
        public double minZ { get; set; }
    }

    public class GcodeAnalysis
    {
        public Dimensions dimensions { get; set; }
        public double estimatedPrintTime { get; set; }
        public Filament filament { get; set; }
        public PrintingArea printingArea { get; set; }
    }

    public class Refs
    {
        public string download { get; set; }
        public string resource { get; set; }
    }
}