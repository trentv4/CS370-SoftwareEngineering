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
	/// <summary> Primary rendering class, instantiated in Program and continuously executed. OpenGL is only referenced from here and related classes. </summary>
	public class Renderer : GameWindow {
		public Renderer(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }
		public static Renderer INSTANCE;

		/// <summary> Radian Conversion Factor (used for degree-radian conversions). Equal to pi/180. </summary>
		internal const float RCF = 0.017453293f;

		// Render
		public ShaderProgramForwardRenderer ForwardProgram { get; private set; } // Forward rendering technique
		public ShaderProgramInterface InterfaceProgram { get; private set; } // Interface renderer (z=0)
		public ShaderProgramFog FogProgram { get; private set; } // Fog renderer

		public ShaderProgram CurrentProgram;

		public static ConcurrentQueue<string> EventQueue = new ConcurrentQueue<string>();
		private static GameRoot SceneHierarchy = new GameRoot();
		private static InterfaceRoot Interface = new InterfaceRoot();

		private static int FramebufferID;
		private static int depth;

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
			GL.Enable(EnableCap.Blend);
			// A: new color, B: existing color
			// This isn't correct and it influences render order, but..... it works?
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.BlendEquation(BlendEquationMode.FuncAdd);
			GL.Enable(EnableCap.DepthTest);
			VSync = VSyncMode.On;
			GL.Viewport(0, 0, Size.X, Size.Y);
			GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);

			// Shader program creation
			ForwardProgram = new ShaderProgramForwardRenderer("src/render/shaders/ForwardShader_vertex.glsl",
												"src/render/shaders/ForwardShader_fragment.glsl");
			InterfaceProgram = new ShaderProgramInterface("src/render/shaders/InterfaceShader_vertex.glsl",
												"src/render/shaders/InterfaceShader_fragment.glsl");
			FogProgram = new ShaderProgramFog("src/render/shaders/FogShader_vertex.glsl",
												"src/render/shaders/FogShader_fragment.glsl");

			FramebufferID = GL.GenFramebuffer();
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferID);
			depth = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, depth);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent24, Size.X, Size.Y,
						  0, PixelFormat.DepthComponent, PixelType.UnsignedByte, new byte[0]);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
									TextureTarget.Texture2D, depth, 0);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

			// Creates "unit" models - models specified in code that are manipulated with model matrices
			Model.CreateUnitModels();
			InterfaceModel.CreateUnitModels();
			// Builds the scene. Includes player, interface, and world.
			SceneHierarchy.Build();

			FontAtlas.Load("calibri", "assets/fonts/calibri.png", "assets/fonts/calibri.json");

			// Shorthand for setting vertex shader attribs
			ForwardProgram.SetVertexAttribPointers(new[] { 3, 3, 4, 2 });
			InterfaceProgram.SetVertexAttribPointers(new[] { 2, 2 });
			FogProgram.SetVertexAttribPointers(new[] { 3, 3, 4, 2 });
		}

		/// <summary> Core render loop. Use GameState copies to access logic thread information.</summary>
		protected override void OnRenderFrame(FrameEventArgs args) {
			GameState state = Program.LogicThread.GetGameState();

			ProcessEventsFromQueue(state);

			// Setting player model location according to logic thread player location
			Model PlayerModel = SceneHierarchy.PlayerModel;
			PlayerModel.SetPosition(new Vector3(state.PlayerX, 0f, state.PlayerY));
			PlayerModel.SetRotation(PlayerModel.Rotation + new Vector3(0, 1f, 0));
			int debugGroup = 0;

			DebugGroup("Geometry pass", debugGroup++);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			Vector3 cameraRotation = new Vector3(0f, 2f, -3f) * Matrix3.CreateRotationY(state.CameraYaw * RCF);
			Matrix4 View = Matrix4.LookAt(PlayerModel.Position + cameraRotation, PlayerModel.Position, Vector3.UnitY);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			ForwardProgram.Use();
			Matrix4 Perspective3D = Matrix4.CreatePerspectiveFieldOfView(90f * RCF, (float)Size.X / (float)Size.Y, 0.01f, 100.0f);
			GL.UniformMatrix4(ForwardProgram.UniformView_ID, true, ref View);
			GL.UniformMatrix4(ForwardProgram.UniformPerspective_ID, true, ref Perspective3D);
			SceneHierarchy.Render();
			DebugGroupEnd();

			DebugGroup("Backface depth + geo depth", debugGroup++);
			GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
			GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, FramebufferID);
			GL.BlitFramebuffer(0, 0, Size.X, Size.Y, 0, 0, Size.X, Size.Y, ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);
			GL.Enable(EnableCap.CullFace);
			GL.CullFace(CullFaceMode.Front);
			SceneHierarchy.Render();
			GL.Disable(EnableCap.CullFace);
			DebugGroupEnd();

			DebugGroup("Fog", debugGroup++);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			FogProgram.Use();
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, depth);
			GL.UniformMatrix4(FogProgram.UniformView_ID, true, ref View);
			GL.UniformMatrix4(FogProgram.UniformPerspective_ID, true, ref Perspective3D);
			SceneHierarchy.Render();
			DebugGroupEnd();

			DebugGroup("Interface", debugGroup++);
			Interface.Rebuild(state);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			InterfaceProgram.Use();
			Matrix4 Perspective2D = Matrix4.CreateOrthographicOffCenter(0f, (float)Size.X, 0f, (float)Size.Y, 0.1f, 100f);
			GL.UniformMatrix4(InterfaceProgram.UniformPerspective_ID, true, ref Perspective2D);
			GL.Disable(EnableCap.DepthTest);
			Interface.Render(state);
			GL.Enable(EnableCap.DepthTest);
			DebugGroupEnd();

			Context.SwapBuffers();
		}

		private static void ProcessEventsFromQueue(GameState state) {
			// Process event queue
			while (!EventQueue.IsEmpty) {
				string eventString;
				bool result = EventQueue.TryDequeue(out eventString);
				switch (eventString) {
					case "LevelRegenerated":
						Interface.Map = Interface.BuildMapInterface(state);
						SceneHierarchy.Scene = SceneHierarchy.BuildRoom(state.Level.CurrentRoom);
						break;
					default:
						break;
				}
			}
		}

		/// <summary> Handles all debug callbacks from OpenGL and throws exceptions if unhandled. </summary>
		private static void DebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam) {
			string messageString = Marshal.PtrToStringAnsi(message, length);
			if (type < DebugType.DebugTypeOther && type < DebugType.DebugTypeError)
				Console.WriteLine($"{severity} {type} | {messageString}");
			if (type == DebugType.DebugTypeError)
				throw new Exception(messageString);
		}

		private static void DebugGroup(string title, int id) {
			GL.PushDebugGroup(DebugSourceExternal.DebugSourceApplication, id, title.Length, title);
		}

		private static void DebugGroupEnd() {
			GL.PopDebugGroup();
		}

		/// <summary> Stub method to call the external Program method, helps in isolation of logic from rendering </summary>
		protected override void OnUpdateFrame(FrameEventArgs args) {
			Program.LogicThread.Update();
		}
	}
}
