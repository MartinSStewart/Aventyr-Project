﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Game;
using OpenTK;
using Game.Portals;

namespace UnitTest
{
    [TestClass]
    public class PortalTests
    {
        const double PATH_INTERSECTION_DELTA = 0.0001;

        [TestMethod]
        public void GetPortalTransformTest0()
        {
            Scene scene = new Scene();
            FloatPortal p0 = new FloatPortal(scene);
            p0.SetTransform(new Transform2(new Vector2(1, 2), 4, 23));
            FloatPortal p1 = new FloatPortal(scene);
            p1.SetTransform(new Transform2(new Vector2(4, -1), 1.4f, -3));

            Transform2 result = Portal.GetLinkedTransform(p0, p1);
            Assert.IsTrue(Matrix4Ext.AlmostEqual(result.GetMatrix(), Portal.GetLinkedMatrix(p0, p1)));
        }

        [TestMethod]
        public void GetPortalTransformTest1()
        {
            Scene scene = new Scene();
            FloatPortal p0 = new FloatPortal(scene);
            p0.SetTransform(new Transform2(new Vector2(1, 2), 4, 23));
            FloatPortal p1 = new FloatPortal(scene);
            p1.SetTransform(new Transform2(new Vector2(4, -1), 1.4f, -3, true));

            Transform2 result = Portal.GetLinkedTransform(p0, p1);
            Assert.IsTrue(Matrix4Ext.AlmostEqual(result.GetMatrix(), Portal.GetLinkedMatrix(p0, p1)));
        }

        #region PathIntersections tests
        [TestMethod]
        public void PathIntersectionsTest0()
        {
            Scene scene = new Scene();
            FloatPortal p0 = new FloatPortal(scene);
            p0.SetTransform(new Transform2(new Vector2(1, 0)));
            FloatPortal p1 = new FloatPortal(scene);
            p1.SetTransform(new Transform2(new Vector2(10, -1)));
            
            p0.Linked = p1;
            p1.Linked = p0;

            Line ray = new Line(new Vector2(0, 0), new Vector2(8, -1));
            PortalPath path = new PortalPath();
            path.Enter(p0);
            var intersections = Portal.PathIntersections(path, ray);
            Assert.AreEqual(1, intersections.Length);
            Assert.AreEqual(0.5, intersections[0].TFirst, PATH_INTERSECTION_DELTA);
            Assert.AreEqual(1.0 / 3, intersections[0].TLast, PATH_INTERSECTION_DELTA);
        }

        [TestMethod]
        public void PathIntersectionsTest1()
        {
            Scene scene = new Scene();
            FloatPortal p0 = new FloatPortal(scene);
            p0.SetTransform(new Transform2(new Vector2(1, 0)));
            FloatPortal p1 = new FloatPortal(scene);
            p1.SetTransform(new Transform2(new Vector2(2, 0), 1, (float)Math.PI));

            p0.Linked = p1;
            p1.Linked = p0;

            FloatPortal p2 = new FloatPortal(scene);
            p2.SetTransform(new Transform2(new Vector2(3, 0)));
            FloatPortal p3 = new FloatPortal(scene);
            p3.SetTransform(new Transform2(new Vector2(4, 0), 1, (float)Math.PI));

            p2.Linked = p3;
            p3.Linked = p2;

            Line ray = new Line(new Vector2(0, 0), new Vector2(5, 0));
            PortalPath path = new PortalPath();
            path.Enter(p0);
            path.Enter(p2);

            var intersections = Portal.PathIntersections(path, ray);
            Assert.AreEqual(2, intersections.Length);
            Assert.AreEqual(0.5, intersections[0].TFirst, PATH_INTERSECTION_DELTA);
            Assert.AreEqual(1.0 / 3, intersections[0].TLast, PATH_INTERSECTION_DELTA);

            Assert.AreEqual(0.5, intersections[1].TFirst, PATH_INTERSECTION_DELTA);
            Assert.AreEqual(2.0 / 3, intersections[1].TLast, PATH_INTERSECTION_DELTA);
        }

        [TestMethod]
        public void PathIntersectionsTest2()
        {
            Scene scene = new Scene();
            FloatPortal p0 = new FloatPortal(scene);
            p0.SetTransform(new Transform2(new Vector2(1, 0)));
            FloatPortal p1 = new FloatPortal(scene);
            p1.SetTransform(new Transform2(new Vector2(2, 0), 1, (float)Math.PI));

            p0.Linked = p1;
            p1.Linked = p0;

            FloatPortal p2 = new FloatPortal(scene);
            p2.SetTransform(new Transform2(new Vector2(3, 0)));
            FloatPortal p3 = new FloatPortal(scene);
            p3.SetTransform(new Transform2(new Vector2(6, 3), 1, (float)Math.PI/2));

            p2.Linked = p3;
            p3.Linked = p2;

            Line ray = new Line(new Vector2(0, 0), new Vector2(6, 2));
            PortalPath path = new PortalPath();
            path.Enter(p0);
            path.Enter(p2);

            var intersections = Portal.PathIntersections(path, ray);
            Assert.AreEqual(2, intersections.Length);
            Assert.AreEqual(0.5, intersections[0].TFirst, PATH_INTERSECTION_DELTA);
            Assert.AreEqual(1.0 / 3, intersections[0].TLast, PATH_INTERSECTION_DELTA);

            Assert.AreEqual(0.5, intersections[1].TFirst, PATH_INTERSECTION_DELTA);
            Assert.AreEqual(2.0 / 3, intersections[1].TLast, PATH_INTERSECTION_DELTA);
        }
        #endregion

        #region Enter tests
        [TestMethod]
        public void EnterTest0()
        {
            Scene scene = new Scene();
            NodePortalable parent = new NodePortalable(scene);
            FloatPortal portal = new FloatPortal(scene);
            portal.SetTransform(new Transform2(new Vector2(5, 0)));
            portal.SetParent(parent);

            FloatPortal enter = new FloatPortal(scene);
            FloatPortal exit = new FloatPortal(scene);

            enter.SetTransform(new Transform2(new Vector2(2, 0)));
            exit.SetTransform(new Transform2(new Vector2(100, 5)));

            enter.Linked = exit;
            exit.Linked = enter;

            Portal.Enter(enter, parent, 0.5f);

            Assert.IsTrue(new Transform2(new Vector2(5, 0)).AlmostEqual(portal.WorldTransformPrevious));
        }
        #endregion
    }
}
