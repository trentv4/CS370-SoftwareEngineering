using OpenTK.Mathematics;
using Project.Levels;

namespace Project.Render {
	public class SceneBuilder {

		public static void Initialize() {

		}

		public static RenderableNode BuildRoomScene(Room[] rooms) {
			RenderableNode allRooms = new RenderableNode();
			foreach (Room r in rooms) {
				Vector3 positionOffset = new Vector3(r.X * 30, 0, r.Y * 10);
				RenderableNode currentRoom = new RenderableNode();

				Model plane = Model.GetUnitRectangle().SetPosition(positionOffset + new Vector3(0, -1f, 0))
								   .SetRotation(new Vector3(90f, 0, 0)).SetScale(new Vector3(20f, 6f, 1f));
				plane.AlbedoTexture = new Texture("assets/textures/plane.png");
				currentRoom.children.AddRange(new RenderableNode[] {
					plane,
					Model.GetUnitRectangle().SetPosition(positionOffset + new Vector3(0, 0.5f, 3f)).SetRotation(new Vector3(0, 0, 0)).SetScale(new Vector3(3f, 3f, 5f)),
					Model.GetUnitRectangle().SetPosition(positionOffset + new Vector3(-5f, 0.5f, 3f)).SetRotation(new Vector3(0, 0, 0)).SetScale(new Vector3(3f, 3f, 5f)),
					Model.GetUnitRectangle().SetPosition(positionOffset + new Vector3(5f, 0.5f, 3f)).SetRotation(new Vector3(0, 0, 0)).SetScale(new Vector3(3f, 3f, 5f)),
				});
				allRooms.children.Add(currentRoom);
			}
			return allRooms;
		}

		public static RenderableNode CreateDemoScene() {
			RenderableNode Scene = new RenderableNode();

			Model PlayerModel = new Model(new float[] {
				 0.0f,  0.5f,  0.0f,  1.0f, 0.0f, 0.0f,   0.0f, 0.8f, 0.8f, 0.0f,  0.0f, 0.0f,
				 0.5f,  0.0f,  0.5f,  1.0f, 0.0f, 0.0f,   0.0f, 0.0f, 0.8f, 0.0f,  0.0f, 0.0f,
				 0.5f,  0.0f, -0.5f,  1.0f, 0.0f, 0.0f,   0.0f, 0.0f, 0.8f, 0.0f,  0.0f, 0.0f,
				-0.5f,  0.0f, -0.5f,  1.0f, 0.0f, 0.0f,   0.0f, 0.0f, 0.8f, 0.0f,  0.0f, 0.0f,
				-0.5f,  0.0f,  0.5f,  1.0f, 0.0f, 0.0f,   0.0f, 0.0f, 0.8f, 0.0f,  0.0f, 0.0f,
				 0.0f,  -0.5f, 0.0f,  1.0f, 0.0f, 0.0f,   0.8f, 0.0f, 0.8f, 0.0f,  0.0f, 0.0f,
			}, new uint[] {
				0, 1, 2,
				0, 2, 3,
				0, 3, 4,
				0, 4, 1,
				5, 1, 2,
				5, 2, 3,
				5, 3, 4,
				5, 4, 1
			}).SetScale(new Vector3(0.5f, 2f, 0.5f));

			Model plane = Model.GetUnitRectangle().SetPosition(new Vector3(0, -1f, 0)).SetRotation(new Vector3(90f, 0, 0)).SetScale(new Vector3(20f, 6f, 1f));
			plane.AlbedoTexture = new Texture("assets/textures/plane.png");
			Model door1 = Model.GetUnitRectangle().SetPosition(new Vector3(0, 0.5f, 3f)).SetRotation(new Vector3(0, 0, 0)).SetScale(new Vector3(3f, 3f, 5f));
			Model door2 = Model.GetUnitRectangle().SetPosition(new Vector3(-5f, 0.5f, 3f)).SetRotation(new Vector3(0, 0, 0)).SetScale(new Vector3(3f, 3f, 5f));
			Model door3 = Model.GetUnitRectangle().SetPosition(new Vector3(5f, 0.5f, 3f)).SetRotation(new Vector3(0, 0, 0)).SetScale(new Vector3(3f, 3f, 5f));
			RenderableNode room = new RenderableNode();
			room.children.AddRange(new RenderableNode[] {
				plane, door1, door2, door3
			});

			// Order matters! [0] must always be PlayerModel
			Scene.children.AddRange(new RenderableNode[] {
				PlayerModel, room
			});

			return Scene;
		}
	}
}
