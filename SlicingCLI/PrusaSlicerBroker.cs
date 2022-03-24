using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SlicingCLI
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
        private static SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private bool isBusy = false;

        private string GCodePath = null;

        public PrusaSlicerBroker(string localSlicerPath)
        {
            SlicerPath = localSlicerPath;
        }

        public async Task SliceAsync(PrusaSlicerCLICommands commands)
        {
            //request entry to the function if there is no one else using it
            await semaphore.WaitAsync();
            Console.WriteLine("Slicing started.");
            Console.WriteLine(commands.ToString());
            isBusy = true;
            var arguments = commands.ToString();
            eventHandled = new TaskCompletionSource<bool>();
            int errorsReceived = 0;
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

                    slicingProcess.Exited += (sender, args) =>
                    {
                        eventHandled.TrySetResult(true);
                        FileSlicedArgs eventArgs;
                        if (CheckFileSliced(commands, errorsReceived))
                        {
                            int days = 0, hours = 0, minutes = 0;
                            float usedFilament = 0f;
                            Regex rx = new Regex(@"(?<LayerHeight>\d*[.]\d+).*_(?:(?:(?<Days>\d*)d)?(?<Hours>\d+)h)?(?<Minutes>\d+)m_(?<UsedFilament>\d*[.]\d*)", RegexOptions.Compiled); //get layerheight and print duration in day, hours and minutes

                            var match = rx.Match(GCodePath);

                            var layerHeight = match.Groups["LayerHeight"].Value;
                            var daysString = match.Groups["Days"].Value;
                            var hoursString = match.Groups["Hours"].Value;
                            var minutesString = match.Groups["Minutes"].Value;
                            var usedFilamentString = match.Groups["UsedFilament"].Value;

                            if (!string.IsNullOrEmpty(daysString)) days = Int32.Parse(daysString);
                            if (!string.IsNullOrEmpty(hoursString)) hours = Int32.Parse(hoursString);
                            if (!string.IsNullOrEmpty(minutesString)) minutes = Int32.Parse(minutesString);
                            if (!string.IsNullOrEmpty(usedFilamentString)) usedFilament = float.Parse(usedFilamentString, CultureInfo.InvariantCulture);


                            eventArgs = new FileSlicedArgs(true, GCodePath, days, hours, minutes, usedFilament);
                        }
                        else
                        {
                            eventArgs = new FileSlicedArgs(false);
                        }

                        FileSliced?.Invoke(this, eventArgs);
                        //release the locking of the function so that the other callers who are waiting can get to it one by one.
                        semaphore.Release();
                        Console.WriteLine("Slicing done.");
                        isBusy = false;
                    };
                    slicingProcess.OutputDataReceived += (sender, args) =>
                    {
                        if (!String.IsNullOrEmpty(args.Data))
                        {
                            Debug.WriteLine("Output: " + args.Data);

                            if(args.Data.StartsWith("Slicing result exported to"))
                            {
                                GCodePath = args.Data.Substring(27);
                            }

                            OutputDataReceived(args);
                        }
                    };
                    slicingProcess.ErrorDataReceived += (sender, args) =>
                    {
                        if (!String.IsNullOrEmpty(args.Data))
                        {
                            errorsReceived++;
                            Debug.WriteLine("Error: " + args.Data);
                            OutputDataReceived(args);
                        }
                    };

                    slicingProcess.StartInfo = psi;
                    slicingProcess.EnableRaisingEvents = true;

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


            isBusy = false;
        }

        private bool CheckFileSliced(PrusaSlicerCLICommands commands, int errorsReceived)
        {
            bool ret = true;
            if (errorsReceived > 0)
                ret = false;

            //var slicedFilePath =
            //    Path.Combine(commands.Output, Path.GetFileNameWithoutExtension(commands.File) + ".gcode");
            if (GCodePath == null)
            {
                ret = false;
            }
            else
            {
                if (!File.Exists(GCodePath))
                    ret = false;

                var fileSize = new FileInfo(GCodePath).Length;
                if (fileSize == 0)
                    ret = false;

            }
            return ret;
            
        }

        private void OutputDataReceived(DataReceivedEventArgs args)
        {
            DataReceived?.Invoke(this, args);
        }

    }
}
