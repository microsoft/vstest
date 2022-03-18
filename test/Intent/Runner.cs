// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Intent;

public class Runner
{
    public static void Run(IEnumerable<string> paths, IRunLogger logger)
    {
        foreach (var path in paths)
        {
            try
            {
                var assembly = Assembly.LoadFrom(path);
                if (assembly.IsExcluded())
                    continue;

                var types = assembly.GetTypes().SkipNonPublic().SkipExcluded();
                foreach (var type in types)
                {
                    var methods = type.GetMethods().SkipExcluded();

                    // TODO: This chooses the Only tests only for single assembly and single class,
                    // to support this full we would have to enumerate all classes and methods first,
                    // it is easy, I just don't need it right now.
                    var methodsWithOnly = methods.Where(m => m.GetCustomAttribute<OnlyAttribute>() != null).ToList();
                    if (methodsWithOnly.Count > 0)
                        methods = methodsWithOnly;

                    foreach (var method in methods)
                    {
                        try
                        {
                            var instance = Activator.CreateInstance(type);
                            var testResult = method.Invoke(instance, Array.Empty<object>());
                            if (testResult is Task task)
                            {
                                // When the result is a task we need to await it.
                                // TODO: this can be improved with await, imho
                                task.GetAwaiter().GetResult();
                            };

                            logger.WriteTestPassed(method);
                        }
                        catch (Exception ex)
                        {
                            if (ex is TargetInvocationException tex && tex.InnerException != null)
                            {
                                logger.WriteTestFailure(method, tex.InnerException);
                            }
                            else
                            {
                                logger.WriteTestFailure(method, ex);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.WriteFrameworkError(ex);
            }
        }
    }
}
