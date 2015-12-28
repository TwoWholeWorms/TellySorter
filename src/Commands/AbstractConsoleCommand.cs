namespace TellySorter.Commands
{

    using ManyConsole;
    using NLog;
    using System;
    using System.IO;
    using TellySorter.Utilities;

    abstract public class AbstractConsoleCommand : ConsoleCommand
    {

        readonly static Logger logger = LogManager.GetCurrentClassLogger();
        protected Configuration config;

        public bool Simulate = false;
        public string DbFile = "TellySorter.db";

        bool initialised = false;

        protected AbstractConsoleCommand()
        {

            SkipsCommandSummaryBeforeRunning();

            this.HasOption("s|simulate", "Simulate process (ie, don't actually move or rename files)", s => Simulate = true);
            this.HasOption("d|database=", "Specify database file to use instead of the default", s => DbFile = "TellySorter.db");

        }

        public void Initialise()
        {
            if (initialised) {
                return;
            }

            initialised = true;

            logger.Debug("Initialising things");

            config = Configuration.GetConfig(this);

            if (!Directory.Exists(config.DefaultTargetPath)) {
                logger.Warn(string.Format("The default target path `{0}` does not exist. You should set a default target path with `set DefaultTargetPath /path/to/default/target/path` or create the directory `{0}`", config.DefaultTargetPath));
            }

//            foreach (var pathConfig in config.ShowTargetPaths) {
//                if (!Directory.Exists(pathConfig.Path)) {
//                    throw new Exception(string.Format("The show target path `{0}` for TVDB episode ids `{1}` does not exist.", pathConfig.Path, string.Join(",", pathConfig.ShowIds)));
//                }
//            }

        }

    }

}
