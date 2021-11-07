using Project.Networking;
using Project.Levels;
using Project.Items;
using Project.Util;
using Project.Render;
using System;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;

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
		// Interface elements
		public bool IsInGame;
	}

	public class GameLogic {
		/// <summary> Actively modified state, constantly being changed by Update() </summary>
		private GameState State = new GameState();
		/// <summary> "Backbuffer" of the game state, which is only ever changed in one go. This will be the state sent to the renderer. </summary>
		private GameState StateBuffer = new GameState();

		public Level Level;
		public Player Player;
		public int GameTick;

		//Camera variables
		private readonly float CameraMinPitch = -89.0f;
		private readonly float CameraMaxPitch = 89.0f;
		public float CameraPitch = 0.0f;
		public float CameraYaw = 0.0f;
		/// <summary>Used to adjust camera mouse movement speed. Default speed is too fast.</summary>
		public float CameraMouseSensitivity = 0.1f;
		public bool MouseLocked = false;
		public bool IsViewingMap = false;

		public Server Server;

		public GameLogic() {
			StateBuffer.PlayerX = 0.0f;
			StateBuffer.PlayerY = 0.0f;
			StateBuffer.IsViewingMap = false;
			StateBuffer.CameraPitch = 0.0f;
			StateBuffer.CameraYaw = 0.0f;
			StateBuffer.IsInGame = true;
		}

		~GameLogic() {
			Sounds.Cleanup();
		}

		/// <summary> Handles all on-startup tasks, instantiation of objects, or other similar run-once tasks. </summary>
		public virtual void Initialize() {
			ItemDefinition.LoadDefinitions();
			Player = new Player(new Vector2(0.0f, 0.0f));
			Level = LevelGenerator.TryGenerateLevel();
			Level.Player = Player;
			Renderer.EventQueue.Enqueue("LevelRegenerated"); //Signal to renderer to regenerate map scene
			Server = new Server(Level);
			Sounds.Init();
		}

		/// <summary> Primary gameplay loop. Make all your calls and modifications to State, not StateBuffer!</summary>
		public virtual void Update(double deltaTime) {
			//Regenerate level if signalled
			if (Level.NeedsRegen) {
				uint score = Level.Score;
				Level = LevelGenerator.TryGenerateLevel(Level.Depth);
				Level.Score = score;
				Level.Player = Player;
				Renderer.EventQueue.Enqueue("LevelRegenerated"); //Signal to renderer to regenerate map scene
			}

			//Player died, reset level
			if (Player.Health <= 0) {
				Level.CurrentRoom.OnExit(Level, Level.StartRoom); //Call on exit so things like ambient music are stopped
				Sounds.PlaySound("assets/sounds/PlayerDeath0.wav"); //Play death sound
				//Recreate player and generate new level
				Player = new Player(new Vector2(0.0f, 0.0f));
				Level = LevelGenerator.TryGenerateLevel();
				Level.Player = Player;
				Console.WriteLine($"Player died with a score of {Level.Score}!");
				Renderer.EventQueue.Enqueue("LevelRegenerated"); //Signal to renderer to regenerate map scene
			}

			Input.Update();
			Level.Update(deltaTime);
			UpdateInput();
			GameTick++;

			State.PlayerX = Level.Player.Position.X;
			State.PlayerY = Level.Player.Position.Y;
			State.Level = (Level)Level.Clone();
			State.IsViewingMap = IsViewingMap;
			State.Score = (GameTick / 600) + ((int)Level.Score);
			State.CameraPitch = CameraPitch;
			State.CameraYaw = CameraYaw;
			State.IsInGame = true;
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

			//Toggle mouse visibility and centering for mouse camera movement
			if (Input.IsKeyPressed(Keys.Escape)) {
				Renderer.INSTANCE.CursorGrabbed = false;
				Renderer.INSTANCE.CursorVisible = true;
			}

			if (Input.MouseState.IsAnyButtonDown) {
				Renderer.INSTANCE.CursorGrabbed = true;
			}

			//Player and camera movement
			if (Renderer.INSTANCE.CursorGrabbed) {
				//Camera movement
				CameraPitch += -Input.MouseDeltaY * CameraMouseSensitivity;
				CameraYaw -= Input.MouseDeltaX * CameraMouseSensitivity;
				CameraPitch = MathUtil.MinMax(CameraPitch, CameraMinPitch, CameraMaxPitch);

				//Player movement
				int ws = Convert.ToInt32(Input.IsKeyDown(Keys.W)) - Convert.ToInt32(Input.IsKeyDown(Keys.S));
				int ad = Convert.ToInt32(Input.IsKeyDown(Keys.A)) - Convert.ToInt32(Input.IsKeyDown(Keys.D));
				int qe = Convert.ToInt32(Input.IsKeyDown(Keys.Q)) - Convert.ToInt32(Input.IsKeyDown(Keys.E));
				int sl = Convert.ToInt32(Input.IsKeyDown(Keys.Space)) - Convert.ToInt32(Input.IsKeyDown(Keys.LeftShift));

				float yawRadian = CameraYaw * Renderer.RCF; // Angle (in radians) pointing "forwards"
				float yawPerpRadian = (CameraYaw + 90) * Renderer.RCF; // Angle pointing perpendicular to the above angle

				player.Velocity += new Vector2(
					(float)((Math.Sin(yawRadian) * ws) + (Math.Sin(yawPerpRadian) * ad)) * player.MovementSpeed,
					(float)((Math.Cos(yawRadian) * ws) + (Math.Cos(yawPerpRadian) * ad)) * player.MovementSpeed
				);
			}

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
				IsViewingMap = !IsViewingMap;

			//Regenerate level
			if (Input.IsKeyPressed(Keys.G)) {
				Level.NeedsRegen = true;
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
						Level.PreviousRoom.OnExit(Level, Level.CurrentRoom);
						Level.CurrentRoom.OnEnter(Level, Level.PreviousRoom);
						Level.Score += 100;
						Renderer.EventQueue.Enqueue("LevelRegenerated"); //Signal to renderer to regenerate map scene

						Level.CurrentRoom.Visited = Room.VisitedState.Visited;
						foreach (Room c in Level.CurrentRoom.ConnectedRooms) {
							if (c.Visited == Room.VisitedState.NotSeen) {
								c.Visited = Room.VisitedState.Seen;
							}
						}
					}
				}
			}

			//Debug full level visibility
			if (Input.IsKeyPressed(Keys.V)) {
				foreach (Room room in Level.Rooms) {
					room.Visited = Room.VisitedState.Seen;
				}
			}
			//Debug keybind to add all keys required for end room
			if (Input.IsKeyPressed(Keys.K)) {
				foreach(ItemDefinition keyDef in Level.KeyDefinitions) {
					if (!Level.Player.Inventory.Items.Contains(item => item.Definition == keyDef))
						Level.Player.Inventory.Items.Add(new Item(keyDef)); //Add the key if player doesn't have it
				}
			}
		}

		/// <summary>Input for inventory management</summary>
		private void UpdateInventoryInput() {
			var player = Level.Player;
			var inventory = player.Inventory;

			if (Input.IsKeyPressed(Keys.R)) { //Add random items to the inventory
				uint numItemsAdded = inventory.AddRandomItems(5);
				Console.WriteLine($"Added {numItemsAdded} to the inventory.");
			}

			//Update inventory position selector
			if(Input.IsKeyPressed(Keys.Q)) {
				inventory.Position--;
				if (inventory.Position == -1) {
					inventory.Position = inventory.Items.Count - 1;
				}
			}
			if(Input.IsKeyPressed(Keys.E)) {
				inventory.Position++;
				if (inventory.Position == inventory.Items.Count) {
					inventory.Position = 0;
				}
			}
			//Ensure inventory position is in valid range
			inventory.Position = MathUtil.MinMax(inventory.Position, 0, Math.Max(inventory.Items.Count - 1, 0));

			//Use selected item
		 	if (Input.IsKeyPressed(Keys.U) && inventory.Position < inventory.Items.Count) {
				var item = inventory.Items[inventory.Position];
				bool used = false;
				if (item.Definition.Consumeable) {
					item.Consume(player);
					used = true;
					Console.WriteLine($"Consumed {item.Definition.Name}! {item.UsesRemaining} uses remaining.");
				} else if (item.Definition.IsKey) {
					//Not yet implemented
					Console.WriteLine($"You try to use {item.Definition.Name}, but you have nothing to unlock!");
				} else {
					Console.WriteLine($"{item.Definition.Name} isn't a useable item!");
				}

				if (item != null && item.UsesRemaining == 0 && used) {
					Console.WriteLine($"{item.Definition.Name} was removed from the inventory since it has 0 uses left.");
					inventory.Items.Remove(item);
					inventory.Position = Math.Max(0, inventory.Position - 1);
				}
			}
		}
	}

	public class RemoteGameLogic : GameLogic {
		public override void Initialize() {

		}

		public override void Update(double deltaTime) {
			// every 16.6ms (60fps), retreive a new GameState from the server
		}

		public override GameState GetGameState() {
			// retrieve the remote game state from buffer
			return new GameState();
		}
	}
}
