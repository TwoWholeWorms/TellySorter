namespace TellySorter
{

    //using NLog;
    using System.Configuration;

    public class ShowTargetPathConfigurationElement : ConfigurationElement
	{

    //    readonly static Logger logger = LogManager.GetCurrentClassLogger();
        
        [ConfigurationProperty("Path", IsRequired = true, IsKey = true)]
        public string Path
        {
            get { return (base["Path"] as string); }
        }

        [ConfigurationProperty("ShowIds", IsRequired = true)]
        public string[] ShowIds
        {
            get { return (base["ShowIds"] as string).Split(','); }
        }

	}

}
