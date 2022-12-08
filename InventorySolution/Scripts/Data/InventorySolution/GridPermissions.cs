using RWS.Data.InventorySolution.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RWS.Data.InventorySolution
{
    [RequireComponent(typeof(IContainer))]
    public class GridPermissions : MonoBehaviour
    {
        [SerializeField] private Transform permissionHandler;

        private List<IPermission> permissions = new();
        private IContainer m_Container;

        private void Awake()
        {
            m_Container = GetComponent<IContainer>();

            Transform handler = permissionHandler == null ? transform : permissionHandler;
            permissions = new(handler.GetComponents<IPermission>());
        }

        public bool ValidatePermissions(IContainerItem source)
        {
            return permissions.TrueForAll(data => data.HasPermission(source));
        }

        public bool ValidatePermissions(ItemData source)
        {
            return permissions.TrueForAll(data => data.HasPermission(source));
        }
    }
}
