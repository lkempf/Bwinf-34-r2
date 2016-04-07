using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Linq;

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

		private Path redCandlePath, yellowCandlePath, greenCandlePath, heartPath;
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
					GeometryGroup candleGroup = null;
					switch (color)
					{
						case 0: //Rot
							candleGroup = (GeometryGroup)redCandlePath.Data;
							break;
						case 1: //Gelb
							candleGroup = (GeometryGroup)yellowCandlePath.Data;
							break;
						case 2: //Grün
							candleGroup = (GeometryGroup)greenCandlePath.Data;
							break;
					}
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
			if (target.Children.Count == 2 && target.Children[1] is Path &&
				(target.Children[1] == redCandlePath || target.Children[1] == yellowCandlePath || target.Children[1] == greenCandlePath))
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

			redCandlePath = new Path();
			redCandlePath.Stroke = redCandlePath.Fill = new SolidColorBrush(Colors.Red);
			yellowCandlePath = new Path();
			yellowCandlePath.Stroke = yellowCandlePath.Fill = new SolidColorBrush(Colors.Yellow);
			greenCandlePath = new Path();
			greenCandlePath.Stroke = greenCandlePath.Fill = new SolidColorBrush(Colors.YellowGreen);

			RedrawCandles();
			target.Children.Add(redCandlePath);
			target.Children.Add(yellowCandlePath);
			target.Children.Add(greenCandlePath);
		}

		private void RedrawCandles()
		{
			GeometryGroup redCandleGroup = new GeometryGroup(), yellowCandleGroup = new GeometryGroup(), greenCandleGroup = new GeometryGroup();
			foreach (var candle in Candles)
			{
				switch (candle.Color)
				{
					case 0: //Rot
						redCandleGroup.Children.Add(new EllipseGeometry(new Point(candle.X, candle.Y), 2, 2));
						break;
					case 1: //Gelb
						yellowCandleGroup.Children.Add(new EllipseGeometry(new Point(candle.X, candle.Y), 2, 2));
						break;
					case 2: //Grün
						greenCandleGroup.Children.Add(new EllipseGeometry(new Point(candle.X, candle.Y), 2, 2));
						break;
				}
			}
			redCandlePath.Data = redCandleGroup;
			yellowCandlePath.Data = yellowCandleGroup;
			greenCandlePath.Data = greenCandleGroup;
		}

		public float CalculateScore()
		{
			distanceToClosestNeighbor = new List<float>();
			for (int i = 0; i < Candles.Count; i++)
				distanceToClosestNeighbor.Add(float.PositiveInfinity);

			//Indexe der Kerzen nach Kerzenfarbe gruppieren
			List<List<int>> colorGroupings = new List<List<int>>();
			for (int i = 0; i < Candles.Count; i++)
			{
				var candle = Candles[i];
				if (candle.Color >= colorGroupings.Count)
				{
					for (int j = colorGroupings.Count; j <= candle.Color; j++)
					{
						colorGroupings.Add(new List<int>());
					}
				}

				colorGroupings[candle.Color].Add(i);
			}

			List<float> scores = new List<float>();
			foreach (var color in colorGroupings)
			{
				scores.Add(CalculateScoreForColor(color));
			}

			var distanceToClosestNeighbor2 = new List<float>(Candles.Count);
			for (int i = 0; i < Candles.Count; i++)
			{
				distanceToClosestNeighbor2.Add(float.PositiveInfinity);
			}

			for (int i = 0; i < Candles.Count; i++)
			{
				for (int j = 0; j < Candles.Count; j++)
				{
					if (i == j)
						continue;
					float dist = (float)Math.Sqrt(Math.Pow(Candles[i].X - Candles[j].X, 2) + Math.Pow(Candles[i].Y - Candles[j].Y, 2));
					if (dist < distanceToClosestNeighbor2[i])
					{
						distanceToClosestNeighbor2[i] = dist;
					}
				}
			}

			minValue = float.PositiveInfinity;
			for (int i = 0; i < distanceToClosestNeighbor2.Count; i++)
			{
				if (distanceToClosestNeighbor2[i] < minValue)
				{
					minValue = distanceToClosestNeighbor2[i];
					NearestCandle = i;
				}
			}

			float averageColor = scores.Average();
			float averageAll = distanceToClosestNeighbor2.Average();

			float deviation = 0;
			distanceToClosestNeighbor2.ForEach(d => deviation += Math.Abs(averageAll - d));
			deviation /= distanceToClosestNeighbor2.Count;

			int colorMisdistribution = 0;
			for(int i = 0; i < Candles.Count; i++)
			{
				if (Math.Abs(distanceToClosestNeighbor2[i] - distanceToClosestNeighbor[i]) < 0.0001f)
					colorMisdistribution++;
			}

			return 2 * averageColor +  4 * averageAll - 4 * deviation - colorMisdistribution;
		}

		private float CalculateScoreForColor(List<int> colorerCandles)
		{
			//Berechne Distanzen zwischen allen Paaren in coloredCandles
			for (int i = 0; i < colorerCandles.Count; i++)
			{
				for (int j = 0; j < colorerCandles.Count; j++)
				{
					if (i == j)
						continue;
					float dist = (float)Math.Sqrt(Math.Pow(Candles[colorerCandles[i]].X - Candles[colorerCandles[j]].X, 2)
						+ Math.Pow(Candles[colorerCandles[i]].Y - Candles[colorerCandles[j]].Y, 2));
					if (dist < distanceToClosestNeighbor[colorerCandles[i]])
					{
						distanceToClosestNeighbor[colorerCandles[i]] = dist;
					}
				}
			}

			float average = (float)colorerCandles.Average();

			float deviation = 0;
			colorerCandles.ForEach(c => deviation += Math.Abs(average - distanceToClosestNeighbor[c]));
			deviation /= colorerCandles.Count;

			return average - deviation;
		}
		
		public Cake Clone() //Mache mehr Kuchen
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
		public readonly int NumberOfCandles, DegreeOfParallelization, Colors;
		private float bestScore = float.NegativeInfinity;
		private Cake cake;
		private int globalIterations;

		//Der Zugriff auf den Kuchen ist threadsafe
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

		public CakeGenerator(int numberOfCandles, int degreeOfParallelization, int size, float angle, int colors)
		{
			//Spannende Zuweisungen
			NumberOfCandles = numberOfCandles;
			DegreeOfParallelization = degreeOfParallelization;
			Colors = colors;
			cake = new Cake(size, angle);

			threads = new Thread[DegreeOfParallelization];
			internalCakes = new Cake[DegreeOfParallelization];

			for (int i = 0; i < DegreeOfParallelization; i++)
				internalCakes[i] = cake.Clone();
		}

		public CakeGenerator(Cake cake, int degreeOfParallelization)
		{
			this.cake = cake;
			DegreeOfParallelization = degreeOfParallelization;
			NumberOfCandles = cake.Candles.Count;

			threads = new Thread[DegreeOfParallelization];
			internalCakes = new Cake[DegreeOfParallelization];

			for (int i = 0; i < DegreeOfParallelization; i++)
				internalCakes[i] = cake.Clone();

			//Workaround um zu verhindern das der Kuchen automatisch überschrieben wird
			globalIterations = 1;
		}

		private Thread[] threads;
		private Cake[] internalCakes;
		public async void Optimize(int iterations, CancellationToken cancellationToken, Action endedCallback)
		{
			//0 heißt "endlos" wiederholen
			if (iterations == 0)
				iterations = int.MaxValue;

			//Es wurden noch keine Kuchen erstellt
			if (globalIterations == 0)
			{
				Random random = new Random();
				int[] candleColors = new int[Colors];

				//Erstelle einen zufälligen Kuchen für jeden Thread
				for (int thread = 0; thread < DegreeOfParallelization; thread++)
				{
					var cake = internalCakes[thread];
					int currentColor = -1, nextSwitch = 0;
					for (int i = 0; i < NumberOfCandles; i++)
					{
						int x, y, color = 0;
						if (thread == 0) //Wähle zufällige Kerzenfarbe und zähle mit
						{
							color = random.Next(Colors);
							candleColors[color]++;
						}
						else //Wähle solange die gleiche Farbe bis gleichviele Kerzen diese Frabe haben wie auf dem Kuchen von Thread 0
						{
							if (nextSwitch == i && currentColor < Colors)
							{
								currentColor++;
								nextSwitch += candleColors[currentColor];
							}
							color = currentColor;
						}

						//Ereuge solange zufällige Positionen bis eine gültig ist
						do
						{
							x = random.Next((int)cake.Bounds.X, (int)cake.Bounds.Width);
							y = random.Next((int)cake.Bounds.Y, (int)cake.Bounds.Height);
						}
						while (!cake.Contains(x, y));
						cake.Candles.Add(new Candle(x, y, color));
					}
				}
			}
			//Es gibt schon Kuchen -> Selektion
			else
			{
				for (int i = 0; i < DegreeOfParallelization; i++)
					if (bestScore * 0.9 > internalCakes[i].CalculateScore())
						internalCakes[i] = Cake.Clone();
			}

			//Diese Methode ist asynchron -> Tue irgendwas asynchrones damit der Compiler nicht meckert
			await Task.Run(() =>
			{
				//Erzeuge und starte die Threads
				for (int i = 0; i < DegreeOfParallelization; i++)
				{
					threads[i] = new Thread(OptimizeInternal);
					threads[i].Name = "Evolution worker " + i;
					threads[i].Priority = ThreadPriority.BelowNormal; //Sonst laggt die GUI zu sehr
					threads[i].Start(new Tuple<int, int, CancellationToken>(i, iterations, cancellationToken));
				}
				//Warte darauf, dass alle Threads durchlaufen
				for (int i = 0; i < DegreeOfParallelization; i++)
				{
					threads[i].Join();
				}
			});

			//Führe das Callback auf dem GUI-Thread aus
			Application.Current.Dispatcher.Invoke(endedCallback);
		}

		private void OptimizeInternal(object args)
		{
			//Argumente in sinnvolle Typen zurückverwandeln
			var argsTupel = (Tuple<int, int, CancellationToken>)args;
			int threadId = argsTupel.Item1;
			int iterations = argsTupel.Item2;
			var cancellationToken = argsTupel.Item3;

			var random = new Random();
			var cake = internalCakes[threadId];
			float lastScore = cake.CalculateScore();
			for (int i = 0; i < iterations; i++)
			{
				//Es muss nicht alles n-fach gezählt werden
				if (threadId == 0)
					globalIterations++;

				int randomCandle = random.NextDouble() >= 0.25 ? cake.NearestCandle : random.Next(NumberOfCandles);
				int newX, newY;
				int oldX = cake.Candles[randomCandle].X, oldY = cake.Candles[randomCandle].Y;
				int currentTries = 0;
				while (true)
				{
					i++;
					float cooldown = (float)Math.Ceiling(globalIterations / 5000d);

					//Evolutionen die nicht erfolgsversprechend sind zerstören
					if (i != 0 && i % 10000 == 0) //Zeit für Selektion
					{
						if (bestScore * 0.9 > lastScore)
						{
							internalCakes[threadId] = cake = Cake.Clone();
							lastScore = bestScore;
						}
					}

					currentTries++;
					//Wenn eine Kerze nicht mehr weiter optimiert werden kann, dann wird eine andere genommen
					if (currentTries == 1000)
					{
						currentTries = 0;
						//Aus stucts in einer Liste wird by value zugegriffen -> Hacky workaround
						var tmp = cake.Candles[randomCandle];
						tmp.X = oldX; tmp.Y = oldY;
						cake.Candles[randomCandle] = tmp;
						randomCandle = random.Next(NumberOfCandles);
						oldX = cake.Candles[randomCandle].X;
						oldY = cake.Candles[randomCandle].Y;
					}

					newX = oldX + (int)(Math.Max(((cake.Size * 2) / cooldown) * random.NextDouble(), 1)  //Verschiebungsrange mit cooldown
						* 
						(random.NextDouble() >= 0.5 ? 1 : -1)); //Zufälliges Vorzeichen
					newY = oldY + (int)(Math.Max(((cake.Bounds.Height / 2) / cooldown) * random.NextDouble(), 1)
						* 
						(random.NextDouble() >= 0.5 ? 1 : -1));

					//Wenn eine ungültige Mutation erzeugt wird, dann wird sie nicht mitgezählt
					if (!cake.Contains(newX, newY))
					{
						i--;
						currentTries--;
						continue;
					}

					//Mal wieder der selbe Workaround
					var tmp2 = cake.Candles[randomCandle];
					tmp2.X = newX; tmp2.Y = newY;
					cake.Candles[randomCandle] = tmp2;

					//Abbruchbedingung
					if (cancellationToken.IsCancellationRequested || i >= iterations)
						break;

					if (cake.CalculateScore() > lastScore)
						break;
				}

				lastScore = cake.CalculateScore();
				if (cancellationToken.IsCancellationRequested)
				{
					//Es sollte sichergestellt werden, dass der Kuchen wirklich aktualisiert wird, wenn die Optimierung abgeschlossen ist
					//Sonst kann es sein, dass ein veralteter Kuchen gerendert wird
					UpdateCake(cake.Clone(), lastScore).GetAwaiter().GetResult();
					break;
				}

				//Es ist egal wann der beste Kuchen aktualisiert wird
				UpdateCake(cake.Clone(), lastScore);
			}
		}

		//Ermöglicht es den besten gefundenen Kuchen threadsafe zu aktualisieren
		private async Task UpdateCake(Cake cake, float newScore)
		{
			if (cake == null)
				throw new TheCakeIsALieException();

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

		//Verhindert lustige NullReferenceExceptions beim Schließen des Programms
		public void Cancle()
		{
			for (int i = 0; i < DegreeOfParallelization; i++)
			{
				threads[i].Abort();
			}
		}
	}
}
