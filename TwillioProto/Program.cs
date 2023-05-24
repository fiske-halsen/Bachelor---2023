using EMSuite.Common.PhoneNotification;
using EMSuite.DataAccess;
using EMSuite.PhoneNotification.Models;
using Microsoft.CognitiveServices.Speech.Transcription;
using PhoneNotificationService.Tests.TestServer;
using TwillioProto;

// Replace with your Twilio Account SID and Auth Token
//string accountSid = "xxx";
//string authToken = "xxx";

//TwilioClient.Init(accountSid, authToken);

//string mp3Url = "https://satexttospeech.blob.core.windows.net/audiofilecontainer/TilTester.mp3";
//string encodedMp3Url = Uri.EscapeDataString(mp3Url);

//var twimlUrl = $"xxx";
//var call = CallResource.Create(
//url: new Uri(twimlUrl),
//from: new PhoneNumber("+4552512101"),
//to: new PhoneNumber("+4553780038")
//);

//Console.WriteLine(call.Sid);

//Console.WriteLine(await TranslatationLib.TranslateText("", "fr", "en"));

//SqlDataAccess _dataAccess = new SqlDataAccess("Server=localhost;Database=EMSuite;Trusted_Connection=True;MultipleActiveResultSets=true");

////Repository service
//INotificationRepository nr = new NotificationRepository(_dataAccess);

//await nr.InsertNoticationLog(
//                          new NotificationLog
//                          {
//                              UserId = "9dd85f68-e051-4e5d-9bcc-8fcc4f745979",
//                              BatchAlarmId = 1,
//                              CallId = "sadasdsa",
//                              AlarmMessage = "ohhh yah",
//                              GenderId = 1,
//                              RoundRobinInterval = 60000,
//                              PhoneCallTimeStamp = DateTime.Now,
//                              AzureBlobUrl = "https://satexttospeech.blob.core.windows.net/audiofilecontainer/Phillip.mp3"
//                          });



//await nr.UpdateNotificationlog("sadasdsa", "failed");

var testserver = new TestSignalRServer("http://localhost:5000");


Console.ReadKey();