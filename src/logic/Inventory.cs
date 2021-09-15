using System;
using System.Collections.Generic;
using Project.Items;
using System.Linq;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Project.Render;
namespace Project {
    ///<summary>Manages item instances carried by an entity</summary>
    public class Inventory {
        ///<summary>Entity that's carrying this inventory around</summary>
        public Player Owner = null;
        public List<Item> Items = new List<Item>();
        public int Weight => Items.Sum(item => item.Definition.Weight);
        private uint FramesSinceKeyPressed = 0;

        ///<summary>Last item selected in the inventory UI</summary>
        private Item LastItemSelected = null;

        public Inventory(Player owner) {
            Owner = owner;
        }

        ///<summary>Add item to inventory if it doesn't go over the max weight</summary>
        public bool AddItem(Item item) {
            if(Weight + item.Definition.Weight > Owner.CarryWeight)
                return false; //Failed to add item

            Items.Add(item);
            return true; //Item successfully added
        }

        ///<summary>Add item to inventory if it doesn't go over the max weight</summary>
        public bool AddItem(ItemDefinition definition) {
            var item = new Item(definition);
            if(Weight + item.Definition.Weight > Owner.CarryWeight)
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

        ///<summary>Process user input for inventory user interface
        public void UpdateUI() {
            var keyState = Renderer.INSTANCE.KeyboardState;

            int numKeyPressed = -1;
            if (keyState.IsKeyDown(Keys.D0))
                numKeyPressed = 0;
            if (keyState.IsKeyDown(Keys.D1))
                numKeyPressed = 1;
            if (keyState.IsKeyDown(Keys.D2))
                numKeyPressed = 2;
            if (keyState.IsKeyDown(Keys.D3))
                numKeyPressed = 3;
            if (keyState.IsKeyDown(Keys.D4))
                numKeyPressed = 4;
            if (keyState.IsKeyDown(Keys.D5))
                numKeyPressed = 5;
            if (keyState.IsKeyDown(Keys.D6))
                numKeyPressed = 6;
            if (keyState.IsKeyDown(Keys.D7))
                numKeyPressed = 7;
            if (keyState.IsKeyDown(Keys.D8))
                numKeyPressed = 8;
            if (keyState.IsKeyDown(Keys.D9))
                numKeyPressed = 9;
                
            //Press I to print inventory
            if(keyState.IsKeyDown(Keys.I) && FramesSinceKeyPressed > 5) {
                /*Todo: Come up with a better way of stopping repeat presses. 
                        KeyboardState.IsKeyPressed seems like it'd do it but it isn't working in this function. May be related to how the game is threaded.*/
                FramesSinceKeyPressed = 0;
                PrintInventoryState();
            }
            else if(numKeyPressed > -1 && numKeyPressed < Items.Count && FramesSinceKeyPressed > 5) {
                FramesSinceKeyPressed = 0;
                
                //Get selected item
                Item item = Items[numKeyPressed];
                LastItemSelected = item;

                //Print item info
                Console.WriteLine($"\n\n*****{item.Definition.Name}*****");
                Console.WriteLine($"Weight: {item.Definition.Weight}");
                if(item.Definition.Consumeable || item.Definition.IsKey) {
                    Console.WriteLine($"Uses: (Press U to use) : {LastItemSelected.UsesRemaining} uses remaining");
                    if (item.Definition.Consumeable)
                        Console.WriteLine("\t- Consumeable");
                    if (item.Definition.IsKey)
                        Console.WriteLine("\t- Key");
                }
            }
            else if(keyState.IsKeyDown(Keys.U) && LastItemSelected != null && FramesSinceKeyPressed > 5) {
                FramesSinceKeyPressed = 0;

                if (LastItemSelected.Definition.Consumeable) {
                    LastItemSelected.Consume(Owner);
                    Console.WriteLine($"Consumed {LastItemSelected.Definition.Name}! {LastItemSelected.UsesRemaining} uses remaining.");
                }
                else if (LastItemSelected.Definition.IsKey) {
                    //Not yet implemented
                    Console.WriteLine($"You try to use {LastItemSelected.Definition.Name}, but you have nothing to unlock!");
                }
                else {
                    Console.WriteLine($"{LastItemSelected.Definition.Name} isn't a useable item!");
                }

                if(LastItemSelected != null && LastItemSelected.UsesRemaining == 0) {
                    Console.WriteLine($"{LastItemSelected.Definition.Name} was removed from the inventory since it has 0 uses left.");
                    Items.Remove(LastItemSelected);
                }
            }
            else if (keyState.IsKeyDown(Keys.R) && FramesSinceKeyPressed > 5) { //Add random items to the inventory
                FramesSinceKeyPressed = 0;

                uint numItemsAdded = AddRandomItems(5);
                Console.WriteLine($"Added {numItemsAdded} to the inventory.");
            }
            else if (keyState.IsKeyDown(Keys.P) && FramesSinceKeyPressed > 5) { //Print player stats
                FramesSinceKeyPressed = 0;

                //Todo: This should be moved into the Player class
                Console.WriteLine("\n\n*****Player stats*****");
                Console.WriteLine($"Health: {Owner.Health}/{Owner.MaxHealth}");
                Console.WriteLine($"Mana: {Owner.Mana}/{Owner.MaxMana}");
                Console.WriteLine($"Armor: {Owner.Armor}");
                Console.WriteLine($"Carry weight: {Owner.CarryWeight}");
            }
            else {
                FramesSinceKeyPressed++;
            }
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
            for(uint i = 0; i < numToAdd; i++) {
                //Get random item definition
                var rand = new Random();
                var def = ItemManager.Definitions[rand.Next() % ItemManager.Definitions.Count];
                
                //Create item and add it to the inventory
                var item = new Item(def);
                bool result = AddItem(item);
                if(result)
                    numItemsAdded++;
                else
                    break; //Stop adding items if one fails to be added
            }

            return numItemsAdded;
        }
    }
}