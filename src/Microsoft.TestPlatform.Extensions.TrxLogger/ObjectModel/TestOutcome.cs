// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Outcome of a test or a run.
    /// If a new successful state needs to be added you will need to modify 
    /// RunResultAndStatistics in TestRun.cs and TestOutcomeHelper below.
    /// ----------------------------------------------------------------
    /// NOTE: the order is important and is used for computing outcome for aggregations. 
    ///       More important outcomes come first. See TestOutcomeHelper.GetAggregationOutcome.
    /// </summary>
    public enum TestOutcome
    {
        /// <summary>
        /// There was a system error while we were trying to execute a test.
        /// </summary>
        Error,

        /// <summary>
        /// Test was executed, but there were issues.
        /// Issues may involve exceptions or failed assertions.
        /// </summary>
        Failed,

        /// <summary>
        /// The test timed out
        /// </summary>
        Timeout,

        /// <summary>
        /// Test was aborted. 
        /// This was not caused by a user gesture, but rather by a framework decision.
        /// </summary>
        Aborted,

        /// <summary>
        /// Test has completed, but we can't say if it passed or failed.
        /// May be used for aborted tests...
        /// </summary>
        Inconclusive,

        /// <summary>
        /// Test was executed w/o any issues, but run was aborted.
        /// </summary>
        PassedButRunAborted,

        /// <summary>
        /// Test had it chance for been executed but was not, as ITestElement.IsRunnable == false.
        /// </summary>
        NotRunnable,

        /// <summary>
        /// Test was not executed. 
        /// This was caused by a user gesture - e.g. user hit stop button.
        /// </summary>
        NotExecuted,

        /// <summary>
        /// Test run was disconnected before it finished running.
        /// </summary>
        Disconnected,

        /// <summary>
        /// To be used by Run level results.
        /// This is not a failure.
        /// </summary>
        Warning,

        /// <summary>
        /// Test was executed w/o any issues.
        /// </summary>
        Passed,

        /// <summary>
        /// Test has completed, but there is no qualitative measure of completeness.
        /// </summary>
        Completed,

        /// <summary>
        /// Test is currently executing.
        /// </summary>
        InProgress,

        /// <summary>
        /// Test is in the execution queue, was not started yet.
        /// </summary>
        Pending,

        /// <summary>
        /// The min value of this enum
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
        Min = Error,

        /// <summary>
        /// The max value of this enum
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly",
            Justification = "Reviewed. Suppression is OK here.")]
        Max = Pending
    }
}
