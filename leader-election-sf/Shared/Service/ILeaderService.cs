using Microsoft.ServiceFabric.Services.Remoting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shared.Service
{
    public interface ILeaderService : IService
    {
        Task<List<ApplicationLog>> GetWorkloadChunk();

        Task ReportResult(int total);
    }
}
