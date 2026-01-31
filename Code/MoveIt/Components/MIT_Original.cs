// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

using Unity.Entities;

namespace MoveIt.Components
{
    /// <summary>
    /// A custom component for original entity that is being copied by Move It Tool. Meant to be added to Temp Entities.
    /// </summary>
    public struct MIT_Original : IComponentData, IQueryTypeParameter
    {
        // Entity reference for original instance entity being copied by Move It Tool.
        public Entity m_Original;
    }
}
