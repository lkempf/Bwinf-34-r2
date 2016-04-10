﻿using System;
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
			Exit = 999
		}

		[DebuggerDisplay("{Position} IsExit: {IsExit}")]
		private class Node
		{
			public List<Node> Neighbours { get; private set; } = new List<Node>();
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

			//Alle horizontalen Nachbarn finden
			for (int i = 0; i < worldMap.GetLength(0); i++)
			{
				for (int j = 0; j < worldMap.GetLength(1); j++)
				{
					if (worldMap[i, j] == Tile.Exit || worldMap[i, j] == Tile.Nothing)
					{
						//Erstelle neuen Knoten für die aktuelle Postition
						var node = new Node(worldMap[i, j] == Tile.Exit, new Tuple<int, int>(j + 1, i + 1), worldGraph.Count);
						positionToNode[i, j] = node;
						currentCombo.Add(node);
						worldGraph.Add(node);

						//Wenn ein YamYam über einen Ausgang erreicht, dann erreicht es nicht die nächste Wand -> Ausgang ist Combobreaker
						if (worldMap[i, j] == Tile.Exit)
						{
							ProcessCombo(ref currentCombo);
							currentCombo.Add(node);
						}
					}
					else if (currentCombo.Count != 0) //Wand
					{
						ProcessCombo(ref currentCombo);
					}
				}
				ProcessCombo(ref currentCombo);
			}

			//Alle vertikalen Nachbarn finden
			for (int j = 0; j < worldMap.GetLength(1); j++)
			{
				for (int i = 0; i < worldMap.GetLength(0); i++)
				{
					if (worldMap[i, j] == Tile.Exit || worldMap[i, j] == Tile.Nothing)
					{
						//Keinen neuen Knoten erstellen, ist bereits oben geschehen
						var node = positionToNode[i, j];
						currentCombo.Add(node);
						if(worldMap[i, j] == Tile.Exit)
						{
							ProcessCombo(ref currentCombo);
							currentCombo.Add(node);
						}
					}
					else if (currentCombo.Count != 0) //Wand
					{
						ProcessCombo(ref currentCombo);
					}
				}
				ProcessCombo(ref currentCombo);
			}

			//Wenn man ein Ausgangsfeld erreicht, dann kann man es nicht mehr verlassen -> Alle ausgehenden Kanten von Ausgängen müssen gelöscht werden
			foreach (var node in worldGraph.Where(n => n.IsExit))
				node.Neighbours.Clear();
		}

		//Fügt eine Kante für alle Knoten in einer Sequenz zum Ende und Anfang der Sequenz hinzu
		private void ProcessCombo(ref List<Node> currentCombo)
		{
			foreach (var node in currentCombo)
			{
				if (node != currentCombo.First())
				{
					node.Neighbours.Add(currentCombo.First());
				}
				if (node != currentCombo.Last())
				{
					node.Neighbours.Add(currentCombo.Last());
				}
			}
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
						default:
							throw new FormatException($"{asciiMap[x * numberOfRows + y]} ist kein gültiges Zeichen");
					}
				}

			}
		}

		List<List<Node>> connectedComponents = new List<List<Node>>();
		public IEnumerable<Tuple<int, int>> Solve()
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

			for(int i = 0; i < connectedComponents.Count; i++)
			{
				//Prüfen mit welchen Zusammenhangskomponenten die aktuelle Zusammenhangskomponente verbunden ist
				bool onlyLeadsToExit = true, hasOut = false;
				foreach (var node in connectedComponents[i])
				{
					foreach(var neigbour in node.Neighbours)
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

			//Wähle alle Komponenten aus, die als sicher makierts sind und gibt die Postitionen ihrer Knoten aus
			return connectedComponents.Zip(componentLeadsToExit, (c, b) => b ? c : new List<Node>())
				.SelectMany(c => c)
				.Select(n => n.Position)
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
			foreach (var neighbour in currentNode.Neighbours)
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
