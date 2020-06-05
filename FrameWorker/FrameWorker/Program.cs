using System;
using System.IO;
using System.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;


using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;

namespace FrameWorker
{
   class Program
    {

        const string inputDir = "input";
        const string tempDir = "temp";
        const string toFindDir = "tofind";
        const string faceResultDir = "faces";

        static Stream _sourceImageFileStream;
        static string faceName;
        static List<string> _targetImageFileNames = new List<string> {};

        /*
        *	AUTHENTICATE
        *	Uses subscription key and region to create a client.
        */
        public static IFaceClient Authenticate(string endpoint, string key)
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };
        }

        /*
        *	Initialize
        *	Create folders
        */
        private static void Initialize()
        {
            Console.WriteLine("===== INITIALIZATION =====");
            Console.WriteLine();
            Console.WriteLine("Creating needed folders if needed.");
            Console.WriteLine();

            if (!Directory.Exists(inputDir))
            {
                try
                {
                    Directory.CreateDirectory(inputDir);
                    Console.WriteLine("Folder " + inputDir + " created.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            if (!Directory.Exists(tempDir))
            {
                try
                {
                    Directory.CreateDirectory(tempDir);
                    Console.WriteLine("Folder " + tempDir + " created.");

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            if (!Directory.Exists(toFindDir))
            {
                try
                {
                    Directory.CreateDirectory(toFindDir);
                    Console.WriteLine("Folder " + toFindDir + " created.");

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            if (!Directory.Exists(faceResultDir))
            {
                try
                {
                    Directory.CreateDirectory(faceResultDir);
                    Console.WriteLine("Folder " + faceResultDir + " created.");

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        /*
        *	SPLIT VIDEO
        *	Take media and split them in many frames (1 per second)
        */
        public static void SplitVideo()
        {
            Console.WriteLine("===== Removing old temp files =====");

            try
            {
                string[] picList = Directory.GetFiles(tempDir);

                foreach (string file in picList)
                {
                    Console.WriteLine(file + " removed");
                    File.Delete(file);
                }
                Console.WriteLine();
            }
            catch (DirectoryNotFoundException dirNotFound)
            {
                Console.WriteLine(dirNotFound.Message);
            }


            Console.WriteLine("===== Choose a video to split in many frames =====");
            Console.WriteLine("Accepted format : .mp4");

            try
            {
                string[] vidList = Directory.GetFiles(inputDir);

                foreach (string file in vidList)
                {
                    Console.WriteLine(file.Substring(inputDir.Length + 1));
                }
                Console.WriteLine();
            }
            catch (DirectoryNotFoundException dirNotFound)
            {
                Console.WriteLine(dirNotFound.Message);
            }

            string selectedFile = Console.ReadLine();

            if (selectedFile == null || selectedFile == "") { Console.WriteLine("Can not be null"); return; }

            if (Path.GetExtension(inputDir + "/" + selectedFile) != ".mp4")
            {
                return;
            }

            string fileNameToSplit = inputDir + "/" + selectedFile;

            using (var engine = new Engine())
            {
                var mp4 = new MediaFile { Filename = fileNameToSplit };

                engine.GetMetadata(mp4);

                var i = 0;
                while (i < mp4.Metadata.Duration.TotalSeconds)
                {
                    var options = new ConversionOptions { Seek = TimeSpan.FromSeconds(i) };
                    var outputFile = new MediaFile { Filename = string.Format("{0}\\image-{1}.jpeg", tempDir, i) };
                    engine.GetThumbnail(mp4, outputFile, options);
                    _targetImageFileNames.Add(outputFile.Filename);
                    i++;
                }
            }
            Thread.Sleep(2000);
            Console.WriteLine("Video splited in many frames.");
            Console.WriteLine();
        }

        /*
        *	InitializeResultDirectoryFace
        *	Prepare directory result folder with the base file name (profile picture)
        */
        private static string InitializeResultDirectoryFace(String fileName)
        {
            String directoryName = Path.GetFileNameWithoutExtension(fileName);
            if (!Directory.Exists(faceResultDir + "/" + directoryName))
            {
                try
                {
                    Directory.CreateDirectory(faceResultDir + "/" + directoryName);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            return directoryName;
        }

        /*
        *	SelectImageToFind
        *	Allows users to choose base file (profile picture)
        */
        private static Stream SelectImageToFind()
        {
            Console.WriteLine("===== Choose an image to find similarities with =====");
            Console.WriteLine();

            try
            {
                string[] imgList = Directory.GetFiles(toFindDir);


                foreach (string file in imgList)
                {
                    Console.WriteLine(file.Substring(toFindDir.Length + 1));
                }
                Console.WriteLine();
            }

            catch (DirectoryNotFoundException dirNotFound)
            {
                Console.WriteLine(dirNotFound.Message);
            }

            string selectedFile = Console.ReadLine();

            if (selectedFile == null || selectedFile == "") { Console.WriteLine("Can not be null"); return null; }

            try
            {
                faceName = InitializeResultDirectoryFace(selectedFile);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return File.Open(toFindDir + "/" + selectedFile, FileMode.Open);
        }

        /*
         * FIND SIMILAR
         * Take an image and find a similar one to it in another image and move to person result folder.
         */
        public static async Task FindSimilar(IFaceClient client, IEnumerable<Stream> frames, string RECOGNITION_MODEL2)
        {

            Console.WriteLine("========FIND SIMILAR========");
            Console.WriteLine();

            IList<Guid?> targetFaceIds = new List<Guid?>();
            var i = 0;
            foreach (var targetFrame in frames)
            {
                var fileName = _targetImageFileNames.ElementAt(i);
                var renammed = false;
                Console.WriteLine("===== " +fileName+ " =====");

                var faces = await DetectFaceRecognize(client, targetFrame, RECOGNITION_MODEL2);

                Console.WriteLine("- " + faces.Count + " face(s) found -");
                foreach (var personFace in faces)
                {
                    if (personFace.FaceId != null)
                    {
                        var newFileName = personFace.FaceId.Value;
                        if (renammed == false)
                        {
                            File.Move( fileName, tempDir + "/" + newFileName + ".jpeg");
                            renammed = true;
                        }

                        Console.WriteLine(personFace.FaceId.Value);
                        targetFaceIds.Add(personFace.FaceId.Value);
                    }
                }
                i++;
            }


            IList<DetectedFace> detectedFaces =
                await DetectFaceRecognize(client, _sourceImageFileStream, RECOGNITION_MODEL2);

            var guid = detectedFaces[0].FaceId;
            if (guid != null)
            {
                try
                {
                    var similarResults = await client.Face.FindSimilarAsync(guid.Value, null, null, targetFaceIds);


                    if (similarResults == null) { Console.WriteLine("Error when tryin to find similarities."); return; }

                    Console.WriteLine();
                    foreach (var similarResult in similarResults)
                    {
                        // if (similarResult.Confidence > 0.92)
                        Console.WriteLine("===== " +similarResult.FaceId+ " =====");
                        Console.WriteLine(
                            $"- Face in {similarResult.FaceId}.jpeg " +
                            $"are similar with confidence: {similarResult.Confidence}. -");

                        Console.WriteLine("- Move to " +tempDir + "/" + similarResult.FaceId + ".jpeg"+ " -");
                        File.Move( tempDir + "/" + similarResult.FaceId + ".jpeg", faceResultDir + "/" + faceName + "/" + similarResult.FaceId + ".jpeg");
                        Console.WriteLine("- Moved -");
                    }
                }
                catch (APIErrorException e)
                {
                    Console.WriteLine("No faces have been found in the targeted frames.");
                }
            }
            else
            {
                Console.WriteLine("No face detected in base image.");
            }

            Console.WriteLine();

            _targetImageFileNames = new List<string> { };
        }

        /*
         * DetectFaceRecognize
         * Call Azure Endpoint
         */
        public static async Task<List<DetectedFace>> DetectFaceRecognize(IFaceClient faceClient, Stream image, string RECOGNITION_MODEL1)
        {
            var detectedFaces = await faceClient.Face.DetectWithStreamAsync(image, recognitionModel: RECOGNITION_MODEL1);

            return detectedFaces.ToList();
        }

        /*
         * GetTargetStreamList
         * convert frames file to Stream
         */
        private static List<Stream> GetTargetStreamList()
        {
            List<Stream> streamList = new List<Stream>();
            foreach (string filePath in _targetImageFileNames)
            {
                streamList.Add(File.Open(filePath, FileMode.Open));
            }
            if (streamList == null) { Console.WriteLine("Stream List is null... Error."); return null; }

            return streamList;
        }

        /*
         * MENU
         * interactive menu
         */
        private static void Menu(IFaceClient client, string recognitionModel)
        {
            Console.WriteLine("Move into the \"input\" folder the files that you want to use.");
            Console.WriteLine("Accepted formats are .jpeg and .mp4");
            Console.WriteLine();

            string userInput;

            while (true)
            {
                Console.WriteLine("Choose if you want to compare images or a video :");
                Console.WriteLine("Accepted input -> 1 , 2 or 3");
                Console.WriteLine("1. images        2. video        3. exit");
                Console.WriteLine();

                userInput = Console.ReadLine();
                if (userInput == "1")
                {
                    _sourceImageFileStream = SelectImageToFind();
                    InitFindSimilarImages();
                    FindSimilar(client, GetTargetStreamList(), recognitionModel).Wait();
                }
                if (userInput == "2")
                {
                    _sourceImageFileStream = SelectImageToFind();
                    SplitVideo();
                    FindSimilar(client, GetTargetStreamList(), recognitionModel).Wait();
                }
                if (userInput == "3")
                {
                    Console.WriteLine("Exiting");
                    return;
                }
            }
        }

        /*
         * MENU
         * if user select Images in menu selection, InitFindSimilarImages move frames in temp file and lunch detection
         */
        private static void InitFindSimilarImages()
        {
            string[] files = Directory.GetFiles(inputDir);

            foreach (string file in files)
            {
                if (!File.Exists(tempDir + "/" + file.Substring(inputDir.Length + 1)))
                {
                    File.Copy(file, tempDir + "/" + file.Substring(inputDir.Length + 1));
                    Console.WriteLine(file.Substring(inputDir.Length + 1) + " copied in the temp folder.");
                }
                _targetImageFileNames.Add(tempDir + "/" + file.Substring(inputDir.Length + 1));

            }
            Console.WriteLine();
            Thread.Sleep(2000);
        }


        static void Main(string[] args)
        {
            // Face subscription in the Azure portal, get your subscription key and endpoint.
            // Set your environment variables using the names below. Close and reopen your project for changes to take effect.
            string SUBSCRIPTION_KEY = Environment.GetEnvironmentVariable("FACE_SUBSCRIPTION_KEY");
            string ENDPOINT = Environment.GetEnvironmentVariable("FACE_ENDPOINT");

            // Authenticate.
            IFaceClient client = Authenticate(ENDPOINT, SUBSCRIPTION_KEY);

            // Used in the Detect Faces and Verify examples.
            // Recognition model 2 is used for more precision extraction, use 1 to simply recognize/detect a face.
            // However, the API calls to Detection that are used with Verify, Find Similar, or Identify must share the same recognition model.
            const string RECOGNITION_MODEL2 = RecognitionModel.Recognition02;
            const string RECOGNITION_MODEL1 = RecognitionModel.Recognition01;

            // Initialize the program env
            Initialize();

            Menu(client, RECOGNITION_MODEL2);
        }

    }
}
