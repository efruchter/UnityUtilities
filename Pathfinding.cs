using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Pathfinding;
using Utility;

/**
 * Pathfinding for a gridworld. Has an A* routine as well as a FlowGraph.
 * Links can only be in 4 cardinal directions. Supports only a single global
 * grid.
 
 * -Eric
 */
namespace Pathfinding
{
	public class LinkDirection
	{
		public const int UP = 0, DOWN = 1, LEFT = 2, RIGHT = 3;
	}

	public class SearchNode
	{
		public int x;
		public int y;
		public SearchNode[] neighbors;
		public readonly int hash;

		public SearchNode(int x, int y)
		{
			this.x = x;
			this.y = y;
			this.neighbors = new SearchNode[4];
			hash = (x + ":" + y).GetHashCode();
		}

		public override int GetHashCode()
		{
			return hash;
		}

		public bool PosMatch(SearchNode o)
		{
			return x == o.x && y == o.y;
		}

		public int MDist(SearchNode o)
		{
			return Mathf.Abs(x - o.x) + Mathf.Abs (y - o.y);
		}

		public int NeighborCount()
		{
			int c = 0;
			for (int i = 0; i < this.neighbors.Length; i++)
				if (neighbors[i] != null)
					c++;
			return c;
		}

		public IEnumerable<SearchNode> GetNeighbors(bool weakRandomize = false)
		{
			if (weakRandomize && Random.value < 0.5f)
			{
				for (int i = 0; i < this.neighbors.Length; i++)
					if (neighbors[i] != null)
						yield return neighbors[i];
			}
			else
			{
				for (int i = this.neighbors.Length - 1; i >= 0 ; i--)
					if (neighbors[i] != null)
						yield return neighbors[i];
			}
		}
	}

	public class ManhattanDistance : IComparer<SearchNode>
	{
		private SearchNode dest;
		private int[,] distance;

		public ManhattanDistance(SearchNode destination, int[,] distance)
		{
			dest = destination;
			this.distance = distance;
		}

		public int Compare(SearchNode a, SearchNode b)
		{
			int aD = a.MDist (dest);
			int bD = b.MDist (dest);
			return (distance[a.x, a.y] + aD).CompareTo(distance[b.x, b.y] + bD);
		}
	}

	public class Search
	{
		// FlowGraph
		private static SearchNode[,] latestGraph;
		private static int[,] flowGraph;
		
		// Single pathable graph
		private static bool[,] explored;
		private static bool[,] open;
		private static int[,] distance;
		private static SearchNode[,] parents;

		public static int getFlowNumber(int x, int y)
		{
			return flowGraph [x, y];
		}

		public static void InitializeGraph(int xTiles, int yTiles)
		{
			explored = new bool[xTiles, yTiles];
			open = new bool[xTiles, yTiles];
			distance = new int[xTiles, yTiles];
			parents = new SearchNode[xTiles, yTiles];
			
			bool[,] tiles = WallBuilder.GetTiles ();
			int w = xTiles, h = yTiles;

			latestGraph = new SearchNode[w, h];
			flowGraph = new int[w, h];

			for (int x = 0; x < w; x ++)
			{
				for (int y = 0; y < h; y ++)
				{
					latestGraph[x, y] = new SearchNode(x, y);
					flowGraph[x, y] = int.MaxValue;
				}
			}

			for (int x = 0; x < w; x ++)
			{
				for (int y = 0; y < h; y ++)
				{
					//left
					if (x > 0 && tiles[x - 1, y])
						latestGraph[x, y].neighbors[LinkDirection.LEFT] = latestGraph[x - 1, y];
					//right
					if (x < w - 1 && tiles[x + 1, y])
						latestGraph[x, y].neighbors[LinkDirection.RIGHT] = latestGraph[x + 1, y];
					//up
					if (y > 0 && tiles[x, y - 1])
						latestGraph[x, y].neighbors[LinkDirection.UP] = latestGraph[x, y - 1];
					//down
					if (y < h - 1 && tiles[x, y + 1])
						latestGraph[x, y].neighbors[LinkDirection.DOWN] = latestGraph[x, y + 1];
				}
			}
		}

		public static IEnumerator UpdateFlowGraph (int goalX, int goalY, int exploreCountPerFrame=10)
		{
			LinkedList<SearchNode> frontier = new LinkedList<SearchNode>();
			Dictionary<SearchNode, SearchNode> open = new Dictionary<SearchNode, SearchNode>();
			Dictionary<SearchNode, SearchNode> closed = new Dictionary<SearchNode, SearchNode>();

			open.Add(latestGraph [goalX, goalY], latestGraph [goalX, goalY]);
			frontier.AddLast(latestGraph [goalX, goalY]);
			flowGraph[goalX, goalY] = 0;
			
			int expandedCount = 0;

			while (frontier.Count > 0)
			{
				var current = frontier.First.Value;

				frontier.RemoveFirst ();
				open.Remove (current);
				closed.Add (current, current);

				foreach (var node in current.GetNeighbors())
				{
					if (closed.ContainsKey(node) || open.ContainsKey(node))
						continue;

					flowGraph[node.x, node.y] = flowGraph[current.x, current.y] + 1;
					open.Add (node, node);
					frontier.AddLast (node);
				}

				if ((expandedCount += 1) >= exploreCountPerFrame)
				{
					expandedCount = 0;
					yield return null;
				}
			}
		}

		public static void UpdateWall (int x, int y, bool isUp)
		{
			var wall = latestGraph [x, y];

			//left
			if (x > 0)
				latestGraph [x - 1, y].neighbors [LinkDirection.RIGHT] = isUp ? null : wall;

			//right
			if (x < latestGraph.GetLength(0) - 1)
				latestGraph[x + 1, y].neighbors[LinkDirection.LEFT] = isUp ? null : wall;

			//up
			if (y > 0)
				latestGraph [x, y - 1].neighbors [LinkDirection.DOWN] = isUp ? null : wall;

			//down
			if (y < latestGraph.GetLength(1) - 1)
				latestGraph[x, y + 1].neighbors[LinkDirection.UP] = isUp ? null : wall;

			if (isUp)
				flowGraph [x, y] = int.MaxValue;
			else
			{
				var min = int.MaxValue;
				foreach (var n in wall.GetNeighbors())
				{
					if (flowGraph[n.x, n.y] < min)
						min = flowGraph[n.x, n.y];
				}
				flowGraph[x, y] = min + 1;
			}
		}

		public static LinkedList<SearchNode> PathFindAStar(int[] startArr, int[] endArr, int expandLimit=200)
		{
			bool[,] tiles = WallBuilder.GetTiles ();

			if (tiles [startArr [0], startArr [1]] || tiles [endArr [0], endArr [1]])
				return new LinkedList<SearchNode>();

			int w = tiles.GetLength(0), h = tiles.GetLength (1);

			//Grab init nodes
			var start = latestGraph[startArr[0], startArr[1]];
			var end = latestGraph[endArr [0], endArr [1]];

			if (start.PosMatch (end))
			{
				LinkedList<SearchNode> li = new LinkedList<SearchNode> ();
				li.AddLast(start);
				li.AddLast(end);
				return li;
			}

			//Set up tracking structures
			for (int x = 0; x < w; x ++)
			{
				for (int y = 0; y < h; y ++) {
					explored[x, y] = false;
					open[x, y] = false;
					distance[x, y] = int.MaxValue;
				}
			}

			BinaryHeap<SearchNode> frontier =
				new BinaryHeap<SearchNode>(new ManhattanDistance(end, distance));

			frontier.Insert(start);
			open[start.x, start.y] = true;
			parents[start.x, start.y] = null;

			distance[start.x, start.y] = 0;

			//Frontier loop
			while (frontier.Count > 0)
			{
				//Grab top node
				SearchNode current = frontier.RemoveRoot();
				open[current.x, current.y] = false;
				explored[current.x, current.y] = true;

				if ((expandLimit -= 1) <= 0)
				{
					end = current;
					break;
				}

				//If it's the end, just stop.
				if (current.PosMatch(end))
					break;

				foreach (var node in current.GetNeighbors())
				{
					var newDistance = distance[current.x, current.y] + 1;

					//Move on to next neighbor if already explored
					if (explored[node.x, node.y])
						continue;

					//Update distance
					if (newDistance <= distance[node.x, node.y])
					{
						distance[node.x, node.y] = newDistance;
						parents[node.x, node.y] = current;
					}

					if (!open[node.x, node.y])
						frontier.Insert (node);
				}
			}

			LinkedList<SearchNode> path = new LinkedList<SearchNode>();

			var curr = end;
			while (curr != null)
			{
				path.AddFirst(curr);
				curr = parents[curr.x, curr.y];
			}

			return path;
		}

		public static SearchNode PathFindFlowGraph(int x, int y, bool randomized = false)
		{
			var min = flowGraph[x, y];
			SearchNode target = null;

			foreach (var node in latestGraph[x, y].GetNeighbors(true))
			{
				var isBetter = flowGraph[node.x, node.y] < min;
				var isEqual = flowGraph[node.x, node.y] == min && randomized;

				if (isBetter || (isEqual && Random.value < 0.5f))
				{
					min = flowGraph[node.x, node.y];
					target = node;
				}
			}

			return target;
		}

		public static SearchNode FindReversePathFlowGraph(int x, int y)
		{
			var match = flowGraph[x, y];
			SearchNode target = null;

			foreach (var node in latestGraph[x, y].GetNeighbors(true))
			{
				var isMatch = flowGraph[node.x, node.y] > match;

				if ((isMatch && Random.value < 0.5f) || (isMatch && target == null))
					target = node;
			}

			return target;
		}
	}
}
