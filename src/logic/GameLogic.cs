using System.Collections.Generic;
using Project.Networking;
using Project.Levels;
using Project.Items;
using Project.Util;
using Project.Render;
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

		//Camera variables
		readonly float CameraMinPitch = -89.0f;
        readonly float CameraMaxPitch = 89.0f;
        public float CameraPitch = 0.0f;
        public float CameraYaw = 0.0f;
		/// <summary>Used to adjust camera mouse movement speed. Default speed is too fast.</summary>
        public float CameraMouseSensitivity = 0.1f;
		public bool MouseLocked = false;

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
			UpdateInput();
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
			State.CameraPitch = CameraPitch;
			State.CameraYaw = CameraYaw;
			StateBuffer = State;
		}

		/// <summary> Retrieve a copy of the GameState, typically for the renderer. </summary>
		public virtual GameState GetGameState() {
			return StateBuffer;
		}

		/// <summary>Process user input for the current frame<summary>
		private void UpdateInput() {
			UpdateLevelInput();
			UpdatePlayerInput();
			UpdateInventoryInput();
		}

		/// <summary>Input for player movement and camera controls</summary>
		private void UpdatePlayerInput() {
			var player = Level.Player;

			//Player movement
			int ws = Convert.ToInt32(Input.IsKeyDown(Keys.W)) - Convert.ToInt32(Input.IsKeyDown(Keys.S));
			int ad = Convert.ToInt32(Input.IsKeyDown(Keys.A)) - Convert.ToInt32(Input.IsKeyDown(Keys.D));
			int qe = Convert.ToInt32(Input.IsKeyDown(Keys.Q)) - Convert.ToInt32(Input.IsKeyDown(Keys.E));
			int sl = Convert.ToInt32(Input.IsKeyDown(Keys.Space)) - Convert.ToInt32(Input.IsKeyDown(Keys.LeftShift));
			float speed = 0.1f;
			player.Position.X += ad * speed;
			player.Position.Y += ws * speed;

			//Toggle mouse visibility and centering for mouse camera movement
			if (Input.IsKeyPressed(Keys.F1)) {
				MouseLocked = !MouseLocked;
			}
			if(MouseLocked) {
				Program.INSTANCE.CursorGrabbed = true;
				//CursorVisible isn't explicitly set here because that breaks CursorGrabbed for some reason
			}
			else {
				Program.INSTANCE.CursorGrabbed = false;
				Program.INSTANCE.CursorVisible = true;
			}

            //Mouse camera movement
            CameraPitch += -Input.MouseDeltaY * CameraMouseSensitivity;
            CameraYaw += Input.MouseDeltaX * CameraMouseSensitivity;
            CameraPitch = MathUtil.MinMax(CameraPitch, CameraMinPitch, CameraMaxPitch);

            //Print player stats
            if (Input.IsKeyPressed(Keys.P)) {
				Console.WriteLine("\n\n*****Player stats*****");
				Console.WriteLine($"Health: {player.Health}/{player.MaxHealth}");
				Console.WriteLine($"Mana: {player.Mana}/{player.MaxMana}");
				Console.WriteLine($"Armor: {player.Armor}");
				Console.WriteLine($"Carry weight: {player.CarryWeight}");
				Console.WriteLine($"Position: {player.Position}");
			}
		}

		/// <summary>Input for level generation and the minimap</summary>
		private void UpdateLevelInput() {
			//Toggle minimap
			if (Input.IsKeyPressed(Keys.M))
				Level.IsViewingMap = !Level.IsViewingMap;

			//Regenerate level
			if (Input.IsKeyPressed(Keys.G)) {
				Level.TryGenerateLevel(1000);
			}

			//Debug minimap movement
			if (Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift)) {
				Room nextRoom = null;
				if (Input.IsKeyPressed(Keys.D1) && Level.CurrentRoom.ConnectedRooms.Length >= 1)
					nextRoom = Level.CurrentRoom.ConnectedRooms[0];
				if (Input.IsKeyPressed(Keys.D2) && Level.CurrentRoom.ConnectedRooms.Length >= 2)
					nextRoom = Level.CurrentRoom.ConnectedRooms[1];
				if (Input.IsKeyPressed(Keys.D3) && Level.CurrentRoom.ConnectedRooms.Length >= 3)
					nextRoom = Level.CurrentRoom.ConnectedRooms[2];
				if (Input.IsKeyPressed(Keys.D4) && Level.CurrentRoom.ConnectedRooms.Length >= 4)
					nextRoom = Level.CurrentRoom.ConnectedRooms[3];
				if (Input.IsKeyPressed(Keys.D5) && Level.CurrentRoom.ConnectedRooms.Length >= 5)
					nextRoom = Level.CurrentRoom.ConnectedRooms[4];
				if (nextRoom != null) {
					if (nextRoom == Level.PreviousRoom) {
						Console.WriteLine($"Can't move from room {Level.CurrentRoom.Id} to room {nextRoom.Id} since you'd be moving backwards.");
					} else {
						Level.PreviousRoom = Level.CurrentRoom;
						Level.CurrentRoom = nextRoom;
                        Level.Score += 100;
						Renderer.EventQueue.Enqueue("LevelRegenerated"); //Signal to renderer to regenerate map scene
					}
				}
			}
		}

		/// <summary>Input for inventory management</summary>
		private void UpdateInventoryInput() {
			var player = Level.Player;
			var inventory = player.Inventory;

			int numKeyPressed = -1;
			if (Input.IsKeyPressed(Keys.D0))
				numKeyPressed = 0;
			if (Input.IsKeyPressed(Keys.D1))
				numKeyPressed = 1;
			if (Input.IsKeyPressed(Keys.D2))
				numKeyPressed = 2;
			if (Input.IsKeyPressed(Keys.D3))
				numKeyPressed = 3;
			if (Input.IsKeyPressed(Keys.D4))
				numKeyPressed = 4;
			if (Input.IsKeyPressed(Keys.D5))
				numKeyPressed = 5;
			if (Input.IsKeyPressed(Keys.D6))
				numKeyPressed = 6;
			if (Input.IsKeyPressed(Keys.D7))
				numKeyPressed = 7;
			if (Input.IsKeyPressed(Keys.D8))
				numKeyPressed = 8;
			if (Input.IsKeyPressed(Keys.D9))
				numKeyPressed = 9;

			//Press I to print inventory
			if (Input.IsKeyPressed(Keys.I)) {
				inventory.PrintInventoryState();
			} else if (numKeyPressed > -1 && numKeyPressed < inventory.Items.Count) {
				//Get selected item
				Item item = inventory.Items[numKeyPressed];
				inventory.LastItemSelected = item;

				//Print item info
				Console.WriteLine($"\n\n*****{item.Definition.Name}*****");
				Console.WriteLine($"Weight: {item.Definition.Weight}");
				if (item.Definition.Consumeable || item.Definition.IsKey) {
					Console.WriteLine($"Uses: (Press U to use) : {inventory.LastItemSelected.UsesRemaining} uses remaining");
					if (item.Definition.Consumeable)
						Console.WriteLine("\t- Consumeable");
					if (item.Definition.IsKey)
						Console.WriteLine("\t- Key");
				}
			} else if (Input.IsKeyPressed(Keys.U) && inventory.LastItemSelected != null) {
				if (inventory.LastItemSelected.Definition.Consumeable) {
					inventory.LastItemSelected.Consume(player);
					Console.WriteLine($"Consumed {inventory.LastItemSelected.Definition.Name}! {inventory.LastItemSelected.UsesRemaining} uses remaining.");
				} else if (inventory.LastItemSelected.Definition.IsKey) {
					//Not yet implemented
					Console.WriteLine($"You try to use {inventory.LastItemSelected.Definition.Name}, but you have nothing to unlock!");
				} else {
					Console.WriteLine($"{inventory.LastItemSelected.Definition.Name} isn't a useable item!");
				}

				if (inventory.LastItemSelected != null && inventory.LastItemSelected.UsesRemaining == 0) {
					Console.WriteLine($"{inventory.LastItemSelected.Definition.Name} was removed from the inventory since it has 0 uses left.");
					inventory.Items.Remove(inventory.LastItemSelected);
				}
			} else if (Input.IsKeyPressed(Keys.R)) { //Add random items to the inventory
				uint numItemsAdded = inventory.AddRandomItems(5);
				Console.WriteLine($"Added {numItemsAdded} to the inventory.");
			}
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
