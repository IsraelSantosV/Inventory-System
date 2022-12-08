using RWS.Data.InventorySolution.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RWS.Data.InventorySolution
{
    public class CharacterContainerTest : MonoBehaviour, ICharacterContainerHandler
    {
        private class ContainerData
        {
            public IContainer Container;
            public EContainerCategory Category;
        }

        private readonly List<ContainerData> m_Containers = new();

        public void AttachContainer(IContainer container, EContainerCategory category)
        {
            if(GetContainer(category) == null)
            {
                m_Containers.Add(new ContainerData
                { 
                    Container = container,
                    Category = category
                });
            }
        }

        public List<IContainer> GetAttachedContainers()
        {
            var containers = new List<IContainer>();
            m_Containers.ForEach(data =>
            {
                containers.Add(data.Container);
            });

            return containers;
        }

        public IContainer GetContainer(EContainerCategory byCategory)
        {
            ContainerData data = m_Containers.FirstOrDefault(data => data.Category == byCategory);
            if(data == null)
            {
                return null;
            }

            return data.Container;
        }

        public void OnUseItem(IContainerItem item)
        {
            Debug.Log(transform.name + " use item: " + item.GetData().ItemName);
        }

        public void RemoveContainer(IContainer container)
        {
            ContainerData containerData = m_Containers
                .FirstOrDefault(data => data.Container == container);

            if(containerData != null)
            {
                m_Containers.Remove(containerData);
            }
        }
    }
}
