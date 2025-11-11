using MessagePack;
using System.Buffers;

namespace SpawnDev.WebFS.MessagePack
{
    /// <summary>
    /// MessagePackList for reading lists of varying types 1 at a time
    /// </summary>
    public class MessagePackList : MessagePackElement
    {
        /// <summary>
        /// The items in the list
        /// </summary>
        public List<MessagePackElement> Items { get; } = new List<MessagePackElement>();
        /// <summary>
        /// The number of elements in the list
        /// </summary>
        public int Count => Items.Count;
        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="data"></param>
        public MessagePackList(ReadOnlySequence<byte> data) : base(data)
        {
            if (MessagePackType == MessagePackType.Array)
            {
                var reader = new MessagePackReader(data);
                var start = Data.Start.GetInteger();
                var count = reader.ReadArrayHeader();
                for (var i = 0; i < count; i++)
                {
                    var itemStart = reader.Position.GetInteger() - start;
                    reader.Skip();
                    var itemSize = reader.Position.GetInteger() - (itemStart + start);
                    var seq = data.Slice(itemStart, itemSize);// new ReadOnlySequence<byte>(, itemStart, itemSize);
                    Items.Add(new MessagePackElement(seq));
                }
            }
            else
            {
                throw new Exception("Not an array or list");
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
            if (Count == 0)
            {
                throw new IndexOutOfRangeException();
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
            if (Count == 0)
            {
                throw new IndexOutOfRangeException();
            }
            var ret = GetItem(type, 0);
            Items.RemoveAt(0);
            return ret;
        }
        /// <summary>
        /// Get an item as type at index
        /// </summary>
        /// <param name="type"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public object? GetItem(Type type, int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException("index");
            return Items[index].Get(type);
        }
        /// <summary>
        /// Get the first item
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object? First(Type type) => GetItem(type, 0);
        /// <summary>
        /// Get the first item or null
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object? FirstOrDefault(Type type) => Count == 0 ? null : GetItem(type, 0);
        /// <summary>
        /// Get an item as type T at index
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public T GetItem<T>(int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException("index");
            return Items[index].Get<T>();
        }
        /// <summary>
        /// Get first item as type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T First<T>() => GetItem<T>(0);
        /// <summary>
        /// Get first item as type T or default
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T FirstOrDefault<T>() => Count == 0 ? default! : GetItem<T>(0);
    }
}
