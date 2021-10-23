using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;

namespace Project.Render {
	public class Framebuffer {
		public int FramebufferID { get; private set; }
		public Texture Depth { get; private set; } = null;
		private List<Texture> _bufferTextures = new List<Texture>();

		public Framebuffer() {
			FramebufferID = GL.GenFramebuffer();
			Use();
		}

		public Framebuffer(int id) {
			FramebufferID = id;
			Use();
		}

		public Framebuffer SetDepthBuffer(PixelInternalFormat depthComponent) {
			Depth = new Texture(GL.GenTexture());
			GL.BindTexture(TextureTarget.Texture2D, Depth.TextureID);
			GL.TexImage2D(TextureTarget.Texture2D, 0, depthComponent, Renderer.INSTANCE.Size.X,
						Renderer.INSTANCE.Size.Y, 0, PixelFormat.DepthComponent, PixelType.UnsignedByte, new byte[0]);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
									TextureTarget.Texture2D, Depth.TextureID, 0);
			return this;
		}

		public Texture GetAttachment(int attachment) {
			return _bufferTextures[attachment];
		}

		public Framebuffer AddAttachment() {
			return AddAttachment(PixelInternalFormat.Rgba, PixelFormat.Rgba);
		}

		public Framebuffer AddAttachments(int count) {
			for (int i = 0; i < count; i++)
				AddAttachment();
			return this;
		}

		public Framebuffer AddAttachment(PixelInternalFormat internalFormat, PixelFormat externalFormat) {
			int attachment = _bufferTextures.Count;
			Texture buffer = new Texture(GL.GenTexture());
			GL.BindTexture(TextureTarget.Texture2D, buffer.TextureID);
			GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, Renderer.INSTANCE.Size.X,
						Renderer.INSTANCE.Size.Y, 0, externalFormat, PixelType.UnsignedByte, new byte[0]);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + attachment,
									TextureTarget.Texture2D, buffer.TextureID, 0);

			_bufferTextures.Add(buffer);

			DrawBuffersEnum[] colorAttachments = new DrawBuffersEnum[_bufferTextures.Count];
			for (int i = 0; i < _bufferTextures.Count; i++) {
				colorAttachments[i] = DrawBuffersEnum.ColorAttachment0 + i;
			}

			GL.DrawBuffers(colorAttachments.Length, colorAttachments);
			return this;
		}

		public Framebuffer Use() {
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferID);
			return this;
		}

		public Framebuffer Reset() {
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			return this;
		}
	}
}