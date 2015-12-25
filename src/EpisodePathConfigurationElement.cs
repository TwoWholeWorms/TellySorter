namespace TellySorter
{

    //using NLog;
    using System.Configuration;

    public class EpisodePathConfigurationElement : ConfigurationElement
	{

    //    readonly static Logger logger = LogManager.GetCurrentClassLogger();
        
        [ConfigurationProperty("path", IsRequired = true)]
        public string Path
        {
            get { return (base["path"] as string); }
        }

        [ConfigurationProperty("episodeIds", IsRequired = true, IsKey = true)]
        public string[] EpisodeIds
        {
            get { return (base["episodeIds"] as string).Split(','); }
        }

	}

}
