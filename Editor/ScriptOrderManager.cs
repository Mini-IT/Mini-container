using System;
using UnityEditor;
using UnityEngine;

namespace MiniContainer.Editor
{
    [InitializeOnLoad]
    public class ScriptOrderManager
    {
        static ScriptOrderManager()
        {
            foreach (var monoScript in MonoImporter.GetAllRuntimeMonoScripts())
            {
                if (monoScript.GetClass() == null) continue;
                foreach (var a in Attribute.GetCustomAttributes(monoScript.GetClass(), typeof(DefaultExecutionOrder)))
                {
                    var currentOrder = MonoImporter.GetExecutionOrder(monoScript);
                    var newOrder = ((DefaultExecutionOrder)a).order;
                    if (currentOrder != newOrder)
                        MonoImporter.SetExecutionOrder(monoScript, newOrder);
                }
            }
        }
    }
}