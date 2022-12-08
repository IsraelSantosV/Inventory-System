using RWS.Data;
using RWS.Data.InventorySolution.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RWS.Data.InventorySolution
{
    public class GridContainer : MonoBehaviour, IContainer
    {
        private enum EDebugMode
        {
            Always,
            OnlySelected
        }

        //================================================== FIELDS

        [Header("DEFINITIONS")]
        [SerializeField] private GameObject owner;
        [SerializeField] private EContainerMode containerMode = EContainerMode.FullControl;
        [SerializeField] private EContainerCategory containerCategory = EContainerCategory.Common;
        [SerializeField] private EShowingContainerMode showingMode = EShowingContainerMode.Always;

        [Header("CORE SETTINGS")]
        [SerializeField] private bool autoInit = true;
        [SerializeField] private bool randomGenerateId = false;
        [SerializeField] private string containerId;
        [SerializeField] private Vector2Int containerSize = new(10, 10);
        [SerializeField] private Vector2Int tileSize = new(32, 32);
        [SerializeField] private Vector2Int itemSize = new(32, 32);

        [Header("WEIGHT FEATURE")]
        [SerializeField] private bool useWeightFeature;
        [SerializeField] private float maxWeight;
        [SerializeField] private float exceedPermitedWeight;

        [Header("SAVE SETTINGS")]
        [SerializeField] private bool saveOnQuit;
        [SerializeField] private bool loadOnStart;

        [Header("VISUAL SETTINGS")]
        [SerializeField] private string title = "Container";
        [SerializeField] private bool createWithCustomSlot;
        [SerializeField] private GameObject customSlotPrefab;
        [SerializeField] private Image gridBackground;
        [SerializeField] private Sprite filledSlotTexture;
        [SerializeField] private TMP_Text containerTitle;

        [Header("DEBUG SETTINGS")]
        [SerializeField] private bool drawDebug = true;
        [SerializeField] private EDebugMode debugMode = EDebugMode.OnlySelected;
        [SerializeField] private Color debugColor = Color.red;

        //==================================================

        private IContainerItem[,] m_Container;
        private ContainerSaveData m_SaveData;
        private GridPermissions m_Permissions;
        private ICharacterContainerHandler m_Owner;
        private Image[,] m_CustomSlots;

        private Vector2 m_Size;
        private RectTransform m_Rect;
        private Transform m_Canvas;
        private Sprite m_DefaultSlotIcon;

        public event Action OnUpdateContainer;

        public EContainerMode GetMode() => containerMode;
        public int GetWidth() => containerSize.x;
        public int GetHeight() => containerSize.y;
        public Vector2Int GetTileSize() => tileSize;
        public Vector2Int GetItemSize() => itemSize;
        public float GetMaxWeight() => maxWeight;
        public EContainerCategory GetCategory() => containerCategory;
        public EShowingContainerMode GetShowingMode() => showingMode;

        public ICharacterContainerHandler GetOwner()
        {
            if(m_Owner == null && owner != null)
            {
                SetOwner(owner);
            }

            return m_Owner;
        }

        public RectTransform GetRect()
        {
            if(m_Rect != null)
            {
                return m_Rect;
            }

            m_Rect = GetComponent<RectTransform>();
            return m_Rect;
        }

        public string GetId()
        {
            if(randomGenerateId && string.IsNullOrEmpty(containerId))
            {
                containerId = Guid.NewGuid().ToString();
            }

            return containerId;
        }

        public void SetOwner(GameObject targetOwner)
        {
            if (targetOwner != null)
            {
                owner = targetOwner;
                if (owner.TryGetComponent(out m_Owner))
                {
                    m_Owner.AttachContainer(this, containerCategory);
                }
            }
        }

        public void SetTitle(string text)
        {
            if(containerTitle != null)
            {
                containerTitle.text = text;
            }

            title = text;
        }

        private void Awake()
        {
            if(m_Canvas == null)
            {
                m_Canvas = ContainerController.Instance.GetCanvasTransform();
                if(m_Canvas == null)
                {
                    Debug.Log("Don't found canvas for the " + transform.name);
                    return;
                }
            }

            SetOwner(owner);

            if(autoInit)
            {
                Init();
            }
        }

        private void OnApplicationQuit()
        {
            if(saveOnQuit)
            {
                Save();
            }
        }

        public void InitFromData(InsiderContainerItemData data)
        {
            if(data == null)
            {
                return;
            }

            containerSize = data.ContainerSize;
            containerMode = data.ContainerMode;
            containerCategory = data.ContainerCategory;
            useWeightFeature = data.UseWeightFeature;
            maxWeight = data.MaxWeight;
            exceedPermitedWeight = data.ExceedPermitedWeight;

            title = data.ItemName;
            Init();
        }

        private void Init()
        {
            m_Size = new Vector2(GetWidth() * tileSize.x, GetHeight() * tileSize.y);
            m_Container = new IContainerItem[GetWidth(), GetHeight()];
            m_CustomSlots = new Image[GetWidth(), GetHeight()];
            m_SaveData = new ContainerSaveData(this);
            m_Permissions = GetComponent<GridPermissions>();

            SetTitle(title);

            if (gridBackground == null)
            {
                gridBackground = GetComponent<Image>();
            }

            CreateGrid();

            if(loadOnStart)
            {
                Load();
            }
        }

        private void CreateGrid()
        {
            GetRect().sizeDelta = m_Size;
            GetRect().pivot = new Vector2(0, 1);

            if(gridBackground != null)
            {
                gridBackground.raycastTarget = true;
                gridBackground.raycastPadding = Vector4.one;
            }

            if(createWithCustomSlot && customSlotPrefab != null)
            {
                var spritesRoot = new GameObject("SlotHandler");
                spritesRoot.transform.SetParent(transform, false);
                spritesRoot.transform.localPosition = Vector3.zero;

                var gridGroup = spritesRoot.AddComponent<GridLayoutGroup>();
                gridGroup.spacing = Vector2.zero;
                gridGroup.cellSize = GetTileSize();

                var groupRect = gridGroup.GetComponent<RectTransform>();
                groupRect.pivot = new Vector2(.5f, .5f);

                groupRect.anchorMin = Vector2.zero;
                groupRect.anchorMax = Vector2.one;

                groupRect.offsetMax = Vector2.zero;
                groupRect.offsetMin = Vector2.zero;

                for (var y = 0; y < GetHeight(); y++)
                {
                    for (var x = 0; x < GetWidth(); x++)
                    {
                        GameObject customSlot = Instantiate(customSlotPrefab, gridGroup.transform, false);
                        if (customSlot.TryGetComponent(out Image icon))
                        {
                            m_CustomSlots[x, y] = icon;

                            if (m_DefaultSlotIcon == null)
                            {
                                m_DefaultSlotIcon = icon.sprite;
                            }
                        }
                    }
                }
            }
        }

        public EContainerOp PlaceItem(IContainerItem item, int x, int y, ref IContainerItem overlapItem)
        {
            Vector2Int itemSize = item.GetSize();

            if (!PositionInsideGrid(x, y, itemSize))
            {
                return EContainerOp.ErrorInPosition;
            }

            if (!OverlapChecking(x, y, itemSize, ref overlapItem))
            {
                overlapItem = null;
                return EContainerOp.BlockedByItem;
            }

            if(!HasPermissionToPlace(item))
            {
                return EContainerOp.DontHavePermission;
            }

            if (!HasAvailableWeight(item.GetData()))
            {
                return EContainerOp.WeightExcedeed;
            }

            if (overlapItem != null)
            {
                if(overlapItem.GetData() == item.GetData())
                {
                    IContainerItem lastOverlap = overlapItem;
                    overlapItem = PlaceSameItemInPosition(item, overlapItem);
                    if(overlapItem == null || lastOverlap != overlapItem)
                    {
                        OnUpdateContainer?.Invoke();
                        return EContainerOp.Success;
                    }
                }

                CleanGridReference(overlapItem);
            }

            return PlaceItem(item, x, y);
        }

        public EContainerOp PlaceItem(IContainerItem item, int x, int y)
        {
            Vector2Int itemSize = item.GetSize();
            item.GetRect().SetParent(GetRect());

            for (var posX = 0; posX < itemSize.x; posX++)
            {
                for (var posY = 0; posY < itemSize.y; posY++)
                {
                    m_Container[x + posX, y + posY] = item;

                    Image slotIcon = GetCustomSlotIcon(x + posX, y + posY);
                    if (slotIcon != null)
                    {
                        slotIcon.sprite = filledSlotTexture;
                    }
                }
            }

            item.SetGridPosition(new Vector2Int(x, y));
            Vector2 worldPosition = CalculatePositionOnGrid(x, y, itemSize);

            item.GetRect().localPosition = worldPosition;
            OnUpdateContainer?.Invoke();
            return EContainerOp.Success;
        }

        public EContainerOp AddItem(ItemData item, int amount)
        {
            if (m_Permissions != null && !m_Permissions.ValidatePermissions(item))
            {
                return EContainerOp.DontHavePermission;
            }

            if (!HasAvailableWeight(item))
            {
                return EContainerOp.WeightExcedeed;
            }

            Vector2Int? availablePos = FindSpaceForItem(item, 1);
            if (availablePos == null)
            {
                return EContainerOp.ContainerIsFull;
            }

            IContainerItem existentItem = m_Container[availablePos.Value.x, availablePos.Value.y];
            if (existentItem != null && existentItem.GetAvailableStack() > 0)
            {
                return AddAmountInPosition(existentItem, amount);
            }

            IContainerItem newItem = ItemDatabase.Instance.CreateContainerItem(item, this, m_Canvas);
            return PlaceItem(newItem, availablePos.Value.x, availablePos.Value.y);
        }

        public EContainerOp RemoveItem(ItemData item, int amount)
        {
            IContainerItem foundItem = GetItem(item);
            if(foundItem == null)
            {
                return EContainerOp.Incomplete;
            }

            Vector2Int slotPos = foundItem.GetGridPosition();
            (EContainerOp op, int removedAmount) = RemoveFromSlot(slotPos.x, slotPos.y, amount);
            if(op != EContainerOp.Success)
            {
                return op;
            }

            int restAmountToRemove = amount - removedAmount;
            if(restAmountToRemove > 0)
            {
                return RemoveItem(item, restAmountToRemove);
            }

            return EContainerOp.Success;
        }

        public (EContainerOp, int) RemoveFromSlot(int x, int y, int amount)
        {
            IContainerItem item = GetItem(x, y);
            if(item == null)
            {
                return (EContainerOp.ErrorInItem, 0);
            }

            if(amount <= 0)
            {
                return (EContainerOp.ErrorInAmount, 0);
            }

            int finalAmount = amount > item.GetAmount() ? item.GetAmount() : amount;
            item.SetAmount(item.GetAmount() - finalAmount);
            OnUpdateContainer?.Invoke();

            if (item.GetAmount() <= 0)
            {
                DropItem(item, false);
            }

            return (EContainerOp.Success, finalAmount);
        }

        public CollectItem DropItem(IContainerItem item, bool safeClear)
        {
            if(item == null)
            {
                return null;
            }

            item.DestroyInsideContainer();

            CollectItem collectable = item.GetCollectable();
            CleanGridReference(item, safeClear);
            if(item is MonoBehaviour behaviour)
            {
                Destroy(behaviour.gameObject);
            }

            OnUpdateContainer?.Invoke();
            return collectable;
        }

        public Vector2Int GetTileGridPosition(Vector2 worldPosition)
        {
            Vector2 positionOnTheGrid = new()
            {
                x = worldPosition.x - GetRect().position.x,
                y = GetRect().position.y - worldPosition.y
            };

            return new Vector2Int()
            {
                x = (int)(positionOnTheGrid.x / tileSize.x),
                y = (int)(positionOnTheGrid.y / tileSize.y)
            };
        }

        public Vector2 CalculatePositionOnGrid(int x, int y, Vector2Int itemSize)
        {
            return new()
            {
                x = x * GetTileSize().x + GetTileSize().x * itemSize.x / 2,
                y = -(y * GetTileSize().y + GetTileSize().y * itemSize.y / 2)
            };
        }

        public bool PositionInsideGrid(int x, int y, Vector2Int size)
        {
            if (!IsValidPosition(x, y))
            {
                return false;
            }

            x += size.x - 1;
            y += size.y - 1;

            if (!IsValidPosition(x, y))
            {
                return false;
            }

            return true;
        }

        public float GetContainerWeight()
        {
            List<IContainerItem> countItems = new();
            for (var x = 0; x < GetWidth(); x++)
            {
                for (var y = 0; y < GetHeight(); y++)
                {
                    IContainerItem item = GetItem(x, y);
                    if(item != null && !countItems.Contains(item))
                    {
                        countItems.Add(item);
                    }
                }
            }

            float weight = 0;
            countItems.ForEach(countItem => weight += countItem.GetWeight());
            return weight;
        }

        public int GetAmountRemaining(ItemData item)
        {
            List<IContainerItem> countItems = new();
            for (var x = 0; x < GetWidth(); x++)
            {
                for (var y = 0; y < GetHeight(); y++)
                {
                    IContainerItem currentItem = GetItem(x, y);
                    if (currentItem != null && currentItem.GetData() == item)
                    {
                        if(!countItems.Contains(currentItem))
                        {
                            countItems.Add(currentItem);
                        }
                    }
                }
            }

            int amount = 0;
            countItems.ForEach(countItem => amount += countItem.GetAmount());
            return amount;
        }

        public IContainerItem PickUpItem(int x, int y)
        {
            IContainerItem item = m_Container[x, y];

            if (item == null)
            {
                return null;
            }

            CleanGridReference(item);
            return item;
        }

        public IContainerItem GetItem(int x, int y)
        {
            return m_Container[x, y];
        }

        public IContainerItem GetItem(ItemData item)
        {
            for (var x = 0; x < GetWidth(); x++)
            {
                for (var y = 0; y < GetHeight(); y++)
                {
                    IContainerItem currentItem = GetItem(x, y);
                    if(currentItem != null && currentItem.GetData() == item)
                    {
                        return currentItem;
                    }
                }
            }

            return null;
        }

        public Vector2Int? FindSpaceForItem(ItemData item, int amount)
        {
            for (var y = 0; y < GetHeight() - item.Size.y + 1; y++)
            {
                for (var x = 0; x < GetWidth() - item.Size.x + 1; x++)
                {
                    if (m_Container[x, y] != null && m_Container[x, y].GetData() == item)
                    {
                        int availableAmount = GetAvailableStackInPosition(x, y);
                        if(availableAmount >= amount)
                        {
                            return new Vector2Int(x, y);
                        }
                    }

                    if (CheckForAvailableSpace(x, y, item.Size))
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }

            return null;
        }

        public bool OverlapChecking(int x, int y, Vector2Int itemSize, ref IContainerItem overlapItem)
        {
            for (var posX = 0; posX < itemSize.x; posX++)
            {
                for (var posY = 0; posY < itemSize.y; posY++)
                {
                    IContainerItem currentItem = m_Container[x + posX, y + posY];
                    if (currentItem != null)
                    {
                        if (overlapItem == null)
                        {
                            overlapItem = currentItem;
                        }
                        else if (overlapItem != currentItem)
                        {
                            overlapItem = null;
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public bool HasAvailableWeight(ItemData item)
        {
            if(!useWeightFeature)
            {
                return true;
            }

            float currentWeight = GetContainerWeight();
            return item.Weight + currentWeight <= maxWeight + exceedPermitedWeight;
        }

        public bool HasPermissionToPlace(IContainerItem item)
        {
            if(m_Permissions == null)
            {
                return true;
            }

            return m_Permissions.ValidatePermissions(item);
        }

        public void ClearContainer()
        {
            GetAllItems().ForEach(item =>
            {
                Vector2Int gridPos = item.GetGridPosition();
                RemoveFromSlot(gridPos.x, gridPos.y, item.GetAmount());
            });

            OnUpdateContainer?.Invoke();
        }

        public void Save()
        {
            m_SaveData.SaveData(GetAllItems());
        }

        public void Load()
        {
            List<ContainerSaveData.InternalData> savedData = m_SaveData.LoadData();
            if(savedData != null)
            {
                ClearContainer();
                savedData.ForEach(data =>
                {
                    ItemDatabase database = ItemDatabase.Instance;
                    ItemData itemData = database.GetItem(data.Id);

                    IContainerItem item = database.CreateContainerItem(itemData, this, m_Canvas);
                    item.SetAmount(data.Amount);
                    if(!data.InHorizontal)
                    {
                        item.Rotate();
                    }

                    EContainerOp placeOp = PlaceItem(item, data.PosX, data.PosY);
                    if(placeOp != EContainerOp.Success)
                    {
                        Debug.Log("Error on load data: [" + data.Id + "] | " + placeOp.ToString());
                    }
                });
            }
        }

        private List<IContainerItem> GetAllItems()
        {
            var items = new List<IContainerItem>();
            for (var x = 0; x < GetWidth(); x++)
            {
                for (var y = 0; y < GetHeight(); y++)
                {
                    IContainerItem item = GetItem(x, y);
                    if (item != null && !items.Contains(item))
                    {
                        items.Add(item);
                    }
                }
            }

            return items;
        }

        private EContainerOp AddAmountInPosition(IContainerItem containerItem, int amount)
        {
            if(containerItem == null)
            {
                return EContainerOp.ErrorInItem;
            }

            int restAmount = amount;
            int availableStack = containerItem.GetAvailableStack();
            if (availableStack >= amount)
            {
                containerItem.SetAmount(containerItem.GetAmount() + amount);
                restAmount = 0;
            }
            else
            {
                int lastAmount = containerItem.GetAmount();
                containerItem.SetAmount(containerItem.GetData().MaxStack);
                restAmount -= (containerItem.GetData().MaxStack - lastAmount);
            }

            if (restAmount > 0)
            {
                return AddItem(containerItem.GetData(), restAmount);
            }

            OnUpdateContainer?.Invoke();
            return EContainerOp.Success;
        }

        private void CleanGridReference(IContainerItem item, bool safeClean = false)
        {
            Vector2Int itemSize = item.GetSize();
            Vector2Int gridPos = item.GetGridPosition();

            for (var posX = 0; posX < itemSize.x; posX++)
            {
                for (var posY = 0; posY < itemSize.y; posY++)
                {
                    if(safeClean && GetItem(gridPos.x + posX, gridPos.y + posY) != null)
                    {
                        continue;
                    }

                    m_Container[gridPos.x + posX, gridPos.y + posY] = null;

                    Image slotIcon = GetCustomSlotIcon(gridPos.x + posX, gridPos.y + posY);
                    if(slotIcon != null)
                    {
                        slotIcon.sprite = m_DefaultSlotIcon;
                    }
                }
            }
        }

        private IContainerItem PlaceSameItemInPosition(IContainerItem source, IContainerItem destination)
        {
            if(destination.GetAvailableStack() <= 0 || source.GetAvailableStack() <= 0)
            {
                return destination;
            }

            if(source.GetAmount() <= destination.GetAvailableStack())
            {
                destination.SetAmount(destination.GetAmount() + source.GetAmount());
                if(source is MonoBehaviour behaviour)
                {
                    Destroy(behaviour.gameObject);
                }

                return null;
            }

            int lastAmount = destination.GetAmount();
            destination.SetAmount(destination.GetData().MaxStack);
            source.SetAmount(source.GetAmount() - (destination.GetData().MaxStack - lastAmount));
            return source;
        }

        private bool IsValidPosition(int x, int y)
        {
            if(x < 0 || y < 0)
            {
                return false;
            }

            if(x >= GetWidth() || y >= GetHeight())
            {
                return false;
            }

            return true;
        }

        private bool CheckForAvailableSpace(int x, int y, Vector2Int itemSize)
        {
            for (var posX = 0; posX < itemSize.x; posX++)
            {
                for (var posY = 0; posY < itemSize.y; posY++)
                {
                    IContainerItem currentItem = m_Container[x + posX, y + posY];
                    if (currentItem != null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private int GetAvailableStackInPosition(int x, int y)
        {
            if(!IsValidPosition(x, y))
            {
                return 0;
            }

            if(m_Container[x, y] == null)
            {
                return int.MaxValue;
            }

            return m_Container[x, y].GetAvailableStack();
        }

        private Image GetCustomSlotIcon(int x, int y)
        {
            if(!createWithCustomSlot || customSlotPrefab == null || filledSlotTexture == null)
            {
                return null;
            }

            return m_CustomSlots[x, y];
        }

        private void OnDrawGizmosSelected()
        {
            if(!drawDebug || debugMode != EDebugMode.OnlySelected)
            {
                return;
            }

            Debug_DrawGrid();
        }

        private void OnDrawGizmos()
        {
            if (!drawDebug || debugMode != EDebugMode.Always)
            {
                return;
            }

            Debug_DrawGrid();
        }

        private void Debug_DrawGrid()
        {
            Gizmos.color = debugColor;
            Vector2 startPos = GetRect().position;

            for (var x = 0; x < GetWidth(); x++)
            {
                for (var y = 0; y < GetHeight(); y++)
                {
                    Vector2 positionOnTheGrid = new Vector2(x * GetTileSize().x, -(y * GetTileSize().y)) + startPos;
                    Vector2 rightUpPos = new(positionOnTheGrid.x + GetTileSize().x, positionOnTheGrid.y);
                    Vector2 rightDownPos = new(rightUpPos.x, rightUpPos.y - GetTileSize().y);
                    Vector2 leftDownPos = new(rightDownPos.x - GetTileSize().x, rightDownPos.y);
                    Vector2 leftUpPos = new(leftDownPos.x, leftDownPos.y + GetTileSize().y);

                    Debug_DrawSquare(positionOnTheGrid, new Vector2[]
                    {
                        rightUpPos, rightDownPos, leftDownPos, leftUpPos
                    });
                }
            }
        }

        private void Debug_DrawSquare(Vector2 startPos, Vector2[] corners)
        {
            Gizmos.DrawLine(startPos, corners[0]);
            Gizmos.DrawLine(corners[0], corners[1]);
            Gizmos.DrawLine(corners[1], corners[2]);
            Gizmos.DrawLine(corners[2], corners[3]);
        }
    }
}
