using System.Collections.Generic;

namespace RJLG.IntelliSEM.Data.PythonDataScience
{
    public class RunConfiguration
    {
        public string Name { get; set; }
        public string ScriptPath { get; set; }
        public string Arguments { get; set; }
        public string InputFilePath { get; set; }
        public bool UseCurrentFile { get; set; }

        public RunConfiguration()
        {
            Name = "";
            ScriptPath = "";
            Arguments = "";
            InputFilePath = "";
            UseCurrentFile = true;
        }

        public RunConfiguration Clone()
        {
            return new RunConfiguration
            {
                Name = Name,
                ScriptPath = ScriptPath,
                Arguments = Arguments,
                InputFilePath = InputFilePath,
                UseCurrentFile = UseCurrentFile
            };
        }
    }

    public static class RunConfigurationStore
    {
        public static List<RunConfiguration> Load(string filePath, out int selectedIndex)
        {
            selectedIndex = -1;
            var configs = new List<RunConfiguration>();
            if (!System.IO.File.Exists(filePath))
                return configs;

            try
            {
                string[] lines = System.IO.File.ReadAllLines(filePath);
                RunConfiguration current = null;
                bool inSelected = false;
                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (line == "[config]")
                    {
                        inSelected = false;
                        if (current != null)
                            configs.Add(current);
                        current = new RunConfiguration();
                    }
                    else if (line == "[selected]")
                    {
                        inSelected = true;
                        if (current != null)
                        {
                            configs.Add(current);
                            current = null;
                        }
                    }
                    else if (inSelected && line.Contains("="))
                    {
                        int eqIdx = line.IndexOf('=');
                        string key = line.Substring(0, eqIdx).Trim();
                        string val = line.Substring(eqIdx + 1).Trim();
                        if (key == "index")
                        {
                            int.TryParse(val, out selectedIndex);
                        }
                    }
                    else if (current != null && line.Contains("="))
                    {
                        int eqIdx = line.IndexOf('=');
                        string key = line.Substring(0, eqIdx).Trim();
                        string val = line.Substring(eqIdx + 1).Trim();
                        switch (key)
                        {
                            case "name": current.Name = val; break;
                            case "script": current.ScriptPath = val; break;
                            case "args": current.Arguments = val; break;
                            case "input": current.InputFilePath = val; break;
                            case "use_current": current.UseCurrentFile = val == "true"; break;
                        }
                    }
                }
                if (current != null)
                    configs.Add(current);
            }
            catch { }

            if (selectedIndex >= configs.Count)
                selectedIndex = -1;

            return configs;
        }

        public static void Save(string filePath, List<RunConfiguration> configs, int selectedIndex = -1)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var c in configs)
            {
                sb.AppendLine("[config]");
                sb.AppendLine("name=" + (c.Name ?? ""));
                sb.AppendLine("script=" + (c.ScriptPath ?? ""));
                sb.AppendLine("args=" + (c.Arguments ?? ""));
                sb.AppendLine("input=" + (c.InputFilePath ?? ""));
                sb.AppendLine("use_current=" + (c.UseCurrentFile ? "true" : "false"));
            }
            sb.AppendLine("[selected]");
            sb.AppendLine("index=" + selectedIndex);
            System.IO.File.WriteAllText(filePath, sb.ToString());
        }
    }
}
