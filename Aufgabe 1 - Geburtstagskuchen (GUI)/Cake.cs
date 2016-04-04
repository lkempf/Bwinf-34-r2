using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

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

		[Newtonsoft.Json.JsonIgnore]
		public readonly Rect Bounds;

		private Path candlePath, heartPath;
		private StreamGeometry heart;
		private Point startPoint, circleIntersectionPoint, circleEndPoint, trianglePoint;

		[Newtonsoft.Json.JsonIgnore]
		public int NearestCandle = -1;
		private float minValue;
		private List<float> distanceToClosestNeighbor;

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
			Vector angledVector = new Vector(-sinAngle, cosAngle);
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
			if(target.Children.Count == 2 && target.Children[1] is Path && target.Children[1] == candlePath)
			{
				RedrawCandles();
				return;
			}

			target.Children.Clear();

			heartPath = new Path();
			heartPath.Data = heart;
			heartPath.Stroke = new SolidColorBrush(Colors.CornflowerBlue);
			heartPath.StrokeThickness = 1;
			heartPath.Fill = new SolidColorBrush(Colors.CornflowerBlue);
			target.Children.Add(heartPath);

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
			distanceToClosestNeighbor = new List<float>(Candles.Count);
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
					float dist = (float)Math.Sqrt(Math.Pow(Candles[i].X - Candles[j].X, 2) + Math.Pow(Candles[i].Y - Candles[j].Y, 2));
					if (dist < distanceToClosestNeighbor[i])
					{
						distanceToClosestNeighbor[i] = dist;
					}
				}
			}

			minValue = float.PositiveInfinity;
			for (int i = 0; i < distanceToClosestNeighbor.Count; i++)
			{
				if (distanceToClosestNeighbor[i] < minValue)
				{
					minValue = distanceToClosestNeighbor[i];
					NearestCandle = i;
				}
			}

			float average = 0;
			distanceToClosestNeighbor.ForEach(d => average += d);
			average /= distanceToClosestNeighbor.Count;

			float deviation = 0;
			distanceToClosestNeighbor.ForEach(d => deviation += (float)Math.Abs(average - d));
			deviation /= distanceToClosestNeighbor.Count;

			return average - deviation;
		}

		//public float CalculateScorePartial(int i)
		//{
		//	if (distanceToClosestNeighbor == null)
		//		return CalculateScore();

		//	distanceToClosestNeighbor[i] = float.PositiveInfinity;
		//	for (int j = 0; j < Candles.Count; j++)
		//	{
		//		if (i == j)
		//			continue;
		//		float dist = (float)Math.Sqrt(Math.Pow(Candles[i].X - Candles[j].X, 2) + Math.Pow(Candles[i].Y - Candles[j].Y, 2));
		//		if (dist < distanceToClosestNeighbor[i])
		//		{
		//			distanceToClosestNeighbor[i] = dist;
		//			distanceToClosestNeighbor[j] = Math.Min(distanceToClosestNeighbor[j], distanceToClosestNeighbor[i]);
		//		}
		//	}

		//	distanceToClosestNeighbor[closestNeighbor[i]] = float.PositiveInfinity;
		//	for (int j = 0; j < Candles.Count; j++)
		//	{
		//		if (closestNeighbor[i] == j)
		//			continue;
		//		float dist = (float)Math.Sqrt(Math.Pow(Candles[closestNeighbor[i]].X - Candles[j].X, 2) + Math.Pow(Candles[closestNeighbor[i]].Y - Candles[j].Y, 2));
		//		if (dist < distanceToClosestNeighbor[closestNeighbor[i]])
		//		{
		//			distanceToClosestNeighbor[closestNeighbor[i]] = dist;
		//			distanceToClosestNeighbor[j] = Math.Min(distanceToClosestNeighbor[j], distanceToClosestNeighbor[closestNeighbor[i]]);
		//		}
		//	}

		//	minValue = float.PositiveInfinity;
		//	maxValue = float.NegativeInfinity;
		//	for (int j = 0; j < distanceToClosestNeighbor.Count; j++)
		//	{
		//		if (distanceToClosestNeighbor[j] < minValue)
		//		{
		//			minValue = distanceToClosestNeighbor[j];
		//			NearestCandle = j;
		//		}
		//		if (distanceToClosestNeighbor[j] > maxValue)
		//		{
		//			maxValue = distanceToClosestNeighbor[j];
		//			FarestCandle = j;
		//		}
		//	}

		//	float average = 0;
		//	distanceToClosestNeighbor.ForEach(d => average += d);
		//	average /= distanceToClosestNeighbor.Count;

		//	return average;
		//}

		public Cake Clone()
		{
			var cake = new Cake(Size, Angle);
			cake.Candles.Capacity = Candles.Count;
			foreach (var candle in Candles)
				cake.Candles.Add(candle);
			return cake;
		}
	}

	class CakeGenerator : IDisposable
	{
		public readonly int NumberOfCandles, DegreeOfParallelization;
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

		public CakeGenerator(int numberOfCandles, int degreeOfParallelization, int size, float angle)
		{
			NumberOfCandles = numberOfCandles;
			DegreeOfParallelization = degreeOfParallelization;
			cake = new Cake(size, angle);

			threads = new Thread[DegreeOfParallelization];
			internalCakes = new Cake[DegreeOfParallelization];

			for (int i = 0; i < DegreeOfParallelization; i++)
				internalCakes[i] = cake.Clone();
		}

		private Thread[] threads;
		private Cake[] internalCakes;
		private Action redrawCallback;
		public async void Optimize(int iterations, CancellationToken cancellationToken, Action endedCallback, Action redrawCallback = null)
		{
			this.redrawCallback = redrawCallback;

			//0 heißt "endlos" wiederholen
			if (iterations == 0)
				iterations = int.MaxValue;

			if (globalIterations == 0)
			{
				Random random = new Random();
				for (int thread = 0; thread < DegreeOfParallelization; thread++)
				{
					var cake = internalCakes[thread];
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
			}
			else
			{
				for (int i = 0; i < DegreeOfParallelization; i++)
					if (bestScore * 0.9 > internalCakes[i].CalculateScore())
						internalCakes[i] = Cake.Clone();
			}

			await Task.Run(() =>
			{
				for (int i = 0; i < DegreeOfParallelization; i++)
				{
					threads[i] = new Thread(OptimizeInternal);
					threads[i].Name = "Evolution worker " + i;
					threads[i].Priority = ThreadPriority.BelowNormal;
					threads[i].Start(new Tuple<int, int, CancellationToken>(i, iterations, cancellationToken));
				}
				for (int i = 0; i < DegreeOfParallelization; i++)
				{
					threads[i].Join();
				}
			});
			Application.Current.Dispatcher.Invoke(endedCallback);
		}

		private void OptimizeInternal(object args)
		{
			var argsTupel = (Tuple<int, int, CancellationToken>)args;
			int threadId = argsTupel.Item1;
			int iterations = argsTupel.Item2;
			var cancellationToken = argsTupel.Item3;

			var random = new Random();
			var cake = internalCakes[threadId];
			float lastScore = cake.CalculateScore();
			for (int i = 0; i < iterations; i++)
			{
				if (threadId == 0)
					Interlocked.Increment(ref globalIterations);
				int randomCandle = random.NextDouble() >= 0.25 ? cake.NearestCandle : random.Next(NumberOfCandles);
				int newX, newY;
				int oldX = cake.Candles[randomCandle].X, oldY = cake.Candles[randomCandle].Y;
				int currentTries = 0;
				while (true)
				{
					i++;
					float cooldown = (float)Math.Ceiling(globalIterations / 5000d);

					if (i % 5000 == 0 || threadId == 0)
						Application.Current.Dispatcher.InvokeAsync(redrawCallback);

					//Evolutionen die nicht erfolgsversprechend sind zerstören
					if (i != 0 && i % 10000 == 0)
					{
						if (bestScore * 0.9 > lastScore)
						{
							internalCakes[threadId] = cake = Cake.Clone();
							lastScore = bestScore;
						}
					}

					//Wenn eine Kerze nicht mehr weiter optimiert werden kann, dann wird eine andere genommen
					currentTries++;
					if (currentTries == 1000/* * cooldown*/)
					{
						currentTries = 0;
						var tmp = cake.Candles[randomCandle];
						tmp.X = oldX; tmp.Y = oldY;
						cake.Candles[randomCandle] = tmp;
						randomCandle = random.Next(NumberOfCandles);
						oldX = cake.Candles[randomCandle].X;
						oldY = cake.Candles[randomCandle].Y;
					}

					newX = oldX + (int)(Math.Max(((cake.Size * 2) / cooldown) * random.NextDouble(), 1)
						* (random.NextDouble() >= 0.5 ? 1 : -1));
					newY = oldY + (int)(Math.Max(((cake.Bounds.Height / 2) / cooldown) * random.NextDouble(), 1)
						* (random.NextDouble() >= 0.5 ? 1 : -1));

					if (!cake.Contains(newX, newY))
					{
						i--;
						continue;
					}

					var tmp2 = cake.Candles[randomCandle];
					tmp2.X = newX; tmp2.Y = newY;
					cake.Candles[randomCandle] = tmp2;

					if (cancellationToken.IsCancellationRequested || i >= iterations)
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
						//this.cake.TransferUIControl(cake);
						this.cake = cake;
					}
				}
			});
		}

		public void Dispose()
		{
			for(int i = 0; i < DegreeOfParallelization; i++)
			{
				threads[i].Abort();
			}
		}
	}
}
