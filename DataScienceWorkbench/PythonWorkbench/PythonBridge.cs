using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using RJLG.IntelliSEM.Data.PythonDataScience;

namespace RJLG.IntelliSEM.UI.Controls.PythonDataScience
{
    public class PythonRunner
    {
        private static string _venvRelativePath = Path.Combine("python", "venv");

        public static string VenvRelativePath
        {
            get { return _venvRelativePath; }
            set { _venvRelativePath = value; }
        }

        private string systemPythonPath;
        private string pythonPath;
        private bool pythonAvailable;
        private string pythonError;
        private string pythonVersion;
        private string venvPath;
        private string tempPath;
        private bool venvReady;
        private string venvError;

        public bool PythonAvailable { get { return pythonAvailable; } }
        public string PythonError { get { return pythonError; } }
        public string PythonVersion { get { return pythonVersion; } }
        public string VenvPath { get { return venvPath; } }
        public bool VenvReady { get { return venvReady; } }
        public string VenvError { get { return venvError; } }

        public event Action<string> SetupProgress;

        public PythonRunner()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            venvPath = Path.Combine(appDir, _venvRelativePath);
            tempPath = Path.Combine(Path.GetDirectoryName(venvPath), "temp");

            systemPythonPath = FindPython();
            pythonPath = systemPythonPath;
            ValidatePython();
        }

        public void EnsureVenv()
        {
            if (!pythonAvailable)
            {
                venvReady = false;
                venvError = "Cannot create virtual environment: " + pythonError;
                return;
            }

            try
            {
                Directory.CreateDirectory(venvPath);
                Directory.CreateDirectory(tempPath);
            }
            catch (Exception ex)
            {
                venvReady = false;
                venvError = "Failed to create python/venv directory: " + ex.Message;
                return;
            }

            string venvPython = GetVenvPythonPath();
            if (File.Exists(venvPython))
            {
                pythonPath = venvPython;
                ValidatePython();

                if (pythonAvailable)
                {
                    venvReady = true;
                    venvError = null;
                    RaiseProgress("Using existing virtual environment.");
                    return;
                }
                else
                {
                    pythonPath = systemPythonPath;
                    venvReady = false;
                    venvError = "Existing virtual environment Python is not working: " + pythonError;
                    ValidatePython();
                    return;
                }
            }

            RaiseProgress("Creating virtual environment...");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = systemPythonPath,
                    Arguments = "-m venv \"" + venvPath + "\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var proc = Process.Start(psi);
                string _discard, stderr;
                ReadProcessOutputs(proc, out _discard, out stderr, 120000);
                bool exited = proc.WaitForExit(120000);

                if (!exited)
                {
                    try { proc.Kill(); } catch { }
                    venvReady = false;
                    venvError = "Virtual environment creation timed out after 120 seconds.";
                    return;
                }

                if (proc.ExitCode != 0)
                {
                    venvReady = false;
                    venvError = "Failed to create virtual environment: " + stderr;
                    return;
                }

                if (!File.Exists(venvPython))
                {
                    venvReady = false;
                    venvError = "Virtual environment was created but Python executable not found at expected location.";
                    return;
                }

                pythonPath = venvPython;
                venvReady = true;
                venvError = null;
                ValidatePython();

                if (!pythonAvailable)
                {
                    pythonPath = systemPythonPath;
                    venvReady = false;
                    venvError = "Virtual environment was created but its Python could not be validated: " + pythonError;
                    ValidatePython();
                    return;
                }

                RaiseProgress("Virtual environment created. Installing base packages...");
                InstallBasePackages();
                RaiseProgress("Virtual environment ready.");
            }
            catch (Exception ex)
            {
                venvReady = false;
                venvError = "Error creating virtual environment: " + ex.Message;
            }
        }

        private string GetVenvPythonPath()
        {
            bool isWindows = IsWindows();
            if (isWindows)
                return Path.Combine(venvPath, "Scripts", "python.exe");

            string python3 = Path.Combine(venvPath, "bin", "python3");
            if (File.Exists(python3))
                return python3;
            return Path.Combine(venvPath, "bin", "python");
        }

        private void InstallBasePackages()
        {
            string[] basePackages = { "pandas", "numpy", "matplotlib", "scikit-learn" };
            foreach (var pkg in basePackages)
            {
                RaiseProgress("Installing " + pkg + "...");
                var result = InstallPackage(pkg);
                if (!result.Success)
                    RaiseProgress("Warning: Failed to install " + pkg + ": " + result.Error);
            }
        }

        public PythonResult ResetEnvironment()
        {
            try
            {
                pythonPath = systemPythonPath;

                if (Directory.Exists(venvPath))
                {
                    try
                    {
                        Directory.Delete(venvPath, true);
                    }
                    catch (Exception ex)
                    {
                        return new PythonResult
                        {
                            ExitCode = -1,
                            Output = "",
                            Error = "Failed to delete existing environment: " + ex.Message,
                            Success = false
                        };
                    }
                }

                venvReady = false;
                venvError = null;
                EnsureVenv();

                if (venvReady)
                {
                    return new PythonResult
                    {
                        ExitCode = 0,
                        Output = "Virtual environment has been reset successfully.\n",
                        Error = "",
                        Success = true
                    };
                }
                else
                {
                    return new PythonResult
                    {
                        ExitCode = -1,
                        Output = "",
                        Error = "Environment was deleted but recreation failed: " + (venvError ?? "Unknown error"),
                        Success = false
                    };
                }
            }
            catch (Exception ex)
            {
                return new PythonResult
                {
                    ExitCode = -1,
                    Output = "",
                    Error = "Error resetting environment: " + ex.Message,
                    Success = false
                };
            }
        }

        private void RaiseProgress(string message)
        {
            if (SetupProgress != null)
                SetupProgress(message);
        }

        private bool IsWindows()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT
                || Environment.OSVersion.Platform == PlatformID.Win32S
                || Environment.OSVersion.Platform == PlatformID.Win32Windows
                || Environment.OSVersion.Platform == PlatformID.WinCE;
        }

        private void ConfigurePipEnvironment(ProcessStartInfo psi)
        {
            if (venvReady)
            {
                psi.EnvironmentVariables["PIP_USER"] = "0";
                psi.EnvironmentVariables["VIRTUAL_ENV"] = venvPath;
            }
        }

        private string FindPython()
        {
            bool isWindows = IsWindows();

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
                string stdout, stderr;
                ReadProcessOutputs(proc, out stdout, out stderr, 10000);
                stdout = (stdout ?? "").Trim();
                stderr = (stderr ?? "").Trim();
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

        private string GetTempFilePath(string suffix)
        {
            if (Directory.Exists(tempPath))
            {
                string name = Guid.NewGuid().ToString("N") + suffix;
                return Path.Combine(tempPath, name);
            }
            return Path.GetTempFileName() + suffix;
        }

        private void CleanTempFiles()
        {
            if (!Directory.Exists(tempPath)) return;
            try
            {
                foreach (var file in Directory.GetFiles(tempPath, "*.py"))
                {
                    try { File.Delete(file); } catch { }
                }
            }
            catch { }
        }

        private static void ReadProcessOutputs(Process proc, out string stdout, out string stderr, int timeoutMs)
        {
            string capturedStderr = null;
            var stderrThread = new Thread(() => { try { capturedStderr = proc.StandardError.ReadToEnd(); } catch { capturedStderr = ""; } });
            stderrThread.IsBackground = true;
            stderrThread.Start();

            stdout = proc.StandardOutput.ReadToEnd();
            stderrThread.Join(timeoutMs > 0 ? timeoutMs : 30000);
            stderr = capturedStderr ?? "";
        }

        private void AppendBootstrapCode(StringBuilder sb, bool hasMemData, bool hasStreamData)
        {
            sb.AppendLine("import sys, io, base64, pandas as pd");
            sb.AppendLine("import numpy as np");
            sb.AppendLine("from PIL import Image as _PILImage");
            sb.AppendLine("import csv as _csv");
            sb.AppendLine("def _decode_img(s):");
            sb.AppendLine("    if s is None or (isinstance(s, float) and s != s): return None");
            sb.AppendLine("    if not isinstance(s, str) or s == '': return None");
            sb.AppendLine("    if not s.startswith('__IMG__:'): return s");
            sb.AppendLine("    try:");
            sb.AppendLine("        b = base64.b64decode(s[7:])");
            sb.AppendLine("        return _PILImage.open(io.BytesIO(b))");
            sb.AppendLine("    except Exception:");
            sb.AppendLine("        return None");
            sb.AppendLine("def _decode_img_columns(df):");
            sb.AppendLine("    for col in df.columns:");
            sb.AppendLine("        first = df[col].dropna().iloc[0] if len(df[col].dropna()) > 0 else None");
            sb.AppendLine("        if isinstance(first, str) and first.startswith('__IMG__:'):");
            sb.AppendLine("            df[col] = df[col].apply(_decode_img)");
            sb.AppendLine("    return df");
            sb.AppendLine("class _DatasetRow:");
            sb.AppendLine("    def __init__(self, series):");
            sb.AppendLine("        object.__setattr__(self, '_s', series)");
            sb.AppendLine("    def __getattr__(self, name):");
            sb.AppendLine("        _s = object.__getattribute__(self, '_s')");
            sb.AppendLine("        if name in _s.index:");
            sb.AppendLine("            return _s[name]");
            sb.AppendLine("        raise AttributeError(f\"Row has no field '{name}'\")");
            sb.AppendLine("    def __repr__(self):");
            sb.AppendLine("        return repr(object.__getattribute__(self, '_s'))");
            sb.AppendLine("    def __dir__(self):");
            sb.AppendLine("        _s = object.__getattribute__(self, '_s')");
            sb.AppendLine("        return list(_s.index)");
            sb.AppendLine("class _IntelliSEMset:");
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
            sb.AppendLine("        _df = object.__getattribute__(self, '_df')");
            sb.AppendLine("        if isinstance(key, (int, slice)):");
            sb.AppendLine("            result = _df.iloc[key]");
            sb.AppendLine("            if isinstance(result, pd.Series):");
            sb.AppendLine("                return _DatasetRow(result)");
            sb.AppendLine("            return _IntelliSEMset(result.reset_index(drop=True))");
            sb.AppendLine("        return _df[key]");
            sb.AppendLine("    def __iter__(self):");
            sb.AppendLine("        _df = object.__getattribute__(self, '_df')");
            sb.AppendLine("        for i in range(len(_df)):");
            sb.AppendLine("            yield _DatasetRow(_df.iloc[i])");
            sb.AppendLine("    @property");
            sb.AppendLine("    def df(self):");
            sb.AppendLine("        return object.__getattribute__(self, '_df')");

            if (hasStreamData)
            {
                sb.AppendLine("class _StreamRow:");
                sb.AppendLine("    __slots__ = ('_data',)");
                sb.AppendLine("    def __init__(self, data):");
                sb.AppendLine("        object.__setattr__(self, '_data', data)");
                sb.AppendLine("    def __getattr__(self, name):");
                sb.AppendLine("        _data = object.__getattribute__(self, '_data')");
                sb.AppendLine("        if name in _data:");
                sb.AppendLine("            return _data[name]");
                sb.AppendLine("        raise AttributeError(f\"Row has no field '{name}'\")");
                sb.AppendLine("    def __getitem__(self, key):");
                sb.AppendLine("        return object.__getattribute__(self, '_data')[key]");
                sb.AppendLine("    def __repr__(self):");
                sb.AppendLine("        return repr(object.__getattribute__(self, '_data'))");
                sb.AppendLine("    def __dir__(self):");
                sb.AppendLine("        return list(object.__getattribute__(self, '_data').keys())");
                sb.AppendLine("class _DotNetStream:");
                sb.AppendLine("    def __init__(self, name, columns):");
                sb.AppendLine("        object.__setattr__(self, '_name', name)");
                sb.AppendLine("        object.__setattr__(self, '_columns', columns)");
                sb.AppendLine("        object.__setattr__(self, '_consumed', False)");
                sb.AppendLine("    @property");
                sb.AppendLine("    def columns(self):");
                sb.AppendLine("        return list(object.__getattribute__(self, '_columns'))");
                sb.AppendLine("    @property");
                sb.AppendLine("    def name(self):");
                sb.AppendLine("        return object.__getattribute__(self, '_name')");
                sb.AppendLine("    def __repr__(self):");
                sb.AppendLine("        _n = object.__getattribute__(self, '_name')");
                sb.AppendLine("        _c = object.__getattribute__(self, '_consumed')");
                sb.AppendLine("        return f'DotNetStream({_n}, columns={self.columns}, consumed={_c})'");
                sb.AppendLine("    def __iter__(self):");
                sb.AppendLine("        if object.__getattribute__(self, '_consumed'):");
                sb.AppendLine("            raise RuntimeError('Stream has already been consumed. Re-register the data source for another pass.')");
                sb.AppendLine("        object.__setattr__(self, '_consumed', True)");
                sb.AppendLine("        _cols = object.__getattribute__(self, '_columns')");
                sb.AppendLine("        while True:");
                sb.AppendLine("            _raw = sys.stdin.readline()");
                sb.AppendLine("            if not _raw:");
                sb.AppendLine("                break");
                sb.AppendLine("            _raw = _raw.rstrip('\\n')");
                sb.AppendLine("            if _raw == '__STREAM_END__':");
                sb.AppendLine("                break");
                sb.AppendLine("            _row_vals = list(_csv.reader([_raw]))[0]");
                sb.AppendLine("            _data = {}");
                sb.AppendLine("            for _i, _col in enumerate(_cols):");
                sb.AppendLine("                _v = _row_vals[_i] if _i < len(_row_vals) else ''");
                sb.AppendLine("                if isinstance(_v, str) and _v.startswith('__IMG__:'):");
                sb.AppendLine("                    _v = _decode_img(_v)");
                sb.AppendLine("                _data[_col] = _v");
                sb.AppendLine("            yield _StreamRow(_data)");
            }

            sb.AppendLine("import types as _types");
            sb.AppendLine("_dotnet_mod = _types.ModuleType('IntelliSEM')");
            sb.AppendLine("_dotnet_mod.__doc__ = 'Datasets piped from the .NET host application.'");
            sb.AppendLine("_dotnet_mod.__all__ = []");
            sb.AppendLine("sys.modules['IntelliSEM'] = _dotnet_mod");
            sb.AppendLine("while True:");
            sb.AppendLine("    _hdr = sys.stdin.readline().rstrip('\\n')");
            sb.AppendLine("    if _hdr == '__DONE__': break");
            sb.AppendLine("    if _hdr.startswith('__DATASET__||'):");
            sb.AppendLine("        _parts = _hdr.split('||')");
            sb.AppendLine("        _name = _parts[1]");
            sb.AppendLine("        _nlines = int(_parts[2])");
            sb.AppendLine("        _img_cols = _parts[3].split(',') if len(_parts) > 3 and _parts[3] else []");
            sb.AppendLine("        _lines = []");
            sb.AppendLine("        for _ in range(_nlines):");
            sb.AppendLine("            _lines.append(sys.stdin.readline())");
            sb.AppendLine("        _tmpdf = pd.read_csv(io.StringIO(''.join(_lines)))");
            sb.AppendLine("        if _img_cols:");
            sb.AppendLine("            for _ic in _img_cols:");
            sb.AppendLine("                if _ic in _tmpdf.columns:");
            sb.AppendLine("                    _tmpdf[_ic] = _tmpdf[_ic].apply(_decode_img)");
            sb.AppendLine("        else:");
            sb.AppendLine("            _tmpdf = _decode_img_columns(_tmpdf)");
            sb.AppendLine("        setattr(_dotnet_mod, _name, _IntelliSEMset(_tmpdf))");

            sb.AppendLine("        _dotnet_mod.__all__.append(_name)");

            if (hasStreamData)
            {
                sb.AppendLine("    if _hdr.startswith('__STREAM__||'):");
                sb.AppendLine("        _parts = _hdr.split('||')");
                sb.AppendLine("        _sname = _parts[1]");
                sb.AppendLine("        _scols = _parts[2].split(',')");
                sb.AppendLine("        setattr(_dotnet_mod, _sname, _DotNetStream(_sname, _scols))");
                sb.AppendLine("        _dotnet_mod.__all__.append(_sname)");
            }

            AppendHelperFunctions(sb);

            sb.AppendLine("del _types, _dotnet_mod");
            sb.AppendLine();
        }

        private static List<string> _builtInHelperNames;

        public static string[] BuiltInHelperNames
        {
            get
            {
                if (_builtInHelperNames == null)
                    _builtInHelperNames = LoadHelperNames();
                return _builtInHelperNames.ToArray();
            }
        }

        private static List<string> LoadHelperNames()
        {
            var names = new List<string>();
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            foreach (var resName in assembly.GetManifestResourceNames())
            {
                if (!resName.EndsWith(".py")) continue;
                using (var stream = assembly.GetManifestResourceStream(resName))
                using (var reader = new System.IO.StreamReader(stream))
                {
                    string firstLine = reader.ReadLine();
                    if (firstLine != null && firstLine.StartsWith("# exports:"))
                    {
                        foreach (var name in firstLine.Substring(10).Split(','))
                        {
                            string trimmed = name.Trim();
                            if (trimmed.Length > 0)
                                names.Add(trimmed);
                        }
                    }
                }
            }
            return names;
        }

        private void AppendHelperFunctions(StringBuilder sb)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var exports = new List<string>();

            foreach (var resName in assembly.GetManifestResourceNames())
            {
                if (!resName.EndsWith(".py")) continue;
                string code;
                using (var stream = assembly.GetManifestResourceStream(resName))
                using (var reader = new System.IO.StreamReader(stream))
                    code = reader.ReadToEnd();

                string firstLine = code.Substring(0, code.IndexOf('\n')).Trim();
                if (!firstLine.StartsWith("# exports:")) continue;

                foreach (var name in firstLine.Substring(10).Split(','))
                {
                    string trimmed = name.Trim();
                    if (trimmed.Length > 0)
                        exports.Add(trimmed);
                }

                sb.AppendLine(code);
            }

            foreach (var name in exports)
            {
                sb.AppendLine("setattr(_dotnet_mod, '" + name + "', " + name + ")");
                sb.AppendLine("_dotnet_mod.__all__.append('" + name + "')");
            }
        }

        private PythonResult CreateUnavailableResult(string operation)
        {
            return new PythonResult
            {
                ExitCode = -1,
                Output = "",
                Error = "Python is not available \u2014 cannot " + operation + ".\n" + (pythonError ?? "Unknown error."),
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

        public PythonResult Execute(string script, Dictionary<string, IInMemoryDataSource> inMemoryData, string preamble = null)
        {
            return Execute(script, inMemoryData, null, preamble);
        }

        private string BuildFullScript(string script, Dictionary<string, IInMemoryDataSource> inMemoryData,
            Dictionary<string, IStreamingDataSource> streamingData, string preamble, out int preambleLineCount)
        {
            bool hasMemData = inMemoryData != null && inMemoryData.Count > 0;
            bool hasStreamData = streamingData != null && streamingData.Count > 0;
            bool hasPreamble = !string.IsNullOrEmpty(preamble);

            if (!hasMemData && !hasStreamData && !hasPreamble)
            {
                preambleLineCount = 0;
                return script;
            }

            var sb = new StringBuilder();
            if (hasMemData || hasStreamData)
                AppendBootstrapCode(sb, hasMemData, hasStreamData);
            if (hasPreamble)
                sb.AppendLine(preamble);
            preambleLineCount = CountLines(sb.ToString());
            sb.AppendLine(script);
            return sb.ToString();
        }

        private static int CountLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n') count++;
            }
            if (text.Length > 0 && text[text.Length - 1] != '\n') count++;
            return count;
        }

        private static string RemapErrorLineNumbers(string stderr, int preambleLineCount, string tempScriptPath)
        {
            if (string.IsNullOrEmpty(stderr) || (preambleLineCount == 0 && string.IsNullOrEmpty(tempScriptPath)))
                return stderr;

            var lines = stderr.Split('\n');
            var result = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd('\r');
                bool isScriptLine = !string.IsNullOrEmpty(tempScriptPath) && line.Contains(tempScriptPath);

                if (isScriptLine)
                    line = line.Replace(tempScriptPath, "<script>");

                if (preambleLineCount > 0 && isScriptLine)
                {
                    int lineMarker = line.IndexOf(", line ", StringComparison.Ordinal);
                    if (lineMarker >= 0)
                    {
                        int numStart = lineMarker + 7;
                        int numEnd = numStart;
                        while (numEnd < line.Length && char.IsDigit(line[numEnd]))
                            numEnd++;
                        if (numEnd > numStart)
                        {
                            int origLine;
                            if (int.TryParse(line.Substring(numStart, numEnd - numStart), out origLine))
                            {
                                int adjusted = origLine - preambleLineCount;
                                if (adjusted < 1) adjusted = origLine;
                                line = line.Substring(0, numStart) + adjusted + line.Substring(numEnd);
                            }
                        }
                    }
                }

                result.Append(line);
                if (i < lines.Length - 1)
                    result.Append('\n');
            }
            return result.ToString();
        }

        private void ConfigureProcessEnvironment(ProcessStartInfo psi)
        {
            psi.EnvironmentVariables["MPLBACKEND"] = "Agg";
            string displayVar = Environment.GetEnvironmentVariable("DISPLAY");
            if (!string.IsNullOrEmpty(displayVar))
                psi.EnvironmentVariables["DISPLAY"] = displayVar;
        }

        private void PipeDataToStdin(Process proc, Dictionary<string, IInMemoryDataSource> inMemoryData,
            Dictionary<string, IStreamingDataSource> streamingData)
        {
            bool hasMemData = inMemoryData != null && inMemoryData.Count > 0;
            bool hasStreamData = streamingData != null && streamingData.Count > 0;
            if (!hasMemData && !hasStreamData) return;

            if (hasMemData)
            {
                foreach (var kvp in inMemoryData)
                {
                    var source = kvp.Value;
                    var imgCols = source.GetImageColumnNames();
                    string header = "__DATASET__||" + kvp.Key + "||" + source.LineCount;
                    if (imgCols != null && imgCols.Length > 0)
                        header += "||" + string.Join(",", imgCols);
                    proc.StandardInput.WriteLine(header);
                    foreach (var line in source.StreamCsvLines())
                        proc.StandardInput.WriteLine(line);
                }
            }
            if (hasStreamData)
            {
                foreach (var kvp in streamingData)
                    proc.StandardInput.WriteLine("__STREAM__||" + kvp.Key + "||" + kvp.Value.GetCsvHeader());
            }
            proc.StandardInput.WriteLine("__DONE__");
            proc.StandardInput.Flush();

            if (hasStreamData)
            {
                foreach (var kvp in streamingData)
                {
                    foreach (var line in kvp.Value.StreamCsvRows())
                        proc.StandardInput.WriteLine(line);
                    proc.StandardInput.WriteLine("__STREAM_END__");
                }
                proc.StandardInput.Flush();
            }
        }

        private static void FilterPlotPaths(string stdout, out string cleanOutput, out List<string> plotPaths)
        {
            plotPaths = new List<string>();
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
            cleanOutput = string.Join("\n", filteredLines).TrimEnd('\n', '\r');
            if (cleanOutput.Length > 0)
                cleanOutput += "\n";
        }

        private void ProcessStreamingLine(string line, List<string> plotPaths, Action<string> onOutputLine)
        {
            if (line.StartsWith("__PLOT__:"))
            {
                string path = line.Substring(9).Trim();
                if (File.Exists(path))
                    plotPaths.Add(path);
            }
            else if (onOutputLine != null)
            {
                onOutputLine(line + "\n");
            }
        }

        private static bool CouldBePlotMarker(string partial)
        {
            return partial.StartsWith("__PLOT__:") ||
                (partial.Length < 9 && "__PLOT__:".StartsWith(partial));
        }

        public PythonResult Execute(string script, Dictionary<string, IInMemoryDataSource> inMemoryData, Dictionary<string, IStreamingDataSource> streamingData, string preamble = null)
        {
            if (!pythonAvailable)
                return CreateUnavailableResult("run script");

            bool hasData = (inMemoryData != null && inMemoryData.Count > 0) ||
                           (streamingData != null && streamingData.Count > 0);

            int preambleLines;
            string fullScript = BuildFullScript(script, inMemoryData, streamingData, preamble, out preambleLines);
            string tempScript = GetTempFilePath(".py");
            File.WriteAllText(tempScript, fullScript);

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = "\"" + tempScript + "\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = hasData,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                ConfigureProcessEnvironment(psi);

                var proc = Process.Start(psi);

                if (hasData)
                {
                    PipeDataToStdin(proc, inMemoryData, streamingData);
                    proc.StandardInput.Close();
                }

                string stdout, stderr;
                ReadProcessOutputs(proc, out stdout, out stderr, 60000);
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

                string cleanOutput;
                List<string> plotPaths;
                FilterPlotPaths(stdout, out cleanOutput, out plotPaths);

                return new PythonResult
                {
                    ExitCode = proc.ExitCode,
                    Output = cleanOutput,
                    Error = RemapErrorLineNumbers(stderr, preambleLines, tempScript),
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

        private Process _runningProcess;
        private readonly object _processLock = new object();

        public bool IsScriptRunning
        {
            get { lock (_processLock) return _runningProcess != null; }
        }

        public void ExecuteAsync(string script, Dictionary<string, IInMemoryDataSource> inMemoryData, Dictionary<string, IStreamingDataSource> streamingData, string preamble,
            Action<string> onOutputLine, Action<string> onErrorLine, Action<PythonResult> onComplete,
            string scriptArguments = null, string inputFilePath = null)
        {
            if (!pythonAvailable)
            {
                if (onComplete != null)
                    onComplete(CreateUnavailableResult("run script"));
                return;
            }

            int preambleLines;
            string fullScript = BuildFullScript(script, inMemoryData, streamingData, preamble, out preambleLines);
            string tempScript = GetTempFilePath(".py");
            File.WriteAllText(tempScript, fullScript);

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = "-u \"" + tempScript + "\"" + (string.IsNullOrEmpty(scriptArguments) ? "" : " " + scriptArguments),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                ConfigureProcessEnvironment(psi);
                psi.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";

                var proc = Process.Start(psi);

                lock (_processLock)
                {
                    _runningProcess = proc;
                }

                var plotPaths = new List<string>();
                var stderrBuilder = new StringBuilder();
                int capturedPreambleLines = preambleLines;
                string capturedTempScript = tempScript;

                proc.ErrorDataReceived += (s, ev) =>
                {
                    if (ev.Data != null)
                    {
                        string remapped = RemapErrorLineNumbers(ev.Data, capturedPreambleLines, capturedTempScript);
                        stderrBuilder.AppendLine(remapped);
                        if (onErrorLine != null)
                            onErrorLine(remapped);
                    }
                };
                proc.BeginErrorReadLine();

                string[] inputFileLines = null;
                if (!string.IsNullOrEmpty(inputFilePath) && File.Exists(inputFilePath))
                {
                    try { inputFileLines = File.ReadAllLines(inputFilePath); }
                    catch { inputFileLines = null; }
                }

                bool hasData = (inMemoryData != null && inMemoryData.Count > 0) ||
                               (streamingData != null && streamingData.Count > 0);
                var capturedInputLines = (inputFileLines != null && inputFileLines.Length > 0) ? inputFileLines : null;

                var stdinThread = new Thread(() =>
                {
                    try
                    {
                        if (hasData)
                            PipeDataToStdin(proc, inMemoryData, streamingData);

                        if (capturedInputLines != null)
                        {
                            Thread.Sleep(100);
                            foreach (var line in capturedInputLines)
                            {
                                try
                                {
                                    proc.StandardInput.WriteLine(line);
                                    proc.StandardInput.Flush();
                                }
                                catch { break; }
                            }
                        }
                        else if (hasData)
                        {
                            try { proc.StandardInput.Close(); } catch { }
                        }
                    }
                    catch { }
                });
                stdinThread.IsBackground = true;
                stdinThread.Start();

                var worker = new BackgroundWorker();
                worker.DoWork += (s, ev) =>
                {
                    try
                    {
                        char[] buffer = new char[1024];
                        var lineBuilder = new StringBuilder();

                        while (true)
                        {
                            int count = proc.StandardOutput.Read(buffer, 0, buffer.Length);
                            if (count <= 0) break;

                            for (int i = 0; i < count; i++)
                            {
                                char c = buffer[i];
                                if (c == '\n')
                                {
                                    string line = lineBuilder.ToString().TrimEnd('\r');
                                    ProcessStreamingLine(line, plotPaths, onOutputLine);
                                    lineBuilder.Clear();
                                }
                                else
                                {
                                    lineBuilder.Append(c);
                                }
                            }

                            if (lineBuilder.Length > 0 && onOutputLine != null)
                            {
                                string partial = lineBuilder.ToString();
                                if (!CouldBePlotMarker(partial))
                                {
                                    onOutputLine(partial);
                                    lineBuilder.Clear();
                                }
                            }
                        }

                        if (lineBuilder.Length > 0)
                        {
                            string remaining = lineBuilder.ToString().TrimEnd('\r');
                            if (remaining.StartsWith("__PLOT__:"))
                            {
                                string path = remaining.Substring(9).Trim();
                                if (File.Exists(path))
                                    plotPaths.Add(path);
                            }
                            else if (onOutputLine != null)
                            {
                                onOutputLine(remaining);
                            }
                        }

                        proc.WaitForExit();

                        ev.Result = new PythonResult
                        {
                            ExitCode = proc.ExitCode,
                            Output = "",
                            Error = stderrBuilder.ToString(),
                            Success = proc.ExitCode == 0,
                            PlotPaths = plotPaths
                        };
                    }
                    catch (Exception ex)
                    {
                        ev.Result = CreateProcessErrorResult("run script", ex);
                    }
                };
                worker.RunWorkerCompleted += (s, ev) =>
                {
                    lock (_processLock)
                    {
                        _runningProcess = null;
                    }
                    try { File.Delete(tempScript); } catch { }

                    if (onComplete != null)
                    {
                        var result = ev.Result as PythonResult;
                        if (result == null)
                            result = CreateProcessErrorResult("run script", ev.Error ?? new Exception("Unknown error"));
                        onComplete(result);
                    }
                };
                worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                lock (_processLock)
                {
                    _runningProcess = null;
                }
                try { File.Delete(tempScript); } catch { }
                if (onComplete != null)
                    onComplete(CreateProcessErrorResult("run script", ex));
            }
        }

        public void SendInput(string line)
        {
            lock (_processLock)
            {
                if (_runningProcess != null)
                {
                    try
                    {
                        _runningProcess.StandardInput.WriteLine(line);
                        _runningProcess.StandardInput.Flush();
                    }
                    catch { }
                }
            }
        }

        public void CancelExecution()
        {
            lock (_processLock)
            {
                if (_runningProcess != null)
                {
                    try { _runningProcess.Kill(); } catch { }
                }
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
                    Arguments = "-m pip install " + packageName,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                ConfigurePipEnvironment(psi);

                var proc = Process.Start(psi);
                string stdout, stderr;
                ReadProcessOutputs(proc, out stdout, out stderr, 120000);
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
                ConfigurePipEnvironment(psi);

                var proc = Process.Start(psi);
                string stdout, stderr;
                ReadProcessOutputs(proc, out stdout, out stderr, 60000);
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

        public void InstallPackageAsync(string packageName, Action<string> onOutput, Action<PythonResult> onComplete)
        {
            if (!pythonAvailable)
            {
                onComplete(CreateUnavailableResult("install package '" + packageName + "'"));
                return;
            }

            RunPipAsync("-m pip install --progress-bar off " + packageName, "install package", 300000, onOutput, onComplete);
        }

        public void UninstallPackageAsync(string packageName, Action<string> onOutput, Action<PythonResult> onComplete)
        {
            if (!pythonAvailable)
            {
                onComplete(CreateUnavailableResult("uninstall package '" + packageName + "'"));
                return;
            }

            RunPipAsync("-m pip uninstall -y " + packageName, "uninstall package", 120000, onOutput, onComplete);
        }

        private void RunPipAsync(string arguments, string operationName, int timeoutMs, Action<string> onOutput, Action<PythonResult> onComplete)
        {
            var thread = new Thread(new ThreadStart(delegate
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = pythonPath,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    ConfigurePipEnvironment(psi);

                    var proc = Process.Start(psi);
                    var stdoutBuilder = new StringBuilder();
                    var stderrBuilder = new StringBuilder();

                    var stderrThread = new Thread(new ThreadStart(delegate
                    {
                        try
                        {
                            string line;
                            while ((line = proc.StandardError.ReadLine()) != null)
                            {
                                stderrBuilder.AppendLine(line);
                                onOutput(line);
                            }
                        }
                        catch { }
                    }));
                    stderrThread.IsBackground = true;
                    stderrThread.Start();

                    string stdoutLine;
                    while ((stdoutLine = proc.StandardOutput.ReadLine()) != null)
                    {
                        stdoutBuilder.AppendLine(stdoutLine);
                        onOutput(stdoutLine);
                    }

                    stderrThread.Join(10000);

                    bool exited = proc.WaitForExit(timeoutMs);

                    if (!exited)
                    {
                        try { proc.Kill(); } catch { }
                        onComplete(new PythonResult
                        {
                            ExitCode = -1,
                            Output = stdoutBuilder.ToString(),
                            Error = "Operation timed out after " + (timeoutMs / 1000) + " seconds.",
                            Success = false
                        });
                        return;
                    }

                    onComplete(new PythonResult
                    {
                        ExitCode = proc.ExitCode,
                        Output = stdoutBuilder.ToString(),
                        Error = stderrBuilder.ToString(),
                        Success = proc.ExitCode == 0
                    });
                }
                catch (Exception ex)
                {
                    onComplete(CreateProcessErrorResult(operationName, ex));
                }
            }));
            thread.IsBackground = true;
            thread.Start();
        }

        public PythonResult CheckSyntax(string script)
        {
            if (!pythonAvailable)
                return CreateUnavailableResult("check syntax");

            string scriptFile = GetTempFilePath("_src.py");
            File.WriteAllText(scriptFile, script);

            string checkFile = GetTempFilePath("_chk.py");
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
                string stdout, stderr;
                ReadProcessOutputs(proc, out stdout, out stderr, 10000);
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

        public Dictionary<string, ModuleIntrospection> IntrospectModules(IEnumerable<string> moduleNames)
        {
            var result = new Dictionary<string, ModuleIntrospection>();
            if (!pythonAvailable) return result;

            var modules = new List<string>(moduleNames);
            if (modules.Count == 0) return result;

            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python", "introspect_modules.py");
            if (!File.Exists(scriptPath)) return result;

            string args = "\"" + scriptPath + "\"";
            foreach (var m in modules)
                args += " " + m;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var proc = Process.Start(psi);
                string stdout = null;
                var readThread = new Thread(() => { stdout = proc.StandardOutput.ReadToEnd(); });
                readThread.IsBackground = true;
                readThread.Start();
                bool exited = proc.WaitForExit(30000);

                if (!exited)
                {
                    try { proc.Kill(); } catch { }
                    return result;
                }
                readThread.Join(5000);

                if (stdout == null || proc.ExitCode != 0) return result;

                int startIdx = stdout.IndexOf("__INTROSPECT_START__");
                int endIdx = stdout.IndexOf("__INTROSPECT_END__");
                if (startIdx < 0 || endIdx < 0) return result;

                startIdx += "__INTROSPECT_START__".Length;
                string json = stdout.Substring(startIdx, endIdx - startIdx).Trim();
                result = ModuleIntrospection.ParseJson(json);
            }
            catch { }

            return result;
        }

        public void IntrospectModulesAsync(IEnumerable<string> moduleNames, Action<Dictionary<string, ModuleIntrospection>> onComplete)
        {
            var thread = new Thread(new ThreadStart(() =>
            {
                var result = IntrospectModules(moduleNames);
                onComplete(result);
            }));
            thread.IsBackground = true;
            thread.Start();
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
                ConfigurePipEnvironment(psi);

                var proc = Process.Start(psi);
                string stdout, stderr;
                ReadProcessOutputs(proc, out stdout, out stderr, 30000);
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

    public class DocEntry
    {
        public string Signature { get; set; }
        public string DocString { get; set; }
        public DocEntry() { Signature = ""; DocString = ""; }
        public DocEntry(string sig, string doc) { Signature = sig ?? ""; DocString = doc ?? ""; }
    }

    public class ModuleIntrospection
    {
        public List<string> Functions { get; set; }
        public Dictionary<string, List<string>> Classes { get; set; }
        public List<string> Constants { get; set; }
        public List<string> Submodules { get; set; }
        public Dictionary<string, DocEntry> FunctionDocs { get; set; }
        public Dictionary<string, DocEntry> ClassDocs { get; set; }

        public ModuleIntrospection()
        {
            Functions = new List<string>();
            Classes = new Dictionary<string, List<string>>();
            Constants = new List<string>();
            Submodules = new List<string>();
            FunctionDocs = new Dictionary<string, DocEntry>();
            ClassDocs = new Dictionary<string, DocEntry>();
        }

        public List<string> GetAllMembers()
        {
            var all = new List<string>();
            foreach (var f in Functions)
                all.Add(f + "()");
            all.AddRange(Constants);
            all.AddRange(Submodules);
            foreach (var cls in Classes.Keys)
                all.Add(cls);
            all.Sort(StringComparer.OrdinalIgnoreCase);
            return all;
        }

        public static Dictionary<string, ModuleIntrospection> ParseJson(string json)
        {
            var result = new Dictionary<string, ModuleIntrospection>();
            if (string.IsNullOrEmpty(json)) return result;

            try
            {
                int pos = 0;
                SkipWhitespace(json, ref pos);
                if (pos >= json.Length || json[pos] != '{') return result;
                pos++;

                while (pos < json.Length)
                {
                    SkipWhitespace(json, ref pos);
                    if (pos >= json.Length || json[pos] == '}') break;
                    if (json[pos] == ',') { pos++; continue; }

                    string moduleName = ReadJsonString(json, ref pos);
                    if (moduleName == null) break;
                    SkipWhitespace(json, ref pos);
                    if (pos >= json.Length || json[pos] != ':') break;
                    pos++;

                    var intro = ParseModuleObject(json, ref pos);
                    if (intro != null)
                        result[moduleName] = intro;
                }
            }
            catch { }

            return result;
        }

        private static ModuleIntrospection ParseModuleObject(string json, ref int pos)
        {
            var intro = new ModuleIntrospection();
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != '{') return null;
            pos++;

            while (pos < json.Length)
            {
                SkipWhitespace(json, ref pos);
                if (pos >= json.Length || json[pos] == '}') { pos++; break; }
                if (json[pos] == ',') { pos++; continue; }

                string key = ReadJsonString(json, ref pos);
                if (key == null) break;
                SkipWhitespace(json, ref pos);
                if (pos >= json.Length || json[pos] != ':') break;
                pos++;

                if (key == "classes")
                {
                    intro.Classes = ReadJsonDictOfStringArrays(json, ref pos);
                }
                else if (key == "function_docs" || key == "class_docs")
                {
                    var docs = ReadJsonDictOfDocEntries(json, ref pos);
                    if (key == "function_docs") intro.FunctionDocs = docs;
                    else intro.ClassDocs = docs;
                }
                else
                {
                    var arr = ReadJsonStringArray(json, ref pos);
                    if (key == "functions") intro.Functions = arr;
                    else if (key == "constants") intro.Constants = arr;
                    else if (key == "submodules") intro.Submodules = arr;
                }
            }

            return intro;
        }

        private static void SkipWhitespace(string json, ref int pos)
        {
            while (pos < json.Length && char.IsWhiteSpace(json[pos])) pos++;
        }

        private static string ReadJsonString(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != '"') return null;
            pos++;
            var sb = new StringBuilder();
            while (pos < json.Length && json[pos] != '"')
            {
                if (json[pos] == '\\' && pos + 1 < json.Length)
                {
                    pos++;
                    char esc = json[pos];
                    switch (esc)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u':
                            if (pos + 4 < json.Length)
                            {
                                string hex = json.Substring(pos + 1, 4);
                                int code;
                                if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out code))
                                {
                                    sb.Append((char)code);
                                    pos += 4;
                                }
                                else
                                    sb.Append(esc);
                            }
                            else
                                sb.Append(esc);
                            break;
                        default: sb.Append(esc); break;
                    }
                }
                else
                {
                    sb.Append(json[pos]);
                }
                pos++;
            }
            if (pos < json.Length) pos++;
            return sb.ToString();
        }

        private static List<string> ReadJsonStringArray(string json, ref int pos)
        {
            var list = new List<string>();
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != '[') return list;
            pos++;

            while (pos < json.Length)
            {
                SkipWhitespace(json, ref pos);
                if (pos >= json.Length || json[pos] == ']') { pos++; break; }
                if (json[pos] == ',') { pos++; continue; }
                string val = ReadJsonString(json, ref pos);
                if (val != null) list.Add(val);
                else break;
            }

            return list;
        }

        private static Dictionary<string, List<string>> ReadJsonDictOfStringArrays(string json, ref int pos)
        {
            var dict = new Dictionary<string, List<string>>();
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != '{') return dict;
            pos++;

            while (pos < json.Length)
            {
                SkipWhitespace(json, ref pos);
                if (pos >= json.Length || json[pos] == '}') { pos++; break; }
                if (json[pos] == ',') { pos++; continue; }

                string key = ReadJsonString(json, ref pos);
                if (key == null) break;
                SkipWhitespace(json, ref pos);
                if (pos >= json.Length || json[pos] != ':') break;
                pos++;

                var arr = ReadJsonStringArray(json, ref pos);
                dict[key] = arr;
            }

            return dict;
        }

        private static Dictionary<string, DocEntry> ReadJsonDictOfDocEntries(string json, ref int pos)
        {
            var dict = new Dictionary<string, DocEntry>();
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != '{') return dict;
            pos++;

            while (pos < json.Length)
            {
                SkipWhitespace(json, ref pos);
                if (pos >= json.Length || json[pos] == '}') { pos++; break; }
                if (json[pos] == ',') { pos++; continue; }

                string key = ReadJsonString(json, ref pos);
                if (key == null) break;
                SkipWhitespace(json, ref pos);
                if (pos >= json.Length || json[pos] != ':') break;
                pos++;

                var entry = ReadDocEntryObject(json, ref pos);
                if (entry != null)
                    dict[key] = entry;
            }

            return dict;
        }

        private static DocEntry ReadDocEntryObject(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != '{') return null;
            pos++;

            string sig = "";
            string doc = "";

            while (pos < json.Length)
            {
                SkipWhitespace(json, ref pos);
                if (pos >= json.Length || json[pos] == '}') { pos++; break; }
                if (json[pos] == ',') { pos++; continue; }

                string k = ReadJsonString(json, ref pos);
                if (k == null) break;
                SkipWhitespace(json, ref pos);
                if (pos >= json.Length || json[pos] != ':') break;
                pos++;

                string v = ReadJsonString(json, ref pos);
                if (k == "sig") sig = v ?? "";
                else if (k == "doc") doc = v ?? "";
            }

            return new DocEntry(sig, doc);
        }
    }
}
