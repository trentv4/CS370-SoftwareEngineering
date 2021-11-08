using System;
using System.Collections.Generic;
using Project.Items;
using OpenTK.Mathematics;
using Project.Util;
using System.Linq;

namespace Project.Levels {
	/// <summary> Procedurally generates levels </summary>
	public static class LevelGenerator {
		public static int Seed { get; private set; }
		private static readonly List<LevelGenConfig> GenerationConfigs = new List<LevelGenConfig>() {
			new LevelGenConfig() {NumLevels = 1, NumPrimaryPaths = 2, MinRoomsPerPath = 2, MaxRoomsPerPath = 3, PruneChance = 0.15f, SecondaryPathChance = 0.2f },
			new LevelGenConfig() {NumLevels = 1, NumPrimaryPaths = 3, MinRoomsPerPath = 3, MaxRoomsPerPath = 4, PruneChance = 0.25f, SecondaryPathChance = 0.4f },
			new LevelGenConfig() {NumLevels = 1, NumPrimaryPaths = 4, MinRoomsPerPath = 3, MaxRoomsPerPath = 7, PruneChance = 0.5f, SecondaryPathChance = 0.5f },
		};

		/// <summary> Generates and returns a new level instance. Will retry numGenerationAttempts times if generation errors occur.
		///			  If it fails every time an exception will be thrown.
		/// </summary>
		public static Level TryGenerateLevel(int levelDepth = 0, int numGenerationAttempts = 1000) {
            for (int i = 0; i < numGenerationAttempts; i++)
				if (GenerateLevel(levelDepth, out Level level))
					return level;

			throw new Exception($"ERROR! Tried to regenerate level {numGenerationAttempts} times and failed every time! Seed: {Seed}");
        }

		/// <summary> Generates a new level instance </summary>
		private static bool GenerateLevel(int levelDepth, out Level level) {
			level = null;

			//Random number generator used to vary level gen
			Seed = (int)System.DateTime.Now.Ticks; //Seed with time so generation is always different
			Random rand = new Random(Seed);

            //Get config depending on depth (number of levels completed)
            LevelGenConfig genConfig = null;
            int depth = 0;
            foreach (var config in GenerationConfigs) {
                genConfig = config;
                if(levelDepth >= depth && levelDepth < depth + config.NumLevels)
                    break;

                depth += config.NumLevels;
            }
            if(genConfig == null) //Use final config if none is found for current depth
                genConfig = GenerationConfigs[GenerationConfigs.Count - 1];

			int minRoomsPerPath = genConfig.MinRoomsPerPath;
			int maxRoomsPerPath = genConfig.MaxRoomsPerPath;
            int numPrimaryPaths = genConfig.NumPrimaryPaths;
            double pruneChance = genConfig.PruneChance;
            float secondaryPathChance = genConfig.SecondaryPathChance;

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
                    	float angle = rand.NextFloat(-1.0f, 1.0f) * angleMaxRadians;
                    	roomX += xDelta * MathF.Cos(angle);
                    	roomY += xDelta * MathF.Sin(angle);
					}

                    //Create next room
                    lastRoom = curRoom;
                    curRoom = MakeRoom(roomX, roomY, rand);
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
                    if (rand.NextFloat() <= secondaryPathChance) {
                     	Room primary = path[i];

                        //Look for a nearby primary path to connect with this one
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

						if (nextRoom != null) { //Connect to another primary path
                            connections[primary].Add(nextRoom);
                            connections[nextRoom].Add(primary);
                        } else { //Branch off this primary and loop back into it elsewhere.
							//Todo: Rewrite this so it forms more complex branches. Currently just forms a loop off the current primary.
							//Determine the end room of the loop
							Room loopEnd = null;
							if(i == (path.Count - 1))
								loopEnd = path[i - 1]; //Go backwards if we're at the end of the primary
							else
								loopEnd = path[i + 1]; //Otherwise, go forward

							//Calculate angle the loop takes off the primary path
                        	float minSecondaryAngle = 5.0f;
                        	float angle = rand.NextFloat(-1.0f, 1.0f) * angleMaxRadians;
                        	angle = Math.Sign(angle) * Math.Max(minSecondaryAngle, Math.Abs(angle));
							
							//Calculate the position of the loop center room
							float xDelta = 10.0f / (path.Count + 1); //Distance between primary path rooms
                        	float roomX = primary.Position.X;
                        	float roomY = primary.Position.Y;
                        	roomX += xDelta * MathF.Cos(angle) * 0.5f;
                        	roomY += xDelta * MathF.Sin(angle) * 0.5f;

							//Create a room at the center of the loop
                        	var room = MakeRoom(roomX, roomY, rand);
                        	roomsGen.Add(room);
                        	connections[room] = new List<Room>();

							//Connect loop start and center rooms
                        	connections[room].Add(primary);
                        	connections[primary].Add(room);
							
							//Connect loop center and end rooms
                        	connections[room].Add(loopEnd);
                        	connections[loopEnd].Add(room);
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

                    //Remove connection if there will still be a valid path to the end room
                    if (room2 != null && CanRoomBeSafelyRemoved(room, room2, startRoom, endRoom, roomsGen, connections)) {
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
					float x = rand.NextFloat(-2.5f, 2.5f);
					float y = rand.NextFloat(-2.5f, 2.5f);
					item.Position = new Vector2(x, y);
				}
			}

			//Instantiate level as generation can't fail past this point
			level = new Level();

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
			level.KeyDefinitions = keyDefs.ToArray();

			//Add keys to rooms somewhere in the map
			foreach(ItemDefinition keyDef in keyDefs) {
				while(true) {
					//Pick a random room
					Room room = roomsGen[rand.Next(0, roomsGen.Count)];

					//Use room if it's not the start/end room and doesn't already have a key
					if (room != startRoom && room != endRoom && !room.Items.Contains(item => item.Definition.IsKey)) {
						//Create key and add it to the room in a random position
						var key = new Item(keyDef);
						room.Items.Add(key);
						float x = rand.NextFloat(-2.5f, 2.5f);
						float y = rand.NextFloat(-2.5f, 2.5f);
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
			level.Rooms = roomsGen.ToArray();
			level.StartRoom = startRoom;
			level.EndRoom = endRoom;
			level.CurrentRoom = startRoom;
			level.PreviousRoom = startRoom;
			level.Depth = levelDepth;

			level.CurrentRoom.Visited = Room.VisitedState.Visited;
			foreach (Room c in level.CurrentRoom.ConnectedRooms) {
				c.Visited = Room.VisitedState.Seen;
			}

			Console.WriteLine($"Generated new level with {level.Rooms.Length} rooms.");
			return true;
		}

		/// <summary>
		/// Returns true if there will still be a connection between the start and end room if the connection between target0 and target1 is removed.
		/// </summary>
		private static bool CanRoomBeSafelyRemoved(Room target0, Room target1, Room startRoom, Room endRoom, IReadOnlyList<Room> rooms, IReadOnlyDictionary<Room, List<Room>> connections) {
            //Do floodfill from the start room
			var checkedRooms = new List<Room>();
            var roomQueue = new Queue<Room>();
            roomQueue.Enqueue(startRoom);
            bool reachedEndRoom = false;
        	while (roomQueue.Count > 0) {
                //Get next room from queue
                var room = roomQueue.Dequeue();
                checkedRooms.Add(room);

                //Check if we've reached the end room
                if (room == endRoom)
                    reachedEndRoom = true;

                //Push connections onto queue if they haven't already been checked
                foreach (var connection in connections[room])
                    if (!checkedRooms.Contains(connection))
						if ((room == target0 && connection == target1) || (room == target1 && connection == target0)) //Pretend the connection doesn't exist
                        	roomQueue.Enqueue(connection);
            }

            return reachedEndRoom;
		}

		/// <summary> Creates a new Room instance with a chance of creating a room with obstacles </summary>
		private static Room MakeRoom(float x, float y, Random rand, bool allowObstacles = true) {
			const float obstacleChance = 0.6f;
			if (allowObstacles && rand.NextFloat() <= obstacleChance) { //Random chance to create obstacle a room
				//Randomly pick room type
				Room room = null;
				switch(rand.Next(2)) {
					case 0:
						WindyRoom windyRoom = new WindyRoom(x, y);
						windyRoom.WindSpeed = rand.NextFloat(5.0f, 6.2f);
						windyRoom.WindDirection = rand.NextVec2();
						windyRoom.DirectionChangePeriod = rand.NextFloat(4.0f, 12.0f); //Change wind direction periodically
						room = windyRoom;
						break;
					case 1:
						room = new IcyRoom(x, y);	
						break;
				}

				//Add some spikes to the room for added danger
				int numSpikes = rand.Next(3, 10);
				for (int i = 0; i < numSpikes; i++) {
					Vector2 pos = rand.NextVec2(-6.0f, 6.0f, -6.0f, 6.0f);
					var obj = new FloorSpike(pos, "spikes.png", 0.8f, 1);
					room.Objects.Add(obj);
				}

				return room;
			}
			else
				return new Room(x, y); //Otherwise create a normal room
		}

		/// <summary> Level generator configuration </summary>
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
}