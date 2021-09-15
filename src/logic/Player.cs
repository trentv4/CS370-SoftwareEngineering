using System;
using Project.Items;
using Project.Render;
using OpenTK.Windowing.GraphicsLibraryFramework;
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
        public float xPos = 0.0f;
        public float yPos = 0.0f;

        public Player()
        {
            Inventory = new Inventory(this);
        }

        public void Update()
        {
            Inventory.UpdateUI();
            
            //Player movement user input
            var KeyboardState = Renderer.INSTANCE.KeyboardState;
            int ws = Convert.ToInt32(KeyboardState.IsKeyDown(Keys.W)) - Convert.ToInt32(KeyboardState.IsKeyDown(Keys.S));
			int ad = Convert.ToInt32(KeyboardState.IsKeyDown(Keys.A)) - Convert.ToInt32(KeyboardState.IsKeyDown(Keys.D));
			int qe = Convert.ToInt32(KeyboardState.IsKeyDown(Keys.Q)) - Convert.ToInt32(KeyboardState.IsKeyDown(Keys.E));
			int sl = Convert.ToInt32(KeyboardState.IsKeyDown(Keys.Space)) - Convert.ToInt32(KeyboardState.IsKeyDown(Keys.LeftShift));
            xPos += ad;
            yPos -= ws;
        }
    }
}