using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace OctoPrintLib
{
    /// <summary>
    /// The base class for the different Trackers
    /// </summary>
    public class OctoprintConnection
    {
        protected OctoprintServer server { get; set; }
        public OctoprintConnection(OctoprintServer server)
        {
            this.server = server;
        }


        /// <summary>
        /// A Get request for any String using your Account
        /// </summary>
        /// <returns>The result as a String, doesn't handle Exceptions</returns>
        /// <param name="location">The url sub-address like "http://192.168.1.2/<paramref name="location"/>"</param>
        protected async Task<string> GetAsync(string location)
        {
            string strResponseValue = string.Empty;
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add("X-Api-Key", server.ApplicationKey);
                Stream downStream = await wc.OpenReadTaskAsync(server.BaseURL + location);
                using (StreamReader sr = new StreamReader(downStream))
                {
                    strResponseValue = await sr.ReadToEndAsync();
                }
                return strResponseValue;
            }
        }

        /// <summary>
        /// Posts a string with the rights of your Account to a given <paramref name="location"/>..
        /// </summary>
        /// <returns>The Result if any exists. Doesn't handle exceptions</returns>
        /// <param name="location">The url sub-address like "http://192.168.1.2/<paramref name="location"/>"</param>
        /// <param name="arguments">The string to post tp the address</param>
        protected async Task<string> PostStringAsync(string location, string arguments)
        {
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add("X-Api-Key", server.ApplicationKey);
                return await wc.UploadStringTaskAsync(server.BaseURL + location, arguments);
            }
        }

        /// <summary>
        /// Posts a JSON object as a string, uses JObject from Newtonsoft.Json to a given <paramref name="location"/>.
        /// </summary>
        /// <returns>The Result if any exists. Doesn't handle exceptions</returns>
        /// <param name="location">The url sub-address like "http://192.168.1.2/<paramref name="location"/>"</param>
        /// <param name="arguments">The Newtonsoft Jobject to post tp the address</param>
        protected string PostJson(string location, JObject arguments)
        {
            string strResponseValue = string.Empty;
            String argumentString = string.Empty;
            argumentString = JsonConvert.SerializeObject(arguments);
            HttpWebRequest request = WebRequest.CreateHttp(server.BaseURL + location);// + "?apikey=" + apiKey);
            request.Method = "POST";
            request.Headers["X-Api-Key"] = server.ApplicationKey;
            request.ContentType = "application/json";
            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                streamWriter.Write(argumentString);
            }
            HttpWebResponse httpResponse;
            httpResponse = (HttpWebResponse)request.GetResponse();

            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                strResponseValue = streamReader.ReadToEnd();
            }
            return strResponseValue;
        }

        /// <summary>
        /// Posts a Delete request to a given <paramref name="location"/>
        /// </summary>
        /// <returns>The Result if any, shouldn't return anything.</returns>
        /// <param name="location">The url sub-address like "http://192.168.1.2/<paramref name="location"/>"</param>
        protected async Task<string> DeleteAsync(string location)
        {
            string strResponseValue = string.Empty;
            HttpWebRequest request = WebRequest.CreateHttp(server.BaseURL + location);
            request.Method = "DELETE";
            request.Headers["X-Api-Key"] = server.ApplicationKey;
            HttpWebResponse httpResponse;
            httpResponse = (HttpWebResponse)request.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = await streamReader.ReadToEndAsync();
            }
            return strResponseValue;
        }

        ///// <summary>
        ///// Posts a multipart reqest to a given <paramref name="location"/>
        ///// </summary>
        ///// <returns>The Result if any.</returns>
        ///// <param name="packagestring">A packagestring should be generated elsewhere and input here as a String</param>
        ///// <param name="location">The url sub-address like "http://192.168.1.2/<paramref name="location"/>"</param>
        //protected string PostMultipartOld(string packagestring, string location)
        //{
        //    string strResponseValue = string.Empty;
        //    var webClient = new WebClient();
        //    string boundary = "------------------------" + DateTime.Now.Ticks.ToString("x");
        //    webClient.Headers.Add("Content-Type", "multipart/form-data; boundary=" + boundary);
        //    webClient.Headers.Add("X-Api-Key", server.ApiKey);
        //    packagestring.Replace("{0}", boundary);
        //    string package = packagestring.Replace("{0}", boundary);

        //    var nfile = webClient.Encoding.GetBytes(package);


        //    File.WriteAllBytes(@"G:\Work\testbytes.txt", nfile);

        //    byte[] resp = webClient.UploadData(server.BaseURL + location, "POST", nfile);
        //    return strResponseValue;
        //}
        protected async Task<string> PostMultipartAsync(byte[] fileData, string fileName, string location = "local" , string path = "")
        {
            var httpClient = new HttpClient();
            var headers = httpClient.DefaultRequestHeaders;

            headers.Add("X-Api-Key", server.ApplicationKey);
            Uri requestUri = new Uri(server.BaseURL + location);

            MultipartFormDataContent multipartContent = new MultipartFormDataContent();
            multipartContent.Add(new StreamContent(new MemoryStream(fileData)), "file", fileName);
            if (path != "") multipartContent.Add(new StringContent(path), "path");
            string responsebody;
            try
            {
                //Send the GET request
                var response = await httpClient.PostAsync(requestUri, multipartContent);
                responsebody = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                responsebody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }
            return responsebody;
        }
    }
}