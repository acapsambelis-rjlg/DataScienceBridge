using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RJLG.IntelliSEM.Data.PythonDataScience
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class PythonVisibleAttribute : Attribute
    {
        public string Description { get; private set; }
        public string Example { get; set; }

        public PythonVisibleAttribute() { Description = null; }
        public PythonVisibleAttribute(string description) { Description = description; }
    }

    public class FlattenedProperty
    {
        public string ColumnName { get; set; }
        public System.Reflection.PropertyInfo[] PropertyPath { get; set; }
        public Type LeafType { get; set; }
        public bool IsComputed { get; set; }

        public object GetValue(object root)
        {
            object current = root;
            foreach (var p in PropertyPath)
            {
                if (current == null) return null;
                current = p.GetValue(current);
            }
            return current;
        }

        public PythonVisibleAttribute GetAttribute()
        {
            var leaf = PropertyPath[PropertyPath.Length - 1];
            var attrs = leaf.GetCustomAttributes(typeof(PythonVisibleAttribute), true);
            if (attrs.Length > 0) return (PythonVisibleAttribute)attrs[0];
            return null;
        }
    }

    public static class PythonVisibleHelper
    {
        public static List<System.Reflection.PropertyInfo> GetVisibleProperties(Type type)
        {
            var allProps = type.GetProperties();
            var markedProps = new List<System.Reflection.PropertyInfo>();
            bool anyMarked = false;

            foreach (var p in allProps)
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(List<>)) continue;
                if (p.PropertyType.IsClass && p.PropertyType != typeof(string) && !IsImageType(p.PropertyType) && !IsDictionaryType(p.PropertyType) && !IsPythonVisibleClass(p.PropertyType)) continue;
                if (p.PropertyType.IsClass && p.PropertyType != typeof(string) && !IsImageType(p.PropertyType) && !IsDictionaryType(p.PropertyType) && IsPythonVisibleClass(p.PropertyType)) continue;

                if (p.GetCustomAttributes(typeof(PythonVisibleAttribute), true).Length > 0)
                {
                    markedProps.Add(p);
                    anyMarked = true;
                }
            }

            if (anyMarked)
                return markedProps;

            var result = new List<System.Reflection.PropertyInfo>();
            foreach (var p in allProps)
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(List<>)) continue;
                if (p.PropertyType.IsClass && p.PropertyType != typeof(string) && !IsImageType(p.PropertyType) && !IsDictionaryType(p.PropertyType)) continue;
                result.Add(p);
            }
            return result;
        }

        public static List<FlattenedProperty> GetFlattenedProperties(Type type)
        {
            var result = new List<FlattenedProperty>();
            CollectFlattened(type, "", new System.Reflection.PropertyInfo[0], result, 0);
            return result;
        }

        private static void CollectFlattened(Type type, string prefix, System.Reflection.PropertyInfo[] parentPath, List<FlattenedProperty> result, int depth)
        {
            if (depth > 4) return;

            var allProps = type.GetProperties();
            bool anyMarked = false;
            foreach (var p in allProps)
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (p.GetCustomAttributes(typeof(PythonVisibleAttribute), true).Length > 0)
                {
                    anyMarked = true;
                    break;
                }
            }

            foreach (var p in allProps)
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(List<>)) continue;

                bool isNestedVisibleClass = p.PropertyType.IsClass
                    && p.PropertyType != typeof(string)
                    && !IsDictionaryType(p.PropertyType)
                    && IsPythonVisibleClass(p.PropertyType);

                if (isNestedVisibleClass)
                {
                    bool propMarked = p.GetCustomAttributes(typeof(PythonVisibleAttribute), true).Length > 0;
                    if (anyMarked && !propMarked) continue;

                    string nestedPrefix = string.IsNullOrEmpty(prefix) ? p.Name + "_" : prefix + p.Name + "_";
                    var newPath = new System.Reflection.PropertyInfo[parentPath.Length + 1];
                    Array.Copy(parentPath, newPath, parentPath.Length);
                    newPath[parentPath.Length] = p;
                    CollectFlattened(p.PropertyType, nestedPrefix, newPath, result, depth + 1);
                    continue;
                }

                if (p.PropertyType.IsClass && p.PropertyType != typeof(string) && !IsImageType(p.PropertyType) && !IsDictionaryType(p.PropertyType)) continue;

                if (anyMarked && p.GetCustomAttributes(typeof(PythonVisibleAttribute), true).Length == 0) continue;

                string colName = string.IsNullOrEmpty(prefix) ? p.Name : prefix + p.Name;
                var fullPath = new System.Reflection.PropertyInfo[parentPath.Length + 1];
                Array.Copy(parentPath, fullPath, parentPath.Length);
                fullPath[parentPath.Length] = p;

                result.Add(new FlattenedProperty
                {
                    ColumnName = colName,
                    PropertyPath = fullPath,
                    LeafType = p.PropertyType,
                    IsComputed = p.GetSetMethod() == null
                });
            }
        }

        public static bool IsPythonVisibleClass(Type t)
        {
            return t.IsClass && t != typeof(string) && t.GetCustomAttributes(typeof(PythonVisibleAttribute), true).Length > 0;
        }

        public static bool IsImageType(Type t)
        {
            return t == typeof(Bitmap) || t == typeof(Image);
        }

        public static bool IsDictionaryType(Type t)
        {
            if (!t.IsGenericType) return false;
            var gd = t.GetGenericTypeDefinition();
            return gd == typeof(Dictionary<,>) || gd == typeof(SortedDictionary<,>) || gd == typeof(SortedList<,>);
        }

        public static bool IsSortedDictionaryType(Type t)
        {
            if (!t.IsGenericType) return false;
            var gd = t.GetGenericTypeDefinition();
            return gd == typeof(SortedDictionary<,>) || gd == typeof(SortedList<,>);
        }

        public static string DictionaryToJson(object obj)
        {
            if (obj == null) return "";
            var dict = obj as System.Collections.IDictionary;
            if (dict == null) return "";
            var sb = new System.Text.StringBuilder();
            sb.Append("__DICT__:{");
            bool first = true;
            foreach (System.Collections.DictionaryEntry entry in dict)
            {
                if (!first) sb.Append(",");
                first = false;
                sb.Append("\"");
                sb.Append(JsonEscapeString(entry.Key.ToString()));
                sb.Append("\":");
                if (entry.Value is double d)
                    sb.Append(d.ToString("G", System.Globalization.CultureInfo.InvariantCulture));
                else if (entry.Value is float f)
                    sb.Append(f.ToString("G", System.Globalization.CultureInfo.InvariantCulture));
                else if (entry.Value is decimal dec)
                    sb.Append(dec.ToString(System.Globalization.CultureInfo.InvariantCulture));
                else if (entry.Value is int iv)
                    sb.Append(iv);
                else if (entry.Value is long lv)
                    sb.Append(lv);
                else if (entry.Value is bool bv)
                    sb.Append(bv ? "true" : "false");
                else if (entry.Value == null)
                    sb.Append("null");
                else
                {
                    sb.Append("\"");
                    sb.Append(JsonEscapeString(entry.Value.ToString()));
                    sb.Append("\"");
                }
            }
            sb.Append("}");
            return sb.ToString();
        }

        private static string JsonEscapeString(string s)
        {
            if (s == null) return "";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < ' ')
                            sb.AppendFormat("\\u{0:X4}", (int)c);
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        public static Action<object> PrepareItem { get; set; }
        public static Action<object> ReleaseItem { get; set; }

        public static string BitmapToBase64(Bitmap bmp)
        {
            if (bmp == null) return "";
            using (var ms = new System.IO.MemoryStream())
            {
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return "__IMG__:" + Convert.ToBase64String(ms.ToArray());
            }
        }

        public static string GetPythonTypeName(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                Type inner = Nullable.GetUnderlyingType(t);
                return GetPythonTypeName(inner) + " (nullable)";
            }
            if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte)) return "int";
            if (t == typeof(double) || t == typeof(float) || t == typeof(decimal)) return "float";
            if (t == typeof(bool)) return "bool";
            if (t == typeof(string)) return "string";
            if (t == typeof(DateTime)) return "datetime";
            if (t == typeof(Bitmap) || t == typeof(Image)) return "image";
            if (IsDictionaryType(t))
            {
                var args = t.GetGenericArguments();
                string sortedLabel = IsSortedDictionaryType(t) ? "sorted, " : "";
                return "dict (" + sortedLabel + GetPythonTypeName(args[0]) + " → " + GetPythonTypeName(args[1]) + ")";
            }
            if (t.IsEnum)
            {
                string[] names = Enum.GetNames(t);
                return "string (enum: " + string.Join(", ", names) + ")";
            }
            return t.Name;
        }

        public static bool IsNullableType(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static bool IsEnumType(Type t)
        {
            if (t.IsEnum) return true;
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                return Nullable.GetUnderlyingType(t).IsEnum;
            return false;
        }

        public static Type GetUnderlyingEnumType(Type t)
        {
            if (t.IsEnum) return t;
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                Type inner = Nullable.GetUnderlyingType(t);
                if (inner.IsEnum) return inner;
            }
            return null;
        }
    }
}
