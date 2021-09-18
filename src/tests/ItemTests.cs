using System;
using NUnit.Framework;
using Project.Items;
using OpenTK.Mathematics;

namespace Project.Tests {
    /// <summary>Test item loading and game logic</summary>
    [TestFixture]
    public class ItemTests {
        string ArmorPotionItemDefinition = @"
        <root>
            <Item>
              <Name>Armor potion</Name>
              <Weight>1</Weight>
              <Damage>0</Damage>
              <Armor>0</Armor>
              <TextureName>plane.png</TextureName>
              <Uses NumUses=""1"">
                <Consume>
                  <Armor>2</Armor>
                </Consume>
              </Uses>
            </Item>
        </root>";

        [Test]
        public void TestItemDefinitionLoading() {
            Assert.IsTrue(ItemManager.LoadDefinitionsFromString(ArmorPotionItemDefinition), "Failed to load valid item definition xml.");
        }

        [Test]
        public void TestItemUsage() {
            //Clear existing definition and load armor potion 
            ItemManager.Definitions.Clear();
            Assert.IsTrue(ItemManager.LoadDefinitionsFromString(ArmorPotionItemDefinition), "Failed to load valid item definition xml.");

            //Create player and add armor potion to their inventory
            var player = new Player(new Vector3(0.0f, 0.0f, 0.0f));
            var inventory = new Inventory(player);
            inventory.AddItem(ItemManager.Definitions[0]);

            //Use armor potion and check that it changed play stats correctly
            int initialArmor = player.Armor;
            int expectedFinalArmor = initialArmor + 2;
            inventory.Items[0].Consume(player);
            Assert.AreEqual(player.Armor, expectedFinalArmor);
        }
    }
}