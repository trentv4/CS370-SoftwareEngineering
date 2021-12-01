using OpenTK.Mathematics;
using System;
using Project.Levels;

namespace Project {
	///<summary>All state of the player character.</summary>
	public class Player : ICloneable {
		public Inventory Inventory;
		public int Health = 10;
		public int MaxHealth = 10;
		public int Armor = 0;
		public int CarryWeight = 10;
		public Vector2 Position;
		public Vector2 Velocity;
		public float MovementSpeed = DefaultMovementSpeed;
		public readonly float MaxSpeed = 6.5f;
		/// <summary> Amount of time in seconds that the player can't take damage again after hitting the spikes. </summary>
		public static readonly double DamageSafeTime = 0.65;
		private DateTime _lastDamageTime = DateTime.UnixEpoch;
		//Store defaults so special rooms can reset values after changing them. E.g. Icy rooms change movement speed
		public static readonly float DefaultMovementSpeed = 6.5f;

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
			copy.Armor = Armor;
			copy.CarryWeight = CarryWeight;
			copy.MovementSpeed = MovementSpeed;
			return copy;
		}

		public void Update(double deltaTime, Room room) {
			//Enforce max speed
			if (Velocity.Length > MaxSpeed)
				 	Velocity *= (MaxSpeed / Velocity.Length);

			//Update position
			Position += Velocity * (float)deltaTime;

			//Apply floor friction
			Velocity -= room.FloorFriction * Velocity;
		}

		/// <summary>
		/// Lower the players health. After taking damage the player can't take damage again for DamageSafeTime seconds
		/// That way if they collide with something like a floor spike for 0.5s they don't take damage 30+ times (each frame).
		/// Returns true if the player is damaged.
		/// </summary>
		/// <param name="amount">How much to lower health by.</param>
		/// <param name="overrideInvulnerability">If true ignore invulnerability and the damage timer.</param>
		public bool TryDamage(int amount, bool overrideInvulnerability = false) {
			TimeSpan timeSinceDamage = DateTime.Now.Subtract(_lastDamageTime);
			//Damage the player if it hasn't happened too recently
			if (timeSinceDamage >= TimeSpan.FromSeconds(DamageSafeTime) || overrideInvulnerability) {
				Health -= amount;
				_lastDamageTime = DateTime.Now;
				return true;
			}

			return false;
		}
	}
}