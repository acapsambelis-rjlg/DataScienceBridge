using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataScienceWorkbench.PythonWorkbench
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class PythonVisibleAttribute : Attribute
    {
        public string Description { get; private set; }
        public string Example { get; set; }

        public PythonVisibleAttribute() { Description = null; }
        public PythonVisibleAttribute(string description) { Description = description; }
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
                if (p.PropertyType.IsClass && p.PropertyType != typeof(string)) continue;

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
                if (p.PropertyType.IsClass && p.PropertyType != typeof(string)) continue;
                result.Add(p);
            }
            return result;
        }

        public static string GetPythonTypeName(Type t)
        {
            if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte)) return "int";
            if (t == typeof(double) || t == typeof(float) || t == typeof(decimal)) return "float";
            if (t == typeof(bool)) return "bool";
            if (t == typeof(string)) return "string";
            if (t == typeof(DateTime)) return "datetime";
            return t.Name;
        }
    }
}
