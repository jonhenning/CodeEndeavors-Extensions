using CodeEndeavors.Extensions;
using System;
using System.Linq;
using System.IO;
using System.Web;
using System.Collections.Concurrent;
using System.Reflection;
using System.Management.Instrumentation;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CodeEndeavors.Extensions
{
    public static class ReflectionExtensions
    {
        private static readonly ConcurrentDictionary<string, Type> _cachedTypes = new ConcurrentDictionary<string, Type>();
        private static readonly ConcurrentDictionary<string, IEnumerable<Assembly>> _cachedAssemblies = new ConcurrentDictionary<string, IEnumerable<Assembly>>();

        public static T GetInstance<T>(this string typeName, string assemblyPath = null)
        {
            var type = typeName.ToType(assemblyPath);
            var obj = Activator.CreateInstance(type);
            if (obj == null)
                throw new InstanceNotFoundException(string.Format("Unable to create a valid instance of {0} from type: {1}", typeof(T).ToString(), typeName));
            return obj.ToType<T>();
        }

        public static T GetInstance<T>(this string typeName, string assemblyPath = null, params object[] args)
        {
            var type = typeName.ToType(assemblyPath);
            var obj = Activator.CreateInstance(type, args);
            if (obj == null)
                throw new InstanceNotFoundException(string.Format("Unable to create a valid instance of {0} from type: {1}", typeof(T).ToString(), typeName));
            return obj.ToType<T>();
        }

        public static List<T> GetAllInstances<T>(string assemblyPath = null)
        {
            try
            {
                Type theType = typeof(T);
                var types = theType.GetAllTypes(assemblyPath);
                return types.Select(t => Activator.CreateInstance(t)).Cast<T>().ToList();
            }
            catch (System.Reflection.ReflectionTypeLoadException ex)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var e in ex.LoaderExceptions)
                    sb.AppendLine(e.Message);
                sb.AppendLine(ex.Message);
                throw new Exception(sb.ToString());
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        }

        public static Type ToType(this string typeName, string assemblyPath = null)
        {
            return ToType(typeName, false, true, assemblyPath);
        }
        
        public static Type ToType(this string typeName, bool onlyPublic, bool throwError, string assemblyPath = null)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentException("The parameter cannot be null.", "typeName");

            Type type;
            if (_cachedTypes.TryGetValue(typeName, out type))
                return type;

            if (!string.IsNullOrWhiteSpace(assemblyPath))
                Assembly.LoadFrom(assemblyPath);

            type = Type.GetType(typeName);
            if (type == null)
            {
                var assemblies = getAllAssemblies(assemblyPath);
                type = assemblies.SelectMany(a => 
                {
                    try 
                    {
                        return a.GetTypes().Where(t => t.FullName.Equals(typeName) && (onlyPublic == false || t.IsPublic));
                    }
                    catch (Exception ex) { }    //eat exception
                    return new List<Type>();
                }).FirstOrDefault();
            }

            if (throwError && type == null)
                throw new TypeLoadException(string.Format("Unable to load type: {0}", typeName));

            _cachedTypes[typeName] = type;
            return type;
        }

        public static List<T> GetAllTypes<T>(this T theInterface, string assemblyPath = null) where T : Type
        {
            return getAllAssemblies(assemblyPath).SelectMany(a => 
                {
                    var ret = new List<Type>();
                    try
                    {
                        ret = a.GetTypes().Where(t => t.GetInterfaces().Contains(theInterface) && !t.IsInterface).ToList();
                    }
                    catch (Exception ex)
                    {
                        //todo: log?
                    }
                    return ret;
                }).Cast<T>().ToList();
        }

        private static IEnumerable<Assembly> getAllAssemblies(string assemblyPath = null)
        {
            if (string.IsNullOrEmpty(assemblyPath))
                assemblyPath = AppDomain.CurrentDomain.SetupInformation.PrivateBinPath; //grab web\bin folder if exists
            if (string.IsNullOrEmpty(assemblyPath))
                assemblyPath = AppDomain.CurrentDomain.BaseDirectory;   //grab bin folder

            if (!_cachedAssemblies.ContainsKey(assemblyPath))
            {
                var assemblies = new List<Assembly>();
                var files = Directory.EnumerateFiles(assemblyPath, "*.dll", SearchOption.AllDirectories); 
                foreach (var file in files)
                {
                    var name = Path.GetFileNameWithoutExtension(file);  

                    Assembly assembly = null;
                    try
                    {
                        assembly = AppDomain.CurrentDomain.Load(name);  //for ASP.NET we do not want the assembly in the bin folder so we cannot simply do an Assembly.LoadFile - instead we need to get the file from C:\Windows\Microsoft.NET\Framework64\v4.0.30319\Temporary ASP.NET Files\............
                    }
                    catch (Exception)
                    {
                        //todo:  log?
                    }

                    if (assembly != null)
                        assemblies.Add(assembly);
                }
                _cachedAssemblies[assemblyPath] = assemblies;
            }
            return _cachedAssemblies[assemblyPath];
        }

        public static bool IsNullable<T>(this T value)  //http://stackoverflow.com/questions/6026824/detecting-a-nullable-type-via-reflection
        {
            return Nullable.GetUnderlyingType(typeof(T)) != null;
        }

        public static object InvokeStaticMethod(this Type o, string name)
        {
            return ReflectionExtensions.InvokeStaticMethod(o, name, null);
        }

        public static object InvokeStaticMethod(this Type o, string name, params object[] args)
        {
            return o.InvokeMember(name, BindingFlags.InvokeMethod, null, null, args);
        }

        public static T InvokeStaticMethod<T>(this Type o, string name, params object[] args)
        {
            return (T)((object)o.InvokeMember(name, BindingFlags.InvokeMethod, null, null, args));
        }

        public static object InvokePropertyGet(this object o, string name, params object[] args)
        {
            return o.GetType().InvokeMember(name, BindingFlags.GetProperty, null, RuntimeHelpers.GetObjectValue(o), args);
        }

        public static T InvokePropertyGet<T>(this object o, string name)
        {
            return (T)((object)ReflectionExtensions.InvokePropertyGet(RuntimeHelpers.GetObjectValue(o), name));
        }

        public static T InvokePropertyGet<TType, T>(this TType o, string name)
        {
            return (T)((object)ReflectionExtensions.InvokePropertyGet(o, name));
        }

        public static object InvokePropertyGet(this object o, string name)
        {
            return ReflectionExtensions.InvokePropertyGet(RuntimeHelpers.GetObjectValue(o), name, null);
        }

        public static void InvokePropertySet(this object o, string name, params object[] args)
        {
            bool flag = args.Length == 1;
            if (flag)
            {
                o.GetType().InvokeMember(name, BindingFlags.SetProperty, null, RuntimeHelpers.GetObjectValue(o), args);
            }
            else
            {
                o.GetType().GetProperty(name).SetValue(RuntimeHelpers.GetObjectValue(o), RuntimeHelpers.GetObjectValue(args[1]), new object[]
		        {
			        RuntimeHelpers.GetObjectValue(args[0])
		        });
            }
        }

        public static void InvokeStaticPropertySet(this Type o, string name, params object[] args)
        {
            bool flag = args.Length == 1;
            if (flag)
            {
                o.InvokeMember(name, BindingFlags.Static | BindingFlags.SetProperty, null, null, args);
            }
            else
            {
                ReflectionExtensions.InvokePropertySet(RuntimeHelpers.GetObjectValue(o.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty).GetValue(null, null)), "Item", new object[]
		        {
			        RuntimeHelpers.GetObjectValue(args[0]),
			        RuntimeHelpers.GetObjectValue(args[1])
		        });
            }
        }



    }

}