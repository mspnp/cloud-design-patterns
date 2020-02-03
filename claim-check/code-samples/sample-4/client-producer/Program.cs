using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ClientProducer
{
    class Program
    {
        public static async Task Main()
        {
            Console.WriteLine("Begin sample 4 for Claim Check pattern");
            Console.WriteLine();

            Console.WriteLine("Creating payload and uploading it to Azure Blob Storage...");
            var payloadDetails = await ProcessAsync();
            Console.WriteLine("Done.");
            Console.WriteLine();

            Console.WriteLine("Sending Claim Check message...");
            await SendNotification(payloadDetails);
            Console.WriteLine("Done.");
            Console.WriteLine();

            Console.WriteLine("Press any key to exit the sample application.");
            Console.ReadLine();
        }

        private static async Task SendNotification(PayloadDetails payloadDetails)
        {
            string brokerList = ConfigurationManager.AppSettings["EH_FQDN"];
            string connectionString = ConfigurationManager.AppSettings["EH_CONNECTION_STRING"];
            string topic = ConfigurationManager.AppSettings["EH_NAME"];
            string caCertLocation = ConfigurationManager.AppSettings["CA_CERT_LOCATION"];

            await Worker.Producer(brokerList, connectionString, topic, caCertLocation, payloadDetails);
        }

        private static async Task<PayloadDetails> ProcessAsync()
        {
            // Store message in blob storage
            string storageConnectionString = ConfigurationManager.AppSettings["STORAGE_CONNECTION_STRING"];
            
            // Check whether the connection string can be parsed.
            if (CloudStorageAccount.TryParse(storageConnectionString, out var storageAccount))
            {
                // Create the CloudBlobClient that represents the Blob storage endpoint for the storage account.
                CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

                // Create a container called 'heavypayload' if not exists;
                var containerName = "heavypayload";
                Console.WriteLine("- Checking for existence of container '{0}'", containerName);                
                var cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
                if (!await cloudBlobContainer.ExistsAsync())
                {
                    Console.WriteLine("- Creating container '{0}'", containerName);                
                    await cloudBlobContainer.CreateAsync();
                }                

                // Set the permissions so the blobs are private. 
                BlobContainerPermissions permissions = new BlobContainerPermissions() {
                    PublicAccess = BlobContainerPublicAccessType.Off
                };                
                Console.WriteLine("- Setting permissions");
                await cloudBlobContainer.SetPermissionsAsync(permissions);

                // This would ideally be a large message/payload/file
                // Create a file in your local MyDocuments folder to upload to a blob.
                string localPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string localFileName = "HeavyPayload_" + Guid.NewGuid().ToString() + ".txt";
                var sourceFile = Path.Combine(localPath, localFileName);
                
                // Write contents to file
                await File.WriteAllTextAsync(sourceFile, "Hello, World! This is a huge file");

                Console.WriteLine("- Created temp file = {0}", sourceFile);
                Console.WriteLine("- Uploading to Blob storage as blob '{0}'", localFileName);

                // Get a reference to the blob address, then upload the file to the blob.
                // Use the value of localFileName for the blob name.
                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(localFileName);
                await cloudBlockBlob.UploadFromFileAsync(sourceFile);

                // Delete tempo file
                Console.WriteLine("- Deleting temp file {0}", sourceFile);
                File.Delete(sourceFile);

                // Store details of blob to be sent as a message
                return new PayloadDetails
                {
                    ContainerName = cloudBlobContainer.Name,
                    BlobName = localFileName
                };
            }
            else
            {
                // Otherwise, let the user know that they need to define the environment variable.
                throw new ApplicationException(
                    "A connection string has not been defined in the system environment variables. " +
                    "Add a environment variable named 'storageconnectionstring' with your storage " +
                    "connection string as a value.");
            }
        }
    }
}
