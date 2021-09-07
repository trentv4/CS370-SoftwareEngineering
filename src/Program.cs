using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Project.Render;

namespace Project {
	public class Program {
		public static readonly GameLogic LogicThread = new GameLogic();

		public static void Main(string[] args) {
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

			LogicThread.Initialize();

			using (Renderer g = new Renderer(gameSettings, windowSettings)) {
				g.Run();
			}
		}
	}
}
