using System.Runtime.Serialization;

namespace Shared
{
    [DataContract]
    public class ApplicationLog
    {
        [DataMember]
        public int Total { get; set; }
    }
}
