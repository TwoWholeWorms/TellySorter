namespace TellySorter
{

    using NLog;
    using System;
    using System.IO;
    using TellySorter.Utilities;

    class Core
    {

        readonly static Logger logger = LogManager.GetCurrentClassLogger();

        string command;
        string type;

        public static string[] args;

        Configuration config;

        public static void Main(string[] args)
        {

            try {
                Core core = new Core(args);
                core.Run();
            } catch (Exception e) {
                logger.Fatal(e);
            }

        }

        public Core(string[] args)
        {

            Core.args = args;

        }

        public void Initialise()
        {

            logger.Debug("Initialising things");

            if (args.Length < 1) {
                SpitOutUsage();
            }

            command = args[0];
            if (command != "process") {
                if (args.Length < 2) {
                    SpitOutUsage();
                }
                type = args[1];
            }

            config = Configuration.GetConfig();

            if (!Directory.Exists(config.DefaultTargetPath)) {
                throw new Exception(string.Format("The default target path `{0}` does not exist.", config.DefaultTargetPath));
            }

            foreach (var pathConfig in config.EpisodeTargetPaths) {
                if (!Directory.Exists(pathConfig.Path)) {
                    throw new Exception(string.Format("The episode target path `{0}` for TVDB episode ids `{1}` does not exist.", pathConfig.Path, string.Join(",", pathConfig.EpisodeIds)));
                }
            }

        }

        public void Run()
        {

            logger.Info("TellySorter v{0}", CoreAssembly.Version);
            logger.Info("============={0}\n", new String('=', CoreAssembly.Version.ToString().Length));

            Initialise();

            // Can you tell this was written /really/ quickly? o.o
            switch (command) {
                case "add":
                    if (type == "source") {
                        AddSource();
                    } else if (type == "target") {
                        AddTarget();
                    } else {
                        SpitOutUsage();
                    }
                    break;

                case "remove":
                    if (type == "source") {
                        RemoveSource();
                    } else if (type == "target") {
                        RemoveTarget();
                    } else {
                        SpitOutUsage();
                    }
                    break;

                case "set":
                    if (type == "target") {
                        SetDefaultTarget();
                    } else {
                        SpitOutUsage();
                    }
                    break;

                case "process":
                    ProcessFiles();
                    break;
            }

            logger.Info("Woooo! Congratulations, your TV episodes are all sorted now! :D");

        }

        public void SpitOutUsage()
        {

            logger.Info("Usage: TellySorter.exe <add|remove|set|process> [source|target] [--simulate]");
            logger.Info("");
            logger.Info("Examples:");
            logger.Info("");
            logger.Info("    Source folders:");
            logger.Info("        TellySorter.exe add source /path/to/source/file/directory/to/add/to/search/list [--simulate]");
            logger.Info("        TellySorter.exe remove source /path/to/source/file/directory/to/remove/from/search/list [--simulate]");
            logger.Info("");
            logger.Info("    Set the default target folder:");
            logger.Info("        TellySorter.exe set default-target /path/to/target/file/directory/to/add/to/search/list [--simulate]");
            logger.Info("");
            logger.Info("    Episode-specific destination folders:");
            logger.Info("        TellySorter.exe add target /path/to/target/file/directory/to/add/to/search/list <tvdb_series_id> [--simulate]");
            logger.Info("        TellySorter.exe remove target /path/to/target/file/directory/to/remove/from/search/list <tvdb_series_id> [--simulate]");
            logger.Info("");
            logger.Info("    Process files:");
            logger.Info("        TellySorter.exe process [--simulate]");
            logger.Info("");

            throw new Exception("Invalid usage");

        }

        public void ProcessFiles()
        {

            throw new NotImplementedException("TODO: Write this");

        }

        public void SetDefaultTarget()
        {

            throw new NotImplementedException("TODO: Write this");

        }

        public void AddSource()
        {

            throw new NotImplementedException("TODO: Write this");

        }

        public void RemoveSource()
        {

            throw new NotImplementedException("TODO: Write this");

        }

        public void AddTarget()
        {

            throw new NotImplementedException("TODO: Write this");

        }

        public void RemoveTarget()
        {

            throw new NotImplementedException("TODO: Write this");

        }

    }

}
