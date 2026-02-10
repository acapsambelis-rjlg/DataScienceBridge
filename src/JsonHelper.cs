using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DataScienceWorkbench
{
    public static class SimpleJson
    {
        public static string Serialize(object obj)
        {
            if (obj == null) return "null";
            return SerializeValue(obj, 0);
        }

        private static string SerializeValue(object obj, int depth)
        {
            if (obj == null) return "null";
            if (depth > 5) return "\"...\"";

            Type t = obj.GetType();

            if (t == typeof(string)) return EscapeString((string)obj);
            if (t == typeof(bool)) return (bool)obj ? "true" : "false";
            if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte))
                return obj.ToString();
            if (t == typeof(double)) return ((double)obj).ToString("G");
            if (t == typeof(float)) return ((float)obj).ToString("G");
            if (t == typeof(decimal)) return ((decimal)obj).ToString("G");
            if (t == typeof(DateTime)) return EscapeString(((DateTime)obj).ToString("yyyy-MM-dd HH:mm:ss"));

            if (obj is IList list)
            {
                var sb = new StringBuilder();
                sb.Append("[\n");
                for (int i = 0; i < list.Count; i++)
                {
                    sb.Append(Indent(depth + 1));
                    sb.Append(SerializeValue(list[i], depth + 1));
                    if (i < list.Count - 1) sb.Append(",");
                    sb.Append("\n");
                }
                sb.Append(Indent(depth));
                sb.Append("]");
                return sb.ToString();
            }

            if (t.IsClass && t != typeof(string))
            {
                var sb = new StringBuilder();
                sb.Append("{\n");
                var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                for (int i = 0; i < props.Length; i++)
                {
                    var prop = props[i];
                    if (prop.GetIndexParameters().Length > 0) continue;
                    object val = null;
                    try { val = prop.GetValue(obj); } catch { continue; }
                    sb.Append(Indent(depth + 1));
                    sb.Append(EscapeString(prop.Name));
                    sb.Append(": ");
                    sb.Append(SerializeValue(val, depth + 1));
                    if (i < props.Length - 1) sb.Append(",");
                    sb.Append("\n");
                }
                sb.Append(Indent(depth));
                sb.Append("}");
                return sb.ToString();
            }

            return EscapeString(obj.ToString());
        }

        private static string EscapeString(string s)
        {
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t") + "\"";
        }

        private static string Indent(int depth)
        {
            return new string(' ', depth * 2);
        }
    }
}
