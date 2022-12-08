using RWS.Data.InventorySolution.Core;
using RWS.Data.InventorySolution.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RWS.Data.InventorySolution.Permissions
{
    public class PermissionItemCategory : MonoBehaviour, IPermission
    {
        [SerializeField] private List<EItemCategory> allowedCategories = new();

        public bool HasPermission(IContainerItem request)
        {
            if (request == null)
            {
                return false;
            }

            return HasPermission(request.GetData());
        }

        public bool HasPermission(ItemData request)
        {
            if (request == null)
            {
                return false;
            }

            return allowedCategories.Contains(request.Category);
        }
    }
}
