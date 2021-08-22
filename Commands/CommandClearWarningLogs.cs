using System.Collections.Generic;
using AdminWarnings.Helpers;
using Rocket.API;

namespace AdminWarnings.Commands
{
    class CommandClearWarningLogs : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "clearwarninglogs";

        public string Help => "Clears the warning logs";

        public string Syntax => "";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            WarningLogger.ClearWarningLogs();
            WarningUtilities.SendMessage(caller, WarningsPlugin.Instance.Translate("cleared_logs"));
        }
    }
}
