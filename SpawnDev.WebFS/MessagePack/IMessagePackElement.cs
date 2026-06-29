namespace SpawnDev.WebFS.MessagePack
{
    public interface IMessagePackElement
    {
        //static abstract MessagePackSerializerOptions Options { get; }
        //ReadOnlySequence<byte> Data { get; }
        //MessagePackType MessagePackType { get; }

        //static abstract MessagePackElement Deserialize(byte[] data);
        //static abstract T Deserialize<T>(byte[] data);
        //static abstract MessagePackList DeserializeList(byte[] data);
        //static abstract byte[] Serialize(object? data);
        //static abstract Task SerializeAsync(Stream writableStream, object? data, CancellationToken cancellationToken = default);
        object? Get(Type type);
        T Get<T>();
    }
}