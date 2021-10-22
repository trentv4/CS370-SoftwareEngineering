using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using System.Runtime.InteropServices;
using Project.Levels;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Project.Render {
	public class Framebuffer {
		public int FramebufferID { get; private set; }
		public Texture Depth { get; private set; } = null;
		private List<DrawBuffersEnum> _colorAttachments = new List<DrawBuffersEnum>();
		private List<Texture> _bufferTextures = new List<Texture>();

		public Framebuffer() {
			FramebufferID = GL.GenFramebuffer();
			Use();
		}

		public Framebuffer SetDepthBuffer() {
			Depth = new Texture(GL.GenTexture());
			GL.BindTexture(TextureTarget.Texture2D, Depth.TextureID);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent24, Renderer.INSTANCE.Size.X,
						Renderer.INSTANCE.Size.Y, 0, PixelFormat.DepthComponent, PixelType.UnsignedByte, new byte[0]);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
									TextureTarget.Texture2D, Depth.TextureID, 0);
			return this;
		}

		public Framebuffer SetAttachment(int attachment, PixelInternalFormat internalFormat, PixelFormat externalFormat) {
			Texture buffer = new Texture(GL.GenTexture());
			GL.BindTexture(TextureTarget.Texture2D, buffer.TextureID);
			GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, Renderer.INSTANCE.Size.X,
						Renderer.INSTANCE.Size.Y, 0, externalFormat, PixelType.UnsignedByte, new byte[0]);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + attachment,
									TextureTarget.Texture2D, buffer.TextureID, 0);

			if (_bufferTextures.Count > attachment) {
				_bufferTextures[attachment] = buffer;
				_colorAttachments[attachment] = DrawBuffersEnum.ColorAttachment0 + attachment;
			} else {
				_bufferTextures.Add(buffer);
				_colorAttachments.Add(DrawBuffersEnum.ColorAttachment0 + attachment);
			}

			GL.DrawBuffers(_colorAttachments.Count, _colorAttachments.ToArray());
			return this;
		}

		public void Use() {
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferID);
		}
	}
}