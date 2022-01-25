﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    public class BlameTestObject
    {
        private Guid id;
        private string displayName;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameTestObject"/> class.
        /// </summary>
        public BlameTestObject()
        {
            // Default constructor
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameTestObject"/> class.
        /// </summary>
        /// <param name="fullyQualifiedName">
        /// Fully qualified name of the test case.
        /// </param>
        /// <param name="executorUri">
        /// The Uri of the executor to use for running this test.
        /// </param>
        /// <param name="source">
        /// Test container source from which the test is discovered.
        /// </param>
        public BlameTestObject(string fullyQualifiedName, Uri executorUri, string source)
        {
            ValidateArg.NotNullOrEmpty(fullyQualifiedName, nameof(fullyQualifiedName));
            ValidateArg.NotNull(executorUri, nameof(executorUri));
            ValidateArg.NotNullOrEmpty(source, nameof(source));

            Id = Guid.Empty;
            FullyQualifiedName = fullyQualifiedName;
            ExecutorUri = executorUri;
            Source = source;
            IsCompleted = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameTestObject"/> class.
        /// </summary>
        /// <param name="testCase">
        /// The test case
        /// </param>
        public BlameTestObject(TestCase testCase)
        {
            Id = testCase.Id;
            FullyQualifiedName = testCase.FullyQualifiedName;
            ExecutorUri = testCase.ExecutorUri;
            Source = testCase.Source;
            DisplayName = testCase.DisplayName;
            IsCompleted = false;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the id of the test case.
        /// </summary>
        public Guid Id
        {
            get
            {
                return id;
            }

            set
            {
                id = value;
            }
        }

        /// <summary>
        /// Gets or sets the fully qualified name of the test case.
        /// </summary>
        public string FullyQualifiedName { get; set; }

        /// <summary>
        /// Gets or sets the Uri of the Executor to use for running this test.
        /// </summary>
        public Uri ExecutorUri
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the test container source from which the test is discovered.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether test case is completed or not.
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Gets or sets the display name of the test case
        /// </summary>
        public string DisplayName
        {
            get
            {
                return displayName;
            }

            set
            {
                displayName = value;
            }
        }

        #endregion
    }
}