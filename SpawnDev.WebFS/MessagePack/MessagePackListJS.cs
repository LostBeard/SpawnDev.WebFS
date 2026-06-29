using Microsoft.JSInterop;
using SpawnDev.BlazorJS;

namespace SpawnDev.WebFS.MessagePack
{
    /// <summary>
    /// MessagePackList for reading lists of varying types 1 at a time
    /// </summary>
    public class MessagePackListJS : MessagePackElementJS, IMessagePackList
    {
        /// <inheritdoc/>
        public MessagePackListJS(IJSInProcessObjectReference _ref) : base(_ref) { }

        public int Count => JSRef!.Get<int>("length");

        public object? First(Type type) => GetItem(ResolveType(type), 0);

        public T First<T>() => (T)First(typeof(T))!;

        public object? FirstOrDefault(Type type)
        {
            type = ResolveType(type);
            if (Count == 0) return type.GetDefaultValue();
            var value = First(type);
            return value ?? type.GetDefaultValue();
        }

        public object? Get(Type type) => JSRef!.As(ResolveType(type));

        public T Get<T>() => (T)Get(typeof(T))!;

        public object? GetItem(Type type, int index) => JSRef!.Get(ResolveType(type), index);

        public T GetItem<T>(int index) => (T)GetItem(typeof(T), index)!;

        public object? Shift(Type type) => JSRef!.Call(ResolveType(type), "shift");

        public T Shift<T>() => (T)Shift(typeof(T))!;

        public T FirstOrDefault<T>() => (T)FirstOrDefault(typeof(T))!;

        public IMessagePackElement GetElement(int index) => GetItem<MessagePackElementJS>(index);
    }

}
