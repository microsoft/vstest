// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Class to categorize the tests.
    /// </summary>
    internal sealed class TestListCategoryId
    {
        private static TestListCategoryId emptyId = new TestListCategoryId(Guid.Empty);

        private static TestListCategoryId uncategorizedId = new TestListCategoryId(new Guid("8C84FA94-04C1-424b-9868-57A2D4851A1D"));

        private static TestListCategoryId categoriesId = new TestListCategoryId(new Guid("8C43106B-9DC1-4907-A29F-AA66A61BF5B6"));

        private static TestListCategoryId all = new TestListCategoryId(new Guid("19431567-8539-422a-85D7-44EE4E166BDA"));

        private Guid id;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestListCategoryId"/> class.
        /// </summary>
        public TestListCategoryId()
        {
            this.id = Guid.NewGuid();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestListCategoryId"/> class.
        /// </summary>
        /// <param name="id">
        /// The id (GUID).
        /// </param>
        public TestListCategoryId(Guid id)
        {
            this.id = id;
        }


        /// <summary>
        /// Gets the Id of very root category - parent of all categories (fake, not real category).
        /// </summary>
        public static TestListCategoryId Root
        {
            get { return emptyId; }
        }

        /// <summary>
        /// Gets an object of <see cref="TestListCategoryId"/> class with empty GUID.
        /// </summary>
        public static TestListCategoryId Empty
        {
            get { return emptyId; }
        }

        /// <summary>
        /// Gets an object of <see cref="TestListCategoryId"/> class with GUID which represent uncategorized.
        /// </summary>
        public static TestListCategoryId Uncategorized
        {
            get { return uncategorizedId; }
        }

        /// <summary>
        /// Gets an object of <see cref="TestListCategoryId"/> class with GUID which represent categorize.
        /// </summary>
        public static TestListCategoryId Categories
        {
            get { return categoriesId; }
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        public Guid Id
        {
            get { return this.id; }
        }

        public static TestListCategoryId AllItems
        {
            get { return all; }
        }


        /// <summary>
        /// Override function for Equals.
        /// </summary>
        /// <param name="other">
        /// The object to compare.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool Equals(object other)
        {
            TestListCategoryId testListCategoryId = other as TestListCategoryId;
            if (testListCategoryId == null)
            {
                return false;
            }

            return this.id.Equals(testListCategoryId.id);
        }

        /// <summary>
        /// Override function for GetHashCode.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return this.id.GetHashCode();
        }

        /// <summary>
        /// Override function for ToString.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public override string ToString()
        {
            // "B" adds curly braces around guid
            string s = this.id.ToString("B");
            return string.Format(CultureInfo.InvariantCulture, s);
        }
    }
}
