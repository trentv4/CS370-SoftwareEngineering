using System;
using Project.Util;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Project.Items {
	/// <summary> An instance of an item in the game world. Uses ItemDefinition to determine its behavior. </summary>
	public class Item : ICloneable {
        public ItemDefinition Definition;
        public uint UsesRemaining;
		public Vector2 Position;

        public Item(ItemDefinition definition) {
            Definition = definition;
            UsesRemaining = definition.NumUses;
        }

		/// <summary>Make a deep copy</summary>
		public object Clone() {
			Item copy = new Item(Definition);
			copy.UsesRemaining = UsesRemaining;
			copy.Position = Position;
			return copy;
		}

		/// <summary> Consume the item if it's consumable and call OnConsume() on the target. </summary>
		public void Consume(Player target) {
            if(UsesRemaining == 0)
                return;

            if(Definition.Consumeable) {
                target.Health += Definition.OnConsume.Health;
                target.Health = MathUtil.MinMax(target.Health, 0, target.MaxHealth);

                target.MaxHealth += Definition.OnConsume.MaxHealth;
                target.MaxHealth = MathUtil.MinMax(target.MaxHealth, 0, 100);

                target.Mana += Definition.OnConsume.Mana;
                target.Mana = MathUtil.MinMax(target.Mana, 0, target.MaxMana);

                target.MaxMana += Definition.OnConsume.MaxMana;
                target.MaxMana = MathUtil.MinMax(target.MaxMana, 0, 100);

                target.CarryWeight += Definition.OnConsume.CarryWeight;
                target.CarryWeight = MathUtil.MinMax(target.CarryWeight, 0, 100);

                target.Armor += Definition.OnConsume.Armor;
                target.Armor = MathUtil.MinMax(target.Armor, 0, 100);

                UsesRemaining--;
            }
        }
    }

	[Flags]
	public enum ItemUseFlags {
		Consume = 1,
		Key = 2,
	}

	public struct ConsumeEffects {
		public int Health;
		public int Mana;
		public int CarryWeight;
		public int Armor;
		public int MaxHealth;
		public int MaxMana;
	}

	/// <summary>Characteristics of an <see cref="Item"/>. Loaded by <see cref="ItemManager"/>.</summary>
	public class ItemDefinition {
		public static string DefinitionsFolderPath = @".\assets\definitions\";
		public static List<ItemDefinition> Definitions = new List<ItemDefinition>();

		public string Name;
		public int Weight;
		public int Damage;
		public int Armor;
		public uint NumUses;
		public ItemUseFlags Uses;
		public ConsumeEffects OnConsume;
		public string KeyType;
		public string TextureName;

		public bool Consumeable => (Uses & ItemUseFlags.Consume) == ItemUseFlags.Consume;
		public bool IsKey => (Uses & ItemUseFlags.Key) == ItemUseFlags.Key;

		public static void LoadDefinitions() {
			//Load definitions from xml files in assets/definitions/
			if (Directory.Exists(DefinitionsFolderPath)) {
				foreach (var file in Directory.EnumerateFiles(DefinitionsFolderPath, "*.xml"))
					LoadDefinitionsFromFile(file);
			}
		}

		public static bool LoadDefinitionsFromFile(string path) {
			string definitionXml = File.ReadAllText(path);
			return LoadDefinitionsFromString(definitionXml);
		}

		public static bool LoadDefinitionsFromString(string definitionXml) {
			var doc = new XmlDocument();
			doc.LoadXml(definitionXml);

			var root = doc.SelectSingleNode("root");
			if (root == null)
				return false;

			var items = root.SelectNodes("Item");
			foreach (XmlElement item in items) {
				var definition = new ItemDefinition();

				//General stats
				var name = item.SelectSingleNode("Name");
				var weight = item.SelectSingleNode("Weight");
				var damage = item.SelectSingleNode("Damage");
				var armor = item.SelectSingleNode("Armor");
				var textureName = item.SelectSingleNode("TextureName");

				if (name != null)
					definition.Name = name.InnerText;
				if (weight != null)
					definition.Weight = int.Parse(weight.InnerText);
				if (damage != null)
					definition.Damage = int.Parse(damage.InnerText);
				if (armor != null)
					definition.Armor = int.Parse(armor.InnerText);
				if (textureName != null)
					definition.TextureName = textureName.InnerText;

				//Use effects
				var uses = item.SelectSingleNode("Uses") as XmlElement;
				if (uses != null) {
					var consume = uses.SelectSingleNode("Consume");
					var key = uses.SelectSingleNode("Key");
					var numUses = uses.GetAttribute("NumUses");
					if (numUses != null && numUses != "")
						definition.NumUses = UInt32.Parse(numUses);

					if (consume != null) {
						definition.Uses |= ItemUseFlags.Consume;
						var healthChange = consume.SelectSingleNode("Health");
						var manaChange = consume.SelectSingleNode("Mana");
						var weightChange = consume.SelectSingleNode("CarryWeight");
						var armorChange = consume.SelectSingleNode("Armor");
						var maxHealthChange = consume.SelectSingleNode("MaxHealth");
						var maxManaChange = consume.SelectSingleNode("MaxMana");

						if (healthChange != null)
							definition.OnConsume.Health = int.Parse(healthChange.InnerText);
						if (manaChange != null)
							definition.OnConsume.Mana = int.Parse(manaChange.InnerText);
						if (weightChange != null)
							definition.OnConsume.CarryWeight = int.Parse(weightChange.InnerText);
						if (armorChange != null)
							definition.OnConsume.Armor = int.Parse(armorChange.InnerText);
						if (maxHealthChange != null)
							definition.OnConsume.MaxHealth = int.Parse(maxHealthChange.InnerText);
						if (maxManaChange != null)
							definition.OnConsume.MaxMana = int.Parse(maxManaChange.InnerText);
					}
					if (key != null) {
						definition.Uses |= ItemUseFlags.Key;

						var keyType = key.SelectSingleNode("Type");
						if (keyType != null)
							definition.KeyType = keyType.InnerText;
					}
				}

				Definitions.Add(definition);
			}

			return true;
		}
	}
}