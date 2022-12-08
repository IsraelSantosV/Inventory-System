using System.Collections;
using System.Collections.Generic;
using RWS.Data;
using System;

namespace RWS.Core {

    [Serializable]
    public class ItemData {

        [SerializeField] private Item m_Item;
        [SerializeField, Range(0, 999)] private int m_Amount;

        public Item MyItem => m_Item;
        public int MyAmount => m_Amount;

        public ItemData(Item item, int amount) {
            m_Item = item;
            m_Amount = amount;
        }

        public void ChangeAmount(int newAmount) {
            m_Amount = newAmount;
        }

        public void ChangeItem(Item newItem) {
            m_Item = newItem;
        }
    }

    public class Inventory {

        public enum ActionResult { Success, NullItem, InventoryFull, ExceededWeight }
        private const int NONE = -1;

        private ItemData[,] m_Container;
        private int m_Width;
        private int m_Height;

        private float m_WeightLimit;
        private float m_Weight;

        private List<ItemData> m_Items;

        public float GetCurrentWeight() => m_Weight;
        public int GetWidth() => m_Width;
        public int GetHeight() => m_Height;

        public Action<int, int> OnRefreshSlot;
        public Action<float> OnRefreshWeight;

        public Inventory(int width, int height, float weightLimit, ItemData[] startItems = null) {
            m_Width = width;
            m_Height = height;
            m_WeightLimit = weightLimit;
            m_Items = new List<ItemData>();
            m_Container = new ItemData[m_Width, m_Height];
            m_Weight = 0;
            ClearContainer();

            if(startItems != null && startItems.Length > 0) {
                foreach(var item in startItems) {
                    AddItem(item.MyItem, item.MyAmount);
                }
            }
        }

        /// <summary>
        /// Add a specific quantity of an item to inventory with the following specifications: 
        /// If the item can be stacked and a valid stack exists (smaller than the maximum size): adds the item to that stack. 
        /// If the item cannot be stacked: look for a valid slot to add it
        /// </summary>
        /// <param name="item"></param>
        /// <param name="amount"></param>
        /// <returns>Returns the result of the operation</returns>
        public ActionResult AddItem(Item item, int amount) {
            if (item == null) {
                return ActionResult.NullItem;
            }

            if(m_Weight + (item.GetWeight() * amount) > m_WeightLimit) {
                return ActionResult.ExceededWeight;
            }

            if (item.GetMaxStack() > 1) {
                (int, int) validSlot = GetValidItemToStack(item);
                if(!IsInvalidPosition(validSlot)) {
                    var target = m_Container[validSlot.Item1, validSlot.Item2];

                    if (target.MyAmount + amount > item.GetMaxStack()) {
                        var currentAmount = target.MyAmount;
                        var rest = (target.MyAmount + amount) - item.GetMaxStack();
                        target.ChangeAmount(item.GetMaxStack());

                        //Setup correctly weight
                        m_Weight += (item.GetMaxStack() - currentAmount) * item.GetWeight();
                        AddItemInList(item, item.GetMaxStack() - currentAmount);

                        OnRefreshSlot?.Invoke(validSlot.Item1, validSlot.Item2);
                        OnRefreshWeight?.Invoke(m_Weight);
                        return AddItem(item, rest);
                    }

                    m_Weight += amount * item.GetWeight();
                    target.ChangeAmount(target.MyAmount + amount);
                    AddItemInList(item, amount);

                    OnRefreshWeight?.Invoke(m_Weight);
                    OnRefreshSlot?.Invoke(validSlot.Item1, validSlot.Item2);
                    return ActionResult.Success;
                }
            }

            var slot = GetEmptySlot();
            if (IsInvalidPosition(slot)) {
                return ActionResult.InventoryFull;
            }

            m_Container[slot.Item1, slot.Item2] = new ItemData(item, amount);
            m_Weight += amount * item.GetWeight();
            AddItemInList(item, amount);

            OnRefreshWeight?.Invoke(m_Weight);
            OnRefreshSlot?.Invoke(slot.Item1, slot.Item2);
            return ActionResult.Success;
        }

        /// <summary>
        /// Removes the first occurrence of the item in which it has a quantity 
        /// greater than 1. By iteration, the next occurrences are removed 
        /// if there are still values to remove.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public ActionResult RemoveItem(Item item, int amount) {
            var currentData = GetItemData(item.GetName(), out var result, out var index);
            if(result != ActionResult.Success || IsInvalidPosition(index)) {
                return ActionResult.NullItem;
            }

            if(currentData.MyAmount < amount) {
                var takeAmount = currentData.MyAmount;
                var rest = amount - takeAmount;

                m_Weight -= item.GetWeight() * takeAmount;
                RemoveItemInList(item, takeAmount);
                m_Container[index.Item1, index.Item2] = new ItemData(null, 0);

                OnRefreshWeight?.Invoke(m_Weight);
                OnRefreshSlot?.Invoke(index.Item1, index.Item2);
                return RemoveItem(item, rest);
            }

            RemoveItemInList(item, amount);
            m_Weight -= item.GetWeight() * amount;
            currentData.ChangeAmount(currentData.MyAmount - amount);
            if(currentData.MyAmount <= 0) {
                m_Container[index.Item1, index.Item2] = new ItemData(null, 0);
            }

            OnRefreshWeight?.Invoke(m_Weight);
            OnRefreshSlot?.Invoke(index.Item1, index.Item2);
            return ActionResult.Success;
        }

        /// <summary>
        /// Remove the item in the highlighted position in x and y
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public ActionResult RemoveFromPosition(int x, int y, int amount) {
            if (IsInvalidPosition((x, y))) {
                return ActionResult.NullItem;
            }

            var slot = m_Container[x, y];
            if (slot.MyItem == null || slot.MyAmount <= 0) {
                return ActionResult.NullItem;
            }

            var validAmount = slot.MyAmount < amount ? slot.MyAmount : amount;
            slot.ChangeAmount(slot.MyAmount - validAmount);
            
            RemoveItemInList(slot.MyItem, validAmount);
            m_Weight -= slot.MyItem.GetWeight() * validAmount;
            if (slot.MyAmount <= 0) {
                m_Container[x, y] = new ItemData(null, 0);
            }

            OnRefreshWeight?.Invoke(m_Weight);
            OnRefreshSlot?.Invoke(x, y);
            return ActionResult.Success;
        }

        private (int, int) GetEmptySlot() {
            for(int x = 0; x < m_Width; x++) {
                for(int y = 0; y < m_Height; y++) {
                    var slot = m_Container[x, y];
                    if (slot.MyItem == null) {
                        return (x, y);
                    }
                }
            }

            return (NONE, NONE);
        }

        private (int, int) GetValidItemToStack(Item item) {
            for (int x = 0; x < m_Width; x++) {
                for (int y = 0; y < m_Height; y++) {
                    var slot = m_Container[x, y];
                    if (slot.MyItem == item && slot.MyAmount < item.GetMaxStack()) {
                        return (x, y);
                    }
                }
            }

            return (NONE, NONE);
        }

        public int GetItemAmountRemaining(Item item) {
            foreach(var it in m_Items) {
                if(it.MyItem != null && it.MyItem == item) {
                    return it.MyAmount;
                }
            }

            return 0;
        }

        private ItemData GetItemData(string name, out ActionResult result, out (int,int) index) {
            for (int x = 0; x < m_Width; x++) {
                for (int y = 0; y < m_Height; y++) {
                    var slot = m_Container[x, y];
                    if(slot.MyItem != null && slot.MyItem.GetName() == name) {
                        result = ActionResult.Success;
                        index = (x, y);
                        return slot;
                    }
                }
            }

            result = ActionResult.NullItem;
            index = (NONE, NONE);
            return null;
        }

        public ItemData GetItemData(int m_X, int m_Y) {
            for (int x = 0; x < m_Width; x++) {
                if (m_X != x) continue;
                for (int y = 0; y < m_Height; y++) {
                    if (m_Y != y) continue;
                    return m_Container[x, y];
                }
            }

            return null;
        }

        public int GetCurrentSize() {
            int count = 0;
            for (int x = 0; x < m_Width; x++) {
                for (int y = 0; y < m_Height; y++) {
                    if(m_Container[x,y].MyItem != null) {
                        count++;
                    }
                }
            }

            return count;
        }

        public void ClearContainer() {
            for (int x = 0; x < m_Width; x++) {
                for (int y = 0; y < m_Height; y++) {
                    m_Container[x, y] = new ItemData(null, 0);
                }
            }
        }

        private int GetItemIndexInList(Item item) {
            for (int i = 0; i < m_Items.Count; i++) {
                ItemData it = m_Items[i];
                if (it.MyItem != null && it.MyItem == item) {
                    return i;
                }
            }

            return NONE;
        }

        private void AddItemInList(Item item, int amount) {
            var itemIndex = GetItemIndexInList(item);
            if (itemIndex != NONE) {
                var listItem = m_Items[itemIndex];
                listItem.ChangeAmount(listItem.MyAmount + amount);
            }
            else {
                var newItem = new ItemData(item, amount);
                m_Items.Add(newItem);
            }
        }

        private void RemoveItemInList(Item item, int amount) {
            var itemIndex = GetItemIndexInList(item);
            if (itemIndex != NONE) {
                var listItem = m_Items[itemIndex];
                listItem.ChangeAmount(listItem.MyAmount - amount);

                if(listItem.MyAmount <= 0) {
                    m_Items.RemoveAt(itemIndex);
                }
            }
        }

        /// <summary>
        /// Operation to find out if it is possible to add an item before it is added
        /// </summary>
        /// <param name="item"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public bool CanStackItem(Item item, int amount) {
            var currentItemAmount = GetItemAmountRemaining(item);
            var addResult = AddItem(item, amount);
            if (GetItemAmountRemaining(item) != currentItemAmount) {
                //Remove added items
                var removeAmount = GetItemAmountRemaining(item) - currentItemAmount;
                if(removeAmount > 0) {
                    RemoveItem(item, removeAmount);
                }
            }

            return addResult == ActionResult.Success;
        }

        private bool IsInvalidPosition((int, int) position) {
            return position == (NONE, NONE);
        }
    }
}
