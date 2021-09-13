using System;
using Project.Util;

namespace Project.Items {
    ///<summary>An instance of an item in the game world. Uses ItemDefinition to determine its behavior.</summary>
    public class Item {
        public ItemDefinition Definition;
        public uint UsesRemaining;
        public int xPos;
        public int yPos;

        public Item(ItemDefinition definition) {
            Definition = definition;
            UsesRemaining = definition.NumUses;
        }

        ///<summary>Consome the item if it's consumeable and apply the OnConsume effects to the provided target.</summary>
        public void Consume(Player target)
        {
            if(UsesRemaining == 0)
                return;

            if(Definition.Consumeable) {
                target.Health += Definition.OnConsume.Health;
                target.Health = MathUtil.MinMax(target.Health, 0, target.MaxHealth);

                target.MaxHealth += Definition.OnConsume.MaxHealth;
                target.MaxHealth += MathUtil.MinMax(target.MaxHealth, 0, 100);

                target.Mana += Definition.OnConsume.Mana;
                target.Mana = MathUtil.MinMax(target.Mana, 0, target.MaxMana);

                target.MaxMana += Definition.OnConsume.MaxMana;
                target.MaxMana += MathUtil.MinMax(target.MaxMana, 0, 100);

                target.CarryWeight += Definition.OnConsume.CarryWeight;
                target.CarryWeight = MathUtil.MinMax(target.CarryWeight, 0, 100);

                target.Armor += Definition.OnConsume.Armor;
                target.Armor = MathUtil.MinMax(target.Armor, 0, 100);

                UsesRemaining--;
            }
        }
    }
}