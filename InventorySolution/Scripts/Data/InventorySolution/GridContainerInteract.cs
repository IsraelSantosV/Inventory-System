using RWS.Data.InventorySolution.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RWS.Data.InventorySolution
{
    [RequireComponent(typeof(IContainer))]
    public class GridContainerInteract : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private ContainerController m_Controller;
        private IContainer m_Container;

        private void Awake()
        {
            m_Controller = FindObjectOfType<ContainerController>();
            m_Container = GetComponent<IContainer>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            m_Controller.SelectContainer(m_Container);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            m_Controller.SelectContainer(null);
        }
    }
}
