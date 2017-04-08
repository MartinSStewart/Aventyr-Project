﻿using Game;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TankGameTestFramework
{
    public class FakeController : IController
    {
        Size IController.CanvasSize => CanvasSize;
        public Size CanvasSize => new Size(800, 600);
        IInput IController.Input => Input;
        public FakeInput Input { get; private set; } = new FakeInput();
    }
}
