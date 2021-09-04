using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Project {
	/// <summary> Struct storing all data required to execute the game. By storing this 
	/// in a struct, it allows it to be easily copied to a new object that can be sent
	/// to the renderer without violating thread safety. </summary>
	public struct GameState {

	}

	public class GameLogic {
		private GameState State = new GameState();

		/// <summary> Handles all on-startup tasks, instantiation of objects, or other similar run-once tasks. </summary>
		public void Initialize() {

		}

		/// <summary> Primary gameplay loop. </summary>
		public void Update() {
			Console.WriteLine("Update thread running");
		}

		/// <summary> Retrieve a copy of the GameState, typically for the renderer. </summary>
		public GameState GetGameState() {
			return State;
		}
	}
}
