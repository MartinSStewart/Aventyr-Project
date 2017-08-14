﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Common;
using Game.Models;
using OpenTK;
using Game.Rendering;
using OpenTK.Graphics;

namespace Ui
{
    public class TextBox : Element, IElement
    {
        public enum Input { Text, Numbers }

        public ElementFunc<Font> FontFunc { get; }
        public ElementFunc<Color4> BackgroundColorFunc { get; } 
        public Func<string> GetText { get; }
        public Action<string> SetText { get; }
        public Input InputType { get; set; } = Input.Numbers;
        public int CursorStart { get; set; }
        public int CursorEnd { get; set; }

        public TextBox(
            ElementFunc<float> x = null,
            ElementFunc<float> y = null,
            ElementFunc<float> width = null, 
            ElementFunc<float> height = null,
            ElementFunc<Font> font = null, 
            Func<string> getText = null, 
            Action<string> setText = null,
            ElementFunc<Color4> backgroundColor = null,
            ElementFunc<bool> hidden = null)
            : base(x, y, width, height, hidden)
        {
            FontFunc = font;

            var defaultText = "";
            GetText = getText ?? (() => defaultText);
            SetText = setText ?? (newText => defaultText = newText);

            BackgroundColorFunc = backgroundColor ?? (_ => Color4.White);
        }

        public TextBox(
            out TextBox id,
            ElementFunc<float> x = null,
            ElementFunc<float> y = null,
            ElementFunc<float> width = null, 
            ElementFunc<float> height = null,
            ElementFunc<Font> font = null, 
            Func<string> getText = null, 
            Action<string> setText = null,
            ElementFunc<Color4> backgroundColor = null,
            ElementFunc<bool> hidden = null)
            : this(x, y, width, height, font, getText, setText, backgroundColor, hidden)
        {
            id = this;
        }

        [DetectLoop]
        public Font GetFont() => FontFunc(ElementArgs);
        [DetectLoop]
        public Color4 GetBackgroundColor() => BackgroundColorFunc(ElementArgs);

        public override List<Model> GetModels()
        {
            var models = new List<Model>();
            var margin = new Vector2(2, 2);
            var size = this.GetSize();
            if (size != new Vector2())
            {
                models.AddRange(Draw.Rectangle(new Vector2(), size, Color4.Brown).GetModels());
                models.AddRange(Draw.Rectangle(margin, size - margin, GetBackgroundColor()).GetModels());
                var font = GetFont();
                var text = GetText();
                var textModel = font.GetModel(text, Color4.Black);
                textModel.Transform.Position += new Vector3(
                    margin.X, 
                    (size.Y - font.GetSize(text, new Font.Settings()).Y) / 2, 
                    0); 
                models.Add(textModel);
            }
            return models;
        }

        public override bool IsInside(Vector2 localPoint)
        {
            return MathEx.PointInRectangle(new Vector2(), this.GetSize(), localPoint);
        }

        public IEnumerator<IElement> GetEnumerator() => new List<IElement>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
