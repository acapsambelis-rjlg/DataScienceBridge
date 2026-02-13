using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DataScienceWorkbench
{
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

        public PythonResult Execute(string script, Dictionary<string, string> inMemoryData, string preamble = null)
        {
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

        public PythonResult CheckSyntax(string script)
        {
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
                proc.WaitForExit(10000);

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
                try { File.Delete(scriptFile); } catch { }
                try { File.Delete(checkFile); } catch { }
            }
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
