using Newtonsoft.Json.Linq;
using OctoPrintLib.OctoPrintEvents;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace OctoPrintLib

{
    /// <summary>
    /// is the base Class connecting your project to different parts of Octoprint.
    /// </summary>
    public class OctoprintConnection
    {
        /// <summary>
        /// The base URL like https://192.168.1.2/
        /// </summary>
        public string BaseURL { get; private set; }

        /// <summary>
        /// The end point Api Key like "ABCDE12345"
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// The Websocket Client
        /// </summary>
        ClientWebSocket WebSocket { get; set; }
        /// <summary>
        /// Defines if the WebsocketClient is listening and the Tread is running
        /// </summary>
        public volatile bool listening;
        /// <summary>
        /// The size of the web socket buffer. Should work just fine, if the Websocket sends more, it will be split in 4096 Byte and reassembled in this class.
        /// </summary>
        public int WebSocketBufferSize = 4096;


        /// <summary>
        /// Creates a <see cref="T:OctoprintClient.OctoprintConnection"/> 
        /// </summary>
        /// <param name="baseURL">The endpoint Address like "http://192.168.1.2/"</param>
        /// <param name="aK">The Api Key of the User account you want to use. You can get this in the user settings</param>
        public OctoprintConnection(string baseURL, string aK)
        {
            BaseURL = baseURL;
            ApiKey = aK;
        }

        
               
        /// <summary>
        /// Gets the websocketUrl.
        /// </summary>
        /// <returns>The websocket Url.</returns>
        

        /// <summary>
        /// A Get request for any String using your Account
        /// </summary>
        /// <returns>The result as a String, doesn't handle Exceptions</returns>
        /// <param name="location">The url sub-address like "http://192.168.1.2/<paramref name="location"/>"</param>
        public string Get(string location)
        {
            string strResponseValue = string.Empty;
            Debug.WriteLine("This was searched:");
            Debug.WriteLine(BaseURL + location + "?apikey=" + ApiKey);
            WebClient wc = new WebClient();
            wc.Headers.Add("X-Api-Key", ApiKey);
            Stream downStream = wc.OpenRead(BaseURL + location);
            using (StreamReader sr = new StreamReader(downStream))
            {
                strResponseValue = sr.ReadToEnd();
            }
            return strResponseValue;
        }

        /// <summary>
        /// Posts a string with the rights of your Account to a given <paramref name="location"/>..
        /// </summary>
        /// <returns>The Result if any exists. Doesn't handle exceptions</returns>
        /// <param name="location">The url sub-address like "http://192.168.1.2/<paramref name="location"/>"</param>
        /// <param name="arguments">The string to post tp the address</param>
        public string PostString(string location, string arguments)
        {
            string strResponseValue = string.Empty;
            Debug.WriteLine("This was searched:");
            Debug.WriteLine(BaseURL + location + "?apikey=" + ApiKey);
            WebClient wc = new WebClient();
            wc.Headers.Add("X-Api-Key", ApiKey);
            strResponseValue = wc.UploadString(BaseURL + location, arguments);
            return strResponseValue;
        }

        ///// <summary>
        ///// Posts a JSON object as a string, uses JObject from Newtonsoft.Json to a given <paramref name="location"/>.
        ///// </summary>
        ///// <returns>The Result if any exists. Doesn't handle exceptions</returns>
        ///// <param name="location">The url sub-address like "http://192.168.1.2/<paramref name="location"/>"</param>
        ///// <param name="arguments">The Newtonsoft Jobject to post tp the address</param>
        //public string PostJson(string location, JObject arguments)
        //{
        //    string strResponseValue = string.Empty;
        //    Debug.WriteLine("This was searched:");
        //    Debug.WriteLine(EndPoint + location + "?apikey=" + ApiKey);
        //    String argumentString = string.Empty;
        //    argumentString = JsonConvert.SerializeObject(arguments);
        //    //byte[] byteArray = Encoding.UTF8.GetBytes(argumentString);
        //    HttpWebRequest request = WebRequest.CreateHttp(EndPoint + location);// + "?apikey=" + apiKey);
        //    request.Method = "POST";
        //    request.Headers["X-Api-Key"] = ApiKey;
        //    request.ContentType = "application/json";
        //    using (var streamWriter = new StreamWriter(request.GetRequestStream()))
        //    {
        //        streamWriter.Write(argumentString);
        //    }
        //    HttpWebResponse httpResponse;
        //    httpResponse = (HttpWebResponse)request.GetResponse();

        //    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
        //    {
        //        var result = streamReader.ReadToEnd();
        //    }
        //    return strResponseValue;
        //}

        /// <summary>
        /// Posts a Delete request to a given <paramref name="location"/>
        /// </summary>
        /// <returns>The Result if any, shouldn't return anything.</returns>
        /// <param name="location">The url sub-address like "http://192.168.1.2/<paramref name="location"/>"</param>
        public string Delete(string location)
        {
            string strResponseValue = string.Empty;
            Debug.WriteLine("This was deleted:");
            Debug.WriteLine(BaseURL + location + "?apikey=" + ApiKey);
            HttpWebRequest request = WebRequest.CreateHttp(BaseURL + location);// + "?apikey=" + apiKey);
            request.Method = "DELETE";
            request.Headers["X-Api-Key"] = ApiKey;
            HttpWebResponse httpResponse;
            httpResponse = (HttpWebResponse)request.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
            }
            return strResponseValue;
        }

        /// <summary>
        /// Posts a multipart reqest to a given <paramref name="location"/>
        /// </summary>
        /// <returns>The Result if any.</returns>
        /// <param name="packagestring">A packagestring should be generated elsewhere and input here as a String</param>
        /// <param name="location">The url sub-address like "http://192.168.1.2/<paramref name="location"/>"</param>
        public string PostMultipartOld(string packagestring, string location)
        {
            Debug.WriteLine("A Multipart was posted to:");
            Debug.WriteLine(BaseURL + location + "?apikey=" + ApiKey);
            string strResponseValue = String.Empty;
            var webClient = new WebClient();
            string boundary = "------------------------" + DateTime.Now.Ticks.ToString("x");
            webClient.Headers.Add("Content-Type", "multipart/form-data; boundary=" + boundary);
            webClient.Headers.Add("X-Api-Key", ApiKey);
            packagestring.Replace("{0}", boundary);
            string package = packagestring.Replace("{0}", boundary);

            var nfile = webClient.Encoding.GetBytes(package);


            File.WriteAllBytes(@"G:\Work\testbytes.txt", nfile);

            byte[] resp = webClient.UploadData(BaseURL + location, "POST", nfile);
            return strResponseValue;
        }
        public async Task<string> PostMultipartAsync(string fileData, string fileName, string location, string path = "")
        {
            var httpClient = new HttpClient();
            var headers = httpClient.DefaultRequestHeaders;

            headers.Add("X-Api-Key", ApiKey);
            Uri requestUri = new Uri(BaseURL + location);


            MultipartFormDataContent multipartContent = new MultipartFormDataContent();
            multipartContent.Add(new StringContent(fileData), "file", fileName);
            if (path != "") multipartContent.Add(new StringContent(path), "path");
            string responsebody = string.Empty;
            try
            {
                //Send the GET request
                var response = await httpClient.PostAsync(requestUri, multipartContent);

                responsebody = await response.Content.ReadAsStringAsync();
                //strResponseValue = httpResponseBody;
            }
            catch (Exception ex)
            {
                responsebody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }
            return responsebody;

        }
                

        /// <summary>
        /// Starts the Websocket Thread.
        /// </summary>
        public async Task WebsocketStartAsync()
        {
            if (!listening)
            {
                listening = true;
                await ConnectWebsocket();
                await Task.Run(WebsocketSyncAsync);
            }
        }

        private async Task ConnectWebsocket()
        {
            var canceltoken = CancellationToken.None;
            WebSocket = new ClientWebSocket();
            await WebSocket.ConnectAsync(GetWebsocketURI(),
                canceltoken);
        }

        private Uri GetWebsocketURI()
        {
            return new Uri("ws://" + BaseURL.Replace("https://", "").Replace("http://", "") + "sockjs/websocket");
        }

        private async Task WebsocketSyncAsync()
        {
            var buffer = new byte[8096];
            while (!WebSocket.CloseStatus.HasValue && listening)
            {
                WebSocketReceiveResult received = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string text = System.Text.Encoding.UTF8.GetString(buffer, 0, received.Count);
                HandleWebSocketData(text);
            }
        }
        
        private void HandleWebSocketData(string data)
        {
            JObject obj = null;

            try
            {
                obj = JObject.Parse(data);
            }
            catch
            {
                return;
            }

            JToken events = obj?.Value<JToken>("event");

            if (events != null)
            {
                string eventName = events.Value<string>("type");

                if (eventName == "FileAdded")
                {
                    JObject eventpayload = events.Value<JObject>("payload");
                    var file = new OctoprintFile(eventpayload);

                    if (file.Type == "stl")
                    {
                        //Download file and trigger slicing process
                    }
                }
            }
        }
    }
}
