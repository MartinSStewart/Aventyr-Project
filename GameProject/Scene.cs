﻿using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game
{
    public class Scene
    {
        private List<Portal> _portals = new List<Portal>();
        private List<Entity> _entities = new List<Entity>();
        private Controller _window;

        public List<Entity> EntityList
        {
            get { return _entities; }
        }

        public Scene(Controller controller)
        {
            _window = controller;   
        }

        public void DrawScene(Matrix4 viewMatrix, float timeRenderDelta)
        {
            foreach (Entity v in EntityList)
            {
                v.Render(this, viewMatrix, (float)Math.Min(timeRenderDelta, 1 / _window.UpdateFrequency));
            }
        }

        public void AddEntity(Entity entity)
        {
            _entities.Add(entity);
        }

        public void AddPortal(Portal portal)
        {
            _portals.Add(portal);
        }
    }
}
