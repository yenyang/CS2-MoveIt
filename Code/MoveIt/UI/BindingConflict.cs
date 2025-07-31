// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

using Colossal.UI.Binding;

namespace MoveIt.UI
{
    public struct BindingConflict : IJsonWritable
    {
        public string mapNameLocaleKey;
        public string mapNameFallback;
        public string actionNameLocaleKey;
        public string actionNameFallback;

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(GetType().FullName);
            writer.PropertyName(nameof(mapNameLocaleKey));
            writer.Write(mapNameLocaleKey);
            writer.PropertyName(nameof(mapNameFallback));
            writer.Write(mapNameFallback);
            writer.PropertyName(nameof(actionNameLocaleKey));
            writer.Write(actionNameLocaleKey);
            writer.PropertyName(nameof(actionNameFallback));
            writer.TypeEnd();
        }
    }
}
