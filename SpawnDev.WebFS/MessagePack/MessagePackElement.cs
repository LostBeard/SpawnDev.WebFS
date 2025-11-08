using MessagePack;
using System.Buffers;

namespace SpawnDev.WebFS.MessagePack
{
    /// <summary>
    /// Represents an unspecified data structure represented by a ReadOnlySequence&lt;byte>
    /// </summary>
    public class MessagePackElement
    {
        /// <summary>
        /// The data source
        /// </summary>
        public ReadOnlySequence<byte> Data { get; }
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="data"></param>
        public MessagePackElement(ReadOnlySequence<byte> data)
        {
            Data = data;
        }
        /// <summary>
        /// Get the item as type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>()
        {
            var ret = MessagePackSerializer.Deserialize<T>(Data);
            return ret;
        }
        /// <summary>
        /// Get the item as type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object? Get(Type type)
        {
            var ret = MessagePackSerializer.Deserialize(type, Data);
            return ret;
        }
    }
}

