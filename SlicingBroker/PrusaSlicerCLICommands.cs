using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using System.Text;
using System.Globalization;

namespace SlicingBroker
{
    public class PrusaSlicerCLICommands
    {
        #region Properties
        [CLICommand("--export-gcode")]
        public bool? ExportGCode { get; set; }

        [CLICommand("--export-obj")]
        public bool? ExportOBJ { get; set; }

        [CLICommand("-s")]
        public bool? Slice { get; set; }

        [CLICommand("--single-isntance")]
        public bool? SingleInstance { get; set; }

        [CLICommand("--repair")]
        public bool? Repair { get; set; }

        [CLICommand("--support-material")]
        public bool? SupportMaterial { get; set; }

        [CLICommand("--rotate")]
        public float? Rotate { get; set; }

        [CLICommand("--rotate-x")] 
        public float? RotateX { get; set; }

        [CLICommand("--rotate-y")] 
        public float? RotateY { get; set; }

        [CLICommand("--scale")] 
        public float? Scale { get; set; }

        [CLICommand("--layer-height")] 
        public float? LayerHeight { get; set; }
               
        [CLICommand("--load")] 
        public string LoadConfigFile { get; set; }

        [CLICommand("-o")] 
        public string Output { get; private set; }

        [CLICommand("--save")] 
        public string SaveConfigFile { get; set; }

        [CLICommand("")] 
        public string File { get; set; }

        [CLICommand("--loglevel")] 
        public int? Loglevel { get; set; }

        [CLICommand("--fill-density")] 
        public int? FillDensity { get; set; }


        [CLICommand("--scale-to-fit")] 
        public SerializableVector3 ScaleToFit { get; set; }

        [CLICommand("--align-xy")] 
        public SerializableVector2 AlignXY { get; set; }

        [CLICommand("--center")] 
        public SerializableVector2 Center { get; set; }

        #endregion

        public bool isValid()
        {
#warning Fill with logic!

            //check all important parameters
            //align and center needs to be positve
            //check fill between 0 and 100
            //check layerheight (between 0.05 and 0.3) 
            //check for input file not null or empty



            return true;
        }

        public override string ToString()
        {
            StringBuilder commandBuilder = new StringBuilder();

            //get all properties
            var props = typeof(PrusaSlicerCLICommands).GetProperties();

            foreach (var prop in props)
            {
                if(prop.GetValue(this) != null)
                {
                    var CLICommand = prop.GetCustomAttributes(false)[0] as CLICommand;

                    //there could be the rare case that GetProperties gets an property that has no CLICommand attribute -> ignore all of these properties
                    if (CLICommand != null) 
                    {
                        if (prop.PropertyType == typeof(bool?))
                        {
                            // Add command only if value is true
                            if ((bool?)prop.GetValue(this) == true)
                            {
                                commandBuilder.Append(CLICommand.GetCommand()); 
                                commandBuilder.Append(" ");
                            }
                        }
                        else
                        {
                            //first add command and add value later
                            commandBuilder.Append(CLICommand.GetCommand());
                            commandBuilder.Append(" ");

                            if (prop.PropertyType == typeof(float?))
                            {
                                commandBuilder.Append(((float)prop.GetValue(this)).ToString("F", CultureInfo.InvariantCulture));
                                commandBuilder.Append(" ");
                            }
                            else if (prop.PropertyType == typeof(string))
                            {
                                commandBuilder.Append((string)prop.GetValue(this));
                                commandBuilder.Append(" ");
                            }
                            else if (prop.PropertyType == typeof(int?))
                            {
                                commandBuilder.Append(((int)prop.GetValue(this)).ToString());
                                commandBuilder.Append(" ");
                            }
                            else if (prop.PropertyType == typeof(SerializableVector2))
                            {
                                float x, y;
                                var tmp = (SerializableVector2)prop.GetValue(this);
                                x = tmp.X;
                                y = tmp.Y;
                                commandBuilder.Append(x.ToString("F", CultureInfo.InvariantCulture));
                                commandBuilder.Append(",");
                                commandBuilder.Append(y.ToString("F", CultureInfo.InvariantCulture));
                                commandBuilder.Append(" ");

                            }
                            else if (prop.PropertyType == typeof(SerializableVector3))
                            {
                                float x, y, z;
                                var tmp = (SerializableVector3)prop.GetValue(this);
                                x = tmp.X;
                                y = tmp.Y;
                                z = tmp.Z;
                                commandBuilder.Append(x.ToString("F", CultureInfo.InvariantCulture));
                                commandBuilder.Append(",");
                                commandBuilder.Append(y.ToString("F", CultureInfo.InvariantCulture));
                                commandBuilder.Append(",");
                                commandBuilder.Append(z.ToString("F", CultureInfo.InvariantCulture));
                                commandBuilder.Append(" ");
                            }
                        }
                    }
                }
            }
            
            return commandBuilder.ToString();
        }
    }
}
