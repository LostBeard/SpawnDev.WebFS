namespace SpawnDev.WebFS.MessagePack
{
    public interface IMessagePackList
    {
        IMessagePackElement GetElement(int index);
        int Count { get; }
//        List<MessagePackElement> Items { get; }

        object? First(Type type);
        T First<T>();
        object? FirstOrDefault(Type type);
        T FirstOrDefault<T>();
        object? GetItem(Type type, int index);
        T GetItem<T>(int index);
        object? Shift(Type type);
        T Shift<T>();
    }
}