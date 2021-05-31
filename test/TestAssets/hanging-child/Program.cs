using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace hanging_child
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                var val = int.Parse(args[0]);
                if (val > 0)
                {
                    // 2 chidren, that is 3 hanging processes
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Process.Start(Process.GetCurrentProcess().MainModule.FileName, (val - 1).ToString());
                    }
                    else
                    {
                        var dll = Assembly.GetCallingAssembly().Location;
                        Process.Start(GetFullPath("dotnet"), $"{dll} {val - 1}");
                    }
                }
            }

            Thread.Sleep(30_000);
        }
        public static string GetFullPath(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }
    }
}
