using RWS.Data.InventorySolution.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;

namespace RWS.Data.InventorySolution
{
    public class ContainerItem : MonoBehaviour, IContainerItem
    {
        private const float ROTATE_ANGLE = 90;

        [SerializeField] private RectTransform inverseRotation;

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

        private IContainer m_InsiderContainer;
        public IContainer GetInsiderContainer()
        {
            if (m_InsiderContainer == null)
            {
                m_InsiderContainer = GetComponentInChildren<IContainer>();
            }

            return m_InsiderContainer;
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
            float totalWeight = 0;
            IContainer insiderContainer = GetInsiderContainer();
            if(insiderContainer != null)
            {
                totalWeight += insiderContainer.GetContainerWeight();
            }

            return totalWeight + (GetData().Weight * GetAmount());
        }

        private void Awake()
        {
            GetComponentsInChildren<Image>().ToList().ForEach(image =>
            {
                if(image.GetComponent<Button>() == null)
                {
                    image.raycastTarget = false;
                }
            });

            GetComponentsInChildren<TMP_Text>().ToList().ForEach(text =>
            {
                text.raycastTarget = false;
            });
        }

        private void Start()
        {
            IContainer insiderContainer = GetInsiderContainer();
            if (insiderContainer != null)
            {
                ContainerController.Instance.HideContainer(insiderContainer);
            }
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

            if(inverseRotation != null)
            {
                inverseRotation.rotation = Quaternion.Euler(Vector3.zero);
            }
        }

        public void RefreshAmount()
        {
            if (AmountText != null)
            {
                string amountText = GetAmount() == 1 ? "" : GetAmount().ToString();
                AmountText.text = amountText;
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


        public void DestroyInsideContainer()
        {
            if (GetInsiderContainer() != null)
            {
                IContainer insideContainer = GetInsiderContainer();
                insideContainer.ClearContainer();

                if (insideContainer is MonoBehaviour containerBehaviour)
                {
                    Destroy(containerBehaviour.gameObject);
                }
            }
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
            if (!GetData().Usable)
            {
                return;
            }

            GetData().OnUse();

            if(owner != null)
            {
                owner.OnUseItem(this);
            }
        }

        public void UseInsiderContainer(ICharacterContainerHandler owner)
        {
            bool enabled = false;
            IContainer insiderContainer = GetInsiderContainer();
            if (insiderContainer is MonoBehaviour behaviour)
            {
                enabled = behaviour.gameObject.activeInHierarchy;
            }

            if (enabled)
            {
                ContainerController.Instance.HideContainer(insiderContainer);
            }
            else
            {
                ContainerController.Instance.ShowContainer(insiderContainer);
            }
        }

        public bool IsShowingInsiderContainer()
        {
            IContainer insiderContainer = GetInsiderContainer();
            if(insiderContainer == null)
            {
                return false;
            }

            if(insiderContainer is MonoBehaviour behaviour)
            {
                return behaviour.gameObject.activeInHierarchy;
            }

            return false;
        }
    }
}
