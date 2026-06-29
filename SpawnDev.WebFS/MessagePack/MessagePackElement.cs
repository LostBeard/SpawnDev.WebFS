using MessagePack;
using MessagePack.Resolvers;
using System.Buffers;

namespace SpawnDev.WebFS.MessagePack
{
    /// <summary>
    /// Represents an unspecified data structure represented by a ReadOnlySequence&lt;byte>
    /// </summary>
    public class MessagePackElement : IMessagePackElement
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
            // The goal of the code below is to allow compatiblity between the .Net MessagePack and the JS MessagePack
            Options = MessagePackSerializerOptions.Standard.WithResolver(CompositeResolver.Create
            (
                // 1. Maintain your custom wrapper element definitions first
                MessagePackElementResolver.Instance,

                // 2. Intercept numbers/enums from JS and force float64 -> int/long conversion
                FlexibleNumberResolver.Instance,

                // 3. Intercepts standard collections (like object[] arrays) early, allowing 
                // the elements inside to evaluate independently instead of crashing on System.Object.
                DynamicGenericResolver.Instance,

                // 4. Generates map formatters for clean C# classes dynamically.
                // It works like JSON, processes inheritance hierarchies (FindFilesResult), 
                // and maps types like Temp1 and Temp2 cleanly.
                DynamicContractlessObjectResolver.Instance,

                // 5. Default structural mapping fallback for base components
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

