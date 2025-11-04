namespace SpawnDev.DB
{
    public class AppDBConfig
    {
        /// <summary>
        /// Default value is %USERPROFILE%/[AppFriendlyName]
        /// </summary>
        public string StoragePath
        {
            get => GetAppStorageFolder();
            set => GetAppStorageFolder(value);
        }
        private static string _AppStorageFolder = "";

        private string GetAppStorageFolder(string? value = null)
        {
            if (value == null && !string.IsNullOrEmpty(_AppStorageFolder)) return _AppStorageFolder;
            var newValue = value;
            var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(userProfilePath))
            {
                userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            }
            if (string.IsNullOrEmpty(newValue))
            {
                if (!string.IsNullOrEmpty(userProfilePath))
                {
                    newValue = Path.Combine(userProfilePath, $".{AppDomain.CurrentDomain.FriendlyName}");
                }
            }
            else
            {
                newValue = newValue.Replace("%UserProfile%", userProfilePath, StringComparison.OrdinalIgnoreCase);
                newValue = Path.GetFullPath(Environment.ExpandEnvironmentVariables(newValue));
            }
            if (!string.IsNullOrEmpty(newValue))
            {
                if (!Directory.Exists(newValue))
                {
                    Directory.CreateDirectory(newValue);
                }
                _AppStorageFolder = newValue;
            }
            Console.WriteLine($"SiteInfoConfig.AppStorageFolder: {_AppStorageFolder}");
            return _AppStorageFolder;
        }
        // if DBFile is set, that path will be used for the dbfile
        public string DBFile { get; set; } = "";
        // else if DBFolder is set, the filename will be app.db and will use the folder set below
        public string DBFolder { get; set; } = "";
        // if set DBPass is set it will be used as the DB password
        public string DBPass { get; set; } = "";
    }
}
