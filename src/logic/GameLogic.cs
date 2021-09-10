using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Project.Items;
using Project.Levels;

namespace Project {
	/// <summary> Struct storing all data required to execute the game. By storing this 
	/// in a struct, it allows it to be easily copied to a new object that can be sent
	/// to the renderer without violating thread safety. </summary>
	public struct GameState {
	}

	public class GameLogic {
		/// <summary> Actively modified state, constantly being changed by Update() </summary>
		private GameState State = new GameState();
		/// <summary> "Backbuffer" of the game state, which is only ever changed in one go. This will be the state sent to the renderer. </summary>
		private GameState StateBuffer = new GameState();

		/// <summary> Handles all on-startup tasks, instantiation of objects, or other similar run-once tasks. </summary>
		public void Initialize() {
			ItemManager.LoadDefinitions();
			foreach (var def in ItemManager.Definitions)
				Console.WriteLine(def.ToString());
			Level firstLevel = new Level(500,500); //placeholder to test level class
		}

		/// <summary> Primary gameplay loop. Make all your calls and modifications to State, not StateBuffer!</summary>
		public void Update() {
			StateBuffer = State;
		}

		/// <summary> Retrieve a copy of the GameState, typically for the renderer. </summary>
		public GameState GetGameState() {
			return StateBuffer;
		}
	}
}
