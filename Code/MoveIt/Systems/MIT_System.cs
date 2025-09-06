// <copyright>
// Copyright (c) Yenyang. MIT License See LICENSE.txt
// Forked with permission from Quboid's CS2-MoveIt project.
// </copyright>

using Game.Tools;
using MoveIt.Tool;
using Unity.Entities;

namespace MoveIt.Systems
{
    internal abstract partial class MIT_System : SystemBase
    {
        protected static readonly MoveItToolSystem _MIT = MoveItToolSystem.m_Instance;

        protected override void OnCreate()
        {
            Enabled = false;
        }

        internal virtual void Start()
        {
            Enabled = true;
        }

        internal virtual void Start(Actions.Action action, int actionIndex)
        {
            Enabled = true;
        }

        internal virtual void End()
        {
            Enabled = false;
        }
    }
}
