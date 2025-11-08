using MessagePack;
using MessagePack.Formatters;

namespace SpawnDev.WebFS.MessagePack
{
    /// <summary>
    /// MessagePackElement formatter
    /// </summary>
    public class MessagePackElementFormatter : IMessagePackFormatter<MessagePackElement?>
    {
        /// <inheritdoc/>
        public void Serialize(ref MessagePackWriter writer, MessagePackElement? value, MessagePackSerializerOptions options)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public MessagePackElement? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return new MessagePackElement(reader.Sequence);
        }
    }
}

