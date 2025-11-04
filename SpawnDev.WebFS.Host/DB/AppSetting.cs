using Dapper.Contrib.Extensions;

namespace SpawnDev.DB
{
    [Table("AppSettings")]
    public class AppSetting
    {
        [ExplicitKey]
        public string Id { get; set; } = "";
        public string JsonValue { get; set; } = "";
    }
}


