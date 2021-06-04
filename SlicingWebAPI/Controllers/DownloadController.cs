using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SlicingWebAPI.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class DownloadController : ControllerBase
    {
        private string DataPath;
        // for controlling the connection with Hololens application
        public DownloadController(IConfiguration configuration)
        {
            var tmp = configuration as ConfigurationRoot;

            var BasePath = configuration.GetValue<string>("OctoPrint:BasePath");
            DataPath = Path.Combine(BasePath, "Meshes");
            if (!Directory.Exists(DataPath))
                Directory.CreateDirectory(DataPath);
        }

        // GET: api/<DownloadController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return System.IO.Directory.GetFiles(DataPath, "*.zip");
        }

        [HttpGet("{filename}")]
        public async Task<IActionResult> DownloadFile(string filename)
        {
            var filePath = Path.Combine(DataPath, filename + ".zip");
            if (CheckFileAvailability(filename, filePath, out string message))
            {
                var memory = new MemoryStream();
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;
                return File(memory, "application/zip", Path.GetFileName(filePath));
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
                message = "filename not present";
                return false;
            }

            if (!System.IO.File.Exists(filepath))
            {
                message = "The requested file was not found";
                return false;
            }

            return true;
        }
    }
}
