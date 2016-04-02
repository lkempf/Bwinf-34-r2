using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Diagnostics;

namespace Aufgabe_1___Geburtstagskuchen__GUI_
{
	struct Candle
	{
		public int X;
		public int Y;
		public int Color;

		public Candle(int x, int y, int color)
		{
			X = x;
			Y = y;
			Color = color;
		}
	}

	class Cake
	{
		public List<Candle> Candles;
		public readonly int Size;
		public readonly float Angle;
		public readonly Rect Bounds;

		private Path candlePath, heartPath;
		private StreamGeometry heart;
		private Point startPoint, circleIntersectionPoint, circleEndPoint, trianglePoint;

		public Cake(int size, float angle)
		{
			Candles = new List<Candle>();
			Size = size;
			Angle = angle;
			Application.Current.Dispatcher.Invoke(CalculateShape);
			Bounds = new Rect(0, 0, 4 * Size, trianglePoint.Y);
		}

		private void CalculateShape()
		{
			float angle = Angle * (float)(Math.PI / 180);
			float sinAngle = (float)Math.Sin(angle);
			float cosAngle = (float)Math.Cos(angle);
			startPoint = new Point(Size - cosAngle * Size, Size + sinAngle * Size);
			circleIntersectionPoint = new Point(2 * Size, Size);
			circleEndPoint = new Point(Size * 3 + cosAngle * Size, Size + sinAngle * Size);
			Vector angledVector = new Vector(Math.Cos(angle + Math.PI / 2), Math.Sin(angle + Math.PI / 2));
			double lineLength = (2 * Size - (Size * 3 + cosAngle * Size)) / angledVector.X;
			trianglePoint = Point.Add(circleEndPoint, Vector.Multiply(angledVector, lineLength));

			heart = new StreamGeometry();
			using (StreamGeometryContext ctx = heart.Open())
			{
				ctx.BeginFigure(startPoint, true, true);
				ctx.ArcTo(circleIntersectionPoint, new Size(Size, Size), Math.PI + angle, true, SweepDirection.Clockwise, true, false);
				ctx.ArcTo(circleEndPoint, new Size(Size, Size), Math.PI + angle, true, SweepDirection.Clockwise, true, false);
				ctx.LineTo(trianglePoint, true, false);
			}
			heart.Freeze();
		}

		public bool AddCandle(int x, int y, int color, bool renderCandle = true)
		{
			if (Contains(x, y))
			{
				Candles.Add(new Candle(x, y, color));

				if (renderCandle)
				{
					GeometryGroup candleGroup = (GeometryGroup)candlePath.Data;
					candleGroup.Children.Add(new EllipseGeometry(new Point(x, y), 2, 2));
				}

				return true;
			}
			return false;
		}

		public bool Contains(int x, int y)
		{
			return heart.FillContains(new Point(x, y), 1, ToleranceType.Absolute);
		}

		public void Render(ref Canvas target)
		{
			target.Children.Clear();

			heartPath = new Path();
			target.Children.Add(heartPath);
			heartPath.Data = heart;
			heartPath.Stroke = new SolidColorBrush(Colors.CornflowerBlue);
			heartPath.StrokeThickness = 1;
			heartPath.Fill = new SolidColorBrush(Colors.CornflowerBlue);

			candlePath = new Path();
			candlePath.Stroke = new SolidColorBrush(Colors.Red);
			candlePath.Fill = new SolidColorBrush(Colors.Red);
			RedrawCandles();
			target.Children.Add(candlePath);
		}

		private void RedrawCandles()
		{
			GeometryGroup candleGroup = new GeometryGroup();
			foreach (var candle in Candles)
			{
				candleGroup.Children.Add(new EllipseGeometry(new Point(candle.X, candle.Y), 2, 2));
			}
			candlePath.Data = candleGroup;
		}

		public float CalculateScore()
		{
			List<float> distanceToClosestNeighbor = new List<float>(Candles.Count);
			for (int i = 0; i < Candles.Count; i++)
			{
				distanceToClosestNeighbor.Add(float.PositiveInfinity);
			}

			for (int i = 0; i < Candles.Count; i++)
			{
				for (int j = 0; j < Candles.Count; j++)
				{
					if (i == j)
						continue;
					distanceToClosestNeighbor[i] = Math.Min(distanceToClosestNeighbor[i], (float)Math.Sqrt(
						Math.Pow(Candles[i].X - Candles[j].X, 2) + Math.Pow(Candles[i].Y - Candles[j].Y, 2)));
				}
			}

			float average = 0;
			distanceToClosestNeighbor.ForEach(d => average += d);
			average /= distanceToClosestNeighbor.Count;

			float deviation = 0;
			distanceToClosestNeighbor.ForEach(d => deviation += (float)Math.Pow(average - d, 2));
			deviation /= distanceToClosestNeighbor.Count;
			deviation = (float)Math.Sqrt(deviation);

			return average;
		}

		public Cake Clone()
		{
			var cake = new Cake(Size, Angle);
			cake.Candles.Capacity = Candles.Count;
			foreach (var candle in Candles)
				cake.Candles.Add(candle);
			return cake;
		}
	}

	class CakeGenerator
	{
		public readonly int NumberOfCandles;
		private float bestScore = float.NegativeInfinity;
		private Cake cake;
		private int globalIterations;

		public Cake Cake
		{
			get
			{
				lock (cake)
				{
					return cake;
				}
			}
		}

		public CakeGenerator(int numberOfCandles, int size, float angle)
		{
			NumberOfCandles = numberOfCandles;
			cake = new Cake(size, angle);
		}

		public async void Optimize(int iterations, CancellationToken cancellationToken, Action endedCallback)
		{
			//0 heißt "endlos" wiederholen
			if (iterations == 0)
				iterations = int.MaxValue;

			if (globalIterations == 0)
			{
				Random random = new Random();
				for (int i = 0; i < NumberOfCandles; i++)
				{
					int x, y;
					do
					{
						x = random.Next((int)cake.Bounds.X, (int)cake.Bounds.Width);
						y = random.Next((int)cake.Bounds.Y, (int)cake.Bounds.Height);
					}
					while (!cake.Contains(x, y));
					cake.Candles.Add(new Candle(x, y, 0));
				}
			}

			await Task.Run(() =>
			{
				var random = new Random();
				var cake = Cake.Clone();
				float lastScore = cake.CalculateScore();
				for (int i = 0; i < iterations; i++)
				{
					Interlocked.Increment(ref globalIterations);
					int randomCandle = random.Next(NumberOfCandles);
					int newX, newY;
					int oldX = cake.Candles[randomCandle].X, oldY = cake.Candles[randomCandle].Y;
					int currentTries = 0;
					while(true)
					{
						//Wenn eine Kerze nicht mehr weiter optimiert werden kann, dann wird eine andere genommen
						currentTries++;
						if (currentTries == 1000)
						{
							currentTries = 0;
							var tmp = cake.Candles[randomCandle];
							tmp.X = oldX; tmp.Y = oldY;
							cake.Candles[randomCandle] = tmp;
							randomCandle = random.Next(NumberOfCandles);
							oldX = cake.Candles[randomCandle].X;
							oldY = cake.Candles[randomCandle].Y;
						}

						newX = oldX + (int)(cake.Size * random.NextDouble() * (random.NextDouble() >= 0.5 ? 1 : -1));
						newY = oldY + (int)((cake.Bounds.Height / 4) * random.NextDouble() * (random.NextDouble() >= 0.5 ? 1 : -1));

						if (!cake.Contains(newX, newY))
							continue;

						var tmp2 = cake.Candles[randomCandle];
						tmp2.X = newX; tmp2.Y = newY;
						cake.Candles[randomCandle] = tmp2;

						if (cancellationToken.IsCancellationRequested)
							break;

						if (cake.CalculateScore() > lastScore)
							break;
					}

					lastScore = cake.CalculateScore();
					if (cancellationToken.IsCancellationRequested)
					{
						UpdateCake(cake.Clone(), lastScore).GetAwaiter().GetResult();
						break;
					}
					UpdateCake(cake.Clone(), lastScore);
				}
			});
			Application.Current.Dispatcher.Invoke(endedCallback);
		}

		private async Task UpdateCake(Cake cake, float newScore)
		{
			await Task.Run(() =>
			{
				lock (this.cake)
				{
					if (newScore > bestScore)
					{
						bestScore = newScore;
						this.cake = cake;
					}
				}
			});
		}
	}
}
