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
                if (p.PropertyType.IsClass && p.PropertyType != typeof(string) && !IsImageType(p.PropertyType) && !IsPythonVisibleClass(p.PropertyType)) continue;
                if (p.PropertyType.IsClass && p.PropertyType != typeof(string) && !IsImageType(p.PropertyType) && IsPythonVisibleClass(p.PropertyType)) continue;

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
                if (p.PropertyType.IsClass && p.PropertyType != typeof(string) && !IsImageType(p.PropertyType)) continue;
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

                if (p.PropertyType.IsClass && p.PropertyType != typeof(string) && !IsImageType(p.PropertyType)) continue;

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
            if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte)) return "int";
            if (t == typeof(double) || t == typeof(float) || t == typeof(decimal)) return "float";
            if (t == typeof(bool)) return "bool";
            if (t == typeof(string)) return "string";
            if (t == typeof(DateTime)) return "datetime";
            if (t == typeof(Bitmap) || t == typeof(Image)) return "image";
            return t.Name;
        }
    }
}
