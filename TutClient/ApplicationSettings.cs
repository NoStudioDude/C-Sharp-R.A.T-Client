using System;
using System.Configuration;

namespace TutClient
{
    public static class ApplicationSettings
    {
        /// <summary>
        /// ALG HASH Base ID
        /// </summary>
        public const int ALG_CLASS_HASH = 4 << 13;
        
        /// <summary>
        /// SHA1 hashing ID
        /// </summary>
        public const int ALG_SID_SHA1 = 4;

        /// <summary>
        /// Frame rate control variable
        /// </summary>
        public static int FPS = 100;

        public static bool HideClient = GetBooleanValue(ConfigurationManager.AppSettings["HideClient"]);
        public static bool IsLinuxServer = GetBooleanValue(ConfigurationManager.AppSettings["IsLinuxServer"]);
        public static string SocketIp = ConfigurationManager.AppSettings["SocketIP"];
        public static int Port = GetIntValue(ConfigurationManager.AppSettings["port"]);

        private static bool GetBooleanValue(string setting)
        {
            return Convert.ToBoolean(setting);
        }

        private static int GetIntValue(string setting)
        {
            return Convert.ToInt32(setting);
        }
    }
}