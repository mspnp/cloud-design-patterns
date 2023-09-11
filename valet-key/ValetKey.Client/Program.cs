namespace ValetKey.Client
{
    using Azure;
    using Azure.Storage.Blobs;
    using Microsoft.Extensions.Configuration;
    using System.Net.Http.Json;

    public class Program
    {
        private static readonly HttpClient httpClient = new();

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Press any key to run the sample...");
            Console.ReadKey();

            var configuration = new ConfigurationBuilder()
                                    .AddJsonFile("appsettings.json", false, false).Build();

            // Make sure the endpoint matches with the web API's endpoint.
            var tokenServiceEndpoint = configuration.GetSection("AppSettings:ServiceEndpointUrl").Value;

            var blobSas = await GetBlobSas(new Uri(tokenServiceEndpoint));
            UriBuilder sasUri = new UriBuilder(blobSas.BlobUri);
            sasUri.Query = blobSas.Signature;

            var blob = new BlobClient(sasUri.Uri);
            try
            {
                using (var stream = GetFileToUpload(10))
                {
                    blob.Upload(stream);
                }

                Console.WriteLine("Blob uploaded successful: {0}", blob.Name);
            }
            catch (RequestFailedException e)
            {
                // Check for a 403 (Forbidden) error. If the SAS is invalid, 
                // Azure Storage returns this error.
                if (e.Status == 403)
                {
                    Console.WriteLine("Write operation failed for SAS {0}", sasUri);
                    Console.WriteLine("Additional error information: " + e.Message);
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
            }

            Console.WriteLine();
            Console.WriteLine("Done. Press any key to exit...");
            Console.ReadKey();
        }

        private static async Task<StorageEntitySas> GetBlobSas(Uri blobUri)
        {
            return await httpClient.GetFromJsonAsync<StorageEntitySas>(blobUri);
        }

        /// <summary>
        /// Create a sample file containing random bytes of data
        /// </summary>
        /// <param name="sizeMb"></param>
        /// <returns></returns>
        private static MemoryStream GetFileToUpload(int sizeMb)
        {
            var stream = new MemoryStream();

            var rnd = new Random();
            var buffer = new byte[1024 * 1024];

            for (int i = 0; i < sizeMb; i++)
            {
                rnd.NextBytes(buffer);
                stream.Write(buffer, 0, buffer.Length);
            }

            stream.Position = 0;

            return stream;
        }

        public class StorageEntitySas
        {
            public string? Signature { get; set; }
            public Uri? BlobUri { get; set; }
        }
    }
}