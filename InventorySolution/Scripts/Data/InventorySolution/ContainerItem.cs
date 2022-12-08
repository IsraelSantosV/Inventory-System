using RWS.Data.InventorySolution.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RWS.Data.InventorySolution
{
    public class ContainerItem : MonoBehaviour, IContainerItem
    {
        private const float ROTATE_ANGLE = 90;

        private ItemData m_Item;
        private int m_Amount;
        private Vector2Int m_GridPosition;
        private bool m_InHorizontal = true;
        private IContainer m_Container;

        public Vector2Int GetSize()
        {
            Vector2Int originalSize = GetData().Size;
            if(InHorizontalOrientation())
            {
                return new Vector2Int(originalSize.x, originalSize.y);
            }

            return new Vector2Int(originalSize.y, originalSize.x);
        }

        private RectTransform m_ItemRect;
        public RectTransform ItemRect
        {
            get
            {
                if(m_ItemRect == null)
                {
                    m_ItemRect = GetComponent<RectTransform>();
                }

                return m_ItemRect;
            }
        }

        private Image m_ItemImage;
        public Image ItemImage
        {
            get
            {
                if (m_ItemImage == null)
                {
                    m_ItemImage = GetComponent<Image>();
                }

                return m_ItemImage;
            }
        }

        private TMP_Text m_AmountText;
        public TMP_Text AmountText
        {
            get
            {
                if (m_AmountText == null)
                {
                    m_AmountText = GetComponentInChildren<TMP_Text>();
                }

                return m_AmountText;
            }
        }

        public RectTransform GetRect()
        {
            return ItemRect;
        }

        public Image GetImage()
        {
            return ItemImage;
        }

        public ItemData GetData()
        {
            return m_Item;
        }

        public int GetAmount()
        {
            return m_Amount;
        }

        public int GetAvailableStack()
        {
            if(GetData() == null)
            {
                return int.MaxValue;
            }

            return GetData().MaxStack - GetAmount();
        }

        public Vector2Int GetGridPosition()
        {
            return m_GridPosition;
        }

        public void SetGridPosition(Vector2Int position)
        {
            m_GridPosition = position;
        }

        public IContainer GetContainer()
        {
            return m_Container;
        }

        public float GetWeight()
        {
            return GetData().Weight * GetAmount();
        }

        private void Awake()
        {
            GetComponentsInChildren<Image>().ToList().ForEach(image =>
            {
                image.raycastTarget = false;
            });

            GetComponentsInChildren<TMP_Text>().ToList().ForEach(text =>
            {
                text.raycastTarget = false;
            });
        }

        public void PutItem(ItemData item, IContainer fromContainer)
        {
            m_Item = item;
            ItemImage.sprite = item.Texture;
            SetContainer(fromContainer);
            RefreshAmount();
            RefreshSize(fromContainer);
        }

        public void SetAmount(int amount)
        {
            m_Amount = Mathf.Max(0, amount);
            RefreshAmount();
        }

        public void SetContainer(IContainer container)
        {
            m_Container = container;
            RefreshSize(m_Container);
        }

        public void SetPreviewContainer(IContainer preview)
        {
            if(preview == null)
            {
                RefreshSize(m_Container);
            }
            else
            {
                RefreshSize(preview);
            }
        }

        public bool InHorizontalOrientation()
        {
            return m_InHorizontal;
        }

        public void Rotate()
        {
            m_InHorizontal = !m_InHorizontal;

            float angle = m_InHorizontal ? 0 : ROTATE_ANGLE;
            ItemRect.rotation = Quaternion.Euler(0, 0, angle);
        }

        public void RefreshAmount()
        {
            if (AmountText != null)
            {
                AmountText.text = m_Amount.ToString();
            }
        }

        private void RefreshSize(IContainer container)
        {
            Vector2Int tileSize = container.GetTileSize();
            Vector2Int itemMaxSize = container.GetItemSize();

            Vector2 itemSize = new()
            {
                x = GetData().Size.x * Mathf.Min(tileSize.x, itemMaxSize.x),
                y = GetData().Size.y * Mathf.Min(tileSize.y, itemMaxSize.y)
            };

            ItemRect.sizeDelta = itemSize;
        }

        public CollectItem GetCollectable()
        {
            return new()
            {
                Item = GetData(),
                Count = GetAmount()
            };
        }

        public void UseItem(ICharacterContainerHandler owner)
        {
            if(!GetData().Usable)
            {
                return;
            }

            GetData().OnUse();

            if(owner != null)
            {
                owner.OnUseItem(this);
            }
        }
    }
}
