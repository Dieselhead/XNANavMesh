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

namespace anhu07_NavMesh
{
    public class FloodFillNode
    {
        public BoundingBox AABB;
        protected Vector3 m_position;
        public Vector3 Position { get { return m_position; } set { m_position = value; Update(); } }
        
        public static float Size = 1.00f;

        public List<FloodFillNode> Neighbors;

        public List<NavMeshVertex> Corners;


        public FloodFillNode()
        {
            AABB = new BoundingBox();
            Neighbors = new List<FloodFillNode>();
            m_position = Vector3.Zero;
            

            Corners = new List<NavMeshVertex>();
            Corners.Add(new NavMeshVertex());
            Corners.Add(new NavMeshVertex());
            Corners.Add(new NavMeshVertex());
            Corners.Add(new NavMeshVertex());

            /*
            Corners[0].Linked.Add(Corners[3]);
            Corners[0].Linked.Add(Corners[1]);
            Corners[1].Linked.Add(Corners[0]);
            Corners[1].Linked.Add(Corners[2]);
            Corners[2].Linked.Add(Corners[1]);
            Corners[2].Linked.Add(Corners[3]);
            Corners[3].Linked.Add(Corners[2]);
            Corners[3].Linked.Add(Corners[0]);
            */

            Update();

        }

        public void Update()
        {
            AABB.Min = Position - (Vector3.UnitX * Size * 0.420f) - (Vector3.UnitY * 0.1f) - (Vector3.UnitZ * Size * 0.420f);
            AABB.Max = Position + (Vector3.UnitX * Size * 0.420f) + (Vector3.UnitY * 0.1f) + (Vector3.UnitZ * Size * 0.420f);

            Corners[0].Position = Position - (Vector3.UnitX * Size * 0.5f) + (Vector3.UnitY * 0.1f) - (Vector3.UnitZ * Size * 0.5f);
            Corners[1].Position = Position + (Vector3.UnitX * Size * 0.5f) + (Vector3.UnitY * 0.1f) - (Vector3.UnitZ * Size * 0.5f);
            Corners[2].Position = Position + (Vector3.UnitX * Size * 0.5f) + (Vector3.UnitY * 0.1f) + (Vector3.UnitZ * Size * 0.5f);
            Corners[3].Position = Position - (Vector3.UnitX * Size * 0.5f) + (Vector3.UnitY * 0.1f) + (Vector3.UnitZ * Size * 0.5f);
        }
    }
}
