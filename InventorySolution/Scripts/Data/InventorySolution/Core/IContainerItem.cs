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
        IContainer GetInsiderContainer();
        bool IsShowingInsiderContainer();

        void SetGridPosition(Vector2Int position);
        void SetAmount(int amount);
        void SetContainer(IContainer container);
        void SetPreviewContainer(IContainer preview);
        void DestroyInsideContainer();

        void PutItem(ItemData item, IContainer fromContainer);
        void RefreshAmount();
        void Rotate();
        void UseItem(ICharacterContainerHandler owner);
        void UseInsiderContainer(ICharacterContainerHandler owner);
    }
}
