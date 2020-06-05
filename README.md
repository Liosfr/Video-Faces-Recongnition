# Video-Faces-Recongnition

*My first program* in C# with Azure Face allows you to find person's frames in video from a profile picture.

## Setup

Set variables env : 

```
FACE_SUBSCRIPTION_KEY : Azure key
FACE_ENDPOINT : Azure face region endpoint
```

Move to the root of the project and compile it with the commands :

```
cd FrameWorker
dotnet build
```

To execute it, you have to run the command :

```
dotnet run --project FrameWorker
```

An initialization of the program will then run to create the necessary folders to run the program.
You will receive instructions about the files you intend to use such as videos or photos to be analyzed.

## Packages : 

Packages to install with NuGet

- Microsoft.Azure.CognitiveServices.Vision.Face
- MediaToolkit
- System.Configuration.ConfigurationManager
