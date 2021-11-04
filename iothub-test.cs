using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Azure.Identity;
using Azure.Core;
using Microsoft.Azure.Devices;
using System;
using System.Text;

namespace eugene.Function
{
    public static class iothub_test
    {
        [FunctionName("iothub_test")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            // 7f60f9a3-f9fc-42a3-8329-c1f00c77c013
            TokenCredential tokenCredential = new DefaultAzureCredential(); 
            
            using var serviceClient = ServiceClient.Create("iotstarter-iothub.azure-devices.net", tokenCredential, TransportType.Amqp);
            log.LogInformation("Connecting using token credential.");
            await serviceClient.OpenAsync();
            log.LogInformation("Successfully opened connection.");

            log.LogInformation("Sending a cloud-to-device message.");
            using var message = new Message(Encoding.ASCII.GetBytes("Hello, Cloud!"));
            await serviceClient.SendAsync("edgeDevicedev", message);
            log.LogInformation("Successfully sent message.");            

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }
}

