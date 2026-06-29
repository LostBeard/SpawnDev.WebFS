using SpawnDev.BlazorJS;

namespace SpawnDev.WebFS.MessagePack
{
    public class MessagePackElementUnpacked : IMessagePackElement
    {
        public object? Value { get; set; }
        public object? Get(Type type)
        {
            if (Value == null) return type.GetDefaultValue();
            if (Value.GetType() == type) return Value;
            return Convert.ChangeType(Value, type);
        }
        public T Get<T>() => Value == null ? default : (T)Value;
        public MessagePackElementUnpacked(object? value)
        {
            Value = value;
        }
    }
}