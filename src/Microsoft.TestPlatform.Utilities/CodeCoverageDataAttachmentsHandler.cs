// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

    public class CodeCoverageDataAttachmentsHandler : IDataCollectorAttachmentProcessor
    {
        private const string CoverageUri = "datacollector://microsoft/CodeCoverage/2.0";
        private const string CoverageFileExtension = ".coverage";
        private const string XmlFileExtension = ".xml";
        private const string CoverageFriendlyName = "Code Coverage";

        private const string CodeCoverageIOAssemblyName = "Microsoft.VisualStudio.Coverage.IO";
        private const string CoverageFileUtilityTypeName = "CoverageFileUtility";
        private const string MergeMethodName = "MergeCoverageReportsAsync";
        private const string CoverageMergeOperationName = "CoverageMergeOperation";

        private static readonly Uri CodeCoverageDataCollectorUri = new Uri(CoverageUri);
        private static Assembly CodeCoverageAssembly;
        private static object ClassInstance;
        private static MethodInfo MergeMethodInfo;
        private static Array MergeOperationEnumValues;

        public bool SupportsIncrementalProcessing => true;

        public IEnumerable<Uri> GetExtensionUris()
        {
            yield return CodeCoverageDataCollectorUri;
        }

        public async Task<ICollection<AttachmentSet>> ProcessAttachmentSetsAsync(ICollection<AttachmentSet> attachments, IProgress<int> progressReporter, IMessageLogger logger, CancellationToken cancellationToken)
        {
            if ((attachments?.Any()) != true)
                return new Collection<AttachmentSet>();

            var coverageReportFilePaths = new List<string>();
            var coverageOtherFilePaths = new List<string>();

            foreach (var attachmentSet in attachments)
            {
                foreach (var attachment in attachmentSet.Attachments)
                {
                    if (attachment.Uri.LocalPath.EndsWith(CoverageFileExtension, StringComparison.OrdinalIgnoreCase) ||
                        attachment.Uri.LocalPath.EndsWith(XmlFileExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        coverageReportFilePaths.Add(attachment.Uri.LocalPath);
                    }
                    else
                    {
                        coverageOtherFilePaths.Add(attachment.Uri.LocalPath);
                    }
                }
            }

            if (coverageReportFilePaths.Count > 1)
            {
                var mergedCoverageReports = await this.MergeCodeCoverageFilesAsync(coverageReportFilePaths, progressReporter, cancellationToken).ConfigureAwait(false);
                var resultAttachmentSet = new AttachmentSet(CodeCoverageDataCollectorUri, CoverageFriendlyName);

                foreach (var coverageReport in mergedCoverageReports)
                {
                    resultAttachmentSet.Attachments.Add(UriDataAttachment.CreateFrom(coverageReport, CoverageFriendlyName));
                }

                foreach (var coverageOtherFilePath in coverageOtherFilePaths)
                {
                    resultAttachmentSet.Attachments.Add(UriDataAttachment.CreateFrom(coverageOtherFilePath, string.Empty));
                }

                return new Collection<AttachmentSet> { resultAttachmentSet };
            }

            return attachments;
        }

        private async Task<IList<string>> MergeCodeCoverageFilesAsync(IList<string> files, IProgress<int> progressReporter, CancellationToken cancellationToken)
        {
            try
            {
                // Warning: Don't remove this method call.
                //
                // We took a dependency on Coverage.CoreLib.Net. In the unlikely case it cannot be
                // resolved, this method call will throw an exception that will be caught and
                // absorbed here.
                var result = await this.MergeCodeCoverageFilesAsync(files, cancellationToken).ConfigureAwait(false);
                progressReporter?.Report(100);
                return result;
            }
            catch (OperationCanceledException)
            {
                // Occurs due to cancellation, ok to re-throw.
                throw;
            }
            catch (Exception ex)
            {
                EqtTrace.Error(
                    "CodeCoverageDataCollectorAttachmentsHandler: Failed to load datacollector. Error: {0}",
                    ex.ToString());
            }

            return null;
        }

        private async Task<IList<string>> MergeCodeCoverageFilesAsync(IList<string> files, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Invoke methods
            LoadCodeCoverageAssembly();
            var task = (Task)MergeMethodInfo.Invoke(ClassInstance, new object[] { files[0], files, MergeOperationEnumValues.GetValue(0), true, cancellationToken });
            await task.ConfigureAwait(false);
            var coverageData = task.GetType().GetProperty("Result").GetValue(task, null);
            var mergedResults = coverageData as IList<string>;

            // Delete original files and keep merged file only
            foreach (var file in files)
            {
                if (mergedResults.Contains(file))
                    continue;

                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    EqtTrace.Error($"CodeCoverageDataCollectorAttachmentsHandler: Failed to remove {file}. Error: {ex}");
                }
            }

            return mergedResults;
        }

        private void LoadCodeCoverageAssembly()
        {
            if (CodeCoverageAssembly != null)
                return;

            var assemblyPath = Path.Combine(Path.GetDirectoryName(typeof(CodeCoverageDataAttachmentsHandler).GetTypeInfo().Assembly.GetAssemblyLocation()), CodeCoverageIOAssemblyName + ".dll");
            CodeCoverageAssembly = new PlatformAssemblyLoadContext().LoadAssemblyFromPath(assemblyPath);

            var classType = CodeCoverageAssembly.GetType($"{CodeCoverageIOAssemblyName}.{CoverageFileUtilityTypeName}");
            ClassInstance = Activator.CreateInstance(classType);

            var types = CodeCoverageAssembly.GetTypes();
            var mergeOperationEnum = Array.Find(types, d => d.Name == CoverageMergeOperationName);
            MergeOperationEnumValues = Enum.GetValues(mergeOperationEnum);
            MergeMethodInfo = classType?.GetMethod(MergeMethodName, new[] { typeof(string), typeof(IList<string>), mergeOperationEnum, typeof(bool), typeof(CancellationToken) });
        }
    }
}
