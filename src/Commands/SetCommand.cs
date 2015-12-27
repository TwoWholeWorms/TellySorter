namespace TellySorter.Commands
{
    
    using ManyConsole;
    using NLog;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Mono.Data.Sqlite;

    public class SetCommand : AbstractConsoleCommand
    {

        readonly static Logger logger = LogManager.GetCurrentClassLogger();

        readonly string[] validVariables = {
            "ApiKey",
            "DefaultTargetPath",
            "EpisodeFileFormat",
            "SeasonFolderFormat",
            "SeriesFolderFormat",
        };

        public SetCommand() : base()
        {

            this.IsCommand("set", "Sets a config variable");

            this.AllowsAnyAdditionalArguments(string.Format("<{0}> <value>", string.Join("|", validVariables)));

        }

        public override int Run(string[] remainingArguments)
        {
            if (remainingArguments.Length != 2) {
                throw new ConsoleHelpAsException("The variable name and value are required");
            }

            Configuration config = Configuration.GetConfig(this);

            // TODO: Optimise / DRY this up
            switch (remainingArguments[0]) {
                case "DefaultTargetPath":
                    if (!Directory.Exists(remainingArguments[1])) {
                        throw new ConsoleHelpAsException(string.Format("The directory `{0}` does not exist", remainingArguments[1]));
                    }

                    SqliteManager.SetConfigValue(remainingArguments[0], remainingArguments[1]);
                    config.DefaultTargetPath = remainingArguments[1];

                    logger.Info(string.Format("`{0}` set to `{1}`", remainingArguments[0], remainingArguments[1]));

                    break;

                case "EpisodeFileFormat":
                    SqliteManager.SetConfigValue(remainingArguments[0], remainingArguments[1]);
                    config.EpisodeFileFormat = remainingArguments[1];

                    logger.Info(string.Format("`{0}` set to `{1}`", remainingArguments[0], remainingArguments[1]));

                    break;

                case "SeasonFolderFormat":
                    SqliteManager.SetConfigValue(remainingArguments[0], remainingArguments[1]);
                    config.SeasonFolderFormat = remainingArguments[1];

                    logger.Info(string.Format("`{0}` set to `{1}`", remainingArguments[0], remainingArguments[1]));

                    break;

                case "SeriesFolderFormat":
                    SqliteManager.SetConfigValue(remainingArguments[0], remainingArguments[1]);
                    config.SeasonFolderFormat = remainingArguments[1];

                    logger.Info(string.Format("`{0}` set to `{1}`", remainingArguments[0], remainingArguments[1]));

                    break;

                default:
                    throw new ConsoleHelpAsException(string.Format("Unknown variable `{0}`. Valid variables: {1}", remainingArguments[0], string.Join(",", validVariables)));
            }

            return 0;

        }

    }

}
