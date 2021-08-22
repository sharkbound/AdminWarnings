using System.Collections.Generic;
using Rocket.API;
using Rocket.Unturned.Player;

namespace AdminWarnings
{
    public class CommandRemoveWarn : IRocketCommand
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
                WarningUtilities.SendMessage(caller, WarningsPlugin.Instance.Translate("wrong_usage_removewarn"));
                return;
            }

            UnturnedPlayer targetPlayer = UnturnedPlayer.FromName(command[0]);
            if (targetPlayer == null)
            {
                WarningUtilities.SendMessage(caller, WarningsPlugin.Instance.Translate("player_not_found", command[0]));
                return;
            }

            if (!WarningsPlugin.util.CheckIfHasData(targetPlayer))
            {
                WarningUtilities.SendMessage(caller, WarningsPlugin.Instance.Translate("no_data", targetPlayer.DisplayName));
                return;
            }

            if (command.Length == 1)
            {
                WarningsPlugin.util.DecreasePlayerWarnings(targetPlayer, 1);
                WarningUtilities.SendMessage(caller, WarningsPlugin.Instance.Translate("remove_warn", 1, targetPlayer.DisplayName));
            }
            else if (command.Length == 2)
            {
                int decreaseAmount = 0;
                if (!int.TryParse(command[1], out decreaseAmount))
                {
                    WarningUtilities.SendMessage(caller, WarningsPlugin.Instance.Translate("wrong_usage_removewarn"));
                    return;
                }

                WarningsPlugin.util.DecreasePlayerWarnings(targetPlayer, decreaseAmount);
                WarningUtilities.SendMessage(caller, WarningsPlugin.Instance.Translate("remove_warn",  decreaseAmount, targetPlayer.DisplayName));

            }

        }

        public string Help
        {
            get { return "Removes warnings from a player"; }
        }

        public string Name
        {
            get { return "removewarn"; }
        }

        public List<string> Permissions
        {
            get { return new List<string> { "removewarn" }; }
        }

        public string Syntax
        {
            get { return "<player> [amount]"; }
        }
    }
}
