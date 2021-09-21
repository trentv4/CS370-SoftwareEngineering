using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.InteropServices;
using Project.Levels;
using System.Collections.Generic;

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

		// Debug
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
			GL.Viewport(0, 0, Size.X, Size.Y);
			GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);

			ForwardProgram = new ShaderProgramForwardRenderer("src/render/shaders/ForwardShader_vertex.glsl",
												"src/render/shaders/ForwardShader_fragment.glsl");
			InterfaceProgram = new ShaderProgramInterface("src/render/shaders/InterfaceShader_vertex.glsl",
												"src/render/shaders/InterfaceShader_fragment.glsl");
			ForwardProgram.Use();

			Scene = SceneBuilder.CreateDemoScene();

			// don't look at the following lines.. extremely gross, but works
			PlayerModel = (Model)Scene.Children[0];
			// end gross

			ForwardProgram.SetVertexAttribPointers(new[] { 3, 3, 4, 2 });
			InterfaceProgram.SetVertexAttribPointers(new[] { 3, 2 });
		}

		private RenderableNode rooms = null;

		/// <summary> Core render loop. Be careful to not reference anything on the logic thread from here! </summary>
		protected override void OnRenderFrame(FrameEventArgs args) {
			GameState state = Program.LogicThread.GetGameState();

			if (rooms == null && state.Level != null) {
				rooms = SceneBuilder.BuildRoomScene(state.Level.Rooms);
			} else {
				rooms = new RenderableNode();
			}

			Vector3 playerPosition = new Vector3(state.PlayerX, 0f, state.PlayerY);
			PlayerModel.SetPosition(playerPosition);
			CameraTarget = playerPosition;
			CameraPosition = playerPosition + new Vector3(0, 2, -3);
			PlayerModel.SetRotation(PlayerModel.Rotation + new Vector3(0, 1f, 0));

			GL.Enable(EnableCap.DepthTest);

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

			Scene.Render();
			rooms.Render();

			GL.Disable(EnableCap.DepthTest);

			InterfaceProgram.Use();
			GL.UniformMatrix4(InterfaceProgram.UniformView_ID, true, ref Model);
			GL.UniformMatrix4(InterfaceProgram.UniformPerspective_ID, true, ref Perspective);

			if (state.IsViewingMap) {
				RenderableNode interfaceNode = new RenderableNode();
				float levelToMapScaling = 0.3f;
				Vector3 levelToMapTranslation = new Vector3(-1.5f, -0.75f, -1);

				var drawnConnections = new List<(Room, Room)>(); //Used to stop duplicate connection draws
				foreach (Room currentRoom in state.Level.Rooms) {
					InterfaceModel roomCircleModel = InterfaceModel.GetUnitCircle();
					roomCircleModel.SetPosition(new Vector3(currentRoom.Position.X * levelToMapScaling, currentRoom.Position.Z * levelToMapScaling, 0) + levelToMapTranslation);
					roomCircleModel.SetScale(0.1f);
					roomCircleModel.SetRotation(new Vector3(0f, 90f, 0f));
					if (currentRoom == state.Level.EndRoom) //Color end room yellow
						roomCircleModel.AlbedoTexture = new Texture("assets/textures/gold.png");
					interfaceNode.Children.Add(roomCircleModel);

					uint index = 0;
					foreach (Room connection in currentRoom.ConnectedRooms) {
						//Don't draw connections to state.Level.CurrentRoom, let state.Level.CurrentRoom draw color coded connections to the other rooms
						//Also don't draw duplicate connections
						if (connection == state.Level.CurrentRoom || drawnConnections.Contains((currentRoom, connection)))
							continue;

						Vector2 currentPos = new Vector2(currentRoom.Position.X, currentRoom.Position.Z);
						Vector2 connectPosCorrected = new Vector2(connection.Position.X, connection.Position.Z) - currentPos;
						Vector3 connectedRoomPosition = new Vector3(currentPos + (connectPosCorrected / 2)) * levelToMapScaling;

						float magnitude = Vector2.Distance(Vector2.Zero, connectPosCorrected);
						float angle = (float)Math.Atan2(connectPosCorrected.Y, connectPosCorrected.X);

						InterfaceModel connectorQuadModel = InterfaceModel.GetUnitRectangle();
						connectorQuadModel.SetPosition(connectedRoomPosition + levelToMapTranslation);
						connectorQuadModel.SetScale(new Vector3(magnitude * levelToMapScaling, 0.02f, 1f));
						connectorQuadModel.SetRotation(new Vector3(0, 0, angle / RCF));

						//Color code connections from current room
						if (currentRoom == state.Level.CurrentRoom) {
							if (index == 0)
								connectorQuadModel.AlbedoTexture = new Texture("assets/textures/red.png");
							else if (index == 1)
								connectorQuadModel.AlbedoTexture = new Texture("assets/textures/green.png");
							else if (index == 2)
								connectorQuadModel.AlbedoTexture = new Texture("assets/textures/blue.png");
							else if (index == 3)
								connectorQuadModel.AlbedoTexture = new Texture("assets/textures/gold.png");
							else if (index == 4)
								connectorQuadModel.AlbedoTexture = new Texture("assets/textures/cyan.png");
						}
						interfaceNode.Children.Add(connectorQuadModel);
						drawnConnections.Add((currentRoom, connection));
						drawnConnections.Add((connection, currentRoom));
						index++;
					}
				}

				//Draw player sprite on map over the room they're in
				Vector3 playerRoomPos = new Vector3(state.Level.CurrentRoom.Position.X, state.Level.CurrentRoom.Position.Z, 0.0f);
				Vector3 playerRoomPosAdjusted = playerRoomPos * levelToMapScaling;
				InterfaceModel playerCircleModel = InterfaceModel.GetUnitCircle();
				playerCircleModel.SetPosition(playerRoomPosAdjusted + levelToMapTranslation);
				playerCircleModel.SetScale(0.1f);
				playerCircleModel.SetRotation(new Vector3(0f, 90f, 0f));
				playerCircleModel.AlbedoTexture = new Texture("assets/textures/plane.png");
				interfaceNode.Children.Add(playerCircleModel);

				interfaceNode.Render();
			}

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
