// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Intent;

using System.Reflection;

public class Runner
{
    public static void Run(IEnumerable<string> path, IRunLogger logger)
    {
        foreach (var p in path)
        {
            try
            {
                var asm = Assembly.LoadFrom(p);
                if (asm.IsExcluded())
                    continue;

                var ts = asm.GetTypes().SkipNonPublic().SkipExcluded();
                foreach (var t in ts)
                {
                    var ms = t.GetMethods().SkipExcluded();

                    // TODO: This chooses the Only tests only for single assembly and single class,
                    // to support this full we would have to enumerate all classes and methods first,
                    // it is easy, I just don't need it right now.
                    var only = ms.Where(m => m.GetCustomAttribute<OnlyAttribute>() != null).ToList();
                    if (only.Any())
                        ms = only;

                    foreach (var m in ms)
                    {
                        try
                        {
                            var i = Activator.CreateInstance(t);
                            var result = m.Invoke(i, Array.Empty<object>());
                            if (result is Task task)
                            {
                                // When the result is a task we need to await it.
                                // TODO: this can be improved with await, imho
                                task.GetAwaiter().GetResult();
                            };

                            logger.WriteTestPassed(m);
                        }
                        catch (Exception ex)
                        {
                            if (ex is TargetInvocationException tex && tex.InnerException != null)
                            {
                                logger.WriteTestFailure(m, tex.InnerException);
                            }
                            else
                            {
                                logger.WriteTestFailure(m, ex);
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
