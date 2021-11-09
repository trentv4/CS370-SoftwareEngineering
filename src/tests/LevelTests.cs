using System;
using NUnit.Framework;
using Project.Levels;

namespace Project.Tests {
    /// <summary>Tests for level generation and logic.</summary>
    [TestFixture]
    public class LevelTests {
        [Test]
        public void TestRoomValidity() {
			//Generate a new level
			Level level = LevelGenerator.TryGenerateLevel();

			//Ensure that all rooms have at least 2 connections so the player can't get stuck
			foreach (var room in level.Rooms) {
                Assert.IsTrue(room.ConnectedRooms.Length >= 2);
            }
        }

		[Test]
        public void GenerateManyRooms() {
			//Generate many levels
			bool error = false;
			try {
				for (int i = 0; i < 1000; i++) {
					//Attempts to regenerate level if the first seed fails. If it fails too many times it throws an exception.
					Level level = LevelGenerator.TryGenerateLevel();
				}
			} catch(Exception ex) {
				Console.WriteLine(ex.Message);
				error = true;
			}

			//Level generation shouldn't completely fail
			Assert.IsFalse(error);
		}
    }
}