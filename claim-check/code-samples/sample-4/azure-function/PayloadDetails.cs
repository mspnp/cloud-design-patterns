using System;
using System.Collections.Generic;
using System.Text;

namespace azure_function
{
    public class PayloadDetails
    {
        public string ContainerName { get; set; }
        public string BlobName { get; set; }
    }
}
