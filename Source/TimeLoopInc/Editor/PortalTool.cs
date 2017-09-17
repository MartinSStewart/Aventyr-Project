﻿using System;
using System.Collections.Generic;
using System.Linq;
using Game.Common;
using Game.Rendering;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;

namespace TimeLoopInc.Editor
{
    public class PortalTool : ITool
    {
        readonly IEditorController _editor;

        public PortalTool(IEditorController editor)
        {
            _editor = editor;
        }

        /// <summary>
        /// Returns a modified scene or null if no changes have been made.
        /// </summary>
        public void Update()
        {
            var window = _editor.Window;
            var scene = _editor.Scene;
            var mousePosition = window.MouseWorldPos(_editor.Camera);
            var mouseGridPos = (Vector2i)mousePosition.Floor(Vector2.One);
            if (window.ButtonPress(_editor.PlaceButton))
            {
                var sides = EditorController.PortalValidSides(mouseGridPos, scene.Floor);
                if (sides.Count > 0)
                {
                    var side = sides
                        .OrderBy(item => ((Vector2)item.Vector - mousePosition.Frac(Vector2.One) + Vector2.One / 2).Length)
                        .First();

                    var newPortal = new PortalBuilder(mouseGridPos, side);

                    var links = scene.Links;

                    // Remove any portals the new portal is overlapping.
                    var collisions = EditorController.PortalCollisions(newPortal, scene.Links.SelectMany(item => item.Portals));
                    links = EditorController.GetPortals(portal => !collisions.Contains(portal), links);

                    var selectedPortal = LinkTool.SelectedPortal(scene);
                    links = links.Add(new PortalLink(new[] { newPortal }));
                    if (window.ButtonDown(KeyBoth.Shift) && selectedPortal != null)
                    {
                        DebugEx.Assert(LinkTool.GetLink(selectedPortal, links).Portals.Length == 1);
                        links = LinkTool.LinkPortals(selectedPortal, newPortal, links);
                        selectedPortal = null;
                    }
                    else
                    {
                        selectedPortal = newPortal;
                    }

                    _editor.ApplyChanges(scene.With(links: links).With(selectedPortal?.Position));
                }
            }
            EditorController.DeleteAndSelect(_editor, mouseGridPos);
        }

        public List<IRenderable> Render()
        {
            var output = new List<IRenderable>();
            var window = _editor.Window;

            var _mousePosition = window.MouseWorldPos(_editor.Camera);

            var previousLink = _editor.Scene.Links.LastOrDefault();
            var selectedPortal = LinkTool.SelectedPortal(_editor.Scene);
            if (window.ButtonDown(KeyBoth.Shift) && selectedPortal != null)
            {
                var line = Draw.Line(
                    new LineF(selectedPortal.Center, _mousePosition),
                    Color4.Black,
                    0.04f);
                output.Add(line);
            }

            return output;
        }
    }
}
