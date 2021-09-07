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
		public virtual int RenderSelf() {
			return 0;
		}
	}
}