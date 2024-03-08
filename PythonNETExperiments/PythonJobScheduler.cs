using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Python.Runtime;

namespace PythonNETExperiments
{
    public static class PythonJobScheduler
    {
        public static readonly string[] PACKAGES;
        
        [ModuleInitializer]
        public static void InstallPythonPackages()
        {
            RuntimeHelpers.RunClassConstructor(typeof(PythonJobScheduler).TypeHandle);
        }
        
        private static void InstallPackage(string packageName)
        {
            Console.WriteLine($"Installing {packageName}...");
            
            // Define the process start information
            var startInfo = new ProcessStartInfo
            {
                FileName = "pip3",
                Arguments = $"install {packageName}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            // Create and start the process
            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    Console.WriteLine("Process could not be started. Ensure Python is installed and accessible.");
                    return;
                }
            
                // Read the output
                string output = process.StandardOutput.ReadToEnd();
                string err = process.StandardError.ReadToEnd();

                process.WaitForExit(); // Wait for the process to exit

                // Optionally: Display the output
                Console.WriteLine(output);
                if (!string.IsNullOrEmpty(err))
                {
                    Console.WriteLine("Errors during installation:");
                    Console.WriteLine(err);
                }
                else
                {
                    Console.WriteLine($"{packageName} installation was successful.");
                }
            }
        }

        static PythonJobScheduler()
        {
            Runtime.PythonDLL = "/Library/Frameworks/Python.framework/Versions/3.11/lib/libpython3.11.dylib";
            
            // Console.WriteLine(Runtime.PythonDLL);
            // Console.WriteLine(PythonEngine.Platform);
            // Console.WriteLine(PythonEngine.MinSupportedVersion);
            // Console.WriteLine(PythonEngine.MaxSupportedVersion);
            // Console.WriteLine(PythonEngine.BuildInfo);
            // Console.WriteLine(PythonEngine.PythonPath);
            
            PythonEngine.Initialize();
            PythonEngine.BeginAllowThreads();
            
            var jobBaseType = typeof(IPythonJobBase);

            var jobs = typeof(PythonJobScheduler).Assembly
                .GetTypes()
                .Where(x => x.IsValueType && x.GetInterfaces().Contains(jobBaseType))
                .ToArray(); 
            
            var packageList = new HashSet<string>();
            
            foreach (var job in jobs)
            {
                var packagesProp = job.GetProperty(nameof(IPythonJobBase.Packages), BindingFlags.Static | BindingFlags.Public);

                if (packagesProp != null)
                {
                    foreach (var package in Unsafe.As<string[]>(packagesProp.GetValue(null))!)
                    {
                        packageList.Add(package);
                    }
                }
            }
            
            foreach (var package in packageList)
            {
                InstallPackage(package);
            }

            // packageList.Add("pip");

            var packageCount = packageList.Count;
                
            Console.WriteLine($"All packages installed! [ {packageCount} ]");
                
            // Static Initialize

            using (Py.GIL())
            {
                foreach (var job in jobs)
                {
                    job.GetMethod(nameof(IPythonJobBase.StaticInitialize), BindingFlags.Static | BindingFlags.Public)!
                        .Invoke(null, null);
                }
            }
            
            // PythonEngine.BeginAllowThreads();
        }
        
        public static JobT ExecuteJob<JobT>(this JobT job) where JobT: struct, IPythonJob
        {
            using (Py.GIL())
            {
                job.Initialize();
                job.Run();
                return job;
            }
        }
        
        public static async Task<JobT> ExecuteJobThreaded<JobT>(this JobT job) where JobT: struct, IPythonJob
        {
            return await Task.Run(() =>
            {
                using (Py.GIL())
                {
                    job.Run();
                }

                return job;
            });
        }
    }
}
