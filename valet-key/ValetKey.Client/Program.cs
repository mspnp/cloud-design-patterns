namespace ValetKey.Client
{
    using Azure;
    using Azure.Storage.Blobs;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.IO;
    using System.Net;
    using System.Runtime.Serialization.Json;
    using System.Threading.Tasks;

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Press any key to run sample...");
            Console.ReadKey();

            IConfiguration configuration = new ConfigurationBuilder()
                                    .AddJsonFile("appsettings.json").Build();

            // Make sure the endpoint matches with the web apis's endpoint.
            var tokenServiceEndpoint = configuration.GetSection("AppSettings:ServiceEndpointUrl").Value;

            var blobSas = GetBlobSas(new Uri(tokenServiceEndpoint)).Result;
            UriBuilder sasUri = new UriBuilder(blobSas.BlobUri);
            sasUri.Query = blobSas.Credentials;

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
                    Console.WriteLine("Read operation failed for SAS {0}", sasUri);
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
            var request = HttpWebRequest.Create(blobUri);
            var response = await request.GetResponseAsync();

            var serializer = new DataContractJsonSerializer(typeof(StorageEntitySas));
            var blobSas = (StorageEntitySas)serializer.ReadObject(response.GetResponseStream());

            return blobSas;
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

        public struct StorageEntitySas
        {
            public string Credentials;
            public Uri BlobUri;
        }
    }
}
