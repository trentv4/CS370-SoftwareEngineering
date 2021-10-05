using System;
using System.Collections.Generic;
using Project.Items;
using OpenTK.Mathematics;
using Project.Render;
using Project.Util;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Linq;

namespace Project.Levels {
	public class Level {
		private readonly Random Random = new System.Random();
		public Player Player;
		public Room[] Rooms;

		///<summary>Goes up with each room you visit before passing the end room. Lower scores are better.</summary>
		public uint Score = 0;

		///<summary>The room that the player is in.</summary>
		public Room CurrentRoom = null;
		///<summary>Previous room that the player was in. Used to stop player from moving backwards.</summary>
		public Room PreviousRoom = null;

		public Room StartRoom = null;
		public Room EndRoom = null;

		public bool IsViewingMap = false;

		public long LastMapRegenTick = 0;

		/// <summary>Current level the player is on. Starts at 0 and increases each time they reach and end room.</summary>
        public int CurrentLevel = 0;

        private int LastLevelGenSeed = 0;

        public List<LevelGenConfig> GenerationConfigs;

        public Level() {
            SetupGenerationConfigs();
            TryGenerateLevel(1000);

            Player.Inventory.PrintInventoryControls();
		}

		/// <summary>Set level generation configs</summary>
		void SetupGenerationConfigs() {
            GenerationConfigs = new List<LevelGenConfig>() {
            	new LevelGenConfig() {NumLevels = 1, NumPrimaryPaths = 2, MinRoomsPerPath = 2, MaxRoomsPerPath = 3, PruneChance = 0.15f, SecondaryPathChance = 0.2f },
            	new LevelGenConfig() {NumLevels = 1, NumPrimaryPaths = 3, MinRoomsPerPath = 3, MaxRoomsPerPath = 4, PruneChance = 0.25f, SecondaryPathChance = 0.4f },
            	new LevelGenConfig() {NumLevels = 1, NumPrimaryPaths = 4, MinRoomsPerPath = 3, MaxRoomsPerPath = 7, PruneChance = 0.5f, SecondaryPathChance = 0.5f },
            };
		}

		/// <summary>Attempts to generate a level the provided number of times. Returns true if one of the attempts is successful and false if they all fail.</summary>
		public bool TryGenerateLevel(int numGenerationAttempts) {
            for (int i = 0; i < numGenerationAttempts; i++)
				if(GenerateNewLevel())
                    return true;

            Console.WriteLine($"ERROR! Tried to regenerate level {numGenerationAttempts} times and failed every time! Seed: {LastLevelGenSeed}");
            return false;
        }

		/// <summary>Generates new level. Can be called multiple times to regenerate the level. Returns true if it succeeds and false if it fails.</summary>
		bool GenerateNewLevel() {
            //Random number generator used to vary level gen
            LastLevelGenSeed = (int)System.DateTime.Now.Ticks; //Seed with time so generation is always different
			var rand = new Random(LastLevelGenSeed);

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
            float centerY = yDelta * numPrimaryPaths / 2;
            float angleMaxDegrees = 12.0f; //Max angle magnitude of each room relative to the previous room
            float angleMaxRadians = angleMaxDegrees * (MathF.PI / 180.0f);
            float xDeltaMax = 10.0f / maxRoomsPerPath; //Maximum x separation between each room on a primary path

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
            var endRoom = new Room(xDeltaMax * (maxRoomsPerPath + 1), centerY);
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

            //Push close rooms away from each other
            int roomSeparationSteps = 5;
            float minPushDistance = 2.0f;
            for (int i = 0; i < roomSeparationSteps; i++) {
                foreach (var room0 in roomsGen) {
                    foreach (var room1 in roomsGen) {
                        if (room0 == room1 || room0 == startRoom || room0 == endRoom || room1 == startRoom || room0 == endRoom)
                            continue;

						//Push rooms away from each other if they're within minPushDistance
                        float distance = (float)room0.DistanceToRoom(room1);
                        if (distance <= minPushDistance) {
                            float strength = 1.0f / (distance * distance);
                            strength *= 0.05f;
                            Vector2 dir0 = (room0.Position - room1.Position).Normalized();
                            Vector2 dir1 = -dir0;

                            room0.Position += dir0 * strength;
                            room1.Position += dir1 * strength;
                        }
                    }
				}
			}

			//Ensure rooms aren't before/after the start/end rooms
			foreach (var room in roomsGen) {
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
			
			Console.WriteLine($"Generated new level with {Rooms.Length} rooms.");
			Renderer.EventQueue.Enqueue("LevelRegenerated"); //Signal to renderer to regenerate map scene
			return true;
		}

		public void Update() {
			Player.Update();

			//Print out score and generate a new level if player completes this one
			if (CurrentRoom == EndRoom) {
				Console.WriteLine($"\n\n*****Level completed with a score of {Score}!*****\n");
				Score = 0;
                CurrentLevel++;
                TryGenerateLevel(10000);
            }

            CheckIfPlayerEnteredDoorway();
            CheckIfPlayerOnItem();
        }

		/// <summary>Moves them to the next room if they collide with a doorway and it doesn't go to the previous room.</summary>
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
                    Renderer.EventQueue.Enqueue("LevelRegenerated"); //Signal to renderer to regenerate scene
                    break;
                }
            }
		}

		/// <summary>If the player is stepping on an item and there's enough room in their inventory the item is picked up.</summary>
		void CheckIfPlayerOnItem() {
            float itemRadius = 0.6f;
			foreach (Item item in CurrentRoom.Items) {
                var itemPos = new Vector2(item.Position.X, item.Position.Z);
                float playerItemDistance = (itemPos - Player.Position).Length;

				//If player is within the items radius attempt to pick it up
				if(playerItemDistance < itemRadius) {
                    bool result = Player.Inventory.AddItem(item);
					if(result) {
						//Added to inventory
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