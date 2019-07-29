using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.TestPlatform.Extensions.HtmlLogger.Utility;
using Constants = Microsoft.TestPlatform.Extensions.HtmlLogger.Utility.Constants;

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger
{
    class Converter
    {
        public static Guid GetParentExecutionId(ObjectModel.TestResult testResult)
        {
            TestProperty parentExecutionIdProperty = testResult.Properties.FirstOrDefault(
                property => property.Id.Equals(Constants.ParentExecutionIdPropertyIdentifier));

            return parentExecutionIdProperty == null ?
                Guid.Empty :
                testResult.GetPropertyValue(parentExecutionIdProperty, Guid.Empty);
        }


        public static Guid GetExecutionId(ObjectModel.TestResult testResult)
        {
            TestProperty executionIdProperty = testResult.Properties.FirstOrDefault(
                property => property.Id.Equals(Constants.ExecutionIdPropertyIdentifier));

            var executionId = Guid.Empty;
            if (executionIdProperty != null)
                executionId = testResult.GetPropertyValue(executionIdProperty, Guid.Empty);

            return executionId.Equals(Guid.Empty) ? Guid.NewGuid() : executionId;
        }
    }


}
