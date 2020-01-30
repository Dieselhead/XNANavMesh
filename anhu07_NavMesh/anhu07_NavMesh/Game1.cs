/*
 * 
 * D1	Toggle AABB
D2	Toggle Pylon sphere
D3	Toggle Verts
D4	Toggle Fill AABBs
D5	Toggle Edges
D6	Toggle Portals
D7	Toggle Text
D8	Toggle Final Path

J	Merge Verts
L	Create Mesh
K	Create Edges 2
M	Show All Connections
N	Show Edges
B	Clean Edges
I	Create Portals
Y	Create ASNodes
U	Do Mesh Search
Enter	Start Filling



O
ENTER
L
K
N
B
B
I
Y
U
 * */

#define SD

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using Surgeon;

using PerformanceUtility.GameDebugTools;

namespace anhu07_NavMesh
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        DebugSystem debugSystem;

        Texture2D crosshair;
        SpriteFont Font;


        public DeferredRenderer Renderer;
        public CameraFree Camera;

        public bool bDrawGeomAABB = true;

        Plane FloorPlane;
        Vector3 UnprojectSource;

        public List<Geometry> Geom;

        Geometry SelectedGeometry;

        
        public List<Pylon> Pylons;
        public bool bDrawPylonSphere = false;

        public List<FloodFillNode> FillNodes;
        public bool bDrawFillNodes = false;

        public List<NavMeshVertex> Vertecies;
        public bool bDrawVertecies = false;

        public bool bDrawEdges = true;

        public List<NavMeshVertex> Edges;

        public List<NavMeshVertex> RealEdges;
        public List<NavMeshVertex> VerticesCopy;
        public int VertexCount = 0;

        public List<Portal> Portals;
        public List<NavMeshVertex> PointedVertex;
        public List<NavMeshVertex> RoundedVertex;

        public List<NavMeshVertex> VertexAddedByPortalCreation;

        public List<Portal> EdgeList;

        public bool bDrawPortals = true;

        public int removedPortals = 0;

        public List<ASNNavMesh> NodeList;
        public List<ASNNavMesh> NodePath;

        public List<Vector3> CenterPointsList;

        public int pathCount = 0;

        public int portalToSearch = 0;

        public bool bDrawFinalPath = true;
        public bool bDrawDebugText = true;


        public List<BoundingSphere> BoundingSpheres;
        public bool bDrawBoundingSpheres = true;

        public float GeometryScale = 1.0f;
        

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

#if HD
            graphics.PreferredBackBufferHeight = 1080;
            graphics.PreferredBackBufferWidth = 1920;
#else
            graphics.PreferredBackBufferHeight = 720;
            graphics.PreferredBackBufferWidth = 1280;
#endif


            graphics.IsFullScreen = false;

        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            anhu07_NavMesh.DebugShapeRenderer.Initialize(GraphicsDevice);

            debugSystem = DebugSystem.Initialize(this, "SpriteFont1");
            debugSystem.TimeRuler.Visible = true;
            debugSystem.FpsCounter.Visible = true;
            debugSystem.TimeRuler.ShowLog = true;

            Font = Content.Load<SpriteFont>("SpriteFont1");

            // TODO: Add your initialization logic here
            Camera = new CameraFree(MathHelper.ToRadians(70), GraphicsDevice.Viewport.AspectRatio, 0.1f, 100.0f);
            Camera.CameraRotationSpeed = 0.8f;

            Renderer = new DeferredRenderer(this, Camera,
                                    Content.Load<Effect>("PPLighting")
                                    , Content.Load<Effect>("DirectionalLighting")
                                    , Content.Load<Effect>("ShadowDepth")
                                    , Content.Load<Effect>("Combine")
                                    , new CModel(Content.Load<Model>("PPLightMesh"), Vector3.Zero, Vector3.Zero, Vector3.One, GraphicsDevice, Content.Load<Effect>("PPLighting"))
                                    , 2048);

            Renderer.EffectBloom = Content.Load<Effect>("GaussianBlur");
            Renderer.EffectEmissive = Content.Load<Effect>("AddEmissive");
            Renderer.bBloomEnabled = false;

            Renderer.AddDirectionalLight(new XDirectionalLight(Vector3.Normalize(Vector3.Down + Vector3.Right * 0.66f + Vector3.Forward * 0.33f), Color.CadetBlue, 0.75f, true));


            Renderer.AmbientLight = 0.1f;

            


            Renderer.ShadowBias = 0;
            

            BuildWorld();



            SelectedGeometry = null;
            crosshair = Content.Load<Texture2D>("Crosshair");
            UnprojectSource = new Vector3(GraphicsDevice.Viewport.Width * 0.5f, GraphicsDevice.Viewport.Height * 0.5f, 0.0f);

            Pylons = new List<Pylon>();

            FillNodes = new List<FloodFillNode>();

            Vertecies = new List<NavMeshVertex>();

            Edges = new List<NavMeshVertex>();

            RealEdges = new List<NavMeshVertex>();
            VerticesCopy = new List<NavMeshVertex>();

            Portals = new List<Portal>();
            PointedVertex = new List<NavMeshVertex>();

            RoundedVertex = new List<NavMeshVertex>();

            EdgeList = new List<Portal>();

            VertexAddedByPortalCreation = new List<NavMeshVertex>();

            NodeList = new List<ASNNavMesh>();
            NodePath = new List<ASNNavMesh>();

            CenterPointsList = new List<Vector3>();

            BoundingSpheres = new List<BoundingSphere>();

            debugSystem.DebugManager.Enabled = false;
            debugSystem.FpsCounter.Enabled = false;
            debugSystem.TimeRuler.Enabled = false;


            base.Initialize();
        }

        void BuildWorld()
        {
            Geom = new List<Geometry>();

            // Floor (is not geometry)
            CModel model = new CModel(Content.Load<Model>("UnitCube"), Vector3.Zero, Vector3.Zero, Vector3.One, GraphicsDevice, Content.Load<Effect>("DepthNormalDiffuse"));
            model.Scale = new Vector3(50, 1, 50);
            model.Position = new Vector3(0, -0.5f, 0);

            model.bTextureEnabled = false;
            model.DiffuseColor = Color.Gray;
            Renderer.AddModel(model);
            model.Update();

            FloorPlane = new Plane(Vector3.Up, 0.0f);


            float wallHeight = 4.0f;


            // Walls
            Geometry g = new Geometry(this);

            g.Scale = new Vector3(4, wallHeight, 50);
            g.Position = new Vector3(g.Scale.X * 0.5f + g.Scale.Z * 0.5f, wallHeight * 0.5f, 0);

            g.Model.bTextureEnabled = false;
            g.Model.DiffuseColor = Color.DarkGray;
            g.Model.Update();
            Geom.Add(g);

            /**************************/

            g = new Geometry(this);
            g.Scale = new Vector3(4, wallHeight, 50);
            g.Position = new Vector3(-g.Scale.X * 0.5f - g.Scale.Z * 0.5f, wallHeight * 0.5f, 0);

            g.Model.bTextureEnabled = false;
            g.Model.DiffuseColor = Color.DarkGray;
            g.Model.Update();
            Geom.Add(g);

            /**************************/

            g = new Geometry(this);
            g.Scale = new Vector3(50, wallHeight, 4);
            g.Position = new Vector3(0, wallHeight * 0.5f, g.Scale.X * 0.5f + g.Scale.Z * 0.5f);

            g.Model.bTextureEnabled = false;
            g.Model.DiffuseColor = Color.DarkGray;
            g.Model.Update();
            Geom.Add(g);

            /**************************/

            g = new Geometry(this);
            g.Scale = new Vector3(50, wallHeight, 4);
            g.Position = new Vector3(0, wallHeight * 0.5f, -g.Scale.X * 0.5f - g.Scale.Z * 0.5f);

            g.Model.bTextureEnabled = false;
            g.Model.DiffuseColor = Color.DarkGray;
            g.Model.Update();
            Geom.Add(g);


            // North
            g = new Geometry(this);
            g.Scale = new Vector3(1, 5, 1);
            g.Position = Vector3.Forward * 30;

            Geom.Add(g);
        }

        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        Ray UnprojectMouse()
        {
            UnprojectSource.Z = 0.0f;
            Vector3 v1 = GraphicsDevice.Viewport.Unproject(UnprojectSource, Camera.Proj, Camera.View, Matrix.Identity);

            UnprojectSource.Z = 1.0f;
            Vector3 v2 = GraphicsDevice.Viewport.Unproject(UnprojectSource, Camera.Proj, Camera.View, Matrix.Identity);

            return new Ray(Camera.Position, Vector3.Normalize(Camera.Forward));
            //return new Ray(Camera.Position, Vector3.Normalize(v2 - v1));
        }

        void CreateGeometry(Vector3 position)
        {
            Geometry g = new Geometry(this);

            g.Position = position;
            g.Scale = new Vector3(GeometryScale, 1.0f, GeometryScale);
            g.Unselect();
            Geom.Add(g);
        }

        void RemoveGeometry()
        {
            for (int i = 0; i < Geom.Count; i++)
            {
                if (Geom[i].bSelected)
                {
                    Renderer.RemoveModel(Geom[i].Model);
                    Geom.RemoveAt(i);
                    break;
                }
            }
        }
        void CreateSphere(Vector3 position)
        {
            BoundingSphere sphere = new BoundingSphere(position, 5.0f);
            BoundingSpheres.Add(sphere);
        }

        void ClearSphere()
        {
            BoundingSpheres.Clear();
        }

        void CreatePylon(Vector3 position)
        {
            Pylon p = new Pylon(this, 5.0f);

            p.Position = position;
            Pylons.Add(p);
        }

        void StartFilling()
        {

            // Create start node (check to see no collision)
            if (Pylons.Count > 0)
            {
                FillNodes.Clear();
                GC.Collect();

                Queue<FloodFillNode> queue = new Queue<FloodFillNode>();
                

                FloodFillNode ffn = new FloodFillNode();
                ffn.Position = Pylons[0].Position;

                queue.Enqueue(ffn);
                FillNodes.Add(ffn);

                int count = 0;

                while (queue.Count > 0 )
                {
                    ExpandFiller(queue.Dequeue(), queue);
                    count++;
                }
            }
        }


        NavMeshVertex FindOuterEdgeVertex(List<NavMeshVertex> list)
        {
            NavMeshVertex vertex = null;

            if (list.Count > 0)
            {
                vertex = list[0];

                for (int i = 1; i < list.Count; i++)
                {
                    if (vertex.Position.X < list[i].Position.X)
                    {
                        vertex = Vertecies[i];
                    }
                }
            }

            return vertex;
        }

        NavMeshVertex FindVertexAtPosition(Vector3 position)
        {
            for (int i = 0; i < Vertecies.Count; i++)
            {
                if (Vector3.DistanceSquared(position, Vertecies[i].Position) < 0.1f)
                {
                    return Vertecies[i];
                }
            }

            return null;
        }

        List<NavMeshVertex> FindAllVertexAtPosition(Vector3 position)
        {
            List<NavMeshVertex> list = new List<NavMeshVertex>();

            for (int i = 0; i < Vertecies.Count; i++)
            {
                if (Vector3.DistanceSquared(position, Vertecies[i].Position) < 0.1f)
                {
                    list.Add(Vertecies[i]);

                    if (list.Count == 2)
                        break;
                }
            }

            return list;

        }

        void CreateEdges2()
        {
            RealEdges.Clear();
            VerticesCopy.Clear();

            for (int i = 0; i < Vertecies.Count; i++)
            {
                VerticesCopy.Add(Vertecies[i]);
            }

            VertexCount = 0;

            Vector3 v1 = Vector3.Zero;
            Vector3 v2 = Vector3.Zero;
            float dot = 0;


            

            while (VerticesCopy.Count > 0)
            {
                NavMeshVertex start = VerticesCopy[0];
                RealEdges.Add(start);

                NavMeshVertex next = start;
                NavMeshVertex prev = null;
                NavMeshVertex prevPrev = null;

                while (next != null)
                {
                    VerticesCopy.Remove(next);
                    VertexCount++;

                    prevPrev = prev;

                    if (prev != null && next != null)
                        prev.Next = next;

                    prev = next;
                    next = null;

                    for (int i = 0; i < prev.Linked.Count; i++)
                    {
                        if (VerticesCopy.Contains(prev.Linked[i]))
                        {
                            
                            next = prev.Linked[i];
                            next.Prev = prev;
                        }
                    }

                    /*

                    if (prevPrev != null && next != null)
                    {
                        v1 = prev.Position - prevPrev.Position;
                        v2 = prev.Position - next.Position;
                        dot = Vector3.Dot(v1, v2);

                        if (dot < -0.98f)
                        {
                            prevPrev.Linked.Remove(prev);
                            prevPrev.Next = next;
                            prevPrev.Linked.Add(next);
                            next.Linked.Remove(prev);
                            next.Linked.Add(prevPrev);
                            next.Prev = prevPrev;
                            prev = prevPrev;
                            VertexCount--;
                        }
                    }
                    */

                    DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(prev.Position, 0.125f), Color.Chartreuse, 1.0f);

                    if (next == null)
                        break;

                    DebugShapeRenderer.AddLine(prev.Position, next.Position, Color.Aqua, 1.0f);
                }

                prev.Next = start;
                start.Prev = prev;

                #region Old stuff
                /*
                /// ---------- START + 1 ------- START ------ PREV ------ PREVPREV
                /// ------------------------------ X --------- X ----------- X -----
                // DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(start.Position, 0.55f), Color.Yellow, 5.0f);
                // DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(prev.Position, 0.55f), Color.Magenta, 5.0f);
                // DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(prevPrev.Position, 0.55f), Color.Cyan, 5.0f);

                next = start;
                v1 = prev.Position - prevPrev.Position;
                v2 = prev.Position - next.Position;
                dot = Vector3.Dot(v1, v2);

                if (dot < -0.98f)
                {
                    prevPrev.Linked.Remove(prev);
                    prevPrev.Linked.Add(next);
                    next.Linked.Remove(prev);
                    next.Linked.Add(prevPrev);
                    prev = prevPrev;
                    VertexCount--;
                }


                /// ---------- START + 1 ------- START ------ PREV ------ PREVPREV
                /// -------------- X ------------- X --------- X ------------------
                prevPrev = prev;
                prev = start;

                for (int i = 0; i < start.Linked.Count; i++)
                {
                    if (start.Linked[i] != prevPrev)
                        next = start.Linked[i];
                }

                // DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(next.Position, 0.25f), Color.Red, 5.0f);
                // DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(prev.Position, 0.25f), Color.Green, 5.0f);
                // DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(prevPrev.Position, 0.25f), Color.Blue, 5.0f);

                v1 = prev.Position - prevPrev.Position;
                v2 = prev.Position - next.Position;
                dot = Vector3.Dot(v1, v2);

                if (dot < -0.98f)
                {
                    prevPrev.Linked.Remove(prev);
                    prevPrev.Linked.Add(next);
                    next.Linked.Remove(prev);
                    next.Linked.Add(prevPrev);
                    prev = prevPrev;
                    VertexCount--;
                }

                // */

                #endregion

                DebugShapeRenderer.AddLine(prev.Position, start.Position, Color.BlueViolet, 1.0f);

                DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(start.Position, 0.25f), Color.Red, 1.0f);
            }
        }


        void CreateASNodes()
        {
            CenterPointsList.Clear();

            ASNNavMesh node = null;

            for (int i = 0; i < VerticesCopy.Count; i++)
            {
                node = new ASNNavMesh(VerticesCopy[i]);
                NodeList.Add(node);

                VerticesCopy[i].Node = node;
            }

            for (int i = 0; i < VertexAddedByPortalCreation.Count; i++)
            {
                node = new ASNNavMesh(VertexAddedByPortalCreation[i]);
                NodeList.Add(node);

                VertexAddedByPortalCreation[i].Node = node;
            }


            for (int i = 0; i < EdgeList.Count; i++)
            {
                EdgeList[i].Vertex1.Node.Neighbors.Add(EdgeList[i].Vertex2.Node);
                EdgeList[i].Vertex2.Node.Neighbors.Add(EdgeList[i].Vertex1.Node);
            }

            for (int i = 0; i < Portals.Count; i++)
            {
                Portals[i].Vertex1.Node.Neighbors.Add(Portals[i].Vertex2.Node);
                Portals[i].Vertex2.Node.Neighbors.Add(Portals[i].Vertex1.Node);
            }





            CreateNodesFromPortalCenterPoints();
        }

        void CreateNodesFromPortalCenterPoints()
        {
            NodePath.Clear();
            ASNNavMesh node;

            for (int i = 0; i < Portals.Count; i++)
            {
                node = new ASNNavMesh(Portals[i].Center);
                node.Vertex.Node = node;
            }
        }

        void DoMeshSearch()
        {
            //for (int j = 0; j < Portals.Count; j++)
            //{
                List<ASNNavMesh> NodesToConnectToCenter = new List<ASNNavMesh>();

                portalToSearch++;

                Portal p = Portals[portalToSearch % Portals.Count];

                if (p.Searched >= 2)
                    return;

                p.Searched++;

                NodesToConnectToCenter.Add(p.Center.Node);

                
                


                ASNNavMesh start = p.Vertex1.Node;
                ASNNavMesh goal = p.Vertex2.Node;

                start.Neighbors.Remove(goal);
                goal.Neighbors.Remove(start);

                //DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(start.Position, 0.55f), Color.Red, 10.0f);
                //DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(goal.Position, 0.55f), Color.Green, 10.0f);

                List<AStarNode> path = AStar.FindPathNavMesh(start, goal);

                Vector3 center = start.Position;
                //DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(start.Position, 0.25f), Color.LightSeaGreen, 10.0f);

                pathCount = path.Count;

                for (int i = 0; i < path.Count; i++)
                {
                    //DebugShapeRenderer.AddBoundingSphere(new BoundingSphere((path[i] as ASNNavMesh).Vertex.Position, 0.25f), new Color(0.2f * i, 1.0f, 0.2f * i, 1.0f), 10.0f);

                    center += (path[i] as ASNNavMesh).Vertex.Position;

                    if (i + 1 < path.Count)
                    {
                        for (int k = EdgeList.Count - 1; k >= 0; k--)
                        {
                            Portal edge = EdgeList[k];

                            if (((path[i] as ASNNavMesh).Vertex == edge.Vertex1 && (path[i + 1] as ASNNavMesh).Vertex == edge.Vertex2)
                                || ((path[i] as ASNNavMesh).Vertex == edge.Vertex2 && (path[i + 1] as ASNNavMesh).Vertex == edge.Vertex1))
                            {
                                edge.Vertex1.Node.Neighbors.Remove(edge.Vertex2.Node);
                                edge.Vertex2.Node.Neighbors.Remove(edge.Vertex1.Node);
                                //EdgeList.Remove(edge);
                            }
                        }

                        for (int k = Portals.Count - 1; k >= 0; k--)
                        {
                            Portal edge = Portals[k];

                            if (((path[i] as ASNNavMesh).Vertex == edge.Vertex1 && (path[i + 1] as ASNNavMesh).Vertex == edge.Vertex2)
                                || ((path[i] as ASNNavMesh).Vertex == edge.Vertex2 && (path[i + 1] as ASNNavMesh).Vertex == edge.Vertex1))
                            {
                                if (edge != p)
                                    edge.Searched++;

                                if (!NodesToConnectToCenter.Contains(edge.Center.Node))
                                    NodesToConnectToCenter.Add(edge.Center.Node);

                                
                                if (edge.Searched >= 2)
                                {
                                    edge.Vertex1.Node.Neighbors.Remove(edge.Vertex2.Node);
                                    edge.Vertex2.Node.Neighbors.Remove(edge.Vertex1.Node);
                                }
                                
                            }
                        }
                    }

                    for (int k = EdgeList.Count - 1; k >= 0; k--)
                    {
                        Portal edge = EdgeList[k];

                        if (((path[i] as ASNNavMesh).Vertex == edge.Vertex1 && start.Vertex == edge.Vertex2)
                            || ((path[i] as ASNNavMesh).Vertex == edge.Vertex2 && start.Vertex == edge.Vertex1))
                        {
                            edge.Vertex1.Node.Neighbors.Remove(edge.Vertex2.Node);
                            edge.Vertex2.Node.Neighbors.Remove(edge.Vertex1.Node);
                            // EdgeList.Remove(edge);
                        }
                    }

                    for (int k = Portals.Count - 1; k >= 0; k--)
                    {
                        Portal edge = Portals[k];

                        if (((path[i] as ASNNavMesh).Vertex == edge.Vertex1 && start.Vertex == edge.Vertex2)
                            || ((path[i] as ASNNavMesh).Vertex == edge.Vertex2 && start.Vertex == edge.Vertex1))
                        {
                            if (edge != p)
                                edge.Searched++;

                            if (!NodesToConnectToCenter.Contains(edge.Center.Node))
                                    NodesToConnectToCenter.Add(edge.Center.Node);
                            
                            /*
                            if (edge.Searched >= 2)
                            {
                                edge.Vertex1.Node.Neighbors.Remove(edge.Vertex2.Node);
                                edge.Vertex2.Node.Neighbors.Remove(edge.Vertex1.Node);
                            }
                            */
                            
                        }
                    }

                }

                center /= path.Count + 1;

                CenterPointsList.Add(center);

                ASNNavMesh node = new ASNNavMesh(new NavMeshVertex());
                node.Vertex.Position = center;
                node.Position = center;

                for (int i = 0; i < NodesToConnectToCenter.Count; i++)
                {
                    node.Neighbors.Add(NodesToConnectToCenter[i]);
                }

                NodePath.Add(node);


                //DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(center, 0.25f), Color.SpringGreen, 10.0f);

                start.Neighbors.Add(goal);
                goal.Neighbors.Add(start);

                for (int i = 0; i < NodeList.Count; i++)
                {
                    NodeList[i].bInClosed = false;
                    NodeList[i].bInOpen = false;
                    
                }
            //}

            //for (int j = 0; j < Portals.Count; j++)
            //{
                //Portals[j].Searched = 0;
            //}

                
                if (portalToSearch >= Portals.Count * 2)
                {
                    portalToSearch = 0;
                    for (int j = 0; j < Portals.Count; j++)
                    {
                        Portals[j].Searched = 0;
                    }
                }
        }


        void CreatePortals()
        {

            Vector3 v1;
            Vector3 v2;
            float dot;

            PointedVertex.Clear();
            RoundedVertex.Clear();
            VertexAddedByPortalCreation.Clear();

            for (int i = 0; i < VerticesCopy.Count; i++)
            {
                // var signed = (p2.x - p1.x) * (p3.y - p1.y) - (p3.x - p1.x) * (p2.y - p1.y);
                // DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(VerticesCopy[i].Position, 0.25f), Color.LightSeaGreen, 10.0f);

                float signed = (VerticesCopy[i].Position.X - VerticesCopy[i].Prev.Position.X)
                                * (VerticesCopy[i].Next.Position.Z - VerticesCopy[i].Prev.Position.Z)
                                - (VerticesCopy[i].Next.Position.X - VerticesCopy[i].Prev.Position.X)
                                * (VerticesCopy[i].Position.Z - VerticesCopy[i].Prev.Position.Z);

                
                v1 = VerticesCopy[i].Position - VerticesCopy[i].Next.Position;
                v2 = VerticesCopy[i].Position - VerticesCopy[i].Prev.Position;

                dot = Vector3.Dot(v1, v2);

                if (VerticesCopy[i].EdgeIndex == 0)
                {
                    if (signed > 0)
                    {
                        PointedVertex.Add(VerticesCopy[i]);
                    }
                    else
                    {
                        RoundedVertex.Add(VerticesCopy[i]);
                    }
                }
                else
                {
                    if (signed > 0)
                    {
                        PointedVertex.Add(VerticesCopy[i]);
                    }
                    else
                    {
                        RoundedVertex.Add(VerticesCopy[i]);
                    }
                }
            }

            for (int i = 0; i < PointedVertex.Count; i++)
            {
                DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(PointedVertex[i].Position, 0.25f), Color.Green, 10.0f);
            }

            for (int i = 0; i < RoundedVertex.Count; i++)
            {
                DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(RoundedVertex[i].Position, 0.25f), Color.Red, 10.0f);
            }



            Portals.Clear();

            int type = -1;
            int vertext = 0;
            int edge = 1;
            int portalSingle = 2;
            int portalDouble = 3;



            float distance = float.MaxValue;
            float distance2 = float.MaxValue;
            NavMeshVertex connVertex = null;
            Portal connPortal = null;
            Portal connEdge = null;

            Vector3 interestNormal;
            float interestDotLimit;
            Vector3 vPrev;
            Vector3 vNext;
            Vector3 testpointToPointed;
            float testpointDot;

            Vector3 projectedPoint; // Point projected onto line
            Vector3 lineDir;        // direction of line, normalized
            Vector3 pointMod;       // Point to project adjusted to one of the line segments vertices
            float ADotB;            // (ADotB / lineDir.LengthSquared()) * lineDir == projection
            float len;              // lineDir.LengthSquared()
            float num;              // ADotB / len

            float relaxation = 0.05f;

            
            //for (int i = 0; i < PointedVertex.Count; i++)
            for (int i = PointedVertex.Count - 1; i >= 0; i--)
            {
                vPrev = Vector3.Normalize(PointedVertex[i].Position - PointedVertex[i].Prev.Position);
                vNext = Vector3.Normalize(PointedVertex[i].Position - PointedVertex[i].Next.Position);

                interestNormal = Vector3.Normalize(vPrev + vNext);


                #region Check Vertex
                for (int j = 0; j < VerticesCopy.Count; j++)
                {
                    if (VerticesCopy[j] != PointedVertex[i])
                    {
                        testpointToPointed = Vector3.Normalize(PointedVertex[i].Position - VerticesCopy[j].Position);
                        testpointDot = Vector3.Dot(interestNormal, testpointToPointed);
                        interestDotLimit = Vector3.Dot(interestNormal, vPrev);

                        if (testpointDot <= -interestDotLimit + relaxation)
                        {
                            distance2 = Vector3.Distance(PointedVertex[i].Position, VerticesCopy[j].Position);
                            if (distance2 < distance)
                            {
                                distance = distance2;
                                type = vertext;
                                connVertex = VerticesCopy[j];
                            }
                        }
                    }
                }

            #endregion

                // Check edges
                #region Check Edges
                for (int j = 0; j < EdgeList.Count; j++)
                {
                    lineDir = Vector3.Normalize(EdgeList[j].Vertex1.Position - EdgeList[j].Vertex2.Position);
                    pointMod = PointedVertex[i].Position - EdgeList[j].Vertex1.Position;
                    ADotB = Vector3.Dot(lineDir, pointMod);
                    len = lineDir.LengthSquared();
                    num = ADotB / len;

                    projectedPoint = (lineDir * num) + EdgeList[j].Vertex1.Position;

                    distance2 = Vector3.Distance(projectedPoint, PointedVertex[i].Position);

                    

                    if (distance2 < distance)
                    {

                        float len1 = Vector3.Distance(projectedPoint, EdgeList[j].Vertex1.Position);
                        float len2 = Vector3.Distance(projectedPoint, EdgeList[j].Vertex2.Position);
                        float len3 = Vector3.Distance(EdgeList[j].Vertex2.Position, EdgeList[j].Vertex1.Position);

                        if ((len3 > len1) && (len3 > len2))
                        {
                            // Projected point is on the line

                            testpointToPointed = Vector3.Normalize(PointedVertex[i].Position - projectedPoint);
                            testpointDot = Vector3.Dot(interestNormal, testpointToPointed);
                            interestDotLimit = Vector3.Dot(interestNormal, vPrev);


                            if (testpointDot <= -interestDotLimit + relaxation)
                            {
                                // Projected point is within interest
                                type = edge;
                                connEdge = EdgeList[j];
                                distance = distance2;

                                connVertex = new NavMeshVertex();
                                connVertex.Position = projectedPoint;
                                
                            }
                            else
                            {
                                // Projected point is not in interest
                                testpointToPointed = Vector3.Normalize(PointedVertex[i].Position - EdgeList[j].Vertex1.Position);
                                testpointDot = Vector3.Dot(interestNormal, testpointToPointed);
                                interestDotLimit = Vector3.Dot(interestNormal, vPrev);

                                if (testpointDot <= -interestDotLimit + relaxation)
                                {
                                    // Edge vertex 1 is in interest
                                    type = edge;
                                    connEdge = EdgeList[j];
                                    distance = Vector3.Distance(PointedVertex[i].Position, EdgeList[j].Vertex1.Position);

                                    connVertex = EdgeList[j].Vertex1;
                                }
                                else
                                {
                                    testpointToPointed = Vector3.Normalize(PointedVertex[i].Position - EdgeList[j].Vertex2.Position);
                                    testpointDot = Vector3.Dot(interestNormal, testpointToPointed);
                                    interestDotLimit = Vector3.Dot(interestNormal, vPrev);

                                    if (testpointDot <= -interestDotLimit + relaxation)
                                    {
                                        type = edge;
                                        connEdge = EdgeList[j];
                                        distance = Vector3.Distance(PointedVertex[i].Position, EdgeList[j].Vertex2.Position);

                                        connVertex = EdgeList[j].Vertex2;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Projected point is not within segment
                        }

                    }

                }
                #endregion


                // Project point
                    // Get the distance
                        // check if distance is closer then previous best
                        // if better check if point is within edge points
                            // if projected point distance to A 


                // Check portals
                bool enablePortals = true;
                #region Check Portals
                if (enablePortals)
                {
                    for (int j = 0; j < Portals.Count; j++)
                    {
                        lineDir = Vector3.Normalize(Portals[j].Vertex1.Position - Portals[j].Vertex2.Position);
                        pointMod = PointedVertex[i].Position - Portals[j].Vertex1.Position;
                        ADotB = Vector3.Dot(lineDir, pointMod);
                        len = lineDir.LengthSquared();
                        num = ADotB / len;

                        projectedPoint = (lineDir * num) + Portals[j].Vertex1.Position;

                        distance2 = Vector3.Distance(projectedPoint, PointedVertex[i].Position);

                        if (distance2 < distance)
                        {
                            // Projected point is closest
                            float len1 = Vector3.Distance(projectedPoint, Portals[j].Vertex1.Position);
                            float len2 = Vector3.Distance(projectedPoint, Portals[j].Vertex2.Position);
                            float len3 = Vector3.Distance(Portals[j].Vertex2.Position, Portals[j].Vertex1.Position);

                            if ((len3 > len1) && (len3 > len2))
                            {
                                // Projected point is within edge


                                // Is Portal Vertex 1 within interest
                                testpointToPointed = Vector3.Normalize(PointedVertex[i].Position - Portals[j].Vertex1.Position);
                                testpointDot = Vector3.Dot(interestNormal, testpointToPointed);
                                interestDotLimit = Vector3.Dot(interestNormal, vPrev);

                                if (testpointDot <= -interestDotLimit + relaxation)
                                {
                                    type = portalSingle;
                                    connVertex = Portals[j].Vertex1;
                                    distance = Vector3.Distance(PointedVertex[i].Position, connVertex.Position);

                                }
                                else
                                {
                                    // Is portal Vertex 2 within interest
                                    testpointToPointed = Vector3.Normalize(PointedVertex[i].Position - Portals[j].Vertex2.Position);
                                    testpointDot = Vector3.Dot(interestNormal, testpointToPointed);
                                    interestDotLimit = Vector3.Dot(interestNormal, vPrev);

                                    if (testpointDot <= -interestDotLimit + relaxation)
                                    {
                                        type = portalSingle;
                                        connVertex = Portals[j].Vertex2;
                                        distance = Vector3.Distance(PointedVertex[i].Position, connVertex.Position);
                                    }
                                    else
                                    {
                                        testpointToPointed = Vector3.Normalize(PointedVertex[i].Position - projectedPoint);
                                        testpointDot = Vector3.Dot(interestNormal, testpointToPointed);
                                        interestDotLimit = Vector3.Dot(interestNormal, vPrev);

                                        if (testpointDot <= -interestDotLimit + relaxation)
                                        {
                                            // Projected point is within interest
                                            type = portalDouble;
                                            connPortal = Portals[j];
                                            distance = distance2;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion



                // temp
                if (type == vertext)
                {
                    Portals.Add(new Portal(PointedVertex[i], connVertex));
                }
                else if (type == edge)
                {
                    Portals.Add(new Portal(PointedVertex[i], connVertex));

                    connEdge.Vertex1.Next = connVertex;
                    connEdge.Vertex2.Prev = connVertex;
                    connVertex.Prev = connEdge.Vertex1;
                    connVertex.Next = connEdge.Vertex2;

                    VertexAddedByPortalCreation.Add(connVertex);

                    Portal newEdge1 = new Portal(connVertex.Prev, connVertex);
                    Portal newEdge2 = new Portal(connVertex, connVertex.Next);

                    EdgeList.Remove(connEdge);
                    EdgeList.Add(newEdge1);
                    EdgeList.Add(newEdge2);
                }
                else if (type == portalDouble)
                {
                    Portals.Add(new Portal(PointedVertex[i], connPortal.Vertex1));
                    Portals.Add(new Portal(PointedVertex[i], connPortal.Vertex2));
                }
                else if (type == portalSingle)
                {
                    Portals.Add(new Portal(PointedVertex[i], connVertex));
                }



                // Reset values for next notch to test
                type = -1;
                distance = float.MaxValue;
                distance2 = float.MaxValue;

                
                // Get interest normal
                    // Get Prev -> V
                    // Get Next -> V
                    
                // Get interest dot limit

            }

            removedPortals = 0;
            for (int i = 0; i < Portals.Count; i++)
            {
                for (int j = Portals.Count - 1; j > i; j--)
                {
                    if (Portals[i] != Portals[j])
                    {
                        if ((Portals[i].Vertex1 == Portals[j].Vertex1 && Portals[i].Vertex2 == Portals[j].Vertex2)
                            || (Portals[i].Vertex1 == Portals[j].Vertex2 && Portals[i].Vertex2 == Portals[j].Vertex1))
                        {
                            Portals.RemoveAt(j);
                            removedPortals++;
                        }
                    }
                }
            }


            for (int k = 0; k < Portals.Count; k++)
            {
                DebugShapeRenderer.AddLine(Portals[k].Vertex1.Position, Portals[k].Vertex2.Position, Color.CadetBlue, 20.0f);
            }


        }

        void CleanEdges()
        {
            for (int i = RealEdges.Count - 1; i >= 0; i--)
            {
                Vector3 v1;
                Vector3 v2;
                float dot;

                NavMeshVertex vertex = RealEdges[i];
                NavMeshVertex start = vertex;

                while (vertex.Next != start && vertex.Next != null)
                {

                    if (vertex.Next != null)
                    {
                        // WORKS!!!
                        /*
                        v1 = vertex.Position - vertex.Next.Position;
                        v2 = vertex.Position - vertex.Prev.Position;
                        */

                        // TEST
                        v1 = Vector3.Normalize(vertex.Position - vertex.Next.Position);
                        v2 = Vector3.Normalize(vertex.Position - vertex.Prev.Position);

                        
                        dot = Vector3.Dot(v1, v2);

                        if (dot < -0.95f)
                        {
                            NavMeshVertex temp = vertex;
                            if (start == temp)
                            {
                                RealEdges.Remove(start);
                                start = temp.Prev;
                                RealEdges.Add(start);
                            }

                            vertex = temp.Prev;

                            NavMeshVertex next = temp.Next;
                            NavMeshVertex prev = temp.Prev;

                            next.Prev = prev;
                            prev.Next = next;
                        }

                        vertex = vertex.Next;
                    }
                }

                v1 = vertex.Position - vertex.Next.Position;
                v2 = vertex.Position - vertex.Prev.Position;

                dot = Vector3.Dot(v1, v2);

                if (dot < -0.95f)
                {
                    NavMeshVertex temp = vertex;
                    if (start == temp)
                    {
                        RealEdges.Remove(start);
                        start = temp.Prev;
                        RealEdges.Add(start);
                    }

                    vertex = temp.Prev;

                    NavMeshVertex next = temp.Next;
                    NavMeshVertex prev = temp.Prev;

                    next.Prev = prev;
                    prev.Next = next;
                }
            }
        }

        void ShowEdges()
        {
            float showTime = 4.0f;

            VerticesCopy.Clear();
            EdgeList.Clear();

            for (int i = 0; i < RealEdges.Count; i++)
            {

                NavMeshVertex next = RealEdges[i];
                NavMeshVertex start = next;

                DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(start.Position, 0.25f), Color.Red, showTime);

                while (next.Next != start && next.Next != null)
                {
                    VerticesCopy.Add(next);
                    next.EdgeIndex = i;

                    DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(next.Position, 0.125f), Color.HotPink, showTime);

                    if (next.Next != null)
                    {
                        DebugShapeRenderer.AddLine(next.Position, next.Next.Position, Color.LightBlue, showTime);
                        EdgeList.Add(new Portal(next, next.Next));
                        next = next.Next;
                    }
                    else
                        break;
                }

                VerticesCopy.Add(next);

                DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(next.Position, 0.125f), Color.HotPink, showTime);
                DebugShapeRenderer.AddLine(next.Position, next.Next.Position, Color.LightBlue, showTime);
                EdgeList.Add(new Portal(next, next.Next));
            }


            #region Old
            /*
            NavMeshVertex next;
            NavMeshVertex prev = null;
            NavMeshVertex prevPrev = null;

            List<NavMeshVertex> list = new List<NavMeshVertex>();

            for (int i = 0; i < RealEdges.Count; i++)
            {
                next = RealEdges[i];

                list.Add(next);

                while (next != null)
                {
                    prevPrev = prev;
                    prev = next;
                    next = null;

                    for (int j = 0; j < prev.Linked.Count; j++)
                    {
                        if (!list.Contains(prev.Linked[j]))
                        {
                            next = prev.Linked[j];
                            list.Add(next);
                        }
                    }

                    if (next != null)
                    {
                        DebugShapeRenderer.AddLine(prev.Position, next.Position, Color.Cornsilk, 6.0f);
                    }
                    else
                    {
                        break;
                    }
                }

                DebugShapeRenderer.AddLine(prev.Position, RealEdges[i].Position, Color.MintCream);
            }
            // */
            #endregion
        }

        void CreateEdges()
        {
            RealEdges.Clear();
            VerticesCopy.Clear();

            for (int i = 0; i < Vertecies.Count; i++)
            {
                VerticesCopy.Add(Vertecies[i]);
            }

            while (VerticesCopy.Count > 0)
            {
                NavMeshVertex start = VerticesCopy[0];
                RealEdges.Add(start);

                NavMeshVertex next = start;
                NavMeshVertex prev = null;

                while (next != null)
                {
                    VerticesCopy.Remove(next);

                    prev = next;
                    next = null;

                    for (int i = 0; i < prev.Linked.Count; i++)
                    {
                        if (VerticesCopy.Contains(prev.Linked[i]))
                        {
                            next = prev.Linked[i];
                        }
                    }

                    DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(prev.Position, 0.125f), Color.Chartreuse, 5.0f);

                    if (next == null)
                        break;

                    DebugShapeRenderer.AddLine(prev.Position, next.Position, Color.Aqua, 5.0f);
                }

                DebugShapeRenderer.AddLine(prev.Position, start.Position, Color.BlueViolet, 5.0f);
            }
        }

        /// <summary>
        /// Displays debug shapes for connected vertices.
        /// </summary>
        void ShowAllConnections()
        {
            for (int i = 0; i < Vertecies.Count; i++)
            {
                for (int j = 0; j < Vertecies[i].Linked.Count; j++)
                {
                    DebugShapeRenderer.AddLine(Vertecies[i].Position, Vertecies[i].Linked[j].Position, Color.Ivory, 10.0f);
                }
            }
        }


        void AddVertex(int index1, int index2, int i)
        {
            

            NavMeshVertex vtex1;
            NavMeshVertex vtex2;

            vtex1 = FindVertexAtPosition(FillNodes[i].Corners[index1].Position);
            if (vtex1 == null)
                Vertecies.Add(FillNodes[i].Corners[index1]);

            vtex2 = FindVertexAtPosition(FillNodes[i].Corners[index2].Position);
            if (vtex2 == null)
                Vertecies.Add(FillNodes[i].Corners[index2]);


            if (vtex1 == null)
            {
                if (vtex2 == null)
                {
                    FillNodes[i].Corners[index1].Linked.Add(FillNodes[i].Corners[index2]);
                    FillNodes[i].Corners[index2].Linked.Add(FillNodes[i].Corners[index1]);
                }
                else
                {
                    FillNodes[i].Corners[index1].Linked.Add(vtex2);
                    vtex2.Linked.Add(FillNodes[i].Corners[index1]);
                }
            }
            else
            {
                if (vtex2 == null)
                {
                    vtex1.Linked.Add(FillNodes[i].Corners[index2]);
                    FillNodes[i].Corners[index2].Linked.Add(vtex1);
                }
                else
                {
                    vtex1.Linked.Add(vtex2);
                    vtex2.Linked.Add(vtex1);
                }
            }
        }

        void CreateMesh()
        {
            // Go through all nodes
            // If collision on a side, remove the 4 joining vertices

            // Grab the first node and vertices

            bool forward = false;
            bool backward = false;
            bool right = false;
            bool left = false;

            if (Pylons.Count > 0)
            {

                Vector3 point = Pylons[0].Position;

                Vertecies.Clear();

                List<int> indexes = new List<int>();

                for (int i = FillNodes.Count - 1; i >= 0; i--)
                {
                    point = FillNodes[i].Position;

                    int count = 0;

                    forward = false;
                    backward = false;
                    right = false;
                    left = false;

                    for (int j = 0; j < FillNodes.Count; j++)
                    {

                        if (FillNodes[i] != FillNodes[j])
                        {
                            

                            #region Collide with nodes
                            if (!forward)
                                if (FillNodes[j].AABB.Contains(point + Vector3.Forward * FloodFillNode.Size) != ContainmentType.Disjoint)
                                {
                                    count++;
                                    forward = true;
                                }
                            
                            if (!backward)
                                if (FillNodes[j].AABB.Contains(point + Vector3.Backward * FloodFillNode.Size) != ContainmentType.Disjoint)
                                {
                                    count++;
                                    backward = true;

                                }

                            if (!right)
                                if (FillNodes[j].AABB.Contains(point + Vector3.Right * FloodFillNode.Size) != ContainmentType.Disjoint)
                                {
                                    count++;
                                    right = true;

                                }

                            if (!left)
                                if (FillNodes[j].AABB.Contains(point + Vector3.Left * FloodFillNode.Size) != ContainmentType.Disjoint)
                                {
                                    count++;
                                    left = true;

                                }

                            if (FillNodes[j].AABB.Contains(point + (Vector3.Forward + Vector3.Right) * FloodFillNode.Size) != ContainmentType.Disjoint)
                            {
                                count++;

                            }
                            if (FillNodes[j].AABB.Contains(point + (Vector3.Forward + Vector3.Left) * FloodFillNode.Size) != ContainmentType.Disjoint)
                            {
                                count++;

                            }
                            if (FillNodes[j].AABB.Contains(point + (Vector3.Backward + Vector3.Right) * FloodFillNode.Size) != ContainmentType.Disjoint)
                            {
                                count++;

                            }
                            if (FillNodes[j].AABB.Contains(point + (Vector3.Backward + Vector3.Left) * FloodFillNode.Size) != ContainmentType.Disjoint)
                            {
                                count++;

                            }
                            #endregion
                        }
                    }

                    NavMeshVertex vtex1;
                    NavMeshVertex vtex2;

                    #region Third times a charm

                    if (!forward)
                    {
                        #region Forward
                        AddVertex(0, 1, i);
                        #endregion
                    }

                    if (!backward)
                    {
                        #region Backward
                        AddVertex(2, 3, i);
                        #endregion
                    }

                    if (!right)
                    {
                        #region Right
                        AddVertex(1, 2, i);
                        #endregion
                    }

                    if (!left)
                    {
                        #region Left
                        AddVertex(3, 0, i);
                        #endregion
                    }
                    


                    #endregion


                    #region Add All
                    /*
                    if (!forward)
                    {
                        //if (!Vertecies.Contains(FillNodes[i].Corners[0]))
                            Vertecies.Add(FillNodes[i].Corners[0]);

                        // if (!Vertecies.Contains(FillNodes[i].Corners[1]))
                            Vertecies.Add(FillNodes[i].Corners[1]);

                        FillNodes[i].Corners[0].Linked.Add(FillNodes[i].Corners[1]);
                        FillNodes[i].Corners[1].Linked.Add(FillNodes[i].Corners[0]);

                    }

                    if (!backward)
                    {
                        // if (!Vertecies.Contains(FillNodes[i].Corners[2]))
                            Vertecies.Add(FillNodes[i].Corners[2]);
                            // if (!Vertecies.Contains(FillNodes[i].Corners[3]))
                            Vertecies.Add(FillNodes[i].Corners[3]);

                        FillNodes[i].Corners[2].Linked.Add(FillNodes[i].Corners[3]);
                        FillNodes[i].Corners[3].Linked.Add(FillNodes[i].Corners[2]);
                    }

                    if (!right)
                    {
                        // if (!Vertecies.Contains(FillNodes[i].Corners[1]))
                            Vertecies.Add(FillNodes[i].Corners[1]);

                            // if (!Vertecies.Contains(FillNodes[i].Corners[2]))
                            Vertecies.Add(FillNodes[i].Corners[2]);

                        FillNodes[i].Corners[1].Linked.Add(FillNodes[i].Corners[2]);
                        FillNodes[i].Corners[2].Linked.Add(FillNodes[i].Corners[1]);
                    }

                    if (!left)
                    {
                        // if (!Vertecies.Contains(FillNodes[i].Corners[3]))
                            Vertecies.Add(FillNodes[i].Corners[3]);
                            // if (!Vertecies.Contains(FillNodes[i].Corners[0]))
                            Vertecies.Add(FillNodes[i].Corners[0]);

                        FillNodes[i].Corners[3].Linked.Add(FillNodes[i].Corners[0]);
                        FillNodes[i].Corners[0].Linked.Add(FillNodes[i].Corners[3]);

                    }

                    */
                    #endregion

                    #region No Double Vertex
                    // /*
                    if (!forward)
                    {
                        #region Forward
                        vtex1 = FindVertexAtPosition(FillNodes[i].Corners[0].Position);
                        if (vtex1 == null)
                            Vertecies.Add(FillNodes[i].Corners[0]);

                        vtex2 = FindVertexAtPosition(FillNodes[i].Corners[1].Position);
                        if (vtex2 == null)
                            Vertecies.Add(FillNodes[i].Corners[1]);


                        if (vtex1 == null)
                        {
                            if (vtex2 == null)
                            {
                                FillNodes[i].Corners[0].Linked.Add(FillNodes[i].Corners[1]);
                                FillNodes[i].Corners[1].Linked.Add(FillNodes[i].Corners[0]);
                            }
                            else
                            {
                                FillNodes[i].Corners[0].Linked.Add(vtex2);
                                vtex2.Linked.Add(FillNodes[i].Corners[0]);
                            }
                        }
                        else
                        {
                            if (vtex2 == null)
                            {
                                vtex1.Linked.Add(FillNodes[i].Corners[1]);
                                FillNodes[i].Corners[1].Linked.Add(vtex1);
                            }
                            else
                            {
                                vtex1.Linked.Add(vtex2);
                                vtex2.Linked.Add(vtex1);
                            }
                        }
                        #endregion
                    }

                    if (!backward)
                    {
                        #region Backward
                        vtex1 = FindVertexAtPosition(FillNodes[i].Corners[2].Position);
                        if (vtex1 == null)
                            Vertecies.Add(FillNodes[i].Corners[2]);

                        vtex2 = FindVertexAtPosition(FillNodes[i].Corners[3].Position);
                        if (vtex2 == null)
                            Vertecies.Add(FillNodes[i].Corners[3]);


                        if (vtex1 == null)
                        {
                            if (vtex2 == null)
                            {
                                FillNodes[i].Corners[2].Linked.Add(FillNodes[i].Corners[3]);
                                FillNodes[i].Corners[3].Linked.Add(FillNodes[i].Corners[2]);
                            }
                            else
                            {
                                FillNodes[i].Corners[2].Linked.Add(vtex2);
                                vtex2.Linked.Add(FillNodes[i].Corners[2]);
                            }
                        }
                        else
                        {
                            if (vtex2 == null)
                            {
                                vtex1.Linked.Add(FillNodes[i].Corners[3]);
                                FillNodes[i].Corners[3].Linked.Add(vtex1);
                            }
                            else
                            {
                                vtex1.Linked.Add(vtex2);
                                vtex2.Linked.Add(vtex1);
                            }
                        }
                        #endregion
                    }

                    if (!right)
                    {
                        #region Right
                        vtex1 = FindVertexAtPosition(FillNodes[i].Corners[1].Position);
                        if (vtex1 == null)
                            Vertecies.Add(FillNodes[i].Corners[1]);

                        vtex2 = FindVertexAtPosition(FillNodes[i].Corners[2].Position);
                        if (vtex2 == null)
                            Vertecies.Add(FillNodes[i].Corners[2]);


                        if (vtex1 == null)
                        {
                            if (vtex2 == null)
                            {
                                FillNodes[i].Corners[1].Linked.Add(FillNodes[i].Corners[2]);
                                FillNodes[i].Corners[2].Linked.Add(FillNodes[i].Corners[1]);
                            }
                            else
                            {
                                FillNodes[i].Corners[1].Linked.Add(vtex2);
                                vtex2.Linked.Add(FillNodes[i].Corners[1]);
                            }
                        }
                        else
                        {
                            if (vtex2 == null)
                            {
                                vtex1.Linked.Add(FillNodes[i].Corners[2]);
                                FillNodes[i].Corners[2].Linked.Add(vtex1);
                            }
                            else
                            {
                                vtex1.Linked.Add(vtex2);
                                vtex2.Linked.Add(vtex1);
                            }
                        }
                        #endregion
                    }

                    if (!left)
                    {
                        #region Left
                        vtex1 = FindVertexAtPosition(FillNodes[i].Corners[3].Position);
                        if (vtex1 == null)
                            Vertecies.Add(FillNodes[i].Corners[3]);

                        vtex2 = FindVertexAtPosition(FillNodes[i].Corners[0].Position);
                        if (vtex2 == null)
                            Vertecies.Add(FillNodes[i].Corners[0]);


                        if (vtex1 == null)
                        {
                            if (vtex2 == null)
                            {
                                FillNodes[i].Corners[3].Linked.Add(FillNodes[i].Corners[0]);
                                FillNodes[i].Corners[0].Linked.Add(FillNodes[i].Corners[3]);
                            }
                            else
                            {
                                FillNodes[i].Corners[3].Linked.Add(vtex2);
                                vtex2.Linked.Add(FillNodes[i].Corners[3]);
                            }
                        }
                        else
                        {
                            if (vtex2 == null)
                            {
                                vtex1.Linked.Add(FillNodes[i].Corners[0]);
                                FillNodes[i].Corners[0].Linked.Add(vtex1);
                            }
                            else
                            {
                                vtex1.Linked.Add(vtex2);
                                vtex2.Linked.Add(vtex1);
                            }
                        }
                        #endregion
                    }
                    // */
#endregion

                    if (count >= 8)
                    {

                        indexes.Add(i);
                        // FillNodes.RemoveAt(i);
                    }
                }

                for (int i = 0; i < indexes.Count; i++)
                {
                    //FillNodes.RemoveAt(indexes[i]);
                }

                
            }

        }

        void MergeVertexes()
        {
            // Grab a vertex
                // gather all vertex at that position
                // transfer all links to vertex A
                // Remove all other vertex

            if (Vertecies.Count > 0)
            {
                
                NavMeshVertex v = Vertecies[0];

                List<NavMeshVertex> list = new List<NavMeshVertex>();

                list = FindAllVertexAtPosition(v.Position);

                NavMeshVertex A = list[0];

                for (int i = 1; i < list.Count; i++)
                {
                    NavMeshVertex C = list[i];

                    for (int j = C.Linked.Count - 1; j >= 0; j--)
                    {
                        C.Linked[j].Linked.Remove(C);
                        C.Linked[j].Linked.Add(A);

                        A.Linked.Add(C.Linked[j]);

                        C.Linked.RemoveAt(j);
                    }
                }
                
                Edges.Add(Vertecies[0]);
            }
        }

        void SimplifyEdges()
        {
            #region Simplify edges
            //*
            if (Vertecies.Count > 0)
            {
                for (int i = Vertecies.Count - 1; i >= 0; i--)
                {
                    if (Vertecies[i].Linked.Count > 1)
                    {
                        if (Vector3.Dot(Vertecies[i].Position - Vertecies[i].Linked[0].Position, Vertecies[i].Position - Vertecies[i].Linked[1].Position) < -0.99f)
                        {
                            Vertecies[i].Linked[0].Linked.Remove(Vertecies[i]);
                            Vertecies[i].Linked[1].Linked.Remove(Vertecies[i]);
                            Vertecies[i].Linked[0].Linked.Add(Vertecies[i].Linked[1]);
                            Vertecies[i].Linked[1].Linked.Add(Vertecies[i].Linked[0]);

                            Vertecies.RemoveAt(i);
                        }
                    }
                }

            }
            // */
            #endregion
        }

        void ExpandFiller(FloodFillNode node, Queue<FloodFillNode> que)
        {
            bool collision = false;

            for (int i = 0; i < FillNodes.Count; i++)
            {
                if (FillNodes[i] == node)
                    continue;

                if (FillNodes[i].AABB.Intersects(node.AABB))
                {
                    if (FillNodes.Contains(node))
                        FillNodes.Remove(node);
                    
                    collision = true;

                    break;
                }
            }

            for (int i = 0; i < Geom.Count; i++)
            {
               
                if (node.AABB.Intersects(Geom[i].AABB))
                {
                    if (FillNodes.Contains(node))
                        FillNodes.Remove(node);

                    collision = true;

                    break;

                }
            }

            for (int i = 0; i < BoundingSpheres.Count; i++)
            {
                if (node.AABB.Intersects(BoundingSpheres[i]))
                {
                    if (FillNodes.Contains(node))
                        FillNodes.Remove(node);

                    collision = true;

                    break;
                }
            }

            if (!collision)
            {
                

                FloodFillNode north = new FloodFillNode();
                north.Position = node.Position + Vector3.UnitZ * -FloodFillNode.Size;
                que.Enqueue(north);
                FillNodes.Add(north);
                //ExpandFiller(north, que);

                FloodFillNode south = new FloodFillNode();
                south.Position = node.Position + Vector3.UnitZ * FloodFillNode.Size;
                que.Enqueue(south);
                FillNodes.Add(south);
                //ExpandFiller(south, que);

                FloodFillNode east = new FloodFillNode();
                east.Position = node.Position + Vector3.UnitX * FloodFillNode.Size;
                que.Enqueue(east);
                FillNodes.Add(east);
                //ExpandFiller(east, que);

                FloodFillNode west = new FloodFillNode();
                west.Position = node.Position + Vector3.UnitX * -FloodFillNode.Size;
                que.Enqueue(west);
                FillNodes.Add(west);
                //ExpandFiller(west, que);
            }
        }

        bool SelectGeometry(Ray r)
        {
            bool selected = false;

            int index = -1;
            float dist1 = float.MaxValue;
            float dist2 = float.MaxValue;

            for (int i = 0; i < Geom.Count; i++)
            {
                float? hit = r.Intersects(Geom[i].AABB);

                if (hit.HasValue)
                {
                    //dist1 = Vector3.DistanceSquared(Camera.Position, Geom[i].Position);

                    dist1 = hit.Value;

                    if (dist1 < dist2)
                    {
                        dist2 = dist1;
                        index = i;
                    }
                }
            }

            if (index >= 0)
            {
                selected = true;

                if (SelectedGeometry != null)
                {
                    SelectedGeometry.Unselect();
                    
                }

                SelectedGeometry = Geom[index];
                    SelectedGeometry.Select();
                
            }

            return selected;
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            debugSystem.TimeRuler.StartFrame();

            debugSystem.TimeRuler.BeginMark("Update", Color.Blue);

            debugSystem.FpsCounter.Visible = false;
            debugSystem.TimeRuler.Visible = false;
            debugSystem.TimeRuler.ShowLog = false;

            Input.Update();

            Camera.Update(gameTime);

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            if (Input.KBPressed(Keys.Escape))
                this.Exit();


            if (Input.KBPressed(Keys.D1))
                bDrawGeomAABB = !bDrawGeomAABB;
            if (Input.KBPressed(Keys.D2))
                bDrawPylonSphere = !bDrawPylonSphere;
            if (Input.KBPressed(Keys.D3))
                bDrawVertecies = !bDrawVertecies;
            if (Input.KBPressed(Keys.D4))
                bDrawFillNodes = !bDrawFillNodes;
            if (Input.KBPressed(Keys.D5))
                bDrawEdges = !bDrawEdges;
            if (Input.KBPressed(Keys.D6))
                bDrawPortals = !bDrawPortals;

            if (Input.KBPressed(Keys.D7))
                bDrawDebugText = !bDrawDebugText;
            if (Input.KBPressed(Keys.D8))
                bDrawFinalPath = !bDrawFinalPath;

            // TODO: Add your update logic here

            if (Input.KBPressed(Keys.J))
                MergeVertexes();

            if (Input.KBPressed(Keys.L))
                CreateMesh();

            if (Input.KBPressed(Keys.K))
                CreateEdges2();

            if (Input.KBPressed(Keys.M))
                ShowAllConnections();

            if (Input.KBPressed(Keys.N))
                ShowEdges();

            if (Input.KBPressed(Keys.B))
                CleanEdges();
            if (Input.KBPressed(Keys.I))
                CreatePortals();

            if (Input.KBPressed(Keys.Y))
                CreateASNodes();
            if (Input.KBPressed(Keys.U))
                DoMeshSearch();

            if (Input.KBPressed(Keys.Enter))
            {
                StartFilling();
            }

            if (Input.MouseOnePressed())
            {
                Ray r = UnprojectMouse();

                if (!SelectGeometry(r))
                {
                    float? hit = r.Intersects(FloorPlane);

                    if (hit.HasValue)
                    {
                        Vector3 v = r.Position + (r.Direction * hit.Value);
                        CreateGeometry(v);
                    }
                }
            }

            if (Input.MouseTwoPressed())
            {
                RemoveGeometry();
            }

            if (Input.KBPressed(Keys.NumPad0))
            {
                Ray r = UnprojectMouse();

                float? hit = r.Intersects(FloorPlane);

                if (hit.HasValue)
                {
                    CreateSphere(r.Position + (r.Direction * hit.Value));
                }
            }

            if (Input.KBPressed(Keys.D0))
                ClearSphere();


            if (Input.KBPressed(Keys.O))
            {
                Ray r = UnprojectMouse();

                float? hit = r.Intersects(FloorPlane);

                if (hit.HasValue)
                {
                    CreatePylon(r.Position + (r.Direction * hit.Value));
                }
            }

            if (Input.KBPressed(Keys.P))
            {
                Ray r = UnprojectMouse();

                for (int i = 0; i < Pylons.Count; i++)
                {
                    float? hit = r.Intersects(Pylons[i].AABB);

                    if (hit.HasValue)
                        Pylons[i].Remove();
                }
            }


            if (SelectedGeometry != null)
            {
                float scaleRate = 1.0f;
                float modifier = (Input.KBDown(Keys.LeftShift)) ? -1.0f : 1.0f;
                modifier *= (Input.KBDown(Keys.LeftControl)) ? 10.0f : 1.0f;

                if (Input.KBDown(Keys.LeftAlt))
                {
                    if (Input.KBDown(Keys.Up))
                    {
                        SelectedGeometry.Position += -Vector3.UnitZ * (float)gameTime.ElapsedGameTime.TotalSeconds * scaleRate * modifier;
                    }
                    else if (Input.KBDown(Keys.Down))
                    {
                        SelectedGeometry.Position += Vector3.UnitZ * (float)gameTime.ElapsedGameTime.TotalSeconds * scaleRate * modifier;
                    }
                    
                    if (Input.KBDown(Keys.Right))
                    {
                        SelectedGeometry.Position += Vector3.UnitX * (float)gameTime.ElapsedGameTime.TotalSeconds * scaleRate * modifier;
                    }
                    else if (Input.KBDown(Keys.Left))
                    {
                        SelectedGeometry.Position += -Vector3.UnitX * (float)gameTime.ElapsedGameTime.TotalSeconds * scaleRate * modifier;
                    }
                }
                else
                {
                    if (Input.KBDown(Keys.Up))
                    {
                        SelectedGeometry.Scale += Vector3.One * (float)gameTime.ElapsedGameTime.TotalSeconds * scaleRate * modifier;
                    }
                    else
                    {
                        if (Input.KBDown(Keys.Right))
                        {
                            SelectedGeometry.Scale += Vector3.UnitX * (float)gameTime.ElapsedGameTime.TotalSeconds * scaleRate * modifier;
                        }

                        if (Input.KBDown(Keys.Down))
                        {
                            SelectedGeometry.Scale += Vector3.UnitY * (float)gameTime.ElapsedGameTime.TotalSeconds * scaleRate * modifier;
                        }

                        if (Input.KBDown(Keys.Left))
                        {
                            SelectedGeometry.Scale += Vector3.UnitZ * (float)gameTime.ElapsedGameTime.TotalSeconds * scaleRate * modifier;
                        }
                    }
                }
            }


            
            for (int i = 0; i < Geom.Count; i++)
            {
                Geom[i].Update(gameTime);

                if (bDrawGeomAABB)
                    anhu07_NavMesh.DebugShapeRenderer.AddBoundingBox(Geom[i].AABB, Color.Red);
            }
            

            
            for (int i = 0; i < Pylons.Count; i++)
            {
                Pylons[i].Update(gameTime);
                if (bDrawPylonSphere)
                    DebugShapeRenderer.AddBoundingSphere(Pylons[i].Sphere, Color.Orange);
                if (bDrawGeomAABB)
                    DebugShapeRenderer.AddBoundingBox(Pylons[i].AABB, Color.Red);
            }
            

            if (bDrawFillNodes)
            {
                for (int i = 0; i < FillNodes.Count; i++)
                {
                    if (bDrawFillNodes)
                        DebugShapeRenderer.AddBoundingBox(FillNodes[i].AABB, Color.Green);

                    if (bDrawVertecies)
                    {
                        // DebugShapeRenderer.AddLine(FillNodes[i].Corners[0].Position, FillNodes[i].Corners[1].Position, Color.LightBlue);
                        // DebugShapeRenderer.AddLine(FillNodes[i].Corners[1].Position, FillNodes[i].Corners[2].Position, Color.LightBlue);
                        // DebugShapeRenderer.AddLine(FillNodes[i].Corners[2].Position, FillNodes[i].Corners[3].Position, Color.LightBlue);
                        // DebugShapeRenderer.AddLine(FillNodes[i].Corners[3].Position, FillNodes[i].Corners[0].Position, Color.LightBlue);

                        // DebugShapeRenderer.AddTriangle(FillNodes[i].Corners[0].Position, FillNodes[i].Corners[1].Position, FillNodes[i].Corners[3].Position, Color.LightBlue);
                        // DebugShapeRenderer.AddTriangle(FillNodes[i].Corners[1].Position, FillNodes[i].Corners[2].Position, FillNodes[i].Corners[3].Position, Color.LightBlue);

                    }
                }
            }

            if (bDrawVertecies)
            {
                for (int i = 0; i < VerticesCopy.Count; i += 1)
                {
                    DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(VerticesCopy[i].Position, 0.25f), Color.Gainsboro);
                }

                for (int i = 0; i < VertexAddedByPortalCreation.Count; i++)
                {
                    DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(VertexAddedByPortalCreation[i].Position, 0.25f), Color.Gainsboro);
                }
            }

            if (bDrawEdges)
            {
                for (int i = 0; i < EdgeList.Count; i++)
                {
                    DebugShapeRenderer.AddLine(EdgeList[i].Vertex1.Position, EdgeList[i].Vertex2.Position, Color.Coral);
                }
            }

            if (bDrawPortals)
            {
                for (int i = 0; i < Portals.Count; i++)
                {
                    DebugShapeRenderer.AddLine(Portals[i].Vertex1.Position, Portals[i].Vertex2.Position, Color.CadetBlue);

                    DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(Portals[i].Center.Position, 0.25f), Color.DodgerBlue);
                }
            }

            for (int i = 0; i < CenterPointsList.Count; i++)
            {

                // DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(CenterPointsList[i], 0.25f), Color.Wheat);
            }

            if (bDrawFinalPath)
            {
                for (int i = 0; i < NodePath.Count; i++)
                {
                    DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(NodePath[i].Vertex.Position, 0.25f), Color.PaleGreen);

                    for (int j = 0; j < NodePath[i].Neighbors.Count; j++)
                    {
                        DebugShapeRenderer.AddLine(NodePath[i].Vertex.Position, NodePath[i].Neighbors[j].Position, Color.SpringGreen);
                    }
                }
            }

            if (bDrawBoundingSpheres)
            {
                for (int i = 0; i < BoundingSpheres.Count; i++)
                {
                    DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(BoundingSpheres[i].Center, BoundingSpheres[i].Radius), Color.SeaShell);
                }
            }

            if (Edges.Count > 0)
            {
                NavMeshVertex start = Vertecies[0];

                NavMeshVertex next = start;
                NavMeshVertex prev = null;
                NavMeshVertex lastPrev = null;

                int count = 0;

                List<NavMeshVertex> visted = new List<NavMeshVertex>();

                while (next != null)
                {
                    visted.Add(next);

                    prev = next;

                    next = null;

                    for (int i = 0; i < prev.Linked.Count; i++)
                    {
                        if (!visted.Contains(prev.Linked[i]))
                            next = prev.Linked[i];
                    }

                    DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(prev.Position, 0.125f), Color.Chartreuse);

                    if (next == null)
                        break;

                    DebugShapeRenderer.AddLine(prev.Position, next.Position, Color.Aqua);

                    

                    count++;
                }

                DebugShapeRenderer.AddLine(prev.Position, start.Position, Color.BlueViolet);
                DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(start.Position, 0.25f), Color.Chocolate);

                bool bad = false;
            }
            

            /*
            NavMeshVertex startvtex = FindOuterEdgeVertex();

            if (startvtex != null)
            {
                NavMeshVertex a = startvtex;
                NavMeshVertex b = a.Linked[0];
                NavMeshVertex c;

                int count = 0;

                while (b != startvtex && count < 1000)
                {

                    DebugShapeRenderer.AddLine(a.Position, b.Position, Color.LightCoral);

                    for (int i = 0; i < b.Linked.Count; i++)
                    {
                        if (b.Linked[i] != a)
                        {
                            a = b;
                            b = a.Linked[i];
                        }
                    }

                    count++;

                }
                bool basd = true;
            }
            */
            
            base.Update(gameTime);

            debugSystem.TimeRuler.EndMark("Update");
            
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {

            debugSystem.TimeRuler.BeginMark("Draw", Color.Red);
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            Renderer.Draw();


            GraphicsDevice.DepthStencilState = DepthStencilState.None;


            anhu07_NavMesh.DebugShapeRenderer.Draw(gameTime, Camera.View, Camera.Proj);

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            
            spriteBatch.Begin();

            spriteBatch.Draw(crosshair, new Vector2(GraphicsDevice.Viewport.Width * 0.5f - 16.0f, GraphicsDevice.Viewport.Height * 0.5f - 16.0f), Color.White);

            spriteBatch.End();

            
            /*
            spriteBatch.DrawString(Font, "Number of FillNodes: " + FillNodes.Count.ToString(), Vector2.One * 10.0f + Vector2.UnitY * 30.0f, Color.White);
            spriteBatch.DrawString(Font, "Number of Vertices: " + Vertecies.Count.ToString(), Vector2.One * 10.0f + Vector2.UnitY * 50.0f, Color.White);
            spriteBatch.DrawString(Font, "Number of VerticesCopy: " + VerticesCopy.Count.ToString(), Vector2.One * 10.0f + Vector2.UnitY * 70.0f, Color.White);
            spriteBatch.DrawString(Font, "Number of Pointed: " + PointedVertex.Count.ToString(), Vector2.One * 10.0f + Vector2.UnitY * 90.0f, Color.White);
            spriteBatch.DrawString(Font, "Removed Portals: " + removedPortals.ToString(), Vector2.One * 10.0f + Vector2.UnitY * 110.0f, Color.White);
            spriteBatch.DrawString(Font, "Number of Portals: " + Portals.Count.ToString(), Vector2.One * 10.0f + Vector2.UnitY * 130.0f, Color.White);
            spriteBatch.DrawString(Font, "Path Count: " + pathCount.ToString(), Vector2.One * 10.0f + Vector2.UnitY * 150.0f, Color.White);
            spriteBatch.DrawString(Font, "Portal to search: " + portalToSearch.ToString(), Vector2.One * 10.0f + Vector2.UnitY * 170.0f, Color.White);
            spriteBatch.DrawString(Font, "Number of Center Points: " + CenterPointsList.Count.ToString(), Vector2.One * 10.0f + Vector2.UnitY * 190.0f, Color.White);



            if (bDrawDebugText)
            {
                for (int i = 0; i < Portals.Count; i++)
                {
                    Vector3 proj = GraphicsDevice.Viewport.Project(Portals[i].Center.Position, Camera.Proj, Camera.View, Matrix.Identity);
                    spriteBatch.DrawString(Font, "id: " + i.ToString() + "\nSearched: " + Portals[i].Searched.ToString(), Vector2.UnitX * proj.X + Vector2.UnitY * proj.Y, Color.White);
                }
            }
            
            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);

            spriteBatch.Draw(Renderer.RTShadowDepth, new Rectangle(0, 0, 128, 128), Color.White);

            spriteBatch.End();

            */

            base.Draw(gameTime);
            debugSystem.TimeRuler.EndMark("Draw");
        }
    }
}
