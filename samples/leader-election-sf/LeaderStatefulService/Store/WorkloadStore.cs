using Shared;
using System.Collections.Generic;
using System.Linq;

namespace LeaderStatefulService.Store
{
    public class WorkloadStore
    {
        public List<ApplicationLog> ApplicationLogs { get; private set; }

        public WorkloadStore()
        {
            InitializeStore();
        }

        private void InitializeStore()
        {
            ApplicationLogs = Enumerable.Range(0, 100)
                .Select(i =>
                {
                    var appLog = new ApplicationLog { Total = i };
                    return appLog;
                }).ToList();
        }
    }
}
