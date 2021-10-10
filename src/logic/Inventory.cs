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

		public void PrintInventoryControls() {
			//Print inventory controls
			Console.WriteLine("\n\nInventory controls:");
			Console.WriteLine("\tI: Print inventory");
			Console.WriteLine("\t0-9: Press the number next to an inventory item to see a description and get more options.");
			Console.WriteLine("\tU: After selecting an inventory item if it is usable you can press U to use it.");
			Console.WriteLine("\tR: Add a random set of items to the inventory for dev purposes");
			Console.WriteLine("\tP: Print player stats");
		}

		public void PrintInventoryState() {
			//Print inventory state
			Console.WriteLine("\n\n*****Inventory*****");
			Console.WriteLine($"Total weight: {Weight}/{Owner.CarryWeight}");
			Console.WriteLine("Items:");

			uint index = 0;
			foreach (var item in Items) {
				string itemUses = item.Definition.IsKey ? "key " : "";
				itemUses += item.Definition.Consumeable ? "consumeable" : "";
				Console.WriteLine($"{index}: {item.Definition.Name}");
				Console.WriteLine($"\tUses: {itemUses}");
				Console.WriteLine($"\tWeight: {item.Definition.Weight}");
				index++;
			}
		}

		public uint AddRandomItems(uint numToAdd) {
			//Attempt to 5 randomly selected items to the inventory
			uint numItemsAdded = 0;
			for (uint i = 0; i < numToAdd; i++) {
				//Get random item definition
				var rand = new Random();
				var def = ItemManager.Definitions[rand.Next() % ItemManager.Definitions.Count];

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