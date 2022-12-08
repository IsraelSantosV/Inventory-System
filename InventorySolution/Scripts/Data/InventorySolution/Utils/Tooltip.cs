using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RWS.Data.InventorySolution.Utils
{
    public class Tooltip : MonoBehaviour
    {
        private static Tooltip m_Instance;
        public static Tooltip Instance
        {
            get
            {
                if(m_Instance == null)
                {
                    m_Instance = FindObjectOfType<Tooltip>();
                }

                return m_Instance;
            }
        }

        [SerializeField] private RectTransform background;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Vector2 paddingSize = new(4, 4);

        private RectTransform m_Rect;
        private RectTransform MyRect
        {
            get
            {
                if(m_Rect == null)
                {
                    m_Rect = GetComponent<RectTransform>();
                }

                return m_Rect;
            }
        }

        private RectTransform m_CanvasRect;
        private RectTransform CanvasRect
        {
            get
            {
                if (m_CanvasRect == null)
                {
                    m_CanvasRect = transform.root.GetComponent<RectTransform>();
                }

                return m_CanvasRect;
            }
        }

        private void Awake()
        {
            CreateTooltip();
            SetText("");
            Hide();
        }

        private void Update()
        {
            Vector2 anchoredPos = Input.mousePosition / CanvasRect.localScale.x;
            Vector2 backgroundSize = GetBackgroundSize();

            if(anchoredPos.x + backgroundSize.x > CanvasRect.rect.width)
            {
                anchoredPos.x = CanvasRect.rect.width - backgroundSize.x;
            }

            if (anchoredPos.y + backgroundSize.y > CanvasRect.rect.height)
            {
                anchoredPos.y = CanvasRect.rect.height - backgroundSize.y;
            }

            MyRect.anchoredPosition = anchoredPos;
        }

        private void CreateTooltip()
        {
            MyRect.anchorMin = Vector2.zero;
            MyRect.anchorMax = Vector2.zero;
            MyRect.pivot = Vector2.zero;

            MyRect.offsetMin = Vector2.zero;
            MyRect.offsetMax = Vector2.zero;
            MyRect.sizeDelta = new Vector2(0, 50);

            if (MyRect.TryGetComponent(out Image rectImage))
            {
                rectImage.raycastTarget = false;
            }

            if (background != null)
            {
                background.anchorMin = Vector2.zero;
                background.anchorMax = Vector2.zero;

                background.offsetMin = Vector2.zero;
                background.offsetMax = Vector2.zero;
                background.sizeDelta = new Vector2(300, 70);

                if(background.TryGetComponent(out Image image))
                {
                    image.raycastTarget = false;
                }
            }
            
            if(messageText != null)
            {
                messageText.alignment = TextAlignmentOptions.BottomLeft;
                messageText.enableWordWrapping = false;
                messageText.raycastTarget = false;
            }
        }

        private void SetText(string tooltipText)
        {
            messageText.SetText(tooltipText);
            messageText.ForceMeshUpdate();

            Vector2 textSize = messageText.GetRenderedValues(false);
            background.sizeDelta = textSize + paddingSize;
        }

        private Vector2 GetBackgroundSize()
        {
            return new Vector2(background.rect.width, background.rect.height);
        }

        public static bool IsShowing()
        {
            return Instance.gameObject.activeInHierarchy;
        }

        public static void Show(string tooltipText)
        {
            Instance.gameObject.SetActive(true);
            Instance.SetText(tooltipText);
        }
        
        public static void Hide()
        {
            Instance.gameObject.SetActive(false);
        }
    }
}
