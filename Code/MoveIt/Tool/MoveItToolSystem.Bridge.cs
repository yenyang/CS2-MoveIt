// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

using Colossal.Entities;
using Game.Tools;
using MoveIt.Components;
using Unity.Entities;

namespace MoveIt.Tool
{
    public partial class MoveItToolSystem : ObjectToolBaseSystem
    {
        public static Entity GetOriginalEntity(Entity entity)
        {
            if (entity != Entity.Null &&
                m_Instance.EntityManager.TryGetComponent(entity, out MIT_Original mit_original) &&
                mit_original.m_Original != Entity.Null)
            {
                return mit_original.m_Original;
            }

            return Entity.Null;
        }
    }
}
