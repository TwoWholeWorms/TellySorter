﻿namespace TellySorter.Commands
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

    public class ProcessCommand : AbstractConsoleCommand
    {

        readonly static Logger logger = LogManager.GetCurrentClassLogger();

        // This list contains only stuff we actually want to process
        readonly static List<string> processMimeTypes = new List<string>() {
            "taglib/avi",
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
                    logger.Error("Unable to get type of file `{0}`", file);
                    logger.Error(e);
                    erroredFiles++;
                    continue;
                }
                if (processMimeTypes.Contains(fileData.MimeType)) {
                    logger.Debug("Found file: `{0}`", fileData.Name);

                    string seriesName = GuessSeriesName(Path.GetFileNameWithoutExtension(file));
                    int seasonNumber = GuessSeasonNumber(Path.GetFileNameWithoutExtension(file));
                    int episodeNumber = GuessEpisodeNumber(Path.GetFileNameWithoutExtension(file));
                    string episodeName = GuessEpisodeName(Path.GetFileNameWithoutExtension(file));

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
            }

            string[] subDirs = Directory.GetDirectories(path);
            foreach (var dir in subDirs) {
                ProcessDirectory(dir);
            }

        }

        static string GuessSeriesName(string file)
        {
            Regex r = new Regex(@"[\. -]+");
            string guess = r.Replace(file.ToLower(), " ");

            r = new Regex(@"^(?:(.*) )?([s]\d+ ?[xe]\d+|\d+x\d+|season \d+ episode \d+)(?: (.*))?$");
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

            r = new Regex(@"^(?:.* )?(?:[s]\d+ ?[xe]\d+|\d+x\d+|season \d+ episode \d+)(?: (.*))?$");
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

            r = new Regex(@"^(?:.* )?(?:[s](\d+) ?(?:[xe]\d+)?|(\d+)x\d+|season (\d+)(?:.*episode \d+)?)(?: .*)?$");
            Match m = r.Match(guess);

            if (m.Success) {
                guess = (m.Groups[1].ToString().Length >= 1 ? m.Groups[1].ToString() : (m.Groups[2].ToString().Length >= 1 ? m.Groups[2].ToString() : m.Groups[3].ToString()));
            } else {
                guess = "0";
            }

            int output = 0;
            int.TryParse(guess, out output);
            return output;
        }

        static int GuessEpisodeNumber(string file)
        {
            Regex r = new Regex(@"[\. -]+");
            string guess = r.Replace(file.ToLower(), " ");

            r = new Regex(@"^(?:.* )?(?:[s]\d+ ?[xe](\d+)|\d+x(\d+)|season \d+.*episode (\d+))(?: .*)?$");
            Match m = r.Match(guess);

            if (m.Success) {
                guess = (m.Groups[1].ToString().Length >= 1 ? m.Groups[1].ToString() : (m.Groups[2].ToString().Length >= 1 ? m.Groups[2].ToString() : m.Groups[3].ToString()));
            } else {
                guess = "0";
            }

            int output = 0;
            int.TryParse(guess, out output);
            return output;
        }

    }

}
