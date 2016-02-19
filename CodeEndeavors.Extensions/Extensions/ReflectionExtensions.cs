using System;
using System.Linq;
using System.IO;
using System.Web;
using System.Collections.Concurrent;
using System.Reflection;
using System.Management.Instrumentation;
using System.Collections.Generic;

namespace CodeEndeavors.Extensions
{
    public static class ReflectionExtensions
    {
        private static readonly ConcurrentDictionary<string, Type> _cachedTypes = new ConcurrentDictionary<string, Type>();
        private static readonly ConcurrentDictionary<string, IEnumerable<Assembly>> _cachedAssemblies = new ConcurrentDictionary<string, IEnumerable<Assembly>>();

        public static T GetInstance<T>(this string typeName, string assemblyPath = null)
        {
            var type = typeName.ToType();
            var obj = Activator.CreateInstance(type);// as IWidgetContentProvider;
            if (obj == null)
                throw new InstanceNotFoundException(string.Format("Unable to create a valid instance of {0} from type: {1}", typeof(T).ToString(), typeName));
            return obj.ToType<T>();
        }

        public static List<T> GetAllInstances<T>(string assemblyPath = null)
        {
            Type theType = typeof(T);
            var types = theType.GetAllTypes(assemblyPath);
            return types.Select(t => Activator.CreateInstance(t)).Cast<T>().ToList();
        }

        public static Type ToType(this string typeName, string assemblyPath = null)
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
                type = assemblies.SelectMany(a => a.GetTypes().Where(t => t.FullName.StartsWith(typeName))).FirstOrDefault();
            }

            if (type == null)
                throw new TypeLoadException(string.Format("Unable to load type: {0}", typeName));

            _cachedTypes[typeName] = type;
            return type;
        }

        public static List<T> GetAllTypes<T>(this T theInterface, string assemblyPath = null) where T : Type
        {
            return getAllAssemblies(assemblyPath).SelectMany(a => a.GetTypes().Where(t => t.GetInterfaces().Contains(theInterface))).Cast<T>().ToList();
        }

        private static IEnumerable<Assembly> getAllAssemblies(string assemblyPath = null)
        {
            if (string.IsNullOrEmpty(assemblyPath))
                assemblyPath = AppDomain.CurrentDomain.BaseDirectory;
            //if (assemblyPath.Equals(AppDomain.CurrentDomain.BaseDirectory, StringComparison.InvariantCultureIgnoreCase))
            //    return AppDomain.CurrentDomain.GetAssemblies();
            if (!_cachedAssemblies.ContainsKey(assemblyPath))
            {
                _cachedAssemblies[assemblyPath] = (from file in new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).GetFiles()
                        where file.Extension.ToLower() == ".dll"
                        select Assembly.LoadFile(file.FullName));
            }
            return _cachedAssemblies[assemblyPath];
        }

        public static bool IsNullable<T>(this T value)  //http://stackoverflow.com/questions/6026824/detecting-a-nullable-type-via-reflection
        {
            return Nullable.GetUnderlyingType(typeof(T)) != null;
        }
    }

}