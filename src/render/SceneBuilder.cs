using OpenTK.Mathematics;

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
		public InterfaceModel Map;
		public InterfaceModel MainMenu;
		public InterfaceModel InGameInterface;

		public InterfaceRoot Build() {
			return this;
		}

		public void Render(GameState state) {
			if (state.IsViewingMap) {
				// Recreate the map
				RenderableNode interfaceNode = new RenderableNode();

				InterfaceModel circle = InterfaceModel.GetCachedModel("unit_circle");
				circle.SetPosition(new Vector2(0, 0));
				circle.AlbedoTexture = new Texture("assets/textures/plane.png");
				interfaceNode.Children.Add(circle);
				interfaceNode.Render();
			}
		}
	}
}
