using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;

namespace Project.Render {
	/// <summary> Primary rendering class, instantiated in Program and continuously executed. OpenGL is only referenced from here and related classes. </summary>
	public class Renderer : GameWindow {
		public Renderer(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }

		private RenderableNode Scene;

		// Debug
		private static DebugProc debugCallback = DebugCallback;
		private static GCHandle debugCallbackHandle;

		public static readonly GameLogic LogicThread = new GameLogic();

		/// <summary> Handles all OpenGL setup, including shader programs, flags, attribs, etc. </summary>
		protected override void OnRenderThreadStarted() {
			// Sets required OpenGL flags
			debugCallbackHandle = GCHandle.Alloc(debugCallback);
			GL.DebugMessageCallback(debugCallback, IntPtr.Zero);
			GL.Enable(EnableCap.DebugOutput);
			GL.Enable(EnableCap.DebugOutputSynchronous);
			GL.Enable(EnableCap.DepthTest);

			ShaderProgram t = new ShaderProgram("src/render/shaders/ForwardShader_vertex.glsl",
												"src/render/shaders/ForwardShader_fragment.glsl");

			Scene = new RenderableNode();

			// Create camera
			// Create AttribPointers
		}

		/// <summary> Core render loop. Be careful to not reference anything on the logic thread from here! </summary>
		protected override void OnRenderFrame(FrameEventArgs args) {
			//GameState state = Program.LogicThread.GetGameState();

			// Clear scene
			// Use shader
			// Set uniforms
			// Set matrix
			// Draw
		}

		/// <summary> Handles all debug callbacks from OpenGL and throws exceptions if unhandled. </summary>
		private static void DebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam) {
			string messageString = Marshal.PtrToStringAnsi(message, length);
			if (type < DebugType.DebugTypeOther)
				Console.WriteLine($"{severity} {type} | {messageString}");
			if (type == DebugType.DebugTypeError)
				throw new Exception(messageString);
		}

		/// <summary> Stub method to call the external Program method, helps in isolation of logic from rendering </summary>
		protected override void OnUpdateFrame(FrameEventArgs args) {
			Program.LogicThread.Update();
		}
	}
}
