﻿using SadConsole;
using SadRogue.Primitives;
using System.Numerics;
using SFML.Graphics;
using Color = SadRogue.Primitives.Color;
using System;
using Console = SadConsole.Console;
using System.Collections.Generic;

namespace SMPL
{
	internal class Triangle
	{
		public readonly Vertex[] vertsLocal;
		public readonly Vertex[] vertsGlobal = new Vertex[3];
		public readonly Vertex[] vertsCamera = new Vertex[3];

		public Image Image { get; set; }
		private float normalZ;

		public Triangle(Vertex p1, Vertex p2, Vertex p3, Image image, float normalZ = 0)
		{
			vertsLocal = new Vertex[3] { p1, p2, p3 };
			vertsGlobal = new Vertex[3] { p1, p2, p3 };
			vertsCamera = new Vertex[3] { p1, p2, p3 };
			Image = image;
			this.normalZ = normalZ;
		}

		public void UpdatePoints(Area area, Console console, Camera camera)
		{
			for (int i = 0; i < 3; i++)
			{
				vertsGlobal[i].Position = vertsLocal[i].Position * area.Scale;
				vertsGlobal[i].Position = vertsGlobal[i].Position.Rotate(area.Rotation);
				vertsGlobal[i].Position = vertsGlobal[i].Position.Translate(area.Position);

				vertsCamera[i].Position = vertsGlobal[i].Position.Translate(-camera.Area.Position);
				vertsCamera[i].Position = vertsCamera[i].Position.Rotate(-camera.Area.Rotation);
				vertsCamera[i].Position = vertsCamera[i].Position.ApplyPerspective(80, 45);
				vertsCamera[i].Position = vertsCamera[i].Position.CenterScreen(new(console.Width, console.Height));

				vertsCamera[i].TexCoords = vertsLocal[i].TexCoords.FixAffineCoordinates(vertsGlobal[i].Position.Z, 80, 45);
			}

			var p0 = vertsCamera[0].Position;
			var p1 = vertsCamera[1].Position;
			var p2 = vertsCamera[2].Position;
			normalZ = (p1.X - p0.X) * (p2.Y - p0.Y) - (p1.Y - p0.Y) * (p2.X - p0.X);
		}
		public void Draw(Console console, Image image, bool cull)
		{
			if (cull && normalZ >= 0)
				return;

			var aux = new Vertex();
			if (vertsCamera[0].Position.Y > vertsCamera[1].Position.Y)
			{
				aux = vertsCamera[0];
				vertsCamera[0] = vertsCamera[1];
				vertsCamera[1] = aux;
			}
			if (vertsCamera[0].Position.Y > vertsCamera[2].Position.Y)
			{
				aux = vertsCamera[0];
				vertsCamera[0] = vertsCamera[2];
				vertsCamera[2] = aux;
			}
			if (vertsCamera[1].Position.Y > vertsCamera[2].Position.Y)
			{
				aux = vertsCamera[1];
				vertsCamera[1] = vertsCamera[2];
				vertsCamera[2] = aux;
			}

			var p0x = (int)vertsCamera[0].Position.X;
			var p0y = (int)vertsCamera[0].Position.Y;
			var p1x = (int)vertsCamera[1].Position.X;
			var p1y = (int)vertsCamera[1].Position.Y;
			var p2x = (int)vertsCamera[2].Position.X;
			var p2y = (int)vertsCamera[2].Position.Y;

			var texWidth = (int)image.Size.X;
			var texHeight = (int)image.Size.Y;

			if (p0y < p1y)
			{
				var slope1 = ((float)p1x - p0x) / (p1y - p0y);
				var slope2 = ((float)p2x - p0x) / (p2y - p0y);
				for (int i = 0; i <= p1y - p0y; i++)
				{
					var x1 = (int)(p0x + i * slope1);
					var x2 = (int)(p0x + i * slope2);
					var y = p0y + i;

					double us = vertsCamera[0].TexCoords.X + ((float)y - p0y) / (p1y - p0y) * (vertsCamera[1].TexCoords.X - vertsCamera[0].TexCoords.X);
					double vs = vertsCamera[0].TexCoords.Y + ((float)y - p0y) / (p1y - p0y) * (vertsCamera[1].TexCoords.Y - vertsCamera[0].TexCoords.Y);
					double ws = vertsCamera[0].TexCoords.Z + ((float)y - p0y) / (p1y - p0y) * (vertsCamera[1].TexCoords.Z - vertsCamera[0].TexCoords.Z);
					double ue = vertsCamera[0].TexCoords.X + ((float)y - p0y) / (p2y - p0y) * (vertsCamera[2].TexCoords.X - vertsCamera[0].TexCoords.X);
					double ve = vertsCamera[0].TexCoords.Y + ((float)y - p0y) / (p2y - p0y) * (vertsCamera[2].TexCoords.Y - vertsCamera[0].TexCoords.Y);
					double we = vertsCamera[0].TexCoords.Z + ((float)y - p0y) / (p2y - p0y) * (vertsCamera[2].TexCoords.Z - vertsCamera[0].TexCoords.Z);
					double zs = vertsCamera[0].Position.Z + ((float)y - p0y) / (p1y - p0y) * (vertsCamera[1].Position.Z - vertsCamera[0].Position.Z);
					double ze = vertsCamera[0].Position.Z + ((float)y - p0y) / (p2y - p0y) * (vertsCamera[2].Position.Z - vertsCamera[0].Position.Z);

					if (x1 > x2)
					{
						var swap = x1;
						x1 = x2;
						x2 = swap;
						var aux2 = us;
						us = ue;
						ue = aux2;
						aux2 = vs;
						vs = ve;
						ve = aux2;
						aux2 = ws;
						ws = we;
						we = aux2;
						aux2 = zs;
						zs = ze;
						ze = aux2;
					}
					if (x2 > x1)
					{
						var u = us * texWidth;
						var ustep = (ue - us) / (x2 - x1) * texWidth;
						var v = vs * texHeight;
						var vstep = (ve - vs) / (x2 - x1) * texHeight;
						var w = ws;
						var wstep = (we - ws) / (x2 - x1);
						var z = zs;
						var zstep = (ze - zs) / (x2 - x1);
						for (int x = x1; x <= x2; x++)
						{
							u += ustep;
							v += vstep;
							w += wstep;
							z += zstep;

							var tu = Math.Clamp(u / w, 0, texWidth - 1);
							var tv = Math.Clamp(v / w, 0, texHeight - 1);
							var c = image.GetPixel((uint)tu, (uint)tv);
							console.DrawLine(new Point(x, y), new Point(x, y), null, background: new Color(c.R, c.G, c.B, c.A));
						}
					}
				}
			}
			if (p1y < p2y)
			{
				var slope1 = ((float)p2x - p1x) / (p2y - p1y);
				var slope2 = ((float)p2x - p0x) / (p2y - p0y);
				var sx = p2x - (p2y - p1y) * slope2;
				for (int i = 0; i <= p2y - p1y; i++)
				{
					var x1 = (int)(p1x + i * slope1);
					var x2 = (int)(sx + i * slope2);
					var y = p1y + i;

					var us = vertsCamera[1].TexCoords.X + ((float)y - p1y) / (p2y - p1y) * (vertsCamera[2].TexCoords.X - vertsCamera[1].TexCoords.X);
					var vs = vertsCamera[1].TexCoords.Y + ((float)y - p1y) / (p2y - p1y) * (vertsCamera[2].TexCoords.Y - vertsCamera[1].TexCoords.Y);
					var ws = vertsCamera[1].TexCoords.Z + ((float)y - p1y) / (p2y - p1y) * (vertsCamera[2].TexCoords.Z - vertsCamera[1].TexCoords.Z);
					var ue = vertsCamera[0].TexCoords.X + ((float)y - p0y) / (p2y - p0y) * (vertsCamera[2].TexCoords.X - vertsCamera[0].TexCoords.X);
					var ve = vertsCamera[0].TexCoords.Y + ((float)y - p0y) / (p2y - p0y) * (vertsCamera[2].TexCoords.Y - vertsCamera[0].TexCoords.Y);
					var we = vertsCamera[0].TexCoords.Z + ((float)y - p0y) / (p2y - p0y) * (vertsCamera[2].TexCoords.Z - vertsCamera[0].TexCoords.Z);
					var zs = vertsCamera[1].Position.Z + ((float)y - p1y) / (p2y - p1y) * (vertsCamera[2].Position.Z - vertsCamera[1].Position.Z);
					var ze = vertsCamera[0].Position.Z + ((float)y - p0y) / (p2y - p0y) * (vertsCamera[2].Position.Z - vertsCamera[0].Position.Z);

					if (x1 > x2)
					{
						var swap = x1;
						x1 = x2;
						x2 = swap;
						var aux2 = us;
						us = ue;
						ue = aux2;
						aux2 = vs;
						vs = ve;
						ve = aux2;
						aux2 = ws;
						ws = we;
						we = aux2;
						aux2 = zs;
						zs = ze;
						ze = aux2;
					}
					if (x2 > x1)
					{
						var u = us * texWidth;
						var ustep = (ue - us) / (x2 - x1) * texWidth;
						var v = vs * texHeight;
						var vstep = (ve - vs) / (x2 - x1) * texHeight;
						var w = ws;
						var wstep = (we - ws) / (x2 - x1);
						var z = zs;
						var zstep = (ze - zs) / (x2 - x1);
						for (int x = x1; x <= x2; x++)
						{
							u += ustep;
							v += vstep;
							w += wstep;
							z += zstep;

							var tu = Math.Clamp(u / w, 0, texWidth - 1);
							var tv = Math.Clamp(v / w, 0, texHeight - 1);
							var c = image.GetPixel((uint)tu, (uint)tv);
							console.DrawLine(new Point(x, y), new Point(x, y), null, background: new Color(c.R, c.G, c.B, c.A));
						}
					}
				}
			}
		}

		public Triangle[] GetClippedTriangles(Console console)
		{
			var result = new Stack<Triangle>();
			result.Push(new(vertsCamera[0], vertsCamera[1], vertsCamera[2], Image, normalZ));

			var _in = new List<Vertex>();
			var _out = new List<Vertex>();

			// sides of screen V

			// left
			for (int i = 0; i < result.Count; i++)
			{
				var currTriangle = result.Pop();
				_in.Clear();
				_out.Clear();
				for (int j = 0; j < 3; j++)
				{
					var vert = currTriangle.vertsCamera[j];
					if (vert.Position.X < 0)
						_out.Add(vert);
					else
						_in.Add(vert);
				}

				if (_out.Count == 0)
					result.Push(new(_in[0], _in[1], _in[2], Image, normalZ));
				else if (_out.Count == 1)
				{
					var extraPoint1 = new Vertex
					{
						Position = new(
							0,
							_out[0].Position.Y + (0 - _out[0].Position.X) * (_in[0].Position.Y - _out[0].Position.Y) / (_in[0].Position.X - _out[0].Position.X),
							_out[0].Position.Z + (0 - _out[0].Position.X) * (_in[0].Position.Z - _out[0].Position.Z) / (_in[0].Position.X - _out[0].Position.X)),
						TexCoords = new(
							_out[0].TexCoords.X + (0 - _out[0].Position.X) * (_in[0].TexCoords.X - _out[0].TexCoords.X) / (_in[0].Position.X - _out[0].Position.X),
							_out[0].TexCoords.Y + (0 - _out[0].Position.X) * (_in[0].TexCoords.Y - _out[0].TexCoords.Y) / (_in[0].Position.X - _out[0].Position.X),
							_out[0].TexCoords.Z + (0 - _out[0].Position.X) * (_in[0].TexCoords.Z - _out[0].TexCoords.Z) / (_in[0].Position.X - _out[0].Position.X))
					};
					var extraPoint2 = new Vertex
					{
						Position = new(
							0,
							_out[0].Position.Y + (0 - _out[0].Position.X) * (_in[1].Position.Y - _out[0].Position.Y) / (_in[1].Position.X - _out[0].Position.X),
							_out[0].Position.Z + (0 - _out[0].Position.X) * (_in[1].Position.Z - _out[0].Position.Z) / (_in[1].Position.X - _out[0].Position.X)),
						TexCoords = new(
							_out[0].TexCoords.X + (0 - _out[0].Position.X) * (_in[1].TexCoords.X - _out[0].TexCoords.X) / (_in[1].Position.X - _out[0].Position.X),
							_out[0].TexCoords.Y + (0 - _out[0].Position.X) * (_in[1].TexCoords.Y - _out[0].TexCoords.Y) / (_in[1].Position.X - _out[0].Position.X),
							_out[0].TexCoords.Z + (0 - _out[0].Position.X) * (_in[1].TexCoords.Z - _out[0].TexCoords.Z) / (_in[1].Position.X - _out[0].Position.X))
					};

					result.Push(new(extraPoint1, _in[0], _in[1], Image, normalZ));
					result.Push(new(extraPoint2, extraPoint1, _in[1], Image, normalZ));
				}
				else if (_out.Count == 2)
				{
					var extraPoint1 = new Vertex
					{
						Position = new(
							0,
							_out[0].Position.Y + (0 - _out[0].Position.X) * (_in[0].Position.Y - _out[0].Position.Y) / (_in[0].Position.X - _out[0].Position.X),
							_out[0].Position.Z + (0 - _out[0].Position.X) * (_in[0].Position.Z - _out[0].Position.Z) / (_in[0].Position.X - _out[0].Position.X)),
						TexCoords = new(
						_out[0].TexCoords.X + (0 - _out[0].Position.X) * (_in[0].TexCoords.X - _out[0].TexCoords.X) / (_in[0].Position.X - _out[0].Position.X),
						_out[0].TexCoords.Y + (0 - _out[0].Position.X) * (_in[0].TexCoords.Y - _out[0].TexCoords.Y) / (_in[0].Position.X - _out[0].Position.X),
						_out[0].TexCoords.Z + (0 - _out[0].Position.X) * (_in[0].TexCoords.Z - _out[0].TexCoords.Z) / (_in[0].Position.X - _out[0].Position.X))
					};
					var extraPoint2 = new Vertex
					{
						Position = new(
							0,
							_out[1].Position.Y + (0 - _out[1].Position.X) * (_in[0].Position.Y - _out[1].Position.Y) / (_in[0].Position.X - _out[1].Position.X),
							_out[1].Position.Z + (0 - _out[1].Position.X) * (_in[0].Position.Z - _out[1].Position.Z) / (_in[0].Position.X - _out[1].Position.X)),
						TexCoords = new(
							_out[1].TexCoords.X + (0 - _out[1].Position.X) * (_in[0].TexCoords.X - _out[1].TexCoords.X) / (_in[0].Position.X - _out[1].Position.X),
							_out[1].TexCoords.Y + (0 - _out[1].Position.X) * (_in[0].TexCoords.Y - _out[1].TexCoords.Y) / (_in[0].Position.X - _out[1].Position.X),
							_out[1].TexCoords.Z + (0 - _out[1].Position.X) * (_in[0].TexCoords.Z - _out[1].TexCoords.Z) / (_in[0].Position.X - _out[1].Position.X))
					};

					result.Push(new(extraPoint1, extraPoint2, _in[0], Image, normalZ));
				}
			}
			// right
			for (int i = 0; i < result.Count; i++)
			{
				var currentTriangle = result.Pop();
				_in.Clear();
				_out.Clear();
				for (int j = 0; j < 3; j++)
				{
					var vert = currentTriangle.vertsCamera[j];
					if (vert.Position.X >= console.Width)
						_out.Add(vert);
					else
						_in.Add(vert);
				}

				var w = console.Width - 1;
				if (_out.Count == 0)
					result.Push(new(_in[0], _in[1], _in[2], Image, normalZ));
				else if (_out.Count == 1)
				{
					var extraPoint1 = new Vertex
					{
						Position = new(
							w,
							_out[0].Position.Y + (w - _out[0].Position.X) * (_in[0].Position.Y - _out[0].Position.Y) / (_in[0].Position.X - _out[0].Position.X),
							_out[0].Position.Z + (w - _out[0].Position.X) * (_in[0].Position.Z - _out[0].Position.Z) / (_in[0].Position.X - _out[0].Position.X)),
						TexCoords = new(
							_out[0].TexCoords.X + (w - _out[0].Position.X) * (_in[0].TexCoords.X - _out[0].TexCoords.X) / (_in[0].Position.X - _out[0].Position.X),
							_out[0].TexCoords.Y + (w - _out[0].Position.X) * (_in[0].TexCoords.Y - _out[0].TexCoords.Y) / (_in[0].Position.X - _out[0].Position.X),
							_out[0].TexCoords.Z + (w - _out[0].Position.X) * (_in[0].TexCoords.Z - _out[0].TexCoords.Z) / (_in[0].Position.X - _out[0].Position.X))
					};

					var extraPoint2 = new Vertex
					{
						Position = new(
							w,
							_out[0].Position.Y + (w - _out[0].Position.X) * (_in[1].Position.Y - _out[0].Position.Y) / (_in[1].Position.X - _out[0].Position.X),
							_out[0].Position.Z + (w - _out[0].Position.X) * (_in[1].Position.Z - _out[0].Position.Z) / (_in[1].Position.X - _out[0].Position.X)),
						TexCoords = new(
							_out[0].TexCoords.X + (w - _out[0].Position.X) * (_in[1].TexCoords.X - _out[0].TexCoords.X) / (_in[1].Position.X - _out[0].Position.X),
							_out[0].TexCoords.Y + (w - _out[0].Position.X) * (_in[1].TexCoords.Y - _out[0].TexCoords.Y) / (_in[1].Position.X - _out[0].Position.X),
							_out[0].TexCoords.Z + (w - _out[0].Position.X) * (_in[1].TexCoords.Z - _out[0].TexCoords.Z) / (_in[1].Position.X - _out[0].Position.X))
					};

					result.Push(new(extraPoint1, _in[0], _in[1], Image, normalZ));
					result.Push(new(extraPoint2, extraPoint1, _in[1], Image, normalZ));
				}
				else if (_out.Count == 2)
				{
					var extraPoint1 = new Vertex
					{
						Position = new(
						w,
						_out[0].Position.Y + (w - _out[0].Position.X) * (_in[0].Position.Y - _out[0].Position.Y) / (_in[0].Position.X - _out[0].Position.X),
						_out[0].Position.Z + (w - _out[0].Position.X) * (_in[0].Position.Z - _out[0].Position.Z) / (_in[0].Position.X - _out[0].Position.X)),
						TexCoords = new(
						_out[0].TexCoords.X + (w - _out[0].Position.X) * (_in[0].TexCoords.X - _out[0].TexCoords.X) / (_in[0].Position.X - _out[0].Position.X),
						_out[0].TexCoords.Y + (w - _out[0].Position.X) * (_in[0].TexCoords.Y - _out[0].TexCoords.Y) / (_in[0].Position.X - _out[0].Position.X),
						_out[0].TexCoords.Z + (w - _out[0].Position.X) * (_in[0].TexCoords.Z - _out[0].TexCoords.Z) / (_in[0].Position.X - _out[0].Position.X))
					};

					var extraPoint2 = new Vertex
					{
						Position = new(
							w,
							_out[1].Position.Y + (w - _out[1].Position.X) * (_in[0].Position.Y - _out[1].Position.Y) / (_in[0].Position.X - _out[1].Position.X),
							_out[1].Position.Z + (w - _out[1].Position.X) * (_in[0].Position.Z - _out[1].Position.Z) / (_in[0].Position.X - _out[1].Position.X)),
						TexCoords = new(
							_out[1].TexCoords.X + (w - _out[1].Position.X) * (_in[0].TexCoords.X - _out[1].TexCoords.X) / (_in[0].Position.X - _out[1].Position.X),
							_out[1].TexCoords.Y + (w - _out[1].Position.X) * (_in[0].TexCoords.Y - _out[1].TexCoords.Y) / (_in[0].Position.X - _out[1].Position.X),
							_out[1].TexCoords.Z + (w - _out[1].Position.X) * (_in[0].TexCoords.Z - _out[1].TexCoords.Z) / (_in[0].Position.X - _out[1].Position.X))
					};

					result.Push(new(extraPoint1, extraPoint2, _in[0], Image, normalZ));
				}
			}
			// top
			for (int i = 0; i < result.Count; i++)
			{
				var currentTriangle = result.Pop();
				_in.Clear();
				_out.Clear();
				for (int j = 0; j < 3; j++)
				{
					var vert = currentTriangle.vertsCamera[j];
					if (vert.Position.Y < 0)
						_out.Add(vert);
					else
						_in.Add(vert);
				}
				if (_out.Count == 0)
					result.Push(new(_in[0], _in[1], _in[2], Image, normalZ));

				else if (_out.Count == 1)
				{
					var extraPoint1 = new Vertex
					{
						Position = new(
						_out[0].Position.X - _out[0].Position.Y * (_in[0].Position.X - _out[0].Position.X) / (_in[0].Position.Y - _out[0].Position.Y),
						0,
						_out[0].Position.Z - _out[0].Position.Y * (_in[0].Position.Z - _out[0].Position.Z) / (_in[0].Position.Y - _out[0].Position.Y)),
						TexCoords = new(
						_out[0].TexCoords.X - _out[0].Position.Y * (_in[0].TexCoords.X - _out[0].TexCoords.X) / (_in[0].Position.Y - _out[0].Position.Y),
						_out[0].TexCoords.Y - _out[0].Position.Y * (_in[0].TexCoords.Y - _out[0].TexCoords.Y) / (_in[0].Position.Y - _out[0].Position.Y),
						_out[0].TexCoords.Z - _out[0].Position.Y * (_in[0].TexCoords.Z - _out[0].TexCoords.Z) / (_in[0].Position.Y - _out[0].Position.Y))
					};

					var extraPoint2 = new Vertex
					{
						Position = new(
						_out[0].Position.X - _out[0].Position.Y * (_in[1].Position.X - _out[0].Position.X) / (_in[1].Position.Y - _out[0].Position.Y),
						0,
						_out[0].Position.Z - _out[0].Position.Y * (_in[1].Position.Z - _out[0].Position.Z) / (_in[1].Position.Y - _out[0].Position.Y)),
						TexCoords = new(
						_out[0].TexCoords.X - _out[0].Position.Y * (_in[1].TexCoords.X - _out[0].TexCoords.X) / (_in[1].Position.Y - _out[0].Position.Y),
						_out[0].TexCoords.Y - _out[0].Position.Y * (_in[1].TexCoords.Y - _out[0].TexCoords.Y) / (_in[1].Position.Y - _out[0].Position.Y),
						_out[0].TexCoords.Z - _out[0].Position.Y * (_in[1].TexCoords.Z - _out[0].TexCoords.Z) / (_in[1].Position.Y - _out[0].Position.Y))
					};

					result.Push(new(extraPoint1, _in[0], _in[1], Image, normalZ));
					result.Push(new(extraPoint2, extraPoint1, _in[1], Image, normalZ));
				}
				else if (_out.Count == 2)
				{
					var extraPoint1 = new Vertex
					{
						Position = new(
						_out[0].Position.X - _out[0].Position.Y * (_in[0].Position.X - _out[0].Position.X) / (_in[0].Position.Y - _out[0].Position.Y),
						0,
						_out[0].Position.Z - _out[0].Position.Y * (_in[0].Position.Z - _out[0].Position.Z) / (_in[0].Position.Y - _out[0].Position.Y)),
						TexCoords = new(
						_out[0].TexCoords.X - _out[0].Position.Y * (_in[0].TexCoords.X - _out[0].TexCoords.X) / (_in[0].Position.Y - _out[0].Position.Y),
						_out[0].TexCoords.Y - _out[0].Position.Y * (_in[0].TexCoords.Y - _out[0].TexCoords.Y) / (_in[0].Position.Y - _out[0].Position.Y),
						_out[0].TexCoords.Z - _out[0].Position.Y * (_in[0].TexCoords.Z - _out[0].TexCoords.Z) / (_in[0].Position.Y - _out[0].Position.Y))
					};

					var extraPoint2 = new Vertex
					{
						Position = new(
						_out[1].Position.X - _out[1].Position.Y * (_in[0].Position.X - _out[1].Position.X) / (_in[0].Position.Y - _out[1].Position.Y),
						0,
						_out[1].Position.Z - _out[1].Position.Y * (_in[0].Position.Z - _out[1].Position.Z) / (_in[0].Position.Y - _out[1].Position.Y)),
						TexCoords = new(
						_out[1].TexCoords.X - _out[1].Position.Y * (_in[0].TexCoords.X - _out[1].TexCoords.X) / (_in[0].Position.Y - _out[1].Position.Y),
						_out[1].TexCoords.Y - _out[1].Position.Y * (_in[0].TexCoords.Y - _out[1].TexCoords.Y) / (_in[0].Position.Y - _out[1].Position.Y),
						_out[1].TexCoords.Z - _out[1].Position.Y * (_in[0].TexCoords.Z - _out[1].TexCoords.Z) / (_in[0].Position.Y - _out[1].Position.Y))
					};
					result.Push(new(extraPoint1, extraPoint2, _in[0], Image, normalZ));
				}
			}
			// bottom
			for (int i = 0; i < result.Count; i++)
			{
				var currentTriangle = result.Pop();
				_in.Clear();
				_out.Clear();
				for (int j = 0; j < 3; j++)
				{
					var vert = currentTriangle.vertsCamera[j];
					if (vert.Position.Y >= console.Height)
						_out.Add(vert);
					else
						_in.Add(vert);
				}
				var h = console.Height - 1;
				if (_out.Count == 0)
					result.Push(new(_in[0], _in[1], _in[2], Image, normalZ));
				else if (_out.Count == 1)
				{
					var extraPoint1 = new Vertex
					{
						Position = new(
							_out[0].Position.X + (h - _out[0].Position.Y) * (_in[0].Position.X - _out[0].Position.X) / (_in[0].Position.Y - _out[0].Position.Y),
							h,
							_out[0].Position.Z + (h - _out[0].Position.Y) * (_in[0].Position.Z - _out[0].Position.Z) / (_in[0].Position.Y - _out[0].Position.Y)),
						TexCoords = new(
							_out[0].TexCoords.X + (h - _out[0].Position.Y) * (_in[0].TexCoords.X - _out[0].TexCoords.X) / (_in[0].Position.Y - _out[0].Position.Y),
							_out[0].TexCoords.Y + (h - _out[0].Position.Y) * (_in[0].TexCoords.Y - _out[0].TexCoords.Y) / (_in[0].Position.Y - _out[0].Position.Y),
							_out[0].TexCoords.Z + (h - _out[0].Position.Y) * (_in[0].TexCoords.Z - _out[0].TexCoords.Z) / (_in[0].Position.Y - _out[0].Position.Y))
					};

					var extraPoint2 = new Vertex
					{
						Position = new(
							_out[0].Position.X + (h - _out[0].Position.Y) * (_in[1].Position.X - _out[0].Position.X) / (_in[1].Position.Y - _out[0].Position.Y),
							h,
							_out[0].Position.Z + (h - _out[0].Position.Y) * (_in[1].Position.Z - _out[0].Position.Z) / (_in[1].Position.Y - _out[0].Position.Y)),
						TexCoords = new(
							_out[0].TexCoords.X + (h - _out[0].Position.Y) * (_in[1].TexCoords.X - _out[0].TexCoords.X) / (_in[1].Position.Y - _out[0].Position.Y),
							_out[0].TexCoords.Y + (h - _out[0].Position.Y) * (_in[1].TexCoords.Y - _out[0].TexCoords.Y) / (_in[1].Position.Y - _out[0].Position.Y),
							_out[0].TexCoords.Z + (h - _out[0].Position.Y) * (_in[1].TexCoords.Z - _out[0].TexCoords.Z) / (_in[1].Position.Y - _out[0].Position.Y))
					};

					result.Push(new(extraPoint1, _in[0], _in[1], Image, normalZ));
					result.Push(new(extraPoint2, extraPoint1, _in[1], Image, normalZ));
				}
				else if (_out.Count == 2)
				{
					var extraPoint1 = new Vertex
					{
						Position = new(
							_out[0].Position.X + (h - _out[0].Position.Y) * (_in[0].Position.X - _out[0].Position.X) / (_in[0].Position.Y - _out[0].Position.Y),
							h,
							_out[0].Position.Z + (h - _out[0].Position.Y) * (_in[0].Position.Z - _out[0].Position.Z) / (_in[0].Position.Y - _out[0].Position.Y)),
						TexCoords = new(
							_out[0].TexCoords.X + (h - _out[0].Position.Y) * (_in[0].TexCoords.X - _out[0].TexCoords.X) / (_in[0].Position.Y - _out[0].Position.Y),
							_out[0].TexCoords.Y + (h - _out[0].Position.Y) * (_in[0].TexCoords.Y - _out[0].TexCoords.Y) / (_in[0].Position.Y - _out[0].Position.Y),
							_out[0].TexCoords.Z + (h - _out[0].Position.Y) * (_in[0].TexCoords.Z - _out[0].TexCoords.Z) / (_in[0].Position.Y - _out[0].Position.Y))
					};

					var extraPoint2 = new Vertex
					{
						Position = new(
							_out[1].Position.X + (h - _out[1].Position.Y) * (_in[0].Position.X - _out[1].Position.X) / (_in[0].Position.Y - _out[1].Position.Y),
							h,
							_out[1].Position.Z + (h - _out[1].Position.Y) * (_in[0].Position.Z - _out[1].Position.Z) / (_in[0].Position.Y - _out[1].Position.Y)),
						TexCoords = new(
							_out[1].TexCoords.X + (h - _out[1].Position.Y) * (_in[0].TexCoords.X - _out[1].TexCoords.X) / (_in[0].Position.Y - _out[1].Position.Y),
							_out[1].TexCoords.Y + (h - _out[1].Position.Y) * (_in[0].TexCoords.Y - _out[1].TexCoords.Y) / (_in[0].Position.Y - _out[1].Position.Y),
							_out[1].TexCoords.Z + (h - _out[1].Position.Y) * (_in[0].TexCoords.Z - _out[1].TexCoords.Z) / (_in[0].Position.Y - _out[1].Position.Y))
					};

					result.Push(new(extraPoint1, extraPoint2, _in[0], Image, normalZ));
				}
			}

			return result.ToArray();
		}
	}
}
