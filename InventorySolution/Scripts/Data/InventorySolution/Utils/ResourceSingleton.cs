using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RWS.Data.InventorySolution.Utils
{
    public abstract class ResourceSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        private static T m_Instance = null;
        public static T Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    T[] assets = Resources.LoadAll<T>("");
                    if (assets == null || assets.Length == 0)
                    {
                        throw new Exception("Could not find any singleton" +
                            "SO object instance in the resources");
                    }
                    else if (assets.Length > 1)
                    {
                        Debug.LogWarning("Multiple instances of the singleton SO" +
                            "object found in the resources");
                    }

                    m_Instance = assets[0];
                }

                return m_Instance;
            }
        }
    }
}
