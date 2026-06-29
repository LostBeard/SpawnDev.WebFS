using Microsoft.JSInterop;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.MessagePack;
using SpawnDev.BlazorJS.RemoteJSRuntime;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace SpawnDev.WebFS.MessagePack
{
    /// <summary>
    /// Represents an unspecified data structure represented by a ReadOnlySequence&lt;byte>
    /// </summary>
    public class MessagePackElementJS : JSObject, IMessagePackElement
    {
        /// <inheritdoc/>
        public MessagePackElementJS(IJSInProcessObjectReference _ref) : base(_ref) { }

        public object? Get(Type type)
        {
            var resolvedType = ResolveType(type);
            return JSRef!.As(resolvedType);
        }

        public T Get<T>()
        {
            if (typeof(T) == typeof(IMessagePackElement))
            {
                return (T)(object)JSRef!.As<MessagePackElementJS>();
            }
            else if (typeof(T) == typeof(IMessagePackList))
            {
                return (T)(object)JSRef!.As<MessagePackListJS>();
            }
            else
            {
                return JSRef!.As<T>();
            }
        }

        public static Uint8Array Serialize(object? data) => MessagePackSerializer.Encode(data!);

        public static JSObject Deserialize(Uint8Array data) => MessagePackSerializer.Decode(data);

        public static T Deserialize<T>(Uint8Array data)
        {
            if (typeof(T) == typeof(IMessagePackElement))
            {
                return (T)(object)MessagePackSerializer.Decode<MessagePackElementJS>(data);
            }
            else if (typeof(T) == typeof(IMessagePackList))
            {
                return (T)(object)MessagePackSerializer.Decode<MessagePackListJS>(data);
            }
            else
            {
                return MessagePackSerializer.Decode<T>(data);
            }
        }
        static internal Type ResolveType(Type type)
        {
            if (type == typeof(IMessagePackElement)) return typeof(MessagePackElementJS);
            if (type == typeof(IMessagePackList)) return typeof(MessagePackList);
            return type;
        }
    }
}

