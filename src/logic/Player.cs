using System;
using Project.Items;

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

        public Player()
        {
            Inventory = new Inventory(CarryWeight);
        }
    }
}