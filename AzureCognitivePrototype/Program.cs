// See https://aka.ms/new-console-template for more information
using AzureCognitivePrototype;

Console.WriteLine("Hello, World!");

string text = "Newest text";
string language = "en-US";
string voiceName = "en-US-GuyNeural";
var blobName = "sample_audio.mp3";





//var ssml = AzureSpeechLibrary.GenerateSSML(text, "+0st", "0.8", "loud", voiceName);

//var audioDataStream = await AzureSpeechLibrary.GenerateSpeechAsync(ssml, language, voiceName);
//using var audioDataStreamWrapper = new AudioDataStreamWrapper(audioDataStream);
//var uploadedUrl = await AzureSpeechLibrary.UploadToAzureBlobAsync(audioDataStreamWrapper, blobName);


//var voices = await AzureSpeechLibrary.GetAvailableVoicesAsync();
//var languages = await AzureSpeechLibrary.GetAvailableLanguagesAsync();


//Console.WriteLine("Uploaded audio URL: " + uploadedUrl);