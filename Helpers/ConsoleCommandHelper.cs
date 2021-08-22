using Rocket.Unturned.Player;

namespace AdminWarnings.Helpers
{
    class ConsoleCommandHelper
    {
        public static string FormatConsoleCommandString(string cmd, UnturnedPlayer target)
        {
            return cmd
                .Replace("[playername]", target.DisplayName)
                .Replace("[playerid]", target.CSteamID.m_SteamID.ToString());
        }
    }
}
