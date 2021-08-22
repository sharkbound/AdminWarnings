using System.Collections.Generic;
using Rocket.API;
using Rocket.Unturned.Player;

namespace AdminWarnings
{
    public class CommandWarn : IRocketCommand
    {

        public List<string> Aliases
        {
            get { return new List<string>(); }
        }

        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Both; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length != 1 && command.Length != 2)
            {
                WarningUtilities.SendMessage(caller, WarningsPlugin.Instance.Translate("wrong_usage"));
                return;
            }

            UnturnedPlayer warnedPlayer = UnturnedPlayer.FromName(command[0]);
            if (warnedPlayer == null)
            {
                WarningUtilities.SendMessage(caller, WarningsPlugin.Instance.Translate("player_not_found", command[0]));
                return;
            }

            if (!WarningsPlugin.util.CheckIfHasData(warnedPlayer))
            {
                WarningsPlugin.util.AddPlayerData(warnedPlayer);
            }

            if (command.Length == 1)
            {
                WarningsPlugin.util.WarnPlayer(caller, warnedPlayer, "", false);
            }
            else if (command.Length == 2)
            {
                WarningsPlugin.util.WarnPlayer(caller, warnedPlayer, command[1], true);
            }

            WarningUtilities.SendMessage(caller, WarningsPlugin.Instance.Translate("warned_caller", warnedPlayer.DisplayName));


        }

        public string Help
        {
            get { return "warns the specified player"; }
        }

        public string Name
        {
            get { return "warn"; }
        }

        public List<string> Permissions
        {
            get { return new List<string> { "warn" }; }
        }

        public string Syntax
        {
            get { return "<player> [reason]"; }
        }
    }
}
