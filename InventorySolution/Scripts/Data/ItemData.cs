using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RWS.Data.InventorySolution.Definitions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RWS.Data
{

#if UNITY_EDITOR
    public class ScriptableObjectIdAttribute : PropertyAttribute { }

    [CustomPropertyDrawer(typeof(ScriptableObjectIdAttribute))]
    public class ScriptableObjectIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            if (string.IsNullOrEmpty(property.stringValue))
            {
                property.stringValue = Guid.NewGuid().ToString();
            }
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
#endif

    [CreateAssetMenu(fileName = "New Item", menuName = "RWSEngine/Database/Item")]
    public class ItemData : ScriptableObject, IComparable<ItemData>
    {
        [SerializeField, ScriptableObjectId] private string id;
        [SerializeField] private string itemName;
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite texture;
        [SerializeField] private float weight;
        [SerializeField] private Vector2Int size = Vector2Int.one;
        [SerializeField] private int maxStack = 1;
        [SerializeField] private bool usable = false;
        [SerializeField] private EItemCategory category;
        [SerializeField] private ERarity rarity;

        public string Id => id;
        public string ItemName => itemName;
        public Sprite Icon => icon;
        public Sprite Texture => texture;
        public float Weight => weight;
        public Vector2Int Size => size;
        public int MaxStack => maxStack;
        public bool Usable => usable;
        public EItemCategory Category => category;
        public ERarity Rarity => rarity;

        public int CompareTo(ItemData other)
        {
            if(Rarity > other.Rarity)
            {
                return 1;
            }
            
            if(Rarity < other.Rarity)
            {
                return -1;
            }

            return 0;
        }

        public virtual void OnUse() { }
    }
}
