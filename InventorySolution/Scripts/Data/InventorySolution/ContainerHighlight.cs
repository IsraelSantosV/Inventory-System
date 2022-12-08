using RWS.Data.InventorySolution.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RWS.Data.InventorySolution
{
    public class ContainerHighlight : MonoBehaviour
    {
        [SerializeField] private RectTransform highlighter;
        [SerializeField] private bool disableOnHide = true;
        [SerializeField] private Color colorOnShow = Color.white;
        [SerializeField] private Color colorOnError = Color.white;

        private Image m_HighlighterImage;
        private IContainerItem m_HighlightItem;

        private void Awake()
        {
            if(highlighter.TryGetComponent(out m_HighlighterImage))
            {
                m_HighlighterImage.raycastTarget = false;
            }

            highlighter.anchorMin = Vector2.zero;
            highlighter.anchorMax = Vector2.zero;
            highlighter.pivot = new Vector2(.5f, .5f);

            Hide();
        }

        public void Show(IContainerItem item)
        {
            highlighter.gameObject.SetActive(true);
            m_HighlighterImage.color = colorOnShow;
            m_HighlightItem = item;
        }

        public void Hide(bool setErrorColor = false)
        {
            if(disableOnHide && !setErrorColor)
            {
                highlighter.gameObject.SetActive(false);
            }
            else
            {
                highlighter.gameObject.SetActive(true);
            }

            if(setErrorColor)
            {
                m_HighlighterImage.color = colorOnError;
            }
            else
            {
                m_HighlighterImage.color = colorOnShow;
            }
        }

        public bool IsShowing()
        {
            return highlighter.gameObject.activeInHierarchy;
        }

        public void SetSize(IContainerItem item, IContainer fromContainer)
        {
            Vector2Int tileSize = fromContainer.GetTileSize();
            Vector2Int itemSize = item.GetSize();

            Vector2 size = new()
            {
                x = itemSize.x * tileSize.x,
                y = itemSize.y * tileSize.y
            };

            highlighter.sizeDelta = size;
        }

        public void SetPosition(IContainerItem targetItem, IContainer fromContainer)
        {
            Vector2Int gridPosition = targetItem.GetGridPosition();
            Vector2Int itemSize = targetItem.GetSize();

            Vector2 targetPosition = fromContainer.CalculatePositionOnGrid(gridPosition.x, gridPosition.y, itemSize);
            highlighter.localPosition = targetPosition;
        }

        public void SetPosition(IContainerItem targetItem, IContainer fromContainer, int x, int y)
        {
            Vector3 position = fromContainer.CalculatePositionOnGrid(x, y, targetItem.GetSize());
            highlighter.localPosition = position;
        }

        public void AttachToContainer(IContainer container)
        {
            if(container == null)
            {
                return;
            }

            highlighter.SetParent(container.GetRect());
        }
    }
}
