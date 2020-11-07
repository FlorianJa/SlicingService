using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OctoPrintLib.Operations
{
    public class OctoprintGeneral: OctoprintConnection
    {
        public OctoprintGeneral(OctoprintServer server) : base(server)
        {

        }

        public LoginResponse Login()
        {
            JObject data = new JObject
            {
                { "passive", server.ApplicationKey }
            };

            var response = PostJson("api/login", data);
            return JsonConvert.DeserializeObject<LoginResponse>(response);
        }
    }
}
