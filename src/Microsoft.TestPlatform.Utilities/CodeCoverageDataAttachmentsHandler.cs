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
        private const string CoverageFriendlyName = "Code Coverage";

        private const string CodeCoverageIOAssemblyName = "Microsoft.VisualStudio.Coverage.IO";
        private const string CoverageFileUtilityTypeName = "CoverageFileUtility";
        private const string MergeMethodName = "MergeCoverageFilesAsync";
        private const string WriteMethodName = "WriteCoverageFile";

        private static readonly Uri CodeCoverageDataCollectorUri = new Uri(CoverageUri);

        public bool SupportsIncrementalProcessing => true;

        public IEnumerable<Uri> GetExtensionUris()
        {
            yield return CodeCoverageDataCollectorUri;
        }

        public async Task<ICollection<AttachmentSet>> ProcessAttachmentSetsAsync(ICollection<AttachmentSet> attachments, IProgress<int> progressReporter, IMessageLogger logger, CancellationToken cancellationToken)
        {
            if (attachments != null && attachments.Any())
            {
                var coverageReportFilePaths = new List<string>();
                var coverageOtherFilePaths = new List<string>();

                foreach (var attachmentSet in attachments)
                {
                    foreach (var attachment in attachmentSet.Attachments)
                    {
                        if (attachment.Uri.LocalPath.EndsWith(CoverageFileExtension, StringComparison.OrdinalIgnoreCase))
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
                    var mergedCoverageReportFilePath = await this.MergeCodeCoverageFilesAsync(coverageReportFilePaths, progressReporter, cancellationToken).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(mergedCoverageReportFilePath))
                    {
                        var resultAttachmentSet = new AttachmentSet(CodeCoverageDataCollectorUri, CoverageFriendlyName);
                        resultAttachmentSet.Attachments.Add(UriDataAttachment.CreateFrom(mergedCoverageReportFilePath, CoverageFriendlyName));

                        foreach (var coverageOtherFilePath in coverageOtherFilePaths)
                        {
                            resultAttachmentSet.Attachments.Add(UriDataAttachment.CreateFrom(coverageOtherFilePath, string.Empty));
                        }

                        return new Collection<AttachmentSet> { resultAttachmentSet };
                    }
                }

                return attachments;
            }

            return new Collection<AttachmentSet>();
        }

        private async Task<string> MergeCodeCoverageFilesAsync(IList<string> files, IProgress<int> progressReporter, CancellationToken cancellationToken)
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

        private async Task<string> MergeCodeCoverageFilesAsync(IList<string> files, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var assemblyPath = Path.Combine(Path.GetDirectoryName(typeof(CodeCoverageDataAttachmentsHandler).GetTypeInfo().Assembly.GetAssemblyLocation()), CodeCoverageIOAssemblyName + ".dll");
            
            // Get assembly, type and methods
            Assembly assembly = new PlatformAssemblyLoadContext().LoadAssemblyFromPath(assemblyPath);
            var classType = assembly.GetType($"{CodeCoverageIOAssemblyName}.{CoverageFileUtilityTypeName}");
            var classInstance = Activator.CreateInstance(classType);
            var mergeMethodInfo = classType?.GetMethod(MergeMethodName, new[] { typeof(IList<string>), typeof(CancellationToken) });
            var writeMethodInfo = classType?.GetMethod(WriteMethodName);
            
            // Invoke methods
            var task = (Task)mergeMethodInfo.Invoke(classInstance, new object[] { files, cancellationToken });
            await task.ConfigureAwait(false);
            var coverageData = task.GetType().GetProperty("Result").GetValue(task, null);
            writeMethodInfo.Invoke(classInstance, new object[] { files[0], coverageData });

            // Delete original files and keep merged file only
            foreach (var file in files.Skip(1))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    EqtTrace.Error($"CodeCoverageDataCollectorAttachmentsHandler: Failed to remove {file}. Error: {ex}");
                }
            }

            return files[0];
        }
    }
}
