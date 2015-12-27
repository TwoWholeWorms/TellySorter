namespace TellySorter
{
    
    using NLog;
    using Mono.Data.Sqlite;
    using System;
    using System.IO;

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

        static void initialiseDatabase()
        {
            
            logger.Debug("Initialising database");
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "CREATE TABLE [config] ([key] VARCHAR(32) NOT NULL PRIMARY KEY, [value] VARCHAR(256) NOT NULL);";
                cmd.ExecuteNonQuery();
            }
            
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "CREATE TABLE [sources] ([id] INTEGER NOT NULL PRIMARY KEY, [path] VARCHAR(356) NOT NULL);";
                cmd.ExecuteNonQuery();
            }

            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "CREATE TABLE [targets] ([id] INTEGER NOT NULL PRIMARY KEY, [path] VARCHAR(356) NOT NULL);";
                cmd.ExecuteNonQuery();
            }

            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "CREATE TABLE [rules] ([id] INTEGER NOT NULL PRIMARY KEY, [type] VARCHAR(32) NOT NULL, [tvdb_show_id] INTEGER NOT NULL, [target_path_id] INTEGER);";
                cmd.ExecuteNonQuery();
            }

            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "CREATE TABLE [shows] ([tvdb_show_id] INTEGER NOT NULL PRIMARY KEY, [name] VARCHAR(64) NOT NULL);";
                cmd.ExecuteNonQuery();
            }

            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "INSERT INTO [config] VALUES ('ApiKey', @val);";
                cmd.Parameters.Add(new SqliteParameter("@val", ""));
                cmd.ExecuteNonQuery();
            }

            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "INSERT INTO [config] VALUES ('DefaultTargetPath', @val);";
                cmd.Parameters.Add(new SqliteParameter("@val", Path.Combine(config.HomePath, "Media")));
                cmd.ExecuteNonQuery();
            }

            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "INSERT INTO [config] VALUES ('EpisodeFileFormat', @val);";
                cmd.Parameters.Add(new SqliteParameter("@val", "${seriesName} - s${seasonNumberPadded}e${episodeNumberPadded} - ${episodeName}.${fileExtension}"));
                cmd.ExecuteNonQuery();
            }

            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "INSERT INTO [config] VALUES ('SeasonFolderFormat', @val);";
                cmd.Parameters.Add(new SqliteParameter("@val", "Season ${seasonNumberPadded}"));
                cmd.ExecuteNonQuery();
            }

            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "INSERT INTO [config] VALUES ('SeriesFolderFormat', @val);";
                cmd.Parameters.Add(new SqliteParameter("@val", "${seriesName}"));
                cmd.ExecuteNonQuery();
            }
        
        }

        public static string GetConfigValue(string variable)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "SELECT [value] FROM [config] WHERE [key] = @key LIMIT 1";
                cmd.Parameters.Add(new SqliteParameter("@key", variable));
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
                cmd.ExecuteNonQuery();
            }
        }

        public static bool HasSourcePath(string path)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "SELECT [id] FROM [sources] WHERE [path] = @path LIMIT 1";
                cmd.Parameters.Add(new SqliteParameter("@path", path));
                var val = cmd.ExecuteScalar();
                return (val != null && val.ToString() != "");
            }
        }

        public static void AddSourcePath(string path)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "INSERT INTO [sources] ([path]) VALUES (@path)";
                cmd.Parameters.Add(new SqliteParameter("@path", path));
                cmd.ExecuteNonQuery();
            }
        }

        public static void RemoveSourcePath(string path)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "DELETE FROM [sources]  WHERE [path] = @path";
                cmd.Parameters.Add(new SqliteParameter("@path", path));
                cmd.ExecuteNonQuery();
            }
        }

        public static SqliteDataReader GetSourcePaths()
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "SELECT * FROM [sources] ORDER BY [path] ASC";
                return cmd.ExecuteReader();
            }
        }

        public static bool IsShowIgnored(string showId)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "SELECT [id] FROM [rules] WHERE [tvdb_show_id] = @showId AND [type] = @type LIMIT 1";
                cmd.Parameters.Add(new SqliteParameter("@showId", showId));
                cmd.Parameters.Add(new SqliteParameter("@type", "ignore"));
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
                cmd.ExecuteNonQuery();
            }
        }

        public static void RemoveShowRules(string showId)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "DELETE FROM [rules]  WHERE [tvdb_show_id] = @showId";
                cmd.Parameters.Add(new SqliteParameter("@showId", showId));
                cmd.ExecuteNonQuery();
            }
        }

        public static void RemoveShowIgnore(string showId)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "DELETE FROM [rules]  WHERE [type] = @type AND [tvdb_show_id] = @showId";
                cmd.Parameters.Add(new SqliteParameter("@showId", showId));
                cmd.Parameters.Add(new SqliteParameter("@type", "ignore"));
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
                var res = cmd.ExecuteScalar();
                if (res != null && res.ToString() != "") {
                    targetPathId = res.ToString();
                } else {
                    using (var cmd2 = database.CreateCommand()) {
                        cmd2.CommandText = "INSERT INTO [targets] ([path]) VALUES (@path)";
                        cmd2.Parameters.Add(new SqliteParameter("@path", path));
                        cmd2.ExecuteNonQuery();

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
                cmd.ExecuteNonQuery();
            }
        }

        public static void RemoveShowSpecificTarget(string showId)
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "DELETE FROM [rules] WHERE [tvdb_show_id] = @showId";
                cmd.Parameters.Add(new SqliteParameter("@showId", showId));
                cmd.ExecuteNonQuery();
            }
        }

        public static SqliteDataReader GetRules()
        {
            using (var cmd = database.CreateCommand()) {
                cmd.CommandText = "SELECT [r].*, [t].[path] FROM [rules] [r] LEFT JOIN [targets] [t] ON [r].[target_path_id] = [t].[id] ORDER BY [type] ASC, [tvdb_show_id] ASC";
                return cmd.ExecuteReader();
            }
        }

    }

}
