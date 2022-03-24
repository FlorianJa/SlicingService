# Slicing Service
This application provides a web interface (websocket) to slice a given stl file. The resulting gcode can be downloaded via a REST API.


## Installation 
To get your development environment running you need to:
- Install a supported slicer software, currently PrusaSlicer is only supported. [Download PrusaSlicer](https://www.prusa3d.com/prusaslicer/).
- Install a .Net core 3.0 or higer development kit [Download .Netcore](https://dotnet.microsoft.com/download/dotnet-core/3.0).

## How to use
- Fork the application, make sure everything builds and there is no errors on your machine. 
- Open the appsettings.json file and change the following:

|Tag Name        |Property to change            |What to do                   |
|----------------|------------------------------|-----------------------------|
|Slicer          |`"Path"`                      |Put the path of the Prusa slicer console on your machine                      |
|Slicer          |`"ConfigPath"`                |Put the path for additional config files (.ini). This path is optional        |
|Kestrel         |`"Url"`                       |Put the URL/Port number that you want your websocket to be at                 |


## JSON message structure for triggering slicing
 
| Name                            | CLI Command                        | Type                 | Comment                                                                                                                                                                                                        |
|---------------------------------|------------------------------------|----------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| FileURI                         |                                    | string               | Required. Not used in CLI. The program will download the given. Needs to end with .gcode                                                                                                                       |
| Raft                            | --raft-layers                      | int?                 |                                                                                                                                                                                                                |
| Brim                            | --brim-width                       | int?                 |                                                                                                                                                                                                                |
| SupportMaterialBuildeplateOnly  | --support-material-buildplate-only | bool?                |                                                                                                                                                                                                                |
| FillPattern                     | --fill-pattern                     | string               | rectilinear, alignedrectilinear, grid, triangles, stars, cubic, line, concentric, honeycomb, 3dhoneycomb, gyroid, hilbertcurve, archimedeanchords, octagramspiral, adaptivecubic, supportcubic; default: stars |
| ExportGCode                     | --export-gcode                     | bool?                | Required if no config file is selected                                                                                                                                                                         |
| ExportOBJ                       | --export-obj                       | bool?                | Does not work. Will be removed soon.                                                                                                                                                                           |
| Slice                           | -s                                 | bool?                | Use ExportGCode instead.                                                                                                                                                                                       |
| SingleInstance                  | --single-instance                  | bool?                | Does not work. Will be removed soon.                                                                                                                                                                           |
| Repair                          | --repair                           | bool?                |                                                                                                                                                                                                                |
| SupportMaterial                 | --support-material                 | bool?                |                                                                                                                                                                                                                |
| Rotate                          | --rotate                           | float?               |                                                                                                                                                                                                                |
| RotateX                         | --rotate-x                         | float?               |                                                                                                                                                                                                                |
| RotateY                         | --rotate-y                         | float?               |                                                                                                                                                                                                                |
| Scale                           | --scale                            | float?               |                                                                                                                                                                                                                |
| LayerHeight                     | --layer-height                     | float?               |                                                                                                                                                                                                                |
| LoadConfigFile                  | --load                             | string               | Select one of the given profiles (after websocket is conected)                                                                                                                                                 |
| Output                          | -o                                 | string               | Gets overwirtten                                                                                                                                                                                               |
| SaveConfigFile                  | --save                             | string               |                                                                                                                                                                                                                |
| Loglevel                        | --loglevel                         | int?                 |                                                                                                                                                                                                                |
| FillDensity                     | --fill-density                     | float?               |                                                                                                                                                                                                                |
| ScaleToFit                      | --scale-to-fit                     | SerializableVector3  |                                                                                                                                                                                                                |
| AlignXY                         | --align-xy                         | SerializableVector2  |                                                                                                                                                                                                                |
| Center                          | --center                           | SerializableVector2  |                                                                                                                                                                                                                |
| GcodeComments                   | --gcode-comments                   | string               |                                                                                                                                                                                                                |
| File                            |                                    |                      | Use FileURi instead                                                                                                                                                                                                             |
