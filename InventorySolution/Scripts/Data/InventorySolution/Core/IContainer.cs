using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RWS.Data.InventorySolution.Core
{
    public interface IContainer
    {
        public event Action OnUpdateContainer;

        string GetId();
        EContainerMode GetMode();
        int GetWidth();
        int GetHeight();
        float GetMaxWeight();
        Vector2Int GetTileSize();
        Vector2Int GetItemSize();
        RectTransform GetRect();
        ICharacterContainerHandler GetOwner();
        void SetOwner(GameObject owner);
        EContainerCategory GetCategory();

        EContainerOp PlaceItem(IContainerItem item, int x, int y);
        EContainerOp PlaceItem(IContainerItem item, int x, int y, ref IContainerItem overlapItem);

        EContainerOp AddItem(ItemData item, int amount);
        EContainerOp RemoveItem(ItemData item, int amount);
        (EContainerOp, int) RemoveFromSlot(int x, int y, int amount);
        CollectItem DropItem(IContainerItem item);
        
        Vector2Int GetTileGridPosition(Vector2 worldPosition);
        Vector2 CalculatePositionOnGrid(int x, int y, Vector2Int itemSize);
        bool PositionInsideGrid(int x, int y, Vector2Int size);
        float GetContainerWeight();
        int GetAmountRemaining(ItemData item);

        IContainerItem PickUpItem(int x, int y);
        IContainerItem GetItem(int x, int y);
        IContainerItem GetItem(ItemData item);
        Vector2Int? FindSpaceForItem(ItemData item, int amount);
        bool OverlapChecking(int x, int y, Vector2Int itemSize, ref IContainerItem overlapItem);
        bool HasAvailableWeight(ItemData item);
        bool HasPermissionToPlace(IContainerItem item);

        void Save();
        void Load();
    }

    public enum EContainerMode
    {
        FullControl,
        DropOnly,
        DragOnly,
        ReadOnly
    }

    public enum EContainerOp
    {
        Success,
        ErrorInItem,
        ErrorInPosition,
        ErrorInAmount,
        BlockedByItem,
        Incomplete,
        WeightExcedeed,
        DontHavePermission,
        ContainerIsFull
    }

    [Serializable]
    public class CollectItem
    {
        public ItemData Item;
        public int Count;
    }
}
