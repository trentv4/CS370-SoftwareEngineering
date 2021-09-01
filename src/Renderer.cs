using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Project {
	public class Renderer : GameWindow {
		public Renderer(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }

		protected override void OnRenderThreadStarted() {

		}

		protected override void OnRenderFrame(FrameEventArgs args) {

		}

		protected override void OnUpdateFrame(FrameEventArgs args) {
			Program.Update();
		}
	}
}
