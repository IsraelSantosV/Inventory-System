using RWS.Data.InventorySolution.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RWS.Data.InventorySolution.Utils
{
    public class ContainerDragWindow : MonoBehaviour, IDragHandler,
        IBeginDragHandler, IEndDragHandler, IPointerDownHandler
    {
        public static bool IsDragging { get; private set; } = false;

        [SerializeField] private RectTransform dragTransform;
        [SerializeField] private GameObject containerObject;

        private RectTransform m_CanvasRect;
        private IContainer m_Container;

        private Vector2 m_PointerOffset;
        private bool m_ClampedToLeft;
        private bool m_ClampedToRight;
        private bool m_ClampedToTop;
        private bool m_ClampedToBottom;

        private void Start()
        {
            Transform canvasTransform = ContainerController.Instance.GetCanvasTransform();
            m_CanvasRect = canvasTransform as RectTransform;

            m_Container = containerObject.GetComponent<IContainer>();

            m_ClampedToLeft = false;
            m_ClampedToRight = false;
            m_ClampedToTop = false;
            m_ClampedToBottom = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(dragTransform,
                eventData.position, eventData.pressEventCamera, out m_PointerOffset);

            IsDragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragTransform == null || !ContainerController.Instance.CanDragContainer(m_Container))
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_CanvasRect, 
                eventData.position, eventData.pressEventCamera, out Vector2 localPointerPosition))
            {
                dragTransform.localPosition = localPointerPosition - m_PointerOffset;
                ClampToWindow();
                Vector2 clampedPosition = dragTransform.localPosition;

                if (m_ClampedToRight)
                {
                    clampedPosition.x = (m_CanvasRect.rect.width * 0.5f) - (dragTransform.rect.width * (1 - dragTransform.pivot.x));
                }
                else if (m_ClampedToLeft)
                {
                    clampedPosition.x = (-m_CanvasRect.rect.width * 0.5f) + (dragTransform.rect.width * dragTransform.pivot.x);
                }

                if (m_ClampedToTop)
                {
                    clampedPosition.y = (m_CanvasRect.rect.height * 0.5f) - (dragTransform.rect.height * (1 - dragTransform.pivot.y));
                }
                else if (m_ClampedToBottom)
                {
                    clampedPosition.y = (-m_CanvasRect.rect.height * 0.5f) + (dragTransform.rect.height * dragTransform.pivot.y);
                }

                dragTransform.localPosition = clampedPosition;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            IsDragging = false;
        }

        private void ClampToWindow()
        {
            Vector3[] canvasCorners = new Vector3[4];
            Vector3[] panelRectCorners = new Vector3[4];
            m_CanvasRect.GetWorldCorners(canvasCorners);
            dragTransform.GetWorldCorners(panelRectCorners);

            if (panelRectCorners[2].x > canvasCorners[2].x)
            {
                if (!m_ClampedToRight)
                {
                    m_ClampedToRight = true;
                }
            }
            else if (m_ClampedToRight)
            {
                m_ClampedToRight = false;
            }
            else if (panelRectCorners[0].x < canvasCorners[0].x)
            {
                if (!m_ClampedToLeft)
                {
                    m_ClampedToLeft = true;
                }
            }
            else if (m_ClampedToLeft)
            {
                m_ClampedToLeft = false;
            }

            if (panelRectCorners[2].y > canvasCorners[2].y)
            {
                if (!m_ClampedToTop)
                {
                    m_ClampedToTop = true;
                }
            }
            else if (m_ClampedToTop)
            {
                m_ClampedToTop = false;
            }
            else if (panelRectCorners[0].y < canvasCorners[0].y)
            {
                if (!m_ClampedToBottom)
                {
                    m_ClampedToBottom = true;
                }
            }
            else if (m_ClampedToBottom)
            {
                m_ClampedToBottom = false;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            dragTransform.SetAsLastSibling();
        }
    }
}
