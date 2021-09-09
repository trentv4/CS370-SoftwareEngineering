using System;
using System.Collections.Generic;
using Project.Items;
using System.Linq;

namespace Project {
    ///<summary>Manages item instances carried by an entity</summary>
    public class Inventory {
        public List<Item> Items = new List<Item>();
        public int Weight => Items.Sum(item => item.Definition.Weight);
        public int MaxWeight;

        public Inventory(int maxWeight) {
            MaxWeight = maxWeight;
        }

        ///<summary>Add item to inventory if it doesn't go over the max weight</summary>
        public bool AddItem(Item item) {
            if(Weight + item.Definition.Weight > MaxWeight)
                return false; //Failed to add item

            Items.Add(item);
            return true; //Item successfully added
        }

        ///<summary>Add item to inventory if it doesn't go over the max weight</summary>
        public bool AddItem(ItemDefinition definition) {
            var item = new Item(definition);
            if(Weight + item.Definition.Weight > MaxWeight)
                return false; //Failed to add item

            Items.Add(item);
            return true; //Item successfully added
        }
    }
}