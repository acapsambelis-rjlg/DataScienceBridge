using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DataScienceWorkbench
{
    public class DataExporter
    {
        public static string ExportToJson<T>(List<T> data, string name)
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data_exports");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, name + ".json");
            string json = SimpleJson.Serialize(data);
            File.WriteAllText(path, json);
            return path;
        }

        public static string ExportToCsv<T>(List<T> data, string name)
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data_exports");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, name + ".csv");
            var props = typeof(T).GetProperties();
            var sb = new StringBuilder();

            sb.AppendLine(string.Join(",", Array.ConvertAll(props, p => p.Name)));
            foreach (var item in data)
            {
                var values = new List<string>();
                foreach (var prop in props)
                {
                    var val = prop.GetValue(item);
                    string s = val != null ? val.ToString() : "";
                    if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                        s = "\"" + s.Replace("\"", "\"\"") + "\"";
                    values.Add(s);
                }
                sb.AppendLine(string.Join(",", values));
            }
            File.WriteAllText(path, sb.ToString());
            return path;
        }
    }

    public class PythonRunner
    {
        private string pythonPath;

        public PythonRunner()
        {
            pythonPath = FindPython();
        }

        private string FindPython()
        {
            bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT
                          || Environment.OSVersion.Platform == PlatformID.Win32S
                          || Environment.OSVersion.Platform == PlatformID.Win32Windows
                          || Environment.OSVersion.Platform == PlatformID.WinCE;

            string locator = isWindows ? "where" : "which";
            string[] candidates = isWindows
                ? new[] { "python", "python3", "py" }
                : new[] { "python3", "python" };

            foreach (var cand in candidates)
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = locator,
                        Arguments = cand,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    var proc = Process.Start(psi);
                    string output = proc.StandardOutput.ReadToEnd().Trim();
                    proc.WaitForExit();
                    if (proc.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        string firstLine = output.Split('\n')[0].Trim();
                        return firstLine;
                    }
                }
                catch { }
            }

            if (isWindows)
            {
                string[] commonPaths = {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python311", "python.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python310", "python.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python39", "python.exe"),
                    @"C:\Python311\python.exe",
                    @"C:\Python310\python.exe",
                    @"C:\Python39\python.exe"
                };
                foreach (var p in commonPaths)
                {
                    if (File.Exists(p))
                        return p;
                }
            }

            return isWindows ? "python" : "python3";
        }

        public string GetPythonPath() { return pythonPath; }

        public PythonResult Execute(string script, string workingDir = null)
        {
            string tempScript = Path.GetTempFileName() + ".py";
            File.WriteAllText(tempScript, script);

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = "\"" + tempScript + "\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                if (!string.IsNullOrEmpty(workingDir))
                    psi.WorkingDirectory = workingDir;

                psi.EnvironmentVariables["MPLBACKEND"] = "Agg";

                var proc = Process.Start(psi);
                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit(60000);

                return new PythonResult
                {
                    ExitCode = proc.ExitCode,
                    Output = stdout,
                    Error = stderr,
                    Success = proc.ExitCode == 0
                };
            }
            finally
            {
                try { File.Delete(tempScript); } catch { }
            }
        }

        public PythonResult InstallPackage(string packageName)
        {
            var psi = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = "-m pip install --user " + packageName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var proc = Process.Start(psi);
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit(120000);

            return new PythonResult
            {
                ExitCode = proc.ExitCode,
                Output = stdout,
                Error = stderr,
                Success = proc.ExitCode == 0
            };
        }

        public PythonResult UninstallPackage(string packageName)
        {
            var psi = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = "-m pip uninstall -y " + packageName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var proc = Process.Start(psi);
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit(60000);

            return new PythonResult
            {
                ExitCode = proc.ExitCode,
                Output = stdout,
                Error = stderr,
                Success = proc.ExitCode == 0
            };
        }

        public string ListPackages()
        {
            var psi = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = "-m pip list --format=columns",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var proc = Process.Start(psi);
            string stdout = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(30000);
            return stdout;
        }
    }

    public class PythonResult
    {
        public int ExitCode { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
        public bool Success { get; set; }
    }
}
