using System.Collections.Generic;

namespace OctoPrintLib
{
    public class FileBase
    {
        public string name { get; set; }
        public string path { get; set; }

    }
    public class FileInHistoryMessage:FileBase
    {
        public string display { get; set; }
        public string type { get; set; }
        public List<string> typePath { get; set; }

    }

    public class FileInCurrentMessage:FileBase
    {
        public int? size { get; set; }
        public string origin { get; set; }
        public int? date { get; set; }
    }


}

   
