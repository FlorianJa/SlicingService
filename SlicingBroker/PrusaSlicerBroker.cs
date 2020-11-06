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

        public string SlicerPath { get; }
        
        public EventHandler<DataReceivedEventArgs> DataReceived;

        public PrusaSlicerBroker(string localSlicerPath)
        {
            SlicerPath = localSlicerPath;
        }
        
        public async Task SliceAsync(PrusaSlicerCLICommands commands, string ouput = "")
        {
            var arguments = commands.ToString() + " -o " + ouput;
            eventHandled = new TaskCompletionSource<bool>();
            using (slicingProcess = new Process())
            {
                try
                {
                    var psi = new ProcessStartInfo(SlicerPath)
                    {
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    };

                    slicingProcess.StartInfo = psi;
                    slicingProcess.EnableRaisingEvents = true;
                    #warning adjust parameter of SlicingFinished
                    slicingProcess.Exited += (sender, args) => SlicingFinished(""); 
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

        private void SlicingFinished(string slicedOrLocalFilePath)
        {
            eventHandled.TrySetResult(true);
            //string slicedFilePath = System.IO.Path.ChangeExtension(slicedOrLocalFilePath, ".gcode");
            //FileSliced?.Invoke(this, new FileSlicedArgs(slicedFilePath));
        }
    }
}
