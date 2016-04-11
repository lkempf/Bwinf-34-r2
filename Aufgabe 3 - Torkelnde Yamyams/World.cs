using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Aufgabe_3___Torkelnde_Yamyams
{
	class World
	{
		private enum Tile
		{
			Wall = 0,
			Nothing = 1,
			Stone = 2,
			Exit = 999
		}

		[DebuggerDisplay("{Position} IsExit: {IsExit}")]
		private class Node
		{
			public List<Tuple<Node, int>> Neighbours { get; private set; } = new List<Tuple<Node, int>>();
			public bool IsExit { get; set; } = false;
			public int Index { get; private set; }

			public int ConnectedComponent { get; set; }

			public bool OnStack { get; set; } = false;
			public int Label { get; set; } = -1;
			public int LowLink { get; set; } = -1;

			public readonly Tuple<int, int> Position;

			public Node(bool isExit, Tuple<int, int> position, int index)
			{
				IsExit = isExit;
				Position = position;
				Index = index;
			}

			public void Reset()
			{
				OnStack = false;
				Label = -1;
				LowLink = -1;
				ConnectedComponent = -1;
			}
		}

		private Tile[,] worldMap;
		private List<Node> worldGraph;
		private int stoneCount;

		public World(string asciiMap)
		{
			ParseMap(asciiMap);
			CreateGraph();
		}

		private void CreateGraph()
		{
			Node[,] positionToNode = new Node[worldMap.GetLength(0), worldMap.GetLength(1)];
			worldGraph = new List<Node>(worldMap.Length);
			var currentCombo = new List<Node>();
			var stonePositions = new List<int>();

			//Alle horizontalen Nachbarn finden
			for (int i = 0; i < worldMap.GetLength(0); i++)
			{
				for (int j = 0; j < worldMap.GetLength(1); j++)
				{
					if (worldMap[i, j] == Tile.Exit || worldMap[i, j] == Tile.Nothing || worldMap[i, j] == Tile.Stone)
					{
						//Erstelle neuen Knoten für die aktuelle Postition
						var node = new Node(worldMap[i, j] == Tile.Exit, new Tuple<int, int>(j + 1, i + 1), worldGraph.Count);
						positionToNode[i, j] = node;
						currentCombo.Add(node);
						worldGraph.Add(node);

						//Wenn ein YamYam über einen Ausgang erreicht, dann erreicht es nicht die nächste Wand -> Ausgang ist Combobreaker
						if (worldMap[i, j] == Tile.Exit)
						{
							ProcessCombo(ref currentCombo, ref stonePositions);
							currentCombo.Add(node);
						}
						else if (worldMap[i, j] == Tile.Stone)
						{
							stonePositions.Add(j + 1);
						}
					}
					else if (currentCombo.Count != 0) //Wand
					{
						ProcessCombo(ref currentCombo, ref stonePositions);
					}
				}
				ProcessCombo(ref currentCombo, ref stonePositions);
			}

			//Alle vertikalen Nachbarn finden
			for (int j = 0; j < worldMap.GetLength(1); j++)
			{
				for (int i = 0; i < worldMap.GetLength(0); i++)
				{
					if (worldMap[i, j] == Tile.Exit || worldMap[i, j] == Tile.Nothing || worldMap[i, j] == Tile.Stone)
					{
						//Keinen neuen Knoten erstellen, ist bereits oben geschehen
						var node = positionToNode[i, j];
						currentCombo.Add(node);
						if (worldMap[i, j] == Tile.Exit)
						{
							ProcessCombo(ref currentCombo, ref stonePositions);
							currentCombo.Add(node);
						}
						else if (worldMap[i, j] == Tile.Stone)
						{
							stonePositions.Add(i + 1);
						}
					}
					else if (currentCombo.Count != 0) //Wand
					{
						ProcessCombo(ref currentCombo, ref stonePositions);
					}
				}
				ProcessCombo(ref currentCombo, ref stonePositions);
			}

			//Wenn man ein Ausgangsfeld erreicht, dann kann man es nicht mehr verlassen -> Alle ausgehenden Kanten von Ausgängen müssen gelöscht werden
			foreach (var node in worldGraph.Where(n => n.IsExit))
				node.Neighbours.Clear();
		}

		//Fügt eine Kante für alle Knoten in einer Sequenz zum Ende und Anfang der Sequenz hinzu
		private void ProcessCombo(ref List<Node> currentCombo, ref List<int> stonePositions)
		{
			if (currentCombo.Count >= 2)
			{
				bool isX = currentCombo[0].Position.Item1 == currentCombo[1].Position.Item1;
				int stoneCount = 0;
				foreach (var node in currentCombo)
				{
					//Wenn der nächste Stein gefunden wird, dann erhöht sich die Anzahl an Steinen von der aktuellen Node bis zum rechten Ende
					if (stoneCount < stonePositions.Count)
						if (stonePositions[stoneCount] == (isX ? node.Position.Item2 : node.Position.Item1))
							stoneCount++;

					if (node != currentCombo.First())
					{
						node.Neighbours.Add(new Tuple<Node, int>(currentCombo.First(), Math.Max(stoneCount - 1, 0)));
					}
					if (node != currentCombo.Last())
					{
						node.Neighbours.Add(new Tuple<Node, int>(currentCombo.Last(), stonePositions.Count - stoneCount));
					}
				}
			}
			stonePositions.Clear();
			currentCombo.Clear();
		}

		private void ParseMap(string asciiMap)
		{
			//Zeilenumbrüche entfernen und Dimensionen der Welt berechnen
			asciiMap = asciiMap.Trim('\r', '\n');
			int numberOfLines = asciiMap.Count(c => c == Environment.NewLine.First()) + 1;
			asciiMap = asciiMap.Replace(Environment.NewLine, "");
			int numberOfRows = asciiMap.Length / numberOfLines;

			//Einzelne Zeichen parse und 2D-Array aufbauen
			worldMap = new Tile[numberOfLines, numberOfRows];
			for (int x = 0; x < worldMap.GetLength(0); x++)
			{
				for (int y = 0; y < worldMap.GetLength(1); y++)
				{
					switch (asciiMap[x * numberOfRows + y])
					{
						case ' ':
							worldMap[x, y] = Tile.Nothing;
							break;
						case '#':
							worldMap[x, y] = Tile.Wall;
							break;
						case 'E':
							worldMap[x, y] = Tile.Exit;
							break;
						case 'S':
							worldMap[x, y] = Tile.Stone;
							stoneCount++;
							break;
						default:
							throw new FormatException($"{asciiMap[x * numberOfRows + y]} ist kein gültiges Zeichen");
					}
				}

			}
		}

		List<List<Node>> connectedComponents = new List<List<Node>>();
		public IEnumerable<Tuple<int, int, int>> Solve()
		{
			//Graph auf Ausgangszustand zurücksetzen
			foreach (var node in worldGraph)
				node.Reset();
			connectedComponents.Clear();
			index = -1;

			//Tarjan für alle schwachen Zusammenhangskomponenten aufrufen
			foreach (var node in worldGraph)
				if (node.Label == -1)
					ConnectedComponent(node.Index);

			List<bool> componentLeadsToExit = new List<bool>(connectedComponents.Count);
			connectedComponents.ForEach(c => componentLeadsToExit.Add(false));

			for (int i = 0; i < connectedComponents.Count; i++)
			{
				//Prüfen mit welchen Zusammenhangskomponenten die aktuelle Zusammenhangskomponente verbunden ist
				bool onlyLeadsToExit = true, hasOut = false;
				foreach (var node in connectedComponents[i])
				{
					foreach (var neigbour in node.Neighbours.Select(e => e.Item1))
					{
						if (neigbour.ConnectedComponent != node.ConnectedComponent)
						{
							hasOut = true;
							if (!componentLeadsToExit[neigbour.ConnectedComponent])
								onlyLeadsToExit = false;
						}
					}
				}

				componentLeadsToExit[i] = onlyLeadsToExit;
				//Alle Ausgänge als sicher makieren
				if (!hasOut)
					componentLeadsToExit[i] = connectedComponents[i].Any(n => n.IsExit);
			}

			var distanceMatrix = new int[worldGraph.Count, worldGraph.Count];
			if (stoneCount != 0) //Aus Performancegründen nur ausführen wenn es auch wirklich Steine gibt
			{
				//Matrix mit großen Werten befüllen
				for (int i = 0; i < worldGraph.Count; i++)
					for (int j = 0; j < worldGraph.Count; j++)
						distanceMatrix[i, j] = 0xFEFEFEF; //Eine tolle große Zahl

				//Jeder Knoten hat eine Distanz von 0 zu sich selbst
				for (int i = 0; i < worldGraph.Count; i++)
					distanceMatrix[i, i] = 0;

				//Restliche Distanzen eintragen
				foreach (var node in worldGraph)
					foreach (var edge in node.Neighbours)
						distanceMatrix[node.Index, edge.Item1.Index] = edge.Item2;

				//Weglängen berechnen
				for (int i = 0; i < worldGraph.Count; i++)
				{
					for (int j = 0; j < worldGraph.Count; j++)
					{
						if (i == j)
							continue;
						for (int k = 0; k < worldGraph.Count; k++)
							distanceMatrix[i, j] = Math.Min(distanceMatrix[i, j], distanceMatrix[i, k] + distanceMatrix[k, j]);
					}
				}
			}

			var distanceToClosestExit = new List<int>(worldGraph.Count);
			for (int i = 0; i < worldGraph.Count; i++)
				distanceToClosestExit.Add(0);

			for (int i = 0; i < componentLeadsToExit.Count; i++)
			{
				if (!componentLeadsToExit[i])
					continue;
				foreach (var node in connectedComponents[i])
				{
					int bestDistanceToExit = 0xFEFEFEF; //Die gleiche tolle große Zahl wie oben
					foreach (var exit in worldGraph.Where(n => n.IsExit))
					{
						bestDistanceToExit = Math.Min(bestDistanceToExit, distanceMatrix[node.Index, exit.Index]);
					}
					distanceToClosestExit[node.Index] = bestDistanceToExit;
				}
			}

			//Wähle alle Komponenten aus, die als sicher makierts sind und gibt die Postitionen ihrer Knoten aus
			return connectedComponents.Zip(componentLeadsToExit, (c, b) => b ? c : new List<Node>())
				.SelectMany(c => c)
				.Select(n => new Tuple<int, int, int>(n.Position.Item1, n.Position.Item2, distanceToClosestExit[n.Index]))
				.OrderBy(n => n);
		}


		//Invarianten für Tarjan
		int index = -1;
		Stack<int> stack = new Stack<int>();
		private void ConnectedComponent(int n)
		{
			//Aktuellen Knoten als besucht markieren
			Node currentNode = worldGraph[n];
			index++;
			currentNode.Label = index;
			currentNode.LowLink = index;
			currentNode.OnStack = true;
			stack.Push(currentNode.Index);

			//Durch Nachbarn iterieren
			foreach (var neighbour in currentNode.Neighbours.Select(e => e.Item1))
			{
				//Unbesuchter Nachbar -> besuchen
				if (neighbour.Label == -1)
				{
					ConnectedComponent(neighbour.Index);
					currentNode.LowLink = Math.Min(currentNode.LowLink, neighbour.LowLink);
				}
				//Nachbar auf Stack -> Lowlink aktualisieren
				else if (neighbour.OnStack)
				{
					currentNode.LowLink = Math.Min(currentNode.LowLink, neighbour.LowLink);
				}
			}

			//Zusammenhangskomponeten gefunden
			if (currentNode.LowLink == currentNode.Label)
			{
				//Zusammenhangskomponenten vom Stack entfernen
				List<Node> currentComponent = new List<Node>();
				Node nodeFromStack;
				do
				{
					nodeFromStack = worldGraph[stack.Pop()];
					nodeFromStack.OnStack = false;
					nodeFromStack.ConnectedComponent = connectedComponents.Count;
					currentComponent.Add(nodeFromStack);
				} while (nodeFromStack != currentNode);
				//Und zurückgeben
				connectedComponents.Add(currentComponent);
			}
		}
	}
}
