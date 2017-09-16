﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Common;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using OpenTK;

namespace Game.Rendering
{
    [DataContract]
    public class AtlasTexture : ITexture
    {
        [DataMember]
        public TextureFile Texture { get; private set; }
        [DataMember]
        public bool IsTransparent { get; private set; }
        [DataMember]
        public string Name { get; private set; }
        [DataMember]
        public bool MirrorY { get; private set; }
        public int Id => Texture.Id;
        public RectangleF UvBounds => 
            new RectangleF(
                new Vector2(
                    Position.X / (float)Texture.Size.X,
                    (Position.Y + (MirrorY ? Size.Y : 0)) / (float)Texture.Size.Y),
                new Vector2(
                    Size.X / (float)Texture.Size.X,
                    Size.Y / (float)Texture.Size.Y * (MirrorY ? -1 : 1)));
        [DataMember]
        public Vector2i Position { get; private set; }
        [DataMember]
        public Vector2i Size { get; private set; }

        public AtlasTexture(TextureFile texture, Vector2i position, Vector2i size, bool isTranparent, string name, bool mirrorY = false)
        {
            Texture = texture;
            Position = position;
            Size = size;
            IsTransparent = isTranparent;
            Name = name;
            MirrorY = mirrorY;
        }
    }
}