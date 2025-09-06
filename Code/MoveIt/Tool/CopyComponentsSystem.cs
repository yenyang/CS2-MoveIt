// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

using Colossal.Entities;
using Game.Common;
using Game.Rendering;
using Game.Tools;
using MoveIt.Components;
using MoveIt.Systems;
using QCommonLib;
using System;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace MoveIt.Tool
{
    /// <summary>
    /// A system to copy components from originals to temps.
    /// </summary>
    internal partial class CopyComponentsSystem : MIT_System
    {
        private EntityQuery m_TempQuery;
        private ToolSystem m_ToolSystem;
        private ModificationBarrier2 m_Barrier;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_Barrier = World.GetOrCreateSystemManaged<ModificationBarrier2>();

            m_ToolSystem.EventToolChanged += OnToolChanged;

            m_TempQuery = SystemAPI.QueryBuilder()
               .WithAll<Temp>()
               .WithAny<Game.Objects.Transform, Game.Net.Curve, Game.Net.Node>()               
               .WithNone<Deleted, Game.Common.Overridden>()
               .Build();

            RequireForUpdate(m_TempQuery);

            QLog.Info($"{nameof(CopyComponentsSystem)}.{nameof(OnCreate)}");
            Enabled = false;
        }

        protected override void OnUpdate()
        {
            if (!_MIT.Copying ||
                m_TempQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();
            NativeArray<Entity> entities = m_TempQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++) 
            {
                if (!EntityManager.TryGetComponent(entities[i], out Temp temp) ||
                    entities[i] == Entity.Null)
                {
                    continue;
                }

                if (EntityManager.TryGetComponent(temp.m_Original, out Game.Objects.Tree tree) &&
                    EntityManager.HasComponent<Game.Objects.Tree>(entities[i]))
                {
                    buffer.SetComponent(entities[i], tree);
                }

                buffer.RemoveComponent<Hidden>(temp.m_Original);
                temp.m_Original = Entity.Null;
                buffer.SetComponent(entities[i], temp);
            }
        }

        private void OnToolChanged(ToolBaseSystem tool)
        {
            if (tool == _MIT)
            {
                Enabled = true;
                return;
            }

            Enabled = false;
        }
    }
}
