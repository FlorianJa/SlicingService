using OctoPrintLib.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace OctoPrintLib
{
    public class FileAddedEventArgs : EventArgs
    {
        public Payload Payload{get;set;}

        public FileAddedEventArgs(Payload payload)
        {
            Payload = payload;
        }

    }
}
