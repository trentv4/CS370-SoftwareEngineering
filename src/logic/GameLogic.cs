using System.Collections.Generic;
using Project.Networking;
using Project.Levels;
using Project.Items;
using Project.Util;
using System;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Project {
	/// <summary> Struct storing all data required to execute the game. By storing this 
	/// in a struct, it allows it to be easily copied to a new object that can be sent
	/// to the renderer without violating thread safety. </summary>
	public struct GameState {
		public float PlayerX;
		public float PlayerY;
		public Level Level;
		public bool IsViewingMap;
		public int Score;
		public float CameraPitch;
		public float CameraYaw;
	}

	public class GameLogic {
		/// <summary> Actively modified state, constantly being changed by Update() </summary>
		private GameState State = new GameState();
		/// <summary> "Backbuffer" of the game state, which is only ever changed in one go. This will be the state sent to the renderer. </summary>
		private GameState StateBuffer = new GameState();

		public Level Level;
		public int GameTick;

		public Server Server;

		public GameLogic() {
			StateBuffer.PlayerX = 0.0f;
			StateBuffer.PlayerY = 0.0f;
			StateBuffer.IsViewingMap = false;
			StateBuffer.CameraPitch = 0.0f;
			StateBuffer.CameraYaw = 0.0f;
		}

		/// <summary> Handles all on-startup tasks, instantiation of objects, or other similar run-once tasks. </summary>
		public virtual void Initialize() {
			ItemManager.LoadDefinitions();
			Level = new Level();
			Server = new Server(Level);
		}

		/// <summary> Primary gameplay loop. Make all your calls and modifications to State, not StateBuffer!</summary>
		public virtual void Update() {
			Input.Update();
			Level.Update();
			GameTick++;

			//Toggle mouse visibility and centering for mouse camera movement
			if (Input.IsKeyPressed(Keys.F1)) {
				//For some reason setting this to false also makes the cursor invisible, making CursorVisible useless...
				Program.INSTANCE.CursorGrabbed = !Program.INSTANCE.CursorGrabbed;
			}

			State.PlayerX = Level.Player.Position.X;
			State.PlayerY = Level.Player.Position.Y;
			State.Level = Level;
			State.IsViewingMap = Level.IsViewingMap;
			State.Score = (GameTick / 600) + ((int)Level.Score);
			State.CameraPitch = Level.Player.CameraPitch;
			State.CameraYaw = Level.Player.CameraYaw;
			StateBuffer = State;
		}

		/// <summary> Retrieve a copy of the GameState, typically for the renderer. </summary>
		public virtual GameState GetGameState() {
			return StateBuffer;
		}
	}

	public class RemoteGameLogic : GameLogic {
		public override void Initialize() {

		}

		public override void Update() {
			// every 16.6ms (60fps), retreive a new GameState from the server
		}

		public override GameState GetGameState() {
			// retrieve the remote game state from buffer
			return new GameState();
		}
	}
}
