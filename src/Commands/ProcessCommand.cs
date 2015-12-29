namespace TellySorter.Commands
{
    
    using ManyConsole;
    using NLog;
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using TagLib;
    using TellySorter.Models;
    using TVDBSharp;

    /**
     * So, firstly, apologies for how disgusting the code is in here. It was written /very/ quickly over Christmas
     * in the time between the moments when I was trying not to murder my relatives, though that's not really an
     * excuse. Anyway, it works, it could just do with a /lot/ of cleaning up.
     */
    public class ProcessCommand : AbstractConsoleCommand
    {

        readonly static Logger logger = LogManager.GetCurrentClassLogger();

        // This list contains only stuff we actually want to process
        readonly static List<string> processMimeTypes = new List<string>() {
            "taglib/avi",
            "taglib/m4v",
            "taglib/mkv",
            "taglib/mp4",
        };

        TVDB tvdb;

        int successfullyProcessedFiles = 0;
        int erroredFiles = 0;

        List<string> failedNames = new List<string>();

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

            if (Simulate) {
                logger.Info("This is a simulation.");
                logger.Info("");
            }

            tvdb = new TVDB(config.ApiKey);

            string[] sourcePaths = SqliteManager.GetSourcePaths();
            if (sourcePaths.Length < 1) {
                throw new ConsoleHelpAsException("You haven't added any source paths to process yet.  Use the source command to add at least one source path to find files in to process.");
            } else {
                foreach (string path in sourcePaths) {
                    ProcessDirectory(path);
                }
            }

            logger.Info("");
            if (erroredFiles < 1) {
                if (Simulate) {
                    logger.Info("Woooo! {0} files were simulated successfully!", successfullyProcessedFiles);
                } else {
                    logger.Info("Woooo! {0} files were found and sorted successfully!", successfullyProcessedFiles);
                }
            } else {
                if (Simulate) {
                    logger.Info(string.Format("{0} files were simulated successfully, and {1} had errors. Please check the logs and fix them.", successfullyProcessedFiles, erroredFiles));
                } else {
                    logger.Info(string.Format("{0} files were found and sorted successfully, and {1} had errors. Please check the logs and fix them.", successfullyProcessedFiles, erroredFiles));
                }
            }

            return 0;

        }

        void ProcessDirectory(string path)
        {
            string targetFramework = Path.Combine(config.SeriesFolderFormat, Path.Combine(config.SeasonFolderFormat, config.EpisodeFileFormat));

            string[] files = Directory.GetFiles(path);
            foreach (var file in files) {
                FileInfo info = new FileInfo(file);
                // Ignore anything under about 100MB
                if (info.Length < 100000000) {
                    continue;
                }
                TagLib.File fileData;
                try {
                    fileData = TagLib.File.Create(file);
                } catch (Exception e) {
                    logger.Error("Unable to get media info for file `{0}`", file);
                    logger.Error(e);
                    erroredFiles++;
                    continue;
                }
                logger.Debug("Mime type: `{0}`", fileData.MimeType);
                if (!processMimeTypes.Contains(fileData.MimeType)) {
                    logger.Trace("Skipping file `{0}` because it's not in the process formats list.", file);
                    continue;
                }

                logger.Debug("Found file: `{0}`", file);

                string seriesName = GuessSeriesName(file);
                int seasonNumber = GuessSeasonNumber(file);
                int episodeNumber = GuessEpisodeNumber(file);
                string episodeName = GuessEpisodeName(file);

                if (seasonNumber < 1 && episodeNumber < 1) {
                    logger.Error("Unable to establish a season number or an episode number for file `{0}`", file);
                    erroredFiles++;
                    continue;
                }
                logger.Debug("Guessed details:");
                logger.Debug("    Series name: {0}", seriesName);
                logger.Debug("    Season number: {0}", seasonNumber.ToString("D2"));
                logger.Debug("    Episode number: {0}", episodeNumber.ToString("D2"));
                logger.Debug("    Episode name: {0}", episodeName);

                Series series = SqliteManager.FindOrCreateSeries(seriesName);
                if (series == null) {
                    logger.Error("Unable to find or create a series for `{0}`", seriesName);
                    erroredFiles++;
                    continue;
                }

                if (failedNames.Contains(seriesName)) {
                    logger.Error(string.Format("`{0}` belongs to previously failed series `{1}`", file, seriesName));
                    erroredFiles++;
                    continue;
                }

                if (series.TvdbShowId == 0) {
                    try {
                        // Get the series info from the TV DB
                        var results = tvdb.Search(seriesName, 5);
                        bool found = false;
                        foreach (var result in results) {
                            if (result.Name.ToLower() == seriesName) {
                                series.CompleteWith(result);
                                found = true;
                                break;
                            }
                        }

                        // Nice matching failed, now let's get MESSY!
                        Regex r = new Regex("[^a-zA-Z0-9]+");
                        if (!found) {
                            foreach (var result in results) {
                                if (result.Language == config.DefaultLanguage && r.Replace(result.Name.ToLower(), "") == r.Replace(seriesName.ToLower(), "")) {
                                    series.CompleteWith(result);
                                    found = true;
                                    break;
                                }
                            }
                        }

                        if (!found) {
                            logger.Error("Unable to match series name `{0}` with any of the following results:", seriesName);
                            foreach (var result in results) {
                                logger.Error(string.Format("    #{0} {1} ({2})", result.Id, result.Name, result.Language));
                            }
                            logger.Error("Use `set ShowId {0} <tvdbShowId>` with one of the numbers from the list above to set the TV DB show id to that show id", series.Id);
                            erroredFiles++;
                            failedNames.Add(seriesName);
                            continue;
                        }
                    } catch (Exception e) {
                        logger.Error("Something went horrifically wrong whilst trying to get the series data for file `{0}`", file);
                        logger.Error(e);
                        erroredFiles++;
                        failedNames.Add(seriesName);
                        continue;
                    }
                }

                Episode episode = SqliteManager.FindOrCreateEpisode(series, seasonNumber, episodeNumber);
                if (episode == null) {
                    logger.Error(string.Format("Unable to find or create an episode for series id {0} s{1}e{2}", series.Id, seasonNumber.ToString("D2"), episodeNumber.ToString("D2")));
                    erroredFiles++;
                    continue;
                }

                if (episode.TvdbEpisodeId == 0) {
                    var show = tvdb.GetShow(series.TvdbShowId);
                    if (show == null) {
                        logger.Error(string.Format("Unable to get series {0} from TVDB for episode s{1}e{2}", series.TvdbShowId, seasonNumber.ToString("D2"), episodeNumber.ToString("D2")));
                        erroredFiles++;
                        continue;
                    }

                    SqliteManager.UpdateOrCreateSeriesEpisodes(series, show);
                    episode = SqliteManager.FindOrCreateEpisode(series, seasonNumber, episodeNumber);
                    if (episode == null) {
                        logger.Error(string.Format("Episode s{1}e{2} of series id {0} was previously found, but can't be found any more. O.o", series.Id, seasonNumber.ToString("D2"), episodeNumber.ToString("D2")));
                        erroredFiles++;
                        continue;
                    }
                }

                string targetPath = targetFramework;
                targetPath = targetPath.Replace("${seriesName}", series.Name);
                targetPath = targetPath.Replace("${episodeName}", episode.Name);
                targetPath = targetPath.Replace("${seasonNumberPadded}", episode.SeasonNumber.ToString("D2"));
                targetPath = targetPath.Replace("${episodeNumberPadded}", episode.EpisodeNumber.ToString("D2"));
                targetPath = targetPath.Replace("${fileExtension}", Path.GetExtension(file));

                string basePath = config.DefaultTargetPath;
                var rules = SqliteManager.GetRulesByTvdbShowId(series.TvdbShowId);
                if (rules.Count > 0) {
                    foreach (var rule in rules) {
                        if (rule.Type == "target") {
                            basePath = rule.Path;
                            break;
                        }
                    }
                }

                string finalLocation = Path.Combine(basePath, targetPath);
                if (finalLocation.ToLower() == file.ToLower()) {
                    logger.Info("File `{0}` is already where it should be :)", file);
                    successfullyProcessedFiles++;
                } else if (System.IO.File.Exists(finalLocation)) {
                    logger.Error("Target file `{0}` already exists!", finalLocation);
                    erroredFiles++;
                } else if (Simulate) {
                    logger.Info(string.Format("Simulated move: {0} -> {1}", file, finalLocation));
                    successfullyProcessedFiles++;
                } else {
                    string dirTree = Path.GetDirectoryName(finalLocation);
                    if (!Directory.Exists(dirTree)) {
                        try {
                            logger.Debug("Creating directory `{0}`", dirTree);
                            Directory.CreateDirectory(dirTree);
                        } catch (Exception e) {
                            logger.Error("Unable to create directory `{0}`", dirTree);
                            logger.Error(e);
                            erroredFiles++;
                            continue;
                        }
                    }
                    try {
                        System.IO.File.Move(file, finalLocation);
                    } catch (Exception e) {
                        logger.Error(string.Format("Unable to move file `{0}` to `{1}`", file, finalLocation));
                        logger.Error(e);
                        erroredFiles++;
                        continue;
                    }
                    logger.Info(string.Format("Moved: {0} -> {1}", file, finalLocation));
                    successfullyProcessedFiles++;
                }
            }

            string[] subDirs = Directory.GetDirectories(path);
            foreach (var dir in subDirs) {
                ProcessDirectory(dir);
            }

        }

        string GuessSeriesName(string file)
        {
            string guessFrom = file.Replace(config.DefaultTargetPath, "").ToLower().TrimStart(Path.DirectorySeparatorChar);
            guessFrom = guessFrom.TrimStart(Path.DirectorySeparatorChar);

            Regex r = new Regex(@"[\. -]+");
            string startWith = r.Replace(Path.GetFileNameWithoutExtension(guessFrom), " ");

            string guess = "";
            r = new Regex(@"^(?:(?<seriesName1>.+) )?([s](?<seasonNumber2>\d+) ?[xe](?<episodeNumber1>\d+)|(?<seasonNumber3>\d+)x(?<episodeNumber2>\d+)|season (?<seasonNumber4>\d+) episode (?<episodeNumber3>\d+))(?: (?<episodeName>.*))?$");
            Match m = r.Match(startWith);
            if (m.Success) {
                guess = m.Groups["seriesName1"].ToString();
            }

            r = new Regex(@"[^a-zA-Z0-9]+");
            if (r.Replace(guess, "").Length < 1) {
                // Try guessing from the folder names
                logger.Trace("Using messy series name matching");
                string[] bits = guessFrom.Split(Path.DirectorySeparatorChar);
                if (bits.Length == 2) {
                    // First element is /probably/ the series name, and the second bit is the episode file, so just return [0]
                    guess = bits[0];
                } else if (bits.Length == 3) {
                    if (bits[1].Contains("season")) {
                        // First element is /probably/ the series name, the second bit the season number, and the last bit is most likely the episode file, so just return [0]
                        guess = bits[0];
                    }
                }
            }

            return guess;
        }

        string GuessEpisodeName(string file)
        {
            Regex r = new Regex(@"[\. -]+");
            string guess = r.Replace(Path.GetFileNameWithoutExtension(file.ToLower()), " ");

            r = new Regex(@"^(?:.* )?(?:[s]\d+ ?[xe]\d+|\d+x\d+|season \d+ episode \d+)(?: (.*))?$");
            Match m = r.Match(guess);

            if (m.Success) {
                guess = m.Groups[1].ToString();
            }

            return guess;
        }

        int GuessSeasonNumber(string file)
        {
            Regex r = new Regex(@"[\. -]+");
            string startWith = r.Replace(Path.GetFileNameWithoutExtension(file.ToLower()), " ");

            r = new Regex(@"^(?:(?<seriesName1>.+) )?([s](?<seasonNumber1>\d+) ?[xe](?<episodeNumber1>\d+)|(?<seasonNumber2>\d+)x(?<episodeNumber2>\d+)|season (?<seasonNumber3>\d+) episode (?<episodeNumber3>\d+))(?: (?<episodeName>.*))?$");
            Match m = r.Match(startWith);

            string guess = "";
            m = r.Match(startWith);
            if (m.Success) {
                if (m.Groups["seasonNumber1"].ToString().Length > 0) {
                    guess = m.Groups["seasonNumber1"].ToString();
                } else if (m.Groups["seasonNumber2"].ToString().Length > 0) {
                    guess = m.Groups["seasonNumber2"].ToString();
                } else if (m.Groups["seasonNumber3"].ToString().Length > 0) {
                    guess = m.Groups["seasonNumber3"].ToString();
                }
            }

            if (guess == "") {
                string guessFrom = file.Replace(config.DefaultTargetPath, "").ToLower().TrimStart(Path.DirectorySeparatorChar);
                r = new Regex(@"^(?:(?<seriesName2>.+) )?(season (?<seasonNumber4>\d+) )?(?:(?<seriesName1>.+) )?([s](?<seasonNumber1>\d+) ?[xe](?<episodeNumber1>\d+)|(?<seasonNumber2>\d+)x(?<episodeNumber2>\d+)|season (?<seasonNumber3>\d+) episode (?<episodeNumber3>\d+))(?: (?<episodeName>.*))?$");
                m = r.Match(guessFrom);
                if (m.Success) {
                    if (m.Groups["seasonNumber4"].ToString().Length > 0) {
                        guess = m.Groups["seasonNumber4"].ToString();
                    }
                } else {
                    string[] bits = guessFrom.Split(Path.DirectorySeparatorChar);
                    if (bits.Length == 3) {
                        // First element is /probably/ the series name, the second bit the season number, and the last bit is most likely the episode file, so we do our best to extract the season number from the second bit
                        r = new Regex(@"^.*s(?:eason ?)?(\d+).*$");
                        m = r.Match(bits[1]);
                        if (m.Success) {
                            guess = m.Groups[1].ToString();
                        }
                    }
                }
            }

            if (guess == "") {
                guess = "0";
            }

            int output = 0;
            int.TryParse(guess, out output);
            return output;
        }

        int GuessEpisodeNumber(string file)
        {
            Regex r = new Regex(@"[\. -]+");
            string guess = r.Replace(Path.GetFileNameWithoutExtension(file.ToLower()), " ");

            r = new Regex(@"^(?:.* )?(?:[s]\d+ ?[xe](\d+)|\d+x(\d+)|season \d+.*episode (\d+))(?: .*)?$");
            Match m = r.Match(guess);

            if (m.Success) {
                guess = (m.Groups[1].ToString().Length >= 1 ? m.Groups[1].ToString() : (m.Groups[2].ToString().Length >= 1 ? m.Groups[2].ToString() : m.Groups[3].ToString()));
            } else {
                r = new Regex(@"^(\d+).*$");
                m = r.Match(guess);
                if (m.Success) {
                    guess = m.Groups[0].ToString();
                } else {
                    guess = "0";
                }
            }

            int output = 0;
            int.TryParse(guess, out output);
            return output;
        }

    }

}
