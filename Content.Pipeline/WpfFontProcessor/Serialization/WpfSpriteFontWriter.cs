using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using tainicom.Aether.Content.Pipeline.WpfSpriteFont;

namespace tainicom.Aether.Content.Pipeline.Serialization
{
    [ContentTypeWriter]
    class WpfSpriteFontWriter : ContentTypeWriter<WpfSpriteFontContent>
    {
        protected override void Write(ContentWriter output, WpfSpriteFontContent value)
        {
            output.WriteObject(value.Texture);
            output.WriteObject(value.Glyphs);
            output.WriteObject(value.Cropping);
            output.WriteObject(value.CharacterMap);
            output.Write(value.VerticalLineSpacing);
            output.Write(value.HorizontalSpacing);
            output.WriteObject(value.Kerning);
            output.Write(value.DefaultCharacter.HasValue);

            if (value.DefaultCharacter.HasValue)
            {
                output.Write(value.DefaultCharacter.Value);
            }
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return "Microsoft.Xna.Framework.Content.SpriteFontReader, " +
                   "Microsoft.Xna.Framework.Graphics, Version=4.0.0.0, " +
                   "Culture=neutral, PublicKeyToken=842cf8be1de50553";
        }
    }
}
