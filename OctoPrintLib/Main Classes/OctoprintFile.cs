using System;
using Newtonsoft.Json.Linq;

namespace OctoPrintLib
{
    public class OctoprintFile
    {
        public OctoprintFile(JObject filedata)
        {
            Name = filedata.Value<String>("name") ?? "";
            Path = filedata.Value<String>("path") ?? "";
            Type = filedata.Value<String>("type") ?? "file";
            Hash = filedata.Value<String>("hash") ?? "";
            Size = filedata.Value<int?>("size") ?? -1;
            Date = filedata.Value<int?>("date") ?? -1;
            Origin = filedata.Value<String>("origin") ?? "";
        }

        public OctoprintFile()
        {
                
        }
        
        public string Name { get; set; }
        public string Path { get; set; }
        public string Type { get; set; }
        public string[] TypePath { get; set; }
        public string Hash { get; set; }
        public int Size { get; set; }
        public int Date { get; set; }
        public string Origin { get; set; }
        public string Refs_resource { get; set; }
        public string Refs_download { get; set; }
        public int GcodeAnalysis_estimatedPrintTime { get; set; }
        public int GcodeAnalysis_filament_length { get; set; }
        public int GcodeAnalysis_filament_volume { get; set; }
        public int Print_failure { get; set; }
        public int Print_success { get; set; }
        public int Print_last_date { get; set; }
        public bool Print_last_success { get; set; }
        public override string ToString()
        {
            string returnvalue = "";
            returnvalue += Name + ", path: " +Origin+"/"+ Path + " ("+ Type + ") :\n";
            return returnvalue;
        }
    }
}