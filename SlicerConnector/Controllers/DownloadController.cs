using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SlicerConnector.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class DownloadController : ControllerBase
    {
        private string DataPath;

        public DownloadController(IConfiguration configuration)
        {
            var tmp = configuration as ConfigurationRoot;

            var BasePath = configuration.GetValue<string>("OctoPrint:BasePath");
            DataPath = Path.Combine(BasePath, "Mehes");
        }

        // GET: api/<DownloadController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            //TODO: list all files
            return new string[] { "value1", "value2" }; 
        }

        [HttpGet("{filename}")]
        public async Task<IActionResult> DownloadFile(string filename)
        {
            if (filename == null)
                return Content("filename not present");

            var path = @"D:\SlicerConnector\Meshes\triceratops-combinedonlyExternal.zip";

            //Path.Combine(
            //               Directory.GetCurrentDirectory(),
            //               "wwwroot", filename);

            var memory = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, "application/zip", Path.GetFileName(path));
        }
    }
}
