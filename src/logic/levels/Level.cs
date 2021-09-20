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
		public Level() {
			while (!GenerateNewLevel()) { }

			Player.Inventory.PrintInventoryControls();
		}

		///<summary>Generates new level. Can be called multiple times to regenerate the level. 
		///			Returns true if it succeeds and false if it fails.
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
			int numRows = 5; //Num rows excluding start and end room
			int minRoomsPerRow = 2;
			int maxRoomsPerRow = 5;
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
						float connectionMaximumThreshold = 3f;
						if (Vector3.Distance(room.Position, previousRoom.Position) < connectionMaximumThreshold) {
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

			//Prune connections from some rooms with > 2 connections
			foreach (var room in roomsGen) {
				var roomConnections = connections[room];
				//If room has < 5 connections there's a chance of removing connections
				//If room has >= 5 connections then some connections are always removed to avoid an overly connected map 
				double chance = rand.NextDouble();
				const double pruneChance = 0.6; //Chance that connections will be pruned
				if (chance > pruneChance && roomConnections.Count < 5)
					continue;

				while(roomConnections.Count > 2) {
					//Prune a connection if that's possible without giving another room < 2 connections
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

			//Do floodfill from initial room to see if final room can be reached
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

			//Check again that all rooms have >= 2 && <= 5 connections
			foreach (var room in roomsGen) {
				var roomConnections = connections[room];
				if (roomConnections.Count < 2 || roomConnections.Count > 5) {
					Console.WriteLine($"Level generation error! Room {room.Id} has < 2 connections.");
					return false;
				}
				if (roomConnections.Count > 5) { //Note: This is a temporary connection limit until door based room movement is added
					Console.WriteLine($"Level generation error! Room {room.Id} has > 5 connections.");
					return false;
				}
			}

			//Set final ConnectedRooms array for each room
			foreach (var room in roomsGen) {
				var connectedRooms = connections[room];
				room.ConnectedRooms = connectedRooms.ToArray();
			}

			//Set final room array
			Rooms = roomsGen.ToArray();
			StartRoom = startRoom;
			EndRoom = endRoom;

			//Spawn the player
			Player = new Player(Rooms[0].Position);
            CurrentRoom = startRoom;
            PreviousRoom = startRoom;
            Console.WriteLine($"Generated new level with {Rooms.Length} rooms.");
			return true;
		}

		public void Update() {
			Player.Update();

			//Map view toggle
			if (Input.IsKeyPressed(Keys.M))
				IsViewingMap = !IsViewingMap;

			//Regenerate level
			if (Input.IsKeyPressed(Keys.G))
				while (!GenerateNewLevel()) { }

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
                    }
                }
            }

			//Print out score and generate a new level if player completes this one
			if(CurrentRoom == EndRoom) {
                Console.WriteLine($"Level completed with a score of {Score}!");
                Score = 0;
				while(!GenerateNewLevel()) {}
            }
		}
	}

	public class Room {
		public Vector3 Position;
		public Room[] ConnectedRooms;
		public List<Item> Items = new List<Item>();
		private static int _currentId = 0;
		//Unique ID used for use as dictionary key
		private readonly int _id = 0;
		public int Id => _id;

		public Room(float X, float Y) {
			this.Position = new Vector3(X, 0.0f, Y);
			_id = _currentId++;
		}

		public double DistanceToRoom(Room otherRoom) {
			return (otherRoom.Position - Position).Length;
		}
	}
}