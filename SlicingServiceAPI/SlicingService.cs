using SlicingServiceCommon;

namespace SlicingServiceAPI
{
    public class SlicingService
    {
        private readonly String ModelDownloadPath;
        private readonly String GCodePath;
        private readonly String SlicingConfigPath;

        public SlicingService(string modelDownloadPath, string gCodePath, string slicingConfigPath)
        {
            ModelDownloadPath = modelDownloadPath;
            GCodePath = gCodePath;
            SlicingConfigPath = slicingConfigPath;
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
            return null;
        }

        public async Task PrepareSlicing(PrusaSlicerCLICommands commands)
        {
            // Ouput folder and config file path. These parameters need to be overwritten.
            commands.Output = GCodePath;
            SetSlicingProfilPath(commands);

            var fileUri = new Uri(commands.FileURI);
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
