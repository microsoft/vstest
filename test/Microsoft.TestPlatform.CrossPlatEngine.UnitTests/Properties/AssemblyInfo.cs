// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

// MtpTestNodeConverterTestIdTests pins the test id algorithm by setting
// VSTEST_TESTCASE_ID_ALGORITHM and resetting TestCase's cache of it. Both are process wide, so a
// class level [DoNotParallelize] does not stop another class from observing the pinned value. This
// assembly already runs sequentially because nothing opts it into parallelization; this states that
// as a requirement rather than leaving it as an accident.
[assembly: DoNotParallelize]
