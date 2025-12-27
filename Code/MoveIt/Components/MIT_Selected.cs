// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

using Unity.Entities;

namespace MoveIt.Components
{
    /// <summary>
    /// A tag component for entity is selected by Move It Tool.
    /// </summary>
    public struct MIT_Selected : IComponentData, IQueryTypeParameter
    {
    }
}
