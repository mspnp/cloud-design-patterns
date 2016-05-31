// ==============================================================================================================
// Microsoft patterns & practices
// Cloud Design Patterns project
// ==============================================================================================================
// ©2013 Microsoft. All rights reserved. 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================
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
