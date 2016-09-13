// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace PipesAndFilters.Shared
{
    using System;

    /// <summary>
    /// Pipes and Filters Constants
    /// </summary>
    public static class Constants
    {
        public const string QueueAPath = "queue-a";
        public const string QueueBPath = "queue-b";
        public const string QueueFinalPath = "queue-final";
        public const string FilterAMessageKey = "FilterA";
        public const string FilterBMessageKey = "FilterB";
        public const int MaxServiceBusDeliveryCount = 4;
    }
}
