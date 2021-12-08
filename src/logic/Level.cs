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
	 	private String backgroundMusicFile = "assets/sounds/music/AncientRuins.wav";
		private float backgroundMusicGain = 0.5f;

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

			//Get indices of connected rooms
			var connections = new Dictionary<int, List<int>>();
			for (int i = 0; i < Rooms.Length; i++) {
				Room room = Rooms[i];
				var connectedRoomIndices = new List<int>();
				connections[i] = connectedRoomIndices;
				foreach (Room connection in room.ConnectedRooms) {
					for (int j = 0; j < Rooms.Length; j++) {
						if (connection == Rooms[j]) {
							connectedRoomIndices.Add(j);
							break;
						}
					}
				}
			}
			//Update connected rooms in clone since they still reference the original rooms
			foreach(var kv in connections) {
				Room room = copy.Rooms[kv.Key]; //Room in the level copy
				List<int> connectedIndices = kv.Value;
				for (int i = 0; i < room.ConnectedRooms.Length; i++)
					room.ConnectedRooms[i] = copy.Rooms[connectedIndices[i]];
			}

			copy.Depth = Depth;
			return copy;
		}

		public void Update(double deltaTime) {
			Player.Update(deltaTime, CurrentRoom);
			CheckIfPlayerEnteredDoorway();
            CheckIfPlayerOnItem();
			CurrentRoom.Update(deltaTime, this);

			//Start background music
			if(!Sounds.IsSoundPlaying(backgroundMusicFile))
				Sounds.PlaySound(backgroundMusicFile, true, backgroundMusicGain);
		}

		/// <summary> Moves the player to the next room if they collide with a doorway and it doesn't go to the previous room. </summary>
		void CheckIfPlayerEnteredDoorway() {
			// Loop through each door
			float roomSize = Room.RoomRadius;
            float doorSize = 0.65f;
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

	public class Room : ICloneable {
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
		public string FloorTexture = "plane.png";
		public float FloorFriction = DefaultFloorFriction;
		public static readonly float DefaultFloorFriction = 0.65f;
		public static readonly float RoomRadius = 8.5f;
		private static int _currentId = 0;
		//Unique ID used for use as dictionary key
		private readonly int _id = 0;
		public int Id => _id;

		public Room(float X, float Y) {
			this.Position = new Vector2(X, Y);
			_id = _currentId++;
		}

		/// <summary> Clone base class data. Derived classes can use this instead of duplicating the clone code </summary>
		public void CloneBase(Room clone) {
			clone.Visited = Visited;
			clone.Items = new List<Item>();
			clone.Objects = new List<LevelObject>();
			clone.ConnectedRooms = (Room[])ConnectedRooms.Clone(); //Makes a shallow copy, Level.Clone() updates references
			clone.FloorTexture = FloorTexture;
			foreach (Item item in Items)
				clone.Items.Add((Item)item.Clone());
			foreach (LevelObject obj in Objects)
				clone.Objects.Add((LevelObject)obj.Clone());
		}

		public virtual object Clone() {
			var room = new Room(Position.X, Position.Y);
			CloneBase(room);
			return room;
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

			//Keep player in room bounds
			Vector2 direction = level.Player.Position.Normalized();
			float distance = level.Player.Position.Length;
			if (distance >= RoomRadius)
				level.Player.Position += direction * (RoomRadius - distance); //Push back into room if outside of it
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
		public float DirectionChangePeriod = 5.0f; //Time between wind direction changes
		private float _directionChangeTimer = 0.0f;
		public static readonly string windSoundEffect = "assets/sounds/Wind0.wav";

		public WindyRoom(float x, float y) : base(x, y) { }
		
		public override object Clone() {
			var room = new WindyRoom(Position.X, Position.Y);
			CloneBase(room);
			room.WindDirection = WindDirection;
			room.WindSpeed = WindSpeed;
			return room;
		}

		public override void Update(double deltaTime, Level level) {
			base.Update(deltaTime, level);

			//Push the player around
			level.Player.Velocity += WindDirection * WindSpeed;

			//Periodically change wind direction
			_directionChangeTimer += (float)deltaTime;
			if (_directionChangeTimer >= DirectionChangePeriod) {
				_directionChangeTimer = 0.0f;
				WindDirection = new Random().NextVec2();
			}
		}

		public override void OnEnter(Level level, Room previousRoom) {
			base.OnEnter(level, previousRoom);
			if (!Sounds.IsSoundPlaying(windSoundEffect))
				Sounds.PlaySound(windSoundEffect, true, 3.0f); //Start wind sound if it isn't already playing
		}

		public override void OnExit(Level level, Room nextRoom) {
			base.OnExit(level, nextRoom);
			if (nextRoom.GetType() != typeof(WindyRoom))
				Sounds.StopSound(windSoundEffect); //Stop wind sound if next room isn't windy
		}
	}

	
	/// <summary> Room with slippery floors.  </summary>
	public class IcyRoom : Room {
		public IcyRoom(float x, float y) : base(x, y) { }

		public override object Clone() {
			var room = new IcyRoom(Position.X, Position.Y);
			CloneBase(room);
			room.FloorFriction = FloorFriction;
			return room;
		}

		public override void Update(double deltaTime, Level level) {
			base.Update(deltaTime, level);
			base.FloorTexture = "Ice.png";
		}

		public override void OnEnter(Level level, Room previousRoom) {
			base.OnEnter(level, previousRoom);
			//Make the floors slippery and movement slower while in icy rooms
			FloorFriction = 0.01f;
			level.Player.MovementSpeed = 0.1f;
		}

		public override void OnExit(Level level, Room nextRoom) {
			base.OnExit(level, nextRoom);
			FloorFriction = DefaultFloorFriction;
			level.Player.MovementSpeed = Player.DefaultMovementSpeed;
		}
	}

	/// <summary> Room at the end of each level. Contains a door that requires all 3 keys to go through </summary>
	public class EndRoom : Room {
		
		public EndRoom(float x, float y) : base(x, y) {
			//Note: Texture is likely a placeholder. Could probably make something that fits the aesthetic better with a model + volumetric fog
			Objects.Add(new EndRoomDoor(new Vector2(0.0f, 6.0f), "EndRoomPortal.png", 4.0f));
		}

		public override object Clone() {
			var room = new EndRoom(Position.X, Position.Y);
			CloneBase(room);
			return room;
		}

		public override void Update(double deltaTime, Level level) {
			base.Update(deltaTime, level);
		}

		public override void OnEnter(Level level, Room previousRoom) {
			base.OnEnter(level, previousRoom);
		}

		public override void OnExit(Level level, Room nextRoom) {
			base.OnExit(level, nextRoom);
		}
	}

	/// <summary> Interactable object found in rooms. </summary>
	public class LevelObject : ICloneable {
		public Vector2 Position;
		public readonly string TextureName;
		public float Scale = 1.0f;

		public LevelObject(Vector2 position, string textureName, float scale = 1.0f) {
			Position = position;
			TextureName = textureName;
			Scale = scale;
		}

		public virtual object Clone() {
			var clone = new LevelObject(Position, TextureName);
			return clone;
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

		public FloorSpike(Vector2 position, string textureName, float radius, int damage) : base(position, textureName) {
			Radius = radius;
			Damage = damage;
		}

		public override object Clone() {
			var clone = new FloorSpike(Position, TextureName, Radius, Damage);
			return clone;
		}

		public override void Update(double deltaTime, Level level) {
			base.Update(deltaTime, level);

			//Perform circular collision handling. Both the player and the spikes are treated as circles.
			Player player = level.Player;
			float distance = player.Position.Distance(this.Position);
			if (distance <= Radius) { //Check if player is colliding with the spikes
				//Push player away from the spikes
				Vector2 dir = (player.Position - Position).Normalized();
				player.Position += dir * (Radius - distance);

				//Damage the player
				if (player.TryDamage(Damage))
					Sounds.PlaySound("assets/sounds/FloorSpikeHit0.wav");
			}
		}
	}

	/// <summary> Door found in level end rooms. Walking into it sends the player to the next level. Requires all three keys </summary>
	public class EndRoomDoor : LevelObject {
		/// <summary> Time in seconds since last time the player was checked for end room keys. Used to prevent console spam. </summary>
		private float _timeSinceLastKeyCheck = float.PositiveInfinity;

		public EndRoomDoor(Vector2 position, string textureName, float scale = 1.0f) : base(position, textureName, scale) {

		}

		public override object Clone() {
			var clone = new EndRoomDoor(Position, TextureName);
			return clone;
		}

		public override void Update(double deltaTime, Level level) {
			base.Update(deltaTime, level);

			//If player collides with it and has all the keys, send them to the next level
			Player player = level.Player;
			float distance = player.Position.Distance(this.Position);
			float Radius = 2.5f;
			if (distance <= Radius) { //Check if player is colliding with the spikes
				OnCollide(deltaTime, level);
			}
		}

		/// <summary> Called whenever the player collides with the door. Sends them to the next level if they have all the keys. </summary>
		private void OnCollide(double deltaTime, Level level) {
			Player player = level.Player;

			//Check that the player has the keys needed to pass through
			bool hasRequiredKeys = true;
			_timeSinceLastKeyCheck += (float)deltaTime;
			foreach (ItemDefinition keyDef in level.KeyDefinitions)
				if (!player.Inventory.Items.Contains(item => item.Definition == keyDef))
					hasRequiredKeys = false;

			//Has required keys. Print out score and generate a new level.
			if (hasRequiredKeys) {
				Sounds.PlaySound("assets/sounds/Portal0.wav");

				//Remove keys from inventory
				foreach(ItemDefinition keyDef in level.KeyDefinitions) {
					foreach (Item item in player.Inventory.Items.ToArray()) { //Iterate array copy so we can remove items while iterating
						if (item.Definition == keyDef) {
							player.Inventory.Items.Remove(item);
							break; //Only remove one instance of each key
						}
					}
				}
			
				//Trigger regen
				Console.WriteLine($"\n\n*****Level completed with a score of {level.Score}!*****\n");
               	level.Depth++;
				level.NeedsRegen = true;
			} else if(_timeSinceLastKeyCheck >= 3.5f) {
				_timeSinceLastKeyCheck = 0.0f;
				Console.WriteLine("Don't have all keys needed to pass through the end room. Keys needed:");
				foreach (ItemDefinition keyDef in level.KeyDefinitions) {
					bool hasKey = player.Inventory.Items.Contains(item => item.Definition == keyDef);
					Console.WriteLine($"\t- {keyDef.Name} ({(hasKey ? "Holding" : "Not holding")})");
				}
				Sounds.PlaySound("assets/sounds/PortalMissingKeys.wav");
			}
		}
	}
}