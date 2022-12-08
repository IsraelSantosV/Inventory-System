using RWS.Data.InventorySolution.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace RWS.Data.InventorySolution.Utils
{
    [RequireComponent(typeof(IContainer))]
    public class ContainerWeightText : MonoBehaviour
    {
        [SerializeField] private TMP_Text weightText;
        [SerializeField] private string format = "00";
        [SerializeField] private string divisor = "/";
        [SerializeField] private bool showMaxValue = true;

        private IContainer m_Container;

        private void Start()
        {
            UpdateWeigth();
        }

        private void OnEnable()
        {
            m_Container = GetComponent<IContainer>();
            m_Container.OnUpdateContainer += UpdateWeigth;
        }

        private void OnDisable()
        {
            m_Container.OnUpdateContainer -= UpdateWeigth;
        }

        private void UpdateWeigth()
        {
            if(weightText == null)
            {
                return;
            }

            string weight = m_Container.GetContainerWeight().ToString(format);
            if(showMaxValue)
            {
                weight += divisor + m_Container.GetMaxWeight();
            }
            weightText.text = weight;
        }
    }
}
