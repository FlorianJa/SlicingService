using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using System.Text;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace SlicingCLI
{
    public class PrusaSlicerCLICommands
    {
        public static PrusaSlicerCLICommands Default { get { return new PrusaSlicerCLICommands() { ExportGCode = true, SupportMaterial = false, LayerHeight = 0.2f, FillDensity = 0.5f, GcodeComments = true, Loglevel = 3, FillPattern = "line" }; } }

        #region File location

        public string FileURI { get; set; }
        public string FileName { get; set; }

        #endregion

        #region CLI Commands
        [CLICommand("--raft-layers")]
        public int? Raft { get; set; }

        [CLICommand("--brim-width")]
        public int? Brim { get; set; }

        [CLICommand("--support-material-buildplate-only")]
        public bool? SupportMaterialBuildeplateOnly { get; set; }

        [CLICommand("--fill-pattern")]
        public string FillPattern { get; set; }

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
        public string Output { get; set; }

        [CLICommand("--save")] 
        public string SaveConfigFile { get; set; }

        [CLICommand("--loglevel")] 
        public int? Loglevel { get; set; }

        [CLICommand("--fill-density")] 
        public float? FillDensity { get; set; }


        [CLICommand("--scale-to-fit")] 
        public SerializableVector3 ScaleToFit { get; set; }

        [CLICommand("--align-xy")] 
        public SerializableVector2 AlignXY { get; set; }

        [CLICommand("--center")] 
        public SerializableVector2 Center { get; set; }

        [CLICommand("--gcode-comments")]
        public bool? GcodeComments { get; set; }

        [CLICommand("")]
        public string File { get; set; }
        #endregion

        public bool isValid()
        {

            //if (String.IsNullOrEmpty(File))
            //    return false;

            if (FileURI == null)
                return false;
            
            if (!FillDensity.HasValue)
                return false;
            
            if (FillDensity < 0f || FillDensity > 1f)
                return false;

            if (!LayerHeight.HasValue)
                return false;

            if (LayerHeight < 0.05f || LayerHeight > 0.3f)
                return false;

            if (AlignXY!=null && (AlignXY.X < 0f || AlignXY.Y < 0f))
                return false;
            if (Center!=null &&(Center.X < 0f || Center.Y < 0f))
                return false;

            return true;
        }

        public override string ToString()
        {
            StringBuilder commandBuilder = new StringBuilder();

            //get all properties
            var props = typeof(PrusaSlicerCLICommands).GetProperties();

            foreach (var prop in props)
            {
                if (prop.GetValue(this) == null) continue;

                var attributes = prop.GetCustomAttributes(false);

                if (attributes.Length <= 0) continue;

                var CLICommand = attributes.First() as CLICommand;

                //there could be the rare case that GetProperties gets an property that has no CLICommand attribute -> ignore all of these properties
                if (CLICommand == null) continue;

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
                        var tempStr = (string)prop.GetValue(this);
                        if (tempStr.Contains(" "))
                        {
                            tempStr = "\"" + tempStr + "\"";
                        }
                        commandBuilder.Append(tempStr);
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
            
            return commandBuilder.ToString();
        }
    }
}
