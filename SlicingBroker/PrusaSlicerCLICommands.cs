using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using System.Text;

namespace SlicingBroker
{
    public class PrusaSlicerCLICommands
    {
        public bool? ExportGCode { get; set; }
        public bool? ExportOBJ { get; set; }
        public bool? Slice { get; set; }
        public bool? SingleInstance { get; set; }
        public bool? Repair { get; set; }
        public bool? SupportMaterial { get; set; }

        public float? Rotate { get; set; }
        public float? RotateX { get; set; }
        public float? RotateY { get; set; }
        public float? Scale { get; set; }

        public string LoadConfigFile { get; set; }
        public string Output { get; private set; } 
        public string SaveConfigFile { get; set; }
        public string File { get; set; }

        public int? Loglevel { get; set; }
        public int? LayerHeight { get; set; }
        public int? FillDensity { get; set; }


        public Vector3? ScaleToFit { get; set; }
        public Vector2? AlignXY { get; set; }
        public Vector2? CenterXY { get; set; }

        public bool isValid()
        {
            //check all important parameters
            //align and center needs to be positve
            //check fill between 0 and 100
            //check layerheight (between 0.05 and 0.3) 
            //check for input file not null or empty



            return true;
        }

        public override string ToString()
        {

            //ToDo: Add all the other parameters!

            StringBuilder commandBuilder = new StringBuilder();

            //actions

            if(ExportGCode.HasValue && ExportGCode == true)
            {
                commandBuilder.Append("--export-gcode ");
            }

            //transform


            if(Scale.HasValue)
            {
                commandBuilder.Append("--scale ");
                commandBuilder.Append(Scale.ToString());
                commandBuilder.Append(" ");
            }
            //options

            //input path/file 


            return commandBuilder.ToString();
        }
    }
}
