using System.Collections.Generic;
using Project.Networking;
using Project.Levels;
using Project.Items;
using Project.Util;
using System;

namespace Project {
	/// <summary> Struct storing all data required to execute the game. By storing this 
	/// in a struct, it allows it to be easily copied to a new object that can be sent
	/// to the renderer without violating thread safety. </summary>
	public struct GameState {
		public float PlayerX;
		public float PlayerY;
		public Level Level;
		public bool IsViewingMap;
	}

	public class GameLogic {
		/// <summary> Actively modified state, constantly being changed by Update() </summary>
		private GameState State = new GameState();
		/// <summary> "Backbuffer" of the game state, which is only ever changed in one go. This will be the state sent to the renderer. </summary>
		private GameState StateBuffer = new GameState();

		public Level Level;

		public Server Server;

		public GameLogic() {
			StateBuffer.PlayerX = 0.0f;
			StateBuffer.PlayerY = 0.0f;
			StateBuffer.IsViewingMap = false;
		}

		/// <summary> Handles all on-startup tasks, instantiation of objects, or other similar run-once tasks. </summary>
		public void Initialize() {
			ItemManager.LoadDefinitions();
			Level = new Level();
			Server = new Server(Level);
		}

		/// <summary> Primary gameplay loop. Make all your calls and modifications to State, not StateBuffer!</summary>
		public void Update() {
			Input.Update();
			Level.Update();

			State.PlayerX = Level.Player.Position.X;
			State.PlayerY = Level.Player.Position.Y;
			State.Level = Level;
			State.IsViewingMap = Level.IsViewingMap;

			StateBuffer = State;
		}

		/// <summary> Retrieve a copy of the GameState, typically for the renderer. </summary>
		public GameState GetGameState() {
			return StateBuffer;
		}
	}
}
