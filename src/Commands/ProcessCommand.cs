namespace TellySorter.Commands
{
    
    using ManyConsole;
    using NLog;
    using System;
    using System.IO;
    using System.Collections.Generic;

    public class ProcessCommand : AbstractConsoleCommand
    {

        readonly static Logger logger = LogManager.GetCurrentClassLogger();

        public ProcessCommand() : base()
        {

            this.IsCommand("process", "Run the file sorting process according to the rules you've previously defined");

        }

        public override int Run(string[] remainingArguments)
        {

            logger.Info("Processing directories");
            logger.Info("----------------------");
            logger.Info("");

            using (var sourcePathsRes = SqliteManager.GetSourcePaths()) {
                
                var dirs = TraverseTree(sourcePathsRes["path"].ToString());
                foreach (var dir in dirs) {
                    
                    string[] files = null;
                    try {
                        files = System.IO.Directory.GetFiles(dir);
                    } catch (UnauthorizedAccessException e) {
                        logger.Warn("{0}: Permission denied", dir);
                        continue;
                    } catch (System.IO.DirectoryNotFoundException e) {
                        logger.Warn("{0}: Directory was deleted or moved(?!)", dir);
                        continue;
                    }
                    foreach (string file in files) {
                        try {
                            // Identify the file

                        } catch (System.IO.FileNotFoundException e) {
                            // If file was deleted by a separate application
                            //  or thread since the call to TraverseTree()
                            // then just continue.
                            Console.WriteLine(e.Message);
                            continue;
                        }
                    }

                }

            }

            logger.Info("Woooo! Congratulations, your TV episodes are all sorted now! :D");

            return 0;

        }

        public static Stack<string> TraverseTree(string root)
        {
            
            // Data structure to hold names of subfolders to be
            // examined for files.
            var dirs = new Stack<string>(20);

            if (!System.IO.Directory.Exists(root)) {
                throw new ArgumentException();
            }

            dirs.Push(root);

            while (dirs.Count > 0) {
                string currentDir = dirs.Pop();
                string[] subDirs;
                try {
                    subDirs = System.IO.Directory.GetDirectories(currentDir);
                }
                // An UnauthorizedAccessException exception will be thrown if we do not have
                // discovery permission on a folder or file. It may or may not be acceptable 
                // to ignore the exception and continue enumerating the remaining files and 
                // folders. It is also possible (but unlikely) that a DirectoryNotFound exception 
                // will be raised. This will happen if currentDir has been deleted by
                // another application or thread after our call to Directory.Exists. The 
                // choice of which exceptions to catch depends entirely on the specific task 
                // you are intending to perform and also on how much you know with certainty 
                // about the systems on which this code will run.
                catch (UnauthorizedAccessException e) {                    
                    logger.Warn("{0}: Permission denied.", currentDir);
                    continue;
                } catch (System.IO.DirectoryNotFoundException e) {
                    logger.Warn("{0}: Directory was deleted or moved(?!)", currentDir);
                    continue;
                }

                // Push the subdirectories onto the stack for traversal.
                // This could also be done before handing the files.
                foreach (string str in subDirs)
                    dirs.Push(str);
            }

            return dirs;

        }

    }

}
