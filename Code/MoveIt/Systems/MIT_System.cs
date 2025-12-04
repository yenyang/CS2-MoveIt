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
        protected static readonly MIT _MIT = MIT.m_Instance;

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


    internal abstract partial class MIT_ToolSystem : ObjectToolBaseSystem
    {
        protected static MIT _MIT;

        public MIT_ToolSystem ()
        {
        } 

        protected override void OnCreate()
        {
            _MIT = World.GetOrCreateSystemManaged<MIT>();
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
