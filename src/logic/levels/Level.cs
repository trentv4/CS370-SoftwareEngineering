using System;
using System.Collections.Generic;
using Project.Items;
using OpenTK.Mathematics;
using Project.Render;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Project.Levels {
	public class Level {
		private readonly Random Random = new System.Random();
		public Player Player;
		public Room[] Rooms;

		public bool IsViewingMap = false;

		private uint framesSinceKeyPressed = 0;

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

			var KeyboardState = Renderer.INSTANCE.KeyboardState;
			if(KeyboardState.IsKeyDown(Keys.M) && framesSinceKeyPressed > 5) {
				framesSinceKeyPressed = 0;
				IsViewingMap = !IsViewingMap;
			}
			else {
				framesSinceKeyPressed++;
			}
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
			return (otherRoom.Position - Position).Length;
		}
	}
}