// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Threading.Tasks;
using Fabrikam.Choreography.ChoreographyService.Models;

namespace Fabrikam.Choreography.ChoreographyService.Services
{
    public interface IDroneSchedulerServiceCaller
    {
        Task<string> GetDroneIdAsync(Delivery deliveryRequest);
    }
}
