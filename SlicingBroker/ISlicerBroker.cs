using System;
using System.Threading.Tasks;

namespace SlicingBroker
{
    public interface ISlicerBroker
    {
        string SlicerPath { get; }

        Task SliceAsync(PrusaSlicerCLICommands arguments, string outputPath="");

    }
}