// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace PipesAndFilters.Shared
{
    using System.Runtime.Serialization;

    [DataContract]
    public class TestMessage
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Text { get; set; }
    }
}
