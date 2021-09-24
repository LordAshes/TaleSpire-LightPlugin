using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using UnityEngine;

namespace LordAshes
{
    public static class ReflectionObjectManipulator
    {
        public static bool showDiagnostics {get; set;} = false;

        public static void BuildObjectsFromFile(ref Dictionary<string, object> target, string filename)
        {
            string[] contents = (System.IO.File.ReadAllText(filename)+"[]").Replace("\r","").Replace("\n","").Split(';');
            List<string> pokes = new List<string>();
            int pointer = 0;
            string content = "";
            while (pointer < contents.Length)
            {
                if (contents[pointer].Trim().StartsWith("//") || contents[pointer].Trim().StartsWith("#") || contents[pointer].Trim().StartsWith("'") || contents[pointer].Trim()=="")
                {
                    // Skip Comment
                }
                else if (contents[pointer].StartsWith("["))
                {
                    // Geneate Last Object
                    if (content != "")
                    {
                        string line = content.Replace("[", "").Replace("]", "");
                        string[] parts = line.Split(':');
                        // Create Corresponding Object
                        object obj = CreateObject(parts[1].Trim());
                        // Transfer Properties
                        pokes.Add(".name=" + parts[0].Trim());
                        Transfer(obj, pokes.ToArray());
                        // Add Object To Dictionary
                        target.Add(parts[0].Trim(), obj);
                    }
                    // Store Header For New Object
                    content = contents[pointer];
                    pokes.Clear();
                }
                else
                {
                    // Add Entry To Current Object
                    pokes.Add(contents[pointer]);
                }
                pointer++;
            }
        }

        public static void Transfer(object target, string[] pokes)
        {
            foreach (string poke in pokes)
            {
                try
                {
                    string[] kvp = null;
                    bool setProp = true;
                    // Set Property
                    if (poke.Contains("="))
                    {
                        // Set property with value
                        kvp = new string[] { poke.Substring(0, poke.IndexOf("=")).Trim(), poke.Substring(poke.IndexOf("=") + 1).Trim() };
                        setProp = true;
                    }
                    else
                    {
                        // Set property with new instance of specified class
                        kvp = new string[] { poke.Substring(0, poke.IndexOf(":")).Trim(), poke.Substring(poke.IndexOf(":") + 1).Trim() };
                        setProp = false;
                    }
                    string[] parts = kvp[0].Split('.');
                    object victim = target;
                    // Traverse object properties
                    for (int i = 1; i < parts.Length - 1; i++)
                    {
                        victim = victim.GetType().GetProperty(parts[i]).GetValue(victim);
                    }
                    // Get property
                    PropertyInfo prop = victim.GetType().GetProperty(parts[parts.Length - 1]);
                    // Determine type
                    object value = null;
                    if (!setProp)
                    {
                        // Value is specified type instance
                        value = CreateObject(kvp[1]);
                    }
                    else if (prop.PropertyType != typeof(String))
                    {
                        // Value is converted from JSON string
                        object subObj = Activator.CreateInstance(prop.PropertyType);
                        MethodInfo mi = typeof(ReflectionObjectManipulator).GetMethod("ConvertType");
                        var typeRef = mi.MakeGenericMethod(prop.PropertyType);
                        value = typeRef.Invoke(null, new object[] { kvp[1] });
                    }
                    else
                    {
                        // Value is string
                        value = kvp[1];
                    }
                    // Set Property
                    Debug.Log("Setting Object " + victim.ToString() + " (" + victim.GetType() + ") Property " + prop.Name + " (" + prop.PropertyType + ") To " + Convert.ToString(value));
                    prop.SetValue(victim, value);
                }
                catch (Exception) { Debug.LogWarning("Light Plugin: Unabled To Process '"+poke+"'"); }
            }
        }

        public static object CreateObject(string classType)
        {
            List<Type> types = new List<Type>();
            try { types.AddRange(Assembly.GetCallingAssembly().GetTypes()); } catch (Exception) {; }
            try { types.AddRange(Assembly.GetEntryAssembly().GetTypes()); } catch (Exception) {; }
            try { types.AddRange(Assembly.GetExecutingAssembly().GetTypes()); } catch (Exception) {; }

            foreach (System.Reflection.AssemblyName an in System.Reflection.Assembly.GetExecutingAssembly().GetReferencedAssemblies())
            {
                try
                {
                    System.Reflection.Assembly asm = System.Reflection.Assembly.Load(an.ToString());
                    types.AddRange(asm.GetTypes());
                }
                catch (Exception) {; }
            }
            foreach (Type type in types)
            {
                if (type.Name == classType)
                {
                    return Activator.CreateInstance(type);
                }
            }
            if (showDiagnostics) { Debug.Log("  Didn't Find A Match For Type " + classType); }
            return null;
        }

        public static T ConvertType<T>(object value)
        {
            if (showDiagnostics) { Debug.Log("Converting " + Convert.ToString(value) + " To " + typeof(T).ToString()); }
            if (typeof(T) == typeof(UnityEngine.Color))
            {
                string[] parts = value.ToString().Split(',');
                object c = null;
                if (parts.Length == 3)
                {
                    c = new UnityEngine.Color(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
                }
                else if (parts.Length == 3)
                {
                    c = new UnityEngine.Color(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
                }
                return (T)c;
            }
            if (typeof(T) == typeof(UnityEngine.Vector3))
            {
                string[] parts = value.ToString().Split(',');
                object v3 = new UnityEngine.Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
                return (T)v3;
            }
            if (typeof(T) == typeof(System.Boolean))
            {
                object b = false;
                if((Convert.ToString(value)=="True") || (Convert.ToString(value) == "true") || (Convert.ToString(value) == "1") || (Convert.ToString(value) == "-1")) { b = true; }
                return (T)b;
            }
            return JsonConvert.DeserializeObject<T>(value.ToString());
        }
    }
}


