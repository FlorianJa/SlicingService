using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SlicerConnector.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GCodeController : Controller
    {
        private string DataPath;
        // for controlling the connection with Hololens application
        public GCodeController(IConfiguration configuration)
        {
            var tmp = configuration as ConfigurationRoot;

            var BasePath = configuration.GetValue<string>("OctoPrint:BasePath");
            DataPath = Path.Combine(BasePath, "GCode");
            if (!Directory.Exists(DataPath))
                Directory.CreateDirectory(DataPath);
        }

        // GET: api/<DownloadController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            var fileNames = new List<string>();
            var filesFullPath = System.IO.Directory.GetFiles(DataPath, "*.gcode");

            foreach (string file in filesFullPath)
                fileNames.Add(Path.GetFileName(file));

            return fileNames;
        }

        [HttpGet("{filename}")]
        public async Task<IActionResult> DownloadFile(string filename)
        {
            var filePath = Path.Combine(DataPath, filename);
            if (CheckFileAvailability(filename, filePath, out string message))
            {
                var memory = new MemoryStream();
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;
                return File(memory, "application/gcode", Path.GetFileName(filePath));
            }

            else
            {
                return StatusCode(404, message);
            }
        }

        private bool CheckFileAvailability(string filename, string filepath, out string message)
        {
            message = "";
            if (String.IsNullOrWhiteSpace(filename))
            {
                message = "400. filename not present";
                return false;
            }

            if (!System.IO.File.Exists(filepath))
            {
                message = "404. The requested file was not found";
                return false;
            }

            return true;
        }
    }
}
