using OpenTK.Mathematics;
using System.Collections.Generic;
using Project.Levels;
using System;
using OpenTK.Graphics.OpenGL4;

namespace Project.Render {
	public class SceneBuilder {
		public static RenderableNode CreateDemoScene() {
			RenderableNode Scene = new RenderableNode();

			Model plane = Model.GetCachedModel("unit_rectangle").SetPosition(new Vector3(0, -1f, 0)).SetRotation(new Vector3(90f, 0, 0)).SetScale(new Vector3(20f, 6f, 1f));
			plane.AlbedoTexture = new Texture("assets/textures/plane.png");
			Model door1 = Model.GetCachedModel("unit_rectangle").SetPosition(new Vector3(0, 0.5f, 3f)).SetRotation(new Vector3(0, 0, 0)).SetScale(new Vector3(3f, 3f, 5f));
			Model door2 = Model.GetCachedModel("unit_rectangle").SetPosition(new Vector3(-5f, 0.5f, 3f)).SetRotation(new Vector3(0, 0, 0)).SetScale(new Vector3(3f, 3f, 5f));
			Model door3 = Model.GetCachedModel("unit_rectangle").SetPosition(new Vector3(5f, 0.5f, 3f)).SetRotation(new Vector3(0, 0, 0)).SetScale(new Vector3(3f, 3f, 5f));

			// Order matters! [0] must always be PlayerModel
			Scene.Children.AddRange(new RenderableNode[] {
				plane, door1, door2, door3
			});

			return Scene;
		}

		public static InterfaceModel CreateInterface() {
			return InterfaceModel.GetCachedModel("unit_circle");
		}
	}

	public struct GameRoot {
		public RenderableNode Scene;
		public Model PlayerModel;
		public InterfaceRoot Interface;

		public void Build() {
			Scene = SceneBuilder.CreateDemoScene();
			PlayerModel = Model.GetCachedModel("player").SetScale(new Vector3(0.5f, 2f, 0.5f));
			Interface = new InterfaceRoot().Build();
		}

		public void Render() {
			Scene.Render();
			PlayerModel.Render();
		}
	}

	public struct InterfaceRoot {
		public RenderableNode Map;
		public InterfaceModel MainMenu;
		public InterfaceModel InGameInterface;

        public InterfaceRoot Build() {
			return this;
		}

		public void Render(GameState state) {
			if (state.IsViewingMap) {
				if(Map == null) Map = CreateMapNode(state);
                Map.Render();
			}
		}

		public RenderableNode CreateMapNode(GameState state) {
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

		private Vector2 Transform(Vector2 start, Matrix3 adjustmentMatrix) {
			Vector3 adjustedPosition = new Vector3(start.X, start.Y, 1.0f) * adjustmentMatrix;
			return new Vector2(adjustedPosition.X, adjustedPosition.Y);
		}
	}
}
