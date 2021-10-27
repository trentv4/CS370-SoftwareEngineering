using System;
using System.Collections.Generic;
using Project.Items;
using OpenTK.Mathematics;
using Project.Render;
using Project.Util;
using System.Linq;

namespace Project.Levels {
	public class Level : ICloneable {
		private readonly Random Random = new System.Random();

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

		/// <summary>Current level the player is on. Starts at 0 and increases each time they reach and end room.</summary>
		public int CurrentLevel = 0;
		private int LevelSeed = 0;

		private readonly List<LevelGenConfig> GenerationConfigs = new List<LevelGenConfig>() {
				new LevelGenConfig() {NumLevels = 1, NumPrimaryPaths = 2, MinRoomsPerPath = 2, MaxRoomsPerPath = 3, PruneChance = 0.15f, SecondaryPathChance = 0.2f },
				new LevelGenConfig() {NumLevels = 1, NumPrimaryPaths = 3, MinRoomsPerPath = 3, MaxRoomsPerPath = 4, PruneChance = 0.25f, SecondaryPathChance = 0.4f },
				new LevelGenConfig() {NumLevels = 1, NumPrimaryPaths = 4, MinRoomsPerPath = 3, MaxRoomsPerPath = 7, PruneChance = 0.5f, SecondaryPathChance = 0.5f },
			};

		public Level(bool generateLevel = true) {
			if (generateLevel)
				TryGenerateLevel(1000);
		}

		/// <summary>Make a deep copy</summary>
		public object Clone() {
			Level copy = new Level(false);
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

			copy.CurrentLevel = CurrentLevel;
			copy.LevelSeed = LevelSeed;
			return copy;
		}

		/// <summary> Attempts to generate a level the provided number of times. Returns true if one of the attempts is successful and false if they all fail. </summary>
		public bool TryGenerateLevel(int numGenerationAttempts) {
            for (int i = 0; i < numGenerationAttempts; i++)
				if(GenerateNewLevel())
                    return true;

			throw new Exception($"ERROR! Tried to regenerate level {numGenerationAttempts} times and failed every time! Seed: {LevelSeed}");
        }

		/// <summary> Generates new level. Can be called multiple times to regenerate the level. Returns true if it succeeds and false if it fails. </summary>
		private bool GenerateNewLevel() {
			//Random number generator used to vary level gen
			LevelSeed = (int)System.DateTime.Now.Ticks; //Seed with time so generation is always different
			Random rand = new Random(LevelSeed);

            //Room generation config
            LevelGenConfig genSettings = null;
            int level = 0;
            int index = 0;
            foreach (var config in GenerationConfigs) {
                genSettings = config;
                if(CurrentLevel >= level && CurrentLevel < level + config.NumLevels)
                    break;

                level += config.NumLevels;
                index++;
            }
            if(genSettings == null) //Use final config if none is found for current level
                genSettings = GenerationConfigs[GenerationConfigs.Count - 1];

			int minRoomsPerPath = genSettings.MinRoomsPerPath;
			int maxRoomsPerPath = genSettings.MaxRoomsPerPath;
            int numPrimaryPaths = genSettings.NumPrimaryPaths;
            double pruneChance = genSettings.PruneChance;
            float secondaryPathChance = genSettings.SecondaryPathChance;

			float yDelta = 5.0f / numPrimaryPaths; //Vertical separation between each primary path
            float xDeltaMax = 10.0f / maxRoomsPerPath; //Maximum x separation between each room on a primary path
            float centerY = yDelta * numPrimaryPaths / 2; //Map center y 
            float angleMaxDegrees = 12.0f; //Max angle magnitude of each room relative to the previous room
            float angleMaxRadians = angleMaxDegrees * (MathF.PI / 180.0f);

            //Level generation state
            var roomsGen = new List<Room>();
			var connections = new Dictionary<Room, List<Room>>();
			var pathEnds = new List<Room>(); //End rooms of each primary path
            var primaryPaths = new List<List<Room>>(); //Rooms in each primary path

            //Create start room
            var startRoom = new Room(-0.5f, centerY);
			roomsGen.Add(startRoom);
			connections[startRoom] = new List<Room>();

            //Generate primary paths
            for (uint i = 0; i < numPrimaryPaths; i++) {
                int roomsInPath = rand.Next(minRoomsPerPath, maxRoomsPerPath);
                float xDelta = 10.0f / (roomsInPath + 1);
                float roomX = xDelta;
                float roomY = yDelta * i + 0.5f;
                Room lastRoom = null;
                Room curRoom = startRoom;
                var path = new List<Room>();
                primaryPaths.Add(path);

                for (int j = 0; j < roomsInPath; j++) {
					//Calculate position of next room
					if(j != 0) {
                    	float angle = ((float)rand.NextDouble() * 2.0f - 1.0f) * angleMaxRadians;
                    	roomX += xDelta * MathF.Cos(angle);
                    	roomY += xDelta * MathF.Sin(angle);
					}

                    //Create next room
                    lastRoom = curRoom;
                    curRoom = new Room(roomX, roomY);
                    roomsGen.Add(curRoom);
                    path.Add(curRoom);
                    connections[curRoom] = new List<Room>();
                	connections[curRoom].Add(lastRoom);
                	connections[lastRoom].Add(curRoom);
                }
                pathEnds.Add(curRoom);
            }

            //Add end room
            var endRoom = new Room(xDeltaMax * maxRoomsPerPath + 0.5f, centerY);
			roomsGen.Add(endRoom);
			connections[endRoom] = new List<Room>();

			//Connect primary path end points to end room
			foreach (Room room in pathEnds) {
                connections[room].Add(endRoom);
                connections[endRoom].Add(room);
            }

            //Generate secondary paths that branch off primary paths
            foreach (List<Room> path in primaryPaths) {
                for (int i = 0; i < path.Count; i++) {
                    //Chance on each primary path room to have a secondary path
                    if (rand.NextDouble() <= secondaryPathChance) {
                     	Room primary = path[i];

                        //Look for another primary in range to connect this room with
                        float closestRoom = float.PositiveInfinity;
                        Room nextRoom = null;
                        foreach (var room in roomsGen) {
							if(room == primary || path.Contains(room) || room == startRoom || room == endRoom)
                                continue;
							if(connections[room].Contains(primary) || connections[primary].Contains(room))
                                continue;

                            float dist = (float)primary.DistanceToRoom(room);
							if(dist < closestRoom) {
								closestRoom = dist;
                                nextRoom = room;
                            }
                        }

						if(nextRoom != null) { //Connect to another primary
                            connections[primary].Add(nextRoom);
                            connections[nextRoom].Add(primary);
                        }
						else { //Form a separate branch
							//Todo: Rewrite this so branches do more than just looping back into the same path
							Room nextPrimary = (i == path.Count - 1) ? path[i - 1] : path[i + 1];
                        	float xDelta = 10.0f / (path.Count + 1);
                        	float minSecondaryAngle = 5.0f;
                        	float angle = ((float)rand.NextDouble() * 2.0f - 1.0f) * angleMaxRadians;
                        	angle = Math.Sign(angle) * Math.Max(minSecondaryAngle, Math.Abs(angle));
                        	float roomX = primary.Position.X;
                        	float roomY = primary.Position.Y;
                        	roomX += xDelta * MathF.Cos(angle) * 0.5f;
                        	roomY += xDelta * MathF.Sin(angle) * 0.5f;

                        	var room = new Room(roomX, roomY);
                        	roomsGen.Add(room);
                        	connections[room] = new List<Room>();
                        	connections[room].Add(primary);
                        	connections[primary].Add(room);

                        	connections[room].Add(nextPrimary);
                        	connections[nextPrimary].Add(room);
						}
                    }
                }
            }

			//Determine min/max room positions pre room push step
			Vector2 prePushMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
			Vector2 prePushMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
			foreach(var room in roomsGen) {
				if(room == startRoom || room == endRoom)
					continue;

				if (room.Position.X < prePushMin.X)
					prePushMin.X = room.Position.X;
				if (room.Position.Y < prePushMin.Y)
					prePushMin.Y = room.Position.Y;
				if (room.Position.X > prePushMax.X)
					prePushMax.X = room.Position.X;
				if (room.Position.Y > prePushMax.Y)
					prePushMax.Y = room.Position.Y;
			}

			//Push close rooms away from each other
			int roomSeparationSteps = 5;
            float maxPushDistance = 2.0f; //Maximum distance a room can be pushed each step
            for (int i = 0; i < roomSeparationSteps; i++) {
                foreach (var room0 in roomsGen) {
                    foreach (var room1 in roomsGen) {
                        if (room0 == room1 || room0 == startRoom || room0 == endRoom || room1 == startRoom || room0 == endRoom)
                            continue;

						//Push rooms away from each other if they're within minPushDistance
                        float distance = (float)room0.DistanceToRoom(room1);
                        if (distance <= maxPushDistance) {
                            float strength = 1.0f / (distance * distance);
                            strength *= 0.05f;
                            Vector2 dir0 = (room0.Position - room1.Position).Normalized();
                            Vector2 dir1 = -dir0;

                            room0.Position += dir0 * strength;
                            room1.Position += dir1 * strength;

							//How much to increase min/max bounds by during push step. Currently disabled but left in for future tweaking
							float pushStepBoundsIncrease = 0.0f;
							//Ensure rooms aren't pushed out of bounds
							room0.Position.X = MathUtil.MinMax(room0.Position.X, prePushMin.X - pushStepBoundsIncrease, prePushMax.X + pushStepBoundsIncrease);
							room0.Position.Y = MathUtil.MinMax(room0.Position.Y, prePushMin.Y - pushStepBoundsIncrease, prePushMax.Y + pushStepBoundsIncrease);
							room1.Position.X = MathUtil.MinMax(room1.Position.X, prePushMin.X - pushStepBoundsIncrease, prePushMax.X + pushStepBoundsIncrease);
							room1.Position.Y = MathUtil.MinMax(room1.Position.Y, prePushMin.Y - pushStepBoundsIncrease, prePushMax.Y + pushStepBoundsIncrease);
                        }

                    }
				}
			}

			//Ensure rooms aren't before/after the start/end rooms
			foreach (var room in roomsGen) {
				if(room == startRoom || room == endRoom)
					continue;

				room.Position.X = MathUtil.MinMax(room.Position.X, startRoom.Position.X + xDeltaMax * 0.5f, endRoom.Position.X - xDeltaMax * 0.5f);
			}

			//Iterate rooms in random order and prune some connections
			foreach (int i in Enumerable.Range(0, roomsGen.Count).OrderBy(i => rand.Next())) {
                Room room = roomsGen[i];
            	var roomConnections = connections[room];
				if (room == startRoom || room == endRoom)
                    continue;
                if (roomConnections.Count <= 2 || rand.NextDouble() > pruneChance)
                    continue;

                //Find a connection where the room on the other end will still have >= 2 connections
                if (roomConnections.Count > 2) {
					Room room2 = null;
					foreach (var connectedRoom in roomConnections) {
						if (connections[connectedRoom].Count > 2) {
							room2 = connectedRoom;
							break;
						}
					}

                    //Remove connection
                    if (room2 != null) {
						roomConnections.Remove(room2);
                        connections[room2].Remove(room);
                    }
				}
            }

            //Do floodfill to see if theres a path from start to finish, and to detect stranded rooms
            {
                var checkedRooms = new List<Room>();
                var roomQueue = new Queue<Room>();
                roomQueue.Enqueue(startRoom);
                bool success = false;
                while (roomQueue.Count > 0) {
                    //Get next room from queue
                    var room = roomQueue.Dequeue();
                    checkedRooms.Add(room);

                    //Check if we've reached the end room
                    if (room == endRoom)
                        success = true;

                    //Push connections onto queue if they haven't already been checked
                    foreach (var connection in connections[room])
                        if (!checkedRooms.Contains(connection))
                            roomQueue.Enqueue(connection);
                }

                //Fail level generation if start and end rooms aren't connected
                if (!success) {
                    Console.WriteLine("Level generation error! No valid path from start room to end room.");
                    return false;
                }

                //Get a list of rooms that aren't connected to the start room
                var strandedRooms = new List<Room>(); //Rooms that don't have a path to the start room
                foreach (var room in roomsGen)
                    if (!checkedRooms.Contains(room))
                        strandedRooms.Add(room);

                //Remove rooms that aren't connected to the start room.
                //Done in two steps since you shouldn't enumerate a list (roomsGen) while removing items from it
                foreach (var room in strandedRooms) {
                    roomsGen.Remove(room);

                    //Iterate all other rooms and remove their connections to this one if present
                    foreach (var room2 in roomsGen) {
                        var room2Connections = connections[room2];
                        if (room2Connections.Contains(room))
                            room2Connections.Remove(room);
                    }
                }
            }

			//Check again that all rooms have >= 2 connections
			foreach (var room in roomsGen) {
            	if (connections[room].Count < 2) {
            		Console.WriteLine($"Level generation error! Room {room.Id} has < 2 connections.");
            		return false;
            	}
            }

			//Add random non-key items to each room
			foreach (var room in roomsGen) {
				int numItemsToAdd = rand.Next(0, 5);
				for (int i = 0; i < numItemsToAdd; i++) {
					//Get random item definition that isn't a key
					ItemDefinition def = null;
					while (def == null) {
						ItemDefinition temp = ItemDefinition.Definitions[rand.Next(ItemDefinition.Definitions.Count)];
						if (!temp.IsKey)
							def = temp;
					}

					//Add item to the room in random position
					var item = new Item(def);
					room.Items.Add(item);
					float x = (float)((rand.NextDouble() - 0.5) * 5.0);
					float y = (float)((rand.NextDouble() - 0.5) * 5.0);
					item.Position = new Vector2(x, y);
				}
			}

			//Get 3 key definitions
			var keyDefs = new List<ItemDefinition>();
			foreach (int i in Enumerable.Range(0, ItemDefinition.Definitions.Count).OrderBy(i => rand.Next())) {
				if (keyDefs.Count == 3)
					break;

				ItemDefinition def = ItemDefinition.Definitions[i];
				if (def.IsKey)
					keyDefs.Add(def);
			}
			//Store key defs for end room checks
			KeyDefinitions = keyDefs.ToArray();

			//Add keys to rooms somewhere in the map
			foreach(ItemDefinition keyDef in keyDefs) {
				while(true) {
					//Pick a random room
					Room room = roomsGen[rand.Next(0, roomsGen.Count)];

					//Use room if it's not the start/end room and doesn't already have a key
					if (room != startRoom && room != endRoom && room.Items.Find(item => item.Definition.IsKey) == null) {
						//Create key and add it to the room in a random position
						var key = new Item(keyDef);
						room.Items.Add(key);
						float x = (float)((rand.NextDouble() - 0.5) * 5.0);
						float y = (float)((rand.NextDouble() - 0.5) * 5.0);
						key.Position = new Vector2(x, y);
						break;
					}
				}
			}

			//Set final rooms list and their connections
			foreach (var room in roomsGen) {
				var connectedRooms = connections[room];
				room.ConnectedRooms = connectedRooms.ToArray();
			}
			Rooms = roomsGen.ToArray();
			StartRoom = startRoom;
			EndRoom = endRoom;

			//Spawn the player in the start room
			Player = new Player(new Vector2(0.0f, 0.0f));
			CurrentRoom = startRoom;
			PreviousRoom = startRoom;

			CurrentRoom.Visited = Room.VisitedState.Visited;
			foreach (Room c in CurrentRoom.ConnectedRooms) {
				c.Visited = Room.VisitedState.Seen;
			}

			Console.WriteLine($"Generated new level with {Rooms.Length} rooms.");
			Renderer.EventQueue.Enqueue("LevelRegenerated"); //Signal to renderer to regenerate map scene
			return true;
		}

		public void Update() {
			CheckIfPlayerCompletedLevel();
			CheckIfPlayerEnteredDoorway();
            CheckIfPlayerOnItem();
        }

		/// <summary>The current level is completed if the player is inside the end room and has all the keys required to open it.</summary>
		void CheckIfPlayerCompletedLevel() {
			if (CurrentRoom == EndRoom) {
				//Check that the player has the keys needed to pass through
				bool hasRequiredKeys = true;
				foreach (ItemDefinition keyDef in KeyDefinitions)
					if (Player.Inventory.Items.Find(item => item.Definition == keyDef) == null)
						hasRequiredKeys = false;

				//Todo: The player should also be required to walk through a special "end room door" so they can still backtrack and grab more loot if needed
				//Has required keys. Print out score and generate a new level.
				if(hasRequiredKeys) {
					Console.WriteLine($"\n\n*****Level completed with a score of {Score}!*****\n");
					Score = 0;
                	CurrentLevel++;
                	TryGenerateLevel(10000);
				} else {
					Console.WriteLine("Don't have all keys needed to pass through the end room. Keys needed:");
					foreach (ItemDefinition keyDef in KeyDefinitions) {
						bool hasKey = Player.Inventory.Items.Find(item => item.Definition == keyDef) != null;
						Console.WriteLine($"\t- {keyDef.Name} ({(hasKey ? "Holding" : "Not holding")})");
					}
				}
            }
		}

		/// <summary> Moves the player to the next room if they collide with a doorway and it doesn't go to the previous room. </summary>
		void CheckIfPlayerEnteredDoorway() {
			// Loop through each door
			float roomSize = 10.0f;
            float doorSize = 0.8f;
            foreach (Room r in CurrentRoom.ConnectedRooms) {
				float angle = CurrentRoom.AngleToRoom(r);
				Vector3 doorPosition = new Vector3((float)Math.Sin(angle), (float)Math.Cos(angle), 0.0f) * (roomSize - 0.1f);
                float distance = (doorPosition.Xy - Player.Position).Length;

				//If player within certain distance of door and it's not the previous room, move to next room
                if(distance < doorSize && r != PreviousRoom) {
                    PreviousRoom = CurrentRoom;
                    CurrentRoom = r;
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
	}

	/// <summary>Configuration for level generation</summary>
	public class LevelGenConfig {
		/// <summary>The number of levels to use this config for before moving to the next one</summary>
        public int NumLevels;
		/// <summary>The number of rows in the level, excluding the start and end rows</summary>
        public int NumPrimaryPaths;
		/// <summary>Minimum number of rooms per row</summary>
        public int MinRoomsPerPath;
		/// <summary>Maximum number of rooms per row</summary>
        public int MaxRoomsPerPath;
		/// <summary>The chance of pruning connections for rooms when they have < MaxConnections.</summary>
        public float PruneChance;
        /// <summary>The chance of a secondary path forming off of each room on a primary path</summary>
        public float SecondaryPathChance;
    }
}