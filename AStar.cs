using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Surgeon
{
    public class AStarNode
    {
        public List<AStarNode> Neighbors;

        public float Weight { get; set; }

        public AStarNode Parent { get; set; }

        public Vector3 Position { get; set; }

        /// <summary>
        /// G + H.
        /// </summary>
        public float F { get; set; }
        
        /// <summary>
        /// Cost from start.
        /// </summary>
        public float G { get; set; }
        
        /// <summary>
        /// Estimated value from this node to the goal node.
        /// </summary>
        public float H { get; set; }

        public bool bInOpen { get; set; }
        public bool bInClosed { get; set; }

        public bool bWalkable { get; set; }

        public AStarNode(float weight, Vector3 position)
        {
            Position = position;
            Weight = weight;
            Neighbors = new List<AStarNode>();
            G = F = H = 0;

            bInOpen = bInClosed = false;

            bWalkable = true;
        }

        
        /// <summary>
        /// Add a new node as a neighbor to this node, and this node as a neighbor for the new node.
        /// </summary>
        /// <param name="neighborNode">Node to connect with.</param>
        public void ConnectNodes(AStarNode neighborNode)
        {
            if (!Neighbors.Contains(neighborNode))
                Neighbors.Add(neighborNode);

            if (!neighborNode.Neighbors.Contains(this))
                neighborNode.Neighbors.Add(this);
        }
    }

    

    public class AStar
    {
        public static float ASHManhattan3D(AStarNode node, AStarNode goal)
        {
            return Math.Abs(goal.Position.X - node.Position.X) + Math.Abs(goal.Position.Y - node.Position.Y) + Math.Abs(goal.Position.Z - node.Position.Z);
        }

        public static float ASHDistance(AStarNode node, AStarNode goal)
        {
            return Vector3.Distance(node.Position, goal.Position);
        }

        private static List<AStarNode> RebuildPath(AStarNode start)
        {
            List<AStarNode> path = new List<AStarNode>();

            if (start != null)
            {
                AStarNode node = start;

                while (node.Parent != null)
                {
                    // path.Insert(0, node);
                    path.Add(node);
                    node = node.Parent;
                }
            }

            return path;
        }

        public static void AddToClosed(AStarNode node, List<AStarNode> closedList)
        {
            if (!node.bInClosed)
            {
                node.bInClosed = true;
                closedList.Add(node);
            }
        }

        public static void AddToOpen(AStarNode node, List<AStarNode> openList)
        {
            if (!node.bInOpen)
            {
                node.bInOpen = true;
                openList.Add(node);
            }
        }

        public static void RemoveFromOpen(AStarNode node, List<AStarNode> openList)
        {
            if (node.bInOpen)
            {
                node.bInOpen = false;
                openList.Remove(node);
            }
        }


        public static List<AStarNode> FindPathNavMesh(AStarNode start, AStarNode end)
        {
            // G score = total cost up to the current node
            // H score = estimated cost from current node to end

            bool bSuccess = false;

            List<AStarNode> path = new List<AStarNode>();
            List<AStarNode> open = new List<AStarNode>();
            List<AStarNode> closed = new List<AStarNode>();

            AStarNode currentNode;

            start.G = 0;
            start.H = ASHManhattan3D(start, end);
            start.F = start.G + start.H;
            start.Parent = null;

            AddToOpen(start, open);

            while (open.Count > 0 && !bSuccess)
            {
                currentNode = open[0];

                for (int i = 1; i < open.Count; i++)
                {
                    if (open[i].F < currentNode.F)
                        currentNode = open[i];
                }


                RemoveFromOpen(currentNode, open);

                AddToClosed(currentNode, closed);

                if (currentNode == end)
                {
                    bSuccess = true;

                    path = RebuildPath(currentNode);
                }
                else
                {
                    if (currentNode.Neighbors.Count > 0)
                    {
                        for (int i = 0; i < currentNode.Neighbors.Count; i++)
                        {
                            if (!currentNode.Neighbors[i].bInClosed)
                            {
                                if (!currentNode.Neighbors[i].bWalkable)
                                {
                                    AddToClosed(currentNode.Neighbors[i], closed);
                                    continue;
                                }
                                // Node has not been tested yet
                                float new_g = currentNode.G + Vector3.Distance(currentNode.Neighbors[i].Position, currentNode.Position);
                                bool bNewGisBetteR = false;

                                //if (!open.Contains(currentNode.Neighbors[i]))
                                if (!currentNode.Neighbors[i].bInOpen)
                                {
                                    AddToOpen(currentNode.Neighbors[i], open);
                                    currentNode.Neighbors[i].H = ASHDistance(currentNode.Neighbors[i], end);
                                    bNewGisBetteR = true;
                                }
                                else if (new_g < currentNode.Neighbors[i].G)
                                {
                                    bNewGisBetteR = true;
                                }

                                if (bNewGisBetteR)
                                {
                                    currentNode.Neighbors[i].G = new_g;
                                    currentNode.Neighbors[i].Parent = currentNode;
                                    currentNode.Neighbors[i].F = currentNode.Neighbors[i].H + currentNode.Neighbors[i].G;
                                }
                            }
                        }
                    }
                }
            }


            return path;
        }




        public static void FindPath(AStarNode start, AStarNode end, out List<AStarNode> path)
        {
            // G score = total cost up to the current node
            // H score = estimated cost from current node to end
            // 

            bool bSuccess = false;

            // List<AStarNode> path = new List<AStarNode>();
            List<AStarNode> open = new List<AStarNode>();
            List<AStarNode> closed = new List<AStarNode>();

            AStarNode currentNode = null;

            start.G = 0;
            start.H = ASHManhattan3D(start, end);
            start.F = start.G + start.H;
            start.Parent = null;



            AddToOpen(start, open);
            //open.Add(start);
            //start.bInOpen = true;

            while (open.Count > 0 && !bSuccess)
            {
                // open.Sort(SortOnF);

                currentNode = open[0];

                for (int i = 1; i < open.Count; i++)
                {
                    if (open[i].F < currentNode.F)
                        currentNode = open[i];
                }


                RemoveFromOpen(currentNode, open);
                //open.Remove(currentNode);
                //currentNode.bInOpen = false;

                AddToClosed(currentNode, closed);
                //closed.Add(currentNode);
                //currentNode.bInClosed = true;

                if (currentNode == end)
                {
                    // End
                    bSuccess = true;
                    // open.Clear();

                    
                }
                else
                {
                    if (currentNode.Neighbors.Count > 0)
                    {
                        for (int i = 0; i < currentNode.Neighbors.Count; i++)
                        {
                            if (!currentNode.Neighbors[i].bInClosed)
                            {
                                if (!currentNode.Neighbors[i].bWalkable)
                                {
                                    AddToClosed(currentNode.Neighbors[i], closed);
                                    continue;
                                }
                                // Node has not been tested yet
                                float new_g = currentNode.G + currentNode.Neighbors[i].Weight;
                                bool bNewGisBetteR = false;

                                //if (!open.Contains(currentNode.Neighbors[i]))
                                if (!currentNode.Neighbors[i].bInOpen)
                                {
                                    AddToOpen(currentNode.Neighbors[i], open);
                                    //currentNode.Neighbors[i].bInOpen = true;
                                    //open.Add(currentNode.Neighbors[i]);
                                    currentNode.Neighbors[i].H = ASHManhattan3D(currentNode.Neighbors[i], end);
                                    bNewGisBetteR = true;
                                }
                                else if (new_g < currentNode.Neighbors[i].G)
                                {
                                    bNewGisBetteR = true;
                                }

                                if (bNewGisBetteR)
                                {
                                    currentNode.Neighbors[i].G = new_g;
                                    currentNode.Neighbors[i].Parent = currentNode;
                                    currentNode.Neighbors[i].F = currentNode.Neighbors[i].H + currentNode.Neighbors[i].G;
                                }


                            }
                        }
                    }
                }
            }

            
            path = RebuildPath(currentNode);
        }


        public static List<AStarNode> FindPath(AStarNode start, AStarNode end)
        {
            // G score = total cost up to the current node
            // H score = estimated cost from current node to end

            bool bSuccess = false;

            List<AStarNode> path = new List<AStarNode>();
            List<AStarNode> open = new List<AStarNode>();
            List<AStarNode> closed = new List<AStarNode>();

            AStarNode currentNode;

            start.G = 0;
            start.H = ASHManhattan3D(start, end);
            start.F = start.G + start.H;
            start.Parent = null;

            AddToOpen(start, open);

            while (open.Count > 0 && !bSuccess)
            {
                currentNode = open[0];

                for (int i = 1; i < open.Count; i++)
                {
                    if (open[i].F < currentNode.F)
                        currentNode = open[i];
                }


                RemoveFromOpen(currentNode, open);

                AddToClosed(currentNode, closed);

                if (currentNode == end)
                {
                    bSuccess = true;

                    path = RebuildPath(currentNode);
                }
                else
                {
                    if (currentNode.Neighbors.Count > 0)
                    {
                        for (int i = 0; i < currentNode.Neighbors.Count; i++)
                        {
                            if (!currentNode.Neighbors[i].bInClosed)
                            {
                                if (!currentNode.Neighbors[i].bWalkable)
                                {
                                    AddToClosed(currentNode.Neighbors[i], closed);
                                    continue;
                                }
                                // Node has not been tested yet
                                float new_g = currentNode.G + currentNode.Neighbors[i].Weight;
                                bool bNewGisBetteR = false;

                                //if (!open.Contains(currentNode.Neighbors[i]))
                                if (!currentNode.Neighbors[i].bInOpen)
                                {
                                    AddToOpen(currentNode.Neighbors[i], open);
                                    currentNode.Neighbors[i].H = ASHManhattan3D(currentNode.Neighbors[i], end);
                                    bNewGisBetteR = true;
                                }
                                else if (new_g < currentNode.Neighbors[i].G)
                                {
                                    bNewGisBetteR = true;
                                }

                                if (bNewGisBetteR)
                                {
                                    currentNode.Neighbors[i].G = new_g;
                                    currentNode.Neighbors[i].Parent = currentNode;
                                    currentNode.Neighbors[i].F = currentNode.Neighbors[i].H + currentNode.Neighbors[i].G;
                                }
                            }
                        }
                    }
                }
            }


            return path;
        }

        
        public static List<AStarNode> FindPath(AStarNode start, AStarNode end, out List<AStarNode> searchedClosed, out List<AStarNode> searchedOpen)
        {
            // G score = total cost up to the current node
            // H score = estimated cost from current node to end
            // 

            bool bSuccess = false;

            List<AStarNode> path = new List<AStarNode>();
            List<AStarNode> open = new List<AStarNode>();
            List<AStarNode> closed = new List<AStarNode>();

            AStarNode currentNode;

            start.G = 0;
            start.H = ASHManhattan3D(start, end);
            start.F = start.G + start.H;
            start.Parent = null;

            

            AddToOpen(start, open);
            //open.Add(start);
            //start.bInOpen = true;

            while (open.Count > 0 && !bSuccess)
            {
                // open.Sort(SortOnF);

                currentNode = open[0];

                for (int i = 1; i < open.Count; i++)
                {
                    if (open[i].F < currentNode.F)
                        currentNode = open[i];
                }


                RemoveFromOpen(currentNode, open);
                //open.Remove(currentNode);
                //currentNode.bInOpen = false;

                AddToClosed(currentNode, closed);
                //closed.Add(currentNode);
                //currentNode.bInClosed = true;

                if (currentNode == end)
                {
                    // End
                    bSuccess = true;
                    // open.Clear();

                    path = RebuildPath(currentNode);
                }
                else
                {
                    if (currentNode.Neighbors.Count > 0)
                    {
                        for (int i = 0; i < currentNode.Neighbors.Count; i++)
                        {
                            if (!currentNode.Neighbors[i].bInClosed)
                            {
                                if (!currentNode.Neighbors[i].bWalkable)
                                {
                                    AddToClosed(currentNode.Neighbors[i], closed);
                                    continue;
                                }
                                // Node has not been tested yet
                                float new_g = currentNode.G + currentNode.Neighbors[i].Weight;
                                bool bNewGisBetteR = false;

                                //if (!open.Contains(currentNode.Neighbors[i]))
                                if (!currentNode.Neighbors[i].bInOpen)
                                {
                                    AddToOpen(currentNode.Neighbors[i], open);
                                    //currentNode.Neighbors[i].bInOpen = true;
                                    //open.Add(currentNode.Neighbors[i]);
                                    currentNode.Neighbors[i].H = ASHManhattan3D(currentNode.Neighbors[i], end);
                                    bNewGisBetteR = true;
                                }
                                else if (new_g < currentNode.Neighbors[i].G)
                                {
                                    bNewGisBetteR = true;
                                }

                                if (bNewGisBetteR)
                                {
                                    currentNode.Neighbors[i].G = new_g;
                                    currentNode.Neighbors[i].Parent = currentNode;
                                    currentNode.Neighbors[i].F = currentNode.Neighbors[i].H + currentNode.Neighbors[i].G;
                                }


                            }
                        }
                    }
                }
            }

            
            searchedClosed = closed;
            searchedOpen = open;



            return path;
        }

        
    }
}