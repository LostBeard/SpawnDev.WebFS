using SpawnDev.BlazorJS;

namespace SpawnDev.WebFS.MessagePack
{
    public class MessagePackCollection : IMessagePackList
    {
        List<IMessagePackElement> Items = new List<IMessagePackElement>();
        public MessagePackCollection(IEnumerable<IMessagePackElement> items)
        {
            Items = items.ToList();
        }

        public int Count => Items.Count;

        public object? First(Type type) => GetItem(ResolveType(type), 0);

        public T First<T>() => (T)First(typeof(T))!;

        public object? FirstOrDefault(Type type)
        {
            type = ResolveType(type);
            if (Count == 0) return type.GetDefaultValue();
            var value = First(type);
            return value ?? type.GetDefaultValue();
        }

        public object? Get(Type type) => throw new NotImplementedException();

        public T Get<T>() => throw new NotImplementedException();

        public object? GetItem(Type type, int index) => Items[index].Get(ResolveType(type));

        public T GetItem<T>(int index) => (T)GetItem(typeof(T), index)!;

        public object? Shift(Type type)
        {
            var ret = First(type);
            Items.RemoveAt(0);
            return ret;
        }

        public T Shift<T>() => (T)Shift(typeof(T))!;

        public T FirstOrDefault<T>() => (T)FirstOrDefault(typeof(T))!;
        static internal Type ResolveType(Type type)
        {
            if (type == typeof(IMessagePackElement)) return typeof(MessagePackElementJS);
            if (type == typeof(IMessagePackList)) return typeof(MessagePackList);
            return type;
        }

        public IMessagePackElement GetElement(int index) => Items[index];
    }
}