using OpenTK.Mathematics;
using System.Collections.Generic;
using Project.Levels;
using System;
using OpenTK.Graphics.OpenGL4;
using Project.Items;

namespace Project.Render {
	/// <summary> Primary rendering root that stores custom references to track important objects in the rendering hierarchy. </summary>
	public struct GameRoot {
		/// <summary> 3-dimensional world, including models, entities, and anything "3d" excluding the player model. </summary>
		public RenderableNode Scene;
		/// <summary> Specific reference to the player model, rendered after the scene is rendered, but in the same world space. </summary>
		public Model PlayerModel;

		private Model _fogWall;

		/// <summary> Creates one-time objects, in particular the player model. </summary>
		public void Build() {
			PlayerModel = Model.GetCachedModel("player").SetScale(new Vector3(0.5f, 2f, 0.5f));
		}

		/// <summary> Renders all three-dimensional objects into world space. </summary>
		public void Render() {
			Scene.Render();
			PlayerModel.Render();
		}

		/// <summary> Provides a RenderableNode containing all contents of the provided room, arranged in no particular order. </summary>
		public RenderableNode BuildRoom(Room currentRoom) {
			List<Model> models = new List<Model>();

			float roomSize = 10.0f;

			// Floor
			Model plane = Model.GetCachedModel("unit_circle").SetPosition(new Vector3(0, -1f, 0)).SetRotation(new Vector3(0f, 90f, 0)).SetScale(40f);
			plane.AlbedoTexture = new Texture("assets/textures/plane.png");
			models.Add(plane);

			//try/catch is a temporary fix.
			try {
			// Items on the floor
			foreach (Item i in currentRoom.Items) {
				Model itemModel = Model.GetCachedModel("unit_rectangle").SetPosition(new Vector3(i.Position.X, 0, i.Position.Y)).SetRotation(new Vector3(20.0f, 0, 0));
				itemModel.AlbedoTexture = new Texture($"assets/textures/{i.Definition.TextureName}");
				models.Add(itemModel);
			}
			} catch (Exception e) { Console.WriteLine(e.ToString()); }

			// Room connectors (doorways)
			foreach (Room r in currentRoom.ConnectedRooms) {
				float angle = currentRoom.AngleToRoom(r);
				Vector3 doorPosition = new Vector3((float)Math.Sin(angle), 0, (float)Math.Cos(angle)) * (roomSize - 0.1f);

				Model door = Model.GetCachedModel("unit_rectangle")
									  .SetPosition(doorPosition)
									  .SetRotation(new Vector3(0, angle / Renderer.RCF, 0))
									  .SetScale(new Vector3(2f, 4f, 1f));
				door.AlbedoTexture = new Texture("assets/textures/door0.png");
				//models.Add(door);
			}

			// Room connectors (doorways)
			List<float> connectionAngles = new List<float>();
			foreach (Room r in currentRoom.ConnectedRooms) {
				float angle = currentRoom.AngleToRoom(r);
				if (angle < 0) angle = (360 * Renderer.RCF) + angle;
				connectionAngles.Add(angle);
			}
			uint density = 360;
			if (_fogWall == null) {
				List<uint> indices = new List<uint>();
				for (uint g = 0; g < density * 2 - 2; g++) {
					indices.AddRange(new uint[] { g, g + 1, g + 2 });
				}
				indices.AddRange(new uint[] { density * 2 - 2, density * 2 - 1, 0, density * 2 - 1, 0, 1 });
				_fogWall = new Model(new float[] { 0 }, indices.ToArray())
					.SetFoggy(true)
					.SetPosition(new Vector3(0f, -1f, 00f))
					.SetScale(new Vector3(roomSize, 10f, roomSize));
			}

			List<float> vc = new List<float>();
			float transparency = 1.0f;
			for (uint g = 0; g < density; g++) {
				float angle = ((float)g) * Renderer.RCF;

				float minDistance = 360;
				foreach (float a in connectionAngles) {
					float x = Math.Min(Math.Abs(angle - a), 6.28f - Math.Abs((angle - a)));
					minDistance = Math.Min(minDistance, x);
				}

				float depth = 0.5f + (1 / (minDistance * 20));
				depth = Math.Min(depth, 3);
				float color = minDistance;
				float cos = depth * (float)Math.Sin(angle);
				float sin = depth * (float)Math.Cos(angle);
				vc.AddRange(new[] { cos, 0.0f, sin });
				vc.AddRange(new[] { 0f, 0f, 0f, color, color, color, transparency, 0.0f, 0.0f });
				vc.AddRange(new[] { cos, 1.0f, sin });
				vc.AddRange(new[] { 0f, 0f, 0f, color, color, color, transparency, 0.0f, 0.0f });
			}

			_fogWall.SetVertices(vc.ToArray());

			RenderableNode Scene = new RenderableNode();
			Scene.Children.AddRange(models);
			Scene.Children.Add(_fogWall);
			return Scene;
		}
	}

	/// <summary> Primary interface root for all user interfaces, separated into "categories" or "specific interface pieces". </summary>
	public struct InterfaceRoot {
		/// <summary> The widescreen map shown when "M" (or other keybinds) are pressed. </summary>
		public RenderableNode Map;
		/// <summary> Unimplemented. </summary>
		public RenderableNode MainMenu;
		/// <summary> Unimplemented. </summary>
		public RenderableNode InGameInterface;

		/// <summary> Creates one-time objects, in particular the main menu and in-game interface shell. </summary>
		public InterfaceRoot Build() {
			return this;
		}

		public InterfaceRoot Rebuild(GameState state) {
			Map = BuildMapInterface(state);
			InGameInterface = BuildInGameInterface(state);
			return this;
		}

		/// <summary> Draws different interface nodes based on the current game state. </summary>
		public void Render(GameState state) {
			if (state.IsInGame) {
				InGameInterface = BuildInGameInterface(state);
				InGameInterface.Render();
				if (state.IsViewingMap)
					Map.Render();
			} else {
				//MainMenu.Render();
			}
		}

		public RenderableNode BuildInGameInterface(GameState state) {
			RenderableNode node = new RenderableNode();
			Room currentLevel = state.Level.CurrentRoom;
			Vector2 screenSize = Renderer.INSTANCE.Size;
			Inventory inventory = state.Level.Player.Inventory;

			// Minimap "rear view mirror"

			List<InterfaceModel> circles = new List<InterfaceModel>();
			List<InterfaceModel> connectionModels = new List<InterfaceModel>();

			Vector2 centerPosition = new Vector2(screenSize.X / 2, screenSize.Y - (250f / 2f));
			circles.Add(InterfaceModel.GetCachedModel("unit_circle")
				.SetTexture(new Texture("assets/textures/interface/circle_blank.png", TextureMinFilter.Nearest))
				.SetScale(30f)
				.SetPosition(centerPosition));

			circles.Add(InterfaceModel.GetCachedModel("pointer")
				.SetScale(30f)
				.SetTexture(new Texture("assets/textures/gold.png"))
				.SetPosition(centerPosition)
				.SetRotation(state.CameraYaw - 90));

			foreach (Room connection in currentLevel.ConnectedRooms) {
				float angle = currentLevel.AngleToRoom(connection);
				Texture tex = new Texture("assets/textures/interface/circle_blank.png");
				if (connection == state.Level.PreviousRoom)
					tex = new Texture("assets/textures/interface/circle_previous_room.png");
				else if (connection == state.Level.EndRoom) {
					tex = new Texture("assets/textures/interface/circle_final_room.png");
				} else if (connection == state.Level.StartRoom) {
					tex = new Texture("assets/textures/interface/circle_start_room.png");
				} else if (connection.Visited == Room.VisitedState.Seen) {
					tex = new Texture("assets/textures/interface/circle_not_visited.png");
				}

				Vector2 connectionPosition = centerPosition + new Vector2((float)Math.Cos(angle) * 75, (float)Math.Sin(angle) * 75);
				circles.Add(InterfaceModel.GetCachedModel("unit_circle")
					.SetTexture(tex)
					.SetScale(20f)
					.SetPosition(connectionPosition));

				Vector2 positionBNormalized = connectionPosition - centerPosition;
				connectionModels.Add(InterfaceModel.GetCachedModel("unit_rectangle")
					.SetPosition((connectionPosition + centerPosition) / 2)
					.SetScale(new Vector2(Vector2.Distance(Vector2.Zero, positionBNormalized), 10f))
					.SetRotation((float)Math.Atan2(positionBNormalized.Y, positionBNormalized.X) / Renderer.RCF));
			}

			node.Children.AddRange(connectionModels);
			node.Children.AddRange(circles);

			// Inventory wheel

			int currentIndex = inventory.Position;
			if (inventory.Items.Count > 0) {
				InterfaceModel currentItem = InterfaceModel.GetCachedModel("unit_circle")
					.SetPosition(new Vector2(screenSize.X / 2, 150))
					.SetScale(150f / 2)
					.SetTexture(new Texture($"assets/textures/{inventory.Items[currentIndex].Definition.TextureName}"));
				InterfaceString u = new InterfaceString("calibri", "[ U ]");
				u.SetScale(35f).SetPosition(new Vector2((screenSize.X / 2) - ((u.Width / 2) * u.Scale.X), 40f));
				node.Children.AddRange(new RenderableNode[] { currentItem, u });
			}
			if (inventory.Items.Count > 1) {
				int rightIndex = currentIndex + 1;
				if (rightIndex == inventory.Items.Count)
					rightIndex = 0;

				InterfaceModel rightItem = InterfaceModel.GetCachedModel("unit_circle")
					.SetPosition(new Vector2((screenSize.X / 2) + 150, 100))
					.SetScale(100f / 2)
					.SetOpacity(0.5f)
					.SetTexture(new Texture($"assets/textures/{inventory.Items[rightIndex].Definition.TextureName}"));
				InterfaceString spinRight = new InterfaceString("calibri", "[E]");
				spinRight.SetScale(35f).SetPosition(new Vector2((screenSize.X / 2) - ((spinRight.Width / 2) * spinRight.Scale.X) + 150, 165));
				node.Children.AddRange(new RenderableNode[] { rightItem, spinRight });
			}

			if (inventory.Items.Count > 2) {
				int leftIndex = currentIndex - 1;
				if (leftIndex == -1)
					leftIndex = inventory.Items.Count - 1;
				InterfaceModel leftItem = InterfaceModel.GetCachedModel("unit_circle")
					.SetPosition(new Vector2((screenSize.X / 2) - 150, 100))
					.SetScale(100f / 2)
					.SetOpacity(0.5f)
					.SetTexture(new Texture($"assets/textures/{inventory.Items[leftIndex].Definition.TextureName}"));
				InterfaceString spinLeft = new InterfaceString("calibri", "[Q]");
				spinLeft.SetScale(35f).SetPosition(new Vector2((screenSize.X / 2) - ((spinLeft.Width / 2) * spinLeft.Scale.X) - 150, 165));
				node.Children.AddRange(new RenderableNode[] { leftItem, spinLeft });
			}

			return node;
		}

		/// <summary> Constructs a RenderableNode containing the widescreen map. This contains rooms as circles, the background element, and connections between rooms. </summary>
		public RenderableNode BuildMapInterface(GameState state) {
			RenderableNode interfaceNode = new RenderableNode();
			List<InterfaceModel> roomNodes = new List<InterfaceModel>();
			List<InterfaceModel> connectorNodes = new List<InterfaceModel>();
			Room[] rooms = state.Level.Rooms;
			Vector2 screenSize = Renderer.INSTANCE.Size;

			interfaceNode.Children.Add(InterfaceModel.GetCachedModel("unit_rectangle")
				.SetTexture(new Texture("assets/textures/interface/map_background.png", TextureMinFilter.Nearest))
				.SetScale(new Vector2(screenSize.X / 1.25f, screenSize.Y / 1.25f))
				.SetPosition(new Vector2(screenSize.X / 2, screenSize.Y / 2)));

			interfaceNode.Children.Add(new InterfaceString("calibri", "Map (known)")
				.SetScale(50f)
				.SetPosition(new Vector2(screenSize.X / 8.5f, screenSize.Y / 1.2f)));

			interfaceNode.Children.Add(new InterfaceString("calibri", $"Score: {state.Level.Score}")
				.SetScale(50f)
				.SetPosition(new Vector2(screenSize.X / 1.4f, screenSize.Y / 1.2f)));


			Vector2 min = new Vector2(10000, 10000);
			Vector2 max = new Vector2(0, 0);
			foreach (Room r in rooms) {
				if (r.Position.X > max.X) max.X = r.Position.X;
				if (r.Position.Y > max.Y) max.Y = r.Position.Y;
				if (r.Position.X < min.X) min.X = r.Position.X;
				if (r.Position.Y < min.Y) min.Y = r.Position.Y;
			}
			Vector2 screenMin = new Vector2(screenSize.X / 1.25f, screenSize.Y / 1.25f);
			Vector2 screenMax = new Vector2(screenSize.X - (screenSize.X / 1.25f), screenSize.Y - (screenSize.Y / 1.25f));

			foreach (Room current in rooms) {
				if (current.Visited == Room.VisitedState.NotSeen)
					continue;
				Vector2 screenSpacePosition = new Vector2(
					screenSize.X - (screenMin.X + (current.Position.X - min.X) * (screenMax.X - screenMin.X) / (max.X - min.X)),
					screenSize.Y - (screenMin.Y + (current.Position.Y - min.Y) * (screenMax.Y - screenMin.Y) / (max.Y - min.Y))
				);
				InterfaceModel circle = InterfaceModel.GetCachedModel("unit_circle").SetPosition(screenSpacePosition);
				circle.SetTexture(new Texture("assets/textures/interface/circle_blank.png"));
				if (current == state.Level.EndRoom) {
					circle.SetTexture(new Texture("assets/textures/interface/circle_final_room.png"));
				} else if (current == state.Level.StartRoom) {
					circle.SetTexture(new Texture("assets/textures/interface/circle_start_room.png"));
				} else if (current == state.Level.PreviousRoom) {
					circle.SetTexture(new Texture("assets/textures/interface/circle_previous_room.png"));
				} else if (current.Visited == Room.VisitedState.Seen) {
					circle.SetTexture(new Texture("assets/textures/interface/circle_not_visited.png"));
				}
				circle.SetScale(40f);
				roomNodes.Add(circle);

				foreach (Room connection in current.ConnectedRooms) {
					if (current.Position.X < connection.Position.X) continue;
					if (connection.Visited == Room.VisitedState.NotSeen) continue;
					Vector2 positionA = screenSpacePosition;
					Vector2 positionB = new Vector2(
						screenSize.X - (screenMin.X + (connection.Position.X - min.X) * (screenMax.X - screenMin.X) / (max.X - min.X)),
						screenSize.Y - (screenMin.Y + (connection.Position.Y - min.Y) * (screenMax.Y - screenMin.Y) / (max.Y - min.Y))
					);
					Vector2 positionBNormalized = positionB - positionA;

					InterfaceModel connector = InterfaceModel.GetCachedModel("unit_rectangle")
						.SetPosition((positionA + positionB) / 2)
						.SetScale(new Vector2(Vector2.Distance(Vector2.Zero, positionBNormalized), 10f))
						.SetRotation((float)Math.Atan2(positionBNormalized.Y, positionBNormalized.X) / Renderer.RCF);

					connectorNodes.Add(connector);
				}
			}

			Vector2 currentRoom = state.Level.CurrentRoom.Position;
			Vector2 pointerPosition = new Vector2(
				screenSize.X - (screenMin.X + (currentRoom.X - min.X) * (screenMax.X - screenMin.X) / (max.X - min.X)),
				screenSize.Y - (screenMin.Y + (currentRoom.Y - min.Y) * (screenMax.Y - screenMin.Y) / (max.Y - min.Y))
			);

			interfaceNode.Children.AddRange(connectorNodes.ToArray());
			interfaceNode.Children.AddRange(roomNodes.ToArray());

			interfaceNode.Children.Add(InterfaceModel.GetCachedModel("unit_circle")
				.SetScale(30f)
				.SetTexture(new Texture("assets/textures/gold.png"))
				.SetPosition(pointerPosition));

			return interfaceNode;
		}
	}
}
