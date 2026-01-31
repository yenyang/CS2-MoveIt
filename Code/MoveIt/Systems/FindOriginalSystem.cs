// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

using Colossal.Entities;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using MoveIt.Components;
using QCommonLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Systems
{
    // Determines entity reference to Original for MIT_Original component.
    internal partial class FindOriginalSystem : MIT_System
    {
        private ToolSystem m_ToolSystem;
        private TerrainSystem m_TerrainSystem;
        private EntityQuery m_TempTransformQuery;
        private EntityQuery m_SelectedObjectsQuery;
        private EntityQuery m_TempNodeQuery;
        private EntityQuery m_SelectedNodeQuery;
        private EntityQuery m_TempEdgeQuery;
        private EntityQuery m_SelectedEdgeQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_TerrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();

            m_ToolSystem.EventToolChanged += OnToolChanged;

            m_TempTransformQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Tools.Temp, Game.Objects.Transform, PrefabRef>()
                .WithNone<Deleted, Owner, MIT_Original>()
                .Build();

            m_SelectedObjectsQuery = SystemAPI.QueryBuilder()
                .WithAll<MIT_Selected, Game.Objects.Transform, PrefabRef>()
                .WithNone<Deleted, Owner, Temp, MIT_ControlPoint>()
                .Build();

            m_TempNodeQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Tools.Temp, Game.Net.Node, PrefabRef>()
                .WithNone<Deleted, Owner, MIT_Original>()
                .Build();

            m_SelectedNodeQuery = SystemAPI.QueryBuilder()
                .WithAll<MIT_Selected, Game.Net.Node, PrefabRef>()
                .WithNone<Deleted, Owner, Temp, MIT_ControlPoint>()
                .Build();

            m_TempEdgeQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Tools.Temp, Game.Net.Curve, Game.Net.Edge, PrefabRef>()
                .WithNone<Deleted, Owner, MIT_Original>()
                .Build();

            m_SelectedEdgeQuery = SystemAPI.QueryBuilder()
                .WithAll<MIT_Selected, Game.Net.Curve, Game.Net.Edge, PrefabRef>()
                .WithNone<Deleted, Owner, Temp, MIT_ControlPoint>()
                .Build();

            RequireAnyForUpdate(new EntityQuery[] { m_TempTransformQuery, m_TempNodeQuery, m_TempEdgeQuery });

            Enabled = false;

            QLog.Info($"{nameof(FindOriginalSystem)}.{nameof(OnCreate)}");
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
                !_MIT.Copying ||
                _MIT.Deleting)
            {
                return;
            }

            EntityCommandBuffer buffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            // Find original objects.
            NativeArray<Entity> tempObjects = m_TempTransformQuery.ToEntityArray(Allocator.Temp);
            NativeList<Entity> selectedObjects = m_SelectedObjectsQuery.ToEntityListAsync(Allocator.Temp, Dependency, out JobHandle jobHandle);
            jobHandle.Complete();

            for (int i = 0; i < tempObjects.Length; i++)
            {
                if (tempObjects[i] != Entity.Null &&
                    EntityManager.TryGetComponent(tempObjects[i], out Game.Objects.Transform tempTransform) &&
                    EntityManager.TryGetComponent(tempObjects[i], out Game.Prefabs.PrefabRef tempPrefabRef) &&
                    tempPrefabRef.m_Prefab != Entity.Null)
                {
                    int matched = 0;
                    for (int j = 0; j < selectedObjects.Length; j++)
                    {
                        if (selectedObjects[j] != Entity.Null &&
                            EntityManager.TryGetComponent(selectedObjects[j], out Game.Objects.Transform selectedTransform) &&
                            EntityManager.TryGetComponent(selectedObjects[j], out Game.Prefabs.PrefabRef selectedPrefabRef) &&
                            selectedPrefabRef.m_Prefab == tempPrefabRef.m_Prefab &&
                            MatchesOriginal(tempTransform, selectedTransform))
                        {
                            buffer.AddComponent(tempObjects[i], new MIT_Original() { m_Original = selectedObjects[j] });
                            matched = j;
                            break;
                        }
                    }

                    selectedObjects.RemoveAt(matched);
                }
            }

            // Find original nodes.
            NativeArray<Entity> tempNodes = m_TempNodeQuery.ToEntityArray(Allocator.Temp);
            NativeList<Entity> selectedNodes = m_SelectedNodeQuery.ToEntityListAsync(Allocator.Temp, Dependency, out JobHandle jobHandle2);
            NativeList<Entity> selectedEdges = m_SelectedEdgeQuery.ToEntityListAsync(Allocator.Temp, Dependency, out JobHandle jobHandle3);
            jobHandle2.Complete();
            jobHandle3.Complete();

            for (int i = 0; i < selectedNodes.Length; i++)
            {
                if (EntityManager.TryGetBuffer(selectedNodes[i], isReadOnly: true, out DynamicBuffer<ConnectedEdge> connectedEdges))
                {
                    for (int j = 0; j < connectedEdges.Length; j++)
                    {
                        if (connectedEdges[j].m_Edge != Entity.Null &&
                            !selectedEdges.Contains(connectedEdges[j].m_Edge))
                        {
                            selectedEdges.Add(connectedEdges[j].m_Edge);
                        }

                        if (connectedEdges[j].m_Edge != Entity.Null &&
                            EntityManager.TryGetComponent(connectedEdges[j].m_Edge, out Game.Net.Edge edge))
                        {
                            if  (edge.m_End != Entity.Null &&
                                !selectedNodes.Contains(edge.m_End))
                            {
                                selectedNodes.Add(edge.m_End);
                            }

                            if (edge.m_Start != Entity.Null &&
                               !selectedNodes.Contains(edge.m_Start))
                            {
                                selectedNodes.Add(edge.m_Start);
                            }
                        }

                    }
                }

            }

            for (int i = 0; i < tempNodes.Length; i++)
            {
                if (tempNodes[i] != Entity.Null &&
                    EntityManager.TryGetComponent(tempNodes[i], out Game.Net.Node tempNode) &&
                    EntityManager.TryGetComponent(tempNodes[i], out Game.Prefabs.PrefabRef tempPrefabRef) &&
                    tempPrefabRef.m_Prefab != Entity.Null)
                {
                    int matched = 0;
                    for (int j = 0; j < selectedNodes.Length; j++)
                    {
                        if (selectedNodes[j] != Entity.Null &&
                            EntityManager.TryGetComponent(selectedNodes[j], out Game.Net.Node selectedNode) &&
                            EntityManager.TryGetComponent(selectedNodes[j], out Game.Prefabs.PrefabRef selectedPrefabRef) &&
                            selectedPrefabRef.m_Prefab == tempPrefabRef.m_Prefab &&
                            MatchesOriginal(tempNode.m_Position, selectedNode.m_Position))
                        {
                            buffer.AddComponent(tempNodes[i], new MIT_Original() { m_Original = selectedNodes[j] });
                            matched = j;
                            
                            break;
                        }
                    }

                    selectedNodes.RemoveAt(matched);
                }
            }


            buffer.Playback(EntityManager);
            buffer.Dispose();
        }


        private Game.Objects.Transform GetRotatedPosition(Game.Objects.Transform originalTransform)
        {
            ControlPoint centroid = new ControlPoint() { m_Position = _MIT.Selection.Center };
            return ObjectUtils.LocalToWorld(new Game.Objects.Transform(centroid.m_Position, quaternion.RotateY(_MIT.m_RotationAboutCenter)), new Game.Objects.Transform() { m_Position = originalTransform.m_Position - centroid.m_Position, m_Rotation = originalTransform.m_Rotation });
        }

        private float3 GetTranslatedXZPositionAndVerticallyDisplace(float3 position)
        {
            ControlPoint endPoint = new ControlPoint() { m_Position = _MIT.m_PointerPos };
            ControlPoint centroid = new ControlPoint() { m_Position = _MIT.Selection.Center };
            position.x += endPoint.m_Position.x - centroid.m_Position.x;
            position.z += endPoint.m_Position.z - centroid.m_Position.z;

            position.y += _MIT.m_VerticalDisplacement;

            return position;
        }

        private float3 FollowTerrain(float3 newPosition, float3 originalPosition)
        {
            TerrainHeightData terrainHeightData = m_TerrainSystem.GetHeightData(waitForPending: false);
            newPosition.y += TerrainUtils.SampleHeight(ref terrainHeightData, newPosition) - TerrainUtils.SampleHeight(ref terrainHeightData, originalPosition);
            return newPosition;
        }

        private bool MatchesOriginal(float3 tempPosition, float3 originalPosition)
        {
            float3 referencePoint = originalPosition;
            if (_MIT.m_RotationAboutCenter != 0)
            {
                referencePoint = GetRotatedPosition(new Game.Objects.Transform() { m_Position = referencePoint, m_Rotation = quaternion.identity }).m_Position;
            }

            referencePoint = GetTranslatedXZPositionAndVerticallyDisplace(referencePoint);

            if (_MIT.m_FollowingTerrain)
            {
                referencePoint = FollowTerrain(referencePoint, originalPosition);
            }

            if (Vector3.Distance(tempPosition, referencePoint) < 0.1f)
            {
                return true;
            }

            return false;
        }


        private bool MatchesOriginal(Game.Objects.Transform tempTransform, Game.Objects.Transform originalTransform)
        {
            Game.Objects.Transform referenceTransform = originalTransform;
            if (_MIT.m_RotationAboutCenter != 0)
            {
                referenceTransform = GetRotatedPosition(originalTransform);
            }

            referenceTransform.m_Position = GetTranslatedXZPositionAndVerticallyDisplace(referenceTransform.m_Position);

            if (_MIT.m_FollowingTerrain)
            {
                referenceTransform.m_Position = FollowTerrain(referenceTransform.m_Position, originalTransform.m_Position);
            }

            if (Vector3.Distance(tempTransform.m_Position, referenceTransform.m_Position) < 0.001f &&
                Mathf.Approximately(tempTransform.m_Rotation.value.x, referenceTransform.m_Rotation.value.x) &&
                Mathf.Approximately(tempTransform.m_Rotation.value.y, referenceTransform.m_Rotation.value.y) &&
                Mathf.Approximately(tempTransform.m_Rotation.value.z, referenceTransform.m_Rotation.value.z) &&
                Mathf.Approximately(tempTransform.m_Rotation.value.w, referenceTransform.m_Rotation.value.w))
            {
                return true;
            }

            return false;
        }
    }
}
