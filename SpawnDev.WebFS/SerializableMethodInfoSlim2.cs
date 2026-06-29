using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using TypeExtensions = SpawnDev.BlazorJS.TypeExtensions;

namespace SpawnDev.WebFS
{
    public class SerializableMethodInfoSlim2
    {
        [JsonIgnore]
        public MethodInfo? MethodInfo
        {
            get
            {
                if (!Resolved) Resolve();
                return _MethodInfo;
            }
        }
        private MethodInfo? _MethodInfo = null;
        private bool Resolved = false;
        /// <summary>
        /// MethodInfo.ReflectedType type name
        /// </summary>
        public string ReflectedTypeName { get; init; } = "";
        /// <summary>
        /// MethodInfo.DeclaringType type name
        /// </summary>
        public string DeclaringTypeName { get; init; } = "";
        /// <summary>
        /// MethodInfo.Name
        /// </summary>
        public string MethodName { get; init; } = "";
        /// <summary>
        /// methodInfo.GetParameters() type names
        /// </summary>
        public List<string> ParameterTypes { get; init; } = new List<string>();
        /// <summary>
        /// MethodInfo.ReturnType type name
        /// </summary>
        public string ReturnType { get; init; } = "";
        /// <summary>
        /// methodInfo.GetGenericArguments() type names
        /// </summary>
        public List<string> GenericArguments { get; init; } = new List<string>();
        /// <summary>
        /// Deserialization constructor
        /// </summary>
        public SerializableMethodInfoSlim2() { }
        /// <summary>
        /// Creates a new instance of SerializableMethodInfoSlim2 that represents
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <exception cref="Exception"></exception>
        public SerializableMethodInfoSlim2(MethodInfo methodInfo)
        {
            var mi = methodInfo;
            if (methodInfo.ReflectedType == null) throw new Exception("Cannot serialize MethodInfo without ReflectedType");
            if (methodInfo.IsConstructedGenericMethod)
            {
                GenericArguments = methodInfo.GetGenericArguments().Select(o => GetTypeName(o)).ToList();
                mi = methodInfo.GetGenericMethodDefinition();
            }
            MethodName = mi.Name;
            ReflectedTypeName = GetTypeName(methodInfo.ReflectedType);
            DeclaringTypeName = GetTypeName(methodInfo.DeclaringType);
            ReturnType = GetTypeName(mi.ReturnType);
            ParameterTypes = mi.GetParameters().Select(o => GetTypeName(o.ParameterType)).ToList();
            _MethodInfo = methodInfo;
            Resolved = true;
        }
        public SerializableMethodInfoSlim2(Delegate methodDelegate)
        {
            var methodInfo = methodDelegate.Method;
            var mi = methodInfo;
            if (methodInfo.ReflectedType == null) throw new Exception("Cannot serialize MethodInfo without ReflectedType");
            if (methodInfo.IsConstructedGenericMethod)
            {
                GenericArguments = methodInfo.GetGenericArguments().Select(o => GetTypeName(o)).ToList();
                mi = methodInfo.GetGenericMethodDefinition();
            }
            MethodName = mi.Name;
            ReflectedTypeName = GetTypeName(methodInfo.ReflectedType);
            DeclaringTypeName = GetTypeName(methodInfo.DeclaringType);
            ReturnType = GetTypeName(mi.ReturnType);
            ParameterTypes = mi.GetParameters().Select(o => GetTypeName(o.ParameterType)).ToList();
            _MethodInfo = methodInfo;
            Resolved = true;
        }

        /// <summary>
        /// Deserializes SerializableMethodInfoSlim2 instance from string using System.Text.Json<br />
        /// PropertyNameCaseInsensitive = true is used in deserialization
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [RequiresUnreferencedCode("")]
        public static SerializableMethodInfoSlim2? FromString(string json)
        {
            var ret = string.IsNullOrEmpty(json) || !json.StartsWith("{") ? null : JsonSerializer.Deserialize<SerializableMethodInfoSlim2>(json, DefaultJsonSerializerOptions);
            return ret;
        }
        /// <summary>
        /// Serializes SerializableMethodInfoSlim2 to a string using System.Text.Json
        /// </summary>
        /// <returns></returns>
        public override string ToString() => JsonSerializer.Serialize(this, DefaultJsonSerializerOptions);

        internal static JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// Get Type FullName without assembly info
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        static string GetTypeName(Type? type)
        {
            if (type == null) return "";
            // SpawnDev.WebFS.DokanAsync.GetFileInformationResult, SpawnDev.WebFS, Version=2.0.1.0, Culture=neutral, PublicKeyToken=null
            var ret = !string.IsNullOrEmpty(type.FullName) ? type.FullName : type.Name;
            // Remove assembly info for more leniant matching between versions
            ret = RemoveAssemblyPattern.Replace(ret, "[$1]");
            return ret;
        }

        static Regex RemoveAssemblyPattern = new Regex(@"\[([a-zA-Z]\S*?), (\S*?), (\S*?), (\S*?), (\S*?)\]", RegexOptions.Compiled);

        void Resolve()
        {
            MethodInfo? methodInfo = null;
            if (Resolved) return;
            Resolved = true;
            methodInfo = null;
            var reflectedType = TypeExtensions.GetType(ReflectedTypeName);
            if (reflectedType == null)
            {
                // Reflected type not found
                return;
            }
            var methodsWithName = reflectedType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Where(o => o.Name == MethodName);
            if (!methodsWithName.Any())
            {
                // No method found with this MethodName found in ReflectedType
                return;
            }
            MethodInfo? mi = null;
            foreach (var method in methodsWithName)
            {
                var msi = new SerializableMethodInfoSlim2(method);
                if (msi.ParameterTypes.SequenceEqual(ParameterTypes))
                {
                    mi = method;
                    break;
                }
            }
            if (mi == null)
            {
                // No method found that matches the base method signature
                return;
            }
            if (mi.IsGenericMethod)
            {
                if (GenericArguments == null || !GenericArguments.Any())
                {
                    // Generics information in GenericArguments is missing. Resolve not possible.
                    return;
                }
                var genericTypes = new Type[GenericArguments.Count];
                for (var i = 0; i < genericTypes.Length; i++)
                {
                    var gTypeName = GenericArguments[i];
                    var gType = TypeExtensions.GetType(gTypeName);
                    if (gType == null)
                    {
                        // One of the generic types needed to make the generic method was not found
                        return;
                    }
                    genericTypes[i] = gType;
                }
                methodInfo = mi.MakeGenericMethod(genericTypes);
            }
            else
            {
                methodInfo = mi;
            }
            _MethodInfo = methodInfo;
        }
        /// <summary>
        /// Converts a MethodInfo instance into a string
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public static string SerializeMethodInfo(MethodInfo methodInfo) => new SerializableMethodInfoSlim2(methodInfo).ToString();
        public static string SerializeMethodInfo(Delegate methodDeleate) => new SerializableMethodInfoSlim2(methodDeleate.Method).ToString();

        /// <summary>
        /// Converts a MethodInfo that has been serialized using SerializeMethodInfo into a MethodInfo if serialization is successful or a null otherwise.
        /// </summary>
        /// <param name="serializableMethodInfoJson"></param>
        /// <returns></returns>
        [RequiresUnreferencedCode("")]
        public static MethodInfo? DeserializeMethodInfo(string serializableMethodInfoJson)
        {
            var tmp = FromString(serializableMethodInfoJson);
            return tmp == null ? null : tmp.MethodInfo;
        }
    }
}
