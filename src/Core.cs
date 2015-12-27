namespace TellySorter
{

    using ManyConsole;
    using NLog;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using TellySorter.Utilities;

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
                return ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
            } catch (Exception e) {
                logger.Fatal(e);
            }
 
            return 1;

        }

        public static IEnumerable<ConsoleCommand> GetCommands()
        {
            return ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof(Core));
        }

    }

}
