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
		public Vector2 Position;

        public Player(Vector2 initialPosition) {
			Position = initialPosition;
			Inventory = new Inventory(this);
		}

		public void Update() {
			
		}
	}
}