using System;
using System.Runtime.Serialization;

namespace Fabrikam.Choreography.ChoreographyService.Services
{
    public class EventException: Exception
    {
        public EventException() : base() { }

        public EventException(string message) : base(message) { }

        public EventException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public EventException(string message, Exception innerException) : base(message, innerException) { }

    }
}
