using System;
using System.Threading.Tasks;

namespace SlicingCLI
{
    public interface ISlicerBroker
    {
        string SlicerPath { get; }

        Task SliceAsync(PrusaSlicerCLICommands arguments);

    }
}