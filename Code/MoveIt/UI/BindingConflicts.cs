// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

using Colossal.UI.Binding;
using Game.Input;
using QCommonLib;
using System.Collections.Generic;

namespace MoveIt.UI
{
    public struct BindingConflicts : IJsonWritable
    {
        public List<BindingConflict> conflicts;

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(GetType().FullName);
            writer.PropertyName(nameof(conflicts));
            writer.ArrayBegin(conflicts.Count);
            foreach (BindingConflict bindingConflict in conflicts)
            {
                bindingConflict.Write(writer);
            }
            writer.ArrayEnd();
            writer.TypeEnd();
        }

        public void AddBindingConflict(ProxyBinding proxyBinding) 
        {
            BindingConflict conflict = new BindingConflict()
            {
                mapNameFallback = proxyBinding.mapName,
                mapNameLocaleKey = $"Options.INPUT_MAP[{proxyBinding.mapName}]",
                actionNameFallback = proxyBinding.actionName,
                actionNameLocaleKey = $"Options.OPTION[InputSettings.Gamepad.{proxyBinding.mapName}/{proxyBinding.actionName}/{InputManager.GetBindingName(proxyBinding.component)}]",           
            };
            
            if (!conflicts.Contains(conflict))
            {
                conflicts.Add(conflict);
                QLog.Debug($"Added {proxyBinding.mapName}:{proxyBinding.actionName}");
            }
        }

        public void Clear()
        {
            conflicts.Clear();
        }
    }
}
