﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Game.Portals;
using Game;
using OpenTK;
using System.Linq;

namespace UnitTest
{
    [TestClass]
    public class SimulationStepTests
    {
        #region Step tests
        [TestMethod]
        public void StepTest0()
        {
            Portalable p = new Portalable();
            Transform2 start = new Transform2(new Vector2(1, 5), 2.3f, 3.9f);
            Transform2 velocity = Transform2.CreateVelocity(new Vector2(-3, 4), 23, 0.54f);
            p.SetTransform(start);
            p.SetVelocity(velocity);
            SimulationStep.Step(new IPortalable[] { p }, new IPortal[0], 1, null);

            Assert.IsTrue(p.GetTransform().AlmostEqual(start.Add(velocity)));
        }

        /// <summary>
        /// Portal and portalable shouldn't collide so the result should be the same as in StepTest0.
        /// </summary>
        [TestMethod]
        public void StepTest1()
        {
            Portalable p = new Portalable();
            Transform2 start = new Transform2(new Vector2(1, 5), 2.3f, 3.9f);
            Transform2 velocity = Transform2.CreateVelocity(new Vector2(-3, 4), 23, 0.54f);
            p.SetTransform(start);
            p.SetVelocity(velocity);

            Scene scene = new Scene();
            FloatPortal portal = new FloatPortal(scene);

            SimulationStep.Step(new IPortalable[] { p }, new IPortal[] { portal }, 1, null);

            Assert.IsTrue(p.GetTransform().AlmostEqual(start.Add(velocity)));
        }

        [TestMethod]
        public void StepTest2()
        {
            Portalable p = new Portalable();
            Transform2 start = new Transform2(new Vector2(0, 0));
            Transform2 velocity = Transform2.CreateVelocity(new Vector2(3, 0));
            p.SetTransform(start);
            p.SetVelocity(velocity);

            Scene scene = new Scene();
            FloatPortal enter = new FloatPortal(scene);
            enter.SetTransform(new Transform2(new Vector2(1, 0)));

            FloatPortal exit = new FloatPortal(scene);
            exit.SetTransform(new Transform2(new Vector2(10, 10)));

            enter.Linked = exit;
            exit.Linked = enter;

            SimulationStep.Step(new IPortalable[] { p, enter, exit }, new IPortal[] { enter, exit }, 1, null);

            Assert.IsTrue(p.GetTransform().Position == new Vector2(8, 10));
        }

        [TestMethod]
        public void StepTest3()
        {
            Portalable p = new Portalable();
            Transform2 start = new Transform2(new Vector2(0, 0));
            Transform2 velocity = Transform2.CreateVelocity(new Vector2(3, 0));
            p.SetTransform(start);
            p.SetVelocity(velocity);

            Scene scene = new Scene();
            FloatPortal enter = new FloatPortal(scene);
            enter.SetTransform(new Transform2(new Vector2(1, 0)));
            enter.SetVelocity(Transform2.CreateVelocity(new Vector2(1, 0)));

            FloatPortal exit = new FloatPortal(scene);
            exit.SetTransform(new Transform2(new Vector2(10, 10)));

            enter.Linked = exit;
            exit.Linked = enter;

            SimulationStep.Step(new IPortalable[] { p, enter, exit }, new IPortal[] { enter, exit }, 1, null);

            Assert.IsTrue(p.GetTransform().Position == new Vector2(9, 10));
            Assert.IsTrue(p.GetVelocity().Position == new Vector2(-2, 0));
        }

        [TestMethod]
        public void StepTest4()
        {
            Portalable p = new Portalable();
            Transform2 start = new Transform2(new Vector2(0, 0));
            Transform2 velocity = Transform2.CreateVelocity(new Vector2(3, 0));
            p.SetTransform(start);
            p.SetVelocity(velocity);

            Scene scene = new Scene();
            FloatPortal enter = new FloatPortal(scene);
            enter.SetTransform(new Transform2(new Vector2(1, 0)));
            enter.SetVelocity(Transform2.CreateVelocity(new Vector2(1, 0)));

            FloatPortal exit = new FloatPortal(scene);
            exit.SetTransform(new Transform2(new Vector2(10, 10)));
            exit.SetVelocity(Transform2.CreateVelocity(new Vector2(10, 0)));

            enter.Linked = exit;
            exit.Linked = enter;

            SimulationStep.Step(new IPortalable[] { p, enter, exit }, new IPortal[] { enter, exit }, 1, null);

            Assert.IsTrue(p.GetTransform().Position == new Vector2(19, 10));
            Assert.IsTrue(p.GetVelocity().Position == new Vector2(8, 0));
        }

        /*[TestMethod]
        public void StepTest5()
        {
            Scene scene = new Scene();

            Actor p = new Actor(scene, PolygonFactory.CreateRectangle(2, 2));
            Transform2 start = new Transform2(new Vector2(0, 0));
            Transform2 velocity = Transform2.CreateVelocity(new Vector2(3, 0));
            p.SetTransform(start);
            p.SetVelocity(velocity);

            FloatPortal enter = new FloatPortal(scene);
            enter.SetTransform(new Transform2(new Vector2(1, 0)));
            enter.SetVelocity(Transform2.CreateVelocity(new Vector2(1, 0)));

            FloatPortal exit = new FloatPortal(scene);
            exit.SetTransform(new Transform2(new Vector2(10, 10)));
            exit.SetVelocity(Transform2.CreateVelocity(new Vector2(10, 0)));

            enter.Linked = exit;
            exit.Linked = enter;

            FixturePortal child = new FixturePortal(scene, p, new PolygonCoord(0, 0.5f));

            SimulationStep.Step(scene.GetAll().OfType<IPortalable>(), scene.GetAll().OfType<IPortal>(), 1, null);

            Assert.IsTrue(p.GetTransform().Position == new Vector2(19, 10));
            Assert.IsTrue(p.GetVelocity().Position == new Vector2(8, 0));
        }*/
        #endregion
    }
}