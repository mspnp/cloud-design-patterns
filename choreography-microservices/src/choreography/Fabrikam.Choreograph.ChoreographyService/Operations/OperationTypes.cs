// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Newtonsoft.Json.Linq;

namespace Fabrikam.Communicator.Service.Operations
{
    public static class Operations
    {


        public static T ConvertDataEventToType<T>(object dataObject)
        {
            if (dataObject is JObject o)
            {
                return o.ToObject<T>();
            }

            return (T)dataObject;
        }
        public static class ChoreographyOperation
        {
            public const string ScheduleDelivery = nameof(ScheduleDelivery);
            public const string RescheduledDelivery = nameof(RescheduledDelivery);
            public const string CancelDelivery = nameof(CancelDelivery);
            public const string GetDrone = nameof(GetDrone);
            public const string CreatePackage = nameof(CreatePackage);
        }

  
    }
}
