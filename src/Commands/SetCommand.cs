namespace TellySorter.Commands
{
    
    using ManyConsole;
    using Mono.Data.Sqlite;
    using NLog;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using TellySorter.Models;

    public class SetCommand : AbstractConsoleCommand
    {

        readonly static Logger logger = LogManager.GetCurrentClassLogger();

        readonly string[] validVariables = {
            "ApiKey",
            "DefaultLanguage",
            "DefaultTargetPath",
            "EpisodeFileFormat",
            "SeasonFolderFormat",
            "SeriesFolderFormat",
            "ShowId",
        };

        public SetCommand() : base()
        {

            this.IsCommand("set", "Sets a config variable");

            this.AllowsAnyAdditionalArguments(string.Format("<{0}> <value>", string.Join("|", validVariables)));

        }

        public override int Run(string[] remainingArguments)
        {
            
            Initialise();

            if (remainingArguments.Length < 2) {
                throw new ConsoleHelpAsException("The variable name and value are required");
            }

            Configuration config = Configuration.GetConfig(this);

            // TODO: Optimise / DRY this up
            switch (remainingArguments[0]) {
                case "ShowId":
                    if (remainingArguments.Length != 3) {
                        throw new ConsoleHelpAsException("The id and TVDB id are required");
                    }

                    Regex r = new Regex(@"^\d+$");
                    Match m = r.Match(remainingArguments[1]);
                    if (!m.Success) {
                        throw new ConsoleHelpAsException(string.Format("The id must be a number, `{0}` is not valid", remainingArguments[1]));
                    }

                    m = r.Match(remainingArguments[2]);
                    if (!m.Success) {
                        throw new ConsoleHelpAsException(string.Format("The TV DB id must be a number, `{0}` is not valid", remainingArguments[2]));
                    }

                    if (Simulate) {
                        logger.Info(string.Format("Simulated: Series `{0}` would be assigned TV DB id `{1}`", remainingArguments[1], remainingArguments[2]));
                    } else {
                        SqliteManager.SetSeriesTvDbId(remainingArguments[1], remainingArguments[2]);

                        logger.Info(string.Format("Series `{0}` assigned TV DB id `{1}`", remainingArguments[1], remainingArguments[2]));

                        Series series = SqliteManager.FindSeries(int.Parse(remainingArguments[1]));
                        TVDBSharp.TVDB tvdb = new TVDBSharp.TVDB(config.ApiKey);
                        TVDBSharp.Models.Show show = tvdb.GetShow(int.Parse(remainingArguments[2]));
                        series.CompleteWith(show);
                    }

                    break;

                case "DefaultTargetPath":
                    if (!Directory.Exists(remainingArguments[1])) {
                        throw new ConsoleHelpAsException(string.Format("The directory `{0}` does not exist", remainingArguments[1]));
                    }

                    if (Simulate) {
                        logger.Info(string.Format("Simulated: `{0}` would be set to `{1}`", remainingArguments[0], remainingArguments[1]));
                    } else {
                        SqliteManager.SetConfigValue(remainingArguments[0], remainingArguments[1]);
                        config.DefaultTargetPath = remainingArguments[1];

                        logger.Info(string.Format("`{0}` set to `{1}`", remainingArguments[0], remainingArguments[1]));
                    }

                    break;

                case "EpisodeFileFormat":
                    if (Simulate) {
                        logger.Info(string.Format("Simulated: `{0}` would be set to `{1}`", remainingArguments[0], remainingArguments[1]));
                    } else {
                        SqliteManager.SetConfigValue(remainingArguments[0], remainingArguments[1]);
                        config.EpisodeFileFormat = remainingArguments[1];

                        logger.Info(string.Format("`{0}` set to `{1}`", remainingArguments[0], remainingArguments[1]));
                    }

                    break;

                case "DefaultLanguage":
                    if (remainingArguments[1].Length != 2) {
                        throw new ConsoleHelpAsException(string.Format("The value `{0}` is not a valid 2-letter language code, eg: en, es, fr, de, nl", remainingArguments[1]));
                    }

                    if (Simulate) {
                        logger.Info(string.Format("Simulated: `{0}` would be set to `{1}`", remainingArguments[0], remainingArguments[1]));
                    } else {
                        SqliteManager.SetConfigValue(remainingArguments[0], remainingArguments[1]);
                        config.DefaultLanguage = remainingArguments[1];

                        logger.Info(string.Format("`{0}` set to `{1}`", remainingArguments[0], remainingArguments[1]));
                    }

                    break;

                case "SeasonFolderFormat":
                    if (Simulate) {
                        logger.Info(string.Format("Simulated: `{0}` would be set to `{1}`", remainingArguments[0], remainingArguments[1]));
                    } else {
                        SqliteManager.SetConfigValue(remainingArguments[0], remainingArguments[1]);
                        config.SeasonFolderFormat = remainingArguments[1];

                        logger.Info(string.Format("`{0}` set to `{1}`", remainingArguments[0], remainingArguments[1]));
                    }

                    break;

                case "SeriesFolderFormat":
                    if (Simulate) {
                        logger.Info(string.Format("Simulated: `{0}` would be set to `{1}`", remainingArguments[0], remainingArguments[1]));
                    } else {
                        SqliteManager.SetConfigValue(remainingArguments[0], remainingArguments[1]);
                        config.SeasonFolderFormat = remainingArguments[1];

                        logger.Info(string.Format("`{0}` set to `{1}`", remainingArguments[0], remainingArguments[1]));
                    }

                    break;

                case "ApiKey":
                    if (Simulate) {
                        logger.Info(string.Format("Simulated: `{0}` would be set to `{1}`", remainingArguments[0], remainingArguments[1]));
                    } else {
                        SqliteManager.SetConfigValue(remainingArguments[0], remainingArguments[1]);
                        config.ApiKey = remainingArguments[1];

                        logger.Info(string.Format("`{0}` set to `{1}`", remainingArguments[0], remainingArguments[1]));
                    }

                    break;

                default:
                    throw new ConsoleHelpAsException(string.Format("Unknown variable `{0}`. Valid variables: {1}", remainingArguments[0], string.Join(",", validVariables)));
            }

            return 0;

        }

    }

}
