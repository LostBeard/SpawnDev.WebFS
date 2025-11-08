using MessagePack;
using MessagePack.Formatters;

namespace SpawnDev.WebFS.MessagePack
{
    /// <summary>
    /// MessagePackList formatter
    /// </summary>
    public class MessagePackListFormatter : IMessagePackFormatter<MessagePackList?>
    {
        /// <inheritdoc/>
        public void Serialize(ref MessagePackWriter writer, MessagePackList? value, MessagePackSerializerOptions options)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public MessagePackList? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return new MessagePackList(reader.Sequence);
        }
    }
}

