namespace TellySorter
{

    using ManyConsole;
    using NLog;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using TellySorter.Utilities;
    using Mono.Data.Sqlite;

    class Core
    {

        readonly static Logger logger = LogManager.GetCurrentClassLogger();

        public static int Main(string[] args)
        {

            logger.Info("TellySorter v{0}", CoreAssembly.Version);
            logger.Info("============={0}", new String('=', CoreAssembly.Version.ToString().Length));
            logger.Info("");

            try {
                var commands = GetCommands();
                int result = ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
                SqliteManager.CloseConnection();
                return result;
            } catch (Exception e) {
                logger.Fatal(e);
                SqliteManager.CloseConnection();
            }
 
            return 1;

        }

        public static IEnumerable<ConsoleCommand> GetCommands()
        {
            return ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof(Core));
        }

    }

}
