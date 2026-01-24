// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

using Colossal.Entities;
using Game;
using Game.Common;
using Game.Rendering;
using Game.Tools;
using MoveIt.Components;
using MoveIt.Tool;
using QCommonLib;
using Unity.Collections;
using Unity.Entities;

namespace MoveIt.Systems
{
    internal partial class TempNodeSystem : MIT_System
    {
        private EntityQuery m_TempNodeQuery;
        private ToolSystem m_ToolSystem; 

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();

            m_ToolSystem.EventToolChanged += OnToolChanged;

            m_TempNodeQuery = SystemAPI.QueryBuilder()
                .WithAllRW<Game.Net.Node>()
                .WithAll<Game.Tools.Temp, Game.Net.ConnectedEdge, Game.Common.Updated>()
                .WithNone<Deleted, Owner>()
                .Build();

            RequireForUpdate(m_TempNodeQuery);

            QLog.Info($"{nameof(TempNodeSystem)}.{nameof(OnCreate)}");
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

        protected override void OnUpdate()
        {
            QLog.Debug($"{nameof(TempNodeSystem)}.{nameof(OnUpdate)}");
            QLog.Debug($"{nameof(TempNodeSystem)}.{nameof(OnUpdate)} m_ToolSystem.activeTool != _MIT: {m_ToolSystem.activeTool != _MIT}");
            QLog.Debug($"{nameof(TempNodeSystem)}.{nameof(OnUpdate)} _MIT.Copying {_MIT.Copying}");
            QLog.Debug($"{nameof(TempNodeSystem)}.{nameof(OnUpdate)} _MIT.Deleting {_MIT.Deleting}");
            QLog.Debug($"{nameof(TempNodeSystem)}.{nameof(OnUpdate)} _MIT.m_Workflow[(int)Workflow.Elevate] {_MIT.m_Workflow[(int)Workflow.Elevate]}");
            QLog.Debug($"{nameof(TempNodeSystem)}.{nameof(OnUpdate)} _MIT.m_Workflow[(int)Workflow.Lower] {_MIT.m_Workflow[(int)Workflow.Lower]}");

            if (m_ToolSystem.activeTool != _MIT ||
                _MIT.Copying ||
                _MIT.Deleting /*||
               (_MIT.m_Workflow[(int)Workflow.Elevate] == WorkflowProgression.NotStarted &&
                _MIT.m_Workflow[(int)Workflow.Lower] == WorkflowProgression.NotStarted)*/)
            {
                return;
            }

            EntityCommandBuffer buffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            NativeArray<Entity> entities = m_TempNodeQuery.ToEntityArray(Allocator.Temp);
            NativeList<Entity> processedSegments = new NativeList<Entity>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                if (entities[i] == Entity.Null ||
                   !EntityManager.TryGetComponent(entities[i], out Game.Net.Node node) ||
                   !EntityManager.TryGetComponent(entities[i], out Game.Tools.Temp temp) ||
                    temp.m_Original == Entity.Null ||
                   !EntityManager.HasComponent<MIT_Selected>(temp.m_Original) ||
                   !EntityManager.TryGetComponent(temp.m_Original, out Game.Net.Node originalNode))
                {
                    continue;
                }

                temp.m_Flags |= TempFlags.Modify | TempFlags.Essential;
                buffer.SetComponent(entities[i], temp);
                node.m_Position.y = originalNode.m_Position.y + _MIT.m_VerticalDisplacement;
                buffer.SetComponent(entities[i], node);
                /*
                if (!EntityManager.TryGetBuffer(entities[i], isReadOnly: true, out DynamicBuffer<Game.Net.ConnectedEdge> connectedEdges))
                {
                    continue;
                }

                for (int j = 0; j < connectedEdges.Length; j++)
                {
                    if (connectedEdges[j].m_Edge == Entity.Null ||
                        processedSegments.Contains(connectedEdges[j].m_Edge) ||
                        !EntityManager.TryGetComponent(connectedEdges[j].m_Edge, out Game.Net.Curve curve) ||
                        !EntityManager.TryGetComponent(connectedEdges[j].m_Edge, out Game.Net.Edge edge) ||
                        !EntityManager.TryGetComponent(connectedEdges[j].m_Edge, out Game.Tools.Temp connectedEdgeTemp) ||
                         connectedEdgeTemp.m_Original == Entity.Null ||
                        !EntityManager.TryGetComponent(connectedEdgeTemp.m_Original, out Game.Net.Curve originalCurve))
                    {
                        continue;
                    }

                    bool setCurve = false;

                    if (edge.m_End != Entity.Null &&
                        EntityManager.HasComponent<MIT_Selected>(edge.m_End))
                    {
                        curve.m_Bezier.d.y = originalCurve.m_Bezier.d.y + _MIT.m_VerticalDisplacement;
                        setCurve = true;
                    }

                    if (edge.m_Start != Entity.Null &&
                        EntityManager.HasComponent<MIT_Selected>(edge.m_Start))
                    {
                        curve.m_Bezier.a.y = originalCurve.m_Bezier.a.y + _MIT.m_VerticalDisplacement;
                        setCurve = true;
                    }

                    if (setCurve)
                    {
                        buffer.SetComponent(connectedEdges[j].m_Edge, curve);
                    }

                    processedSegments.Add(connectedEdges[j].m_Edge);
                }*/
            }

            buffer.Playback(EntityManager);
            buffer.Dispose();
        }
    }
}
