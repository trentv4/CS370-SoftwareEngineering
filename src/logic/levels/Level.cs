using System;
using Project.Items;
using OpenTK.Mathematics;

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

		public Room(float X, float Y) {
			this.Position = new Vector3((float)X, 0.0f, (float)Y);
		}

		public double DistanceToRoom(Room otherRoom) {
			double yDist = Position.Y - otherRoom.Position.Y;
			double xDist = Position.X - otherRoom.Position.X;
			return Math.Sqrt((yDist * yDist) + (xDist * xDist));
		}
	}
}