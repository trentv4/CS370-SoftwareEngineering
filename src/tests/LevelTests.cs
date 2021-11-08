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
    }
}