using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PushSharp.Core;
using PushSharp.Google;
using PushSharp.Apple;

namespace PushNotifications
{
    class Program
    {
        public static string MarkS8 = "cGYJ24ElFHI:APA91bFfGQnHQqlVGuKuDW89enx6pV_1CTR0smhConeacGnKSvD_dumkxZW5vDQ3Ax3TxaWbcy3Kgjt_xdLJtXhCEYyCG2ubmPOyvO8mJ4cqmojQvYRmlGREz_pAuo0F3CycWGcXHSid";
        public static string BenIphone6 = "839841809466B7AD1D8FDAD6608BB614595051FFC7B138A5F5503F7FBDC34117";
        public static string PushCertificate = "C:\\GIT\\PushNotifications\\PushNotifications\\PushCredentials\\Apple\\ios_development.p12";
        public static string PushCertificatePassword = File.ReadAllText("C:\\GIT\\PushNotifications\\PushNotifications\\PushCredentials\\Apple\\ios_development_password.txt");



        static void Main(string[] args)
        {
            bool CloseApp = false;

            Console.WriteLine("Please Select An Option To Continue");
            Console.WriteLine(" ");
            Console.WriteLine("1. Send Android Push Notification");
            Console.WriteLine("2. Send Apple Push Notificaiton");
            Console.WriteLine(" ");
            Console.WriteLine("3. Exit The Application");


            while (CloseApp == false)
            {
                var input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        {
                            //Payload BasicPayload = new Payload { actionType = "none" };
                            Message MessagePayload = new Message { actionType = "meassage", message = "This is a message" };
                            //Warning WarningPayload = new Warning { actionType = "warning", vehicleId = "M2lQ" };
                            //Deal DealPayload = new Deal { actionType = "deal" };
                            //Url URLPayload = new Url { actionType = "url" , url = "http://smartdriverclub.co.uk" };
                            PushNotification StandardNotification = new PushNotification { title = "New Notification", body = "Test Notification", payload = MessagePayload };


                            List<string> AndroidPushTokens = new List<string>();
                            AndroidPushTokens.Add(MarkS8);


                            SendAndroidPushNotification(AndroidPushTokens, StandardNotification);

                            Console.WriteLine("Android Push Sent");
                            input = null;
                            break;
                        }
                    case "2":
                        {
                            PushNotification StandardNotification = new PushNotification { title = "New Notification", body = "This is a test" };

                            List<string> ApplePushTokens = new List<string>();
                            ApplePushTokens.Add(BenIphone6);

                            SendApplePushNotification(ApplePushTokens, StandardNotification);

                            Console.WriteLine("Apple Push Sent");
                            input = null;
                            break;
                        }
                    case "3":
                        {
                            CloseApp = true;
                            input = null;
                            break;
                        }
                    default:
                        {
                            Console.WriteLine("Incorrect Selection");
                            Console.WriteLine(" ");
                            Console.WriteLine("1. Send Android Push Notification");
                            Console.WriteLine("2. Send Apple Push Notificaiton");
                            Console.WriteLine(" ");
                            Console.WriteLine("3. Exit The Application");
                            input = null;
                            break;
                        }
                }
            }
        }

        private static void SendAndroidPushNotification(List<string> AndroidPushTokens, PushNotification Payload)
        {
            // Configuration
            var config = new GcmConfiguration("GCM-SENDER-ID", "AIzaSyByUHzXZY1lWQU34ssv3a9R3BSxJJALkqk", null);

            // Create a new broker
            var gcmBroker = new GcmServiceBroker(config);

            // Wire up events
            gcmBroker.OnNotificationFailed += (notification, aggregateEx) =>
            {

                aggregateEx.Handle(ex =>
                {

                    // See what kind of exception it was to further diagnose
                    if (ex is GcmNotificationException)
                    {
                        var notificationException = (GcmNotificationException)ex;

                        // Deal with the failed notification
                        var gcmNotification = notificationException.Notification;
                        var description = notificationException.Description;

                        Console.WriteLine($"GCM Notification Failed: ID={gcmNotification.MessageId}, Desc={description}");
                    }
                    else if (ex is GcmMulticastResultException)
                    {
                        var multicastException = (GcmMulticastResultException)ex;

                        foreach (var succeededNotification in multicastException.Succeeded)
                        {
                            Console.WriteLine($"GCM Notification Succeeded: ID={succeededNotification.MessageId}");
                        }

                        foreach (var failedKvp in multicastException.Failed)
                        {
                            var n = failedKvp.Key;
                            var e = failedKvp.Value;

                            Console.WriteLine($"GCM Notification Failed: ID={n.MessageId}, Desc={e.Message}");
                        }

                    }
                    else if (ex is DeviceSubscriptionExpiredException)
                    {
                        var expiredException = (DeviceSubscriptionExpiredException)ex;

                        var oldId = expiredException.OldSubscriptionId;
                        var newId = expiredException.NewSubscriptionId;

                        Console.WriteLine($"Device RegistrationId Expired: {oldId}");

                        if (!string.IsNullOrWhiteSpace(newId))
                        {
                            // If this value isn't null, our subscription changed and we should update our database
                            Console.WriteLine($"Device RegistrationId Changed To: {newId}");
                        }
                    }
                    else if (ex is RetryAfterException)
                    {
                        var retryException = (RetryAfterException)ex;
                        // If you get rate limited, you should stop sending messages until after the RetryAfterUtc date
                        Console.WriteLine($"GCM Rate Limited, don't send more until after {retryException.RetryAfterUtc}");
                    }
                    else
                    {
                        Console.WriteLine("GCM Notification Failed for some unknown reason");
                    }

                    // Mark it as handled
                    return true;
                });
            };

            gcmBroker.OnNotificationSucceeded += (notification) =>
            {
                Console.WriteLine("GCM Notification Sent!");
            };

            // Start the broker
            gcmBroker.Start();

            foreach (var PushToken in AndroidPushTokens)
            {
                // Queue a notification to send
                gcmBroker.QueueNotification(new GcmNotification
                {
                    RegistrationIds = new List<string> {
                     PushToken
                    },
                    Data = JObject.Parse(JsonConvert.SerializeObject(Payload))
                });
            }

            // Stop the broker, wait for it to finish   
            // This isn't done after every message, but after you're
            // done with the broker
            gcmBroker.Stop();
        }

        private static void SendApplePushNotification(List<string> ApplePushTokens, PushNotification Payload)
        {
            // Configuration (NOTE: .pfx can also be used here)
            var config = new ApnsConfiguration(ApnsConfiguration.ApnsServerEnvironment.Production,
                PushCertificate, PushCertificatePassword);

            // Create a new broker
            var apnsBroker = new ApnsServiceBroker(config);

            // Wire up events
            apnsBroker.OnNotificationFailed += (notification, aggregateEx) =>
            {

                aggregateEx.Handle(ex =>
                {

                    // See what kind of exception it was to further diagnose
                    if (ex is ApnsNotificationException)
                    {
                        var notificationException = (ApnsNotificationException)ex;

                        // Deal with the failed notification
                        var apnsNotification = notificationException.Notification;
                        var statusCode = notificationException.ErrorStatusCode;

                        Console.WriteLine($"Apple Notification Failed: ID={apnsNotification.Identifier}, Code={statusCode}");

                    }
                    else
                    {
                        // Inner exception might hold more useful information like an ApnsConnectionException			
                        Console.WriteLine($"Apple Notification Failed for some unknown reason : {ex.InnerException}");
                    }

                    // Mark it as handled
                    return true;
                });
            };

            apnsBroker.OnNotificationSucceeded += (notification) =>
            {
                Console.WriteLine("Apple Notification Sent!");
            };

            // Start the broker
            apnsBroker.Start();

            foreach (var PushToken in ApplePushTokens)
            {
                // Queue a notification to send
                apnsBroker.QueueNotification(new ApnsNotification
                {
                    DeviceToken = PushToken,
                    Payload = JObject.Parse(JsonConvert.SerializeObject(Payload))
                });
            }

            // Stop the broker, wait for it to finish   
            // This isn't done after every message, but after you're
            // done with the broker
            apnsBroker.Stop();
        }

        public class PushNotification 
        {
            public string title { get; set; }
            public string body { get; set; }
            public Payload payload { get; set; }
            
        }

        public class Payload
        {
            public string actionType { get; set; }
        }

        public class Message : Payload
        {
            public string message { get; set; }
        }

        public class Warning : Payload
        {
            public string vehicleId { get; set; }
        }

        public class Deal : Payload
        {
            
        }

        public class Url : Payload
        {
            public string url { get; set; }
        }

    }
}
