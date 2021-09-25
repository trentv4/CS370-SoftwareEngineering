using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Collections.Generic;

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

	public class Model : RenderableNode {
		public readonly int ElementBufferArray_ID;
		public readonly int VertexBufferObject_ID;
		public Vector3 Scale { get; private set; } = Vector3.One;
		public Vector3 Rotation { get; private set; } = Vector3.Zero;
		public Vector3 Position { get; private set; } = Vector3.Zero;
		public Texture AlbedoTexture = new Texture("assets/textures/null.png");

		private int IndexLength;
		private static readonly Dictionary<string, Model> _cachedModels = new Dictionary<string, Model>();

		/// <summary> Creates a Model given vertex data and indices. </summary>
		public Model(float[] vertexData, uint[] indices) {
			IndexLength = indices.Length;

			ElementBufferArray_ID = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray_ID);
			GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

			VertexBufferObject_ID = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject_ID);
			GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float), vertexData, BufferUsageHint.StaticDraw);
		}

		/// <summary> Private constructor used when cached models are loaded. </summary>
		private Model(Model copy) {
			this.IndexLength = copy.IndexLength;
			this.ElementBufferArray_ID = copy.ElementBufferArray_ID;
			this.VertexBufferObject_ID = copy.VertexBufferObject_ID;
		}

		/// <summary> Creates a copy of an existing model that was cached with a name.</summary>
		public static Model GetCachedModel(string modelName) {
			System.Diagnostics.Debug.Assert(_cachedModels.ContainsKey(modelName), $"Tried to load cached model {modelName} and was unable to find it!");
			return new Model(_cachedModels.GetValueOrDefault(modelName));
		}

		/// <summary> Stores this model in the cache so it may be copied later. </summary>
		public Model Cache(string modelName) {
			System.Diagnostics.Debug.Assert(!_cachedModels.ContainsKey(modelName), $"Tried to cache model {modelName} multiple times!");
			_cachedModels.Add(modelName, this);
			return this;
		}

		protected override void RenderSelf() {
			Matrix4 modelMatrix = Matrix4.Identity;
			modelMatrix *= Matrix4.CreateScale(Scale);
			modelMatrix *= Matrix4.CreateRotationX(Rotation.X * Renderer.RCF) * Matrix4.CreateRotationY(Rotation.Y * Renderer.RCF) * Matrix4.CreateRotationZ(Rotation.Z * Renderer.RCF);
			modelMatrix *= Matrix4.CreateTranslation(Position);
			GL.UniformMatrix4(Renderer.INSTANCE.ForwardProgram.UniformModel_ID, true, ref modelMatrix);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray_ID);
			GL.BindVertexBuffer(0, VertexBufferObject_ID, (IntPtr)(0 * sizeof(float)), 12 * sizeof(float));
			GL.BindVertexBuffer(1, VertexBufferObject_ID, (IntPtr)(3 * sizeof(float)), 12 * sizeof(float));
			GL.BindVertexBuffer(2, VertexBufferObject_ID, (IntPtr)(6 * sizeof(float)), 12 * sizeof(float));
			GL.BindVertexBuffer(3, VertexBufferObject_ID, (IntPtr)(10 * sizeof(float)), 12 * sizeof(float));

			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, AlbedoTexture.TextureID);

			GL.DrawElements(OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, IndexLength, DrawElementsType.UnsignedInt, 0);
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

		public static void CreateUnitModels() {
			// Unit rectangle (2 dimensional)
			new Model(new float[] {
					-0.5f, -0.5f, 0.0f, 1.0f,1.0f, 0.0f, 1.0f, 1.0f, 0.5f, 1.0f, 0.0f, 0.0f,
					 0.5f, -0.5f, 0.0f, 1.0f,1.0f, 0.0f, 1.0f, 1.0f, 0.5f, 1.0f, 1.0f, 0.0f,
					-0.5f,  0.5f, 0.0f, 1.0f,1.0f, 0.0f, 1.0f, 1.0f, 0.5f, 1.0f, 0.0f, 1.0f,
					 0.5f,  0.5f, 0.0f, 1.0f,1.0f, 0.0f, 1.0f, 1.0f, 0.5f, 1.0f, 1.0f, 1.0f,
			}, new uint[] {
				0, 1, 2, 1, 2, 3
			}).Cache("unit_rectangle");

			// Unit circle (2 dimensional)
			uint density = 180;
			List<float> v = new List<float>();
			v.AddRange(new float[] { 0, 0, 0, 0, 1, 0, 0.2f, 0.4f, 0.6f, 1.0f, 0.5f, 0.5f });
			for (uint g = 0; g < density; g++) {
				float angle = Renderer.RCF * g * (360.0f / (float)density);
				v.AddRange(new float[] {
					(float)Math.Cos(angle), 0, (float)Math.Sin(angle),
					0, 1, 0,
					0.6f, 0.4f, 0.2f, 1.0f,
					(float)Math.Cos(angle), (float)Math.Sin(angle)});
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

			float transparency = 0.1f;
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
				5, 1, 2,
				5, 2, 3,
				5, 3, 4,
				5, 4, 1
			}).Cache("player");
		}
	}

	public class InterfaceModel : RenderableNode {
		public readonly int ElementBufferArray_ID;
		public readonly int VertexBufferObject_ID;
		public Vector2 Scale { get; private set; } = Vector2.One;
		public float Rotation { get; private set; } = 0.0f;
		public Vector2 Position { get; private set; } = Vector2.Zero;
		public Texture AlbedoTexture = new Texture("assets/textures/null.png");

		private int IndexLength;
		private static readonly Dictionary<string, InterfaceModel> _cachedModels = new Dictionary<string, InterfaceModel>();

		/// <summary> Creates an InterfaceModel given vertex data and indices. </summary>
		public InterfaceModel(float[] vertexData, uint[] indices) {
			IndexLength = indices.Length;

			ElementBufferArray_ID = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray_ID);
			GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

			VertexBufferObject_ID = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject_ID);
			GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float), vertexData, BufferUsageHint.StaticDraw);
		}

		/// <summary> Private constructor used when cached models are loaded. </summary>
		private InterfaceModel(InterfaceModel copy) {
			this.IndexLength = copy.IndexLength;
			this.ElementBufferArray_ID = copy.ElementBufferArray_ID;
			this.VertexBufferObject_ID = copy.VertexBufferObject_ID;
		}

		/// <summary> Creates a copy of an existing model that was cached with a name.</summary>
		public static InterfaceModel GetCachedModel(string modelName) {
			System.Diagnostics.Debug.Assert(_cachedModels.ContainsKey(modelName), $"Tried to load cached model {modelName} and was unable to find it!");
			return new InterfaceModel(_cachedModels.GetValueOrDefault(modelName));
		}

		/// <summary> Stores this model in the cache so it may be copied later. </summary>
		public InterfaceModel Cache(string modelName) {
			System.Diagnostics.Debug.Assert(!_cachedModels.ContainsKey(modelName), $"Tried to cache model {modelName} multiple times!");
			_cachedModels.Add(modelName, this);
			return this;
		}

		protected override void RenderSelf() {
			float scaleRatio = (Renderer.INSTANCE.Size.Y / (float)Renderer.INSTANCE.Size.X);
			Matrix3 modelMatrix = Matrix3.Identity;
			modelMatrix *= Matrix3.CreateScale(new Vector3(scaleRatio * Scale.X, Scale.Y, 1.0f));
			modelMatrix *= Matrix3.CreateScale(1.0f / 10f);
			modelMatrix *= Matrix3.CreateRotationZ(Rotation * Renderer.RCF);
			modelMatrix *= new Matrix3(new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(-Position.X, -Position.Y, 0.0f));

			GL.UniformMatrix3(Renderer.INSTANCE.InterfaceProgram.UniformMVP_ID, true, ref modelMatrix);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray_ID);
			GL.BindVertexBuffer(0, VertexBufferObject_ID, (IntPtr)(0 * sizeof(float)), 4 * sizeof(float));
			GL.BindVertexBuffer(1, VertexBufferObject_ID, (IntPtr)(2 * sizeof(float)), 4 * sizeof(float));

			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, AlbedoTexture.TextureID);

			GL.DrawElements(OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, IndexLength, DrawElementsType.UnsignedInt, 0);
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

		/// <summary> Chainable method to set the position of this object. </summary>
		public InterfaceModel SetPosition(Vector2 position) {
			this.Position = position;
			return this;
		}

		public static void CreateUnitModels() {
			// Unit rectangle (2 dimensional)
			new InterfaceModel(new float[] {
					-0.5f, -0.5f, 0.0f, 1.0f,
					 0.5f, -0.5f, 1.0f, 1.0f,
					-0.5f,  0.5f, 0.0f, 0.0f,
					 0.5f,  0.5f, 1.0f, 0.0f,
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
		}
	}
}