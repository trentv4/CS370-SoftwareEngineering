using System;
using System.Collections.Generic;
using Project.Items;
using OpenTK.Mathematics;
using Project.Render;
using Project.Util;
using OpenTK.Windowing.GraphicsLibraryFramework;

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

        public List<LevelGenConfig> GenerationConfigs;

        public Level() {
            SetupGenerationConfigs();
            TryGenerateLevel(1000);

            Player.Inventory.PrintInventoryControls();
		}

		/// <summary>Set level generation configs</summary>
		void SetupGenerationConfigs() {
            GenerationConfigs = new List<LevelGenConfig>() {
            	new LevelGenConfig() {NumLevels = 2, Rows = 3, MinRoomsPerRow = 1, MaxRoomsPerRow = 2, MaxConnections = 2, PruneChance = 0.9f, MaxConnectionDistance = 3.0f},
            	new LevelGenConfig() {NumLevels = 2, Rows = 4, MinRoomsPerRow = 1, MaxRoomsPerRow = 2, MaxConnections = 3, PruneChance = 0.0f, MaxConnectionDistance = 3.5f},
            	new LevelGenConfig() {NumLevels = 4, Rows = 4, MinRoomsPerRow = 2, MaxRoomsPerRow = 3, MaxConnections = 3, PruneChance = 0.5f, MaxConnectionDistance = 3.0f},
            	new LevelGenConfig() {NumLevels = 4, Rows = 5, MinRoomsPerRow = 2, MaxRoomsPerRow = 3, MaxConnections = 3, PruneChance = 0.6f, MaxConnectionDistance = 3.0f},
            	new LevelGenConfig() {NumLevels = 5, Rows = 5, MinRoomsPerRow = 3, MaxRoomsPerRow = 4, MaxConnections = 4, PruneChance = 0.6f, MaxConnectionDistance = 3.0f},
            	new LevelGenConfig() {NumLevels = 8, Rows = 5, MinRoomsPerRow = 3, MaxRoomsPerRow = 5, MaxConnections = 5, PruneChance = 0.6f, MaxConnectionDistance = 3.0f}
            };
		}

		/// <summary>Attempts to generate a level the provided number of times. Returns true if one of the attempts is successful and false if they all fail.</summary>
		bool TryGenerateLevel(int numGenerationAttempts) {
            for (int i = 0; i < numGenerationAttempts; i++)
				if(GenerateNewLevel())
                    return true;

            Console.WriteLine($"ERROR! Tried to regenerate level {numGenerationAttempts} times and failed every time!");
            return false;
        }

		///<summary>Generates new level. Can be called multiple times to regenerate the level. Returns true if it succeeds and false if it fails.
		///</summary>
		bool GenerateNewLevel() {
			Console.WriteLine("\nGenerating new level...");

            //Temporary room list for generation
            var roomsGen = new List<Room>();
			var connections = new Dictionary<Room, List<Room>>();
			var rows = new List<List<Room>>();

			//Random number generator used to vary level gen
			int randSeed = (int)System.DateTime.Now.Ticks; //Seed with time so generation is always different
			var rand = new Random(randSeed);
			Console.WriteLine($"Level generation seed: {randSeed}");

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

            int numRows = genSettings.Rows; //Num rows excluding start and end room
			int minRoomsPerRow = genSettings.MinRoomsPerRow;
			int maxRoomsPerRow = genSettings.MaxRoomsPerRow;
            int maxConnections = genSettings.MaxConnections;
            double pruneChance = genSettings.PruneChance;
            float maxConnectionDistance = genSettings.MaxConnectionDistance;
            float centerY = (float)maxRoomsPerRow / 2.0f;

			//Create start room and its row
			var startRoom = new Room(0, centerY);
			roomsGen.Add(startRoom);
			connections[startRoom] = new List<Room>();
			var startRow = new List<Room>();
			startRow.Add(startRoom);
			rows.Add(startRow);

			//Generate rooms and set their positions
			float rowX = 1.0f;
			for (int i = 1; i < numRows + 1; i++) {
				var row = new List<Room>();
				int numRooms = rand.Next(minRoomsPerRow, maxRoomsPerRow + 1);
				float roomDistY = 6.0f / (float)numRooms;
				float roomY = (float)(rand.NextDouble() / 2.0); //Random initial Y pos

				for (int j = 0; j < numRooms; j++) {
					var room = new Room(rowX, roomY);
					row.Add(room);
					roomsGen.Add(room);
					connections[room] = new List<Room>();
					roomY += roomDistY;
				}

				//Adjust next row x pos with some random variation
				rowX += 1.5f + ((float)(rand.NextDouble() / 2.0) - 0.125f);
				rows.Add(row);
			}

			//Add end room and its row
			var endRoom = new Room(rowX, centerY);
			roomsGen.Add(endRoom);
			connections[endRoom] = new List<Room>();
			var endRow = new List<Room>();
			endRow.Add(endRoom);
			rows.Add(endRow);

			//Add random items to each room
			foreach (var room in roomsGen) {
				int numItemsToAdd = rand.Next(0, 5);
				for (int i = 0; i < numItemsToAdd; i++) {
					//Pick random item and add it to the room
					var def = ItemManager.Definitions[rand.Next(ItemManager.Definitions.Count)];
					var item = new Item(def);
					room.Items.Add(item);

					//Pick random position for the item
					float x = (float)((rand.NextDouble() - 0.5) * 5.0);
					float y = (float)((rand.NextDouble() - 0.5) * 5.0);
					item.Position = new Vector3(x, 0.0f, y);
				}
			}

			//Connect all rooms between adjacent rows
			List<Room> lastRow = null;
			foreach (var row in rows) { //Loop through rows
				if (lastRow == null) {
					lastRow = row;
					continue;
				}

				//Connect each room in this row to all rooms in the previous row
				foreach (var room in row) {
					var connectedRooms = connections[room];
					foreach (var previousRoom in lastRow) {
						var previousRoomConnections = connections[previousRoom];

						//Connect rooms that are < 3 distance from each other
						if (Vector2.Distance(room.Position, previousRoom.Position) < maxConnectionDistance) {
							connectedRooms.Add(previousRoom);
							previousRoomConnections.Add(room);
						}
					}
				}

				lastRow = row;
			}

			//Ensure all rooms have at least 2 connections to avoid the player getting stuck
			foreach (var room in roomsGen) {
				var roomConnections = connections[room];
				if (roomConnections.Count < 2) {
					Console.WriteLine($"Room at {room.Position} has {roomConnections.Count} connections. 2 is the minimum. Retrying level generation.");
					return false;
				}
			}

			//Shuffle rooms so connections are pruned in a non uniform manner
			int roomCount = roomsGen.Count;
            int[] shuffledList = new int[roomCount];
            for (int i = 0; i < shuffledList.Length; i++) {
                shuffledList[i] = i;
            }
			while(roomCount > 1) {
                roomCount--;
                int nextValue = (int)(rand.NextDouble() * roomCount);
                int listValue = shuffledList[nextValue];
                shuffledList[nextValue] = shuffledList[roomCount];
                shuffledList[roomCount] = listValue;
            }

            //Prune connections if there's more than maxConnections or by random chance
            foreach (var room in roomsGen) {
				var roomConnections = connections[room];
				if (roomConnections.Count > maxConnections || rand.NextDouble() <= pruneChance)
				{
					while (roomConnections.Count > 2) {
						//Look for a connection that removing won't cause another room to have < 2 connections
						Room roomToRemove = null;
						foreach (var connectedRoom in roomConnections) {
							var roomConnections2 = connections[connectedRoom];
							if (roomConnections2.Count > 2) {
								roomToRemove = connectedRoom; //Remove the room outside of the loop to not invalid the enumerator
								roomConnections2.Remove(room);
								break;
							}
						}

						//Remove connection
						if (roomToRemove != null)
							roomConnections.Remove(roomToRemove);
						else
							break;
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
					var connectedRooms = connections[room];
					checkedRooms.Add(room);

					//Check if we've reached the end room
					if (room == endRoom)
						success = true;

					//Push connections onto queue if they haven't already been checked
					foreach (var connection in connectedRooms)
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
				//Done in two steps since you should enumerate a list (roomsGen) while removing items from it
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

			//Check again that all rooms have >= 2 && <= maxConnections
			foreach (var room in roomsGen) {
				var roomConnections = connections[room];
				if (roomConnections.Count < 2) {
					Console.WriteLine($"Level generation error! Room {room.Id} has < 2 connections.");
					return false;
				}
				if (roomConnections.Count > maxConnections) {
					Console.WriteLine($"Level generation error! Room {room.Id} has > 5 connections.");
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

			//Map view toggle
			if (Input.IsKeyPressed(Keys.M))
				IsViewingMap = !IsViewingMap;

			//Regenerate level
			if (Input.IsKeyPressed(Keys.G)) {
				TryGenerateLevel(1000);
			}

			//Player map movement
			if (Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift)) {
				Room nextRoom = null;
				if (Input.IsKeyPressed(Keys.D1) && CurrentRoom.ConnectedRooms.Length >= 1)
					nextRoom = CurrentRoom.ConnectedRooms[0];
				if (Input.IsKeyPressed(Keys.D2) && CurrentRoom.ConnectedRooms.Length >= 2)
					nextRoom = CurrentRoom.ConnectedRooms[1];
				if (Input.IsKeyPressed(Keys.D3) && CurrentRoom.ConnectedRooms.Length >= 3)
					nextRoom = CurrentRoom.ConnectedRooms[2];
				if (Input.IsKeyPressed(Keys.D4) && CurrentRoom.ConnectedRooms.Length >= 4)
					nextRoom = CurrentRoom.ConnectedRooms[3];
				if (Input.IsKeyPressed(Keys.D5) && CurrentRoom.ConnectedRooms.Length >= 5)
					nextRoom = CurrentRoom.ConnectedRooms[4];
				if (nextRoom != null) {
					if (nextRoom == PreviousRoom) {
						Console.WriteLine($"Can't move from room {CurrentRoom.Id} to room {nextRoom.Id} since you'd be moving backwards.");
					} else {
						PreviousRoom = CurrentRoom;
						CurrentRoom = nextRoom;
                        Score += 100;
						Renderer.EventQueue.Enqueue("LevelRegenerated"); //Signal to renderer to regenerate map scene
					}
				}
			}

			//Print out score and generate a new level if player completes this one
			if (CurrentRoom == EndRoom) {
				Console.WriteLine($"\n\n*****Level completed with a score of {Score}!*****\n");
				Score = 0;
                CurrentLevel++;
                TryGenerateLevel(10000);
            }

            CheckIfPlayerEnteredDoorway();
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
                    Renderer.EventQueue.Enqueue("LevelRegenerated"); //Signal to renderer to regenerate map scene
                    break;
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
        public int Rows;
		/// <summary>Minimum number of rooms per row</summary>
        public int MinRoomsPerRow;
		/// <summary>Maximum number of rooms per row</summary>
        public int MaxRoomsPerRow;
		/// <summary>Maximum number of connections a room can have. Any beyond this number are culled. All rooms must have at least 2 connections.</summary>
        public int MaxConnections;
		/// <summary>The chance of pruning connections for rooms when they have < MaxConnections.</summary>
        public float PruneChance;
		/// <summary>Rooms must be within this distance from each other to be connected.</summary>
        public float MaxConnectionDistance;
    }
}