using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Rocket.API;
using Rocket.Core.Assets;
using Rocket.Unturned.Player;

namespace AdminWarnings.Helpers
{
    public class WarningLogger
    {
        private static string filePath = $"{Environment.CurrentDirectory}/Plugins/AdminWarnings/WarningLogs.xml";
        public static Asset<WarningLogs> logs = new Asset<WarningLogs>();

        public static void Init()
        {
            logs = new XMLFileAsset<WarningLogs>(filePath);
        }

        public static void LogWarning(string issuerId, string issuerName, UnturnedPlayer victim, string reason)
        {
            logs.Instance.entries.Add(new WarningLog
            {
                AdminId = $"{issuerId} {issuerName}",
                VictimId = $"{victim.CSteamID} {victim.DisplayName}",
                Reason = reason
            });
            logs.Save();
        }

        public static void LogWarning(UnturnedPlayer issuer, UnturnedPlayer victim, string reason)
        {
            logs.Instance.entries.Add(new WarningLog
            {
                AdminId = $"{issuer.CSteamID} {issuer.DisplayName}",
                VictimId = $"{victim.CSteamID} {victim.DisplayName}",
                Reason = reason
            });
            logs.Save();
        }

        public static void ClearWarningLogs()
        {
            logs.Instance.entries.Clear();
            logs.Save();
        }
    }

    public class WarningLogs : IDefaultable
    {
        [XmlArrayItem(ElementName = "Log")]
        public List<WarningLog> entries;

        public void LoadDefaults()
        {
            entries = new List<WarningLog>();
        }
    }

    public class WarningLog
    {
        //[XmlElement("AdminId")]
        [XmlAttribute("Admin")]
        public string AdminId;

        //[XmlElement("VictimId")]
        [XmlAttribute("Victim")]
        public string VictimId;

        [XmlElement("Reason")]
        public string Reason;
    }
}
