namespace TellySorter
{

    using System;
    using System.Configuration;
    using System.Collections.Generic;

    [ConfigurationCollection(typeof(ShowTargetPathConfigCollection), AddItemName = "ShowTargetPath", CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class ShowTargetPathConfigCollection : ConfigurationElementCollection, IEnumerable<ShowTargetPathConfigurationElement>
    {
        public ShowTargetPathConfigurationElement this[int index]
        {
            get { return (ShowTargetPathConfigurationElement)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(ShowTargetPathConfigurationElement serviceConfig)
        {
            BaseAdd(serviceConfig);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ShowTargetPathConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ShowTargetPathConfigurationElement)element).Path;
        }

        public void Remove(ShowTargetPathConfigurationElement pathConfig)
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

        public new IEnumerator<ShowTargetPathConfigurationElement> GetEnumerator()
        {
            int count = base.Count;
            for (int i = 0; i < count; i++)
            {
                yield return base.BaseGet(i) as ShowTargetPathConfigurationElement;
            }
        }

    }

}
