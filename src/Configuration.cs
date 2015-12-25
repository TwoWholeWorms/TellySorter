namespace TellySorter
{

    using NLog;
    using System;
    using System.Configuration;
    using System.Collections.Generic;
    using System.IO;

    public class Configuration : ConfigurationSection
    {

        public const string SECTION_NAME = "tellySorter";

        readonly static Logger logger = LogManager.GetCurrentClassLogger();

        static Configuration current = null;

        public string[] SourcePaths { get; set; }

        public bool SimulateMovements { get; set; }

        public string homePath;

        readonly string[] args;

        static Configuration()
        {

        }

        public static Configuration GetConfig()
        {

            if (current == null) {
                logger.Debug("Getting home directory");

                string homePath = (Environment.OSVersion.Platform == PlatformID.Unix ||
                    Environment.OSVersion.Platform == PlatformID.MacOSX)
                    ? Environment.GetEnvironmentVariable("HOME")
                    : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

                logger.Debug("Loading data from config file");

                current = ConfigurationManager.GetSection(Configuration.SECTION_NAME) as Configuration;
                current.homePath = homePath;

                logger.Debug("Setting movements");

                current.SimulateMovements = (Array.IndexOf(Core.args, "--simulate") > -1);
            }
            return current;

        }

        [ConfigurationProperty("DefaultTargetPath", IsRequired = true, IsKey = true)]
        public string DefaultTargetPath
        {

            get { return (base["DefaultTargetPath"] as string) != null ? (base["DefaultTargetPath"] as string) : (homePath + Path.DirectorySeparatorChar + "Media" + Path.DirectorySeparatorChar); }

        }

        [ConfigurationProperty("EpisodeNameFormat", IsRequired = true, IsKey = true)]
        public string EpisodeNameFormat
        {

            get { return (base["EpisodeNameFormat"] as string) != null ? (base["EpisodeNameFormat"] as string) : ("${seriesName} - s${seasonNumberPadded}e${episodeNumberPadded} - ${episodeName}.${fileExtension}"); }

        }

        [ConfigurationProperty("SeriesPathFormat", IsRequired = true, IsKey = true)]
        public string SeriesPathFormat
        {

            get { return (base["SeriesPathFormat"] as string) != null ? (base["SeriesPathFormat"] as string) : ("${seriesName}" + Path.DirectorySeparatorChar + "Season ${seasonNumberPadded}" + Path.DirectorySeparatorChar); }

        }

        [ConfigurationProperty("EpisodeTargetPaths")]
        public EpisodePathConfigCollection EpisodeTargetPaths
        {
            get { return (base["EpisodeTargetPaths"] as EpisodePathConfigCollection); }
        }

    }

}
