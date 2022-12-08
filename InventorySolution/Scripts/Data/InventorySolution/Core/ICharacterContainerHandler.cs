using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RWS.Data.InventorySolution.Core
{
    public interface ICharacterContainerHandler
    {
        void OnUseItem(IContainerItem item);
        IContainer GetContainer(EContainerCategory byCategory);
        void AttachContainer(IContainer container, EContainerCategory category);
        void RemoveContainer(IContainer container);
        List<IContainer> GetAttachedContainers();
    }

    public enum EContainerCategory
    {
        Common,
        Equipment,
        Crafting,
        Keys
    }
}
