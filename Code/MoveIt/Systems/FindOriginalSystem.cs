// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

using Colossal.Entities;
using Game.Common;
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
        private EntityQuery m_TempCurveQuery;
        private EntityQuery m_SelectedCurveQuery;

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
                .WithAll<Game.Tools.Temp, Game.Objects.Transform, PrefabRef>()
                .WithNone<Deleted, Owner, MIT_Original>()
                .Build();

            m_SelectedNodeQuery = SystemAPI.QueryBuilder()
                .WithAll<MIT_Selected, Game.Objects.Transform, PrefabRef>()
                .WithNone<Deleted, Owner, Temp, MIT_ControlPoint>()
                .Build();

            m_TempCurveQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Tools.Temp, Game.Objects.Transform, PrefabRef>()
                .WithNone<Deleted, Owner, MIT_Original>()
                .Build();

            m_SelectedCurveQuery = SystemAPI.QueryBuilder()
                .WithAll<MIT_Selected, Game.Objects.Transform, PrefabRef>()
                .WithNone<Deleted, Owner, Temp, MIT_ControlPoint>()
                .Build();

            RequireAnyForUpdate(new EntityQuery[] { m_TempTransformQuery, m_TempNodeQuery, m_TempCurveQuery });
            RequireAnyForUpdate(new EntityQuery[] { m_SelectedObjectsQuery, m_SelectedCurveQuery, m_SelectedCurveQuery });

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
                            MatchesOriginal(tempTransform.m_Position, selectedTransform.m_Position))
                        {
                            buffer.AddComponent(tempObjects[i], new MIT_Original() { m_Original = selectedObjects[j] });
                            matched = j-1;
                            break;
                        }
                    }

                    selectedObjects.RemoveAt(matched);
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

            if (Vector3.Distance(tempPosition, referencePoint) < 0.01f)
            {
                return true;
            }

            return false;
        }
    }
}
