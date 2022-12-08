using UnityEngine;
using UnityEngine.UI;

namespace RWS.Data.InventorySolution.Core
{
    public interface IContainerItem
    {
        Vector2Int GetSize();
        RectTransform GetRect();
        Image GetImage();
        ItemData GetData();
        int GetAmount();
        int GetAvailableStack();
        Vector2Int GetGridPosition();
        bool InHorizontalOrientation();
        IContainer GetContainer();
        CollectItem GetCollectable();
        float GetWeight();

        void SetGridPosition(Vector2Int position);
        void SetAmount(int amount);
        void SetContainer(IContainer container);
        void SetPreviewContainer(IContainer preview);

        void PutItem(ItemData item, IContainer fromContainer);
        void RefreshAmount();
        void Rotate();
        void UseItem(ICharacterContainerHandler owner);
    }
}
