using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace SlicingBroker
{
    /// <summary>
    /// class represents a connection with local Prusa slicer installed, used to slice a file given its path, with the given slicing parameters or the default ones
    /// </summary>
    public class PrusaSlicerBroker : ISlicerBroker
    {
        #region Slicing Process 
        private Process slicingProcess;
        private TaskCompletionSource<bool> eventHandled;
        public event EventHandler<FileSlicedArgs> FileSliced;
        #endregion
        public PrusaSlicerBroker(string localSlicerPath, int fill = 20, double layer = 0.3, bool support = false, string outputpath = "", string outputname = "")
        {
            FillDensity = fill;
            LayerHeightInMM = layer;
            SupportStructureEnabled = support;
            OutputPath = outputpath;
            OutputName = outputname;
            SlicerPath = localSlicerPath;
        }


        public PrusaSlicerBroker(string localSlicerPath)
        {
            SlicerPath = localSlicerPath;
        }


        public string SlicerPath { get;  }
        private int fillDensity = 20;
        //public string FilePath { get;  set; }
        public double LayerHeightInMM { get; private set; } = 0.3;
        public string OutputPath { get; set; }
        public string OutputName { get; set; }

        public bool SupportStructureEnabled { get; private set; } = false;

        public int FillDensity
        {
            get => fillDensity;
            private set => fillDensity = SetFillDensity(value);
        }

        public EventHandler<DataReceivedEventArgs> DataReceived;

        private int SetFillDensity(int value)
        {
            if (value <= 0)
                return 0;
            if (value >= 100)
                return 100;
            // commented because of Issue#7 should be deleted if proven that no future use
            //if (value % 5 == 0)
            //    return value;
            //return 5 * (int)Math.Round(value / 5.0);
            return value;
        }


        public async Task SliceAsync(PrusaSlicerCLICommands commands, string ouput = "")
        {

            //var arguments = commands.ToString() + " -o " + ouput;
            eventHandled = new TaskCompletionSource<bool>();
            using (slicingProcess = new Process())
            {
                try
                {
                    var psi = new ProcessStartInfo(SlicerPath)
                    {
                        Arguments = @"-g --loglevel 5 C:\Users\FlorianJasche\Downloads\PrusaSlicer-2.3.0-alpha2+win64-202010241601\PrusaSlicer-2.3.0-alpha2+win64-202010241601\3DBenchy.stl",//arguments,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };

                    slicingProcess.StartInfo = psi;
                    slicingProcess.EnableRaisingEvents = true;
                    // when slicing complete send the slicing path to the function: if there is not specific output path then send the local file path

#warning adjust parameter of SlicingFinished
                    slicingProcess.Exited += (sender, args) => SlicingFinished(""); 


                    slicingProcess.StartInfo.RedirectStandardOutput = true;
                    slicingProcess.StartInfo.RedirectStandardError = true;

                    slicingProcess.OutputDataReceived += (sender, args) => {
                        if (!String.IsNullOrEmpty(args.Data))
                        {
                            OutputDataReceived(args);
                        }
                    };

                    slicingProcess.ErrorDataReceived += (sender, args) => {
                        if (!String.IsNullOrEmpty(args.Data))
                        {
                            OutputDataReceived(args);
                        }
                    };
                    slicingProcess.Start();

                    // Asynchronously read the standard output of the spawned process.
                    // This raises OutputDataReceived events for each line of output.
                    slicingProcess.BeginOutputReadLine();
                    slicingProcess.BeginErrorReadLine();

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                await Task.WhenAny(eventHandled.Task);
            }


        }

        private void OutputDataReceived(DataReceivedEventArgs args)
        {
            DataReceived?.Invoke(this, args);
        }


        public async Task SliceAsync(string localFilePath ,string outputPath = "")
        {
            //
            //if the path of the output gcode file is specified then slice and put it in that path (must be specified without .gcode extension)
            if (!string.IsNullOrEmpty(outputPath))
                this.OutputPath = outputPath;
            //if there is no specific slicing path, by default it is sliced in the same place of the stl 


            // code to use Prusa Slicer CLI to slice the file with the given parameters 
            string command = GenerateCommandString(localFilePath);

            eventHandled = new TaskCompletionSource<bool>();
            
            //Process.Start(psi)?.WaitForExit(30000);
            using (slicingProcess = new Process())
            {
                try
                {
                    var psi = new ProcessStartInfo(SlicerPath)
                    {
                        Arguments = command,
                        UseShellExecute = true,
                        CreateNoWindow = true
                    };

                    slicingProcess.StartInfo = psi;
                    slicingProcess.EnableRaisingEvents = true; 
                    // when slicing complete send the slicing path to the function: if there is not specific output path then send the local file path
                    slicingProcess.Exited += (sender, args) => SlicingFinished((string.IsNullOrEmpty(OutputPath))? localFilePath:OutputPath);
                    slicingProcess.Start();

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                await Task.WhenAny(eventHandled.Task, Task.Delay(40000));
            }


        }

        private void SlicingFinished(string slicedOrLocalFilePath)
        {
            eventHandled.TrySetResult(true);
            //string slicedFilePath = System.IO.Path.ChangeExtension(slicedOrLocalFilePath, ".gcode");
            //FileSliced?.Invoke(this, new FileSlicedArgs(slicedFilePath));
        }

        private string GenerateCommandString(string filePath)
        {
            StringBuilder commandBuilder = new StringBuilder();
            string fillDensityWithPrusaFormat = GetPrusaFormatFillDensity();


            //put the path to the prusa slicer 
            //commandBuilder.Append(SlicerPath);
            //commandBuilder.Append(" ");
            //add slice command shortcut
            commandBuilder.Append("-g");
            commandBuilder.Append(" ");
            //add model file path
            commandBuilder.Append(filePath);
            commandBuilder.Append(" ");
            //add Layer height 
            commandBuilder.Append("--layer-height=");
            commandBuilder.Append(LayerHeightInMM);
            commandBuilder.Append(" ");
            //add fill density after converting it to a value in the range of  0-1
            commandBuilder.Append("--fill-density=");
            commandBuilder.Append(fillDensityWithPrusaFormat);
            commandBuilder.Append(" ");
            //add support material if selected
            if (SupportStructureEnabled)
            {
                commandBuilder.Append("--support-material");
                commandBuilder.Append(" ");
            }

            //add a specific output path and name
            if (!String.IsNullOrEmpty(OutputPath))
            {
                commandBuilder.Append("-o");
                commandBuilder.Append(" ");
                commandBuilder.Append(OutputPath);
                commandBuilder.Append(".gcode");
            }

            //add a specific output name -in the same directory-
            else if (!String.IsNullOrEmpty(OutputName))
            {
                commandBuilder.Append("-o");
                commandBuilder.Append(" ");
                commandBuilder.Append(OutputName);
                commandBuilder.Append(".gcode");
            }




            return commandBuilder.ToString();
        }

        private string GetPrusaFormatFillDensity()
        {
            return (((double)fillDensity) / 100).ToString();
        }
    }
}
