﻿
using FarseerPhysics.Dynamics;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml;

namespace Game
{
    /// <summary>
    /// Scene graph node.  All derived classes MUST override Clone(Scene) and return an instance of the derived class.
    /// </summary>
    [DataContract]
    public class SceneNode : ITreeNode<SceneNode>
    {
        /// <summary>Unique identifier within the scene.</summary>
        [DataMember]
        public long Id { get; private set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        List<SceneNode> _children = new List<SceneNode>();
        public List<SceneNode> Children { get { return new List<SceneNode>(_children); } }
        [DataMember]
        public SceneNode Parent { get; private set; }

        public readonly Scene Scene;

        #region Constructors
        public SceneNode(Scene scene)
        {
            Debug.Assert(scene != null);
            Scene = scene;
            if (Scene != null)
            {
                Id = Scene.GetId();
                SetParent(Scene.Root);
            }
        }
        #endregion

        /// <summary>
        /// Clones a SceneNode and recursively clones all of it's children.
        /// </summary>
        public SceneNode DeepClone()
        {
            return DeepClone(Scene);
        }

        /// <summary>
        /// Clones a SceneNode and recursively clones all of it's children.
        /// </summary>
        /// <param name="scene">Scene to clone into.</param>
        /// <param name="mask">If not null, Scene nodes will only be cloned if they are in the mask.</param>
        public SceneNode DeepClone(Scene scene, HashSet<SceneNode> mask = null)
        {
            Dictionary<SceneNode, SceneNode> cloneMap = new Dictionary<SceneNode, SceneNode>();
            List<SceneNode> cloneList = new List<SceneNode>();
            SceneNode clone = Clone(scene);
            clone.SetParent(null);
            cloneMap.Add(this, clone);
            cloneList.Add(this);
            CloneChildren(scene, cloneMap, cloneList, mask);
            foreach (SceneNode s in cloneList)
            {
                s.DeepCloneFinalize(cloneMap);
            }
            clone.SetParent(scene.Root);
            return clone;
        }

        /// <summary>
        /// This method is called by DeepClone after all SceneNodes have been cloned and parented. Useful for updating references.
        /// </summary>
        /// <param name="cloneMap">Map of source SceneNodes to destination SceneNodes.</param>
        protected virtual void DeepCloneFinalize(Dictionary<SceneNode, SceneNode> cloneMap)
        {
        }

        private void CloneChildren(Scene scene, Dictionary<SceneNode, SceneNode> cloneMap, List<SceneNode> cloneList, HashSet<SceneNode> mask)
        {
            foreach (SceneNode p in Children)
            {
                if (mask == null || mask.Contains(p))
                {
                    SceneNode clone = p.Clone(scene);
                    cloneMap.Add(p, clone);
                    clone.SetParent(cloneMap[p.Parent]);
                    cloneList.Add(p);
                }
                p.CloneChildren(scene, cloneMap, cloneList, mask);
            }
        }

        public virtual SceneNode Clone(Scene scene)
        {
            SceneNode clone = new SceneNode(scene);
            Clone(clone);
            return clone;
        }

        protected virtual void Clone(SceneNode destination)
        {
            destination.Name = Name;
        }

        public virtual void SetParent(SceneNode parent)
        {
            if (Parent != null)
            {
                Parent._children.Remove(this);
            }
            Parent = parent;
            if (Parent != null)
            {
                Debug.Assert(parent.Scene == Scene, "Parent cannot be in a different scene.");
                parent._children.Add(this);
            }
            Debug.Assert(!Tree<SceneNode>.ParentLoopExists(this), "Cannot have cycles in Parent tree.");
        }

        public void RemoveChildren()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                Children[i].SetParent(Scene.Root);
            }
        }

        /// <summary>Remove from scene graph.</summary>
        public virtual void Remove()
        {
            SetParent(null);
            //RemoveChildren();
        }

        public virtual void StepBegin()
        {
        }

        public virtual void StepEnd()
        {
        }

        public virtual Transform2 GetTransform()
        {
            return new Transform2();
        }

        public virtual Transform2 GetWorldTransform()
        {
            if (Parent != null)
            {
                return GetTransform().Transform(Parent.GetWorldTransform());
            }
            return GetTransform();
        }

        public virtual Transform2 GetVelocity()
        {
            return new Transform2();
        }

        public virtual Transform2 GetWorldVelocity()
        {
            if (Parent != null)
            {
                return GetVelocity().Transform(Parent.GetWorldVelocity());
            }
            return GetVelocity();
        }

        public SceneNode FindByName(string name)
        {
            return Tree<SceneNode>.FindByType<SceneNode>(this).Find(item => (item.Name == name));
        }
    }
}