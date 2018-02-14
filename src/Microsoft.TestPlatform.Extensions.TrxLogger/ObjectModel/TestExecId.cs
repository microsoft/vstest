// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Class identifying test execution id.
    /// Execution ID is assigned to test at run creation time and is guaranteed to be unique within that run.
    /// </summary>
    internal sealed class TestExecId
    {
        private static TestExecId emptyId = new TestExecId(Guid.Empty);

        private Guid execId;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestExecId"/> class.
        /// </summary>
        public TestExecId()
        {
            this.execId = Guid.NewGuid();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestExecId"/> class.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        public TestExecId(Guid id)
        {
            this.execId = id;
        }

        /// <summary>
        /// Gets an object of <see cref="TestExecId"/> class which empty GUID
        /// </summary>
        public static TestExecId Empty
        {
            get { return emptyId; }
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        public Guid Id
        {
            get { return this.execId; }
        }

        /// <summary>
        /// Override function of Equals.
        /// </summary>
        /// <param name="obj">
        /// The object to compare.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool Equals(object obj)
        {
            TestExecId id = obj as TestExecId;

            if (id == null)
            {
                return false;
            }

            return this.execId.Equals(id.execId);
        }

        /// <summary>
        /// Override function of GetHashCode 
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return this.execId.GetHashCode();
        }

        /// <summary>
        /// Override function of ToString.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public override string ToString()
        {
            string s = this.execId.ToString("B");
            return string.Format(CultureInfo.InvariantCulture, s);
        }
    }
}
