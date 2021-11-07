using System;
using System.Collections.Generic;
using Project.Items;
using OpenTK.Mathematics;
using Project.Render;
using Project.Util;
using System.Linq;

namespace Project.Levels {
	public class Level : ICloneable {
		///<summary>Goes up with each room you visit before passing the end room. Lower scores are better.</summary>
		public uint Score = 0;
		public Player Player;

		public Room[] Rooms;
		/// <summary> Keys required to pass pass through the end room into the next level. </summary>
		public ItemDefinition[] KeyDefinitions;
		///<summary>The room that the player is in.</summary>
		public Room CurrentRoom = null;
		///<summary>Previous room that the player was in. Used to stop player from moving backwards.</summary>
		public Room PreviousRoom = null;
		public Room StartRoom = null;
		public Room EndRoom = null;

		/// <summary> Increases by one each time the player completes a level. </summary>
		public int Depth = 0;
		/// <summary> Setting this to true causes GameLogic to regenerate the level. </summary>
		public bool NeedsRegen = false;

		/// <summary>Make a deep copy</summary>
		public object Clone() {
			Level copy = new Level();
			copy.Score = Score;
			copy.Player = (Player)Player.Clone();
			copy.Rooms = (Room[])Rooms.Clone();

			//Find room indices and set them for the clone
			List<Room> tempRooms = Rooms.ToList(); //Temporary list since it has .IndexOf() and arrays don't
			if (CurrentRoom != null) {
				int currentRoomIndex = tempRooms.IndexOf(CurrentRoom);
				copy.CurrentRoom = copy.Rooms[currentRoomIndex];
			}
			if (PreviousRoom != null) {
				int previousRoomIndex = tempRooms.IndexOf(PreviousRoom);
				copy.PreviousRoom = copy.Rooms[previousRoomIndex];
			}
			if (StartRoom != null) {
				int startRoomIndex = tempRooms.IndexOf(StartRoom);
				copy.StartRoom = copy.Rooms[startRoomIndex];
			}
			if (EndRoom != null) {
				int endRoomIndex = tempRooms.IndexOf(EndRoom);
				copy.EndRoom = copy.Rooms[endRoomIndex];
			}

			copy.Depth = Depth;
			return copy;
		}

		public void Update(double deltaTime) {
			Player.Update(deltaTime);
			CheckIfPlayerCompletedLevel();
			CheckIfPlayerEnteredDoorway();
            CheckIfPlayerOnItem();
			CurrentRoom.Update(deltaTime, this);
		}

		/// <summary>The current level is completed if the player is inside the end room and has all the keys required to open it.</summary>
		void CheckIfPlayerCompletedLevel() {
			if (CurrentRoom == EndRoom) {
				//Check that the player has the keys needed to pass through
				bool hasRequiredKeys = true;
				foreach (ItemDefinition keyDef in KeyDefinitions)
					if (!Player.Inventory.Items.Contains(item => item.Definition == keyDef))
						hasRequiredKeys = false;

				//Todo: The player should also be required to walk through a special "end room door" so they can still backtrack and grab more loot if needed
				//Has required keys. Print out score and generate a new level.
				if (hasRequiredKeys) {
					//Remove keys from inventory
					foreach(ItemDefinition keyDef in KeyDefinitions) {
						foreach (Item item in Player.Inventory.Items.ToArray()) { //Iterate array copy so we can remove items while iterating
							if (item.Definition == keyDef) {
								Player.Inventory.Items.Remove(item);
								break; //Only remove one instance of each key
							}
						}
					}

					//Trigger regen
					Console.WriteLine($"\n\n*****Level completed with a score of {Score}!*****\n");
                	Depth++;
					NeedsRegen = true;
				} else {
					Console.WriteLine("Don't have all keys needed to pass through the end room. Keys needed:");
					foreach (ItemDefinition keyDef in KeyDefinitions) {
						bool hasKey = Player.Inventory.Items.Contains(item => item.Definition == keyDef);
						Console.WriteLine($"\t- {keyDef.Name} ({(hasKey ? "Holding" : "Not holding")})");
					}
				}
            }
		}

		/// <summary> Moves the player to the next room if they collide with a doorway and it doesn't go to the previous room. </summary>
		void CheckIfPlayerEnteredDoorway() {
			// Loop through each door
			float roomSize = 10.0f;
            float doorSize = 1.0f;
            foreach (Room r in CurrentRoom.ConnectedRooms) {
				float angle = CurrentRoom.AngleToRoom(r);
				Vector3 doorPosition = new Vector3((float)Math.Sin(angle), (float)Math.Cos(angle), 0.0f) * (roomSize - 0.1f);
                float distance = (doorPosition.Xy - Player.Position).Length;

				//If player within certain distance of door and it's not the previous room, move to next room
                if (distance < doorSize && r != PreviousRoom) {
					//Update room variables
                    PreviousRoom = CurrentRoom;
                    CurrentRoom = r;
					PreviousRoom.OnExit(this, CurrentRoom);
					CurrentRoom.OnEnter(this, PreviousRoom);

					//Update score, player position, and room visited state
					Score += 100;
                    Player.Position = new Vector2(0.0f, 0.0f);
					CurrentRoom.Visited = Room.VisitedState.Visited;
					foreach (Room c in CurrentRoom.ConnectedRooms) {
						if (c.Visited == Room.VisitedState.NotSeen) {
							c.Visited = Room.VisitedState.Seen;
						}
					}
                    Renderer.EventQueue.Enqueue("LevelRegenerated"); //Signal to renderer to regenerate scene
                    break;
                }
            }
		}

		/// <summary>If the player is stepping on an item and there's enough room in their inventory the item is picked up.</summary>
		void CheckIfPlayerOnItem() {
            float itemRadius = 0.6f;
			foreach (Item item in CurrentRoom.Items.ToList()) { //Iterate over a copy of the list so we can safely remove items while iterating
				var itemPos = new Vector2(item.Position.X, item.Position.Y);
                float playerItemDistance = (itemPos - Player.Position).Length;

				//If player is within the items radius attempt to pick it up
				if(playerItemDistance < itemRadius) {
                    bool result = Player.Inventory.AddItem(item);
					if(result) {
						//Added to inventory
						Sounds.PlaySound("assets/sounds/ItemPickup0.wav");
						CurrentRoom.Items.Remove(item);
                    	Renderer.EventQueue.Enqueue("LevelRegenerated"); //Signal to renderer to regenerate scene
                        break;
                    }
					else {
						//Failed to add to inventory
                        Console.Write($"Not enough room in inventory for {item.Definition.Name} (weight: {item.Definition.Weight}). ");
						Console.Write($"Inventory status: {Player.Inventory.Weight}/{Player.CarryWeight}\n");
                    }
                }
            }
        }
	}

	public class Room {
		public enum VisitedState {
			NotSeen,
			Seen,
			Visited
		}
		public VisitedState Visited = VisitedState.NotSeen;
		public Vector2 Position;
		public Room[] ConnectedRooms;
		public List<Item> Items = new List<Item>();
		public List<LevelObject> Objects = new List<LevelObject>();
		private static int _currentId = 0;
		//Unique ID used for use as dictionary key
		private readonly int _id = 0;
		public int Id => _id;

		public Room(float X, float Y) {
			this.Position = new Vector2(X, Y);
			_id = _currentId++;
		}

		public double DistanceToRoom(Room otherRoom) {
			return (otherRoom.Position - Position).Length;
		}

		public float AngleToRoom(Room otherRoom) {
			Vector2 connectedRoomPos = otherRoom.Position - Position;
			float angle = (float)Math.Atan2(connectedRoomPos.Y, connectedRoomPos.X);
			return angle;
		}

		/// <summary> Called once each frame. </summary>
		public virtual void Update(double deltaTime, Level level) {
			foreach (LevelObject obj in Objects)
				obj.Update(deltaTime, level);
		}

		/// <summary> Called whenever the player enters this room. </summary>
		public virtual void OnEnter(Level level, Room previousRoom) {

		}

		/// <summary> Called whenever the player exits this room. </summary>
		public virtual void OnExit(Level level, Room nextRoom) {

		}
	}

	/// <summary> Room with wind constantly pushing the player around. </summary>
	public class WindyRoom : Room {
		public Vector2 WindDirection = new Vector2(0.0f);
		public float WindSpeed = 0.0f;
		public static readonly string windSoundEffect = "assets/sounds/Wind0.wav";

		public WindyRoom(float x, float y) : base(x, y) { }
		public override void Update(double deltaTime, Level level) {
			base.Update(deltaTime, level);
			level.Player.Velocity += WindDirection * WindSpeed;
		}

		public override void OnEnter(Level level, Room previousRoom) {
			base.OnEnter(level, previousRoom);
			if (!Sounds.IsSoundPlaying(windSoundEffect))
				Sounds.PlaySound(windSoundEffect); //Start wind sound if it isn't already playing
		}

		public override void OnExit(Level level, Room nextRoom) {
			base.OnExit(level, nextRoom);
			if (nextRoom.GetType() != typeof(WindyRoom))
				Sounds.StopSound(windSoundEffect); //Stop wind sound if next room isn't windy
		}
	}

	/// <summary> Interactable object found in rooms. </summary>
	public class LevelObject {
		public Vector2 Position;
		public readonly string TextureName;

		public LevelObject(Vector2 position, string textureName) {
			Position = position;
			TextureName = textureName;
		}

		/// <summary> Called each frame when the player is in the same room as the object. </summary>
		public virtual void Update(double deltaTime, Level level) {

		}
	}

	/// <summary> Spikes that damage the player if they walk into them. </summary>
	public class FloorSpike : LevelObject {
		/// <summary> The player takes damage if within this distance from the spikes </summary>
		public float Radius;
		/// <summary> How much of the players health gets removed on collision </summary>
		public int Damage;
		/// <summary> Amount of time in seconds that the player can't take damage again after hitting the spikes. </summary>
		public static readonly double SafeTime = 0.65;
		public DateTime LastCollisionTime = DateTime.UnixEpoch;

		public FloorSpike(Vector2 position, string textureName, float radius, int damage) : base(position, textureName) {
			Radius = radius;
			Damage = damage;
		}

		public override void Update(double deltaTime, Level level) {
			base.Update(deltaTime, level);

			//Perform circular collision handling. Both the player and the spikes are treated as circles.
			Player player = level.Player;
			float distance = player.Position.Distance(this.Position);
			TimeSpan timeSinceCollision = DateTime.Now.Subtract(LastCollisionTime);
			if (distance <= Radius) { //Check if player is colliding with the spikes
				//Push player away from the spikes
				Vector2 dir = (player.Position - Position).Normalized();
				player.Position += dir * (Radius - distance);

				//Damage the player if it hasn't happened too recently
				if(timeSinceCollision >= TimeSpan.FromSeconds(SafeTime)) {
					player.Health -= Damage;
					LastCollisionTime = DateTime.Now;
					Sounds.PlaySound("assets/sounds/FloorSpikeHit0.wav");
				}
			}
		}
	}
}