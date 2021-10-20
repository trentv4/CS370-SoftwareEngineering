using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;
using System.Collections.Generic;

namespace Project.Render {
	/// <summary> Wrapper class for OpenGL textures. This allows for loading textures multiple times by retreiving loaded textures from cache. </summary>
	public class Texture {
		/// <summary> OpenGL texture ID, retreived from a GL call. This changes from execution to execution. </summary>
		public readonly int TextureID;

		private static readonly Dictionary<string, int> _textureCache = new Dictionary<string, int>();
		/// <summary> Parameter controlling texture anisotropic filtering. This is set to 16 all the time, if supported by the GPU. </summary>
		private static readonly float _anisotropicLevel = MathHelper.Clamp(16, 1f, GL.GetFloat((GetPName)All.MaxTextureMaxAnisotropy));

		private Texture(int TextureID) {
			this.TextureID = TextureID;
		}

		/// <summary> Creates a texture with an image loaded from disk. The result is cached. </summary>
		public static Texture CreateTexture(string diskLocation) {
			return CreateTexture(diskLocation, TextureMinFilter.LinearMipmapLinear, TextureWrapMode.Repeat);
		}

		/// <summary> Creates a texture with an image loaded from disk. The result is cached. This allows for specified filtering when resized. </summary>
		public static Texture CreateTexture(string diskLocation, TextureMinFilter filter) {
			return CreateTexture(diskLocation, filter, TextureWrapMode.Repeat);
		}

		/// <summary> Creates a texture with custom settings. The result is cached. </summary>
		public static Texture CreateTexture(string diskLocation, TextureMinFilter filter, TextureWrapMode wrapMode) {
			string cacheName = $"{diskLocation}-{filter.ToString()}";
			if (_textureCache.ContainsKey(cacheName)) {
				return new Texture(_textureCache[cacheName]);
			}

			ImageResult image;
			using (FileStream stream = File.OpenRead(diskLocation)) {
				image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
			}
			return CreateTexture(cacheName, image, filter, wrapMode);
		}

		/// <summary> Creates a texture with a provided image, filter, and wrap mode. </summary>
		public static Texture CreateTexture(string cacheName, ImageResult image, TextureMinFilter filter, TextureWrapMode wrapMode) {
			if (_textureCache.ContainsKey(cacheName)) {
				return new Texture(_textureCache[cacheName]);
			}
			Texture value = new Texture(GL.GenTexture());
			GL.BindTexture(TextureTarget.Texture2D, value.TextureID);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrapMode);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrapMode);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)filter);
			GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)All.TextureMaxAnisotropy, _anisotropicLevel);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
							image.Width, image.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
			GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

			_textureCache.Add(cacheName, value.TextureID);
			return value;
		}
	}
}