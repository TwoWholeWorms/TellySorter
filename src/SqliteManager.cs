namespace TellySorter
{
    
    using NLog;
    using Mono.Data.Sqlite;
    using System;
    using System.IO;
    using System.Collections.Generic;
    using TellySorter.Models;

    // This probably should be called DbMethods or something, since I've lazily combined all the DB crap here. Meh.
    public static class SqliteManager
    {

        readonly static Logger logger = LogManager.GetCurrentClassLogger();

        static SqliteConnection database = null;
        static Configuration config = null;

        public static SqliteConnection GetConnection()
        {
            
            if (database == null) {
                config = Configuration.GetConfig();
                logger.Debug("Opening DB `{0}`", config.DbFile);

                bool initDb = false;
                if (!File.Exists(config.DbFile)) {
                    initDb = true;
                }

                database = new SqliteConnection(string.Format(@"Data Source={0};Pooling=true;FailIfMissing=false;Version=3", config.DbFile));
                database.Open();

                if (initDb) {
                    initialiseDatabase();
                }
            }

            return database;

        }

        public static void CloseConnection()
        {

            if (database != null && database.State != System.Data.ConnectionState.Closed && database.State != System.Data.ConnectionState.Broken) {
                try {
                    logger.Debug("Closing SQLite DB");
                    database.Close();
                } catch (Exception e) {
                    logger.Error("Couldn't close the database properly.");
                    logger.Error(e);
                }
            }

        }

        static void initialiseDatabase()
        {
            
            logger.Debug("Initialising database");
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "CREATE TABLE [config] ([key] VARCHAR(32) NOT NULL PRIMARY KEY, [value] VARCHAR(256) NOT NULL);";
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }
            
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "CREATE TABLE [sources] ([id] INTEGER NOT NULL PRIMARY KEY, [path] VARCHAR(356) NOT NULL);";
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "CREATE TABLE [targets] ([id] INTEGER NOT NULL PRIMARY KEY, [path] VARCHAR(356) NOT NULL);";
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = database.CreateCommand()) {
                cmd.CommandText =
                    "CREATE TABLE [rules] (" +
                        "[id] INTEGER NOT NULL PRIMARY KEY, " +
                        "[type] VARCHAR(32) NOT NULL, " +
                        "[tvdb_show_id] INTEGER NOT NULL, " +
                        "[target_path_id] INTEGER" +
                    ");";
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = database.CreateCommand()) {
                cmd.CommandText =
                    "CREATE TABLE [series] (" +
                        "[id] INTEGER NOT NULL PRIMARY KEY, " +
                        "[tvdb_show_id] INTEGER, " +
                        "[guessed_name] VARCHAR(128) NOT NULL, " +
                        "[imdb_id] VARCHAR(64) NULL, " +
                        "[zap2it_id] VARCHAR(64) NULL, " +
                        "[language] VARCHAR(32) NULL, " +
                        "[name] VARCHAR(128)" +
                    ");";
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "CREATE TABLE [episodes] ([id] INTEGER NOT NULL PRIMARY KEY, [series_id] INTEGER NOT NULL, [tvdb_episode_id] INTEGER, [season_number] INTEGER, [episode_number] INTEGER, [guessed_name] VARCHAR(128), [name] VARCHAR(128));";
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "INSERT INTO [config] VALUES ('ApiKey', @val);";
                cmd.Parameters.Add(new SqliteParameter("@val", ""));
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "INSERT INTO [config] VALUES ('DefaultTargetPath', @val);";
                cmd.Parameters.Add(new SqliteParameter("@val", Path.Combine(config.HomePath, "Media")));
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "INSERT INTO [config] VALUES ('EpisodeFileFormat', @val);";
                cmd.Parameters.Add(new SqliteParameter("@val", "${seriesName} - s${seasonNumberPadded}e${episodeNumberPadded} - ${episodeName}${fileExtension}")); // Extension includes the .
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "INSERT INTO [config] VALUES ('SeasonFolderFormat', @val);";
                cmd.Parameters.Add(new SqliteParameter("@val", "Season ${seasonNumberPadded}"));
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "INSERT INTO [config] VALUES ('SeriesFolderFormat', @val);";
                cmd.Parameters.Add(new SqliteParameter("@val", "${seriesName}"));
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }
        
        }

        public static string GetConfigValue(string variable)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "SELECT [value] FROM [config] WHERE [key] = @key LIMIT 1";
                cmd.Parameters.Add(new SqliteParameter("@key", variable));
                logger.Debug("Executing query: {0}", cmd.CommandText);
                var value = cmd.ExecuteScalar();
                return value.ToString();
            }
        }

        public static void SetConfigValue(string variable, string value)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "UPDATE [config] SET [value] = @variable WHERE [key] = @key";
                cmd.Parameters.Add(new SqliteParameter("@key", variable));
                cmd.Parameters.Add(new SqliteParameter("@variable", value));
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }
        }

        public static Series FindSeries(int seriesId)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "SELECT * FROM [series] WHERE [id] = @seriesId LIMIT 1";
                cmd.Parameters.Add(new SqliteParameter("@seriesId", seriesId));
                logger.Debug("Executing query: {0}", cmd.CommandText);

                using (var res = cmd.ExecuteReader()) {
                    if (res != null && res.HasRows) {
                        return new Series(res);
                    }
                }
            }

            return null;
        }

        public static Series FindOrCreateSeries(string seriesName)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "SELECT * FROM [series] WHERE [guessed_name] = @seriesName OR [name] = @seriesName LIMIT 1";
                cmd.Parameters.Add(new SqliteParameter("@seriesName", seriesName));
                logger.Debug("Executing query: {0}", cmd.CommandText);

                using (var res = cmd.ExecuteReader()) {
                    if (res != null && res.HasRows) {
                        return new Series(res);
                    }
                }

                cmd.CommandText = "INSERT INTO [series] ([guessed_name]) VALUES (@seriesName)";
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();

                cmd.CommandText = "SELECT * FROM [series] WHERE [guessed_name] = @seriesName OR [name] = @seriesName LIMIT 1";
                logger.Debug("Executing query: {0}", cmd.CommandText);
                using (var res = cmd.ExecuteReader()) {
                    if (res != null && res.HasRows) {
                        return new Series(res);
                    }
                }
            }

            return null;
        }

        public static void SaveSeries(Series series)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText =
                    "UPDATE [series] " +
                    "SET [name] = @name, " +
                        "[guessed_name] = @guessedName, " +
                        "[tvdb_show_id] = @tvdbShowId, " +
                        "[imdb_id] = @imdbId, " +
                        "[language] = @language, " +
                        "[zap2it_id] = @zap2ItId " +
                    "WHERE [id] = @id";
                cmd.Parameters.Add(new SqliteParameter("@name", series.Name));
                cmd.Parameters.Add(new SqliteParameter("@guessedName", series.GuessedName));
                cmd.Parameters.Add(new SqliteParameter("@tvdbShowId", series.TvdbShowId));
                cmd.Parameters.Add(new SqliteParameter("@imdbId", series.ImdbId));
                cmd.Parameters.Add(new SqliteParameter("@language", series.Language));
                cmd.Parameters.Add(new SqliteParameter("@zap2ItId", series.Zap2ItId));
                cmd.Parameters.Add(new SqliteParameter("@id", series.Id));
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }
        }

        public static void SetSeriesTvDbId(string seriesId, string tvdbShowId)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText =
                    "UPDATE [series] " +
                    "SET [tvdb_show_id] = @tvdbShowId " +
                    "WHERE [id] = @id";
                cmd.Parameters.Add(new SqliteParameter("@tvdbShowId", tvdbShowId));
                cmd.Parameters.Add(new SqliteParameter("@id", seriesId));
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }
        }

        public static void SaveEpisode(Episode episode)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText =
                    "UPDATE [episodes] " +
                    "SET [name] = @name, " +
                        "[series_id] = @seriesId, " +
                        "[guessed_name] = @guessedName, " +
                        "[tvdb_episode_id] = @tvdbEpisodeId, " +
                        "[season_number] = @seasonNumber, " +
                        "[episode_number] = @episodeNumber " +
                    "WHERE [id] = @id";
                cmd.Parameters.Add(new SqliteParameter("@name", episode.Name));
                cmd.Parameters.Add(new SqliteParameter("@seriesId", episode.Series.Id));
                cmd.Parameters.Add(new SqliteParameter("@guessedName", episode.GuessedName));
                cmd.Parameters.Add(new SqliteParameter("@tvdbEpisodeId", episode.TvdbEpisodeId));
                cmd.Parameters.Add(new SqliteParameter("@seasonNumber", episode.SeasonNumber));
                cmd.Parameters.Add(new SqliteParameter("@episodeNumber", episode.EpisodeNumber));
                cmd.Parameters.Add(new SqliteParameter("@id", episode.Id));
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }
        }

        public static void UpdateOrCreateSeriesEpisodes(Series series, TVDBSharp.Models.Show show)
        {
            if (show.Episodes.Count > 0) {
                foreach (var ep in show.Episodes) {
                    Episode episode = FindOrCreateEpisode(series, ep.SeasonNumber, ep.EpisodeNumber);
                    if (episode != null) {
                        episode.CompleteWith(ep, series);
                    }
                }
            }
        }

        public static Episode FindOrCreateEpisode(Series series, int seasonNumber, int episodeNumber)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "SELECT * FROM [episodes] WHERE [series_id] = @seriesId AND [season_number] = @seasonNumber AND [episode_number] = @episodeNumber LIMIT 1";
                cmd.Parameters.Add(new SqliteParameter("@seriesId", series.Id));
                cmd.Parameters.Add(new SqliteParameter("@seasonNumber", seasonNumber));
                cmd.Parameters.Add(new SqliteParameter("@episodeNumber", episodeNumber));
                logger.Debug("Executing query: {0}", cmd.CommandText);

                using (var res = cmd.ExecuteReader()) {
                    if (res != null && res.HasRows) {
                        return new Episode(res, series);
                    }
                }

                cmd.CommandText = "INSERT INTO [episodes] ([series_id], [season_number], [episode_number]) VALUES (@seriesId, @seasonNumber, @episodeNumber)";
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();

                cmd.CommandText = "SELECT * FROM [episodes] WHERE [series_id] = @seriesId AND [season_number] = @seasonNumber AND [episode_number] = @episodeNumber LIMIT 1";
                logger.Debug("Executing query: {0}", cmd.CommandText);
                using (var res = cmd.ExecuteReader()) {
                    if (res != null && res.HasRows) {
                        return new Episode(res, series);
                    }
                }
            }

            return null;
        }

        public static bool HasSourcePath(string path)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "SELECT [id] FROM [sources] WHERE [path] = @path LIMIT 1";
                cmd.Parameters.Add(new SqliteParameter("@path", path));
                logger.Debug("Executing query: {0}", cmd.CommandText);
                var val = cmd.ExecuteScalar();
                return (val != null && val.ToString() != "");
            }
        }

        public static void AddSourcePath(string path)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "INSERT INTO [sources] ([path]) VALUES (@path)";
                cmd.Parameters.Add(new SqliteParameter("@path", path));
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }
        }

        public static void RemoveSourcePath(string path)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "DELETE FROM [sources]  WHERE [path] = @path";
                cmd.Parameters.Add(new SqliteParameter("@path", path));
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }
        }

        public static string[] GetSourcePaths()
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "SELECT * FROM [sources] ORDER BY [path] ASC";
                logger.Debug("Executing query: {0}", cmd.CommandText);
                using (var res = cmd.ExecuteReader()) {
                    List<string> output = new List<string>();
                    while (res.Read()) {
                        output.Add(res["path"].ToString());
                    }
                    return output.ToArray();
                }
            }
        }

        public static bool IsShowIgnored(string showId)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "SELECT [id] FROM [rules] WHERE [tvdb_show_id] = @showId AND [type] = @type LIMIT 1";
                cmd.Parameters.Add(new SqliteParameter("@showId", showId));
                cmd.Parameters.Add(new SqliteParameter("@type", "ignore"));
                logger.Debug("Executing query: {0}", cmd.CommandText);
                var val = cmd.ExecuteScalar();
                return (val != null && val.ToString() != "");
            }
        }

        public static void AddShowIgnore(string showId)
        {
            RemoveShowRules(showId);

            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "INSERT INTO [rules] ([type], [tvdb_show_id]) VALUES (@type, @showId)";
                cmd.Parameters.Add(new SqliteParameter("@showId", showId));
                cmd.Parameters.Add(new SqliteParameter("@type", "ignore"));
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }
        }

        public static void RemoveShowRules(string showId)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "DELETE FROM [rules]  WHERE [tvdb_show_id] = @showId";
                cmd.Parameters.Add(new SqliteParameter("@showId", showId));
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }
        }

        public static void RemoveShowIgnore(string showId)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "DELETE FROM [rules]  WHERE [type] = @type AND [tvdb_show_id] = @showId";
                cmd.Parameters.Add(new SqliteParameter("@showId", showId));
                cmd.Parameters.Add(new SqliteParameter("@type", "ignore"));
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }
        }

        public static void SetShowSpecificTarget(string showId, string path)
        {
            RemoveShowRules(showId);

            string targetPathId;
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "SELECT [id] FROM [targets] WHERE [path] = @path LIMIT 1";
                cmd.Parameters.Add(new SqliteParameter("@path", path));
                logger.Debug("Executing query: {0}", cmd.CommandText);
                var res = cmd.ExecuteScalar();
                if (res != null && res.ToString() != "") {
                    targetPathId = res.ToString();
                } else {
                    using (var cmd2 = database.CreateCommand()) {
                        cmd2.CommandText = "INSERT INTO [targets] ([path]) VALUES (@path)";
                        cmd2.Parameters.Add(new SqliteParameter("@path", path));
                        logger.Debug("Executing query: {0}", cmd2.CommandText);
                        cmd2.ExecuteNonQuery();

                        logger.Debug("Executing query: {0}", cmd.CommandText);
                        res = cmd.ExecuteScalar();
                        targetPathId = res.ToString();
                    }
                }
            }

            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "INSERT INTO [rules] ([type], [tvdb_show_id], [target_path_id]) VALUES (@type, @showId, @targetPathId)";
                cmd.Parameters.Add(new SqliteParameter("@showId", showId));
                cmd.Parameters.Add(new SqliteParameter("@type", "target"));
                cmd.Parameters.Add(new SqliteParameter("@targetPathId", targetPathId));
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }
        }

        public static void RemoveShowSpecificTarget(string showId)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "DELETE FROM [rules] WHERE [tvdb_show_id] = @showId";
                cmd.Parameters.Add(new SqliteParameter("@showId", showId));
                logger.Debug("Executing query: {0}", cmd.CommandText);
                cmd.ExecuteNonQuery();
            }
        }

        public static List<Rule> GetRules()
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "SELECT [r].*, [t].[path] FROM [rules] [r] LEFT JOIN [targets] [t] ON [r].[target_path_id] = [t].[id] ORDER BY [type] ASC, [tvdb_show_id] ASC";
                logger.Debug("Executing query: {0}", cmd.CommandText);

                using (var res = cmd.ExecuteReader()) {
                    List<Rule> output = new List<Rule>();
                    if (res.HasRows) {
                        while (res.Read()) {
                            output.Add(new Rule(res));
                        }
                    }

                    return output;
                }
            }
        }

    }

}
