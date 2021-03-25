using ct3d.Support;
using MoreLinq;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ct3d.WorldPrimitives
{
    record TerrainGraph(Terrain Terrain)
    {
        record SmallPosition(short X, short Y)
        {
            public SmallPosition(int x, int y) : this((short)x, (short)y) { }
        }

        class RoadGraph
        {
            public HashSet<SmallPosition> Positions { get; } = new();
        }
        readonly List<RoadGraph> roadGraphs = new();
        readonly Dictionary<SmallPosition, RoadGraph> positionToRoadGraph = new();

        static readonly Dictionary<TerrainRoadData, SmallPosition> roadOffsets = new()
        {
            [TerrainRoadData.Up] = new(0, 1),
            [TerrainRoadData.Left] = new(-1, 0),
            [TerrainRoadData.Right] = new(1, 0),
            [TerrainRoadData.Down] = new(0, -1),
        };
        static readonly Dictionary<TerrainRoadData, TerrainRoadData> oppositeRoadTypes = new()
        {
            [TerrainRoadData.Down] = TerrainRoadData.Up,
            [TerrainRoadData.Up] = TerrainRoadData.Down,
            [TerrainRoadData.Left] = TerrainRoadData.Right,
            [TerrainRoadData.Right] = TerrainRoadData.Left,
        };

        public void UpdateGraph(int x, int y, TerrainRoadData newRoadData)
        {
            var smallPosition = new SmallPosition((short)x, (short)y);
            var oldRoadData = Terrain[x, y].RoadData;
            if (newRoadData == oldRoadData) return;

            var addedRoadData = newRoadData & ~oldRoadData;
            if (addedRoadData != TerrainRoadData.None)
            {
                var addedInThisIteration = false;
                for (var addedRoadDataItem = (TerrainRoadData)(1 << 0); addedRoadDataItem < (TerrainRoadData)(1 << 4); addedRoadDataItem = (TerrainRoadData)((int)addedRoadDataItem << 1))
                    if (addedRoadData.HasFlag(addedRoadDataItem))
                    {
                        var terrainOffset = roadOffsets[addedRoadDataItem];
                        if (oldRoadData == TerrainRoadData.None && !addedInThisIteration)
                            if (!Terrain[x + terrainOffset.X, y + terrainOffset.Y].RoadData.HasFlag(oppositeRoadTypes[addedRoadDataItem]))
                            {
                                // new unconnected graph
                                var graph = new RoadGraph() { Positions = { smallPosition } };
                                positionToRoadGraph[smallPosition] = graph;
                                roadGraphs.Add(graph);
                                addedInThisIteration = true;
                            }
                            else
                            {
                                // connect this cell to the other graph
                                var graph = positionToRoadGraph[new(x + terrainOffset.X, y + terrainOffset.Y)];
                                graph.Positions.Add(smallPosition);
                                positionToRoadGraph[smallPosition] = graph;
                            }
                        else if (Terrain[x + terrainOffset.X, y + terrainOffset.Y].RoadData.HasFlag(oppositeRoadTypes[addedRoadDataItem]))
                        {
                            // connect the two graphs
                            var g1 = positionToRoadGraph[smallPosition];
                            var g2 = positionToRoadGraph[new(x + terrainOffset.X, y + terrainOffset.Y)];
                            if (g1 != g2)
                            {
                                roadGraphs.Remove(g2);
                                g1.Positions.AddRange(g2.Positions);
                                g2.Positions.ForEach(p => positionToRoadGraph[p] = g1);
                            }
                        }
                    }
            }

            var removedRoadData = oldRoadData & ~newRoadData;
            if (removedRoadData != TerrainRoadData.None)
                throw new NotImplementedException();
        }
    }
}
