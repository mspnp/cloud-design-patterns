using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class ConfigurationSchema
    {
        [DataMember]
        public string Environment { get; set; }
        [DataMember]
        public ApplicationSettings Settings { get; set; }
    }

    [DataContract]
    public class ApplicationSettings
    {
        [DataMember]
        public string Setting1 { get; set; }
        [DataMember]
        public string Setting2 { get; set; }
    }
}
