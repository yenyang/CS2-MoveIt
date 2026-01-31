// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

using Colossal.Entities;
using Game.Common;
using Game.Simulation;
using Game.Tools;
using MoveIt.Components;
using MoveIt.Tool;
using QCommonLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Systems
{
    // Vanilla GenerateEdges/Nodes Systems do not handle modifications to node elevations. This system fixes that.
    internal partial class TempNodeSystem : MIT_System
    {
        private EntityQuery m_TempNodeQuery;
        private ToolSystem m_ToolSystem; 
        private TerrainSystem m_TerrainSystem;
        private EntityQuery m_SelectedMITControlPointQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_TerrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();

            m_ToolSystem.EventToolChanged += OnToolChanged;

            m_TempNodeQuery = SystemAPI.QueryBuilder()
                .WithAllRW<Game.Net.Node>()
                .WithAll<Game.Tools.Temp, Game.Net.ConnectedEdge, Game.Common.Updated>()
                .WithNone<Deleted, Owner>()
                .Build();

            m_SelectedMITControlPointQuery = SystemAPI.QueryBuilder()
                .WithAll<MIT_Selected, MIT_ControlPoint>()
                .WithNone<Deleted, Temp>()
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
            if (m_ToolSystem.activeTool != _MIT ||
                _MIT.Copying ||
                _MIT.Deleting ||
               (_MIT.m_Workflow[(int)Workflow.Elevate] == WorkflowProgression.NotStarted &&
                _MIT.m_Workflow[(int)Workflow.Lower] == WorkflowProgression.NotStarted &&
               !_MIT.m_FollowingTerrain))
            {
                return;
            }

            EntityCommandBuffer buffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            NativeArray<Entity> entities = m_TempNodeQuery.ToEntityArray(Allocator.Temp);
            NativeList<Entity> processedSegments = new NativeList<Entity>(Allocator.Temp);

            // This runs through selected Move it control points to see if any of them are attached to Nodes and should move those as well. 
            NativeList<Entity> selectedControlPointNodes = new NativeList<Entity>(m_SelectedMITControlPointQuery.CalculateEntityCount(), Allocator.Temp);
            if (_MIT.IsManipulating &&
               !m_SelectedMITControlPointQuery.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> selectedControlPointEntities = m_SelectedMITControlPointQuery.ToEntityArray(Allocator.Temp);
                for (int i = 0; i < selectedControlPointEntities.Length; i++)
                {
                    if (selectedControlPointEntities[i] != Entity.Null &&
                        EntityManager.TryGetComponent(selectedControlPointEntities[i], out MIT_ControlPoint controlPoint) &&
                        controlPoint.m_Parent != Entity.Null &&
                        controlPoint.m_Node != Entity.Null &&
                       (controlPoint.m_ParentKey == 0 ||
                        controlPoint.m_ParentKey == 3) &&
                       !selectedControlPointNodes.Contains(controlPoint.m_Node))
                    {
                        selectedControlPointNodes.Add(controlPoint.m_Node);
                    }
                }
            }

            for (int i = 0; i < entities.Length; i++)
            {
                if (entities[i] == Entity.Null ||
                   !EntityManager.TryGetComponent(entities[i], out Game.Net.Node node) ||
                   !EntityManager.TryGetComponent(entities[i], out Game.Tools.Temp temp) ||
                    temp.m_Original == Entity.Null ||
                  (!EntityManager.HasComponent<MIT_Selected>(temp.m_Original) &&
                   !selectedControlPointNodes.Contains(temp.m_Original)) ||
                   !EntityManager.TryGetComponent(temp.m_Original, out Game.Net.Node originalNode))
                {
                    continue;
                }

                // This flag is important.
                temp.m_Flags |= TempFlags.Modify;
                buffer.SetComponent(entities[i], temp);
                node.m_Position.y = originalNode.m_Position.y + _MIT.m_VerticalDisplacement;
                if (_MIT.m_FollowingTerrain)
                {
                    node.m_Position = FollowTerrain(node.m_Position, originalNode.m_Position);
                }

                buffer.SetComponent(entities[i], node);
                
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
                        !EntityManager.TryGetComponent(connectedEdgeTemp.m_Original, out Game.Net.Curve originalCurve) ||
                        !EntityManager.TryGetComponent(connectedEdgeTemp.m_Original, out Game.Net.Edge originalEdge))
                    {
                        continue;
                    }

                    bool setCurve = false;

                    if (edge.m_End != Entity.Null &&
                       EntityManager.HasComponent<MIT_Selected>(originalEdge.m_End))
                    {
                        curve.m_Bezier.d.y = originalCurve.m_Bezier.d.y + _MIT.m_VerticalDisplacement;
                        curve.m_Bezier.c.y = originalCurve.m_Bezier.d.y + _MIT.m_VerticalDisplacement;

                        if (_MIT.m_FollowingTerrain)
                        {
                            curve.m_Bezier.d = FollowTerrain(curve.m_Bezier.d, originalCurve.m_Bezier.d);
                            curve.m_Bezier.c = FollowTerrain(curve.m_Bezier.c, originalCurve.m_Bezier.c);
                        }
                        setCurve = true;
                    } else if (edge.m_End != Entity.Null &&
                               selectedControlPointNodes.Contains(originalEdge.m_End))
                    {
                        curve.m_Bezier.d.y = originalCurve.m_Bezier.d.y + _MIT.m_VerticalDisplacement;

                        if (_MIT.m_FollowingTerrain)
                        {
                            curve.m_Bezier.d = FollowTerrain(curve.m_Bezier.d, originalCurve.m_Bezier.d);
                        }
                        setCurve = true;
                    }

                    if (edge.m_Start != Entity.Null &&
                        EntityManager.HasComponent<MIT_Selected>(originalEdge.m_Start))
                    {
                        curve.m_Bezier.a.y = originalCurve.m_Bezier.a.y + _MIT.m_VerticalDisplacement;
                        curve.m_Bezier.b.y = originalCurve.m_Bezier.a.y + _MIT.m_VerticalDisplacement;

                        if (_MIT.m_FollowingTerrain)
                        {
                            curve.m_Bezier.a = FollowTerrain(curve.m_Bezier.a, originalCurve.m_Bezier.a);
                            curve.m_Bezier.b = FollowTerrain(curve.m_Bezier.b, originalCurve.m_Bezier.b);
                        }
                        setCurve = true;
                    }
                    else if (edge.m_Start != Entity.Null &&
                             selectedControlPointNodes.Contains(originalEdge.m_Start)) 
                    {
                        curve.m_Bezier.a.y = originalCurve.m_Bezier.a.y + _MIT.m_VerticalDisplacement;

                        if (_MIT.m_FollowingTerrain)
                        {
                            curve.m_Bezier.a = FollowTerrain(curve.m_Bezier.a, originalCurve.m_Bezier.a);
                        }
                        setCurve = true;
                    }

                    if (setCurve)
                    {
                        buffer.SetComponent(connectedEdges[j].m_Edge, curve);
                    }

                    processedSegments.Add(connectedEdges[j].m_Edge);
                }
            }

            buffer.Playback(EntityManager);
            buffer.Dispose();
        }

        private float3 FollowTerrain(float3 newPosition, float3 originalPosition)
        {
            TerrainHeightData terrainHeightData = m_TerrainSystem.GetHeightData(waitForPending: false);
            newPosition.y += TerrainUtils.SampleHeight(ref terrainHeightData, newPosition) - TerrainUtils.SampleHeight(ref terrainHeightData, originalPosition);
            return newPosition;
        }
    }
}
