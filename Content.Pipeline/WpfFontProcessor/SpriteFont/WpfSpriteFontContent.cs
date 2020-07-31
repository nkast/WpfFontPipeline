using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using System.Collections.Generic;

namespace tainicom.Aether.Content.Pipeline.WpfSpriteFont
{
    public class WpfSpriteFontContent
    {
        internal Texture2DContent Texture;
        internal readonly List<Rectangle> Glyphs;
        internal readonly List<Rectangle> Cropping;
        internal readonly List<char> CharacterMap;
        internal readonly List<Vector3> Kerning;
        internal int VerticalLineSpacing;
        internal float HorizontalSpacing;

        [ContentSerializer(AllowNull = true)]
        public char? DefaultCharacter;

        internal WpfSpriteFontContent()
        {
            Texture = new Texture2DContent();
            Glyphs = new List<Rectangle>();
            Cropping = new List<Rectangle>();
            CharacterMap = new List<char>();
            Kerning = new List<Vector3>();
        }
    }
}