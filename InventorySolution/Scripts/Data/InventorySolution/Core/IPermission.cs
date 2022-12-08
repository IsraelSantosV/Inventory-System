using System;
using System.Collections.Generic;
using System.Linq;

namespace RWS.Data.InventorySolution.Core
{
    public interface IPermission
    {
        bool HasPermission(IContainerItem request);
        bool HasPermission(ItemData request);
    }
}
