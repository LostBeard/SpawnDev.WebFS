using Dapper;
using System.Data.SQLite;
using System.Text.Json;

namespace SpawnDev.DB
{
    public class AppDB : ISettingsStore
    {
        AppDBConfig _config;
        public string DBFolder { get; private set; } = "";
        public string DBFile { get; private set; } = "";
        public string DBPass { get; private set; } = "";
        public string ConnectionString { get; private set; }
        public AppDB()
        {
            _config = new AppDBConfig();
            var appfolder = _config.StoragePath;
            string? dbFolder = null;
            if (!string.IsNullOrEmpty(_config.DBFile))
            {
                DBFile = Path.GetFullPath(Environment.ExpandEnvironmentVariables(_config.DBFile));
                dbFolder = Path.GetDirectoryName(DBFile);
            }
            else if (!string.IsNullOrEmpty(_config.DBFolder))
            {
                dbFolder = Path.GetFullPath(Environment.ExpandEnvironmentVariables(_config.DBFolder));
            }
            else if (!string.IsNullOrEmpty(appfolder))
            {
                dbFolder = appfolder;
            }
            if (string.IsNullOrEmpty(dbFolder)) throw new Exception("Invalid DBFolder");
            DBFolder = dbFolder;
            if (!Directory.Exists(DBFolder)) Directory.CreateDirectory(DBFolder);
            //
            if (string.IsNullOrEmpty(DBFile))
            {
                DBFile = Path.Combine(DBFolder, "app.db");
            }
            DBPass = _config.DBPass;
            ConnectionString = string.IsNullOrEmpty(DBPass) ? $"Data Source={DBFile};Version=3;" : $"Data Source={DBFile};Version=3;Password:{DBPass};";
            VerifyDBExists();
            using var conn = GetConn();
            conn.CreateTableIfNotExists<AppSetting>();
        }
        public bool DBExists() => File.Exists(DBFile);
        public void VerifyDBExists()
        {
            if (!File.Exists(DBFile)) SQLiteConnection.CreateFile(DBFile);
        }
        public SQLiteConnection GetConn()
        {
            var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            return conn;
        }
        public void GetCommand(Action<SQLiteConnection, SQLiteCommand> withCommand)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Connection = conn;
                    withCommand(conn, cmd);
                }
            }
        }
        string AppSettingsTableName = "AppSettings";
        public T GetSetting<T>(string id) => GetSetting<T>(id, default(T)!);
        public T GetSetting<T>(string id, T defaultValue)
        {
            try
            {
                using (var conn = GetConn())
                {
                    var row = conn.ExecuteScalar<string>($"SELECT JsonValue FROM {AppSettingsTableName} WHERE Id = @Id", new { Id = id });
                    if (row != null)
                    {
                        return JsonSerializer.Deserialize<T>(row)!;
                    }
                }
            }
            catch (Exception ex)
            {
                var nmt = true;
            }
            return defaultValue;
        }
        public bool SettingExists(string id)
        {
            using (var conn = GetConn())
            {
                return 1 == conn.ExecuteScalar<int>($"SELECT count(*) FROM {AppSettingsTableName} WHERE Id = @Id", new { Id = id });
            }
        }
        public void RemoveSetting(string id)
        {
            try
            {
                using (var conn = GetConn())
                {
                    conn.Execute($"DELETE FROM {AppSettingsTableName} WHERE Id = @Id", new { Id = id });
                }
            }
            catch { }
        }
        public void SetSetting<T>(string id, T value)
        {
            try
            {
                var json = JsonSerializer.Serialize((object?)value);
                using (var conn = GetConn())
                {
                    var ret = conn.ReplaceInto(new AppSetting { Id = id, JsonValue = json });
                }
            }
            catch (Exception ex)
            {
                var nmt = true;
            }
        }
    }
}
