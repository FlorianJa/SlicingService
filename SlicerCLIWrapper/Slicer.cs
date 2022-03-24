using CliWrap;
using CliWrap.EventStream;
using SlicingServiceCommon;
using System.Diagnostics;
using System.Globalization;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace SlicerCLIWrapper
{
    public class Slicer
    {
        private readonly string slicerPath;
        

        public event EventHandler<string> DataReceived;

        public Slicer(string slicerPath)
        {
            this.slicerPath = slicerPath;
        }

        public async Task<FileSlicedArgs> SliceAsync(PrusaSlicerCLICommands commands)
        {
            FileSlicedArgs result = null;
            string fileName = "";
            int errorsReceived = 0;
            var cmd = Cli.Wrap(slicerPath)
                .WithArguments(commands.ToString());

            await cmd.Observe().ForEachAsync(cmdEvent =>
            {
                switch (cmdEvent)
                {
                    case StartedCommandEvent started:
                        break;
                    case StandardOutputCommandEvent stdOut:
                        {
                            if (!String.IsNullOrEmpty(stdOut.Text))
                            {
                                if (stdOut.Text.StartsWith("Slicing result exported to"))
                                {
                                    fileName = stdOut.Text.Substring(27);
                                }

                                OutputDataReceived(stdOut.Text);
                            }
                            break;
                        }
                    case StandardErrorCommandEvent stdErr:
                        errorsReceived++;
                        break;
                    case ExitedCommandEvent exited:
                    {
                        if (CheckFileSliced(fileName, commands, errorsReceived))
                        {
                            int days = 0, hours = 0, minutes = 0;
                            float usedFilament = 0f;
                            Regex rx = new Regex(@"(?<LayerHeight>\d*[.]\d+).*_(?:(?:(?<Days>\d*)d)?(?<Hours>\d+)h)?(?<Minutes>\d+)m_(?<UsedFilament>\d*[.]\d*)", RegexOptions.Compiled); //get layerheight and print duration in day, hours and minutes

                            var match = rx.Match(fileName);

                            var layerHeight = match.Groups["LayerHeight"].Value;
                            var daysString = match.Groups["Days"].Value;
                            var hoursString = match.Groups["Hours"].Value;
                            var minutesString = match.Groups["Minutes"].Value;
                            var usedFilamentString = match.Groups["UsedFilament"].Value;

                            if (!string.IsNullOrEmpty(daysString)) days = Int32.Parse(daysString);
                            if (!string.IsNullOrEmpty(hoursString)) hours = Int32.Parse(hoursString);
                            if (!string.IsNullOrEmpty(minutesString)) minutes = Int32.Parse(minutesString);
                            if (!string.IsNullOrEmpty(usedFilamentString)) usedFilament = float.Parse(usedFilamentString, CultureInfo.InvariantCulture);

                            result = new FileSlicedArgs(true, fileName, days, hours, minutes, usedFilament);
                        }
                        else
                        {
                            result = new FileSlicedArgs(false);
                        }
                        break;
                    }
                }
            });

            return result;
        }

        private void OutputDataReceived(string data)
        {
            DataReceived?.Invoke(this, data);
        }

        private bool CheckFileSliced(string fileName, PrusaSlicerCLICommands commands, int errorsReceived)
        {
            bool ret = true;
            if (errorsReceived > 0)
                ret = false;

            if (string.IsNullOrEmpty(fileName))
            {
                ret = false;
            }
            else
            {
                if (!File.Exists(fileName))
                    ret = false;

                var fileSize = new FileInfo(fileName).Length;
                if (fileSize == 0)
                    ret = false;
            }
            return ret;

        }
    }
}