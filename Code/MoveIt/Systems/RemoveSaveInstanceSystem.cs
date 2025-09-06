// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

#define BURST

using Game;
using Game.Common;
using Game.Rendering;
using Game.Tools;
using MoveIt.Components;
using QCommonLib;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace MoveIt.Systems
{
    /// <summary>
    /// A system to remove SaveInstance component in the editor. This prevents the editor from updating prefabs just because you move something.
    /// </summary>
    internal partial class RemoveSaveInstanceSystem : MIT_System
    {
        private EntityQuery m_SaveInstanceQuery;
        private ToolSystem m_ToolSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();

            m_ToolSystem.EventToolChanged += OnToolChanged;

            m_SaveInstanceQuery = SystemAPI.QueryBuilder()
               .WithAll<Game.Prefabs.SaveInstance>()
               .WithNone<Deleted, Overridden, Temp>()
               .Build();

            RequireForUpdate(m_SaveInstanceQuery);

            QLog.Info($"{nameof(RemoveSaveInstanceSystem)}.{nameof(OnCreate)}");
            Enabled = false;
        }

        protected override void OnUpdate()
        {
            NativeArray<Entity> entities = m_SaveInstanceQuery.ToEntityArray(Allocator.Temp);
            EntityManager.RemoveComponent<Game.Prefabs.SaveInstance>(entities);
        }

        private void OnToolChanged(ToolBaseSystem tool)
        {
            if (tool == _MIT &&
                m_ToolSystem.actionMode.IsEditor())
            {
                Enabled = true;
                return;
            }

            Enabled = false;
        }
    }
}
