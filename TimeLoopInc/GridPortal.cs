﻿using Game.Portals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game;
using Game.Common;
using Game.Serialization;
using Game.Rendering;
using OpenTK;

namespace TimeLoopInc
{
    public class TimePortal : IPortalRenderable
    {
        public Vector2i Position { get; }
        public int TimeOffset { get; private set; }
        public TimePortal Linked { get; private set; }
        public GridAngle Direction { get; }
        IPortalRenderable IPortalRenderable.Linked => Linked;
        public bool OneSided => false;

        public Transform2 WorldTransform => new Transform2(
            (Vector2)Position + (Vector2.One + (Vector2)Direction.Vector) * 0.5f,
            (float)Direction.Radians,
            1.5f);
        public Transform2 WorldVelocity => Transform2.CreateVelocity();

        public TimePortal(Vector2i position, GridAngle direction)
        {
            Position = position;
            Direction = direction;
        }

        public TimePortal DeepClone() => (TimePortal)MemberwiseClone();

        public void SetLinked(TimePortal p1)
        {
            Linked = p1;
            p1.Linked = this;
            p1.TimeOffset = -TimeOffset;
        }

        public void SetTimeOffset(int timeOffset)
        {
            TimeOffset = timeOffset;
            Linked.TimeOffset = -timeOffset;
        }
    }
}
