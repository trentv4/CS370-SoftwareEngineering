using System;
using Project.Items;

namespace Project.Levels {
    public class Level {
        private Random randomInt = new System.Random();
        private int xAxis, yAxis; // dimensions of room
        private int door_xPos, door_yPos;
     // private Enemy enemy;
        private Player player;
        private Item[] items;

        public Level(int new_xAxis, int new_yAxis) {
            xAxis = new_xAxis;
            yAxis = new_yAxis;
            setDoorPos();
            spawnPlayer();
         // spawnEnemy();
         // spawnItems();
            Console.WriteLine("Level created.");
            Console.WriteLine("Door is at coordinate " + door_xPos + ", " + door_yPos + ".");
            Console.WriteLine("Player is at coordinate " + player.xPos + ", " + player.yPos + ".");
        }

        // door across the room from player
        public void setDoorPos() {
            door_xPos = Convert.ToInt32(xAxis / 2);
            door_yPos = yAxis;
        }

        // player across the room from door
        public void spawnPlayer() {
            player = new Player();
            player.xPos = Convert.ToInt32(xAxis / 2);
            player.yPos = 0;
        }

     /* public void spawnEnemy() {
            create random enemy?
            enemy.xPos = random.Next(0, xAxis);
            enemy.yPos = random.Next(0, yAxis);
        } */

     /* public void spawnItems() {
            int numItems = random.Next(1, 5);

            for (int i = 0; i < numItems; i++)
            {
                create random items?
                items[i].xPos = random.Next(0, xAxis);
                items[i].yPos = random.Next(0, yAxis);
            }
        } */
    }
}