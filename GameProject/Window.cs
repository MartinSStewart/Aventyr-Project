﻿using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Game
{
    public class Window : GameWindow
    {
        Controller controller;
        public InputExt InputExt;
        public Window(string[] args)
            : base(800, 600, Renderer.DefaultGraphics, "Game", GameWindowFlags.FixedWindow)
        {
            InputExt = new InputExt(this);
            controller = new Controller(this, args);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Context.SwapInterval = 1;
            controller.OnLoad(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            controller.OnRenderFrame(e);
            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            InputExt.Update(Focused);
            if (InputExt.KeyPress(Key.F4))
            {
                ToggleFullScreen();
            }
            else if (InputExt.KeyPress(Key.Escape))
            {
                Exit();
            }
            controller.OnUpdateFrame(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            controller.OnClosing(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            controller.OnResize(e, ClientSize);
        }

        private void ToggleFullScreen()
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Fullscreen;
                ClientSize = new Size(Width, Height);
            }
            else if (WindowState == WindowState.Fullscreen)
            {
                WindowState = WindowState.Normal;
                ClientSize = new Size(800, 600);
            }
        }
    }
}
