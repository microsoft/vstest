// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

// Do not parallelize performance tests. They should run one by one to
// reduce the chance that one test slows down another randomly.
[assembly: DoNotParallelize]
