using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DataScienceWorkbench
{
    public class PythonRunner
    {
        private string pythonPath;
        private bool pythonAvailable;
        private string pythonError;
        private string pythonVersion;

        public bool PythonAvailable { get { return pythonAvailable; } }
        public string PythonError { get { return pythonError; } }
        public string PythonVersion { get { return pythonVersion; } }

        public PythonRunner()
        {
            pythonPath = FindPython();
            ValidatePython();
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

            return null;
        }

        private void ValidatePython()
        {
            if (string.IsNullOrEmpty(pythonPath))
            {
                pythonAvailable = false;
                pythonError = "Python installation not found. Please install Python 3.x and ensure it is on your system PATH.";
                return;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var proc = Process.Start(psi);
                string stdout = proc.StandardOutput.ReadToEnd().Trim();
                string stderr = proc.StandardError.ReadToEnd().Trim();
                bool exited = proc.WaitForExit(10000);

                if (!exited)
                {
                    try { proc.Kill(); } catch { }
                    pythonAvailable = false;
                    pythonError = "Python at '" + pythonPath + "' did not respond within 10 seconds.";
                    return;
                }

                if (proc.ExitCode != 0)
                {
                    pythonAvailable = false;
                    pythonError = "Python was found at '" + pythonPath + "' but failed to execute (exit code " + proc.ExitCode + ").";
                    return;
                }

                string versionOutput = !string.IsNullOrEmpty(stdout) ? stdout : stderr;
                pythonVersion = versionOutput;
                pythonAvailable = true;
                pythonError = null;
            }
            catch (Win32Exception)
            {
                pythonAvailable = false;
                pythonError = "Python was expected at '" + pythonPath + "' but the file was not found or could not be executed.";
            }
            catch (Exception ex)
            {
                pythonAvailable = false;
                pythonError = "Failed to verify Python installation: " + ex.Message;
            }
        }

        public string GetPythonPath() { return pythonPath ?? "(not found)"; }

        private PythonResult CreateUnavailableResult(string operation)
        {
            return new PythonResult
            {
                ExitCode = -1,
                Output = "",
                Error = "Python is not available — cannot " + operation + ".\n" + (pythonError ?? "Unknown error."),
                Success = false
            };
        }

        private PythonResult CreateProcessErrorResult(string operation, Exception ex)
        {
            string message;
            if (ex is Win32Exception)
                message = "Python executable not found or cannot be started. Verify your Python installation.\nPath: " + (pythonPath ?? "(not set)");
            else if (ex is InvalidOperationException)
                message = "Failed to start the Python process: " + ex.Message;
            else
                message = "Unexpected error while trying to " + operation + ": " + ex.Message;

            return new PythonResult
            {
                ExitCode = -1,
                Output = "",
                Error = message,
                Success = false
            };
        }

        public PythonResult Execute(string script, Dictionary<string, string> inMemoryData, string preamble = null)
        {
            if (!pythonAvailable)
                return CreateUnavailableResult("run script");

            bool hasMemData = inMemoryData != null && inMemoryData.Count > 0;
            bool hasPreamble = !string.IsNullOrEmpty(preamble);

            string fullScript;
            var sb = new StringBuilder();

            if (hasMemData)
            {
                sb.AppendLine("import sys, io, pandas as pd");
                sb.AppendLine("class _DotNetDataset:");
                sb.AppendLine("    def __init__(self, df):");
                sb.AppendLine("        object.__setattr__(self, '_df', df)");
                sb.AppendLine("    def __getattr__(self, name):");
                sb.AppendLine("        _df = object.__getattribute__(self, '_df')");
                sb.AppendLine("        if name in _df.columns:");
                sb.AppendLine("            return _df[name]");
                sb.AppendLine("        return getattr(_df, name)");
                sb.AppendLine("    def __repr__(self):");
                sb.AppendLine("        return repr(object.__getattribute__(self, '_df'))");
                sb.AppendLine("    def __len__(self):");
                sb.AppendLine("        return len(object.__getattribute__(self, '_df'))");
                sb.AppendLine("    def __getitem__(self, key):");
                sb.AppendLine("        return object.__getattribute__(self, '_df')[key]");
                sb.AppendLine("    @property");
                sb.AppendLine("    def df(self):");
                sb.AppendLine("        return object.__getattribute__(self, '_df')");
                sb.AppendLine("while True:");
                sb.AppendLine("    _hdr = sys.stdin.readline().rstrip('\\n')");
                sb.AppendLine("    if _hdr == '__DONE__': break");
                sb.AppendLine("    if _hdr.startswith('__DATASET__||'):");
                sb.AppendLine("        _parts = _hdr.split('||')");
                sb.AppendLine("        _name = _parts[1]");
                sb.AppendLine("        _nlines = int(_parts[2])");
                sb.AppendLine("        _lines = []");
                sb.AppendLine("        for _ in range(_nlines):");
                sb.AppendLine("            _lines.append(sys.stdin.readline())");
                sb.AppendLine("        globals()[_name] = _DotNetDataset(pd.read_csv(io.StringIO(''.join(_lines))))");
                sb.AppendLine("del _DotNetDataset");
                sb.AppendLine();
            }

            if (hasPreamble)
            {
                sb.AppendLine(preamble);
            }

            if (hasMemData || hasPreamble)
            {
                sb.AppendLine(script);
                fullScript = sb.ToString();
            }
            else
            {
                fullScript = script;
            }

            string tempScript = Path.GetTempFileName() + ".py";
            File.WriteAllText(tempScript, fullScript);

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = "\"" + tempScript + "\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = hasMemData,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                psi.EnvironmentVariables["MPLBACKEND"] = "Agg";

                string displayVar = Environment.GetEnvironmentVariable("DISPLAY");
                if (!string.IsNullOrEmpty(displayVar))
                    psi.EnvironmentVariables["DISPLAY"] = displayVar;

                var proc = Process.Start(psi);

                if (hasMemData)
                {
                    foreach (var kvp in inMemoryData)
                    {
                        string csvData = kvp.Value;
                        string[] csvLines = csvData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        proc.StandardInput.WriteLine("__DATASET__||" + kvp.Key + "||" + csvLines.Length);
                        foreach (var line in csvLines)
                            proc.StandardInput.WriteLine(line);
                    }
                    proc.StandardInput.WriteLine("__DONE__");
                    proc.StandardInput.Flush();
                    proc.StandardInput.Close();
                }

                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();
                bool exited = proc.WaitForExit(60000);

                if (!exited)
                {
                    try { proc.Kill(); } catch { }
                    return new PythonResult
                    {
                        ExitCode = -1,
                        Output = stdout,
                        Error = "Script execution timed out after 60 seconds and was terminated.",
                        Success = false
                    };
                }

                var plotPaths = new List<string>();
                var outputLines = stdout.Split('\n');
                var filteredLines = new List<string>();
                foreach (var line in outputLines)
                {
                    string trimmed = line.TrimEnd('\r');
                    if (trimmed.StartsWith("__PLOT__:"))
                    {
                        string path = trimmed.Substring(9).Trim();
                        if (File.Exists(path))
                            plotPaths.Add(path);
                    }
                    else
                    {
                        filteredLines.Add(trimmed);
                    }
                }

                string cleanOutput = string.Join("\n", filteredLines);
                cleanOutput = cleanOutput.TrimEnd('\n', '\r');
                if (cleanOutput.Length > 0)
                    cleanOutput += "\n";

                return new PythonResult
                {
                    ExitCode = proc.ExitCode,
                    Output = cleanOutput,
                    Error = stderr,
                    Success = proc.ExitCode == 0,
                    PlotPaths = plotPaths
                };
            }
            catch (Exception ex)
            {
                return CreateProcessErrorResult("run script", ex);
            }
            finally
            {
                try { File.Delete(tempScript); } catch { }
            }
        }

        public PythonResult InstallPackage(string packageName)
        {
            if (!pythonAvailable)
                return CreateUnavailableResult("install package '" + packageName + "'");

            try
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
                bool exited = proc.WaitForExit(120000);

                if (!exited)
                {
                    try { proc.Kill(); } catch { }
                    return new PythonResult
                    {
                        ExitCode = -1,
                        Output = stdout,
                        Error = "Package installation timed out after 120 seconds.",
                        Success = false
                    };
                }

                return new PythonResult
                {
                    ExitCode = proc.ExitCode,
                    Output = stdout,
                    Error = stderr,
                    Success = proc.ExitCode == 0
                };
            }
            catch (Exception ex)
            {
                return CreateProcessErrorResult("install package", ex);
            }
        }

        public PythonResult UninstallPackage(string packageName)
        {
            if (!pythonAvailable)
                return CreateUnavailableResult("uninstall package '" + packageName + "'");

            try
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
                bool exited = proc.WaitForExit(60000);

                if (!exited)
                {
                    try { proc.Kill(); } catch { }
                    return new PythonResult
                    {
                        ExitCode = -1,
                        Output = stdout,
                        Error = "Package uninstall timed out after 60 seconds.",
                        Success = false
                    };
                }

                return new PythonResult
                {
                    ExitCode = proc.ExitCode,
                    Output = stdout,
                    Error = stderr,
                    Success = proc.ExitCode == 0
                };
            }
            catch (Exception ex)
            {
                return CreateProcessErrorResult("uninstall package", ex);
            }
        }

        public PythonResult CheckSyntax(string script)
        {
            if (!pythonAvailable)
                return CreateUnavailableResult("check syntax");

            string scriptFile = Path.GetTempFileName() + "_src.py";
            File.WriteAllText(scriptFile, script);

            string checkFile = Path.GetTempFileName() + ".py";
            File.WriteAllText(checkFile,
                "import ast, sys\n" +
                "try:\n" +
                "    with open(r'" + scriptFile.Replace("'", "\\'") + "', 'r') as f:\n" +
                "        source = f.read()\n" +
                "    ast.parse(source)\n" +
                "    print('Syntax OK')\n" +
                "except SyntaxError as e:\n" +
                "    print(f'Line {e.lineno}: {e.msg}', file=sys.stderr)\n" +
                "    sys.exit(1)\n");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = "\"" + checkFile + "\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var proc = Process.Start(psi);
                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();
                bool exited = proc.WaitForExit(10000);

                if (!exited)
                {
                    try { proc.Kill(); } catch { }
                    return new PythonResult
                    {
                        ExitCode = -1,
                        Output = "",
                        Error = "Syntax check timed out.",
                        Success = false
                    };
                }

                return new PythonResult
                {
                    ExitCode = proc.ExitCode,
                    Output = stdout,
                    Error = stderr,
                    Success = proc.ExitCode == 0
                };
            }
            catch (Exception ex)
            {
                return CreateProcessErrorResult("check syntax", ex);
            }
            finally
            {
                try { File.Delete(scriptFile); } catch { }
                try { File.Delete(checkFile); } catch { }
            }
        }

        public PythonResult ListPackages()
        {
            if (!pythonAvailable)
                return CreateUnavailableResult("list packages");

            try
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
                string stderr = proc.StandardError.ReadToEnd();
                bool exited = proc.WaitForExit(30000);

                if (!exited)
                {
                    try { proc.Kill(); } catch { }
                    return new PythonResult
                    {
                        ExitCode = -1,
                        Output = "",
                        Error = "Package listing timed out.",
                        Success = false
                    };
                }

                return new PythonResult
                {
                    ExitCode = proc.ExitCode,
                    Output = stdout,
                    Error = stderr,
                    Success = proc.ExitCode == 0
                };
            }
            catch (Exception ex)
            {
                return CreateProcessErrorResult("list packages", ex);
            }
        }
    }

    public class PythonResult
    {
        public int ExitCode { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
        public bool Success { get; set; }
        public List<string> PlotPaths { get; set; } = new List<string>();
    }
}
