namespace TellySorter.Commands
{
    
    using ManyConsole;
    using Mono.Data.Sqlite;
    using NLog;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    public class RuleCommand : AbstractConsoleCommand
    {

        readonly static Logger logger = LogManager.GetCurrentClassLogger();

        readonly string[] validActions = {
            "list",
            "ignore",
            "unignore",
            "target",
            "untarget",
        };

        public RuleCommand() : base()
        {

            this.IsCommand("rule", "Manages additional movement rules (eg, completely ignore show, move show somewhere else, etc)");
            this.AllowsAnyAdditionalArguments(string.Format("<{0}> <tvdb_show_id> [path]", string.Join("|", validActions)));

        }

        public override int Run(string[] remainingArguments)
        {

            Initialise();

            if (remainingArguments.Length < 1 || remainingArguments.Length > 3) {
                throw new ConsoleHelpAsException("Not enough arguments");
            }

            Configuration config = Configuration.GetConfig(this);

            Regex regex;
            Match match;
            switch (remainingArguments[0]) {
                case "ignore":
                    if (remainingArguments.Length != 2) {
                        throw new ConsoleHelpAsException("You need to provide a TVDB show ID to ignore a show");
                    }

                    regex = new Regex(@"^\d+$");
                    match = regex.Match(remainingArguments[1]);
                    if (!match.Success) {
                        throw new ConsoleHelpAsException(string.Format("`{0}` is not a valid TVDB show ID.", remainingArguments[1]));
                    }

                    if (SqliteManager.IsShowIgnored(remainingArguments[1])) {
                        throw new ArgumentException(string.Format("Show `{0}` is already ignored", remainingArguments[1]));
                    }

                    if (Simulate) {
                        logger.Info(string.Format("Simulated: Show `{0}` would be ignored", remainingArguments[1]));
                    } else {
                        SqliteManager.AddShowIgnore(remainingArguments[1]);

                        logger.Info(string.Format("Show `{0}` ignored", remainingArguments[1]));
                    }

                    break;

                case "unignore":
                    if (remainingArguments.Length != 2) {
                        throw new ConsoleHelpAsException("You need to provide a TVDB show ID to ignore a show");
                    }

                    regex = new Regex(@"^\d+$");
                    match = regex.Match(remainingArguments[1]);
                    if (!match.Success) {
                        throw new ConsoleHelpAsException(string.Format("`{0}` is not a valid TVDB show ID.", remainingArguments[1]));
                    }

                    if (!SqliteManager.IsShowIgnored(remainingArguments[1])) {
                        throw new ArgumentException(string.Format("Show `{0}` is not being ignored", remainingArguments[1]));
                    }

                    if (Simulate) {
                        logger.Info(string.Format("Simulated: Show `{0}` would be unignored", remainingArguments[1]));
                    } else {
                        SqliteManager.RemoveShowIgnore(remainingArguments[1]);

                        logger.Info(string.Format("Show `{0}` unignored", remainingArguments[1]));
                    }

                    break;

                case "target":
                    if (remainingArguments.Length != 3) {
                        throw new ConsoleHelpAsException("You need to provide a TVDB show ID and a target path to set a show-specific target path");
                    }

                    regex = new Regex(@"^\d+$");
                    match = regex.Match(remainingArguments[1]);
                    if (!match.Success) {
                        throw new ConsoleHelpAsException(string.Format("`{0}` is not a valid TVDB show ID.", remainingArguments[1]));
                    }
                    if (!Directory.Exists(remainingArguments[2])) {
                        throw new ConsoleHelpAsException(string.Format("The directory `{0}` does not exist", remainingArguments[2]));
                    }

                    if (Simulate) {
                        logger.Info(string.Format("Simulated: Show `{0}` would be set to be moved to `{1}`", remainingArguments[1], remainingArguments[2]));
                    } else {
                        SqliteManager.SetShowSpecificTarget(remainingArguments[1], remainingArguments[2]);

                        logger.Info(string.Format("Show `{0}` set to be moved to `{1}`", remainingArguments[1], remainingArguments[2]));
                    }

                    break;

                case "untarget":
                    if (remainingArguments.Length != 2) {
                        throw new ConsoleHelpAsException("You need to provide a TVDB show ID to remove a show-specific target path");
                    }

                    regex = new Regex(@"^\d+$");
                    match = regex.Match(remainingArguments[1]);
                    if (!match.Success) {
                        throw new ConsoleHelpAsException(string.Format("`{0}` is not a valid TVDB show ID.", remainingArguments[1]));
                    }

                    if (Simulate) {
                        logger.Info(string.Format("Simulated: Show `{0}` would be set to be moved to the default target path `{1}`", remainingArguments[1], config.DefaultTargetPath));
                    } else {
                        SqliteManager.RemoveShowSpecificTarget(remainingArguments[1]);

                        logger.Info(string.Format("Show `{0}` will now be moved to the default target path `{1}`", remainingArguments[1], config.DefaultTargetPath));
                    }

                    break;

                case "list":
                    logger.Info("Rules:");
                    logger.Info("------");
                    logger.Info("");
                    var res = SqliteManager.GetRules();
                    if (!res.HasRows) {
                        logger.Info("There are no rules defined. All shows will be moved to this location:");
                        logger.Info("");
                        logger.Info("    {0}", Path.Combine(config.DefaultTargetPath, Path.Combine(config.SeriesFolderFormat, Path.Combine(config.SeasonFolderFormat, config.EpisodeFileFormat))));
                    } else {
                        while (res.Read()) {
                            switch (res["type"].ToString()) {
                                case "ignore":
                                    logger.Info(string.Format("Ignore: Show id `{0}`", res["tvdb_show_id"]));
                                    break;

                                case "target":
                                    logger.Info(string.Format("Show-specific target: Show id `{0}` to path `{1}`", res["tvdb_show_id"], res["path"]));
                                    break;

                                default:
                                    logger.Info(string.Format("Rule id `{0}` has an unknown type `{1}`", res["id"], res["type"]));
                                    break;
                            }
                        }
                    }

                    break;

                default:
                    throw new ConsoleHelpAsException(string.Format("Unknown action `{0}`. Valid actions: {1}", remainingArguments[0], string.Join(",", validActions)));
            }

            return 0;

        }

    }

}
