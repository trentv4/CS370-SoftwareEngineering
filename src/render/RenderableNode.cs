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
		public int ElementBufferArray_ID;
		public int VertexBufferObject_ID;
		public Vector3 Scale { get; private set; } = Vector3.One;
		public Vector3 Rotation { get; private set; } = Vector3.Zero;
		public Vector3 Position { get; private set; } = Vector3.Zero;
		public Texture AlbedoTexture = new Texture("assets/textures/null.png");

		protected int IndexLength;

		private static readonly Dictionary<string, Model> _cachedModels = new Dictionary<string, Model>();

		/// <summary> Creates a Model given data and indices. </summary>
		public Model(float[] vertexData, uint[] indices) {
			IndexLength = indices.Length;

			ElementBufferArray_ID = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray_ID);
			GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

			VertexBufferObject_ID = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject_ID);
			GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float), vertexData, BufferUsageHint.StaticDraw);
		}

		/// <summary> Creates a Model given data and indices, and stores it in cached models. </summary>
		public Model(float[] vertexData, uint[] indices, string modelName) : this(vertexData, indices) {
			_cachedModels.Add(modelName, this);
		}

		protected Model() { }

		private Model(int IndexLength, int ElementBufferArray, int VertexBufferObject) {
			this.IndexLength = IndexLength;
			this.ElementBufferArray_ID = ElementBufferArray;
			this.VertexBufferObject_ID = VertexBufferObject;
		}

		/// <summary> Creates a copy of an existing model that was cached with a name. <br/>Nullable!</summary>
		public static Model GetCachedModel(string modelName) {
			Model cached = _cachedModels.GetValueOrDefault(modelName);
			if (cached != null) {
				return new Model(cached.IndexLength, cached.ElementBufferArray_ID, cached.VertexBufferObject_ID);
			}
			return cached;
		}

		protected override void RenderSelf() {
			Matrix4 modelMatrix = Matrix4.CreateScale(Scale);
			modelMatrix *= Matrix4.CreateRotationX(Rotation.X * Renderer.RCF);
			modelMatrix *= Matrix4.CreateRotationY(Rotation.Y * Renderer.RCF);
			modelMatrix *= Matrix4.CreateRotationZ(Rotation.Z * Renderer.RCF);
			modelMatrix *= Matrix4.CreateTranslation(Position);
			GL.UniformMatrix4(Renderer.INSTANCE.ForwardProgram.UniformModel_ID, true, ref modelMatrix);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray_ID);
			GL.BindVertexBuffer(0, VertexBufferObject_ID, (IntPtr)(0 * sizeof(float)), 12 * sizeof(float));
			GL.BindVertexBuffer(1, VertexBufferObject_ID, (IntPtr)(3 * sizeof(float)), 12 * sizeof(float));
			GL.BindVertexBuffer(2, VertexBufferObject_ID, (IntPtr)(6 * sizeof(float)), 12 * sizeof(float));
			GL.BindVertexBuffer(3, VertexBufferObject_ID, (IntPtr)(10 * sizeof(float)), 12 * sizeof(float));

			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, AlbedoTexture.TextureID);
			GL.Uniform1(Renderer.INSTANCE.ForwardProgram.UniformTextureAlbedo_ID, 0);

			GL.DrawElements(OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, IndexLength, DrawElementsType.UnsignedInt, 0);
		}

		/// <summary> Deletes buffers in OpenGL. This is automatically done by object garbage collection OR program close.
		/// <br/> !! Warning !! Avoid using this unless you know what you're doing! This can crash! </summary>
		public void Dispose() {
			GL.DeleteBuffer(VertexBufferObject_ID);
			GL.DeleteBuffer(ElementBufferArray_ID);
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

		/// <summary> Creates (or loads) a unit rectangle that is then further manipulated with model manipulations. </summary>
		public static Model GetUnitRectangle() {
			Model unitRectangle = GetCachedModel("unit_rectangle");
			if (unitRectangle == null) {
				return new Model(new float[] {
					-0.5f, -0.5f, 0.0f, 1.0f,1.0f, 0.0f, 1.0f, 1.0f, 0.5f, 1.0f, 0.0f, 0.0f,
					 0.5f, -0.5f, 0.0f, 1.0f,1.0f, 0.0f, 1.0f, 1.0f, 0.5f, 1.0f, 1.0f, 0.0f,
					-0.5f,  0.5f, 0.0f, 1.0f,1.0f, 0.0f, 1.0f, 1.0f, 0.5f, 1.0f, 0.0f, 1.0f,
					 0.5f,  0.5f, 0.0f, 1.0f,1.0f, 0.0f, 1.0f, 1.0f, 0.5f, 1.0f, 1.0f, 1.0f,
				}, new uint[] {
					0, 1, 2, 1, 2, 3
				}, "unit_rectangle");
			} else {
				return unitRectangle;
			}
		}

		public static Model GetUnitCircle() {
			Model unitCircle = GetCachedModel("unit_circle");
			if (unitCircle == null) {
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

				float[] vertices = v.ToArray();
				uint[] indices = i.ToArray();
				return new Model(vertices, indices, "unit_circle");
			} else {
				return unitCircle;
			}
		}
	}

	public class InterfaceModel : Model {
		public InterfaceModel(float[] vertices, uint[] indices) : base(vertices, indices) { }

		private static InterfaceModel UNIT_CIRCLE;
		private static InterfaceModel UNIT_RECTANGLE;

		public InterfaceModel(InterfaceModel template) {
			IndexLength = template.IndexLength;
			ElementBufferArray_ID = template.ElementBufferArray_ID;
			VertexBufferObject_ID = template.VertexBufferObject_ID;
		}

		protected override void RenderSelf() {
			Matrix4 modelMatrix = Matrix4.CreateScale(Scale);
			modelMatrix *= Matrix4.CreateRotationX(Rotation.X * Renderer.RCF);
			modelMatrix *= Matrix4.CreateRotationY(Rotation.Y * Renderer.RCF);
			modelMatrix *= Matrix4.CreateRotationZ(Rotation.Z * Renderer.RCF);
			modelMatrix *= Matrix4.CreateTranslation(Position);
			GL.UniformMatrix4(Renderer.INSTANCE.ForwardProgram.UniformModel_ID, true, ref modelMatrix);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray_ID);
			GL.BindVertexBuffer(0, VertexBufferObject_ID, (IntPtr)(0 * sizeof(float)), 5 * sizeof(float));
			GL.BindVertexBuffer(1, VertexBufferObject_ID, (IntPtr)(3 * sizeof(float)), 5 * sizeof(float));

			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, AlbedoTexture.TextureID);
			GL.Uniform1(Renderer.INSTANCE.InterfaceProgram.UniformTextureAlbedo_ID, 0);

			GL.DrawElements(OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, IndexLength, DrawElementsType.UnsignedInt, 0);
		}

		public static new InterfaceModel GetUnitRectangle() {
			if (UNIT_RECTANGLE == null) {
				UNIT_RECTANGLE = new InterfaceModel(new float[] {
					-0.5f, -0.5f, 0.0f, 0.0f, 0.0f,
					 0.5f, -0.5f, 0.0f, 1.0f, 0.0f,
					-0.5f,  0.5f, 0.0f, 0.0f, 1.0f,
					 0.5f,  0.5f, 0.0f, 1.0f, 1.0f,
				}, new uint[] {
					0, 1, 2, 1, 2, 3
				});
			}
			return new InterfaceModel(UNIT_RECTANGLE);
		}

		public static new InterfaceModel GetUnitCircle() {
			if (UNIT_CIRCLE == null) {
				uint density = 180;

				List<float> v = new List<float>();
				v.AddRange(new float[] { 0, 0, 0.5f, 0.5f });
				for (uint g = 0; g < density; g++) {
					float angle = Renderer.RCF * g * (360.0f / (float)density);
					v.AddRange(new float[] {
					(float)Math.Cos(angle), 0, (float)Math.Sin(angle),
					(float)Math.Cos(angle), (float)Math.Sin(angle)});
				}

				List<uint> i = new List<uint>();
				for (uint g = 1; g < density; g++) {
					i.AddRange(new uint[] {
						0, g, g+1
					});
				}
				i.AddRange(new uint[] { 0, density, 1 });

				float[] vertices = v.ToArray();
				uint[] indices = i.ToArray();
				UNIT_CIRCLE = new InterfaceModel(vertices, indices);
			}
			return new InterfaceModel(UNIT_CIRCLE);
		}
	}
}