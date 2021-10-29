using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Collections.Generic;
using Assimp;
using System.IO;

namespace Project.Render {
	/// <summary> Superclass for all renderable objects. This node is essentially a "container" and does nothing when rendered,
	/// but will still render children. </summary>
	public class RenderableNode {
		public List<RenderableNode> Children = new List<RenderableNode>();
		public bool Enabled = true;

		/// <summary> Renders this object and all children, and returns the number of GL draw calls issued. </summary>
		public void Render() {
			if (!Enabled) return;

			foreach (RenderableNode r in Children) {
				r.Render();
			}
			RenderSelf();
		}

		protected virtual void RenderSelf() { }
	}

	/// <summary> Container class for a 3d model containing vertices and indices that exist on the GPU. </summary>
	public class Model : RenderableNode {
		private static readonly Dictionary<string, Model> _cachedModels = new Dictionary<string, Model>();
		private static readonly AssimpContext _assimp = new AssimpContext();

		public readonly int ElementBufferArray_ID;
		public readonly int VertexBufferObject_ID;
		public Vector3 Scale { get; private set; } = Vector3.One;
		public Vector3 Rotation { get; private set; } = Vector3.Zero;
		public Vector3 Position { get; private set; } = Vector3.Zero;
		public bool IsFog { get; private set; } = false;
		public Texture AlbedoTexture { get; private set; } = Texture.CreateTexture("assets/textures/null.png");

		private int _indexLength;

		private Model() { }

		/// <summary> Creates a Model given vertex data and indices. The data is sent to the GPU and then discarded on main memory. </summary>
		public Model(float[] vertexData, uint[] indices) {
			_indexLength = indices.Length;

			ElementBufferArray_ID = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray_ID);
			GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

			VertexBufferObject_ID = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject_ID);
			GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float), vertexData, BufferUsageHint.StaticDraw);
		}

		/// <summary> Private constructor used during model loading from Assimp. </summary>
		private Model(Assimp.Mesh mesh) {
			uint[] indices = (uint[])(object)mesh.GetIndices(); // nasty...

			List<float> vertexDataList = new List<float>(mesh.Vertices.Count * 12);
			for (int i = 0; i < mesh.Vertices.Count; i++) {
				Assimp.Vector3D position = mesh.HasVertices ? mesh.Vertices[i] : new Assimp.Vector3D(0);
				Assimp.Vector3D normal = mesh.HasNormals ? mesh.Normals[i] : new Assimp.Vector3D(0);
				float[] uvs = new float[] { 0f, 0f };
				if (mesh.TextureCoordinateChannelCount > 0) {
					Assimp.Vector3D channel = mesh.TextureCoordinateChannels[0][i];
					uvs = new float[] { channel.X, channel.Y };
				}
				vertexDataList.AddRange(new float[]{
								position.X, position.Y, position.Z,
								normal.X, normal.Y, normal.Z,
								1f, 1f, 1f, 1f,
								uvs[0], uvs[1] });
			}
			var vertexData = vertexDataList.ToArray();
			_indexLength = indices.Length;

			ElementBufferArray_ID = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray_ID);
			GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

			VertexBufferObject_ID = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject_ID);
			GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float), vertexData, BufferUsageHint.StaticDraw);
		}

		/// <summary> Private constructor used when cached models are loaded. </summary>
		private Model(Model copy) {
			this._indexLength = copy._indexLength;
			this.ElementBufferArray_ID = copy.ElementBufferArray_ID;
			this.VertexBufferObject_ID = copy.VertexBufferObject_ID;
		}

		/// <summary> Creates a copy of an existing model that was cached with a name. </summary>
		public static Model GetCachedModel(string modelName) {
			if (_cachedModels.Count == 0)
				CreateUnitModels();
			System.Diagnostics.Debug.Assert(_cachedModels.ContainsKey(modelName), $"Tried to load cached model {modelName} and was unable to find it!");
			return new Model(_cachedModels.GetValueOrDefault(modelName));
		}

		/// <summary> Stores this model in the cache so it may be copied later. </summary>
		public Model Cache(string modelName) {
			System.Diagnostics.Debug.Assert(!_cachedModels.ContainsKey(modelName), $"Tried to cache model {modelName} multiple times!");
			_cachedModels.Add(modelName, this);
			return this;
		}

		/// <summary> Renders this model. It will be drawn in the correct render pass depending on if it is a fog model or real model. </summary>
		protected override void RenderSelf() {
			if (IsFog != (Renderer.INSTANCE.CurrentProgram == Renderer.INSTANCE.FogProgram))
				return;

			Matrix4 modelMatrix = Matrix4.Identity;
			modelMatrix *= Matrix4.CreateScale(Scale);
			modelMatrix *= Matrix4.CreateRotationX(Rotation.X * Renderer.RCF) * Matrix4.CreateRotationY(Rotation.Y * Renderer.RCF) * Matrix4.CreateRotationZ(Rotation.Z * Renderer.RCF);
			modelMatrix *= Matrix4.CreateTranslation(Position);

			if (IsFog) {
				GL.UniformMatrix4(Renderer.INSTANCE.FogProgram.UniformModel_ID, true, ref modelMatrix);
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray_ID);
				GL.BindVertexBuffer(0, VertexBufferObject_ID, (IntPtr)(0 * sizeof(float)), 12 * sizeof(float));
			} else {
				GL.UniformMatrix4(Renderer.INSTANCE.DeferredProgram.UniformModel_ID, true, ref modelMatrix);
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray_ID);
				GL.BindVertexBuffer(0, VertexBufferObject_ID, (IntPtr)(0 * sizeof(float)), 12 * sizeof(float));
				GL.BindVertexBuffer(1, VertexBufferObject_ID, (IntPtr)(3 * sizeof(float)), 12 * sizeof(float));
				GL.BindVertexBuffer(2, VertexBufferObject_ID, (IntPtr)(6 * sizeof(float)), 12 * sizeof(float));
				GL.BindVertexBuffer(3, VertexBufferObject_ID, (IntPtr)(10 * sizeof(float)), 12 * sizeof(float));
				GL.ActiveTexture(TextureUnit.Texture0);
				GL.BindTexture(TextureTarget.Texture2D, AlbedoTexture.TextureID);
			}

			GL.DrawElements(OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, _indexLength, DrawElementsType.UnsignedInt, 0);
		}

		/// <summary> Chainable method to set the scale of this object. </summary>
		public Model SetScale(Vector3 scale) {
			this.Scale = scale;
			return this;
		}

		/// <summary> Chainable method to set the scale of this object in all axis. </summary>
		public Model SetScale(float scale) {
			return SetScale(new Vector3(scale, scale, scale));
		}

		/// <summary> Chainable method to set the rotation of this object. </summary>
		public Model SetRotation(Vector3 rotation) {
			this.Rotation = rotation;
			return this;
		}

		/// <summary> Chainable method to set the position of this object. </summary>
		public Model SetPosition(Vector3 position) {
			this.Position = position;
			return this;
		}

		/// <summary> Sets the fog status. If true, the object will be rendered as volumetric fog. Be sure that your backfaces are actually back faces! </summary>
		public Model SetFoggy(bool isFoggy) {
			this.IsFog = isFoggy;
			return this;
		}

		public Model SetTexture(Texture texture) {
			this.AlbedoTexture = texture;
			return this;
		}

		public Model SetVertices(float[] vertexData) {
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject_ID);
			GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float), vertexData, BufferUsageHint.StaticDraw);
			return this;
		}

		public Model SetIndices(uint[] indexData) {
			_indexLength = indexData.Length;

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray_ID);
			GL.BufferData(BufferTarget.ElementArrayBuffer, indexData.Length * sizeof(uint), indexData, BufferUsageHint.StaticDraw);
			return this;
		}

		public static Model LoadModelFromFile(string file) {
			if (_cachedModels.ContainsKey(file)) {
				return _cachedModels.GetValueOrDefault(file);
			}
			Console.WriteLine($"Loading model from file: \"{file}\", exists: {File.Exists(file)}");
			Scene scene = _assimp.ImportFile(file, PostProcessSteps.Triangulate | PostProcessSteps.FlipUVs | PostProcessSteps.CalculateTangentSpace);
			string p = Path.GetDirectoryName(file).ToString();

			Model[] models = new Model[scene.Meshes.Count];
			for (int i = 0; i < models.Length; i++) {
				Assimp.Mesh currentMesh = scene.Meshes[i];
				models[i] = new Model(scene.Meshes[i]);

				Assimp.Material mat = scene.Materials[currentMesh.MaterialIndex];
				if (mat.HasTextureDiffuse) {
					if (scene.TextureCount > mat.TextureDiffuse.TextureIndex) {
						models[i].SetTexture(Texture.CreateTexture($"{mat.Name}-albedo", scene.Textures[mat.TextureDiffuse.TextureIndex],
										TextureMinFilter.LinearMipmapLinear, OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat));
					} else {
						models[i].SetTexture(Texture.CreateTexture($"{p}\\{mat.TextureDiffuse.FilePath}"));
					}
				}
			}

			Model root = new Model();
			CreateModelFromAssimpNode(root, scene.RootNode, models);

			root.Cache(file);
			return root;
		}

		private static void CreateModelFromAssimpNode(Model parent, Assimp.Node node, Model[] models) {
			if (node.HasMeshes) {
				foreach (int i in node.MeshIndices) {
					Model m = new Model(models[i]).SetTexture(models[i].AlbedoTexture);
					parent.Children.Add(m);
				}
			}
			if (node.HasChildren) {
				foreach (Node n in node.Children) {
					CreateModelFromAssimpNode(parent, n, models);
				}
			}
		}

		private static void CreateUnitModels() {
			// Unit rectangle (2 dimensional)
			new Model(new float[] {
					-0.5f, -0.5f, 0.0f, 1.0f,1.0f, 0.0f, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f,
					 0.5f, -0.5f, 0.0f, 1.0f,1.0f, 0.0f, 1.0f, 1.0f, 1.0f, 0.0f, 1.0f, 0.0f,
					-0.5f,  0.5f, 0.0f, 1.0f,1.0f, 0.0f, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 1.0f,
					 0.5f,  0.5f, 0.0f, 1.0f,1.0f, 0.0f, 1.0f, 1.0f, 1.0f, 0.0f, 1.0f, 1.0f,
			}, new uint[] {
				0, 1, 2, 1, 2, 3
			}).Cache("unit_rectangle");

			// Unit circle (2 dimensional)
			uint density = 180;
			List<float> v = new List<float>();
			v.AddRange(new float[] { 0, 0, 0, 0, 1, 0, 0.2f, 0.4f, 0.6f, 1.0f, 0.5f, 0.5f });
			for (uint g = 0; g < density; g++) {
				float angle = Renderer.RCF * g * (360.0f / (float)density);
				float cos = (float)Math.Cos(angle);
				float sin = (float)Math.Sin(angle);
				v.AddRange(new float[] {
					cos, 0, sin,
					0, 1, 0,
					0.6f, 0.4f, 0.2f, 1.0f,
					(cos + 1) / 2, (sin + 1) / 2});
			}
			List<uint> i = new List<uint>();
			for (uint g = 1; g < density; g++) {
				i.AddRange(new uint[] {
						0, g, g+1
					});
			}
			i.AddRange(new uint[] { 0, density, 1 });
			new Model(v.ToArray(), i.ToArray()).Cache("unit_circle");

			// Unit cylinder
			uint dc = 180;
			List<float> vc = new List<float>();
			List<uint> ic = new List<uint>();

			float transparency = 1f;
			float color = 1.0f;
			for (uint g = 0; g < dc; g++) {
				float angle = Renderer.RCF * g * (360.0f / (float)dc) * 2;
				vc.AddRange(new[] { (float)Math.Cos(angle), 0.0f, (float)Math.Sin(angle) });
				vc.AddRange(new[] { 0f, 0f, 0f, color, color, color, transparency, 0.0f, 0.0f });
				vc.AddRange(new[] { (float)Math.Cos(angle), 1.0f, (float)Math.Sin(angle) });
				vc.AddRange(new[] { 0f, 0f, 0f, color, color, color, transparency, 0.0f, 0.0f });
			}

			for (uint g = 0; g < dc; g++) {
				ic.AddRange(new uint[] { g, g + 1, g + 2 });
			}
			ic.AddRange(new uint[] { dc - 1, dc, 0 });

			new Model(vc.ToArray(), ic.ToArray()).Cache("unit_cylinder");

			// Player model
			new Model(new float[] {
				 0.0f,  0.5f,  0.0f,  1.0f, 0.0f, 0.0f,   0.0f, 0.8f, 0.8f, 1.0f,  0.0f, 0.0f,
				 0.5f,  0.0f,  0.5f,  1.0f, 0.0f, 0.0f,   0.0f, 0.0f, 0.8f, 1.0f,  0.0f, 0.0f,
				 0.5f,  0.0f, -0.5f,  1.0f, 0.0f, 0.0f,   0.0f, 0.0f, 0.8f, 1.0f,  0.0f, 0.0f,
				-0.5f,  0.0f, -0.5f,  1.0f, 0.0f, 0.0f,   0.0f, 0.0f, 0.8f, 1.0f,  0.0f, 0.0f,
				-0.5f,  0.0f,  0.5f,  1.0f, 0.0f, 0.0f,   0.0f, 0.0f, 0.8f, 1.0f,  0.0f, 0.0f,
				 0.0f,  -0.5f, 0.0f,  1.0f, 0.0f, 0.0f,   0.8f, 0.0f, 0.8f, 1.0f,  0.0f, 0.0f,
			}, new uint[] {
				0, 1, 2,
				0, 2, 3,
				0, 3, 4,
				0, 4, 1,
				1, 5, 2,
				2, 5, 3,
				3, 5, 4,
				4, 5, 1
			}).Cache("player");
		}
	}

	/// <summary> Container class for a 2d model containing text, font, and model transforms for text rendering.. </summary>
	public class InterfaceString : RenderableNode {
		public string TextContent { get; private set; }
		public Vector2 Scale { get; private set; } = Vector2.One;
		public Vector2 Position { get; private set; } = Vector2.Zero;
		public float Width { get; private set; }
		public float Opacity { get; private set; } = 1.0f;

		private readonly int _elementBufferArrayID;
		private readonly int _vertexBufferObjectID;
		private FontAtlas _font;
		private int _indexLength;

		/// <summary> Creates a new InterfaceString and handles sending data to the GPU. </summary>
		public InterfaceString(string font, string TextContent) {
			_font = FontAtlas.GetFont(font);
			_elementBufferArrayID = GL.GenBuffer();
			_vertexBufferObjectID = GL.GenBuffer();

			this.TextContent = TextContent;

			UpdateStringOnGPU(TextContent);
		}

		/// <summary> Updates the vertex and index lists on the GPU for the new text. </summary>
		private void UpdateStringOnGPU(string text) {
			List<float> vertices = new List<float>();
			List<uint> indices = new List<uint>();

			List<int> unicodeList = new List<int>(text.Length);
			for (int i = 0; i < text.Length; i++) {
				unicodeList.Add(Char.ConvertToUtf32(text, i));
				if (Char.IsHighSurrogate(text[i]))
					i++;
			}
			float cursor = 0;
			for (int i = 0; i < unicodeList.Count; i++) {
				FontAtlas.Glyph g = _font.GetGlyph(unicodeList[i]);
				vertices.AddRange(new float[] {
					cursor + g.PositionOffset.X, 0 + g.PositionOffset.Y,
					g.UVs[0].X, g.UVs[0].Y,
					cursor + g.PositionOffset.X + g.Size.X, 0 + g.PositionOffset.Y,
					g.UVs[1].X, g.UVs[1].Y,
					cursor + g.PositionOffset.X, g.Size.Y + g.PositionOffset.Y,
					g.UVs[2].X, g.UVs[2].Y,
					cursor + g.PositionOffset.X + g.Size.X, g.Size.Y + g.PositionOffset.Y,
					g.UVs[3].X, g.UVs[3].Y,
				});
				indices.AddRange(new uint[] { ((uint)i * 4) + 0, ((uint)i * 4) + 1, ((uint)i * 4) + 2, ((uint)i * 4) + 1, ((uint)i * 4) + 2, ((uint)i * 4) + 3 });
				cursor += g.Advance;
			}

			float[] vert = vertices.ToArray();
			uint[] ind = indices.ToArray();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferArrayID);
			GL.BufferData(BufferTarget.ElementArrayBuffer, ind.Length * sizeof(uint), ind, BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObjectID);
			GL.BufferData(BufferTarget.ArrayBuffer, vert.Length * sizeof(float), vert, BufferUsageHint.StaticDraw);

			_indexLength = ind.Length;
			Width = cursor;
		}

		/// <summary> Draws the font on the GPU. This is done with the Interface shader, but the use of the font uniform causes this to be a text box. </summary>
		protected override void RenderSelf() {
			Matrix4 modelMatrix = Matrix4.Identity;
			modelMatrix *= Matrix4.CreateScale(new Vector3(Scale.X, Scale.Y, 1f));
			modelMatrix *= Matrix4.CreateTranslation(new Vector3(Position.X, Position.Y, 0f));
			GL.UniformMatrix4(Renderer.INSTANCE.InterfaceProgram.UniformModel_ID, true, ref modelMatrix);

			GL.Uniform1(Renderer.INSTANCE.InterfaceProgram.UniformIsFont, 1);
			GL.Uniform1(Renderer.INSTANCE.InterfaceProgram.UniformOpacity, Opacity);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferArrayID);
			GL.BindVertexBuffer(0, _vertexBufferObjectID, (IntPtr)(0 * sizeof(float)), 4 * sizeof(float));
			GL.BindVertexBuffer(1, _vertexBufferObjectID, (IntPtr)(2 * sizeof(float)), 4 * sizeof(float));

			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, _font.AtlasTexture.TextureID);

			GL.DrawElements(OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, _indexLength, DrawElementsType.UnsignedInt, 0);
		}

		/// <summary> Chainable method to set the scale of this object. </summary>
		public InterfaceString SetScale(Vector2 scale) {
			this.Scale = scale;
			return this;
		}

		/// <summary> Chainable method to set the scale of this object in all axis. </summary>
		public InterfaceString SetScale(float scale) {
			return SetScale(new Vector2(scale, scale));
		}

		/// <summary> Chainable method to set the position of this object. </summary>
		public InterfaceString SetPosition(Vector2 position) {
			this.Position = position;
			return this;
		}

		public InterfaceString SetOpacity(float opacity) {
			this.Opacity = opacity;
			return this;
		}
	}

	/// <summary> Container class for a 2d model containing a textured quad. </summary>
	public class InterfaceModel : RenderableNode {
		private static readonly Dictionary<string, InterfaceModel> _cachedModels = new Dictionary<string, InterfaceModel>();

		public Vector2 Scale { get; private set; } = Vector2.One;
		public float Rotation { get; private set; } = 0.0f;
		public Vector2 Position { get; private set; } = Vector2.Zero;
		public Texture AlbedoTexture { get; private set; } = Texture.CreateTexture("assets/textures/null.png");
		public float Opacity { get; private set; } = 1.0f;

		private int _indexLength;
		private readonly int _elementBufferArrayID;
		private readonly int _vertexBufferObjectID;

		/// <summary> Creates an InterfaceModel given vertex data and indices. </summary>
		public InterfaceModel(float[] vertexData, uint[] indices) {
			_indexLength = indices.Length;

			_elementBufferArrayID = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferArrayID);
			GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

			_vertexBufferObjectID = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObjectID);
			GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float), vertexData, BufferUsageHint.StaticDraw);
		}

		/// <summary> Private constructor used when cached models are loaded. </summary>
		private InterfaceModel(InterfaceModel copy) {
			this._indexLength = copy._indexLength;
			this._elementBufferArrayID = copy._elementBufferArrayID;
			this._vertexBufferObjectID = copy._vertexBufferObjectID;
		}

		/// <summary> Creates a copy of an existing model that was cached with a name.</summary>
		public static InterfaceModel GetCachedModel(string modelName) {
			if (_cachedModels.Count == 0)
				CreateUnitModels();
			System.Diagnostics.Debug.Assert(_cachedModels.ContainsKey(modelName), $"Tried to load cached model {modelName} and was unable to find it!");
			return new InterfaceModel(_cachedModels.GetValueOrDefault(modelName));
		}

		/// <summary> Stores this model in the cache so it may be copied later. </summary>
		public InterfaceModel Cache(string modelName) {
			System.Diagnostics.Debug.Assert(!_cachedModels.ContainsKey(modelName), $"Tried to cache model {modelName} multiple times!");
			_cachedModels.Add(modelName, this);
			return this;
		}

		/// <summary> Draws this interface quad. The font uniform is set to false in this pass allowing use of traditional textures. </summary>
		protected override void RenderSelf() {
			Matrix4 modelMatrix = Matrix4.Identity;
			modelMatrix *= Matrix4.CreateScale(new Vector3(Scale.X, Scale.Y, 1f));
			modelMatrix *= Matrix4.CreateRotationZ(Rotation * Renderer.RCF);
			modelMatrix *= Matrix4.CreateTranslation(new Vector3(Position.X, Position.Y, 0f));
			GL.UniformMatrix4(Renderer.INSTANCE.InterfaceProgram.UniformModel_ID, true, ref modelMatrix);

			GL.Uniform1(Renderer.INSTANCE.InterfaceProgram.UniformIsFont, 0);
			GL.Uniform1(Renderer.INSTANCE.InterfaceProgram.UniformOpacity, Opacity);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferArrayID);
			GL.BindVertexBuffer(0, _vertexBufferObjectID, (IntPtr)(0 * sizeof(float)), 4 * sizeof(float));
			GL.BindVertexBuffer(1, _vertexBufferObjectID, (IntPtr)(2 * sizeof(float)), 4 * sizeof(float));

			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, AlbedoTexture.TextureID);

			GL.DrawElements(OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, _indexLength, DrawElementsType.UnsignedInt, 0);
		}

		/// <summary> Chainable method to set the scale of this object. </summary>
		public InterfaceModel SetScale(Vector2 scale) {
			this.Scale = scale;
			return this;
		}

		/// <summary> Chainable method to set the scale of this object in all axis. </summary>
		public InterfaceModel SetScale(float scale) {
			return SetScale(new Vector2(scale, scale));
		}

		/// <summary> Chainable method to set the rotation of this object. </summary>
		public InterfaceModel SetRotation(float rotation) {
			this.Rotation = rotation;
			return this;
		}

		public InterfaceModel SetOpacity(float opacity) {
			this.Opacity = opacity;
			return this;
		}

		/// <summary> Chainable method to set the position of this object. </summary>
		public InterfaceModel SetPosition(Vector2 position) {
			this.Position = position;
			return this;
		}

		public InterfaceModel SetTexture(Texture texture) {
			this.AlbedoTexture = texture;
			return this;
		}

		private static void CreateUnitModels() {
			// Unit rectangle (2 dimensional)
			new InterfaceModel(new float[] {
					-0.5f, -0.5f, 0.0f, 0.0f,
					 0.5f, -0.5f, 1.0f, 0.0f,
					-0.5f,  0.5f, 0.0f, 1.0f,
					 0.5f,  0.5f, 1.0f, 1.0f,
			}, new uint[] {
				0, 1, 2, 1, 2, 3
			}).Cache("unit_rectangle");

			// Unit circle (2 dimensional)
			uint density = 180;
			List<float> v = new List<float>();
			v.AddRange(new float[] { 0, 0, 0.5f, 0.5f });
			for (uint g = 0; g < density; g++) {
				float angle = Renderer.RCF * g * (360.0f / (float)density);
				float cos = (float)Math.Cos(angle);
				float sin = (float)Math.Sin(angle);
				v.AddRange(new[] { cos, sin, (cos + 1) / 2, (sin + 1) / 2 });
			}
			List<uint> i = new List<uint>();
			for (uint g = 1; g < density; g++) {
				i.AddRange(new uint[] { 0, g, g + 1 });
			}
			i.AddRange(new uint[] { 0, density, 1 });
			new InterfaceModel(v.ToArray(), i.ToArray()).Cache("unit_circle");

			new InterfaceModel(new float[] {
				0.0f, -0.25f, 0.5f, 0.25f, // 0
				0.5f, -0.5f,  1f, 0f, // 1
				0.0f, 0.5f,   0.5f, 1f, // 2
				-0.5f, -0.5f, 0.0f, 0.0f // 3
			}, new uint[] {
				0,1,2, 0,3,2
			}).Cache("pointer");
		}
	}
}

/*

Console.WriteLine($"Loading model from file: \"{filename}\" exists: {File.Exists(filename)}");
AssimpContext c = new AssimpContext();
var scene = c.ImportFile(filename, PostProcessSteps.Triangulate | PostProcessSteps.FlipUVs | PostProcessSteps.CalculateTangentSpace);
Model a = null;
if (scene.HasMeshes) {
	//TODO(trent): Implement properly (children)
	foreach (Mesh m in scene.Meshes) {
		Vector3D[] vertices = m.Vertices.ToArray();
		Vector3D[] normals = m.Normals.ToArray();
		Vector3D[] uvs = m.TextureCoordinateChannels[0].ToArray();
		Material mat = scene.Materials[m.MaterialIndex];

		List<Texture> t = new List<Texture>();

		t.Add(mat.HasTextureDiffuse ?
				new Texture($"{mat.Name}-diffuse", scene.Textures[mat.TextureDiffuse.TextureIndex])
				: Texture.MissingTexture);
		t.Add(mat.HasTextureSpecular ?
				new Texture($"{mat.Name}-gloss", scene.Textures[mat.TextureSpecular.TextureIndex])
				: Texture.MissingTexture);
		t.Add(mat.HasTextureAmbientOcclusion ?
				new Texture($"{mat.Name}-ao", scene.Textures[mat.TextureAmbientOcclusion.TextureIndex])
				: Texture.MissingTexture);
		t.Add(mat.HasTextureNormal ?
				new Texture($"{mat.Name}-normal", scene.Textures[mat.TextureNormal.TextureIndex])
				: Texture.MissingTexture);
		t.Add(mat.HasTextureHeight ?
				new Texture($"{mat.Name}-height", scene.Textures[mat.TextureHeight.TextureIndex])
				: Texture.MissingTexture);

		List<float> vertexData = new List<float>(vertices.Length * 12);
		for (int i = 0; i < vertices.Length; i++) {
			vertexData.AddRange(new float[]{
				vertices[i].X, vertices[i].Y, vertices[i].Z,
				uvs[i][0], uvs[i][1],
				mat.ColorDiffuse.R, mat.ColorDiffuse.G, mat.ColorDiffuse.B, mat.ColorDiffuse.A,
				normals[i].X, normals[i].Y, normals[i].Z});
		}

		int[] ind = m.GetIndices();
		uint[] indices = (uint[])(object)ind; // nasty...
		a = new Model(vertexData.ToArray(), indices, t.ToArray());
	}
}




*/