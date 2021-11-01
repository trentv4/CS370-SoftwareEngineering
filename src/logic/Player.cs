using OpenTK.Mathematics;
using System;

namespace Project {
	///<summary>All state of the player character.</summary>
	public class Player : ICloneable {
		public Inventory Inventory;
		public int Health = 10;
		public int MaxHealth = 10;
		public int Mana = 10;
		public int MaxMana = 10;
		public int Armor = 0;
		public int CarryWeight = 10;
		public Vector2 Position;
		public Vector2 Velocity;
		public readonly float MovementSpeed = 6.5f;
		public readonly float MaxSpeed = 6.5f;

		public Player(Vector2 initialPosition) {
			Position = initialPosition;
			Inventory = new Inventory(this);
		}

		/// <summary>Make a deep copy</summary>
		public object Clone() {
			Player copy = new Player(Position);
			copy.Inventory = (Inventory)Inventory.Clone();
			copy.Inventory.Owner = copy;
			copy.Health = Health;
			copy.MaxHealth = MaxHealth;
			copy.Mana = Mana;
			copy.MaxMana = MaxMana;
			copy.Armor = Armor;
			copy.CarryWeight = CarryWeight;
			return copy;
		}

		public void Update(double deltaTime) {
			//Enforce max speed
			if (Velocity.Length > MaxSpeed)
				 	Velocity *= (MaxSpeed / Velocity.Length);

			//Update position
			Position += Velocity * (float)deltaTime;

			//Apply floor friction
			Velocity *= 0.85f;
		}
	}
}