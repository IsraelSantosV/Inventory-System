using RWS.Data.InventorySolution.Core;
using RWS.Data.InventorySolution.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RWS.Data.InventorySolution
{
    public class ContainerController : MonoBehaviour
    {
        private static ContainerController m_Instance;
        public static ContainerController Instance
        {
            get
            {
                if(m_Instance == null)
                {
                    m_Instance = FindObjectOfType<ContainerController>();
                }

                return m_Instance;
            }
        }

        [SerializeField] private Transform canvasTransform;
        [SerializeField] private ICharacterContainerHandler mainPlayer;

        [Header("INPUT SETTINGS")]
        [SerializeField] private KeyCode rotateKey = KeyCode.R;
        [SerializeField] private KeyCode specialKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode addItemKey = KeyCode.W;
        [SerializeField] private KeyCode giveItemKey = KeyCode.Q;
        [SerializeField] private KeyCode removeRandomKey = KeyCode.E;

        [Header("DEBUG SETTINGS")]
        [SerializeField] private bool enableDebug = true;
        [SerializeField] private List<ItemData> testItems = new();

        private IContainer m_SelectedContainer;
        private IContainerItem m_SelectedItem;
        private IContainerItem m_OverlapItem;
        private IContainerItem m_OverlapPreviewItem;
        private IContainerItem m_TooltipInItem;
        private RectTransform m_SelectedRect;

        private ContainerHighlight m_Highlight;
        private Vector2 m_LastHighlightPos;

        public Transform GetCanvasTransform()
        {
            return canvasTransform;
        }

        private void Awake()
        {
            m_Highlight = GetComponent<ContainerHighlight>();
        }

        public IContainer GetSelectedContainer()
        {
            return m_SelectedContainer;
        }

        public void SelectContainer(IContainer container)
        {
            if(m_SelectedItem != null && m_SelectedItem.GetContainer() != null)
            {
                if(m_SelectedItem.GetContainer() != container && container != null)
                {
                    if(container.GetShowingMode() != EShowingContainerMode.Closable)
                    {
                        container.GetRect().SetAsFirstSibling();
                    }
                }
            }

            m_SelectedContainer = container;
            m_Highlight.AttachToContainer(m_SelectedContainer);
        }

        public void ShowContainer(IContainer container)
        {
            if(container is MonoBehaviour behaviour)
            {
                behaviour.gameObject.SetActive(true);
                behaviour.transform.SetParent(canvasTransform);
                behaviour.transform.SetAsLastSibling();
            }
        }

        public void HideContainer(IContainer container)
        {
            if (container is MonoBehaviour behaviour)
            {
                if(container.GetShowingMode() == EShowingContainerMode.Closable)
                {
                    behaviour.gameObject.SetActive(false);
                }
            }
        }

        public bool CanDragContainer(IContainer container)
        {
            return m_SelectedItem == null;
        }

        private ItemData GetRandomItem()
        {
            int selectedItemId = UnityEngine.Random.Range(0, testItems.Count);
            return testItems[selectedItemId];
        }

        private void Update()
        {
            if(ContainerDragWindow.IsDragging)
            {
                return;
            }

            ItemDrag();
            HandleTooltip();

            if (m_SelectedContainer == null)
            {
                HandleDropItem();
                m_Highlight.Hide();
                return;
            }

            HandleHighlight();

            if (Input.GetMouseButtonDown(0))
            {
                OnTriggerMainFunction();
            }

            if (Input.GetMouseButtonDown(1))
            {
                OnTriggerSecondaryFunction();
            }

            if (Input.GetKeyDown(rotateKey))
            {
                RotateItem();
            }

            HandleDebug();
        }

        private void HandleDebug()
        {
            if (enableDebug)
            {
                if (Input.GetKeyDown(addItemKey))
                {
                    ItemData randomItem = GetRandomItem();
                    EContainerOp addResult = m_SelectedContainer.AddItem(randomItem, 1);
                    if (addResult == EContainerOp.ContainerIsFull)
                    {
                        Debug.Log("Out of space!");
                    }
                }

                if (Input.GetKeyDown(giveItemKey) && m_SelectedItem == null)
                {
                    Debug_CreateRandomItem();
                }

                if (Input.GetKeyDown(removeRandomKey))
                {
                    Debug_RemoveRandomItem();
                }
            }
        }

        private void RotateItem()
        {
            if(m_SelectedItem == null || m_SelectedItem.GetData().Size == Vector2Int.one)
            {
                return;
            }

            m_SelectedItem.Rotate();
        }

        private void HandleDropItem()
        {
            if(!Input.GetMouseButtonDown(0) || m_SelectedItem == null)
            {
                return;
            }

            if(m_SelectedItem.GetContainer() != null)
            {
                CollectItem dropItem = m_SelectedItem.GetContainer().DropItem(m_SelectedItem, true);

                m_SelectedItem = null;
                m_SelectedRect = null;
            }
        }

        private void HandleHighlight()
        {
            Vector2Int tileGridPos = GetTileGridPosition();
            if(m_LastHighlightPos == tileGridPos)
            {
                return;
            }

            m_LastHighlightPos = tileGridPos;
            if (m_SelectedItem == null)
            {
                if(!m_SelectedContainer.PositionInsideGrid(tileGridPos.x, tileGridPos.y, Vector2Int.one))
                {
                    m_Highlight.Hide();
                    return;
                }

                IContainerItem itemToHighlight = m_SelectedContainer.GetItem(tileGridPos.x, tileGridPos.y);
                if (itemToHighlight != null)
                {
                    m_Highlight.SetSize(itemToHighlight, m_SelectedContainer);
                    m_Highlight.SetPosition(itemToHighlight, m_SelectedContainer);
                    m_Highlight.Show(itemToHighlight);
                }
                else
                {
                    m_Highlight.Hide();
                }
            }
            else
            {
                m_SelectedItem.SetPreviewContainer(m_SelectedContainer);

                Vector2Int itemSize = m_SelectedItem.GetSize();
                if(!m_SelectedContainer.PositionInsideGrid(tileGridPos.x, tileGridPos.y, itemSize))
                {
                    m_Highlight.Hide(true);
                    return;
                }

                m_Highlight.SetSize(m_SelectedItem, m_SelectedContainer);
                m_Highlight.SetPosition(m_SelectedItem, m_SelectedContainer, tileGridPos.x, tileGridPos.y);

                bool canPlaceItem = (m_SelectedContainer.GetMode() != EContainerMode.ReadOnly
                    && m_SelectedContainer.GetMode() != EContainerMode.DragOnly)
                    || m_SelectedContainer == m_SelectedItem.GetContainer();

                if (!canPlaceItem)
                {
                    m_Highlight.Hide(true);
                    return;
                }

                if(!m_SelectedContainer.HasPermissionToPlace(m_SelectedItem))
                {
                    m_Highlight.Hide(true);
                    return;
                }

                if (!ValidateItemInsiderContainer(tileGridPos))
                {
                    m_Highlight.Hide(true);
                    return;
                }

                bool validOverlap = m_SelectedContainer.OverlapChecking(tileGridPos.x,
                    tileGridPos.y, itemSize, ref m_OverlapPreviewItem);

                if (validOverlap)
                {
                    m_Highlight.Show(m_SelectedItem);
                }
                else if(!validOverlap && m_OverlapPreviewItem == null)
                {
                    m_Highlight.Hide(true);
                }

                m_OverlapPreviewItem = null;
            }
        }

        private void HandleTooltip()
        {
            if(m_SelectedContainer == null)
            {
                CloseTooltip();
                return;
            }

            if(m_SelectedItem != null)
            {
                CloseTooltip();
            }
            else
            {
                Vector2Int tileGridPos = GetTileGridPosition();
                if (!m_SelectedContainer.PositionInsideGrid(tileGridPos.x, tileGridPos.y, Vector2Int.one))
                {
                    CloseTooltip();
                    return;
                }

                IContainerItem highlightItem = m_SelectedContainer.GetItem(tileGridPos.x, tileGridPos.y);

                if(highlightItem == null)
                {
                    CloseTooltip();
                    return;
                }

                if(!Tooltip.IsShowing() || m_TooltipInItem != highlightItem)
                {
                    OpenTooltip(highlightItem);
                }
            }
        }

        private void OpenTooltip(IContainerItem forItem)
        {
            Tooltip.Show(ItemDatabase.Instance.GetTooltipForItem(forItem));
            m_TooltipInItem = forItem;
        }

        private void CloseTooltip()
        {
            Tooltip.Hide();
            m_TooltipInItem = null;
        }

        private void ItemDrag()
        {
            if (m_SelectedItem != null)
            {
                m_SelectedRect.position = Input.mousePosition;
            }
        }

        private void OnTriggerMainFunction()
        {
            Vector2Int tileGridPos = GetTileGridPosition();

            if(m_SelectedContainer.GetMode() == EContainerMode.ReadOnly)
            {
                return;
            }

            if (m_SelectedItem == null)
            {
                if(m_SelectedContainer.GetMode() != EContainerMode.DropOnly)
                {
                    if(Input.GetKey(specialKey))
                    {
                        PickUpSplittedItem(tileGridPos);
                    }
                    else
                    {
                        PickUpItem(tileGridPos);
                    }
                }
            }
            else
            {
                bool canPlaceItem = m_SelectedContainer.GetMode() != EContainerMode.DragOnly ||
                    m_SelectedContainer == m_SelectedItem.GetContainer();

                if(canPlaceItem)
                {
                    PlaceItem(tileGridPos);
                }
            }
        }

        private void OnTriggerSecondaryFunction()
        {
            if(m_SelectedItem != null)
            {
                return;
            }

            if(m_SelectedContainer.GetMode() != EContainerMode.FullControl)
            {
                return;
            }

            Vector2Int tileGridPos = GetTileGridPosition();
            IContainerItem targetUseItem = m_SelectedContainer.GetItem(tileGridPos.x, tileGridPos.y);
            if(targetUseItem != null && targetUseItem.GetData() is InsiderContainerItemData)
            {
                targetUseItem.UseInsiderContainer(m_SelectedContainer.GetOwner());
                return;
            }

            if (targetUseItem == null || !targetUseItem.GetData().Usable)
            {
                return;
            }

            (EContainerOp removeOp, int removedAmount) = m_SelectedContainer
                .RemoveFromSlot(tileGridPos.x, tileGridPos.y, 1);

            if(removeOp == EContainerOp.Success && removedAmount == 1)
            {
                OpenTooltip(targetUseItem);
                targetUseItem.UseItem(m_SelectedContainer.GetOwner());
            }
        }

        private Vector2Int GetTileGridPosition()
        {
            Vector2 pointerPosition = Input.mousePosition;
            if (m_SelectedItem != null)
            {
                Vector2Int itemSize = m_SelectedItem.GetSize();
                Vector2Int tileSize = m_SelectedContainer.GetTileSize();

                pointerPosition.x -= (itemSize.x - 1) * tileSize.x / 2;
                pointerPosition.y += (itemSize.y - 1) * tileSize.y / 2;
            }

            return m_SelectedContainer.GetTileGridPosition(pointerPosition);
        }

        private void PlaceItem(Vector2Int tileGridPos)
        {
            if (!ValidateItemInsiderContainer(tileGridPos))
            {
                return;
            }

            EContainerOp placeOperation = m_SelectedContainer.PlaceItem(m_SelectedItem, tileGridPos.x,
                tileGridPos.y, ref m_OverlapItem);

            if(placeOperation == EContainerOp.Success)
            {
                if(m_SelectedItem.GetContainer() != m_SelectedContainer)
                {
                    m_SelectedItem.SetContainer(m_SelectedContainer);
                }

                m_SelectedItem = null;

                if(m_OverlapItem != null)
                {
                    m_SelectedItem = m_OverlapItem;
                    m_OverlapItem = null;
                    m_SelectedRect = m_SelectedItem.GetRect();
                    m_SelectedRect.SetParent(canvasTransform);
                    m_SelectedRect.SetAsLastSibling();
                }
            }
        }

        private void PickUpItem(Vector2Int tileGridPos)
        {
            if (!ValidateItemInsiderContainer(tileGridPos))
            {
                return;
            }

            m_SelectedItem = m_SelectedContainer.PickUpItem(tileGridPos.x, tileGridPos.y);
            if (m_SelectedItem != null)
            {
                m_SelectedRect = m_SelectedItem.GetRect();
                m_SelectedRect.SetParent(canvasTransform);
                m_SelectedRect.SetAsLastSibling();
            }
        }

        private void PickUpSplittedItem(Vector2Int tileGridPos)
        {
            if(!ValidateItemInsiderContainer(tileGridPos))
            {
                return;
            }

            IContainerItem peekItem = m_SelectedContainer.GetItem(tileGridPos.x, tileGridPos.y);
            if (peekItem.GetAmount() == 1)
            {
                PickUpItem(tileGridPos);
                return;
            }

            int splittedAmount = Mathf.FloorToInt(peekItem.GetAmount() / 2);
            IContainerItem splitItem = ItemDatabase.Instance.CreateContainerItem(peekItem.GetData(),
                m_SelectedContainer, canvasTransform);

            peekItem.SetAmount(peekItem.GetAmount() - splittedAmount);
            splitItem.SetAmount(splittedAmount);

            m_SelectedItem = splitItem;
            m_SelectedRect = splitItem.GetRect();
            m_SelectedRect.SetParent(canvasTransform);
            m_SelectedRect.SetAsLastSibling();
        }

        private bool ValidateItemInsiderContainer(Vector2Int tileGridPos)
        {
            if(m_SelectedContainer == null)
            {
                return false;
            }

            IContainerItem peekItem = m_SelectedContainer.GetItem(tileGridPos.x, tileGridPos.y);
            if (peekItem != null && peekItem.IsShowingInsiderContainer())
            {
                return false;
            }

            return true;
        }

        private void Debug_CreateRandomItem()
        {
            if (!enableDebug)
            {
                return;
            }

            ItemData randomItem = ItemDatabase.Instance.GetRandomItem();
            IContainerItem item = ItemDatabase.Instance.CreateContainerItem(
                randomItem, m_SelectedContainer, canvasTransform);

            m_SelectedItem = item;
            m_SelectedRect = item.GetRect();
        }

        private void Debug_RemoveRandomItem()
        {
            if(!enableDebug || m_SelectedContainer == null)
            {
                return;
            }

            ItemData randomItem = ItemDatabase.Instance.GetRandomItem();
            m_SelectedContainer.RemoveItem(randomItem, 1);
        }
    }
}
