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
            // Initialize variables
            CloudStorageAccount storageAccount = null;
            CloudBlobContainer cloudBlobContainer = null;
            string sourceFile = null;

            // Store message in blob storage
            string storageConnectionString = ConfigurationManager.AppSettings["STORAGE_CONNECTION_STRING"];
            
            // Check whether the connection string can be parsed.
            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                // Create the CloudBlobClient that represents the Blob storage endpoint for the storage account.
                CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

                // Create a container called 'heavypayload' and append a GUID value to it to make the name unique. 
                var containerName = "heavypayload" + Guid.NewGuid().ToString();
                Console.WriteLine("- Creating container '{0}'", containerName);
                cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
                await cloudBlobContainer.CreateAsync();

                // Set the permissions so the blobs are public. 
                BlobContainerPermissions permissions = new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                };
                await cloudBlobContainer.SetPermissionsAsync(permissions);

                // This would ideally be a large message/payload/file
                // Create a file in your local MyDocuments folder to upload to a blob.
                string localPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string localFileName = "HeavyPayload_" + Guid.NewGuid().ToString() + ".txt";
                sourceFile = Path.Combine(localPath, localFileName);
                // Write contents to file
                File.WriteAllText(sourceFile, "Hello, World! This is a huge file");

                Console.WriteLine("- Created temp file = {0}", sourceFile);
                Console.WriteLine("- Uploading to Blob storage as blob '{0}'", localFileName);

                // Get a reference to the blob address, then upload the file to the blob.
                // Use the value of localFileName for the blob name.
                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(localFileName);
                await cloudBlockBlob.UploadFromFileAsync(sourceFile);

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
