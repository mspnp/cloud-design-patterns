
using System.Net;
using System.Threading.Tasks;
using PackageService.Models;


namespace PackageService.Services
{
    public interface IPackageRepository
    {
        Task<Package> GetPackageAsync(string id);
        Task<PackageUpsertStatusCode> AddPackageAsync(Package package);
        Task<Package> UpdatePackageAsync(string  id, Package package);
    }
}

