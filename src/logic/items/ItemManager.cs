using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Project.Items {
    /// <summary>Loads <see cref="ItemDefinition"/>s from xml files. Automatically reloads them if the file changes.</summary>
    public static class ItemManager {
        public static string DefinitionsFolderPath = @".\assets\definitions\";
        public static List<ItemDefinition> Definitions = new List<ItemDefinition>();

        public static void LoadDefinitions() {

            //Load definitions from xml files in assets/definitions/
            if(Directory.Exists(DefinitionsFolderPath)) {
                foreach(var file in Directory.EnumerateFiles(DefinitionsFolderPath, "*.xml"))
                    LoadDefinitionsFromFile(file);
            }
        }

        public static void CheckForReload() {
            
        }

        private static void LoadDefinitionsFromFile(string path) {
            var doc = new XmlDocument();
            doc.Load(path);

            var root = doc.SelectSingleNode("root");
            if(root == null)
                return;

            var items = root.SelectNodes("Item");
            foreach(XmlElement item in items) {
                var definition = new ItemDefinition();

                //General stats
                var name = item.SelectSingleNode("Name");
                var weight = item.SelectSingleNode("Weight");
                var damage = item.SelectSingleNode("Damage");
                var armor = item.SelectSingleNode("Armor");

                if (name != null)
                    definition.Name = name.InnerText;
                if (weight != null)
                    definition.Weight = int.Parse(weight.InnerText);
                if (damage != null)
                    definition.Weight = int.Parse(damage.InnerText);
                if (armor != null)
                    definition.Weight = int.Parse(armor.InnerText);

                //Use effects
                var uses = item.SelectSingleNode("Uses");
                if (uses != null) {
                    var consume = uses.SelectSingleNode("Consume");
                    var key = uses.SelectSingleNode("Key");
                    
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
                        if(keyType != null)
                            definition.KeyType = keyType.InnerText;
                    }
                }

                Definitions.Add(definition);
            }
        }
    }
}
