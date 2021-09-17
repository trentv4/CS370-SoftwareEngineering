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

		public Room StartRoom = null;
		public Room EndRoom = null;

		public bool IsViewingMap = false;
		public Level() {
			//Generate the level. Will attempt up to 10 times if it fails. Failure is rare at the moment
			for(int i = 0; i < 10; i++)
				if (GenerateNewLevel())
					break;

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
			int numRows = 8; //Num rows excluding start and end room
			int minRoomsPerRow = 2;
			int maxRoomsPerRow = 4;
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
			for(int i = 1; i < numRows + 1; i++) {
				var row = new List<Room>();
				int numRooms = rand.Next(minRoomsPerRow, maxRoomsPerRow + 1);
				float roomDistY = 6.0f / (float)numRooms;
				float roomY = (float)(rand.NextDouble() / 2.0); //Random initial Y pos

				for(int j = 0; j < numRooms; j++) {
					var room = new Room(rowX, roomY);
					row.Add(room);
					roomsGen.Add(room);
					connections[room] = new List<Room>();
					roomY += roomDistY;
				}

				//Adjust next row x pos with some random variation
				rowX += 1.0f + ((float)(rand.NextDouble() / 4.0) - 0.125f);
				rows.Add(row);
			}

			//Add end room and its row
			var endRoom = new Room(numRows + 2, centerY);
			roomsGen.Add(endRoom);
			connections[endRoom] = new List<Room>();
			var endRow = new List<Room>();
			endRow.Add(endRoom);
			rows.Add(endRow);

			//Add random items to each room
			foreach (var room in roomsGen) {
				int numItemsToAdd = rand.Next(0, 5);
				for(int i = 0; i < numItemsToAdd; i++) {
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
				if(lastRow == null) {
					lastRow = row;
					continue;
				}

				//Connect each room in this row to all rooms in the last row
				foreach(var room in row) {
					var connectedRooms = connections[room];
					foreach(var previousRoom in lastRow) {
						var previousRoomConnections = connections[previousRoom];

						//Connect rooms
						connectedRooms.Add(previousRoom);
						previousRoomConnections.Add(room);
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

			//Prune connections from rooms with >= 3 connections
			foreach (var room in roomsGen) {
				var roomConnections = connections[room];
				if(roomConnections.Count == 2)
					continue;

				double chance = rand.NextDouble();
				if(chance < 0.5) //50% chance of pruning connections
					continue;

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
				if(roomToRemove != null)
					roomConnections.Remove(roomToRemove);
			}

			//Do floodfill from initial room to see if final room can be reached
			{
				var checkedRooms = new List<Room>();
				var roomQueue = new Queue<Room>();
				roomQueue.Enqueue(startRoom);
				bool success = false;
				while(roomQueue.Count > 0) {
					//Get next room from queue
					var room = roomQueue.Dequeue();
					var connectedRooms = connections[room];
					checkedRooms.Add(room);

					//Check if we've reached the end room
					if(room == endRoom)
						success = true;

					//Push connections onto queue if they haven't already been checked
					foreach (var connection in connectedRooms)
						if(!checkedRooms.Contains(connection))
							roomQueue.Enqueue(connection);
				}

				//Fail level generation if start and end rooms aren't connected
				if (!success) {
					Console.WriteLine("Level generation error! No valid path from start room to end room.");
					return false;
				}

				//Prune rooms that aren't connected to the start room. There's no way to reach them
				foreach (var room in roomsGen)
					if (!checkedRooms.Contains(room)) {
						roomsGen.Remove(room); //Remove room

						//Iterate all other rooms and remove their connections to this one if present
						foreach (var room2 in roomsGen) {
							var room2Connections = connections[room2];
							if(room2Connections.Contains(room))
								room2Connections.Remove(room);
						}
					}
			}

			//Check again that all rooms have >= 2 connections
			foreach (var room in roomsGen) {
				var roomConnections = connections[room];
				if(roomConnections.Count < 2) {
					Console.WriteLine($"Level generation error! Room {room.Id} has < 2 connections.");
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
				GenerateNewLevel();
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