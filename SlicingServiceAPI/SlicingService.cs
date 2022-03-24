using SlicerCLIWrapper;
using SlicingServiceCommon;

namespace SlicingServiceAPI
{
    public class SlicingService
    {
        private readonly String ModelDownloadPath;
        private readonly String GCodePath;
        private readonly String SlicingConfigPath;
        private readonly Slicer slicer;

        public event EventHandler<string>? output;

        public SlicingService(string modelDownloadPath, string gCodePath, string slicingConfigPath, Slicer slicer)
        {
            ModelDownloadPath = modelDownloadPath;
            GCodePath = gCodePath;
            SlicingConfigPath = slicingConfigPath;
            this.slicer = slicer;

            slicer.DataReceived += (s, e) =>
            {
                output?.Invoke(s, e);
            };
        }

        public List<string> GetConfigFileNames()
        {
            var ret = new List<string>();
            foreach (var path in Directory.GetFiles(SlicingConfigPath))
            {
                ret.Add(Path.GetFileName(path));
            }
            return ret;
        }

        public async Task<FileSlicedArgs> SliceAsync(PrusaSlicerCLICommands commands)
        {
            await PrepareSlicing(commands);
            return await slicer.SliceAsync(commands);
        }

        public async Task PrepareSlicing(PrusaSlicerCLICommands commands)
        {
            // Ouput folder and config file path. These parameters need to be overwritten.
            commands.Output = GCodePath;
            SetSlicingProfilPath(commands);

            var fileUri = new Uri(commands.FileURI);
            if(commands.FileName == null)
            {
                commands.FileName = fileUri.Segments[^1];
            }
            var localFullPath = Path.Combine(ModelDownloadPath, commands.FileName);
            await DownloadHelper.DownloadModelAsync(fileUri, localFullPath);
            
            // use the local file on the disk
            commands.File = localFullPath;
        }

        private bool SetSlicingProfilPath(PrusaSlicerCLICommands commands)
        {
            if (commands.LoadConfigFile != null)
            {
                var configFilePath = Path.Combine(SlicingConfigPath, commands.LoadConfigFile);
                if (File.Exists(configFilePath))
                {
                    commands.LoadConfigFile = configFilePath;
                    return true;
                }
                else
                {
                    throw new FileNotFoundException("Config file not found");
                }
            }
            return true;
        }

    }
}
