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

    using Microsoft.VisualStudio.Coverage.CoreLib.Net;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

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

        public Task<ICollection<AttachmentSet>> ProcessAttachmentSetsAsync(ICollection<AttachmentSet> attachments, IProgress<int> progressReporter, IMessageLogger logger, CancellationToken cancellationToken)
        {
            if (attachments != null && attachments.Any())
            {
                var codeCoverageFiles = attachments.Select(coverageAttachment => coverageAttachment.Attachments[0].Uri.LocalPath).ToArray();
                var outputFile = MergeCodeCoverageFiles(codeCoverageFiles, progressReporter, cancellationToken);
                var attachmentSet = new AttachmentSet(CodeCoverageDataCollectorUri, CoverageFriendlyName);

                if (!string.IsNullOrEmpty(outputFile))
                {
                    attachmentSet.Attachments.Add(new UriDataAttachment(new Uri(outputFile), CoverageFriendlyName));
                    return Task.FromResult((ICollection<AttachmentSet>)new Collection<AttachmentSet> { attachmentSet });
                }

                // In case merging fails(esp in dotnet core we cannot merge), so return filtered list of Code Coverage Attachments
                return Task.FromResult(attachments);
            }

            return Task.FromResult((ICollection<AttachmentSet>)new Collection<AttachmentSet>());
        }

        private string MergeCodeCoverageFiles(IList<string> files, IProgress<int> progressReporter, CancellationToken cancellationToken)
        {
            if (files.Count == 1)
            {
                return files[0];
            }

            try
            {
                // Warning: Don't remove this method call.
                //
                // We took a dependency on Coverage.CoreLib.Net. In the unlikely case it cannot be
                // resolved, this method call will throw an exception that will be caught and
                // absorbed here.
                this.MergeCodeCoverageFiles(files, cancellationToken);
                progressReporter?.Report(100);
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

            
            return files[0];
        }

        private void MergeCodeCoverageFiles(IList<string> files, CancellationToken cancellationToken)
        {
            var coverageUtility = new CoverageFileUtility();

            var coverageData = Task.Run(
                async () => await coverageUtility.MergeCoverageFilesAsync(
                    files,
                    cancellationToken))
                .GetAwaiter().GetResult();

            coverageUtility.WriteCoverageFile(files[0], coverageData);
        }
    }
}
