using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

namespace Project.Render {
	/// <summary> Primary rendering class, instantiated in Program and continuously executed. OpenGL is only referenced from here and related classes. </summary>
	public class Renderer : GameWindow {
		public Renderer(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }

		/// <summary> Radian Conversion Factor (used for degree-radian conversions). Equal to pi/180. </summary>
		internal const float RCF = 0.017453293f;

		public static readonly GameLogic LogicThread = new GameLogic();
		public static readonly Vector2 ProjectMatrixNearFar = new Vector2(0.01f, 1000000f);
		public static Renderer INSTANCE;
		public static ConcurrentQueue<string> EventQueue = new ConcurrentQueue<string>();
		public static string CurrentPass { get; private set; }

		private static DebugProc _debugCallback = DebugCallback;
		private static GCHandle _debugCallbackHandle;

		public ShaderProgramDeferredRenderer DeferredProgram { get; private set; }
		public ShaderProgramInterface InterfaceProgram { get; private set; }
		public ShaderProgramFog FogProgram { get; private set; }
		public ShaderProgramVignette VignetteProgram { get; private set; }
		public ShaderProgramCompositor CompositorShader { get; private set; }

		public Framebuffer GBuffer;
		public Framebuffer FogFramebuffer;
		public Framebuffer InterfaceBuffer;
		public Framebuffer DefaultFramebuffer;

		private GameRoot _sceneHierarchy = new GameRoot();
		private InterfaceRoot _interfaceRoot = new InterfaceRoot();
		private static bool _isRenderGymActive = false;
		private int _debugGroupTracker = 0;

		/// <summary> Handles all OpenGL setup, including shader programs, flags, attribs, etc. </summary>
		protected override void OnRenderThreadStarted() {
			// Sets required OpenGL flags
			_debugCallbackHandle = GCHandle.Alloc(_debugCallback);
			GL.DebugMessageCallback(_debugCallback, IntPtr.Zero);
			GL.Enable(EnableCap.DebugOutput);
			GL.Enable(EnableCap.DebugOutputSynchronous);
			GL.Enable(EnableCap.DepthTest);
			GL.CullFace(CullFaceMode.Front);
			GL.Enable(EnableCap.Blend);
			GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
			GL.Viewport(0, 0, Size.X, Size.Y);

			VSync = VSyncMode.On;

			_isRenderGymActive = false;

			// Shader program creation
			DeferredProgram = new ShaderProgramDeferredRenderer("src/render/shaders/DeferredShader.glsl");
			InterfaceProgram = new ShaderProgramInterface("src/render/shaders/InterfaceShader.glsl");
			FogProgram = new ShaderProgramFog("src/render/shaders/FogShader.glsl");
			VignetteProgram = new ShaderProgramVignette("src/render/shaders/VignetteShader.glsl");
			CompositorShader = new ShaderProgramCompositor("src/render/shaders/CompositorShader.glsl");

			// Fog temporary buffer to hold back-face + scene depths
			FogFramebuffer = new Framebuffer();
			FogFramebuffer.AddDepthBuffer(PixelInternalFormat.DepthComponent24);

			// Buffer 0: Albedo (RGBA)
			// Buffer 1: normal XYZ, specular component
			// Buffer 2: fog strength, fog depth, unused, unused
			GBuffer = new Framebuffer();
			GBuffer.AddDepthBuffer(PixelInternalFormat.DepthComponent24);
			GBuffer.AddAttachments(new[] { "GB: Albedo", "GB: Normals/specular", "GB: Fog" });

			// Interface buffer (both for UI and for fx like vignettes)
			InterfaceBuffer = new Framebuffer();
			InterfaceBuffer.AddAttachment(PixelInternalFormat.Rgba, PixelFormat.Rgba);
			DebugLabel(ObjectLabelIdentifier.Texture, InterfaceBuffer.GetAttachment(0).TextureID, "Interface");

			// Wrap the default framebuffer but don't assign anything new to it
			DefaultFramebuffer = new Framebuffer(0);

			// Initializers
			_sceneHierarchy.Build();
			FontAtlas.Load("calibri", "assets/fonts/calibri.png", "assets/fonts/calibri.json");

			// Shorthand for setting vertex shader attribs
			DeferredProgram.SetVertexAttribPointers(new[] { 3, 3, 4, 2 });
			InterfaceProgram.SetVertexAttribPointers(new[] { 2, 2 });
			FogProgram.SetVertexAttribPointers(new[] { 3 });
		}

		/// <summary> Core render loop. Use GameState copies to access logic thread information.</summary>
		protected override void OnRenderFrame(FrameEventArgs args) {
			GameState state = Program.LogicThread.GetGameState();
			ProcessEventsFromQueue(state);

			// Setting player model location according to logic thread player location
			Model PlayerModel = _sceneHierarchy.PlayerModel;
			PlayerModel.SetPosition(new Vector3(state.PlayerX, 0f, state.PlayerY));
			PlayerModel.SetRotation(PlayerModel.Rotation + new Vector3(0, 1f, 0));

			// Camera and perspective matrices required for different passes
			Vector3 cameraRotation = new Vector3(0f, 2f, -3f) * Matrix3.CreateRotationY(state.CameraYaw * RCF);
			Matrix4 View = Matrix4.LookAt(PlayerModel.Position + cameraRotation, PlayerModel.Position, Vector3.UnitY);
			Matrix4 Perspective3D = Matrix4.CreatePerspectiveFieldOfView(90f * RCF, (float)Size.X / (float)Size.Y, ProjectMatrixNearFar.X, ProjectMatrixNearFar.Y);
			Matrix4 Perspective2D = Matrix4.CreateOrthographicOffCenter(0f, (float)Size.X, 0f, (float)Size.Y, ProjectMatrixNearFar.X, ProjectMatrixNearFar.Y);

			BeginPass("G-Buffer");
			GBuffer.Use().Reset();
			DeferredProgram.Use();
			GL.UniformMatrix4(DeferredProgram.UniformView_ID, true, ref View);
			GL.UniformMatrix4(DeferredProgram.UniformPerspective_ID, true, ref Perspective3D);
			_sceneHierarchy.Render();
			EndPass();

			BeginPass("Fog");
			FogFramebuffer.Use().Reset();
			FogFramebuffer.BlitFrom(GBuffer, ClearBufferMask.DepthBufferBit); // Copy depth from GBuffer to temporary fog framebuffer
			GL.Enable(EnableCap.CullFace);
			_sceneHierarchy.Render(); // This draws the backfaces of anything that is labeled "foggy"
			GL.Disable(EnableCap.CullFace);
			GBuffer.Use();
			FogProgram.Use();
			FogFramebuffer.Depth.Bind();
			GL.UniformMatrix4(FogProgram.UniformView_ID, true, ref View);
			GL.UniformMatrix4(FogProgram.UniformPerspective_ID, true, ref Perspective3D);
			_sceneHierarchy.Render(); // This draws the frontfaces of anything labeled "foggy", and calculates fog strength.
			EndPass();

			BeginPass("Interface");
			_interfaceRoot.Rebuild(state);
			InterfaceBuffer.Use().Reset();
			InterfaceProgram.Use();
			GL.UniformMatrix4(InterfaceProgram.UniformPerspective_ID, true, ref Perspective2D);
			_interfaceRoot.Render(state);
			EndPass();
			BeginPass("Vignette");
			VignetteProgram.Use();
			GL.Uniform1(VignetteProgram.UniformVignetteStrength_ID, 1.75f);
			GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
			EndPass();

			BeginPass("Compositor");
			DefaultFramebuffer.Use().Reset();
			CompositorShader.Use();
			GBuffer.GetAttachment(0).Bind(0); // G buffer: albedo [RGBA]
			GBuffer.GetAttachment(2).Bind(1); // G buffer: Fog strength, fog depth
			InterfaceBuffer.GetAttachment(0).Bind(3); // Interface [RGBA]
			GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
			EndPass();

			Context.SwapBuffers();
			_debugGroupTracker = 0;
		}

		/// <summary> Processes all events sent over from the GPU, typically for regenerating levels and maps. </summary>
		private void ProcessEventsFromQueue(GameState state) {
			while (!EventQueue.IsEmpty) {
				string eventString;
				bool result = EventQueue.TryDequeue(out eventString);
				switch (eventString) {
					case "LevelRegenerated":
						if (_isRenderGymActive) {
							_interfaceRoot.BuildMapInterface(new GameState());
							_sceneHierarchy.Scene = _sceneHierarchy.BuildRoom(null);
						} else {
							_interfaceRoot.BuildMapInterface(state);
							_sceneHierarchy.Scene = _sceneHierarchy.BuildRoom(state.Level.CurrentRoom);
						}
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

		/// <summary> Starts a GPU debug group, used for grouping operations together into one section for debugging in RenderDoc. </summary>
		private void BeginPass(string title) {
			GL.PushDebugGroup(DebugSourceExternal.DebugSourceApplication, _debugGroupTracker++, title.Length, title);
			CurrentPass = title;
		}

		/// <summary> Ends the current debug group on the GPU. </summary>
		private void EndPass() {
			GL.PopDebugGroup();
		}

		/// <summary> Assigns a debug label to a specified GL object - useful in debugging tools similar to debug groups. </summary>
		private static void DebugLabel(ObjectLabelIdentifier type, int id, string label) {
			GL.ObjectLabel(type, id, label.Length, label);
		}

		/// <summary> Stub method to call the external Program method, helps in isolation of logic from rendering. </summary>
		protected override void OnUpdateFrame(FrameEventArgs args) {
			Program.LogicThread.Update(args.Time);
		}
	}
}
