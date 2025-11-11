using MessagePack;
using MessagePack.Resolvers;
using System.Buffers;

namespace SpawnDev.WebFS.MessagePack
{
    /// <summary>
    /// Represents an unspecified data structure represented by a ReadOnlySequence&lt;byte>
    /// </summary>
    public class MessagePackElement
    {
        /// <summary>
        /// MessagePackElement MessagePack options.<br/>
        /// Uses MessagePackElement and ContractlessStandardResolver 
        /// so MessagePack can provide similar (de)serialization to
        /// JsonSerializer with JsonElement and List&lt;JsonElement>
        /// </summary>
        public static MessagePackSerializerOptions Options { get; }
        static MessagePackElement()
        {
            Options = MessagePackSerializerOptions.Standard.WithResolver(CompositeResolver.Create
            (
                MessagePackElementResolver.Instance,
                ContractlessStandardResolver.Instance,
                StandardResolver.Instance
            ));
        }
        /// <summary>
        /// The data source
        /// </summary>
        public ReadOnlySequence<byte> Data { get; }
        /// <summary>
        /// Message pack type
        /// </summary>
        public MessagePackType MessagePackType { get; }
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="data"></param>
        public MessagePackElement(ReadOnlySequence<byte> data)
        {
            Data = data;
            var reader = new MessagePackReader(Data);
            MessagePackType = reader.NextMessagePackType;
        }
        /// <summary>
        /// Deserialize this to type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>() => MessagePackSerializer.Deserialize<T>(Data, Options);
        /// <summary>
        /// Deserialize this to type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object? Get(Type type) => MessagePackSerializer.Deserialize(type, Data, Options);
        /// <summary>
        /// Serialize the data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] Serialize(object? data) => MessagePackSerializer.Serialize(data, Options);
        /// <summary>
        /// Serialize the data
        /// </summary>
        /// <param name="writableStream"></param>
        /// <param name="data"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task SerializeAsync(Stream writableStream, object? data, CancellationToken cancellationToken = default) => MessagePackSerializer.SerializeAsync(writableStream, data, Options, cancellationToken);
        /// <summary>
        /// Deserialize to type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static T Deserialize<T>(byte[] data) => MessagePackSerializer.Deserialize<T>(data, Options);
        /// <summary>
        /// Deserialize to MessagePackElement
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static MessagePackElement Deserialize(byte[] data) => MessagePackSerializer.Deserialize<MessagePackElement>(data, Options);
        /// <summary>
        /// Deserialize to MessagePackList
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static MessagePackList DeserializeList(byte[] data) => MessagePackSerializer.Deserialize<MessagePackList>(data, Options);
    }
}

