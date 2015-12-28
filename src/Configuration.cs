namespace TellySorter
{

    using NLog;
    using System;
    using System.Configuration;
    using System.Collections.Generic;
    using Mono.Data.Sqlite;
    using System.IO;
    using TellySorter.Commands;

    public class Configuration : ConfigurationSection
    {

        public const string SECTION_NAME = "tellySorter";

        readonly static Logger logger = LogManager.GetCurrentClassLogger();

        static Configuration current = null;
        static AbstractConsoleCommand command = null;

        public string[] SourcePaths { get; set; }
        public string DefaultTargetPath { get; set; }
        public string EpisodeFileFormat { get; set; }
        public string SeasonFolderFormat { get; set; }
        public string SeriesFolderFormat { get; set; }
        public string ApiKey { get; set; }
        public string DefaultLanguage { get; set; }

        public string HomePath;

        string dbFile;

        static Configuration()
        {

        }

        public static Configuration GetConfig()
        {
            return current;
        }

        public static Configuration GetConfig(AbstractConsoleCommand command)
        {

            if (current == null) {
                logger.Debug("Initialising for OS `{0}`", Environment.OSVersion.Platform);

                logger.Debug("Getting home directory");
                string homePath = ((Environment.OSVersion.Platform == PlatformID.Unix) ||
                    (Environment.OSVersion.Platform == PlatformID.MacOSX))
                    ? Environment.GetEnvironmentVariable("HOME")
                    : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

                logger.Debug("Loading data from config file");

                current = ConfigurationManager.GetSection(Configuration.SECTION_NAME) as Configuration;
                current.HomePath = homePath;

                logger.Debug("Getting DB");

                current.DbFile = command.DbFile;

                Configuration.command = command;
                SqliteManager.GetConnection(); // Lazy init

                current.DefaultTargetPath = SqliteManager.GetConfigValue("DefaultTargetPath");
                current.EpisodeFileFormat = SqliteManager.GetConfigValue("EpisodeFileFormat");
                current.SeasonFolderFormat = SqliteManager.GetConfigValue("SeasonFolderFormat");
                current.SeriesFolderFormat = SqliteManager.GetConfigValue("SeriesFolderFormat");
                current.ApiKey = SqliteManager.GetConfigValue("ApiKey");
                current.DefaultLanguage = SqliteManager.GetConfigValue("DefaultLanguage");
            }
            return current;

        }

        [ConfigurationProperty("DbFile", IsRequired = true)]
        public string DbFile
        {
            
            get {
                if (dbFile == null) {
                    DbFile = (base["DbFile"] as string) != null ? (base["DbFile"] as string) : "TellySorter.db";
                }
                return dbFile;
            }
            set {
                dbFile = value;
            }

        }

    }

}
