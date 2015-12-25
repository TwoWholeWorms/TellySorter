namespace TellySorter
{

    using System;
    using System.Configuration;
    using System.Collections.Generic;

    [ConfigurationCollection(typeof(EpisodePathConfigCollection), AddItemName = "EpisodeTargetPath", CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class EpisodePathConfigCollection : ConfigurationElementCollection, IEnumerable<EpisodePathConfigurationElement>
    {
        public EpisodePathConfigurationElement this[int index]
        {
            get { return (EpisodePathConfigurationElement)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(EpisodePathConfigurationElement serviceConfig)
        {
            BaseAdd(serviceConfig);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new EpisodePathConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((EpisodePathConfigurationElement)element).Path;
        }

        public void Remove(EpisodePathConfigurationElement pathConfig)
        {
            BaseRemove(pathConfig.Path);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(String name)
        {
            BaseRemove(name);
        }

        public new IEnumerator<EpisodePathConfigurationElement> GetEnumerator()
        {
            int count = base.Count;
            for (int i = 0; i < count; i++)
            {
                yield return base.BaseGet(i) as EpisodePathConfigurationElement;
            }
        }

    }

}
