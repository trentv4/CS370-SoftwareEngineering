using OpenTK.Mathematics;
using System.Collections.Generic;
using Project.Levels;
using System;
using OpenTK.Graphics.OpenGL4;
using Project.Items;
using System.Linq;

namespace Project.Render {
	/// <summary> Primary rendering root that stores custom references to track important objects in the rendering hierarchy. </summary>
	public struct GameRoot {
		/// <summary> 3-dimensional world, including models, entities, and anything "3d" excluding the player model. </summary>
		public RenderableNode Scene;
		/// <summary> Specific reference to the player model, rendered after the scene is rendered, but in the same world space. </summary>
		public Model PlayerModel;

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
			Model plane = Model.GetCachedModel("unit_circle").SetPosition(new Vector3(0, -1f, 0)).SetRotation(new Vector3(0f, 90f, 0)).SetScale(25f);
			plane.AlbedoTexture = new Texture("assets/textures/plane.png");
			models.Add(plane);

			// Wall
			models.Add(Model.GetCachedModel("unit_cylinder").SetFoggy(true).SetPosition(new Vector3(0f, -1f, 00f)).SetScale(new Vector3(roomSize, 10f, roomSize)));

			// Items on the floor
			foreach (Item i in currentRoom.Items) {
				Model itemModel = Model.GetCachedModel("unit_rectangle").SetPosition(i.Position).SetRotation(new Vector3(20.0f, 0, 0));
				itemModel.AlbedoTexture = new Texture($"assets/textures/{i.Definition.TextureName}");
				models.Add(itemModel);
			}

			// Room connectors (doorways)
			foreach (Room r in currentRoom.ConnectedRooms) {
				float angle = currentRoom.AngleToRoom(r);
				Vector3 doorPosition = new Vector3((float)Math.Sin(angle), 0, (float)Math.Cos(angle)) * (roomSize - 0.1f);

				Model door = Model.GetCachedModel("unit_rectangle")
									  .SetPosition(doorPosition)
									  .SetRotation(new Vector3(0, angle / Renderer.RCF, 0))
									  .SetScale(new Vector3(2f, 4f, 1f));
				door.AlbedoTexture = new Texture("assets/textures/door0.png");
				models.Add(door);
			}

			RenderableNode Scene = new RenderableNode();
			Scene.Children.AddRange(models);
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
			Vector2 screenSize = Renderer.INSTANCE.Size;
			Inventory inventory = state.Level.Player.Inventory;

			Item current = null;
			Item left = null;
			Item right = null;
			if (inventory.Position < inventory.Items.Count && inventory.Items.Count > 0) {
				current = inventory.Items[inventory.Position];
			}
			if (inventory.Position - 1 < inventory.Items.Count && inventory.Items.Count > 1 && inventory.Position - 1 >= 0) {
				left = inventory.Items[inventory.Position - 1];
			}
			if (inventory.Position + 1 < inventory.Items.Count && inventory.Items.Count > 1 && inventory.Position + 1 >= 0) {
				right = inventory.Items[inventory.Position + 1];
			}

			InterfaceModel currentItem = InterfaceModel.GetCachedModel("unit_rectangle").SetPosition(new Vector2(screenSize.X / 2, 150)).SetScale(150f);
			InterfaceModel leftItem = InterfaceModel.GetCachedModel("unit_rectangle").SetPosition(new Vector2((screenSize.X / 2) - 150, 100)).SetScale(100f);
			InterfaceModel rightItem = InterfaceModel.GetCachedModel("unit_rectangle").SetPosition(new Vector2((screenSize.X / 2) + 150, 100)).SetScale(100);
			if (current != null)
				currentItem.AlbedoTexture = new Texture($"assets/textures/{current.Definition.TextureName}");
			if (left != null)
				leftItem.AlbedoTexture = new Texture($"assets/textures/{left.Definition.TextureName}");
			if (right != null)
				rightItem.AlbedoTexture = new Texture($"assets/textures/{right.Definition.TextureName}");

			RenderableNode node = new RenderableNode();
			node.Children.AddRange(new[] { currentItem, leftItem, rightItem });
			return node;
		}

		/// <summary> Constructs a RenderableNode containing the widescreen map. This contains rooms as circles, the background element, and connections between rooms. </summary>
		public RenderableNode BuildMapInterface(GameState state) {
			List<InterfaceModel> roomNodes = new List<InterfaceModel>();
			List<InterfaceModel> connectorNodes = new List<InterfaceModel>();
			Room[] rooms = state.Level.Rooms;

			Vector2 screenSize = Renderer.INSTANCE.Size;
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
				Vector2 screenSpacePosition = new Vector2(
					screenMin.X + (current.Position.X - min.X) * (screenMax.X - screenMin.X) / (max.X - min.X),
					screenMin.Y + (current.Position.Y - min.Y) * (screenMax.Y - screenMin.Y) / (max.Y - min.Y)
				);
				InterfaceModel circle = InterfaceModel.GetCachedModel("unit_circle").SetPosition(screenSpacePosition);
				if (current == state.Level.EndRoom) {
					circle.AlbedoTexture = new Texture("assets/textures/green.png");
				} else if (current == state.Level.StartRoom) {
					circle.AlbedoTexture = new Texture("assets/textures/blue.png");
				} else if (current == state.Level.CurrentRoom) {
					circle.AlbedoTexture = new Texture("assets/textures/gold.png");
				} else {
					circle.AlbedoTexture = new Texture("assets/textures/red.png");
				}
				circle.SetScale(40f);
				roomNodes.Add(circle);

				foreach (Room connection in current.ConnectedRooms) {
					if (current.Position.X < connection.Position.X) continue;
					Vector2 positionA = screenSpacePosition;
					Vector2 positionB = new Vector2(
						screenMin.X + (connection.Position.X - min.X) * (screenMax.X - screenMin.X) / (max.X - min.X),
						screenMin.Y + (connection.Position.Y - min.Y) * (screenMax.Y - screenMin.Y) / (max.Y - min.Y)
					);
					Vector2 positionBNormalized = positionB - positionA;

					InterfaceModel connector = InterfaceModel.GetCachedModel("unit_rectangle").SetPosition((positionA + positionB) / 2);
					connector.SetScale(new Vector2(Vector2.Distance(Vector2.Zero, positionBNormalized), 10f));
					connector.SetRotation((float)Math.Atan2(positionBNormalized.Y, positionBNormalized.X) / Renderer.RCF);

					connectorNodes.Add(connector);
				}
			}

			InterfaceModel mapBackground = InterfaceModel.GetCachedModel("unit_rectangle");
			mapBackground.AlbedoTexture = new Texture("assets/textures/interface/map_background.png", TextureMinFilter.Nearest);
			mapBackground.SetScale(new Vector2(screenSize.X / 1.25f, screenSize.Y / 1.25f));
			mapBackground.SetPosition(new Vector2(screenSize.X / 2, screenSize.Y / 2));

			InterfaceString mapLabel = new InterfaceString("calibri", "Map (known)");
			mapLabel.SetScale(50f);
			mapLabel.SetPosition(new Vector2(screenSize.X / 8.5f, screenSize.Y / 1.2f));

			InterfaceString score = new InterfaceString("calibri", $"Score: {state.Level.Score}");
			score.SetScale(50f);
			score.SetPosition(new Vector2(screenSize.X / 1.4f, screenSize.Y / 1.2f));

			Vector2 currentRoom = state.Level.CurrentRoom.Position;
			Vector2 pointerPosition = new Vector2(
				screenMin.X + (currentRoom.X - min.X) * (screenMax.X - screenMin.X) / (max.X - min.X),
				screenMin.Y + (currentRoom.Y - min.Y) * (screenMax.Y - screenMin.Y) / (max.Y - min.Y)
			);
			InterfaceModel pointer = InterfaceModel.GetCachedModel("pointer").SetScale(50f);
			pointer.AlbedoTexture = new Texture("assets/textures/gold.png");
			pointer.SetPosition(pointerPosition);
			pointer.SetRotation(state.CameraYaw + 90);

			RenderableNode interfaceNode = new RenderableNode();
			interfaceNode.Children.Add(mapBackground);
			interfaceNode.Children.AddRange(connectorNodes.ToArray());
			interfaceNode.Children.AddRange(roomNodes.ToArray());
			interfaceNode.Children.Add(mapLabel);
			interfaceNode.Children.Add(score);
			interfaceNode.Children.Add(pointer);
			return interfaceNode;
		}
	}
}
