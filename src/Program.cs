using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Project.Render;

namespace Project {
	public enum LaunchMode {
		SinglePlayer,
		Server,
		Client
	};

	public class Program {
		public static GameLogic LogicThread;
		public static GameWindow INSTANCE;
		public static LaunchMode Mode = LaunchMode.SinglePlayer;

		public static void Main(string[] args) {
			foreach (String argument in args) {
				if (argument.Equals("-client")) Mode = LaunchMode.Client;
				if (argument.Equals("-server")) Mode = LaunchMode.Server;
			}
			Console.WriteLine("Initializing");
			GameWindowSettings gameSettings = new GameWindowSettings() {
				IsMultiThreaded = true,
				UpdateFrequency = 60
			};

			NativeWindowSettings windowSettings = new NativeWindowSettings() {
				Size = new Vector2i(1600, 900),
				Title = "Display",
				WindowBorder = WindowBorder.Fixed
			};

			if (Mode == LaunchMode.Client) {
				LogicThread = new RemoteGameLogic();
			} else {
				LogicThread = new GameLogic();
			}
			LogicThread.Initialize();

			if (Mode == LaunchMode.Server) {
				LogicThread.Initialize();
				using (StubRenderer renderer = new StubRenderer(gameSettings, windowSettings)) {
					INSTANCE = renderer;
					renderer.Run();
				}
			} else {
				using (Renderer renderer = new Renderer(gameSettings, windowSettings)) {
					Renderer.INSTANCE = renderer;
					INSTANCE = renderer;
					renderer.Run();
				}
			}
		}
	}

	class StubRenderer : GameWindow {
		public StubRenderer(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }

		protected override void OnUpdateFrame(FrameEventArgs args) {
			Program.LogicThread.Update();
		}
	}
}
