// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Fabrikam.Choreography.ChoreographyService.Models;

namespace Fabrikam.Communicator.Service.Utils
{
    class ModelsConverter
    {
        internal static PackageDetail GetPackageDetail(PackageInfo packageInfo)
        {
            var packageDetail = new PackageDetail
            {
                Id = packageInfo.PackageId,
                Size = GetPackageSize(packageInfo.Size)
            };

            return packageDetail;
        }

        private static PackageSize GetPackageSize(ContainerSize containerSize)
        {
            return (PackageSize)(int)containerSize;
        }
    }
}
