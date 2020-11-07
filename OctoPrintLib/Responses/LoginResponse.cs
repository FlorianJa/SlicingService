using System;
using System.Collections.Generic;
using System.Text;

namespace OctoPrintLib
{
    public class LoginResponse
    {
        public bool _is_external_client { get; set; }
        public bool active { get; set; }
        public bool admin { get; set; }
        public string apikey { get; set; }
        public List<string> groups { get; set; }
        public string name { get; set; }
        public Needs needs { get; set; }
        public List<object> permissions { get; set; }
        public List<string> roles { get; set; }
        public string session { get; set; }
        public Settings settings { get; set; }
        public bool user { get; set; }
    }



    public class Needs
    {
        public List<string> group { get; set; }
        public List<string> role { get; set; }
    }

    public class Settings
    {
    }

}
