using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.InteropServices;

namespace Project.Render {
	/// <summary> Primary rendering class, instantiated in Program and continuously executed. OpenGL is only referenced from here and related classes. </summary>
	public class Renderer : GameWindow {
		public Renderer(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }
		public static Renderer INSTANCE;

		/// <summary> Radian Conversion Factor (used for degree-radian conversions). Equal to pi/180. </summary>
		internal const float RCF = 0.017453293f;

		// Render
		public ShaderProgramForwardRenderer ForwardProgram { get; private set; } // Forward rendering technique
		public ShaderProgramInterface InterfaceProgram { get; private set; } // Interface renderer (z=0)

		private RenderableNode Scene;
		private Vector3 CameraPosition = new Vector3(0, 0, -2);
		private Vector3 CameraTarget = new Vector3(0, 0, -1);
		private float CameraAngle = 90;

		// Debug
		private Model spinny;
		private Model PlayerModel;

		// OpenGL error callback
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
			GL.Viewport(0, 0, Size.X, Size.Y);

			ForwardProgram = new ShaderProgramForwardRenderer("src/render/shaders/ForwardShader_vertex.glsl",
												"src/render/shaders/ForwardShader_fragment.glsl");
			InterfaceProgram = new ShaderProgramInterface("src/render/shaders/InterfaceShader_vertex.glsl",
												"src/render/shaders/InterfaceShader_fragment.glsl");
			ForwardProgram.Use();

			Scene = new RenderableNode();
			spinny = Model.GetRoom().SetPosition(new Vector3(0, 0, 1)).SetRotation(new Vector3(135, 0, 0));
			Model plane = Model.GetUnitRectangle().SetPosition(new Vector3(0, -1, 0)).SetRotation(new Vector3(90, 0, 0)).SetScale(10);

			PlayerModel = new Model(new float[] {
				 0.0f,  0.5f,  0.0f,  1.0f, 0.0f, 0.0f,   0.0f, 0.8f, 0.8f, 0.0f,  0.0f, 0.0f,
				 0.5f,  0.0f,  0.5f,  1.0f, 0.0f, 0.0f,   0.0f, 0.0f, 0.8f, 0.0f,  0.0f, 0.0f,
				 0.5f,  0.0f, -0.5f,  1.0f, 0.0f, 0.0f,   0.0f, 0.0f, 0.8f, 0.0f,  0.0f, 0.0f,
				-0.5f,  0.0f, -0.5f,  1.0f, 0.0f, 0.0f,   0.0f, 0.0f, 0.8f, 0.0f,  0.0f, 0.0f,
				-0.5f,  0.0f,  0.5f,  1.0f, 0.0f, 0.0f,   0.0f, 0.0f, 0.8f, 0.0f,  0.0f, 0.0f,
				 0.0f,  -0.5f, 0.0f,  1.0f, 0.0f, 0.0f,   0.8f, 0.0f, 0.8f, 0.0f,  0.0f, 0.0f,
			}, new uint[] {
				0, 1, 2,
				0, 2, 3,
				0, 3, 4,
				0, 4, 1,
				5, 1, 2,
				5, 2, 3,
				5, 3, 4,
				5, 4, 1
			}).SetScale(new Vector3(0.5f, 2f, 0.5f));

			Scene.children.AddRange(new RenderableNode[] {
				spinny, plane, PlayerModel
			});

			ForwardProgram.SetVertexAttribPointers();
			InterfaceProgram.SetVertexAttribPointers();
		}

		/// <summary> Core render loop. Be careful to not reference anything on the logic thread from here! </summary>
		protected override void OnRenderFrame(FrameEventArgs args) {
			GameState state = Program.LogicThread.GetGameState();

			Vector3 playerPosition = new Vector3(state.PlayerX, 0f, state.PlayerY);
			PlayerModel.SetPosition(playerPosition);
			CameraTarget = playerPosition;
			CameraPosition = playerPosition + new Vector3(0, 2, -3);
			PlayerModel.SetRotation(PlayerModel.Rotation + new Vector3(0, 1f, 0));
			spinny.SetRotation(spinny.Rotation + new Vector3(0, 1, 0));

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			ForwardProgram.Use();
			Matrix4 Model = Matrix4.Identity;
			Matrix4 View = Matrix4.LookAt(CameraPosition, CameraTarget, Vector3.UnitY);
			float aspectRatio = (float)Size.X / (float)Size.Y;
			Matrix4 Perspective = Matrix4.CreatePerspectiveFieldOfView(90f * RCF, aspectRatio, 0.01f, 100.0f);

			// Defaults, in case a model for some reason doesn't have this defined
			GL.UniformMatrix4(ForwardProgram.UniformModel_ID, true, ref Model);
			GL.UniformMatrix4(ForwardProgram.UniformView_ID, true, ref View);
			GL.UniformMatrix4(ForwardProgram.UniformPerspective_ID, true, ref Perspective);

			int drawCalls = Scene.Render();

			InterfaceProgram.Use();
			// Draw interface: unimplemented. TODO

			Context.SwapBuffers();
		}

		/// <summary> Handles all debug callbacks from OpenGL and throws exceptions if unhandled. </summary>
		private static void DebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam) {
			string messageString = Marshal.PtrToStringAnsi(message, length);
			if (type < DebugType.DebugTypeOther && type < DebugType.DebugTypeError)
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
