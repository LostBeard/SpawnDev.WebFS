using System.Text.Json;

namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// Adds extension methods to List&lt;JsonElement>
    /// </summary>
    internal static class JsonElementListExtensions
    {
        static JsonSerializerOptions DefaultJsonSerializerOptions { get; } = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        /// <summary>
        /// The ShiftAs method removes the first element from an list and returns that removed deserialized element. This method changes the length of the list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_this"></param>
        /// <returns></returns>
        public static T? Shift<T>(this List<JsonElement> _this, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            if (jsonSerializerOptions == null) jsonSerializerOptions = DefaultJsonSerializerOptions;
            var ret = _this[0].Deserialize<T>(jsonSerializerOptions ?? DefaultJsonSerializerOptions);
            _this.RemoveAt(0);
            return ret;
        }
        /// <summary>
        /// The ShiftAs method removes the first element from an list and returns that removed deserialized element. This method changes the length of the list.
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="type"></param>
        /// <param name="jsonSerializerOptions"></param>
        /// <returns></returns>
        public static object? Shift(this List<JsonElement> _this, Type type, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            var ret = _this[0].Deserialize(type, jsonSerializerOptions ?? DefaultJsonSerializerOptions);
            _this.RemoveAt(0);
            return ret;
        }
        /// <summary>
        /// Deserializes the item at the specified index
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="type"></param>
        /// <param name="i"></param>
        /// <param name="jsonSerializerOptions"></param>
        /// <returns></returns>
        public static object? Deserialize(this List<JsonElement> _this, Type type, int i, JsonSerializerOptions? jsonSerializerOptions = null) => _this[i].Deserialize(type, jsonSerializerOptions ?? DefaultJsonSerializerOptions);

        /// <summary>
        /// Deserializes the item at the specified index
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="type"></param>
        /// <param name="i"></param>
        /// <param name="jsonSerializerOptions"></param>
        /// <returns></returns>
        public static object? GetItem(this List<JsonElement> _this, Type type, int i, JsonSerializerOptions? jsonSerializerOptions = null) => _this[i].Deserialize(type, jsonSerializerOptions ?? DefaultJsonSerializerOptions);

        /// <summary>
        /// Deserializes the item at the specified index
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="type"></param>
        /// <param name="i"></param>
        /// <param name="jsonSerializerOptions"></param>
        /// <returns></returns>
        public static object? Get(this List<JsonElement> _this, Type type, int i, JsonSerializerOptions? jsonSerializerOptions = null) => _this[i].Deserialize(type, jsonSerializerOptions ?? DefaultJsonSerializerOptions);
        /// <summary>
        /// Deserializes the item at the specified index
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_this"></param>
        /// <param name="i"></param>
        /// <param name="jsonSerializerOptions"></param>
        /// <returns></returns>
        public static T? Deserialize<T>(this List<JsonElement> _this, int i, JsonSerializerOptions? jsonSerializerOptions = null) => _this[i].Deserialize<T>(jsonSerializerOptions ?? DefaultJsonSerializerOptions);
        /// <summary>
        /// Deserializes the item at the specified index
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_this"></param>
        /// <param name="i"></param>
        /// <param name="jsonSerializerOptions"></param>
        /// <returns></returns>
        public static T? Get<T>(this List<JsonElement> _this, int i, JsonSerializerOptions? jsonSerializerOptions = null) => _this[i].Deserialize<T>(jsonSerializerOptions ?? DefaultJsonSerializerOptions);
        /// <summary>
        /// Deserializes the item at the specified index
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_this"></param>
        /// <param name="i"></param>
        /// <param name="jsonSerializerOptions"></param>
        /// <returns></returns>
        public static T? GetItem<T>(this List<JsonElement> _this, int i, JsonSerializerOptions? jsonSerializerOptions = null) => _this[i].Deserialize<T>(jsonSerializerOptions ?? DefaultJsonSerializerOptions);
    }
}
