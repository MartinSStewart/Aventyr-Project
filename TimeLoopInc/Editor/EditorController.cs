﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using MoreLinq;
using Game.Common;
using Game.Rendering;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using Ui;
using Game;

namespace TimeLoopInc.Editor
{
    public class EditorController : IEditorController
    {
        enum ToolType { Player, Block, Wall, Portal, Link, Exit }

        public static string LevelPath => "Levels";

        public IVirtualWindow Window { get; }
        readonly UiController _menu;
        public GridCamera Camera { get; }
        readonly Controller _controller;
        readonly Frame _editor, _endGame;
        public MouseButton PlaceButton { get; } = MouseButton.Right;
        public MouseButton SelectButton { get; } = MouseButton.Left;
        public Key DeleteButton { get; } = Key.Delete;
        SceneController _sceneController;
        public SceneBuilder Scene => _sceneChanges[_sceneChangeCurrent];
        bool _isPlaying => _sceneController != null;
        Vector2 _mousePosition;
        Vector2i _mouseGridPos => (Vector2i)_mousePosition.Floor(Vector2.One);
        List<SceneBuilder> _sceneChanges = new List<SceneBuilder>();
        int _sceneChangeCurrent;
        ITool _tool;
        readonly ImmutableDictionary<Hotkey, Action> _hotkeys;

        public EditorController(IVirtualWindow window, Controller controller)
        {
            _hotkeys = new Dictionary<Hotkey, Action>
            {
                { new Hotkey(Key.Number1), () => _tool = new WallTool(this) },
                { new Hotkey(Key.Number2), () => _tool = new ExitTool(this) },
                { new Hotkey(Key.Number3), () => _tool = new PlayerTool(this) },
                { new Hotkey(Key.Number4), () => _tool = new BlockTool(this) },
                { new Hotkey(Key.Number5), () => _tool = new PortalTool(this) },
                { new Hotkey(Key.Number6), () => _tool = new LinkTool(this) },
                { new Hotkey(Key.P, true), Play}
            }.ToImmutableDictionary();

            _sceneChanges.Add(new SceneBuilder());

            Window = window;
            _controller = controller;
            _tool = new WallTool(this);

            _menu = new UiController(Window);

            _menu.Root = new Frame(out Frame rootFrame)
            {
                new Frame(out _editor, new Transform2())
                {
                    new Button(new Transform2(new Vector2(10, 10)), new Vector2(200, 90), Save)
                    {
                        new TextBlock(new Transform2(new Vector2(10, 10)), Window.Fonts.Inconsolata, "Save As...")
                    },
                    new Button(new Transform2(new Vector2(10, 110)), new Vector2(200, 90), Load)
                    {
                        new TextBlock(new Transform2(new Vector2(10, 10)), Window.Fonts.Inconsolata, "Load")
                    },
                    new Button(new Transform2(new Vector2(10, 210)), new Vector2(200, 90), Play)
                    {
                        new TextBlock(new Transform2(new Vector2(10, 10)), Window.Fonts.Inconsolata, "Play")
                    },
                    new TextBox(
                        new Transform2(new Vector2(220, 10)), 
                        new Vector2(200, 90), 
                        Window.Fonts.Inconsolata, 
                        TimeOffsetGetText, 
                        TimeOffsetSetText)
                },
                new Frame(out _endGame, new Transform2(), true)
                {
                    new Button(out Button returnButton, new Transform2(new Vector2(10, 10)), new Vector2(200, 90))
                    {
                        new TextBlock(new Transform2(new Vector2(10, 10)), Window.Fonts.Inconsolata, "Return to editor")
                    },
                    new Button(
                        new Transform2(new Vector2(10, 110)), 
                        new Vector2(200, 90), 
                        () => _sceneController.SetInput(_sceneController.Input.Clear()))
                    {
                        new TextBlock(new Transform2(new Vector2(10, 10)), Window.Fonts.Inconsolata, "Restart")
                    }
                }
            };

            returnButton.OnClick += () =>
            {
                _editor.Hidden = false;
                _endGame.Hidden = true;
                _sceneController = null;
            };

            Camera = new GridCamera(new Transform2(), (float)Window.CanvasSize.XRatio);
            Camera.WorldTransform = Camera.WorldTransform.WithSize(15);
        }

        void Play()
        {
            if (Scene.Entities.OfType<Player>().Any())
            {
                _editor.Hidden = true;
                _endGame.Hidden = false;
                _sceneController = new SceneController(Window, new[] { Scene.CreateScene() });
            }
        }

        void Save()
        {
            var filepath = Path.Combine(LevelPath, "Saved.xml");
            Directory.CreateDirectory(LevelPath);

            File.WriteAllText(filepath, Serializer.Serialize(Scene));
        }

        void Load()
        {
            var filepath = Path.Combine(LevelPath, "Saved.xml");
            if (File.Exists(filepath))
            {
                _sceneChanges = new[]
                {
                    Serializer.Deserialize<SceneBuilder>(File.ReadAllText(filepath))
                }.ToList();
                _sceneChangeCurrent = 0;
            }
        }

        string TimeOffsetGetText()
        {
            if (_tool is PortalTool portalTool)
            {
                var link = LinkTool.GetLink(LinkTool.SelectedPortal(Scene), Scene.Links);
                return link?.TimeOffset.ToString() ?? "";
            }
            return "";
        }

        void TimeOffsetSetText(string newText)
        {
            if (_tool is PortalTool portalTool)
            {
                var link = LinkTool.GetLink(LinkTool.SelectedPortal(Scene), Scene.Links);
                if (link != null)
                {
                    if (int.TryParse(newText, out int newValue))
                    {
                        var newLinks = Scene.Links.Remove(link).Add(new PortalLink(link.Portals, newValue));
                        ApplyChanges(Scene.With(links: newLinks));
                    }
                }
            }
        }

        public void Update(double timeDelta)
        {
            _mousePosition = Window.MouseWorldPos(Camera);

            _menu.Update(1);
            if (_isPlaying)
            {
                _sceneController.Update(timeDelta);
            }
            else
            {
                if (_menu.Hover == null)
                {
                    var v = MoveInput.CreateFromKeyboard(Window)?.Direction.Value.Vector;
                    if (v != null)
                    {
                        Camera.WorldTransform = Camera.WorldTransform.AddPosition((Vector2)v * 3);
                    }

                    _tool.Update();

                    if (Window.ButtonDown(KeyBoth.Control))
                    {
                        if (Window.ButtonPress(Key.Y) || (Window.ButtonDown(KeyBoth.Shift) && Window.ButtonPress(Key.Z)))
                        {
                            RedoChanges();
                        }
                        else if (Window.ButtonPress(Key.Z))
                        {
                            UndoChanges();
                        }
                    }

                    _hotkeys
                        .Where(item => Window.HotkeyPress(item.Key))
                        .ForEach(item => item.Value());
                }
            }
        }

        public static void DeleteAndSelect(IEditorController editor, Vector2i mouseGridPos)
        {
            if (editor.Window.ButtonPress(editor.DeleteButton))
            {
                editor.ApplyChanges(Remove(editor.Scene, mouseGridPos));
            }
            if (editor.Window.ButtonPress(editor.SelectButton))
            {
                editor.ApplyChanges(editor.Scene.With(mouseGridPos), true);
            }
        }

        public static SceneBuilder Remove(SceneBuilder scene, Vector2i v)
        {
            var walls = scene.Walls.Where(item => item != v).ToHashSet();
            return scene.With(
                walls, 
                scene.Exits.Where(item => item != v).ToHashSet(), 
                scene.Entities.Where(item => item.StartTransform.Position != v), 
                GetPortals(portal => portal.Position != v && PortalValidSides(portal.Position, walls).Any(), scene.Links));
        }

        /// <summary>
        /// Returns portals colliding with the given portal. This method assumes portals are in valid positions.
        /// </summary>
        public static List<PortalBuilder> PortalCollisions(PortalBuilder portal, IEnumerable<PortalBuilder> portals)
        {
            var collisionPlaces = new[]
            {
                portal.Position + portal.Direction.Vector.PerpendicularLeft,
                portal.Position + portal.Direction.Vector.PerpendicularRight
            };

            return portals
                .Where(item =>
                    item.Position == portal.Position ||
                    (item.Direction.Vector == portal.Direction.Vector && collisionPlaces.Contains(item.Position)))
                .ToList();
        }

        /// <summary>
        /// Returns portals that meet some criteria while preserving the links they are in.
        /// </summary>
        public static ImmutableList<PortalLink> GetPortals(Func<PortalBuilder, bool> selector, IEnumerable<PortalLink> links)
        {
            return links
                .Select(item =>
                    new PortalLink(
                        item.Portals
                            .Where(linkPortal => selector(linkPortal))
                            .ToArray(),
                        item.TimeOffset))
                .Where(item => item.Portals.Length > 0)
                .ToImmutableList();
        }

        public static HashSet<GridAngle> PortalValidSides(Vector2i worldPosition, ISet<Vector2i> walls)
        {
            var validSides = new HashSet<GridAngle>();
            if (walls.Contains(worldPosition))
            {
                return validSides;
            }

            var up = worldPosition + new Vector2i(0, 1);
            var down = worldPosition + new Vector2i(0, -1);
            var left = worldPosition + new Vector2i(-1, 0);
            var right = worldPosition + new Vector2i(1, 0);

            var column = new[] { new Vector2i(0, -1), new Vector2i(), new Vector2i(0, 1) };
            var row = new[] { new Vector2i(-1, 0), new Vector2i(), new Vector2i(1, 0) };

            if (new[] { up, down }.All(item => !walls.Contains(item)))
            {
                if (column.Select(item => item + left).All(item => walls.Contains(item)))
                {
                    validSides.Add(GridAngle.Left);
                }
                if (column.Select(item => item + right).All(item => walls.Contains(item)))
                {
                    validSides.Add(GridAngle.Right);
                }
            }
            else if (new[] { left, right }.All(item => !walls.Contains(item)))
            {
                if (row.Select(item => item + up).All(item => walls.Contains(item)))
                {
                    validSides.Add(GridAngle.Up);
                }
                if (row.Select(item => item + down).All(item => walls.Contains(item)))
                {
                    validSides.Add(GridAngle.Down);
                }
            }
            return validSides;
        }

        public void ApplyChanges(SceneBuilder newScene, bool undoSkip = false)
        {
            DebugEx.Assert(
                newScene.Links
                    .SelectMany(item => item.Portals)
                    .GroupBy(item => item)
                    .All(item => item.Count() == 1), 
                "There should not be any duplicate portals.");
            _sceneChangeCurrent++;
            _sceneChanges.RemoveRange(_sceneChangeCurrent, _sceneChanges.Count - _sceneChangeCurrent);
            _sceneChanges.Add(newScene);
        }

        public void UndoChanges()
        {
            ResetTool();
            if (_sceneChangeCurrent > 0)
            {
                _sceneChangeCurrent--;
            }
        }

        public void RedoChanges()
        {
            ResetTool();
            if (_sceneChangeCurrent + 1 < _sceneChanges.Count)
            {
                _sceneChangeCurrent++;
            }
        }

        void ResetTool()
        {
            _tool = (ITool)Activator.CreateInstance(_tool.GetType(), this);
        }

        public void Render(double timeDelta)
        {
            if (_isPlaying)
            {
                _sceneController.Render(timeDelta);
            }
            else
            {
                var scene = Scene.CreateScene();

                var layer = new Layer
                {
                    Camera = Camera,
                    Renderables = SceneRender.RenderInstant(scene, scene.GetSceneInstant(0), 1, scene.Portals, Window.Fonts.Inconsolata)
                        .Cast<IRenderable>()
                        .ToList()
                };

                layer.Renderables.AddRange(_tool.Render());
                Window.Layers.Add(layer);
            }

            var gui = _menu.Render();
            //gui.Renderables.Add(Draw.Text(Window.Fonts.Inconsolata, new Vector2(0, 130), _mouseGridPos.ToString()));
            Window.Layers.Add(gui);
        }
    }
}
