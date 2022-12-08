using RWS.Data.InventorySolution.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RWS.Data.InventorySolution
{
    [Serializable]
    public class ContainerSaveData
    {
        [Serializable]
        public class SaveFile
        {
            public List<InternalData> DataList = new();
        }

        [Serializable]
        public class InternalData
        {
            public string Id;
            public int Amount;
            public int PosX;
            public int PosY;
            public bool InHorizontal;
        }

        private const string FOLDER = "ContainerData";
        private const string EXTENSION = ".json";
        private IContainer m_Container;

        public ContainerSaveData(IContainer container)
        {
            m_Container = container;
            //MMSaveLoadManager.saveLoadMethod = new MMSaveLoadManagerMethodJson();
        }

        public void SaveData(List<IContainerItem> items)
        {
            var data = new SaveFile();
            items.ForEach(item =>
            {
                if(item.GetContainer() == m_Container)
                {
                    ItemData itemData = item.GetData();
                    if (itemData != null)
                    {
                        InternalData dataItem = new()
                        {
                            Id = itemData.Id,
                            Amount = item.GetAmount(),
                            InHorizontal = item.InHorizontalOrientation(),
                            PosX = item.GetGridPosition().x,
                            PosY = item.GetGridPosition().y
                        };

                        data.DataList.Add(dataItem);
                    }
                }
            });

            //MMSaveLoadManager.Save(data, m_Container.GetId() + EXTENSION, FOLDER);
        }

        public List<InternalData> LoadData()
        {
            SaveFile loadedData = null;
            /*SaveFile loadedData = (SaveFile)MMSaveLoadManager.Load(typeof(SaveFile), 
                m_Container.GetId() + EXTENSION, FOLDER);
*/
            if(loadedData != null)
            {
                return new List<InternalData>(loadedData.DataList);
            }

            return null;
        }
    }
}
