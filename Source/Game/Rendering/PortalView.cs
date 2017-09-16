﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ClipperLib;
using Game.Common;
using Game.Portals;
using OpenTK;

namespace Game.Rendering
{
    public class PortalView : ITreeNode<PortalView>
    {
        public Matrix4 ViewMatrix { get; private set; }
        public List<List<IntPoint>> Paths { get; private set; }
        public List<PortalView> Children { get; private set; }
        public PortalView Parent { get; private set; }
        public LineF[] FovLines { get; private set; }
        public LineF[] FovLinesPrevious { get; private set; }
        public LineF PortalLine { get; private set; }
        public IPortalRenderable PortalEntrance { get; private set; }

        public PortalView(PortalView parent, Matrix4 viewMatrix, List<IntPoint> path, LineF[] fovLines, LineF[] fovLinesPrevious)
            : this(parent, viewMatrix, new List<List<IntPoint>>(), fovLines, fovLinesPrevious, null, null)
        {
            Paths.Add(path);
        }

        public PortalView(PortalView parent, Matrix4 viewMatrix, List<List<IntPoint>> path, LineF[] fovLines, LineF[] fovLinesPrevious, LineF portalLine, IPortalRenderable portalEntrance)
        {
            PortalLine = portalLine;
            FovLines = fovLines;
            FovLinesPrevious = fovLinesPrevious;
            Children = new List<PortalView>();
            Parent = parent;
            Parent?.Children.Add(this);
            ViewMatrix = viewMatrix;
            Paths = path;
            PortalEntrance = portalEntrance;
        }

        public List<PortalView> GetPortalViewList()
        {
            var list = new List<PortalView> {this};
            foreach (PortalView p in Children)
            {
                list.AddRange(p.GetPortalViewList());
            }
            return list;
        }

        public static PortalView CalculatePortalViews(float shutterTime, IEnumerable<IPortalRenderable> portals, ICamera2 camera, int depth)
        {
            DebugEx.Assert(camera != null);
            DebugEx.Assert(depth >= 0);
            DebugEx.Assert(portals != null);
            List<IntPoint> view = ClipperConvert.ToIntPoint(camera.GetWorldVerts());
            var viewMatrix = camera.GetViewMatrix(camera.IsOrtho);
            var portalView = new PortalView(null, viewMatrix, view, new LineF[0], new LineF[0]);
            Vector2 camPos = camera.WorldTransform.Position;

            var actionList = new List<Func<bool>>();

            foreach (var p in portals)
            {
                actionList.Add(() => CalculatePortalViews(p, null, portals, viewMatrix, camPos, camPos - camera.WorldVelocity.Position * shutterTime, portalView, Matrix4.Identity, actionList));
            }

            while (actionList.Count > 0 && depth > 0)
            {
                bool result = actionList.First().Invoke();
                if (result)
                {
                    depth--;
                }
                actionList.RemoveAt(0);
            }

            return portalView;
        }

        static bool CalculatePortalViews(IPortalRenderable portal, IPortalRenderable portalEnter, IEnumerable<IPortalRenderable> portals, Matrix4 viewMatrix, Vector2 viewPos, Vector2 viewPosPrevious, PortalView portalView, Matrix4 portalMatrix, List<Func<bool>> actionList)
        {
            const float areaEpsilon = 0.0001f;

            //The clipper must be set to strictly simple. Otherwise polygons might have duplicate vertices which causes poly2tri to generate incorrect results.
            var c = new Clipper {StrictlySimple = true};

            if (!_isPortalValid(portalEnter, portal, viewPos))
            {
                return false;
            }

            Vector2[] fov = Vector2Ex.Transform(Portal.GetFov(portal, viewPos, 500, 3), portalMatrix);
            if (MathEx.GetArea(fov) < areaEpsilon)
            {
                return false;
            }
            List<IntPoint> pathFov = ClipperConvert.ToIntPoint(fov);

            var viewNew = new List<List<IntPoint>>();

            c.AddPath(pathFov, PolyType.ptSubject, true);
            c.AddPaths(portalView.Paths, PolyType.ptClip, true);
            c.Execute(ClipType.ctIntersection, viewNew);
            c.Clear();

            if (viewNew.Count <= 0)
            {
                return false;
            }
            c.AddPaths(viewNew, PolyType.ptSubject, true);
            foreach (var other in portals)
            {
                if (other == portal)
                {
                    continue;
                }
                if (!_isPortalValid(portalEnter, other, viewPos))
                {
                    continue;
                }
                //Skip this portal if it's inside the current portal's Fov.
                LineF portalLine = new LineF(portal.GetWorldVerts());
                LineF portalOtherLine = new LineF(other.GetWorldVerts());
                if (portalLine.IsInsideFov(viewPos, portalOtherLine))
                {
                    continue;
                }
                Vector2[] otherFov = Vector2Ex.Transform(Portal.GetFov(other, viewPos, 500, 3), portalMatrix);
                if (MathEx.GetArea(otherFov) < areaEpsilon)
                {
                    continue;
                }
                otherFov = MathEx.SetWinding(otherFov, true);
                List<IntPoint> otherPathFov = ClipperConvert.ToIntPoint(otherFov);
                c.AddPath(otherPathFov, PolyType.ptClip, true);
            }
            var viewNewer = new List<List<IntPoint>>();
            c.Execute(ClipType.ctDifference, viewNewer, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
            c.Clear();
            if (viewNewer.Count <= 0)
            {
                return false;
            }

            Vector2 viewPosNew = Vector2Ex.Transform(viewPos, Portal.GetLinkedMatrix(portal, portal.Linked));
            Vector2 viewPosPreviousNew = Vector2Ex.Transform(viewPosPrevious, Portal.GetLinkedMatrix(portal, portal.Linked));

            Matrix4 portalMatrixNew = Portal.GetLinkedMatrix(portal.Linked, portal) * portalMatrix;
            Matrix4 viewMatrixNew = portalMatrixNew * viewMatrix;

            LineF[] lines = Portal.GetFovLines(portal, viewPos, 500);
            lines[0] = lines[0].Transform(portalMatrix);
            lines[1] = lines[1].Transform(portalMatrix);
            LineF[] linesPrevious = Portal.GetFovLines(portal, viewPosPrevious, 500);
            linesPrevious[0] = linesPrevious[0].Transform(portalMatrix);
            linesPrevious[1] = linesPrevious[1].Transform(portalMatrix);

            LineF portalWorldLine = new LineF(portal.GetWorldVerts());
            portalWorldLine = portalWorldLine.Transform(portalMatrix);
            PortalView portalViewNew = new PortalView(portalView, viewMatrixNew, viewNewer, lines, linesPrevious, portalWorldLine, portal);

            foreach (IPortalRenderable p in portals)
            {
                actionList.Add(() =>
                    CalculatePortalViews(p, portal, portals, viewMatrix, viewPosNew, viewPosPreviousNew, portalViewNew, portalMatrixNew, actionList)
                );
            }
            return true;
        }

        static bool _isPortalValid(IPortalRenderable previous, IPortalRenderable next, Vector2 viewPos)
        {
            //skip this portal if it isn't linked 
            if (!next.IsValid())
            {
                return false;
            }
            //or it's the exit portal
            if (previous != null && next == previous.Linked)
            {
                return false;
            }
            //or if the portal is one sided and the view point is on the wrong side
            Vector2[] pv2 = next.GetWorldVerts();
            LineF portalLine = new LineF(pv2);
            if (next.OneSided)
            {
                if (portalLine.GetSideOf(pv2[0] + next.WorldTransform.GetRight()) != portalLine.GetSideOf(viewPos))
                {
                    return false;
                }
            }
            //or if this portal isn't inside the fov of the exit portal
            if (previous != null)
            {
                LineF portalEnterLine = new LineF(previous.Linked.GetWorldVerts());
                if (!portalEnterLine.IsInsideFov(viewPos, portalLine))
                {
                    return false;
                }
            }
            return true;
        }
    }
}