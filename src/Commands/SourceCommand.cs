namespace TellySorter.Commands
{
    
    using ManyConsole;
    using NLog;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Mono.Data.Sqlite;

    public class SourceCommand : AbstractConsoleCommand
    {

        readonly static Logger logger = LogManager.GetCurrentClassLogger();

        readonly string[] validActions = {
            "list",
            "add",
            "remove",
        };

        public SourceCommand() : base()
        {

            this.IsCommand("source", "Manages source paths");

            this.AllowsAnyAdditionalArguments(string.Format("<{0}> [path]", string.Join("|", validActions)));

        }

        public override int Run(string[] remainingArguments)
        {
            
            Initialise();

            if (remainingArguments.Length < 1 || remainingArguments.Length > 2) {
                throw new ConsoleHelpAsException("Invalid arguments");
            }

            Configuration config = Configuration.GetConfig(this);

            switch (remainingArguments[0]) {
                case "add":
                    if (remainingArguments.Length != 2) {
                        throw new ConsoleHelpAsException("Path is required when adding a path");
                    }

                    if (!Directory.Exists(remainingArguments[1])) {
                        throw new ConsoleHelpAsException(string.Format("The directory `{0}` does not exist", remainingArguments[1]));
                    }

                    if (SqliteManager.HasSourcePath(remainingArguments[1])) {
                        throw new ArgumentException(string.Format("Source path `{0}` has already been added", remainingArguments[1]));
                    }

                    if (Simulate) {
                        logger.Info(string.Format("Simulated: New source path `{0}` would be added", remainingArguments[1]));
                    } else {
                        SqliteManager.AddSourcePath(remainingArguments[1]);

                        logger.Info(string.Format("New source path `{0}` added", remainingArguments[1]));
                    }

                    break;

                case "remove":
                    if (remainingArguments.Length != 2) {
                        throw new ConsoleHelpAsException("Path is required when removing a path");
                    }

                    if (!SqliteManager.HasSourcePath(remainingArguments[1])) {
                        throw new ArgumentException(string.Format("Source path `{0}` is not in the database", remainingArguments[1]));
                    }

                    if (Simulate) {
                        logger.Info(string.Format("Simulated: Source path `{0}` would be removed", remainingArguments[1]));
                    } else {
                        SqliteManager.RemoveSourcePath(remainingArguments[1]);

                        logger.Info(string.Format("Source path `{0}` removed", remainingArguments[1]));
                    }

                    break;

                case "list":
                    logger.Info("Source paths:");
                    logger.Info("-------------");
                    logger.Info("");
                    var res = SqliteManager.GetSourcePaths();
                    if (!res.HasRows) {
                        logger.Info("You haven't added any paths yet");
                    } else {
                        while (res.Read()) {
                            logger.Info("{0}", res["path"]);
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
