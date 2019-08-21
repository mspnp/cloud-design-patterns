using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using PackageService.Models;
using System.Net;
using Microsoft.Extensions.Logging;


namespace PackageService.Services
{
    public class PackageRepository : IPackageRepository
    {
        private readonly IDocumentClient _client;
        private readonly ILogger<PackageRepository> _logger;

        public PackageRepository(IDocumentClient client, ILogger<PackageRepository> logger)
        {
            this._client = client;
            this._logger = logger;
        }

        // Gets a package by id from the list.
        public async Task<Package> GetPackageAsync(string packageid)
        {
            try
            {
                Document document = await _client.ReadDocumentAsync<Document>(
                    UriFactory.CreateDocumentUri(DocumentConfig.DatabaseId, DocumentConfig.CollectionId, packageid),
                    new RequestOptions { PartitionKey = new PartitionKey(packageid) });

                return document.ToPackage();
            }
            catch (DocumentClientException ex)
            {
                _logger.LogError(ex, "Exception");

                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw new DBException("Could not get the package.", ex);

                }
            }

        }


        //Update the specified package 

        public async Task<Package> UpdatePackageAsync(string packageid, Package package)
        {
            // Find the package based on PackageId.
            Package packageToUpdate = await GetPackageAsync(packageid);

            if (packageToUpdate == null)
            {
                _logger.LogInformation("Package {Id} was not found", packageid);

                return null;
            }

            // Compare the existing package to incoming package
            // If the values are different, update the packageToUpdate with the incoming values.

            if (string.Compare(package.Id, packageToUpdate.Id) != 0 && package.Id != null)
            {
                packageToUpdate.Tag = package.Tag;

                _logger.LogInformation("Package {Id} was not found", packageid);

            }

            if ((package.Size != packageToUpdate.Size) && (package.Size != PackageSize.Invalid))
            {
                packageToUpdate.Size = package.Size;

                _logger.LogInformation("Package size was updated to {Size}.", package.Size);

            }

            if ((package.Weight != packageToUpdate.Weight) && (package.Weight != 0))
            {
                packageToUpdate.Weight = package.Weight;

                _logger.LogInformation("Package weight was updated to {Weight}.", package.Weight);

            }

            if (string.Compare(package.Tag, packageToUpdate.Tag) != 0 && package.Tag != null)
            {
                packageToUpdate.Tag = package.Tag;

                _logger.LogInformation("Package tag was updated to {Tag}.", package.Tag);

            }

            try
            {
                Document updated = await _client.ReplaceDocumentAsync(
                    UriFactory.CreateDocumentUri(DocumentConfig.DatabaseId, DocumentConfig.CollectionId, packageid),
                    packageToUpdate,
                    new RequestOptions { PartitionKey = new PartitionKey(packageid) });

                return updated.ToPackage();
            }
            catch (DocumentClientException ex)
            {
                _logger.LogError(ex, "Exception.");

                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw new EventException("Could not update the package.", ex);
                }
            }
        }


        public async Task<PackageUpsertStatusCode> AddPackageAsync(Package package)
        {
            try
            {
                ResourceResponseBase response = await _client.UpsertDocumentAsync(
                        UriFactory.CreateDocumentCollectionUri(DocumentConfig.DatabaseId, DocumentConfig.CollectionId),
                        package.ToDocument(),
                        new RequestOptions { PartitionKey = new PartitionKey(package.Id) });

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:

                        _logger.LogInformation("Package {Id} is updated.", package.Id);
                        return PackageUpsertStatusCode.Updated;

                    case HttpStatusCode.Created:
                        _logger.LogInformation("Package {Id} is created.", package.Id);
                        return PackageUpsertStatusCode.Created;

                    default:
                        _logger.LogError("Package {Id} is invalid.", package.Id);
                        return PackageUpsertStatusCode.Invalid;
                }
            }
            catch (DocumentClientException ex)
            {
                _logger.LogError(ex, "Error in Upsert operation.");

                throw new EventException("Could not add the package", ex);
            }
        }
    }
}