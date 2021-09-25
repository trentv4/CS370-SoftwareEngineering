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
		/// <summary> Interface root object, see documentation of InterfaceRoot for more details. </summary>
		public InterfaceRoot Interface;

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
			Model plane = Model.GetCachedModel("unit_circle").SetPosition(new Vector3(0, -1f, 0)).SetRotation(new Vector3(0f, 90f, 0)).SetScale(50f);
			plane.AlbedoTexture = new Texture("assets/textures/plane.png");
			models.Add(plane);

			// Walls
			float distance = 20;
			float iterations = 50;
			for (float scale = distance + roomSize; scale > roomSize; scale -= (distance / iterations)) {
				models.Add(Model.GetCachedModel("unit_cylinder").SetPosition(new Vector3(0f, -1f, 00f)).SetScale(new Vector3(scale, 10f, scale)));
			}

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
				door.AlbedoTexture = new Texture("assets/textures/blue.png");
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
		public InterfaceModel MainMenu;
		/// <summary> Unimplemented. </summary>
		public InterfaceModel InGameInterface;

		/// <summary> Creates one-time objects, in particular the main menu and in-game interface shell. </summary>
		public InterfaceRoot Build() {
			return this;
		}

		/// <summary> Draws different interface nodes based on the current game state. </summary>
		public void Render(GameState state) {
			if (state.IsViewingMap) {
				Map.Render();
			}
		}

		/// <summary> Constructs a RenderableNode containing the widescreen map. This contains rooms as circles, the background element, and connections between rooms. </summary>
		public RenderableNode BuildMapInterface(GameState state) {
			List<InterfaceModel> roomNodes = new List<InterfaceModel>();
			List<InterfaceModel> connectorNodes = new List<InterfaceModel>();
			Room[] rooms = state.Level.Rooms;

			float bothScaling = 0.75f;
			float xScaling = (18.0f / state.Level.EndRoom.Position.X) * bothScaling;
			float yScaling = 3 * bothScaling;
			Matrix3 adjust = new Matrix3(new Vector3(xScaling, 0, 0), new Vector3(0, yScaling, 0), new Vector3(-9.0f * bothScaling, -2.5f * yScaling, 1));

			foreach (Room current in rooms) {
				InterfaceModel circle = InterfaceModel.GetCachedModel("unit_circle").SetPosition(Transform(current.Position, adjust));
				if (current == state.Level.EndRoom) {
					circle.AlbedoTexture = new Texture("assets/textures/green.png");
				} else if (current == state.Level.StartRoom) {
					circle.AlbedoTexture = new Texture("assets/textures/blue.png");
				} else if (current == state.Level.CurrentRoom) {
					circle.AlbedoTexture = new Texture("assets/textures/gold.png");
				} else {
					circle.AlbedoTexture = new Texture("assets/textures/red.png");
				}
				roomNodes.Add(circle);

				foreach (Room connection in current.ConnectedRooms) {
					if (current.Position.X < connection.Position.X) continue;
					Vector2 positionA = Transform(current.Position, adjust);
					Vector2 positionB = Transform(connection.Position, adjust);
					Vector2 positionBNormalized = positionB - positionA;

					InterfaceModel connector = InterfaceModel.GetCachedModel("unit_rectangle").SetPosition((positionA + positionB) / 2);
					connector.SetScale(new Vector2(Vector2.Distance(Vector2.Zero, positionBNormalized) * xScaling, 0.5f));
					connector.SetRotation((float)Math.Atan2(positionBNormalized.Y, positionBNormalized.X) / Renderer.RCF);

					connectorNodes.Add(connector);
				}
			}

			InterfaceModel mapBackground = InterfaceModel.GetCachedModel("unit_rectangle");
			mapBackground.AlbedoTexture = new Texture("assets/textures/interface/map_background.png", TextureMinFilter.Nearest);
			mapBackground.SetScale(new Vector2(30, 16));

			RenderableNode interfaceNode = new RenderableNode();
			interfaceNode.Children.Add(mapBackground);
			interfaceNode.Children.AddRange(connectorNodes.ToArray());
			interfaceNode.Children.AddRange(roomNodes.ToArray());
			return interfaceNode;
		}

		/// <summary> Used when constructing the map to transform Vector2s by a translation/scaling transform. </summary>
		private static Vector2 Transform(Vector2 start, Matrix3 adjustmentMatrix) {
			Vector3 adjustedPosition = new Vector3(start.X, start.Y, 1.0f) * adjustmentMatrix;
			return new Vector2(adjustedPosition.X, adjustedPosition.Y);
		}
	}
}
