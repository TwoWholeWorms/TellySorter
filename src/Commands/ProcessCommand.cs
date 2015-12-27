namespace TellySorter.Commands
{
    
    using ManyConsole;
    using NLog;
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using TVDBSharp;
    using TagLib;

    public class ProcessCommand : AbstractConsoleCommand
    {

        readonly static Logger logger = LogManager.GetCurrentClassLogger();

        readonly static List<string> mimeTypes = new List<string>() {
            "taglib/avi",
            "taglib/mkv",
            "taglib/mp4",
        };

        public ProcessCommand() : base()
        {
        
            this.IsCommand("process", "Run the file sorting process according to the rules you've previously defined");

        }

        public override int Run(string[] remainingArguments)
        {
            
            Initialise();

            logger.Info("Processing directories");
            logger.Info("----------------------");
            logger.Info("");

            var tvdb = new TVDB(config.ApiKey);

            using (var sourcePathsRes = SqliteManager.GetSourcePaths()) {
                if (!sourcePathsRes.HasRows) {
                    throw new ConsoleHelpAsException("You haven't added any source paths to process yet.  Use the source command to add at least one source path to find files in to process.");
                } else {
                    while (sourcePathsRes.Read()) {
                        ProcessDirectory(sourcePathsRes["path"].ToString());
                    }
                }
            }

            logger.Info("");
            logger.Info("Woooo! Congratulations, your TV episodes are all sorted now! :D");

            return 0;

        }

        static void ProcessDirectory(string path)
        {
            string[] files = Directory.GetFiles(path);
            foreach (var file in files) {
                TagLib.File fileData = TagLib.File.Create(file);
                if (mimeTypes.Contains(fileData.MimeType)) {
                    logger.Info("Found file: `{0}`", fileData.Name);

                    string seriesName = GuessSeriesName(Path.GetFileNameWithoutExtension(file));
                    int seasonNumber = GuessSeasonNumber(Path.GetFileNameWithoutExtension(file));
                    int episodeNumber = GuessEpisodeNumber(Path.GetFileNameWithoutExtension(file));
                    string episodeName = GuessEpisodeName(Path.GetFileNameWithoutExtension(file));

                    logger.Info("Guessed details:");
                    logger.Info("    Series name: {0}", seriesName);
                    logger.Info("    Season number: {0}", seasonNumber);
                    logger.Info("    Episode number: {0}", episodeNumber);
                    logger.Info("    Episode name: {0}", episodeName);

//                    MediaInfo mInfo = new MediaInfo();
//                    mInfo.Open(file);
//                    foreach (var item in mInfo.Option("")) {
//                        logger.Debug(item);
//                    }
                    // Type type = fileData.Tag.GetType();
                    // foreach (var prop in type.GetProperties()) {
                    //     logger.Info(string.Format("    Tag: {0} = {1}", prop.Name, prop.GetValue(fileData.Tag, null)));
                    // }
                    // Type pType = fileData.Properties.GetType();
                    // foreach (var prop in pType.GetProperties()) {
                    //     logger.Info(string.Format("    Property: {0} = {1}", prop.Name, prop.GetValue(fileData.Properties, null)));
                    // }
                }
            }

            string[] subDirs = Directory.GetDirectories(path);
            foreach (var dir in subDirs) {
                ProcessDirectory(dir);
            }
            /*var dirs = TraverseTree(sourcePathsRes["path"].ToString());
            foreach (var dir in dirs) {

                string[] files = null;
                try {
                    files = System.IO.Directory.GetFiles(dir);
                } catch (UnauthorizedAccessException e) {
                    logger.Warn("{0}: Permission denied", dir);
                    continue;
                } catch (DirectoryNotFoundException e) {
                    logger.Warn("{0}: Directory was deleted or moved(?!)", dir);
                    continue;
                }
                foreach (string file in files) {
                    try {
                        // Identify the file
                        TagLib.File fileData = TagLib.File.Create(file);
                        logger.Info("Found file: `{0}`", fileData.Name);
                    } catch (FileNotFoundException e) {
                        // If file was deleted by a separate application
                        //  or thread since the call to TraverseTree()
                        // then just continue.
                        logger.Warn("{0}: File was deleted or moved(?!)", file);
                        continue;
                    }
                }

            }*/
        }

        static string GuessSeriesName(string file)
        {
            Regex r = new Regex(@"[\. -]+");
            string guess = r.Replace(file.ToLower(), " ");

            r = new Regex(@"^(?:(.*) )?([s]\d+[xe]\d+|\d+x\d+)(?: (.*))?$");
            Match m = r.Match(guess);

            if (m.Success) {
                guess = m.Groups[1].ToString();
            }

            return guess;
        }

        static string GuessEpisodeName(string file)
        {
            Regex r = new Regex(@"[\. -]+");
            string guess = r.Replace(file.ToLower(), " ");

            r = new Regex(@"^(?:.* )?(?:[s]\d+[xe]\d+|\d+x\d+)(?: (.*))?$");
            Match m = r.Match(guess);

            if (m.Success) {
                guess = m.Groups[1].ToString();
            }

            return guess;
        }

        static int GuessSeasonNumber(string file)
        {
            Regex r = new Regex(@"[\. -]+");
            string guess = r.Replace(file.ToLower(), " ");

            r = new Regex(@"^(?:.* )?(?:[s](\d+)[xe]\d+|(\d+)x\d+)(?: .*)?$");
            Match m = r.Match(guess);

            if (m.Success) {
                guess = (m.Groups[1].ToString().Length >= 1 ? m.Groups[1].ToString() : m.Groups[2].ToString());
            } else {
                guess = "";
            }

            return int.Parse(guess);
        }

        static int GuessEpisodeNumber(string file)
        {
            Regex r = new Regex(@"[\. -]+");
            string guess = r.Replace(file.ToLower(), " ");

            r = new Regex(@"^(?:.* )?(?:[s]\d+[xe](\d+)|\d+x(\d+))(?: .*)?$");
            Match m = r.Match(guess);

            if (m.Success) {
                guess = (m.Groups[1].ToString().Length >= 1 ? m.Groups[1].ToString() : m.Groups[2].ToString());
            } else {
                guess = "";
            }

            return int.Parse(guess);
        }

        /*static void WalkDirectoryTree(DirectoryInfo root)
        {
            
            FileInfo[] files = null;
            DirectoryInfo[] subDirs = null;

            // First, process all the files directly under this folder
            try {
                files = root.GetFiles("*.*");
            }
            // This is thrown if even one of the files requires permissions greater
            // than the application provides.
            catch (UnauthorizedAccessException e) {
                // This code just writes out the message and continues to recurse.
                // You may decide to do something different here. For example, you
                // can try to elevate your privileges and access the file again.
                log.Add(e.Message);
            } catch (DirectoryNotFoundException e) {
                Console.WriteLine(e.Message);
            }

            if (files != null) {
                foreach (FileInfo fi in files) {
                    // In this example, we only access the existing FileInfo object. If we
                    // want to open, delete or modify the file, then
                    // a try-catch block is required here to handle the case
                    // where the file has been deleted since the call to TraverseTree().
                    Console.WriteLine(fi.FullName);
                }

                // Now find all the subdirectories under this directory.
                subDirs = root.GetDirectories();

                foreach (DirectoryInfo dirInfo in subDirs) {
                    // Resursive call for each subdirectory.
                    WalkDirectoryTree(dirInfo);
                }
            }

        }*/

    }

}
