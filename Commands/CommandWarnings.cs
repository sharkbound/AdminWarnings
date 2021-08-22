using System.Collections.Generic;
using Rocket.API;
using Rocket.Unturned.Player;

namespace AdminWarnings
{
    public class CommandWarnings : IRocketCommand
    {
        public List<string> Aliases
        {
            get { return new List<string> { "warns" }; }
        }

        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Both; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length == 0)
            {
                if (caller is ConsolePlayer)
                {
                    WarningUtilities.SendMessage(caller, WarningsPlugin.Instance.Translate("console_warnings_noparameter"));
                    return;
                }
                else
                {
                    UnturnedPlayer uCaller = (UnturnedPlayer)caller;
                    checkForDataAndSendMessage(uCaller, caller, true);
                }
            }
            else if (command.Length == 1)
            {
                UnturnedPlayer uCaller = UnturnedPlayer.FromName(command[0]);
                if (uCaller == null)
                {
                    WarningUtilities.SendMessage(caller, WarningsPlugin.Instance.Translate("player_not_found", command[0]));
                    return;
                }
                
                checkForDataAndSendMessage(uCaller, caller, false);
            }
        }

        void checkForDataAndSendMessage(UnturnedPlayer player, IRocketPlayer caller, bool checkForCallersWarnings)
        {
            int warnings = 0;

            if (checkForCallersWarnings)
            {
                player = (UnturnedPlayer)caller;
            }

            if(WarningsPlugin.util.CheckIfHasData(player))
            {
                warnings = WarningsPlugin.util.GetPlayerWarnings(player);
            }

            if (checkForCallersWarnings)
            {
                WarningUtilities.SendMessage(caller, WarningsPlugin.Instance.Translate("warning_count_self", warnings));
            }
            else
            {
                WarningUtilities.SendMessage(caller, WarningsPlugin.Instance.Translate("warning_count_admin", player.DisplayName, warnings));
            }
        }

        public string Help
        {
            get { return "Gets a players current warnings"; }
        }

        public string Name
        {
            get { return "warnings"; }
        }

        public List<string> Permissions
        {
            get { return new List<string> { "warnings" }; }
        }

        public string Syntax
        {
            get { return "[player]"; }
        }
    }
}
