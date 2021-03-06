﻿using FarseerPhysics.Dynamics;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Game.Common;
using Game.Serialization;

namespace Game.Portals
{
    [DataContract, DebuggerDisplay("FixturePortal {Name}")]
    public class FixturePortal : SceneNode, IPortal
    {
        [DataMember]
        public IPortal Linked { get; set; }
        /// <summary>
        /// If OneSided is true then the portal can only be viewed through it's front side.
        /// Entities can still travel though the portal in both directions however.
        /// </summary>
        [DataMember]
        public bool OneSided { get; set; } = true;
        [DataMember]
        public bool MirrorX { get; set; }
        [DataMember]
        float _size = 1;
        //Size of portal. Must not be equal to 0.
        public float Size {
            get { return _size; }
            set
            {
                Debug.Assert(_size != 0);
                _size = value;
            }
        }

        [DataMember]
        public IPolygonCoord Position { get; private set; }
        
        public const float EdgeMargin = 0.02f;
        public const float CollisionMargin = 0.1f;

        public FixturePortal(Scene scene)
            : this(scene, null, null)
        {
        }

        public FixturePortal(Scene scene, IWall parent, IPolygonCoord position)
            : base(scene)
        {
            if (parent != null && position != null)
            {
                SetPosition(new WallCoord(parent, position));
            }
        }

        public override IDeepClone ShallowClone()
        {
            FixturePortal clone = new FixturePortal(Scene);
            ShallowClone(clone);
            return clone;
        }

        protected void ShallowClone(FixturePortal destination)
        {
            base.ShallowClone(destination);
            destination.OneSided = OneSided;
            destination.MirrorX = MirrorX;
            destination.Linked = Linked;
        }

        public override void UpdateRefs(IReadOnlyDictionary<IDeepClone, IDeepClone> cloneMap)
        {
            base.UpdateRefs(cloneMap);
            if (Linked != null && cloneMap.ContainsKey(Linked))
            {
                Linked = (IPortal)cloneMap[Linked];
            }
            else
            {
                Linked = null;
            }
        }

        /// <summary>
        /// Returns a copy of the Transform local to the Body this is attached to.
        /// </summary>
        public override Transform2 GetTransform()
        {
            if (Position == null)
            {
                return null;
            }
            Transform2 t = PolygonExt.GetTransform(((IWall)Parent).Vertices, Position); 
            t.Size = Size;
            t.MirrorX = MirrorX;
            return t;
        }

        public override void Remove()
        {
            RemoveFixture();
            if (Linked != null)
            {
                Linked.Linked = null;
                Linked = null;
            }
            //SetLinked(null);
            base.Remove();
        }

        void RemoveFixture()
        {
            if (Position != null)
            {
                //FixtureExt.GetUserData(Position.Fixture).RemovePortal(this);
            }
        }

        public void SetPosition(WallCoord coord)
        {
            RemoveFixture();
            Position = new PolygonCoord(coord.EdgeIndex, coord.EdgeT);
            Debug.Assert(coord.Wall != null);
            Debug.Assert(coord.Wall is SceneNode);
            Debug.Assert(Position != null);
            SetParent((SceneNode)coord.Wall);
            //wake up all the bodies so that they will fall if there is now a portal entrance below them
            foreach (Body b in Scene.World.BodyList)
            {
                b.Awake = true;
            }
        }

        public void SetPosition(WallCoord coord, float size, bool mirrorX)
        {
            MirrorX = mirrorX;
            Size = size;
            SetPosition(coord);
        }

        public void SetMirrorX(bool mirrorX)
        {
            MirrorX = mirrorX;
        }

        public void SetSize(float size)
        {
            Size = size;
        }
    }
}
