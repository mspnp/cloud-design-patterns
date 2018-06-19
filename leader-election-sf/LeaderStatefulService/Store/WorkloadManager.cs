using Shared;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace LeaderStatefulService.Store
{
    [DataContract]
    public class WorkloadManager
    {
        const int DefaultElementsCount = 10;
        
        [DataMember]
        public int AggregatedTotal { get; set; }
        [DataMember]
        public int Page { get; set; }
        [DataMember]
        public int ItemsPerPage { get; set; }

        private static WorkloadStore Store => new WorkloadStore();

        public WorkloadManager(int itemsPerPage = DefaultElementsCount)
        {
            ItemsPerPage = itemsPerPage;
            Page = -1;
        }

        public Task<List<ApplicationLog>> GetNextChunk()
        {
            var result = Store.ApplicationLogs
                .Skip(Page * ItemsPerPage)
                .Take(ItemsPerPage).ToList();

            return Task.FromResult(result);
        }
    }
}
