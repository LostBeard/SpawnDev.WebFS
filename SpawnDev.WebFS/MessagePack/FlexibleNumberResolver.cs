using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace SpawnDev.WebFS.MessagePack
{
    public static class FlexibleNumberResolver
    {
        public static readonly IFormatterResolver Instance = CompositeResolver.Create(
                new IMessagePackFormatter[] {
                    FlexibleInt32Formatter.Instance,
                    FlexibleInt64Formatter.Instance
                },
                new IFormatterResolver[] { }
            );
        // Custom Formatter to safely convert float64 to int
        public class FlexibleInt32Formatter : IMessagePackFormatter<int>
        {
            public static readonly FlexibleInt32Formatter Instance = new FlexibleInt32Formatter();
            public void Serialize(ref MessagePackWriter writer, int value, MessagePackSerializerOptions options) => writer.Write(value);
            public int Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                var type = reader.NextMessagePackType;
                if (type == MessagePackType.Float)
                {
                    double doubleValue = reader.ReadDouble(); // Read code 203 float 64
                    return (int)doubleValue; // Safely convert to C# int
                }
                return reader.ReadInt32(); // Default behavior
            }
        }

        // Custom Formatter to safely convert float64 to long (fixes Ticks / CreationTime fields)
        public class FlexibleInt64Formatter : IMessagePackFormatter<long>
        {
            public static readonly FlexibleInt64Formatter Instance = new FlexibleInt64Formatter();
            public void Serialize(ref MessagePackWriter writer, long value, MessagePackSerializerOptions options) => writer.Write(value);
            public long Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                var type = reader.NextMessagePackType;
                if (type == MessagePackType.Float)
                {
                    double doubleValue = reader.ReadDouble();
                    return (long)doubleValue;
                }
                return reader.ReadInt64();
            }
        }
    }
}

