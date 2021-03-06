﻿using System.Diagnostics;
using Game;

namespace EditorLogic.Tools
{
    public abstract class Tool
    {
        protected IInput Input => Controller.Input;
        public bool Enabled { get; private set; }
        public bool Active { get; protected set; }
        public virtual bool EditorOnly { get; }

        public readonly ControllerEditor Controller;

        #region Constructors
        public Tool()
            : this(null)
        {
        }

        public Tool(ControllerEditor controller)
        {
            Enabled = false;
            Controller = controller;
        }
        #endregion

        public virtual void Update()
        {
            Debug.Assert(Enabled == true, "Tool has not been enabled.  Call Enable before calling Update.");
        }
        public virtual void Enable()
        {
            Enabled = true;
        }
        public virtual void Disable()
        {
            Enabled = false;
        }
        public virtual bool LockCamera()
        {
            return false;
        }
    }
}
