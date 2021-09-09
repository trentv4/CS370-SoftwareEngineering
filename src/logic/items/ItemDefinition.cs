using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Items {
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
        public string Name;
        public int Weight;
        public int Damage;
        public int Armor;
        public uint NumUses;
        public ItemUseFlags Uses;
        public ConsumeEffects OnConsume;
        public string KeyType;

        public bool Consumeable => (Uses & ItemUseFlags.Consume) == ItemUseFlags.Consume;
        public bool IsKey => (Uses & ItemUseFlags.Key) == ItemUseFlags.Key;

        public override string ToString()
        {
            string result = "ItemDefinition:\n";
            result += $"    Name: {Name}\n";
            result += $"    Weight: {Weight}\n";
            result += $"    Damage: {Damage}\n";
            result += $"    Armor: {Armor}\n";
            result += $"    Uses: \n";
            result += $"        NumUses: {NumUses}\n";
            if((Uses & ItemUseFlags.Consume) == ItemUseFlags.Consume) {
                result += $"        Consumable:\n";
                result += $"            Health: {OnConsume.Health}\n";
                result += $"            Mana: {OnConsume.Mana}\n";
                result += $"            CarryWeight: {OnConsume.CarryWeight}\n";
                result += $"            Armor: {OnConsume.Armor}\n";
                result += $"            MaxHealth: {OnConsume.MaxHealth}\n";
                result += $"            MaxMana: {OnConsume.MaxMana}\n";
            }
            if ((Uses & ItemUseFlags.Key) == ItemUseFlags.Key) {
                result += $"        Key:\n";
                result += $"            Type: {KeyType}\n";
            }
            return result;
        }
    }
}