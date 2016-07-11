// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace RuntimeReconfiguration.Web
{
    using System;
    using System.Collections.Generic;

    public class SomeRuntimeComponent
    {
        public static readonly SomeRuntimeComponent Instance = new SomeRuntimeComponent();
        private string currentValue;
        private List<string> pastValues = new List<string>();

        private SomeRuntimeComponent()
        {
            this.LastRestart = DateTimeOffset.UtcNow;
        }

        public string CurrentValue
        {
            get
            { 
                return this.currentValue;
            }

            set
            {
                this.currentValue = value;
                this.pastValues.Add(value);
            }
        }

        public IEnumerable<string> PastValues
        {
            get { return this.pastValues; }
        }

        public DateTimeOffset LastRestart { get; private set; }
    }
}