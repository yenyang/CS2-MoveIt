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
        public static bool TryGetOriginalEntity(Entity entity,  out Entity original)
        {
            if (entity != Entity.Null &&
                m_Instance.EntityManager.TryGetComponent(entity, out MIT_Original mit_original) &&
                mit_original.m_Original != Entity.Null)
            {
                original = mit_original.m_Original;
                return true;
            }

            original = Entity.Null;
            return false;
        }

        public static EntityQuery GetMIT_OriginalQueryWith(ComponentType[] all, ComponentType[] any, ComponentType[] none)
        {
            ComponentType[] allPlusMITOriginal = new ComponentType[all.Length + 1];
            for (int i = 0; i < all.Length; i++)
            {
                allPlusMITOriginal[i] = all[i];
            }
            allPlusMITOriginal[all.Length] = ComponentType.ReadOnly<MIT_Original>();

            return m_Instance.GetEntityQuery(new EntityQueryDesc[] { new EntityQueryDesc { All = allPlusMITOriginal, Any = any, None = none } });
        }
    }
}
