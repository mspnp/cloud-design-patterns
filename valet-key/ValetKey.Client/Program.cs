namespace ValetKey.Client
{
    using Azure;
    using Azure.Storage.Blobs;
    using Microsoft.Extensions.Configuration;
    using System.Net.Http.Json;

    public class Program
    {
        private static readonly HttpClient httpClient = new();

        public static async Task Main()
        {
            Console.WriteLine("Press any key to run the sample...");
            Console.ReadKey();

            var configuration = new ConfigurationBuilder()
                                    .AddJsonFile("appsettings.json", false, false).Build();

            // Get the valet key endpoint from config
            var tokenServiceEndpoint = configuration.GetSection("AppSettings:ServiceEndpointUrl").Value;
            if (string.IsNullOrWhiteSpace(tokenServiceEndpoint)) throw new InvalidOperationException("Configure AppSettings:ServiceEndpointUrl and run again.");

            // Get the valet key
            var blobSas = await GetBlobSasAsync(new Uri(tokenServiceEndpoint));

            var sasUri = new UriBuilder(blobSas!.BlobUri!)
            {
                Query = blobSas.Signature
            };

            var blob = new BlobClient(sasUri.Uri);
            try
            {
                using (var stream = await GetFileToUploadAsync(10))
                {
                    await blob.UploadAsync(stream);
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

        private static async Task<StorageEntitySas?> GetBlobSasAsync(Uri tokenUri)
        {
            return await httpClient.GetFromJsonAsync<StorageEntitySas>(tokenUri);
        }

        /// <summary>
        /// Create a sample file containing random bytes of data
        /// </summary>
        private static async Task<MemoryStream> GetFileToUploadAsync(int sizeMb)
        {
            var stream = new MemoryStream();

            var rnd = new Random();
            var buffer = new byte[1024 * 1024];

            for (int i = 0; i < sizeMb; i++)
            {
                rnd.NextBytes(buffer);
                await stream.WriteAsync(buffer.AsMemory(0, buffer.Length));
            }

            stream.Position = 0;

            return stream;
        }

        public class StorageEntitySas
        {
            public Uri? BlobUri { get; set; }
            public string? Signature { get; set; }
        }
    }
}