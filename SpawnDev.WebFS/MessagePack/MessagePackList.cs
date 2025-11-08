using MessagePack;
using MessagePack.Resolvers;
using System.Buffers;

namespace SpawnDev.WebFS.MessagePack
{
    /// <summary>
    /// MessagePackList for reading lists of varying types 1 at a time
    /// </summary>
    public class MessagePackList
    {
        static MessagePackList()
        {
            Init();
        }
        /// <summary>
        /// Must be called to configure MessagePack for use with MessagePackList
        /// </summary>
        public static void Init()
        {
            var resolver = CompositeResolver.Create(
                MessagePackListResolver.Instance,       // Try custom types first
                ContractlessStandardResolver.Instance,  // Then check for generated formatters
                StandardResolver.Instance               // Finally, use standard built-in formatters
            );
            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            // allow private classes and properties (would be nice if we could do privates types but still ignore private properties)
            //Options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolverAllowPrivate.Instance);
            // default contractless
            Options = options;
            MessagePackSerializer.DefaultOptions = options;
        }
        /// <summary>
        /// The items in the list
        /// </summary>
        public List<MessagePackElement> Items { get; } = new List<MessagePackElement>();
        int _streamStart = 0;
        /// <summary>
        /// The element data
        /// </summary>
        public ReadOnlySequence<byte> Data { get; }
        /// <summary>
        /// The number of elements in the list
        /// </summary>
        public int Length => Items.Count;
        /// <summary>
        /// The number of elements in the list
        /// </summary>
        public int Count => Items.Count;
        /// <summary>
        /// MessagePack options
        /// </summary>
        public static MessagePackSerializerOptions? Options { get; private set; }
        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="stream"></param>
        public MessagePackList(MemoryStream stream) : this(stream.ToArray()) { }
        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="stream"></param>
        public MessagePackList(byte[] stream) : this(new ReadOnlySequence<byte>(stream)) { }
        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="stream"></param>
        public MessagePackList(ReadOnlySequence<byte> stream)
        {
            Data = stream;
            _streamStart = Data.Start.GetInteger();
            var reader = new MessagePackReader(stream);
            var LengthStart = reader.ReadArrayHeader();
            for (var i = 0; i < LengthStart; i++)
            {
                var itemStart = reader.Position.GetInteger() - _streamStart;
                reader.Skip();
                var itemSize = reader.Position.GetInteger() - (itemStart + _streamStart);
                var seq = stream.Slice(itemStart, itemSize);// new ReadOnlySequence<byte>(, itemStart, itemSize);
                Items.Add(new MessagePackElement(seq));
            }
        }
        /// <summary>
        /// Remove the first element and return it as type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public T Shift<T>()
        {
            if (Length == 0)
            {
                throw new Exception("MessagePackList - Out of bounds");
            }
            var ret = GetItem<T>(0);
            Items.RemoveAt(0);
            return ret;
        }
        /// <summary>
        /// Remove the first element and return it as type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public object? Shift(Type type)
        {
            if (Length == 0)
            {
                throw new Exception("MessagePackList - Out of bounds");
            }
            var ret = GetItem(type, 0);
            Items.RemoveAt(0);
            return ret;
        }
        /// <summary>
        /// Get an item as type as index
        /// </summary>
        /// <param name="type"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public object? GetItem(Type type, int index)
        {
            if (index < 0 || index >= Length) throw new ArgumentOutOfRangeException("index");
            return Items[index].Get(type);
        }
        /// <summary>
        /// Get an item as type T as index
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public T GetItem<T>(int index)
        {
            if (index < 0 || index >= Length) throw new ArgumentOutOfRangeException("index");
            return Items[index].Get<T>();
        }
    }
}

