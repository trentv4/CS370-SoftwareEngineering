using System;
using System.Collections.Generic;
using Project.Items;
using OpenTK.Mathematics;

namespace Project.Levels {
	public class Level {
		private readonly Random Random = new System.Random();
		public Player Player;
		public Room[] Rooms;

		public Level(Room[] rooms) {
			this.Rooms = rooms;
			SpawnPlayer();
			Console.WriteLine("Level created.");
			Console.WriteLine("Player is at coordinate " + Player.xPos + ", " + Player.yPos + ".");
			Player.Inventory.PrintInventoryControls();
		}

		public void SpawnPlayer() {
			Player = new Player();
			Player.xPos = Rooms[0].Position.X;
			Player.yPos = Rooms[0].Position.Y;
		}

		public void Update() {
			Player.Update();
		}
	}

	public class Room {
		public Vector3 Position;
		public Room[] ConnectedRooms;

		public List<Item> Items = new List<Item>();

		public Room(float X, float Y) {
			this.Position = new Vector3(X, 0.0f, Y);
		}

		public double DistanceToRoom(Room otherRoom) {
			double yDist = Position.Y - otherRoom.Position.Y;
			double xDist = Position.X - otherRoom.Position.X;
			return Math.Sqrt((yDist * yDist) + (xDist * xDist));
		}
	}
}