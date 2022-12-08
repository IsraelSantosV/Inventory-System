using RWS.Data.InventorySolution.Definitions;
using RWS.Data.InventorySolution.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RWS.Data.InventorySolution.Core
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "RWSEngine/Create Database")]
    public class ItemDatabase : ResourceSingleton<ItemDatabase>
    {
        [Header("CORE SETTINGS")]
        [SerializeField] private string resourcesPath = "Assets/Database";
        [SerializeField] private bool copyToPassData = false;
        [SerializeField] private ContainerItem itemContainerPrefab;
        [SerializeField] private ContainerItem itemInsiderContainerPrefab;
        [SerializeField] private List<ItemData> items = new();

        [Header("STATISTICS")]
        [Tooltip("Create a number of probabilities compatible with the number of rarities!")]
        [SerializeField] private List<float> raritiesProbability = new();
        [SerializeField] private List<EItemCategory> ignoreTypeList = new();
        [SerializeField] private List<ItemData> ignoreSingleItems = new();

        private Dictionary<EItemCategory, Dictionary<ERarity, List<ItemData>>> m_SortedItems = new();
        private bool m_HasInitialized;

#if UNITY_EDITOR
        private void OnValidate()
        {
            PopulateDatabase();
            InitializeDatabase();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private void InitializeDatabase()
        {
            CreateDatabase();
        }

        private void PopulateDatabase()
        {
            if (m_HasInitialized && Application.isPlaying)
            {
                return;
            }

            string[] assetNames = AssetDatabase.FindAssets("t:ItemData", new[] { resourcesPath });
            items.Clear();

            foreach (string SOName in assetNames)
            {
                var SOpath = AssetDatabase.GUIDToAssetPath(SOName);
                var item = AssetDatabase.LoadAssetAtPath<ItemData>(SOpath);
                items.Add(item);
            }

            if (Application.isPlaying)
            {
                Debug.Log("DATABASE INITIALIZATION ==========");
                Debug.Log($"Database Size = {items.Count}");
                m_HasInitialized = true;
            }
        }

        public void CreateDatabase()
        {
            m_SortedItems = new Dictionary<EItemCategory, Dictionary<ERarity, List<ItemData>>>();
            var categories = System.Enum.GetValues(typeof(EItemCategory));
            var rarities = System.Enum.GetValues(typeof(ERarity));

            for (int i = 0; i < categories.Length; i++)
            {
                m_SortedItems.Add((EItemCategory)categories.GetValue(i), new Dictionary<ERarity, List<ItemData>>());
                for (int j = 0; j < rarities.Length; j++)
                {
                    if (m_SortedItems.TryGetValue((EItemCategory)categories.GetValue(i), out var m_RarityDictionary))
                    {
                        m_RarityDictionary.Add((ERarity)rarities.GetValue(j), new List<ItemData>());
                    }
                }
            }

            foreach (var item in items)
            {
                if (item == null) continue;
                if (ignoreSingleItems.Contains(item)) continue;
                if (m_SortedItems.TryGetValue(item.Category, out var m_RarityDictionary))
                {
                    if (m_RarityDictionary.TryGetValue(item.Rarity, out var m_ItemList))
                    {
                        m_ItemList.Add(item);
                    }
                }
            }

            foreach (var m_RarityDictionary in m_SortedItems.Values)
            {
                foreach (var list in m_RarityDictionary.Values)
                {
                    list.Sort();
                }
            }
        }

        public IContainerItem CreateContainerItem(string fromId, IContainer container, Transform canvasTransform = null)
        {
            ItemData data = GetItem(fromId);
            if(data != null)
            {
                return CreateContainerItem(data, container, canvasTransform);
            }

            return null;
        }

        public IContainerItem CreateContainerItem(ItemData fromData, IContainer container, Transform canvasTransform = null)
        {
            ContainerItem prefab = fromData is InsiderContainerItemData
                ? itemInsiderContainerPrefab 
                : itemContainerPrefab;

            IContainerItem newItem = Instantiate(prefab);

            if(fromData is InsiderContainerItemData insiderData)
            {
                IContainer insideContainer = newItem.GetInsiderContainer();
                insideContainer.InitFromData(insiderData);
            }

            Transform canvasT = canvasTransform == null ? GetCanvasTransform() : canvasTransform;
            newItem.GetRect().SetParent(canvasT, false);
            newItem.GetRect().SetAsLastSibling();

            newItem.PutItem(fromData, container);
            newItem.SetAmount(1);
            return newItem;
        }

        public string GetTooltipForItem(IContainerItem item)
        {
            ItemData data = item.GetData();
            var tooltip = new StringBuilder();

            tooltip.Append("\n");
            tooltip.Append(data.ItemName + "          " + item.GetAmount() + "/" + data.MaxStack);
            tooltip.Append("\n");

            return tooltip.ToString();
        }

        private Transform GetCanvasTransform()
        {
            Canvas targetCanvas = FindObjectOfType<Canvas>();
            if (targetCanvas != null)
            {
                return targetCanvas.transform;
            }

            return null;
        }

        public ItemData CreateCopy(ItemData itemToCopy)
        {
            if(!copyToPassData)
            {
                return itemToCopy;
            }

            return Instantiate(itemToCopy);
        }

        private List<float> GetValidProbabilities()
        {
            var m_Probabilities = new List<float>(raritiesProbability);
            var m_RarityAmount = System.Enum.GetValues(typeof(ERarity)).Length;
            if (m_Probabilities.Count > m_RarityAmount)
            {
                int removeValues = m_Probabilities.Count - m_RarityAmount;
                for (int i = 0; i != removeValues; i++)
                {
                    m_Probabilities.RemoveAt(m_Probabilities.Count - 1);
                }
            }

            return m_Probabilities;
        }

        public ItemData GetItem(string m_Id)
        {
            ItemData item = items.FirstOrDefault(item => item.Id == m_Id);
            if (item == null)
            {
                Debug.Log("Item {" + m_Id + "} don't exist in database!");
                return null;
            }

            return CreateCopy(item);
        }

        public ItemData GetReferenceFromItem(string m_Id)
        {
            return items.FirstOrDefault(item => item.Id == m_Id);
        }

        public ItemData GetItem(string m_Name, EItemCategory m_Type)
        {
            if (m_SortedItems.TryGetValue(m_Type, out var m_RarityDictionary))
            {
                foreach (var m_List in m_RarityDictionary.Values)
                {
                    foreach (var item in m_List)
                    {
                        if (item.ItemName == m_Name)
                            return CreateCopy(item);
                    }
                }
            }

            Debug.Log("Item {" + m_Name + "} with type {" + m_Type + "} don't exist in database!");
            return null;
        }

        private EItemCategory GetRandomType()
        {
            var allTypes = System.Enum.GetValues(typeof(EItemCategory));
            var validTypes = new List<EItemCategory>();
            for (int i = 0; i < allTypes.Length; i++)
            {
                var targetType = (EItemCategory)allTypes.GetValue(i);
                if (!ignoreTypeList.Contains(targetType))
                {
                    validTypes.Add(targetType);
                }
            }

            if (validTypes.Count <= 0)
            {
                Debug.Log("Valid ItemTypes count is incorrectly: " + validTypes.Count);
            }

            return validTypes[UnityEngine.Random.Range(0, validTypes.Count)];
        }

        private ERarity SearchForFirstValidRarity(EItemCategory type)
        {
            var lowerRarity = (ERarity)Enum.GetValues(typeof(ERarity)).Length - 1;
            if (m_SortedItems.TryGetValue(type, out Dictionary<ERarity, List<ItemData>> rarityDictionary))
            {
                foreach (ERarity rarityKey in rarityDictionary.Keys)
                {
                    if (rarityDictionary.TryGetValue(rarityKey, out List<ItemData> values))
                    {
                        if (values.Count > 0 && rarityKey < lowerRarity)
                        {
                            lowerRarity = rarityKey;
                        }
                    }
                }
            }

            return lowerRarity;
        }

        public ItemData GetRandomItem()
        {
            var type = GetRandomType();
            return GetRandomItem(type);
        }

        public ItemData GetRandomItem(EItemCategory m_Type)
        {
            if (m_SortedItems.TryGetValue(m_Type, out Dictionary<ERarity, List<ItemData>> m_RarityDictionary))
            {
                List<float> m_Probabilities = GetValidProbabilities();
                int m_RarityIndex = RandomFromDistribution.RandomChoiceFollowingDistribution(m_Probabilities);

                if (m_RarityDictionary.TryGetValue((ERarity)m_RarityIndex, out List<ItemData> m_TargetList))
                {
                    if (m_TargetList.Count <= 0)
                    {
                        Debug.Log("An item of type was not found " + m_Type);
                        return GetRandomItem(m_Type, SearchForFirstValidRarity(m_Type));
                    }

                    return CreateCopy(m_TargetList[UnityEngine.Random.Range(0, m_TargetList.Count)]);
                }
            }

            Debug.Log("An item of type was not found " + m_Type);
            return null;
        }

        public ItemData GetRandomItem(EItemCategory m_Type, ERarity m_Rarity)
        {
            if (m_SortedItems.TryGetValue(m_Type, out var m_RarityDictionary))
            {
                if (m_RarityDictionary.TryGetValue(m_Rarity, out var m_TargetList))
                {
                    if (m_TargetList.Count <= 0)
                    {
                        Debug.Log("Not found an item of type " + m_Type + " and rarity " + m_Rarity);
                        return null;
                    }

                    return CreateCopy(m_TargetList[UnityEngine.Random.Range(0, m_TargetList.Count)]);
                }
            }

            Debug.Log("Not found an item of type " + m_Type + " and rarity " + m_Rarity);
            return null;
        }

        public ItemData GetRandomItem(ERarity m_Rarity)
        {
            var randomType = GetRandomType();

            if (m_SortedItems.TryGetValue(randomType, out var m_RarityDictionary))
            {
                if (m_RarityDictionary.TryGetValue(m_Rarity, out var m_TargetList))
                {
                    if (m_TargetList.Count <= 0)
                    {
                        Debug.Log("Not found an item of type " + randomType + " and rarity " + m_Rarity);
                        return null;
                    }

                    return CreateCopy(m_TargetList[UnityEngine.Random.Range(0, m_TargetList.Count)]);
                }
            }

            Debug.Log("Not found an item of type " + randomType + " and rarity " + m_Rarity);
            return null;
        }

        public ItemData GetRandomItem(List<ERarity> m_Rarities)
        {
            EItemCategory randomType = GetRandomType();
            return GetRandomItem(randomType, m_Rarities);
        }

        public ItemData GetRandomItem(EItemCategory m_Type, List<ERarity> m_Rarities)
        {
            if (m_SortedItems.TryGetValue(m_Type, out Dictionary<ERarity, List<ItemData>> m_RarityDictionary))
            {
                var obtainedItems = new List<ItemData>();
                foreach (ERarity rarity in m_Rarities)
                {
                    if (m_RarityDictionary.TryGetValue(rarity, out List<ItemData> m_TargetList))
                    {
                        if (m_TargetList.Count <= 0) continue;
                        obtainedItems.Add(m_TargetList[UnityEngine.Random.Range(0, m_TargetList.Count)]);
                    }
                }

                List<float> m_Probabilities = GetValidProbabilities();
                int m_RarityIndex = RandomFromDistribution.RandomChoiceFollowingDistribution(m_Probabilities);
                if (m_RarityIndex < 0 || m_RarityIndex >= obtainedItems.Count)
                {
                    if (obtainedItems.Count > 0)
                    {
                        return CreateCopy(obtainedItems[0]);
                    }

                    return null;
                }
                return CreateCopy(obtainedItems[m_RarityIndex]);
            }

            return null;
        }

        public ItemData GetRandomItem(List<EItemCategory> m_ItemTypes, List<ERarity> m_Rarities)
        {
            var sortedItems = new List<ItemData>();

            for (int j = 0; j < m_ItemTypes.Count; j++)
            {
                if (m_SortedItems.TryGetValue(m_ItemTypes[j], out Dictionary<ERarity, List<ItemData>> m_RarityDictionary))
                {
                    var obtainedItems = new List<ItemData>();
                    for (int i = 0; i < m_Rarities.Count; i++)
                    {
                        if (m_RarityDictionary.TryGetValue(m_Rarities[i], out List<ItemData> m_TargetList))
                        {
                            if (m_TargetList.Count <= 0) continue;
                            obtainedItems.Add(m_TargetList[UnityEngine.Random.Range(0, m_TargetList.Count)]);
                        }
                    }

                    var m_Probabilities = GetValidProbabilities();
                    var m_RarityIndex = RandomFromDistribution.RandomChoiceFollowingDistribution(m_Probabilities);
                    if (m_RarityIndex < 0 || m_RarityIndex >= obtainedItems.Count)
                    {
                        if (obtainedItems.Count > 0)
                        {
                            sortedItems.Add(obtainedItems[0]);
                        }

                        continue;
                    }

                    sortedItems.Add(obtainedItems[m_RarityIndex]);
                }
            }

            return sortedItems.Count > 0 ? CreateCopy(sortedItems[UnityEngine.Random.Range(0, sortedItems.Count)]) : null;
        }
    }
}
