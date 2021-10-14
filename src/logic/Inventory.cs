using System;
using System.Collections.Generic;
using Project.Items;
using System.Linq;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Project.Render;
using Project.Util;

namespace Project {
	///<summary>Manages item instances carried by an entity</summary>
	public class Inventory {
		///<summary>Entity that's carrying this inventory around</summary>
		public Player Owner = null;
		public List<Item> Items = new List<Item>();
		public int Weight => Items.Sum(item => item.Definition.Weight);

		///<summary>Last item selected in the inventory UI</summary>
		public Item LastItemSelected = null;

		/// <summary>Index of the currently selected item on the inventory UI</summary>
		public int Position = 0;

		public Inventory(Player owner) {
			Owner = owner;
		}

		///<summary>Add item to inventory if it doesn't go over the max weight</summary>
		public bool AddItem(Item item) {
			if (Weight + item.Definition.Weight > Owner.CarryWeight)
				return false; //Failed to add item

			Items.Add(item);
			return true; //Item successfully added
		}

		///<summary>Add item to inventory if it doesn't go over the max weight</summary>
		public bool AddItem(ItemDefinition definition) {
			var item = new Item(definition);
			if (Weight + item.Definition.Weight > Owner.CarryWeight)
				return false; //Failed to add item

			Items.Add(item);
			return true; //Item successfully added
		}

		public uint AddRandomItems(uint numToAdd) {
			//Attempt to 5 randomly selected items to the inventory
			uint numItemsAdded = 0;
			for (uint i = 0; i < numToAdd; i++) {
				//Get random item definition
				var rand = new Random();
				var def = ItemDefinition.Definitions[rand.Next() % ItemDefinition.Definitions.Count];

				//Create item and add it to the inventory
				var item = new Item(def);
				bool result = AddItem(item);
				if (result)
					numItemsAdded++;
				else
					break; //Stop adding items if one fails to be added
			}

			return numItemsAdded;
		}
	}
}