using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
			public List<Node> ReverseNeighbours { get; private set; } = new List<Node>();
			public bool IsExit { get; set; } = false;
			public int SureNeighbors { get; set; }
			
			public readonly Tuple<int, int> Position;

			public Node(bool isExit, Tuple<int, int> position)
			{
				IsExit = isExit;
				Position = position;
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
						var node = new Node(worldMap[i, j] == Tile.Exit, new Tuple<int, int>(i, j));
						positionToNode[i, j] = node;
						currentCombo.Add(node);
						worldGraph.Add(node);
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
						var node = positionToNode[i, j];
						currentCombo.Add(node);
						//worldGraph.Add(node);
					}
					else if (currentCombo.Count != 0) //Wand
					{
						ProcessCombo(ref currentCombo);
					}
				}
				ProcessCombo(ref currentCombo);
			}
		}

		private void ProcessCombo(ref List<Node> currentCombo)
		{
			foreach (var node in currentCombo)
			{
				if (node != currentCombo.First())
				{
					currentCombo.First().ReverseNeighbours.Add(node);
					node.Neighbours.Add(currentCombo.First());
				}
				if (node != currentCombo.Last())
				{
					currentCombo.Last().ReverseNeighbours.Add(node);
					node.Neighbours.Add(currentCombo.Last());
				}
			}
			currentCombo.Clear();
		}

		private void ParseMap(string asciiMap)
		{
			int numberOfLines = asciiMap.Count(c => c == Environment.NewLine.First());
			asciiMap = asciiMap.Replace(Environment.NewLine, "");
			int numberOfRows = asciiMap.Length / numberOfLines;

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

		public List<Tuple<int, int>> Solve()
		{
			Queue<Node> queue = new Queue<Node>();
			foreach (var node in worldGraph.Where(n => n.IsExit))
			{
				queue.Enqueue(node);
			}

			while (queue.Count != 0)
			{
				var currentNode = queue.Dequeue();
				if(currentNode.IsExit  || currentNode.SureNeighbors == currentNode.Neighbours.Count)
				{
					foreach (var neighbour in currentNode.ReverseNeighbours)
					{
						if (!neighbour.IsExit && neighbour.SureNeighbors != currentNode.Neighbours.Count)
						{
							queue.Enqueue(neighbour);
						}
					}
				}
				else
				{
					currentNode.SureNeighbors++;
					if(currentNode.SureNeighbors == currentNode.Neighbours.Count)
					{
						foreach (var neighbour in currentNode.ReverseNeighbours)
						{
							if (!neighbour.IsExit && neighbour.SureNeighbors != currentNode.Neighbours.Count)
							{
								queue.Enqueue(neighbour);
							}
						}
					}
				}
			}

			return worldGraph.Where(n => n.Neighbours.Count == n.SureNeighbors).Select(n => n.Position).ToList();
		}
	}
}
