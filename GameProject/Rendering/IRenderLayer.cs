﻿using System.Collections.Generic;
using Game.Portals;
using OpenTK;
using OpenTK.Graphics;
using Game.Common;
using Game.Models;

namespace Game.Rendering
{
    public interface IRenderLayer
    {
        List<IRenderable> Renderables { get; }
        List<IPortalRenderable> Portals { get; }
        ICamera2 Camera { get; }
        bool RenderPortalViews { get; }
        bool DepthTest { get; }
    }

    public static class IRenderLayerEx
    {
        public static void DrawText(this IRenderLayer layer, Font font, Vector2 position, string text, Vector2 alignment = new Vector2())
        {
            layer.Renderables.Add(new TextEntity(font, position, text, alignment));
        }

        public static void DrawRectangle(this IRenderLayer layer, Vector2 topLeft, Vector2 bottomRight, Color4 color = new Color4())
        {
            var renderable = new Renderable();
            renderable.IsPortalable = false;
            var plane = new Model(
                ModelFactory.CreatePlaneMesh(
                    Vector2.ComponentMin(topLeft, bottomRight), 
                    Vector2.ComponentMax(topLeft, bottomRight), 
                    color));
            if (color.A < 1)
            {
                plane.IsTransparent = true;
            }
            renderable.Models.Add(plane);

            layer.Renderables.Add(renderable);
        }
    }
}
