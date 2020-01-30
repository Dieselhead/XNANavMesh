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
    public class NavMeshVertex
    {
        public List<NavMeshVertex> Linked;

        public Vector3 Position;

        public NavMeshVertex Next;
        public NavMeshVertex Prev;

        public int EdgeIndex = -1;

        public int m_id;
        public static int ID = 0;


        public ASNNavMesh Node;

        public NavMeshVertex()
        {
            Linked = new List<NavMeshVertex>();
            Position = Vector3.Zero;

            m_id = ID;
            ID++;

        }
    }
}
