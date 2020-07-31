using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using tainicom.Aether.Content.Pipeline.WpfSpriteFont;
using Color = Microsoft.Xna.Framework.Color;

namespace tainicom.Aether.Content.Pipeline.Processors
{
	[ContentProcessor(DisplayName = "WPF Sprite Font Description")]
	public class WpfFontDescriptionProcessor : ContentProcessor<FontDescription, WpfSpriteFontContent>
	{
		//[DisplayName("")]
		//[Description("")]
		[DefaultValue(0)]
		public float OutlineThickness { get; set; }

		//[DisplayName("")]
		//[Description("")]
		[DefaultValue(typeof(Color), "64, 64, 64, 255")]
		public Color OutlineColor { get; set; }

		//[DisplayName("")]
		//[Description("")]
		[DefaultValue(PenLineJoin.Miter)]
		public PenLineJoin OutlineShape { get; set; }

		//[DisplayName("")]
		//[Description("")]
		[DefaultValue(OutlineStroke.StrokeOverFill)]
		public OutlineStroke OutlineStroke { get; set; }

		//[DisplayName("")]
		//[Description("")]
		[DefaultValue(typeof(Color), "255, 255, 255, 255")]
		public Color FontColor { get; set; }

		//[DisplayName("")]
		//[Description("")]
		[DefaultValue(false)]
		public bool UseGradient { get; set; }

		//[DisplayName("")]
		//[Description("")]
		[DefaultValue(typeof(Color), "64, 128, 255, 255")]
		public Color GradientBeginColor { get; set; }

		//[DisplayName("")]
		//[Description("")]
		[DefaultValue(typeof(Color), "0, 0, 128, 255")]
		public Color GradientEndColor { get; set; }

		//[DisplayName("")]
		//[Description("")]
		[DefaultValue(90)]
		public int GradientAngle { get; set; }

		//[DisplayName("")]
		//[Description("")]
		[DefaultValue(WpfTextureFormat.Auto)]
		public WpfTextureFormat TextureFormat { get; set; }

		public WpfFontDescriptionProcessor()
		{
			OutlineColor = new Color(64, 64, 64, 255);
			OutlineShape = PenLineJoin.Miter;
			OutlineStroke = OutlineStroke.StrokeOverFill;
			FontColor = new Color(255, 255, 255, 255);
			GradientBeginColor = new Color(64, 128, 255, 255);
			GradientEndColor = new Color(0, 0, 128, 255);
			GradientAngle = 90;
		}

		public override WpfSpriteFontContent Process(FontDescription input, ContentProcessorContext context)
		{
			if (input.DefaultCharacter.HasValue)
				input.Characters.Add(input.DefaultCharacter.Value);

			CreateWpfFont(input, context);

			var fontContent = new WpfSpriteFontContent();

			fontContent.VerticalLineSpacing = (int)(glyphTypeface.Height * fontSize);
			fontContent.HorizontalSpacing = input.Spacing;
			fontContent.DefaultCharacter = input.DefaultCharacter;

			var layouter = new BoxLayouter();

			ProcessGlyphs(input, context, fontContent, layouter);
			ProcessTexture(fontContent, layouter);

			return fontContent;
		}

		void CreateWpfFont(FontDescription input, ContentProcessorContext context)
		{
			fontSize = (float)(input.Size * (WpfDiu / 72.0));

            var fontWeight = ((input.Style & FontDescriptionStyle.Regular) == 
                FontDescriptionStyle.Regular) ? FontWeights.Regular : FontWeights.Bold;

            var fontStyle = ((input.Style & FontDescriptionStyle.Italic) ==
				FontDescriptionStyle.Italic) ? FontStyles.Italic : FontStyles.Normal;

			typeface = new Typeface(new FontFamily(input.FontName),
									fontStyle, fontWeight, FontStretches.Normal);

			if (typeface == null)
			{
				new PipelineException(
					"フォント\"{0}\"の生成に失敗しました。" +
					"指定されたフォントがインストールされているか確認してください。");
			}

			// GlyphTypefaceの取得
			if (typeface.TryGetGlyphTypeface(out glyphTypeface) == false)
			{
				throw new PipelineException(
					"フォント\"{0}\"のGlyphTypeface生成に失敗しました。");
			}
		}

		void ProcessGlyphs(FontDescription input, ContentProcessorContext context, WpfSpriteFontContent fontContent, BoxLayouter layouter)
		{
			if (UseGradient)
			{
				textBrush = new LinearGradientBrush(
					ToWpfColor(this.GradientBeginColor),
					ToWpfColor(this.GradientEndColor),
					GradientAngle);
			}
			else
			{
				textBrush = new SolidColorBrush(ToWpfColor(FontColor));
			}

			if (OutlineThickness > 0)
			{
				outlinePen = new Pen(new SolidColorBrush(ToWpfColor(OutlineColor)),
									OutlineThickness);
				outlinePen.LineJoin = OutlineShape;
			}
			else
			{
				outlinePen = null;
			}

			renderTarget = null;
			drawingVisual = new DrawingVisual();

			var characters = from c in input.Characters orderby c select c;

			GlyphKerning glyphKerning = new GlyphKerning();
			var kerning = Vector3.Zero;

			foreach (char ch in characters)
			{
				GetKerning(ch, out glyphKerning);

				var glyphBounds = RenderCharacter(ch);

				int stride = renderTarget.PixelWidth;
				uint[] pixels = new uint[stride * renderTarget.PixelHeight];
				renderTarget.CopyPixels(pixels, stride * sizeof(uint), 0);

				var narrowerBounds = NarrowerGlyph(pixels, stride, glyphBounds);
				var blackBox = GetBlackBox(pixels, stride, narrowerBounds);
				pixels = new uint[blackBox.Width * blackBox.Height];
				renderTarget.CopyPixels(ToInt32Rect(blackBox), pixels, blackBox.Width * sizeof(uint), 0);

				kerning.X = SnapPixel(glyphKerning.LeftSideBearing);
				kerning.Y = SnapPixel(glyphKerning.AdvanceWidth);
				kerning.Z = SnapPixel(glyphKerning.RightSideBearing);

				kerning.Y = blackBox.Width;

				fontContent.CharacterMap.Add(ch);
				fontContent.Kerning.Add(kerning);
				fontContent.Glyphs.Add(new Rectangle( 0, 0, blackBox.Width, blackBox.Height));
				fontContent.Cropping.Add(new Rectangle(blackBox.X, blackBox.Y, glyphBounds.Width, glyphBounds.Height));

				layouter.Add(new BoxLayoutItem { Bounds = blackBox, Tag = pixels });
			}

			return;
		}

		/// <param name="layouter"></param>
		void ProcessTexture(WpfSpriteFontContent fontContent, BoxLayouter layouter)
		{
			int width, height;
			layouter.Layout(out width, out height);

			var bitmap = new PixelBitmapContent<Color>(width, height);
			for (int i = 0; i < layouter.Items.Count; ++i)
			{
				var rc = fontContent.Glyphs[i];
				rc.X = layouter.Items[i].Bounds.X;
				rc.Y = layouter.Items[i].Bounds.Y;
				fontContent.Glyphs[i] = rc;

				var pixels = layouter.Items[i].Tag as uint[];
				int idx = 0;
				Color color = default(Color);
				for (int y = 0; y < rc.Height; ++y)
				{
					for (int x = 0; x < rc.Width; ++x)
					{
						color.B = (byte)((pixels[idx] & 0x000000ff) >> 0);
						color.G = (byte)((pixels[idx] & 0x0000ff00) >> 8);
						color.R = (byte)((pixels[idx] & 0x00ff0000) >> 16);
						color.A = (byte)((pixels[idx] & 0xff000000) >> 24);
						bitmap.SetPixel(rc.X + x, rc.Y + y, color);
						++idx;
					}
				}
			}

			fontContent.Texture = new Texture2DContent();
			switch (TextureFormat)
			{
				case WpfTextureFormat.Color:
					fontContent.Texture.Mipmaps = bitmap;
					break;
				case WpfTextureFormat.Bgra4444:
					fontContent.Texture.Mipmaps = bitmap;
					fontContent.Texture.ConvertBitmapType(typeof(PixelBitmapContent<Bgra4444>));
					break;
				case WpfTextureFormat.Auto:
					if (UseGradient)
					{
						fontContent.Texture.Mipmaps = bitmap;
					}
					else if (OutlineThickness > 0)
					{
						fontContent.Texture.Mipmaps = bitmap;
						fontContent.Texture.ConvertBitmapType(typeof(PixelBitmapContent<Bgra4444>));
					}
					else
					{
						fontContent.Texture.Mipmaps =
							SingleColorDxtCompressor.Compress(bitmap, FontColor);
					}
					break;
			}

		}

		protected struct GlyphKerning
		{
			public double LeftSideBearing;
			public double RightSideBearing;
			public double AdvanceWidth;
		}

		protected void GetKerning(char ch, out GlyphKerning glyphKerning)
		{			
			ushort glyphIdx;
			glyphTypeface.CharacterToGlyphMap.TryGetValue(ch, out glyphIdx);
			{
                glyphKerning.LeftSideBearing = glyphTypeface.LeftSideBearings[glyphIdx] * fontSize;
                glyphKerning.RightSideBearing = glyphTypeface.RightSideBearings[glyphIdx] * fontSize;
                glyphKerning.AdvanceWidth = glyphTypeface.AdvanceWidths[glyphIdx] * fontSize;

				//var advanceHeights = glyphTypeface.AdvanceHeights[glyphIdx];
				//var bottomSideBearings = glyphTypeface.BottomSideBearings[glyphIdx];
				//var distancesFromHorizontalBaselineToBlackBoxBottom = glyphTypeface.DistancesFromHorizontalBaselineToBlackBoxBottom[glyphIdx];
				//advanceHeights *= fontSize;
				//bottomSideBearings *= fontSize;
				//distancesFromHorizontalBaselineToBlackBoxBottom *= fontSize;
			}
		}

		static float SnapPixel(double value)
		{
			var bias = 0.0937456;

			if (value > 0)
				return (float)Math.Floor(value + bias);

			return (float)Math.Ceiling(value - bias);
		}

		void EnsureRenderTargetSize(int width, int height)
		{
			if (width <= 0)
				throw new ArgumentException("width");
			if (width <= 0)
				throw new ArgumentException("height");

			if (renderTarget == null ||
		        renderTarget.Width < width || renderTarget.Height < height)
			{
				width  = (int)Math.Ceiling(width *1.5);
				height = (int)Math.Ceiling(height * 1.5);
				width  = (width  + 31) & (~31);
				height = (height + 31) & (~31);
								
				renderTarget = new RenderTargetBitmap(
					width, height,
					WpfDiu, WpfDiu,
					PixelFormats.Pbgra32);
			}
		}

		protected virtual Rectangle RenderCharacter(char ch)
		{
			var formattedText = new FormattedText(
					ch.ToString(), CultureInfo.CurrentCulture,
					FlowDirection.LeftToRight, typeface, fontSize, textBrush);

			var width  = Math.Max((int)Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace + OutlineThickness), 1);
			var height = Math.Max((int)Math.Ceiling(formattedText.Height + OutlineThickness), 1);
			EnsureRenderTargetSize(width, height);

			int fontWidth  = Math.Max((int)Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace), 1);
			int fontHeight = Math.Max((int)Math.Ceiling(formattedText.Height), 1);
			Rectangle rc = new Rectangle(0, 0, fontWidth, fontHeight);

			using (DrawingContext dc = drawingVisual.RenderOpen())
			{
				var pos = new System.Windows.Point(rc.X, rc.Y);
				if (outlinePen != null)
				{
					var geometry = formattedText.BuildGeometry(pos);

					switch (OutlineStroke)
					{
						case OutlineStroke.StrokeOverFill:
							dc.DrawGeometry(textBrush, outlinePen, geometry);
							break;
						case OutlineStroke.FillOverStroke:
							dc.DrawGeometry(null, outlinePen, geometry);
							dc.DrawGeometry(textBrush, null, geometry);
							break;
						case OutlineStroke.StrokeOnly:
							dc.DrawGeometry(null, outlinePen, geometry);
							break;
					}
				}
				else
				{
					dc.DrawText(formattedText, pos);
				}
			}
			renderTarget.Clear();
			renderTarget.Render(drawingVisual);

			return rc;
		}

		Rectangle NarrowerGlyph(uint[] pixels, int stride, Rectangle bounds)
		{
			int left = bounds.X;
			int right = bounds.Right - 1;
			int width = renderTarget.PixelWidth;
			int height = renderTarget.PixelHeight;

			while (left > 0 && !IsEmptyColumn(pixels, stride, left, 0, height))
				left--;

			while ((left < right) && IsEmptyColumn(pixels, stride, left, 0, height))
				left++;

			while ((right < width) && !IsEmptyColumn(pixels, stride, right, 0, height))
				right++;

			right = Math.Min(right, width - 1);

			while ((right > left) && IsEmptyColumn(pixels, stride, right, 0, height))
				right--;

			bounds.X = left;
			bounds.Width = right - left + 1;

			return bounds;
		}

		Rectangle GetBlackBox(uint[] pixels, int stride, Rectangle bounds)
		{
			int x1 = bounds.X;
			int x2 = bounds.Right;
			int top = bounds.Y;
			int bottom = bounds.Bottom - 1;
			int height = renderTarget.PixelHeight;

			while ((0 < top) && !IsEmptyLine(pixels, stride, top, x1, x2))
				top--;

			while ((top < bottom) && IsEmptyLine(pixels, stride, top, x1, x2))
				top++;

			while ((bottom < height) && !IsEmptyLine(pixels, stride, bottom, x1, x2))
				bottom++;

			bottom = Math.Min(bottom, height - 1);

			while ((bottom > top) && IsEmptyLine(pixels, stride, bottom, x1, x2))
				bottom--;

			bounds.Y = top;
			bounds.Height = bottom - top + 1;

			return bounds;
		}

		static bool IsEmptyColumn(uint[] pixels, int stride, int x, int y1, int y2)
		{
			var idx = y1 * stride + x;
			for (int y = y1; y < y2; ++y, idx += stride)
			{
				if (pixels[idx] != TransparentPixel)
					return false;
			}

			return true;
		}

		static bool IsEmptyLine(uint[] pixels, int stride, int y, int x1, int x2)
		{
			var idx = y * stride + x1;
			for (int x = x1; x < x2; ++x, ++idx)
			{
				if (pixels[idx] != TransparentPixel)
					return false;
			}

			return true;
		}

		static System.Windows.Media.Color ToWpfColor(Color color)
		{
			return
				System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
		}

		static Int32Rect ToInt32Rect(Rectangle rc)
		{
			return new Int32Rect(rc.X, rc.Y, rc.Width, rc.Height);
		}

		// WPF DIU (Device Independent Unit)
		const double WpfDiu = 96;

		const uint TransparentPixel = 0;

		Typeface typeface;
		GlyphTypeface glyphTypeface;
		float fontSize;

		Brush textBrush;

		Pen outlinePen;

		RenderTargetBitmap renderTarget;

		DrawingVisual drawingVisual;

	}
}