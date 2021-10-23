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
		public static readonly GameLogic LogicThread = new GameLogic();
		public static Renderer INSTANCE;

		// OpenGL error callback
		private static DebugProc _debugCallback = DebugCallback;
		private static GCHandle _debugCallbackHandle;
		private static int _debugGroupTracker = 0;

		/// <summary> Radian Conversion Factor (used for degree-radian conversions). Equal to pi/180. </summary>
		internal const float RCF = 0.017453293f;

		// Trackers
		public ShaderProgram CurrentProgram;
		public Framebuffer CurrentBuffer;

		// Render
		public ShaderProgramDeferredRenderer DeferredProgram { get; private set; }
		public ShaderProgramInterface InterfaceProgram { get; private set; }
		public ShaderProgramFog FogProgram { get; private set; }
		public ShaderProgramVignette VignetteProgram { get; private set; }
		public ShaderProgramCompositor CompositorShader { get; private set; }

		private static Framebuffer _gBuffer;
		private static Framebuffer _fogFramebuffer;
		private static Framebuffer _interfaceBuffer;
		private static Framebuffer _defaultFramebuffer;

		public static ConcurrentQueue<string> EventQueue = new ConcurrentQueue<string>();
		private static GameRoot _sceneHierarchy = new GameRoot();
		private static InterfaceRoot _interfaceRoot = new InterfaceRoot();

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
			GL.ClearColor(0.1f, 0.1f, 0.1f, 0.0f);
			VSync = VSyncMode.On;

			// Shader program creation
			DeferredProgram = new ShaderProgramDeferredRenderer("src/render/shaders/DeferredShader.glsl");
			InterfaceProgram = new ShaderProgramInterface("src/render/shaders/InterfaceShader.glsl");
			FogProgram = new ShaderProgramFog("src/render/shaders/FogShader.glsl");
			VignetteProgram = new ShaderProgramVignette("src/render/shaders/VignetteShader.glsl");
			CompositorShader = new ShaderProgramCompositor("src/render/shaders/CompositorShader.glsl");

			// Fog temporary buffer to hold back-face + scene depths
			_fogFramebuffer = new Framebuffer();
			_fogFramebuffer.SetDepthBuffer(PixelInternalFormat.DepthComponent24);

			// Buffer 0: Albedo (RGBA)
			// Buffer 1: normal XYZ, specular component
			// Buffer 2: fog strength, fog depth, unused, unused
			_gBuffer = new Framebuffer();
			_gBuffer.SetDepthBuffer(PixelInternalFormat.DepthComponent24);
			_gBuffer.AddAttachments(new[] { "GB: Albedo", "GB: Normals/specular", "GB: Fog" });

			// Interface buffer (both for UI and for fx like vignettes)
			_interfaceBuffer = new Framebuffer();
			_interfaceBuffer.AddAttachment(PixelInternalFormat.Rgba, PixelFormat.Rgba);
			Label(ObjectLabelIdentifier.Texture, _interfaceBuffer.GetAttachment(0).TextureID, "Interface");

			// Wrap the default framebuffer but don't assign anything new to it
			_defaultFramebuffer = new Framebuffer(0);

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
			Matrix4 Perspective3D = Matrix4.CreatePerspectiveFieldOfView(90f * RCF, (float)Size.X / (float)Size.Y, 0.01f, 100.0f);
			Matrix4 Perspective2D = Matrix4.CreateOrthographicOffCenter(0f, (float)Size.X, 0f, (float)Size.Y, 0.1f, 100f);

			DebugGroup("G-Buffer");
			_gBuffer.Use().Reset();
			DeferredProgram.Use();
			GL.UniformMatrix4(DeferredProgram.UniformView_ID, true, ref View);
			GL.UniformMatrix4(DeferredProgram.UniformPerspective_ID, true, ref Perspective3D);
			_sceneHierarchy.Render();
			DebugGroupEnd();

			DebugGroup("Fog");
			// Blit existing g-buffer depth to fog depth buffer
			_fogFramebuffer.Use().Reset();
			_fogFramebuffer.BlitFrom(_gBuffer, ClearBufferMask.DepthBufferBit);
			// Draw depth of back faces of fog to fog depth buffer
			GL.Enable(EnableCap.CullFace);
			_sceneHierarchy.Render();
			GL.Disable(EnableCap.CullFace);
			// Draw front faces, use it to calculate fog strength, write to g-buffer
			_gBuffer.Use();
			FogProgram.Use();
			_fogFramebuffer.Depth.Bind();
			GL.UniformMatrix4(FogProgram.UniformView_ID, true, ref View);
			GL.UniformMatrix4(FogProgram.UniformPerspective_ID, true, ref Perspective3D);
			_sceneHierarchy.Render();
			DebugGroupEnd();

			DebugGroup("Interface");
			_interfaceRoot.Rebuild(state);
			_interfaceBuffer.Use().Reset();
			InterfaceProgram.Use();
			GL.UniformMatrix4(InterfaceProgram.UniformPerspective_ID, true, ref Perspective2D);
			_interfaceRoot.Render(state);
			DebugGroupEnd();
			DebugGroup("Vignette");
			VignetteProgram.Use();
			GL.Uniform1(VignetteProgram.UniformVignetteStrength_ID, 1.75f);
			GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
			DebugGroupEnd();

			DebugGroup("Compositor");
			_defaultFramebuffer.Use().Reset();
			CompositorShader.Use();
			_gBuffer.GetAttachment(0).Bind(0); // G buffer: albedo [RGBA]
			_gBuffer.GetAttachment(2).Bind(1); // G buffer: Fog strength, fog depth
			_interfaceBuffer.GetAttachment(0).Bind(3); // Interface [RGBA]
			GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
			DebugGroupEnd();

			Context.SwapBuffers();
			_debugGroupTracker = 0;
		}

		/// <summary> Processes all events sent over from the GPU, typically for regenerating levels and maps. </summary>
		private static void ProcessEventsFromQueue(GameState state) {
			// Process event queue
			while (!EventQueue.IsEmpty) {
				string eventString;
				bool result = EventQueue.TryDequeue(out eventString);
				switch (eventString) {
					case "LevelRegenerated":
						_interfaceRoot.BuildMapInterface(state);
						_sceneHierarchy.Scene = _sceneHierarchy.BuildRoom(state.Level.CurrentRoom);
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
		private static void DebugGroup(string title) {
			GL.PushDebugGroup(DebugSourceExternal.DebugSourceApplication, _debugGroupTracker++, title.Length, title);
		}

		/// <summary> Ends the current debug group on the GPU. </summary>
		private static void DebugGroupEnd() {
			GL.PopDebugGroup();
		}

		private static void Label(ObjectLabelIdentifier type, int id, string label) {
			GL.ObjectLabel(type, id, label.Length, label);
		}

		/// <summary> Stub method to call the external Program method, helps in isolation of logic from rendering. </summary>
		protected override void OnUpdateFrame(FrameEventArgs args) {
			Program.LogicThread.Update();
		}
	}
}
