using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace OctoPrintLib.Messages
{
    public class Payload
    {
        public string storage { get; set; }
        public string path { get; set; }
        public string name { get; set; }

        public List<string> type { get; set; }
        public string username { get; set; }
        public string remoteAddress { get; set; }
        public string state_id { get; set; }
        public string state_string { get; set; }
}

    public class Event
    {
        public string type { get; set; }
        public Payload payload { get; set; }
    }

    public class OctoprintEvent
    {
        [JsonPropertyName("event")]
        public Event Event { get; set; }
    }
}
