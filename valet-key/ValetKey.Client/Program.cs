// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.===
namespace ValetKey.Client
{
    using Azure.Storage.Blobs;
    using System;
    using System.Configuration;
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

            // Make sure the endpoint matches with the web role's endpoint.
            var tokenServiceEndpoint = ConfigurationManager.AppSettings["serviceEndpointUrl"];

            try
            {
                var blobSas = GetBlobSas(new Uri(tokenServiceEndpoint)).Result;
                UriBuilder sasUri = new UriBuilder(blobSas.BlobUri);
                sasUri.Query = blobSas.Credentials;

                var blob = new BlobClient(sasUri.Uri);

                using (var stream = GetFileToUpload(10))
                {
                    blob.Upload(stream);
                }

                Console.WriteLine("Blob uploaded successful: {0}", blob.Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
            Console.WriteLine();
            Console.WriteLine("Done. Press any key to exit...");
            Console.ReadKey();
        }

        private static async Task<StorageEntitySas> GetBlobSas(Uri blobUri)
        {
            var request = HttpWebRequest.Create(blobUri);
            var response = await request.GetResponseAsync();
            var responseString = string.Empty;

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
