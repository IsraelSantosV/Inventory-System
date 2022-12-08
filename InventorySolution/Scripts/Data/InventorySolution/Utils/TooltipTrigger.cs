using RWS.Data.InventorySolution.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RWS.Data.InventorySolution.Utils
{
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, TextArea(3,3)] private string messageText;
        [SerializeField] private float delayToShow = 1f;

        private float m_DelayToShow;
        private bool m_PointerStayHere;
        private bool m_ShowingTooltip;

        private void Update()
        {
            if(m_PointerStayHere)
            {
                if(!m_ShowingTooltip)
                {
                    m_DelayToShow += Time.deltaTime;
                    if (m_DelayToShow >= delayToShow)
                    {
                        Tooltip.Show(messageText);
                        m_ShowingTooltip = true;
                    }
                }
            }
            else
            {
                m_DelayToShow = 0;
                if(m_ShowingTooltip)
                {
                    m_ShowingTooltip = false;
                    Tooltip.Hide();
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            m_PointerStayHere = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            m_PointerStayHere = false;
        }
    }
}
