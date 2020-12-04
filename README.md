# SlicerConnector
 A software application for controllling connected 3D printers along with Octoprint, It includes features for tracking files and printers on Octoprintslicing, slicing 3d models and creating their meshes from the Gcode.
The application has two main functionalities. Firstly, It is used to interact with Octoprint servers, listen to it's event and deal with it accordingly, downloading and uploading files, slicing models and controling the printing process. Secondly, it also listens for incoming requests on a websocket, using the websocket you can choose the model you want to slice and configure the slicing parameters.


# Technology and frameworks used
- Built with [Microsoft .Net core 3](https://docs.microsoft.com/en-us/dotnet/core/introduction).
- Octoprint [Octoprint Documentation](https://docs.octoprint.org/en/master/).
- For communicating with Octoprint [Octoprint REST API](https://docs.octoprint.org/en/master/api/index.html).

## Installation 
To get your development environment running you need to:
- Install Octoprint on your machine [Download Octoprint](https://octoprint.org/download/) (You might need to install Python first).
- Install a supported slicer software, currently PrusaSlicer is only supported. [Download PrusaSlicer](https://www.prusa3d.com/prusaslicer/).
- Install a .Net core 3.0 or higer development kit [Download .Netcore](https://dotnet.microsoft.com/download/dotnet-core/3.0).

## How to use
- Fork the application, make sure everything builds and there is no errors on your machine. 
- Make sure Octoprint is up and running
- Get your API key from Octoprint's GUI 
- Open the appsettings.json file and change the following:

|Tag Name        |Property to change            |What to do                   |
|----------------|------------------------------|-----------------------------|
|Slicer          |`"Path"`                      |Put the path of the Prusa slicer console on your machine                      |
|OctoPrint       |`"DomainNameOrIP"`            |Put the current domain of the running instance of Octoprint                   |
|OctoPrint       |`"APIKey"`                    |Put your obtained Octoprint API key                                           |
|OctoPrint       |`"BasePath"`                  |Put the local path on your hard drive that you want to download/slice files at|
|Kestrel         |`"Url"`                       |Put the URL/Port number that you want your websocket to be at                 |
