using RWS.Data.InventorySolution.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RWS.Data
{
    [CreateAssetMenu(fileName = "New InsiderContainer", menuName = "RWSEngine/Database/InsiderContainer")]
    public class InsiderContainerItemData : ItemData
    {
        [Header("BASIC FEATURE")]
        [SerializeField] private Vector2Int containerSize;
        [SerializeField] private EContainerMode containerMode = EContainerMode.FullControl;
        [SerializeField] private EContainerCategory containerCategory = EContainerCategory.Common;

        [Header("WEIGHT FEATURE")]
        [SerializeField] private bool useWeightFeature;
        [SerializeField] private float maxWeight;
        [SerializeField] private float exceedPermitedWeight;

        public Vector2Int ContainerSize => containerSize;
        public EContainerMode ContainerMode => containerMode;
        public EContainerCategory ContainerCategory => containerCategory;
        public bool UseWeightFeature => useWeightFeature;
        public float MaxWeight => maxWeight;
        public float ExceedPermitedWeight => exceedPermitedWeight;
    }

}
