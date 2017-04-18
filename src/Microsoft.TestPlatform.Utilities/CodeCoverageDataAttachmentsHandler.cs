// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.IO;
    using System.Reflection;
#if !NET46
    using System.Runtime.Loader;
#endif
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    public class CodeCoverageDataAttachmentsHandler : IDataCollectorAttachments
    {
        private const string CoverageUri = "datacollector://microsoft/CodeCoverage/2.0";
        private const string CoverageFileExtension = ".coverage";
        private static readonly Uri CodeCoverageDataCollectorUri = new Uri(CoverageUri);
        private const string CoverageFriendlyName = "Code Coverage";

        private const string CodeCoverageAnalysisAssemblyName = "Microsoft.VisualStudio.Coverage.Analysis";
        private const string MergeMethodName = "MergeCoverageFiles";
        private const string CoverageInfoTypeName = "CoverageInfo";

        private static readonly string[] SupportedFileExtensions = { ".dll", ".exe" };

        public ICollection<AttachmentSet> HandleDataCollectionAttachmentSets(ICollection<AttachmentSet> dataCollectionAttachments)
        {
            if (dataCollectionAttachments == null)
                return new Collection<AttachmentSet>();

            var coverageAttachments = dataCollectionAttachments
                .Where(dataCollectionAttachment => CodeCoverageDataCollectorUri.Equals(dataCollectionAttachment.Uri)).ToArray();

            if (coverageAttachments.Any())
            {
                var codeCoverageFiles = coverageAttachments.Select(coverageAttachment => coverageAttachment.Attachments[0].Uri.LocalPath).ToArray();
                var outputFile = MergeCodeCoverageFiles(codeCoverageFiles);
                var attachmentSet = new AttachmentSet(CodeCoverageDataCollectorUri, CoverageFriendlyName);

                if (!string.IsNullOrEmpty(outputFile))
                {
                    attachmentSet.Attachments.Add(new UriDataAttachment(new Uri(outputFile), CoverageFriendlyName));
                    return new Collection<AttachmentSet> { attachmentSet };
                }
                // In case merging fails(esp in dotnet core we cannot merge), so return filtered list of Code Coverage Attachments
                return coverageAttachments;
            }

            return new Collection<AttachmentSet>();
        }

        private string MergeCodeCoverageFiles(IList<string> files)
        {
            string fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + CoverageFileExtension);
            string outputfileName = files[0];

            File.Create(fileName).Dispose();

            foreach (var extension in SupportedFileExtensions)
            {
                var assemblyPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), CodeCoverageAnalysisAssemblyName + extension);

                try
                {
                    Assembly assembly = null;
#if NET46
                    assembly = Assembly.LoadFrom(assemblyPath);
#else
                    assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
#endif
                    var type = assembly.GetType(CodeCoverageAnalysisAssemblyName + "." + CoverageInfoTypeName);

                    var methodInfo = type?.GetMethod(MergeMethodName);

                    if (methodInfo != null)
                    {
                        for (int i = 1; i < files.Count; i++)
                        {
                            methodInfo.Invoke(null, new object[] { files[i], outputfileName, fileName, true });
                            File.Copy(fileName, outputfileName, true);

                            File.Delete(files[i]);
                        }

                        File.Delete(fileName);
                    }
                    return outputfileName;
                }
                catch (Exception ex)
                {
                    if (EqtTrace.IsErrorEnabled)
                    {
                        EqtTrace.Error("CodeCoverageDataCollectorAttachmentsHandler: Failed to load datacollector of type : {0} from location : {1}. Error : ", CodeCoverageAnalysisAssemblyName, assemblyPath, ex.Message);
                    }
                }
            }

            return string.Empty;
        }
    }
}
