// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
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

        private const string CodeCoverageAnalysisAssemblyName = "Microsoft.VisualStudio.Coverage.Analysis";
        private const string MergeMethodName = "MergeCoverageFiles";
        private const string CoverageInfoTypeName = "CoverageInfo";

        private static readonly string[] SupportedFileExtensions = new string[] { ".dll", ".exe" };

        public ICollection<AttachmentSet> HandleDataCollectionAttachmentSets(ICollection<AttachmentSet> dataCollectionAttachments)
        {
            IList<string> coverageAttachmentSet = new List<string>();
            foreach (var dataCollectionAttachment in dataCollectionAttachments)
            {
                if (CodeCoverageDataCollectorUri.Equals(dataCollectionAttachment.Uri))
                {
                    foreach (var file in dataCollectionAttachment.Attachments)
                    {
                        coverageAttachmentSet.Add(file.Uri.LocalPath);
                    }
                }
            }

            return MergeCodeCoverageFiles(coverageAttachmentSet);
        }

        private ICollection<AttachmentSet> MergeCodeCoverageFiles(IList<string> files)
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
                    return new Collection<AttachmentSet> { new AttachmentSet(CodeCoverageDataCollectorUri, outputfileName) };
                }
                catch (Exception ex)
                {
                    if (EqtTrace.IsErrorEnabled)
                    {
                        EqtTrace.Error("CodeCoverageDataCollectorAttachmentsHandler: Failed to load datacollector of type : {0} from location : {1}. Error : ", CodeCoverageAnalysisAssemblyName, assemblyPath, ex.Message);
                    }
                }
            }

            return new Collection<AttachmentSet>();
        }
    }
}
