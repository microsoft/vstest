// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.Coverage.CoreLib.Net;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    public class CodeCoverageDataAttachmentsHandler : IDataCollectorAttachmentProcessor
    {
        private const string CoverageUri = "datacollector://microsoft/CodeCoverage/2.0";
        private const string CoverageFileExtension = ".coverage";
        private const string CoverageFriendlyName = "Code Coverage";

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

                if(coverageReportFilePaths.Count > 1)
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
            var coverageUtility = new CoverageFileUtility();

            var coverageData = await coverageUtility.MergeCoverageFilesAsync(
                    files,
                    cancellationToken).ConfigureAwait(false);

            coverageUtility.WriteCoverageFile(files[0], coverageData);

            foreach(var file in files.Skip(1))
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
