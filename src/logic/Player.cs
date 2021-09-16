using System;
using Project.Items;
using Project.Render;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Project.Util;

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
		public float xPos = 0.0f;
		public float yPos = 0.0f;

		public Player() {
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
			xPos += ad * speed;
			yPos += ws * speed;

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