using System;
using System.Collections.Generic;
using System.Text;

namespace OctoPrintLib
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    
    public class Permission
    {
        public string key { get; set; }
        public string name { get; set; }
        public bool dangerous { get; set; }
        public List<string> default_groups { get; set; }
        public string description { get; set; }
        public Needs needs { get; set; }
    }

    public class Connected
    {
        public string version { get; set; }
        public string display_version { get; set; }
        public string branch { get; set; }
        public string plugin_hash { get; set; }
        public string config_hash { get; set; }
        public bool debug { get; set; }
        public object safe_mode { get; set; }
        public List<Permission> permissions { get; set; }
    }

    public class WebSocketConnectedMessage
    {
        public Connected connected { get; set; }
    }
}
