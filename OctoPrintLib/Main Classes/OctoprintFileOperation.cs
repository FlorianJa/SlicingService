using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OctoPrintLib.File;

namespace OctoPrintLib.Operations
{
    /// <summary>
    /// Tracks Files, can delete, upload and slice.
    /// </summary>
    public class OctoprintFileOperation : OctoprintConnection
    {
        /// <summary>
        /// Initializes a Filetracker, this shouldn't be done directly and is part of the Connection it needs anyway
        /// </summary>
        /// <param name="server">The Octoprint connection it connects to.</param>
        public OctoprintFileOperation(OctoprintServer server) : base(server)
        {
        }

        /// <summary>
        /// Gets all the files on the Server
        /// </summary>
        public async Task<OctoprintFileResponse> GetFilesAsync()
        {
            string jobInfo = await GetAsync("api/files");
            return JsonConvert.DeserializeObject<OctoprintFileResponse>(jobInfo);

        }

        /// <summary>
        /// Retrieve a specific file’s or folder’s information
        /// </summary>
        /// <param name="path"> the path of the file including its name and extension</param>
        /// <returns></returns>
        public async Task<OctoprintFile> GetFileInfoAsync(string location, string path)
        {
            string jobInfo = "";
            try
            {
                jobInfo = await GetAsync("api/files/" + location + "/" + path);
            }
            catch (WebException e)
            {
                switch (((HttpWebResponse)e.Response).StatusCode)
                {
                    case HttpStatusCode.NotFound:
                        Debug.WriteLine("searched for a file that wasn't there at " + path);
                        return null;
                }
            }
            return JsonConvert.DeserializeObject<OctoprintFile>(jobInfo);
        }

        /// <summary>
        /// Selects the File for printing
        /// </summary>
        /// <returns>The Http Result</returns>
        /// <param name="path">The path of the file that should be selected.</param>
        /// <param name="location">The location (local or sdcard) where this File should be. Normally local</param>
        /// <param name="print">If set, defines if the GCode should be printed directly after being selected. null means false</param>
        public string Select(string path, bool print = false, string location = "local")
        {
            JObject data = new JObject
            {
                { "command", "select" },
                { "print", print}
            };

            try
            {
                return PostJson("api/files/" + location + "/" + path, data);
            }
            catch (WebException e)
            {
                switch (((HttpWebResponse)e.Response).StatusCode)
                {
                    case HttpStatusCode.Conflict:
                        return "409 The Printer is propably not operational";
                    default:
                        return "unknown webexception occured";
                }

            }
        }

        

        /// <summary>
        /// Deletes a File
        /// </summary>
        /// <returns>The Http Result</returns>
        /// <param name="location">Location of the File to delete, sdcard or local</param>
        /// <param name="path">The path of the File to delete.</param>
        public async Task<string> DeleteAsync(string location, string path)
        {
            try
            {
                return await base.DeleteAsync("api/files/" + location + "/" + path);
            }
            catch (WebException e)
            {
                switch (((HttpWebResponse)e.Response).StatusCode)
                {
                    case HttpStatusCode.Conflict:
                        return "409 The file is currently in use by a Slicer or a Printer";
                    case HttpStatusCode.NotFound:
                        return "404 did not find the file";
                    default:
                        return "unknown webexception occured";
                }

            }


        }

        /// <summary>
        /// Creates a folder, if a subfolder should be created, create it with slashes and the path before it.
        /// </summary>
        /// <returns>The Http Result</returns>
        /// <param name="path">The Path of the Folder.</param>
        public async Task<string> CreateFolderAsync(string path)
        {
            string foldername = path.Split('/')[path.Split('/').Length - 1];
            path = path.Substring(0, path.Length - foldername.Length);
            string packagestring = "" +
                "--{0}\r\n" +
                "Content-Disposition: form-data; name=\"foldername\";\r\n" +
                "\r\n" +
                foldername + "\r\n" +
                "--{0}--\r\n" +
                "Content-Disposition: form-data; name=\"path\"\r\n" +
                "\r\n" +
                path + "\r\n" +
                "--{0}--\r\n";

            return await PostMultipartAsync(Encoding.ASCII.GetBytes(packagestring), "newfolder" ,"/api/files/local");

        }


        ///// <summary>
        ///// Uploads a file from local to the Server
        ///// </summary>
        ///// <returns>The Http Result</returns>
        ///// <param name="filename">Filename of the local file.</param>
        ///// <param name="onlinepath">Path to upload the file to.</param>
        ///// <param name="location">Location to upload to, local or sdcard, not sure if sdcard works, but takes ages anyway.</param>
        ///// <param name="select">If set to <c>true</c> selects the File to print next.</param>
        ///// <param name="print">If set to <c>true</c> prints the File.</param>
        //public string UploadFileOld(string filepath, string filename, string onlinepath = "local\\", string location = "local", bool select = false, bool print = false)
        //{
        //    string fileData = string.Empty;
        //    fileData = System.IO.File.ReadAllText(filepath);
        //    //filename = (filepath.Split('/')[filepath.Split('/').Length - 1]).Split('\\')[filepath.Split('\\')[filepath.Split('\\').Length - 1].Length - 1];
        //    string packagestring = "" +
        //                           "--{0}\r\n" +
        //                           "Content-Disposition: form-data; name=\"file\"; filename=\"" + filename + "\"\r\n" +
        //                           "Content-Type: application/octet-stream\r\n" +
        //                           "\r\n" +
        //                           fileData + "\r\n" +

        //                           "--{0}\r\n" +
        //                           "Content-Disposition: form-data; name=\"path\";\r\n" +
        //                           "\r\n" +
        //                           onlinepath + "\r\n" +
        //                           "--{0}--\r\n" +
        //                           "Content-Disposition: form-data; name=\"select\";\r\n" +
        //                           "\r\n" +
        //                           select + "\r\n" +
        //                           "--{0}--\r\n" +
        //                           "Content-Disposition: form-data; name=\"print\"\r\n" +
        //                           "\r\n" +
        //                           print + "\r\n" +
        //                           "--{0}--\r\n";
        //    return PostMultipartOld(packagestring, "api/files/" + location);
        //}



        public async Task<string>  UploadFileAsync(string localFullFilePath, string filename, string onlinepath = "", string location = "local", bool select = false, bool print = false)
        {
            var fileData = await System.IO.File.ReadAllBytesAsync(localFullFilePath); //check if file exists
            return await PostMultipartAsync(fileData, filename, "api/files/" + location, onlinepath);
        }
    }


    /// <summary>
    /// Uploads a file from local to the Server
    /// </summary>
    /// <returns>The Http Result</returns>
    /// <param name="filename">Filename of the local file.</param>
    /// <param name="onlinepath">Path to upload the file to.</param>
    /// <param name="location">Location to upload to, local or sdcard, not sure if sdcard works, but takes ages anyway.</param>
    /// <param name="select">If set to <c>true</c> selects the File to print next.</param>
    /// <param name="print">If set to <c>true</c> prints the File.</param>
    //public string UploadFile(string filename,  string onlinepath="", string location="local", bool select=false, bool print=false)
    //{
    //    string fileData =string.Empty;
    //    fileData= System.IO.File.ReadAllText(filename);
    //    filename=(filename.Split('/')[filename.Split('/').Length-1]).Split('\\')[filename.Split('\\')[filename.Split('\\').Length - 1].Length - 1];
    //    string packagestring="" +
    //        "--{0}\r\n" +
    //        "Content-Disposition: form-data; name=\"file\"; filename=\""+filename+"\"\r\n" +
    //        "Content-Type: application/octet-stream\r\n" +
    //        "\r\n" +
    //        fileData + "\r\n" +

    //        "--{0}\r\n" +
    //        "Content-Disposition: form-data; name=\"path\";\r\n" +
    //        "\r\n" +
    //        onlinepath + "\r\n" +
    //        "--{0}--\r\n" +
    //        "Content-Disposition: form-data; name=\"select\";\r\n" +
    //        "\r\n" +
    //        select + "\r\n" +
    //        "--{0}--\r\n" +
    //        "Content-Disposition: form-data; name=\"print\"\r\n" +
    //        "\r\n" +
    //        print + "\r\n" +
    //        "--{0}--\r\n";
    //    return Connection.PostMultipart(packagestring, "api/files/"+location);
    //}
}

