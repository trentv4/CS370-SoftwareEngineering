using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;
using System.Collections.Generic;
using System;

namespace Project.Render {
	/// <summary> Wrapper class for OpenGL textures. This allows for loading textures multiple times by retreiving loaded textures from cache. </summary>
	public class Texture {
		/// <summary> OpenGL texture ID, retreived from a GL call. This changes from execution to execution. </summary>
		public readonly int TextureID;

		private static readonly Dictionary<string, int> _loadedTextures = new Dictionary<string, int>();
		private static readonly float _anisotropicLevel = MathHelper.Clamp(16, 1f, GL.GetFloat((GetPName)All.MaxTextureMaxAnisotropy));

		/// <summary> Loads a texture from disk. If it has already been loaded, it will be retrieved from cache. </summary>
		public Texture(string location) {
			if (_loadedTextures.ContainsKey(location)) {
				this.TextureID = _loadedTextures[location];
			} else {
				ImageResult image;
				StbImage.stbi__vertically_flip_on_load = 1;
				using (FileStream stream = File.OpenRead(location)) {
					image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
				}

				TextureID = GL.GenTexture();
				GL.BindTexture(TextureTarget.Texture2D, TextureID);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
				GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)All.TextureMaxAnisotropy, _anisotropicLevel);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
							  image.Width, image.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
				GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

				_loadedTextures.Add(location, TextureID);
			}
		}

		/// <summary> Loads a texture from disk. This will be cached separately from regular textures due to the specific TextureMinFilter applied. </summary>
		public Texture(string diskLocation, TextureMinFilter filter) {
			string location = $"{diskLocation}-{filter.ToString()}";
			if (_loadedTextures.ContainsKey(location)) {
				this.TextureID = _loadedTextures[location];
			} else {
				ImageResult image;
				using (FileStream stream = File.OpenRead(diskLocation)) {
					image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
				}

				TextureID = GL.GenTexture();
				GL.BindTexture(TextureTarget.Texture2D, TextureID);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)filter);
				GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)All.TextureMaxAnisotropy, _anisotropicLevel);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
							  image.Width, image.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
				GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

				_loadedTextures.Add(location, TextureID);
			}
		}
	}
}