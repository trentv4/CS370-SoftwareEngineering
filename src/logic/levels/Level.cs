using System;
using Project.Items;

namespace Project.Levels {
	public class Level {
		private readonly Random Random = new System.Random();
		public Player Player;
		public Room[] Rooms;
		private Item[] items;

		public Level(Room[] rooms) {
			this.Rooms = rooms;
			SpawnPlayer();
			Console.WriteLine("Level created.");
			Console.WriteLine("Player is at coordinate " + Player.xPos + ", " + Player.yPos + ".");
			Player.Inventory.PrintInventoryControls();
		}

		public void SpawnPlayer() {
			Player = new Player();
			Player.xPos = Rooms[0].X;
			Player.yPos = Rooms[0].Y;
		}

		public void Update() {
			Player.Update();
		}
	}

	public class Room {
		public readonly int X, Y;
		public Room[] ConnectedRooms;

		public Room(int X, int Y) {
			this.X = X;
			this.Y = Y;
		}

		public double DistanceToRoom(Room otherRoom) {
			double yDist = Y - otherRoom.Y;
			double xDist = X - otherRoom.X;
			return Math.Sqrt((yDist * yDist) + (xDist * xDist));
		}
	}
}