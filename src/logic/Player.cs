using System;
using Project.Items;
using Project.Render;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Project.Util;
using OpenTK.Mathematics;

namespace Project {
	///<summary>All state of the player character.</summary>
	public class Player {
		public Inventory Inventory;
		public int Health = 10;
		public int MaxHealth = 10;
		public int Mana = 10;
		public int MaxMana = 10;
		public int Armor = 0;
		public int CarryWeight = 10;
		public Vector3 Position;

		public Player(Vector3 initialPosition) {
			Position = initialPosition;
			Inventory = new Inventory(this);
		}

		public void Update() {
			Inventory.UpdateUI();
			UpdateInput();
		}

		private void UpdateInput() {
			//Player movement
			int ws = Convert.ToInt32(Input.IsKeyDown(Keys.W)) - Convert.ToInt32(Input.IsKeyDown(Keys.S));
			int ad = Convert.ToInt32(Input.IsKeyDown(Keys.A)) - Convert.ToInt32(Input.IsKeyDown(Keys.D));
			int qe = Convert.ToInt32(Input.IsKeyDown(Keys.Q)) - Convert.ToInt32(Input.IsKeyDown(Keys.E));
			int sl = Convert.ToInt32(Input.IsKeyDown(Keys.Space)) - Convert.ToInt32(Input.IsKeyDown(Keys.LeftShift));
			float speed = 0.1f;
			Position.X += ad * speed;
			Position.Y += ws * speed;

			//Print player stats
			if (Input.IsKeyPressed(Keys.P)) {
				Console.WriteLine("\n\n*****Player stats*****");
				Console.WriteLine($"Health: {Health}/{MaxHealth}");
				Console.WriteLine($"Mana: {Mana}/{MaxMana}");
				Console.WriteLine($"Armor: {Armor}");
				Console.WriteLine($"Carry weight: {CarryWeight}");
			}
		}
	}
}