using Microsoft.ServiceFabric.Services.Runtime;
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
        const int _defaultElements = 10;
        
        [DataMember]
        public int AggregatedTotal { get; set; }
        [DataMember]
        public int Page { get; set; }
        [DataMember]
        public int ItemsPerPage { get; set; }

        private WorkloadStore Store
        {
            get
            {
                // NOTE: ideally, there would be no need to recreate test
                //       data over and over... :)
                return new WorkloadStore();
            }
        }

        public WorkloadManager(int itemsPerPage = _defaultElements)
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
