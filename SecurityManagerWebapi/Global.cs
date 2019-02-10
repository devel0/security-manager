using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using static System.Environment;

namespace SecurityManagerWebapi
{

    public class Global
    {

        #region singleton instance
        static object _InstanceLck = new object();
        static Global _Instance;
        public static Global Instance
        {
            get
            {
                if (_Instance != null) return _Instance;

                lock (_InstanceLck)
                {
                    if (_Instance == null) _Instance = new Global();
                }

                return _Instance;
            }
        }
        #endregion

        public static string AppFolder
        {
            get
            {
                var pathname = Path.Combine(Path.Combine(Environment.GetFolderPath(SpecialFolder.UserProfile), ".config"), "securitymanager");

                if (!Directory.Exists(pathname)) Directory.CreateDirectory(pathname);

                return pathname;
            }
        }

        public static string AppConfigPathfilename
        {
            get { return Path.Combine(AppFolder, "config.json"); }
        }

        public static string AppConfigPathfilenameBackup
        {
            get { return Path.Combine(AppFolder, "config.json.bak"); }
        }

        public Config Config { get; private set; }

        void InitConfig()
        {
            if (!File.Exists(AppConfigPathfilename))
            {
                Config = new Config();
                Config.Save();
                //LogError($"config file [{AppConfigPathfilename}] not found");
                //Environment.Exit(1);
            }
            else
            {
                // check mode 600
                if (!LinuxHelper.IsFilePermissionSafe(AppConfigPathfilename, 384))
                {
                    throw new Exception($"invalid file permission [{AppConfigPathfilename}] must set to 700");
                }

                var attrs = File.GetAttributes(AppConfigPathfilename);
                Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(AppConfigPathfilename));

                // backward : ensure securitymanager account
                if (!Config.Credentials.Any(w => w.Name == "Security Manager"))
                {
                    Config.Credentials.Add(new CredInfo()
                    {
                        Name = "Security Manager",
                        Password = Config.AdminPassword,
                        Pin = Config.Pin,
                        CreateTimestamp = DateTime.UtcNow,
                        GUID = Guid.NewGuid().ToString("N"),
                        Level = 99
                    });
                }

                var q = Config.Credentials.Where(w => w.Name == "Security Manager");
                if (q.Count() == 1)
                    q.First().Level = 99;

                // backward : ensure guid, createtimestamp, passwordregenlength, level
                foreach (var x in Config.Credentials)
                {
                    if (string.IsNullOrEmpty(x.GUID)) x.GUID = Guid.NewGuid().ToString("N");
                    if (x.CreateTimestamp == null) x.CreateTimestamp = DateTime.UtcNow;
                    if (x.PasswordRegenLength == 0)
                    {
                        if (!string.IsNullOrEmpty(x.Password))
                            x.PasswordRegenLength = x.Password.Length;
                        else
                            x.PasswordRegenLength = 11;
                    }
                    if (x.Level == 0)
                        x.Level = 1;
                }
            }
        }

        public Global()
        {
            InitConfig();
        }

        void Log(ConsoleColor color, string prefix, string msg)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"{DateTime.Now} ({prefix}) : {msg}");
            Console.ResetColor();
        }

        internal void LogDebug(string msg)
        {
            Log(ConsoleColor.DarkGray, "dbg", msg);
        }

        internal void LogInfo(string msg)
        {
            Log(ConsoleColor.Blue, "nfo", msg);
        }

        internal void LogWarning(string msg)
        {
            Log(ConsoleColor.Yellow, "wrn", msg);
        }

        internal void LogError(string msg)
        {
            Log(ConsoleColor.Red, "err", msg);
        }

    }

}