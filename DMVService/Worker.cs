using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.Runtime;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Security.AccessControl;

namespace DMVService

//Filepath for DMV XML: "C:\Users\Morpheus\Documents\BC\Cloud\Project3\Project3\DMVDatabase.xml"
//Upward Queue URL: https://sqs.us-east-1.amazonaws.com/331002970834/P3UpwardQueue 
//Input: Plate Number
//Output: Vehicle and Owner info
//Ideas: put plate numbers in a downward queue for the service to read 
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        private const string logPath = @"C:\Users\Morpheus\Desktop\servicelog.txt";

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            WriteToLog("Task has started" + DateTime.Now);
            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            //here: do anything you want to do when service stops
            WriteToLog("the task has ended: " + DateTime.Now);
            await base.StopAsync(cancellationToken);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //security credentials
            string accessKey = "ASIAU2EKQQLJMTBM4BIG";
            string secretKey = "7pClJGm0i87aVr/Gn9GTfFWO6cs9MSluV4oGDt2L";
            string sessionToken = "FwoGZXIvYXdzECoaDB3xpL6s0p9btsKSLyLNAb9ZBtBp4/99DY3MoY+rZ5B89SOwZg6f/yiDx1CByy5sjd1R0a2rcb/+cF64ZWdzgCnjiQ9o1BiBtKgG+ONgtt+N8QW1AAF2k5w9eubaFoJN8v6aB8ODUnFgZpqNb+vs25Bnlyvx7IOs6JNjgr4/vA1bX1XdAUOfEQImeFuOKdlITOGLkmFyO9N48ca9d5MI8z7lv3jHgEwlp8ukvAoK4+oihRKXM4c6f4ApItRbYZxB5J1OyR7+Dp/ygxn9X5JQ6qJo0UwytjGPV/h50fgolve6pAYyLR8uuixicjzvahcEWwktWXwuUdAPgNj92g7bedgQcoKKW17O5PU9qGMlqG+CIA==";
            SessionAWSCredentials credentials = new SessionAWSCredentials(accessKey, secretKey, sessionToken);

            //debug
            WriteToLog("credentials created...");

            AmazonSQSClient sqsClient = new AmazonSQSClient(credentials, Amazon.RegionEndpoint.USEast1);
            //downward queue
            string downwardQURL = "https://sqs.us-east-1.amazonaws.com/331002970834/P3DownwardQueue";

            //debug
            WriteToLog("sqsClient created...");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                //debug
                WriteToLog("before response...");

                //receive and delete message from downward queue
                ReceiveMessageResponse response = await ReceiveAndDeleteMessage(sqsClient, downwardQURL);

                string messageId = "";
                string body = "";

                if (response.Messages.Count > 0)
                {
                    var message = response.Messages[0];
                    messageId = message.MessageId;
                    body = message.Body;

                    WriteToLog("Received message:");
                    WriteToLog("Message ID: " + messageId);
                    WriteToLog("Body: " + body);
                }

                /*
                 {"plate_number": "6TRJ244", "Type": "no_stop", "DateTime": "6/16/2023 4:45:44 PM",
                "Location": "Main St and 116th Ave intersection - Bellevue"}
                 */


                //Message Read from Downward queue
                WriteToLog("Date: " + DateTime.Now.ToString() + "Read Message: " + body);

                //DMV XML db
                string dbFilePath = "C:\\Users\\Morpheus\\Documents\\BC\\Cloud\\Project3\\Project3\\DMVDatabase.xml";

                //extract values from downward Q json
                //plate number is plate_number value in json message body
                //grab other s3 tags: Type, DateTime
                if (body != "")
                {
                    JObject jsonFromDownwardQ = JObject.Parse(body);
                    string plateNumber = (string)jsonFromDownwardQ["plate_number"];
                    string type = (string)jsonFromDownwardQ["Type"];
                    string dateTime = (string)jsonFromDownwardQ["DateTime"];
                    string location = (string)jsonFromDownwardQ["Location"];

                    //service log debug
                    WriteToLog("plate number: " + plateNumber);
                    WriteToLog("type: " + type);
                    WriteToLog("dateTime: " + dateTime);
                    WriteToLog("location: " + location);

                    //grab owner and vehicle info, must add s3 tags before sending back
                    string info = QueryDMVDatabase(dbFilePath, plateNumber);

                    //upward queue 
                    string upwardQURL = "https://sqs.us-east-1.amazonaws.com/331002970834/P3UpwardQueue";


                    //add s3 tags to info before sending to upward Q
                    //convert info to json
                    JObject infoWithTags = JObject.Parse(info);
                    infoWithTags.Add("Type", type);
                    infoWithTags.Add("DateTime", dateTime);
                    infoWithTags.Add("Location", location);

                    //convert back to string
                    info = infoWithTags.ToString();

                    //send info to upward queue
                    SendMessageToQueue(sqsClient, upwardQURL, info);

                    //debug
                    WriteToLog("Date: " + DateTime.Now.ToString() + "Posted message to Upward Queue: " + info);
                }
                
                await Task.Delay(1000, stoppingToken);
            }
        }
        
        //Query DMV database
        //xmlFilePath = DMV XML file path
        public string QueryDMVDatabase(string xmlFilePath, string plateNumber)
        {
            string response = string.Empty;

            try
            {
                // Load the XML file
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlFilePath);

                // Find the vehicle element with the specified plate number
                XmlNode vehicleNode = xmlDoc.SelectSingleNode($"//vehicle[@plate='{plateNumber}']");

                if (vehicleNode != null)
                {
                    // The vehicle exists in the database
                    string make = string.Empty;
                    string model = string.Empty;
                    string color = string.Empty;
                    string ownerName = string.Empty;
                    string contact = string.Empty;

                    // Extract vehicle information
                    make = vehicleNode.SelectSingleNode("make").InnerText;
                    model = vehicleNode.SelectSingleNode("model").InnerText;
                    color = vehicleNode.SelectSingleNode("color").InnerText;

                    // Extract owner information
                    XmlNode ownerNode = vehicleNode.SelectSingleNode("owner");
                    ownerName = ownerNode.SelectSingleNode("name").InnerText;
                    contact = ownerNode.SelectSingleNode("contact").InnerText;

                    //
                    response = GenerateResponseJson(plateNumber, make, model, color, ownerName, contact);
                }
                else
                {
                    // The vehicle does not exist in the database
                    response = GenerateErrorResponseJson(plateNumber, "Vehicle not found.");
                }
            }
            catch (Exception ex)
            {
                // Error occurred while querying the database
                response = GenerateErrorResponseJson(plateNumber, ex.Message);
            }

            return response;
        }

        //generates the json object with vehicle and owner info
        public string GenerateResponseJson(string plateNumber, string make, string model, string color, string ownerName, string contact)
        {
            return string.Format("{{ \"plateNumber\": \"{0}\", \"make\": \"{1}\", \"model\": \"{2}\", \"color\": \"{3}\", \"ownerName\": \"{4}\", \"contact\": \"{5}\" }}",
                plateNumber, make, model, color, ownerName, contact);
        }

        public string GenerateErrorResponseJson(string plateNumber, string errorMessage)
        {
            return string.Format("{{ \"plateNumber\": \"{0}\", \"error\": \"{1}\" }}", plateNumber, errorMessage);
        }


        //Receive and Delete message from Downward Queue
        public static async Task<ReceiveMessageResponse> ReceiveAndDeleteMessage(IAmazonSQS client, string queueUrl)
        {
            //debug
            WriteToLog("inside ReceiveAndDeleteMessage");
            
            // Receive a single message from the queue.
            var receiveMessageRequest = new ReceiveMessageRequest
            {
                AttributeNames = { "SentTimestamp" },
                MaxNumberOfMessages = 1,
                MessageAttributeNames = { "All" },
                QueueUrl = queueUrl,
                VisibilityTimeout = 0,
                WaitTimeSeconds = 20,
            };

            var receiveMessageResponse = await client.ReceiveMessageAsync(receiveMessageRequest);

            if (receiveMessageResponse.Messages.Count > 0)
            {
                // Delete the received message from the queue.
                var deleteMessageRequest = new DeleteMessageRequest
                {
                    QueueUrl = queueUrl,
                    ReceiptHandle = receiveMessageResponse.Messages[0].ReceiptHandle,
                };

                await client.DeleteMessageAsync(deleteMessageRequest);
            }

            return receiveMessageResponse;
        }

        //send json info message to upward queue
        public static async Task SendMessageToQueue(IAmazonSQS client, string queueUrl, string jsonMessage)
        {
            var sendMessageRequest = new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = jsonMessage
            };

            await client.SendMessageAsync(sendMessageRequest);
        }

        public static void WriteToLog(string message)
        {
            string text = String.Format("{0}:\t{1}", DateTime.Now, message);
            using (StreamWriter writer = new StreamWriter(logPath, append: true))
            {
                writer.WriteLine(text);
            }
        }
    }
}