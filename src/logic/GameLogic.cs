using Project.Items;
using Project.Levels;
using System.Collections.Generic;
using System;

namespace Project {
	/// <summary> Struct storing all data required to execute the game. By storing this 
	/// in a struct, it allows it to be easily copied to a new object that can be sent
	/// to the renderer without violating thread safety. </summary>
	public struct GameState {
		public float PlayerX;
		public float PlayerY;
		public Level Level;
	}

	public class GameLogic {
		/// <summary> Actively modified state, constantly being changed by Update() </summary>
		private GameState State = new GameState();
		/// <summary> "Backbuffer" of the game state, which is only ever changed in one go. This will be the state sent to the renderer. </summary>
		private GameState StateBuffer = new GameState();

		public Level Level;

		public GameLogic() {
			State.PlayerX = 0.0f;
			State.PlayerY = 0.0f;
			StateBuffer.PlayerX = 0.0f;
			StateBuffer.PlayerY = 0.0f;
		}

		/// <summary> Handles all on-startup tasks, instantiation of objects, or other similar run-once tasks. </summary>
		public void Initialize() {
			ItemManager.LoadDefinitions();
			Random r = new Random();
			int roomsPerRow = 2;
			int rowsPerLevel = 3;
			List<Room> roomConstruction = new List<Room>();
			roomConstruction.Add(new Room(0, roomsPerRow / 2)); // Creates the starting room at index zero
			for (int i = 1; i < rowsPerLevel; i++) {
				for (int g = 0; g < roomsPerRow; g++) {
					if (r.NextDouble() > 0.5) {
						roomConstruction.Add(new Room(i, g));
					}
				}
			}

			Level = new Level(roomConstruction.ToArray());
		}

		/// <summary> Primary gameplay loop. Make all your calls and modifications to State, not StateBuffer!</summary>
		public void Update() {
			StateBuffer = State;
			Level.Update();

			State.PlayerX = Level.Player.xPos;
			State.PlayerY = Level.Player.yPos;
			State.Level = Level;
		}

		/// <summary> Retrieve a copy of the GameState, typically for the renderer. </summary>
		public GameState GetGameState() {
			return StateBuffer;
		}
	}
}
