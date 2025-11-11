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
            if (reader.NextMessagePackType == MessagePackType.Array)
            {
                return new MessagePackList(reader.Sequence);
            }
            else
            {
                return new MessagePackElement(reader.Sequence);
            }
        }
    }
}

