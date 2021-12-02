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
		/// <summary> Cached reference to the fog wall surrounding a room. This reuses the same vertex buffers on the GPU, and only new vertices
		/// are uploaded to the GPU to update instead of creating new objects for every room. This object is stored in the scene hierarchy automatically. </summary>
		private Model _fogWall;

		private static RenderableNode RenderTestGym;

		/// <summary> Creates one-time objects. </summary>
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
			if (currentRoom == null)
				return BuildRenderTestGym();

			RenderableNode Scene = new RenderableNode();

			float roomSize = 10.0f;

			// Floor
			Scene.Children.Add(Model.GetCachedModel("unit_circle")
				.SetPosition(new Vector3(0, -1f, 0))
				.SetRotation(new Vector3(0f, 90f, 0))
				.SetScale(roomSize * 4)
				.SetTexture(Texture.CreateTexture($"assets/textures/{currentRoom.FloorTexture}")));

			//try/catch is a temporary fix.
			try {
				// Items on the floor
				foreach (Item i in currentRoom.Items) {
					Scene.Children.Add(Model.GetCachedModel("unit_rectangle")
						.SetPosition(new Vector3(i.Position.X, 0, i.Position.Y))
						.SetRotation(new Vector3(20.0f, 0, 0))
						.SetTexture(Texture.CreateTexture($"assets/textures/{i.Definition.TextureName}")));
				}

				// Non-carryable objects on the floor
				foreach (LevelObject obj in currentRoom.Objects) {
					float posY = (obj.Scale - 1.0f) * 0.5f; //Slide objects up with increasing scale
					Scene.Children.Add(Model.GetCachedModel("unit_rectangle")
						.SetPosition(new Vector3(obj.Position.X, posY, obj.Position.Y))
						.SetScale(obj.Scale)
						.SetRotation(new Vector3(20.0f, 0, 0))
						.SetTexture(Texture.CreateTexture($"assets/textures/{obj.TextureName}")));
				}
			} catch (Exception e) { Console.WriteLine(e.ToString()); }

			// Creates a list of angles pointing towards neighboring room connections
			List<float> connectionAngles = new List<float>();
			foreach (Room r in currentRoom.ConnectedRooms) {
				float angle = currentRoom.AngleToRoom(r);
				if (angle < 0) angle = (360 * Renderer.RCF) + angle;
				connectionAngles.Add(angle);
			}

			uint density = 360;

			// If the fog wall is null, generate the indices and create the model.
			if (_fogWall == null) {
				List<uint> indices = new List<uint>();

				uint d = (density * 2) - 2;
				for (uint g = 0; g < d; g += 2) {
					indices.AddRange(new uint[] { g + 0, g + 1, g + 3 });
					indices.AddRange(new uint[] { g + 0, g + 3, g + 2 });
				}
				indices.AddRange(new uint[] { d, d + 1, 1, d, 1, 0 });

				_fogWall = new Model(new float[] { 0 }, indices.ToArray())
					.SetFoggy(true)
					.SetPosition(new Vector3(0f, -1f, 00f))
					.SetScale(new Vector3(roomSize, 10f, roomSize));
			}

			List<float> vc = new List<float>();
			for (uint g = 0; g < density; g++) {
				float angle = (float)g * Renderer.RCF;

				float minDistance = 360;
				foreach (float a in connectionAngles) {
					minDistance = Math.Min(minDistance, Math.Min(Math.Abs(angle - a), (360f * Renderer.RCF) - Math.Abs((angle - a))));
				}

				float depth = Math.Min(0.5f + (1 / (minDistance * 20)), 3);
				float cos = depth * (float)Math.Sin(angle); // just ignore the mismatch... legacy code
				float sin = depth * (float)Math.Cos(angle);
				vc.AddRange(new[] { cos, 0.0f, sin, 0f, 0f, 0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f });
				vc.AddRange(new[] { cos, 1.0f, sin, 0f, 0f, 0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f });
			}

			_fogWall.SetVertices(vc.ToArray());
			Scene.Children.Add(_fogWall);

			return Scene;
		}

		private RenderableNode BuildRenderTestGym() {
			if (RenderTestGym != null) return RenderTestGym;
			RenderableNode Scene = new RenderableNode();

			Scene.Children.Add(Model.LoadModelFromFile("assets/models/sponza/Sponza.gltf").SetScale(0.05f).SetPosition(new Vector3(0f, -1f, 0f)));
			Scene.Children.Add(Model.LoadModelFromFile("assets/models/truck_grey.glb").SetScale(10f).SetPosition(new Vector3(20f, -1f, 0f)).SetFoggy(true));
			Scene.Children.Add(Model.LoadModelFromFile("assets/models/grave1.obj").SetPosition(new Vector3(-5f, 1f, 0f)));

			RenderTestGym = Scene;
			return Scene;
		}
	}

	/// <summary> Primary interface root for all user interfaces, separated into "categories" or "specific interface pieces". </summary>
	public struct InterfaceRoot {
		/// <summary> The widescreen map shown when "M" (or other keybinds) are pressed. </summary>
		public RenderableNode Map;
		/// <summary> Unimplemented. </summary>
		public RenderableNode MainMenu;
		/// <summary> Interface show during primary gameplay. Contains the inventory and compass. </summary>
		public RenderableNode InGameInterface;

		public InterfaceRoot Rebuild(GameState state) {
			BuildMapInterface(state);
			BuildInGameInterface(state);
			return this;
		}

		/// <summary> Draws different interface nodes based on the current game state. </summary>
		public void Render(GameState state) {
			if (state.IsInGame) {
				BuildInGameInterface(state);
				InGameInterface.Render();
				if (state.IsViewingMap)
					Map.Render();
			} else {
				//MainMenu.Render();
			}
		}

		public void BuildInGameInterface(GameState state) {
			RenderableNode node = new RenderableNode();
			Room currentLevel = state.Level.CurrentRoom;
			Vector2 screenSize = Renderer.INSTANCE.Size;
			Inventory inventory = state.Level.Player.Inventory;

			// Minimap "rear view mirror"
			InterfaceString weight = new InterfaceString("calibri", $"Weight: {state.Level.Player.Inventory.Weight} / {state.Level.Player.CarryWeight}");
			weight.SetScale(35f).SetPosition(new Vector2((screenSize.X / 4) - ((weight.Width / 2) * weight.Scale.X), 30f));
			InterfaceString health = new InterfaceString("calibri", $"Health: {state.Level.Player.Health} / {state.Level.Player.MaxHealth}");
			health.SetScale(35f).SetPosition(new Vector2((screenSize.X / 4) - ((weight.Width / 2) * weight.Scale.X), 60f));
			InterfaceString mana = new InterfaceString("calibri", $"Mana: {state.Level.Player.Mana} / {state.Level.Player.MaxMana}");
			mana.SetScale(35f).SetPosition(new Vector2((screenSize.X / 4) - ((weight.Width / 2) * weight.Scale.X), 90f));
			InterfaceString armor = new InterfaceString("calibri", $"Armor: {state.Level.Player.Armor}");
			armor.SetScale(35f).SetPosition(new Vector2((screenSize.X / 4) - ((weight.Width / 2) * weight.Scale.X), 120f));
			node.Children.AddRange(new RenderableNode[] { weight, health, mana, armor });

			// Minimap "rear view mirror"

			List<InterfaceModel> circles = new List<InterfaceModel>();
			List<InterfaceModel> connectionModels = new List<InterfaceModel>();

			Vector2 centerPosition = new Vector2(screenSize.X / 2, screenSize.Y - (250f / 2f));
			circles.Add(InterfaceModel.GetCachedModel("unit_circle")
				.SetTexture(Texture.CreateTexture("assets/textures/interface/circle_blank.png", TextureMinFilter.Nearest))
				.SetScale(30f)
				.SetPosition(centerPosition));

			circles.Add(InterfaceModel.GetCachedModel("pointer")
				.SetScale(30f)
				.SetTexture(Texture.CreateTexture("assets/textures/gold.png"))
				.SetPosition(centerPosition)
				.SetRotation(state.CameraYaw - 90));

			foreach (Room connection in currentLevel.ConnectedRooms) {
				float angle = currentLevel.AngleToRoom(connection);
				Texture tex = Texture.CreateTexture("assets/textures/interface/circle_blank.png");
				if (connection == state.Level.PreviousRoom)
					tex = Texture.CreateTexture("assets/textures/interface/circle_previous_room.png");
				else if (connection == state.Level.EndRoom) {
					tex = Texture.CreateTexture("assets/textures/interface/circle_final_room.png");
				} else if (connection == state.Level.StartRoom) {
					tex = Texture.CreateTexture("assets/textures/interface/circle_start_room.png");
				} else if (connection.Visited == Room.VisitedState.Seen || connection.Visited == Room.VisitedState.Visited) {
					if (connection.GetType() == typeof(IcyRoom))
						tex = Texture.CreateTexture("assets/textures/interface/circle_ice_room.png");
					else if (connection.GetType() == typeof(WindyRoom))
						tex = Texture.CreateTexture("assets/textures/interface/circle_wind_room.png");
					else if (connection.Visited == Room.VisitedState.Visited && connection.Items.Count != 0)
						tex = Texture.CreateTexture("assets/textures/interface/circle_room_has_items.png");
					else
						tex = Texture.CreateTexture("assets/textures/interface/circle_blank.png");
				}
				else
					tex = Texture.CreateTexture("assets/textures/interface/circle_not_visited.png");


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
					.SetTexture(Texture.CreateTexture($"assets/textures/{inventory.Items[currentIndex].Definition.TextureName}"));
				InterfaceString u = new InterfaceString("calibri", "[ U use ] [ X discard ]");
				u.SetScale(35f).SetPosition(new Vector2((screenSize.X / 2) - ((u.Width / 2) * u.Scale.X), 30f));
				InterfaceString itemName = new InterfaceString("calibri", $"{inventory.Items[currentIndex].Definition.Name}");
				itemName.SetScale(35f).SetPosition(new Vector2((screenSize.X / 2) - ((itemName.Width / 2) * itemName.Scale.X), 250f));
				node.Children.AddRange(new RenderableNode[] { currentItem, u, itemName });
			}
			if (inventory.Items.Count > 1) {
				int rightIndex = currentIndex + 1;
				if (rightIndex == inventory.Items.Count)
					rightIndex = 0;

				InterfaceModel rightItem = InterfaceModel.GetCachedModel("unit_circle")
					.SetPosition(new Vector2((screenSize.X / 2) + 150, 100))
					.SetScale(100f / 2)
					.SetOpacity(0.5f)
					.SetTexture(Texture.CreateTexture($"assets/textures/{inventory.Items[rightIndex].Definition.TextureName}"));
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
					.SetTexture(Texture.CreateTexture($"assets/textures/{inventory.Items[leftIndex].Definition.TextureName}"));
				InterfaceString spinLeft = new InterfaceString("calibri", "[Q]");
				spinLeft.SetScale(35f).SetPosition(new Vector2((screenSize.X / 2) - ((spinLeft.Width / 2) * spinLeft.Scale.X) - 150, 165));
				node.Children.AddRange(new RenderableNode[] { leftItem, spinLeft });
			}

			InGameInterface = node;
		}

		/// <summary> Constructs a RenderableNode containing the widescreen map. This contains rooms as circles, the background element, and connections between rooms. </summary>
		public void BuildMapInterface(GameState state) {
			if (state.Level == null) {
				Map = new RenderableNode();
				return;
			}
			RenderableNode interfaceNode = new RenderableNode();
			List<InterfaceModel> roomNodes = new List<InterfaceModel>();
			List<InterfaceModel> connectorNodes = new List<InterfaceModel>();
			Room[] rooms = state.Level.Rooms;
			Vector2 screenSize = Renderer.INSTANCE.Size;

			interfaceNode.Children.Add(InterfaceModel.GetCachedModel("unit_rectangle")
				.SetTexture(Texture.CreateTexture("assets/textures/interface/map_background.png", TextureMinFilter.Nearest))
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
				circle.SetTexture(Texture.CreateTexture("assets/textures/interface/circle_blank.png"));
				if (current == state.Level.EndRoom) {
					circle.SetTexture(Texture.CreateTexture("assets/textures/interface/circle_final_room.png"));
				} else if (current == state.Level.StartRoom) {
					circle.SetTexture(Texture.CreateTexture("assets/textures/interface/circle_start_room.png"));
				} else if (current == state.Level.PreviousRoom) {
					circle.SetTexture(Texture.CreateTexture("assets/textures/interface/circle_previous_room.png"));
				} else if (current.Visited == Room.VisitedState.Seen || current.Visited == Room.VisitedState.Visited) {
					if (current.GetType() == typeof(IcyRoom))
						circle.SetTexture(Texture.CreateTexture("assets/textures/interface/circle_ice_room.png"));
					else if (current.GetType() == typeof(WindyRoom))
						circle.SetTexture(Texture.CreateTexture("assets/textures/interface/circle_wind_room.png"));
					else if (current.Visited == Room.VisitedState.Visited && current.Items.Count != 0)
						circle.SetTexture(Texture.CreateTexture("assets/textures/interface/circle_room_has_items.png"));
					else
						circle.SetTexture(Texture.CreateTexture("assets/textures/interface/circle_blank.png"));
				}
				else
					circle.SetTexture(Texture.CreateTexture("assets/textures/interface/circle_not_visited.png"));

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
				.SetTexture(Texture.CreateTexture("assets/textures/gold.png"))
				.SetPosition(pointerPosition));

			Map = interfaceNode;
		}
	}
}
