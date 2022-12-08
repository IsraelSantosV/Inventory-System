using RWS.Data.InventorySolution.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RWS.Data.InventorySolution.Utils
{
    [RequireComponent(typeof(Button))]
    public class ContainerCloseButton : MonoBehaviour
    {
        [SerializeField] private GameObject container;

        private IContainer m_Container;
        private Button m_Button;
        
        private void Start()
        {
            m_Button = GetComponent<Button>();
            m_Button.onClick.AddListener(OnClickButton);

            m_Container = container.GetComponent<IContainer>();
            if(m_Container.GetShowingMode() != EShowingContainerMode.Closable)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        private void OnClickButton()
        {
            ContainerController.Instance.HideContainer(m_Container);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
