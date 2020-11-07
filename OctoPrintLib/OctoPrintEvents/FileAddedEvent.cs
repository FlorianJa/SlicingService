using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace OctoPrintLib.OctoPrintEvents
{
    public class FileAddedEvent
    {
        public string Name { get; }
        public JObject Payload { get; set; }

        public JObject GetGenericPayload()
        {
            return Payload;
        }

        public OctoprintFile OctoFile { get; set; }


        public FileAddedEvent(string name, JObject payload)
        {
            this.Name = name;
            this.Payload = payload;
            //this.OctoFile = new OctoprintFile(payload);

        }

    }
}
