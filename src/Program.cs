using System;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using Project.Render;

namespace Project {
	public class Program {
		public static GameLogic LogicThread;

		/// <summary> Defines specific modes the game can run in, and starts different logic threads or renderers depending on mode. </summary>
		private enum LaunchMode {
			SinglePlayer,
			Server,
			Client
		};
		private static LaunchMode _mode = LaunchMode.SinglePlayer;

		/// <summary> Program entry point. Supports multiple launch arguments for different modes. </summary>
		public static void Main(string[] args) {
			foreach (String argument in args) {
				if (argument.Equals("-client")) _mode = LaunchMode.Client;
				if (argument.Equals("-server")) _mode = LaunchMode.Server;
			}
			Console.WriteLine($"Initializing in game mode: {_mode.ToString()}");

			GameWindowSettings gameSettings = new GameWindowSettings() {
				IsMultiThreaded = true,
				UpdateFrequency = 60
			};
			NativeWindowSettings windowSettings = new NativeWindowSettings() {
				Size = new Vector2i(1600, 900),
				Title = "Face the future",
				WindowBorder = WindowBorder.Fixed
			};

			if (_mode == LaunchMode.Client) {
				LogicThread = new RemoteGameLogic();
				LogicThread.Initialize();
				using (Renderer renderer = new Renderer(gameSettings, windowSettings)) {
					Renderer.INSTANCE = renderer;
					renderer.Run();
				}

			} else if (_mode == LaunchMode.Server) {
				LogicThread = new GameLogic();
				LogicThread.Initialize();
				using (StubRenderer renderer = new StubRenderer(gameSettings, windowSettings)) {
					Renderer.INSTANCE = renderer;
					renderer.Run();
				}

			} else if (_mode == LaunchMode.SinglePlayer) {
				LogicThread = new GameLogic();
				LogicThread.Initialize();
				using (Renderer renderer = new Renderer(gameSettings, windowSettings)) {
					Renderer.INSTANCE = renderer;
					renderer.Run();
				}
			}
		}
	}

	/// <summary> Stub rendering GameWindow so complex graphics are not executed on the server side. </summary>
	class StubRenderer : Renderer {
		public StubRenderer(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }

		protected override void OnRenderThreadStarted() { } // Need to disable the window opening, or implement a server screen
		protected override void OnRenderFrame(FrameEventArgs args) { }

		protected override void OnUpdateFrame(FrameEventArgs args) {
			Program.LogicThread.Update();
		}
	}
}
