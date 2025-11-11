using MessagePack;
using MessagePack.Formatters;

namespace SpawnDev.WebFS.MessagePack
{
    /// <summary>
    /// MessagePackList resolver
    /// </summary>
    public class MessagePackElementResolver : IFormatterResolver
    {
        /// <summary>
        /// Instance singleton
        /// </summary>
        public static readonly IFormatterResolver Instance = new MessagePackElementResolver();
        /// <summary>
        /// MessagePackListFormatter singleton
        /// </summary>
        public static readonly IMessagePackFormatter MessagePackListFormatter = new MessagePackListFormatter();
        /// <summary>
        /// MessagePackElementFormatter singleton
        /// </summary>
        public static readonly IMessagePackFormatter MessagePackElementFormatter = new MessagePackElementFormatter();
        /// <inheritdoc/>
        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            if (typeof(T) == typeof(MessagePackList))
            {
                return (IMessagePackFormatter<T>)MessagePackListFormatter;
            }
            else if (typeof(T) == typeof(MessagePackElement))
            {
                return (IMessagePackFormatter<T>)MessagePackElementFormatter;
            }
            // As part of a CompositeResolver, we can return null and it will try the next resolver in the CompositeResolver
            return null!;
        }
    }
}

