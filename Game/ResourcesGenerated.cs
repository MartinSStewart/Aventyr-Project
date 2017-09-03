// This code was autogenerated.

using Game.Rendering;
using System.Linq;

namespace Game
{
    public partial class Resources
    {
        public Font @LatoItalic => Fonts.Single(item => item.FontData.Info.Face == "LatoItalic");
        public Font @LatoRegular => Fonts.Single(item => item.FontData.Info.Face == "LatoRegular");

        public AtlasTexture @Box => Textures.Single(item => item.Name == "Box");
        public AtlasTexture @Default => Textures.Single(item => item.Name == "Default");
        public AtlasTexture @Floor => Textures.Single(item => item.Name == "Floor");
        public AtlasTexture @Grid => Textures.Single(item => item.Name == "Grid");
        public AtlasTexture @LineBlur => Textures.Single(item => item.Name == "LineBlur");
        public AtlasTexture @Wall => Textures.Single(item => item.Name == "Wall");
        public AtlasTexture @WallFade => Textures.Single(item => item.Name == "WallFade");

    }
}