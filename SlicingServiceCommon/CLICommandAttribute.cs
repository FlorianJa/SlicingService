using System;
using System.Collections.Generic;
using System.Text;

namespace SlicingServiceCommon
{
    [System.AttributeUsage(System.AttributeTargets.Property)]   
    public class CLICommand : System.Attribute
    {
        string command;

        public CLICommand(string command)
        {
            this.command = command;
        }

        public string GetCommand()
        {
            return command;
        }
    }
}
