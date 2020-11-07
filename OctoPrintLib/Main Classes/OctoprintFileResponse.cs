using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;


namespace OctoPrintLib.File
{
    public class OctoprintFileResponse
    {
        public List<OctoprintFile> files { get; set; }
        public long free { get; set; }
        public long total { get; set; }


        

       

        //public string Name { get; set; }
        //public string Path { get; set; }
        //public string Type { get; set; }
        ////public string[] TypePath { get; set; }
        //public List<OctoprintFile> octoprintFiles;
        //public List<OctoprintFolder> octoprintFolders;

        //public OctoprintFolder(JObject data, OctoprintFileOperation t)
        //{
        //    octoprintFolders = new List<OctoprintFolder>();
        //    octoprintFiles = new List<OctoprintFile>();
        //    foreach (JObject filedata in data["files"])
        //    {
        //        if ((string)filedata["type"] == "folder")
        //        {
        //            OctoprintFolder folder = t.GetFiles((string)filedata["path"]);
        //            octoprintFolders.Add(folder);
        //        }
        //        else
        //        {
        //            OctoprintFile file = new OctoprintFile(filedata);
        //            JToken refs = filedata.Value<JToken>("refs");
        //            if (refs != null)
        //            {
        //                file.Refs_resource = refs.Value<String>("resource") ?? "";
        //                file.Refs_download = refs.Value<String>("download") ?? "";
        //            }
        //            JToken gcodeanalysis = filedata.Value<JToken>("gcodeAnalysis");
        //            if (gcodeanalysis != null)
        //            {
        //                file.GcodeAnalysis_estimatedPrintTime = gcodeanalysis.Value<int?>("estimatedPrintTime") ?? 0;
        //                JToken filament = gcodeanalysis.Value<JToken>("filament");
        //                if (filament != null)
        //                {
        //                    file.GcodeAnalysis_filament_length = filament.Value<int?>("length") ?? -1;
        //                    file.GcodeAnalysis_filament_volume = filament.Value<int?>("volume") ?? -1;
        //                }
        //            }
        //            JToken print = filedata.Value<JToken>("print");
        //            if (print != null)
        //            {
        //                file.Print_failure = print.Value<int?>("failure") ?? -1;
        //                JToken last = print.Value<JToken>("last");
        //                if (last != null)
        //                {
        //                    file.Print_last_date = last.Value<int>("date");
        //                    file.Print_last_success = last.Value<bool>("success");
        //                }
        //            }
        //            octoprintFiles.Add(file);
        //        }
        //    }
        //}

        //public override string ToString()
        //{
        //    string returnvalue = "";
        //    returnvalue += Name + ": " + Path + " ("+ Type + ") :\n";
        //    foreach (OctoprintFile file in octoprintFiles){
        //        returnvalue+="  " + file.ToString()+"\n";
        //    }
        //    foreach (OctoprintFolder folder in octoprintFolders)
        //    {
        //        if(folder!=null)
        //            returnvalue += "    " + folder.ToString().Replace("\n", "\n ");
        //    }
        //    return returnvalue;
        //}
    }
}