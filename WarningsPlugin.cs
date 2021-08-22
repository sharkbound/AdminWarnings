using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using AdminWarnings.Helpers;
using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using logger = Rocket.Core.Logging.Logger;

namespace AdminWarnings
{
    public class WarningsPlugin : RocketPlugin<WarningsConfig>
    {
        public static MethodInfo onInputCommitted =
            typeof(CommandWindow).GetMethod("onInputCommitted", BindingFlags.Instance | BindingFlags.NonPublic);
        public static WarningsPlugin Instance;
        public static WarningUtilities util = new WarningUtilities();

        public override TranslationList DefaultTranslations
        {
            get
            {
                return new TranslationList
                {
                    {"warning", "You have you given a warning! Current warnings: {0}"},
                    {"warning_reason", "You have been given a warning! Reason: '{0}'"},
                    {"warning_count_self", "You currently have {0} warnings!"},
                    {"warning_count_admin", "'{0}' currently has {1} warnings!"},
                    {
                        "warning_ban",
                        "You have been banned because you reached {0} warnings! Ban duration (seconds): {1}"
                    },
                    {
                        "warning_ban_reason",
                        "You have been banned because you reached {0} warnings! Reason: '{1}' Ban duration (seconds): {2}"
                    },
                    {"warning_kick", "You have been kicked because you reached {0} warnings!"},
                    {"warning_kick_reason", "You have been kicked because you reached {0} warnings! Reason: '{1}'"},
                    {"warned_caller", "You have warned player: {0}"},
                    {"warned_caller_reason", "You have warned player: '{0}' for '{1}'"},
                    {"player_not_found", "A player by the name of '{0}' could not be found!"},
                    {"wrong_usage", "Correct command usage: /warn <player> [reason]"},
                    {"wrong_usage_removewarn", "Correct command usage: /removewarn <player> [amount]"},
                    {"console_player_warning", "'{0}' has warned '{1}', '{1}' is at {2} warnings"},
                    {"console_player_banned", "'{0}' has warned '{1}', '{1}' was banned for {2} seconds"},
                    {
                        "console_player_banned_reason",
                        "'{0}' has warned '{1}', '{1}' was banned for {2} seconds with the reason '{3}'"
                    },
                    {"console_player_kicked", "'{0}' has warned '{1}', '{1}' was kicked"},
                    {"console_player_kicked_reason", "'{0}' has warned '{1}', '{1}' was kicked with the reason '{2}'"},
                    {"public_player_banned", "'{0}' has received {1} warnings and was banned for {2} seconds!"},
                    {"public_player_kicked", "'{0}' has received {1} warnings and was kicked!"},
                    {"public_player_warned", "'{0}' has been giving a warning, they are currently at {1} warnings!"},
                    {
                        "console_warnings_noparameter",
                        "You must enter a player when calling this command from the console!"
                    },
                    {"public_player_warned_reason", "'{0}' has been giving a warning! Reason: {1}"},
                    {"remove_warn", "Removed {0} warnings from '{1}'!"},
                    {"no_data", "'{0}' does not have any warnings!"},
                    {"cleared_logs", "Cleared warning logs!"},
                    {"console_command", "Ran command '{0}' because player:{1} hit {2} warnings"}
                };
            }
        }

        protected override void Load()
        {
            WarningUtilities.Log("AdminWarnings has Loaded!");
            Instance = this;

            util.RemoveExpiredWarnings(1000);
            WarningLogger.Init();
        }

        protected override void Unload()
        {
            WarningUtilities.Log("AdminWarnings has Unloaded!");
        }
    }

    public class WarningUtilities
    {
        public void RemoveExpiredWarnings(int delay)
        {
            new Thread(() =>
            {
                Thread.Sleep(delay);
                foreach (var playerWarning in GetAllPlayerWarnings())
                {
                    if (GetDaysSinceWarning(playerWarning.DateAdded) >=
                        WarningsPlugin.Instance.Configuration.Instance.DaysWarningsExpire)
                    {
                        RemovePlayerData(playerWarning);
                    }
                }
            }).Start();
        }

        public int GetDaysSinceWarning(DateTime warningDate)
        {
            return (int) (DateTime.Now - warningDate).TotalDays;
        }

        public int GetPlayerWarnings(UnturnedPlayer P)
        {
            return GetAllPlayerWarnings()
                .FirstOrDefault(pWarning => pWarning.CSteamID.ToString() == P.CSteamID.ToString()).Warnings;
        }

        public PlayerWarning GetPlayerData(UnturnedPlayer P)
        {
            return GetAllPlayerWarnings()
                .FirstOrDefault(pWarning => pWarning.CSteamID.ToString() == P.CSteamID.ToString());
        }

        public void DecreasePlayerWarnings(UnturnedPlayer player, int amount)
        {
            PlayerWarning PlayerData = GetPlayerData(player);
            if (PlayerData.Warnings > 0)
            {
                PlayerData.Warnings -= amount;
                Save();
            }

            if (GetPlayerWarnings(player) <= 0)
            {
                RemovePlayerData(PlayerData);
            }
        }

        public bool CheckIfHasData(UnturnedPlayer P)
        {
            var pWarning = GetAllPlayerWarnings()
                .FirstOrDefault(warning => warning.CSteamID.ToString() == P.CSteamID.ToString());
            return pWarning != null;
        }

        public void WarnPlayer(IRocketPlayer caller, UnturnedPlayer Player, string reason, bool reasonIncluded)
        {
            bool actionTaken = false;
            PlayerWarning pData = GetPlayerData(Player);
            pData.Warnings += 1;
            Save();

            if (MatchesWarningPoint(pData.Warnings))
            {
                var point = GetWarningPoint(pData.Warnings);

                if (!string.IsNullOrEmpty(point.ConsoleCommand))
                {
                    var cmd = ConsoleCommandHelper.FormatConsoleCommandString(point.ConsoleCommand.ToLower(), Player);
                    WarningsPlugin.onInputCommitted.Invoke(Dedicator.commandWindow, new object[]{cmd});
                    logger.Log(WarningsPlugin.Instance.Translate("console_command", cmd, Player.DisplayName,
                        point.WarningsToTrigger));
                }
                else if (point.KickPlayer)
                {
                    if (reasonIncluded)
                    {
                        KickPlayer(Player, reason, pData.Warnings);
                        LogWarning(WarningsPlugin.Instance.Translate("console_player_kicked_reason",
                            GetPlayerName(caller), Player.DisplayName, reason));
                    }
                    else
                    {
                        KickPlayer(Player, pData.Warnings);
                        LogWarning(WarningsPlugin.Instance.Translate("console_player_kicked", GetPlayerName(caller),
                            Player.DisplayName));
                    }

                    actionTaken = true;

                    if (GetConfigAnnouceMessageServerWide())
                        UnturnedChat.Say(
                            WarningsPlugin.Instance.Translate("public_player_kicked", Player.DisplayName,
                                pData.Warnings), GetMessageColor());
                }
                else if (point.BanPlayer)
                {
                    if (reasonIncluded)
                    {
                        BanPlayer(Player, reason, pData.Warnings, point.BanLengthSeconds, caller);
                        LogWarning(WarningsPlugin.Instance.Translate("console_player_banned_reason",
                            GetPlayerName(caller), Player.DisplayName, point.BanLengthSeconds, reason));
                    }
                    else
                    {
                        BanPlayer(Player, pData.Warnings, point.BanLengthSeconds, caller);
                        LogWarning(WarningsPlugin.Instance.Translate("console_player_banned", GetPlayerName(caller),
                            Player.DisplayName, point.BanLengthSeconds));
                    }

                    actionTaken = true;

                    if (GetConfigAnnouceMessageServerWide())
                        UnturnedChat.Say(
                            WarningsPlugin.Instance.Translate("public_player_banned", Player.DisplayName,
                                pData.Warnings, point.BanLengthSeconds), GetMessageColor());
                }
            }

            if (!actionTaken)
            {
                if (WarningsPlugin.Instance.Configuration.Instance.AnnouceWarningsServerWide)
                {
                    PublicWarnPlayer(Player, pData, reason, reasonIncluded);
                }
                else
                {
                    PrivateWarnPlayer(Player, pData, reason, reasonIncluded);
                }

                LogWarning(WarningsPlugin.Instance.Translate("console_player_warning", GetPlayerName(caller),
                    Player.DisplayName, pData.Warnings));
            }

            var allWarningPoints = GetAllWarningPoints();
            if (pData.Warnings >= allWarningPoints[allWarningPoints.Count - 1].WarningsToTrigger)
            {
                RemovePlayerData(pData);
                Save();
            }

            if (caller is ConsolePlayer)
                WarningLogger.LogWarning(0.ToString(), "*Console*", Player, reason);
            else
                WarningLogger.LogWarning((UnturnedPlayer) caller, Player, reason);
        }

        public void PublicWarnPlayer(UnturnedPlayer Player, PlayerWarning pData, string reason, bool reasonIncluded)
        {
            if (reasonIncluded)
                TellPlayerWarning(Player,
                    WarningsPlugin.Instance.Translate("public_player_warned_reason", Player.DisplayName, reason));
            else
                TellPlayerWarning(Player,
                    WarningsPlugin.Instance.Translate("public_player_warned", Player.DisplayName, pData.Warnings));
        }

        public void PrivateWarnPlayer(UnturnedPlayer Player, PlayerWarning pData, string reason, bool reasonIncluded)
        {
            if (reasonIncluded)
                SendMessage(Player, WarningsPlugin.Instance.Translate("warning_reason", reason));
            else
                SendMessage(Player, WarningsPlugin.Instance.Translate("warning", pData.Warnings));
        }

        public string GetPlayerName(IRocketPlayer caller)
        {
            if (caller is ConsolePlayer)
            {
                return "console";
            }
            else
            {
                return ((UnturnedPlayer) caller).DisplayName;
            }
        }

        public bool GetConfigAnnouceMessageServerWide()
        {
            if (WarningsPlugin.Instance.Configuration.Instance.AnnouceWarningKicksAndBansServerWide)
                return true;
            else
                return false;
        }

        public void TellPlayerWarning(UnturnedPlayer Player, string message)
        {
            if (GetConfigAnnouceMessageServerWide())
            {
                UnturnedChat.Say(message, GetMessageColor());
            }
            else
            {
                UnturnedChat.Say(Player, message, GetMessageColor());
            }
        }

        public void AddPlayerData(UnturnedPlayer player)
        {
            WarningsPlugin.Instance.Configuration.Instance.PlayerWarnings.Add(new PlayerWarning
            {
                Warnings = 0,
                CSteamID = player.CSteamID.ToString(),
                DateAdded = DateTime.Now
            });
            Save();
        }

        public void KickPlayer(UnturnedPlayer player, int warnings)
        {
            player.Kick(WarningsPlugin.Instance.Translate("warning_kick", warnings));
        }

        public void KickPlayer(UnturnedPlayer player, string reason, int warnings)
        {
            player.Kick(WarningsPlugin.Instance.Translate("warning_kick_reason", warnings, reason));
        }

        public void BanPlayer(UnturnedPlayer player, int warnings, uint banDuration, IRocketPlayer caller)
        {
            CSteamID judge = (CSteamID) 0;
            if (!(caller is ConsolePlayer))
            {
                judge = ((UnturnedPlayer) caller).CSteamID;
            }

            SteamBlacklist.ban(player.CSteamID, GetIP(player.CSteamID), judge,
                WarningsPlugin.Instance.Translate("warning_ban", warnings, banDuration),
                banDuration);
        }

        public void BanPlayer(UnturnedPlayer player, string reason, int warnings, uint banDuration,
            IRocketPlayer caller)
        {
            CSteamID judge = (CSteamID) 0;
            if (!(caller is ConsolePlayer))
            {
                judge = ((UnturnedPlayer) caller).CSteamID;
            }

            SteamBlacklist.ban(player.CSteamID, GetIP(player.CSteamID), judge,
                WarningsPlugin.Instance.Translate("warning_ban_reason", warnings, reason, banDuration),
                banDuration);
        }

        public uint GetIP(CSteamID ID)
        {
            P2PSessionState_t p2PSessionStateT;
            uint ip;

            if (!SteamGameServerNetworking.GetP2PSessionState(ID, out p2PSessionStateT))
            {
                ip = 0;
            }
            else
            {
                ip = p2PSessionStateT.m_nRemoteIP;
            }

            return ip;
        }

        public SteamPlayer GetSteamPlayerFromID(string ID)
        {
            return Provider.clients.FirstOrDefault(steamP => steamP.playerID.steamID.ToString() == ID);
        }

        public Color GetMessageColor()
        {
            Color MsgColor;

            MsgColor = UnturnedChat.GetColorFromName(WarningsPlugin.Instance.Configuration.Instance.MessageColor,
                Color.green);
            return MsgColor;
        }

        public bool MatchesWarningPoint(int warnings)
        {
            WarningPoint p = GetAllWarningPoints().FirstOrDefault(point => warnings == point.WarningsToTrigger);
            if (p == null)
                return false;
            return true;
        }

        public WarningPoint GetWarningPoint(int warnings)
        {
            return GetAllWarningPoints().FirstOrDefault(point => warnings == point.WarningsToTrigger);
        }

        public List<PlayerWarning> GetAllPlayerWarnings()
        {
            return WarningsPlugin.Instance.Configuration.Instance.PlayerWarnings;
        }

        public List<WarningPoint> GetAllWarningPoints()
        {
            return WarningsPlugin.Instance.Configuration.Instance.WarningPoints;
        }

        public void RemovePlayerData(PlayerWarning data)
        {
            WarningsPlugin.Instance.Configuration.Instance.PlayerWarnings.Remove(data);
            Save();
        }

        public void Save()
        {
            WarningsPlugin.Instance.Configuration.Save();
        }

        public static void Log(string msg)
        {
            logger.Log(msg);
        }

        public static void LogWarning(string msg)
        {
            logger.LogWarning(msg);
        }

        public static void SendMessage(IRocketPlayer caller, string message)
        {
            if (caller is ConsolePlayer)
                Log(message);
            else
                UnturnedChat.Say(caller, message, WarningsPlugin.util.GetMessageColor());
        }
    }
}