using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;
using System.Collections.Generic;

namespace Project.Render {
	public class Texture {
		public readonly int TextureID;
		private readonly float _maxAnisotrophy = GL.GetFloat((GetPName)All.MaxTextureMaxAnisotropy);
		private static readonly Dictionary<string, int> _loadedTextures = new Dictionary<string, int>();

		public Texture(string location) {
			if (_loadedTextures.ContainsKey(location)) {
				this.TextureID = _loadedTextures[location];
			} else {
				ImageResult image;
				using (FileStream stream = File.OpenRead(location)) {
					image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
				}

				float anisotropicLevel = MathHelper.Clamp(16, 1f, _maxAnisotrophy);

				TextureID = GL.GenTexture();
				GL.BindTexture(TextureTarget.Texture2D, TextureID);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
				GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)All.TextureMaxAnisotropy, anisotropicLevel);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
							  image.Width, image.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
				GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

				_loadedTextures.Add(location, TextureID);
			}
		}

		public Texture(string location, TextureMinFilter filter) {
			if (_loadedTextures.ContainsKey(location)) {
				this.TextureID = _loadedTextures[location];
			} else {
				ImageResult image;
				using (FileStream stream = File.OpenRead(location)) {
					image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
				}

				float anisotropicLevel = MathHelper.Clamp(16, 1f, _maxAnisotrophy);

				TextureID = GL.GenTexture();
				GL.BindTexture(TextureTarget.Texture2D, TextureID);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)filter);
				GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)All.TextureMaxAnisotropy, anisotropicLevel);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
							  image.Width, image.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
				GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

				_loadedTextures.Add(location, TextureID);
			}
		}
	}
}