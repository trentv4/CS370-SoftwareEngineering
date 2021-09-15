using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System.IO;

namespace Project.Render {
	public class RenderableNode {
		public List<RenderableNode> children = new List<RenderableNode>();
		public bool Enabled = true;

		/// <summary> Renders this object and all children, and returns the number of GL draw calls issued. </summary>
		public int Render() {
			if (!Enabled) return 0;

			int runningTotal = 0;
			foreach (RenderableNode r in children) {
				runningTotal += r.Render();
			}
			return runningTotal + Convert.ToByte(RenderSelf());
		}

		/// <summary> Overriden method for rendering a subclass. Return the number of draw calls this issues
		/// in order to retain drawcall statistic authenticity. </summary>
		protected virtual int RenderSelf() {
			return 0;
		}
	}

	public class Model : RenderableNode {
		public readonly int ElementBufferArray_ID;
		public readonly int VertexBufferObject_ID;
		public Vector3 Scale { get; private set; } = Vector3.One;
		public Vector3 Rotation { get; private set; } = Vector3.Zero;
		public Vector3 Position { get; private set; } = Vector3.Zero;
		public Matrix4 ModelMatrix { get; private set; } = Matrix4.Identity;

		private readonly int IndexLength;

		private static Model UNIT_RECTANGLE;

		public Model(float[] vertexData, uint[] indices) {
			IndexLength = indices.Length;

			ElementBufferArray_ID = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray_ID);
			GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

			VertexBufferObject_ID = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject_ID);
			GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float), vertexData, BufferUsageHint.StaticDraw);
		}

		public Model(Model template) {
			IndexLength = template.IndexLength;
			ElementBufferArray_ID = template.ElementBufferArray_ID;
			VertexBufferObject_ID = template.VertexBufferObject_ID;
		}

		protected override int RenderSelf() {
			Matrix4 tempModelMatrix = ModelMatrix;
			GL.UniformMatrix4(GL.GetUniformLocation(Renderer.INSTANCE.ForwardProgram.ShaderProgram_ID, "model"),
							  true, ref tempModelMatrix);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferArray_ID);
			GL.BindVertexBuffer(0, VertexBufferObject_ID, (IntPtr)(0 * sizeof(float)), 12 * sizeof(float));
			GL.BindVertexBuffer(1, VertexBufferObject_ID, (IntPtr)(3 * sizeof(float)), 12 * sizeof(float));
			GL.BindVertexBuffer(2, VertexBufferObject_ID, (IntPtr)(6 * sizeof(float)), 12 * sizeof(float));
			GL.BindVertexBuffer(3, VertexBufferObject_ID, (IntPtr)(10 * sizeof(float)), 12 * sizeof(float));
			GL.DrawElements(OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, IndexLength, DrawElementsType.UnsignedInt, 0);

			return 1;
		}

		/// <summary> Deletes buffers in OpenGL. This is automatically done by object garbage collection OR program close.
		/// <br/> !! Warning !! Avoid using this unless you know what you're doing! This can crash! </summary>
		public void Dispose() {
			GL.DeleteBuffer(VertexBufferObject_ID);
			GL.DeleteBuffer(ElementBufferArray_ID);
		}

		/// <summary> Regenerates the model matrix after an update to scale, translation, or rotation. </summary>
		private void UpdateModelMatrix() {
			Matrix4 m = Matrix4.CreateScale(Scale);
			m *= Matrix4.CreateRotationX(Rotation.X * Renderer.RCF);
			m *= Matrix4.CreateRotationY(Rotation.Y * Renderer.RCF);
			m *= Matrix4.CreateRotationZ(Rotation.Z * Renderer.RCF);
			m *= Matrix4.CreateTranslation(Position);
			ModelMatrix = m;
		}

		/// <summary> Chainable method to set the scale of this object. </summary>
		public Model SetScale(Vector3 scale) {
			this.Scale = scale;
			UpdateModelMatrix();
			return this;
		}

		/// <summary> Chainable method to set the scale of this object in all axis. </summary>
		public Model SetScale(float scale) {
			return SetScale(new Vector3(scale, scale, scale));
		}

		/// <summary> Chainable method to set the rotation of this object. </summary>
		public Model SetRotation(Vector3 rotation) {
			this.Rotation = rotation;
			UpdateModelMatrix();
			return this;
		}

		/// <summary> Chainable method to set the position of this object. </summary>
		public Model SetPosition(Vector3 position) {
			this.Position = position;
			UpdateModelMatrix();
			return this;
		}

		public static Model GetUnitRectangle() {
			if (UNIT_RECTANGLE == null) {
				float[] vertices = new float[] {
					-0.5f, -0.5f, 0.0f, 1.0f,1.0f, 0.0f, 1.0f, 1.0f, 0.5f, 1.0f, 0.0f, 0.0f,
					 0.5f, -0.5f, 0.0f, 1.0f,1.0f, 0.0f, 1.0f, 1.0f, 0.5f, 1.0f, 1.0f, 0.0f,
					-0.5f,  0.5f, 0.0f, 1.0f,1.0f, 0.0f, 1.0f, 1.0f, 0.5f, 1.0f, 0.0f, 1.0f,
					 0.5f,  0.5f, 0.0f, 1.0f,1.0f, 0.0f, 1.0f, 1.0f, 0.5f, 1.0f, 1.0f, 1.0f,
				};
				uint[] indices = new uint[] {
					0, 1, 2, 1, 2, 3
				};

				UNIT_RECTANGLE = new Model(vertices, indices);
			}
			return new Model(UNIT_RECTANGLE);
		}

		public static Model GetRoom() {
			List<float> vertices = new List<float>();
			vertices.AddRange(new float[] { 0f, 0f, 0f, 1.0f, 1.0f, 0.0f, 1.0f, 1.0f, 1f, 1.0f, 0.0f, 0.0f, });

			uint density = 360;
			for (int i = 1; i <= density; i++) {
				float angle = Renderer.RCF * i * (360.0f / (float)density);
				vertices.AddRange(new List<float>{
					(float) Math.Cos(angle), (float) Math.Sin(angle), 0f, //XYZ
					((float) Math.Cos(angle) + 1)/2.0f, ((float) Math.Sin(angle) + 1)/2.0f, 1f, // NORMALS
					0.8f, 0.2f, 0.4f, 0.0f, // RGBA
					0.0f, 0.0f, // UV
				});
			}
			for (int i = 1; i <= density; i++) {
				float angle = Renderer.RCF * i * (360.0f / (float)density);
				vertices.AddRange(new List<float>{
					(float) Math.Cos(angle), (float) Math.Sin(angle), 1, //XYZ
					((float) Math.Cos(angle) + 1)/2.0f, ((float) Math.Sin(angle) + 1)/2.0f, 1f, // NORMALS
					1f, 0f, 0f, 0.0f, // RGBA
					0.0f, 0.0f, // UV
				});
			}


			List<uint> indexList = new List<uint>();
			// "Floor"
			for (uint i = 0; i < density; i++) {
				indexList.AddRange(new uint[] { 0, i, i + 1 });
			}
			indexList.AddRange(new uint[] { 0, 1, (uint)density });
			for (uint i = 1; i < density; i++) {
				indexList.AddRange(new uint[] {
					i, i + density, i + density + 1,
					i, i+1, i + density + 1,
				});
			}
			indexList.AddRange(new uint[] {
					1, density, density + 1,
					density + 1, density, density * 2
				});
			// "Walls"


			return new Model(vertices.ToArray(), indexList.ToArray());
		}
	}
}