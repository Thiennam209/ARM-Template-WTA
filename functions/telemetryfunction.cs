using Azure;
using Azure.Core.Pipeline;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Collections.Generic;

namespace My.Function
{
    // This class processes telemetry events from IoT Hub, reads temperature of a device
    // and sets the "Temperature" property of the device with the value of the telemetry.
    public class telemetryfunction
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static string adtServiceUrl = Environment.GetEnvironmentVariable("ADT_SERVICE_URL");

        [FunctionName("telemetryfunction")]
        public async void Run([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        {
            try
            {
                // After this is deployed, you need to turn the Managed Identity Status to "On",
                // Grab Object Id of the function and assigned "Azure Digital Twins Owner (Preview)" role
                // to this function identity in order for this function to be authorized on ADT APIs.
                //Authenticate with Digital Twins
                var credentials = new DefaultAzureCredential();
                log.LogInformation(credentials.ToString());
                DigitalTwinsClient client = new DigitalTwinsClient(
                    new Uri(adtServiceUrl), credentials, new DigitalTwinsClientOptions
                    { Transport = new HttpClientTransport(httpClient) });
                log.LogInformation($"ADT service client connection created.");
                
                if (eventGridEvent != null && eventGridEvent.Data != null)
                {
                    JObject deviceMessage = (JObject)JsonConvert.DeserializeObject(eventGridEvent.Data.ToString());
                    string deviceId = (string)deviceMessage["systemProperties"]["iothub-connection-device-id"];
                    var ID = deviceMessage["body"]["autoid"];
                    var TimeInterval = deviceMessage["body"]["TimeInterval"];
                    var MaxReverseSpeed = deviceMessage["body"]["MaxReverseSpeed"];
                    var AccelerationMultiplier = deviceMessage["body"]["AccelerationMultiplier"];
                    var DecelerationMultiplier = deviceMessage["body"]["DecelerationMultiplier"];
                    var BrakeForce = deviceMessage["body"]["BrakeForce"];
                    var MaxSteeringAngle = deviceMessage["body"]["MaxSteeringAngle"];
                    var SteeringSpeed = deviceMessage["body"]["SteeringSpeed"];
                    var isCrash = deviceMessage["body"]["isCrash"];

                    log.LogInformation($"Device:{deviceId} Device Id is:{ID}");
                    log.LogInformation($"Device:{deviceId} Time interval is:{TimeInterval}");
                    log.LogInformation($"Device:{deviceId} MaxReverseSpeed is:{MaxReverseSpeed}");
                    log.LogInformation($"Device:{deviceId} AccelerationMultiplier is:{AccelerationMultiplier}");
                    log.LogInformation($"Device:{deviceId} DecelerationMultiplier is:{DecelerationMultiplier}");
                    log.LogInformation($"Device:{deviceId} BrakeForce is:{BrakeForce}");
                    log.LogInformation($"Device:{deviceId} MaxSteeringAngle:{MaxSteeringAngle}");
                    log.LogInformation($"Device:{deviceId} SteeringSpeed is:{SteeringSpeed}");
                    log.LogInformation($"Device:{deviceId} isCrash is:{isCrash}");
                    var updateProperty = new JsonPatchDocument();
                    var turbineTelemetry = new Dictionary<string, Object>()
                    {
                        ["autoid"] = ID,
                        ["TimeInterval"] = TimeInterval,
                        ["MaxReverseSpeed"] = MaxReverseSpeed,
                        ["AccelerationMultiplier"] = AccelerationMultiplier,
                        ["DecelerationMultiplier"] = DecelerationMultiplier,
                        ["BrakeForce"] = BrakeForce,
                        ["MaxSteeringAngle"] = MaxSteeringAngle,
                        ["SteeringSpeed"] = SteeringSpeed,
                        ["isCrash"] = isCrash
                    };
                    updateProperty.AppendAdd("/autoid", ID.Value<string>());

                    log.LogInformation(updateProperty.ToString());
                    try
                    {
                        await client.PublishTelemetryAsync(deviceId, Guid.NewGuid().ToString(), JsonConvert.SerializeObject(turbineTelemetry));
                    }
                    catch (Exception e)
                    {
                        log.LogInformation(e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                log.LogInformation(e.Message);
            }
        }
    }
}