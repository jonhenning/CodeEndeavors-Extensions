using System;
using System.Linq;
using System.IO;
using System.Web;
using System.Collections.Concurrent;
using System.Reflection;
using System.Management.Instrumentation;

namespace CodeEndeavors.Extensions
{
    public static class ReflectionExtensions
    {
        private static readonly ConcurrentDictionary<string, Type> _cachedTypes = new ConcurrentDictionary<string, Type>();
        public static T GetInstance<T>(this string typeName, string assemblyPath = null)
        {
            var type = typeName.ToType();
            var obj = Activator.CreateInstance(type);// as IWidgetContentProvider;
            if (obj == null)
                throw new InstanceNotFoundException(string.Format("Unable to create a valid instance of {0} from type: {1}", typeof(T).ToString(), typeName));
            return obj.ToType<T>();
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
                var assemblies = (from file in new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).GetFiles()
                                  where file.Extension.ToLower() == ".dll"
                                  select Assembly.LoadFile(file.FullName));
                type = assemblies.SelectMany(a => a.GetTypes().Where(t => t.FullName.StartsWith(typeName))).FirstOrDefault();
            }

            if (type == null)
                throw new TypeLoadException(string.Format("Unable to load type: {0}", typeName));

            _cachedTypes[typeName] = type;
            return type;
        }

        public static bool IsNullable<T>(this T value)  //http://stackoverflow.com/questions/6026824/detecting-a-nullable-type-via-reflection
        {
            return Nullable.GetUnderlyingType(typeof(T)) != null;
        }
    }

}