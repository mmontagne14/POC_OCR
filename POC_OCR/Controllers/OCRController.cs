using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Net.Sockets;
using System.Text;


namespace POC_OCR.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OCRController : ControllerBase
    {
        private static ComputerVisionClient cvClient;
        [HttpPost(Name = "ReadOCR")]
        public async Task<ReadOCRResponse> ReadOCR()
        {
            var response = new ReadOCRResponse() { Ok = true };
            string imageFile = "Images/estudio1.jpeg";
            StringBuilder sb = new StringBuilder();
            // Authenticate Computer Vision client

            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json"); 
            IConfigurationRoot configuration = builder.Build(); string cogSvcEndpoint = configuration["CognitiveServicesEndpoint"]; 
            string cogSvcKey = configuration["CognitiveServiceKey"];

            ApiKeyServiceClientCredentials credentials = new ApiKeyServiceClientCredentials(cogSvcKey);
            cvClient = new ComputerVisionClient(credentials)
            {
                Endpoint = cogSvcEndpoint
            };

            

            using (var imageData = System.IO.File.OpenRead(imageFile))
            {
                var readOp = await cvClient.ReadInStreamAsync(imageData);

                // Get the async operation ID so we can check for the results
                string operationLocation = readOp.OperationLocation;
                string operationId = operationLocation.Substring(operationLocation.Length - 36);

                // Wait for the asynchronous operation to complete
                ReadOperationResult results;
                do
                {
                    Thread.Sleep(1000);
                    results = await cvClient.GetReadResultAsync(Guid.Parse(operationId));
                }
                while ((results.Status == OperationStatusCodes.Running ||
                        results.Status == OperationStatusCodes.NotStarted));

                // If the operation was successfuly, process the text line by line
                if (results.Status == OperationStatusCodes.Succeeded)
                {
                    var textUrlFileResults = results.AnalyzeResult.ReadResults;
                    foreach (ReadResult page in textUrlFileResults)
                    {
                        foreach (Line line in page.Lines)
                        {
                            //Console.WriteLine(line.Text);
                            sb.AppendLine(line.Text + "\r");   
                        }
                    }
                    response.Results = sb.ToString();
                }
            }

            
            return response;
        }
    }


}
