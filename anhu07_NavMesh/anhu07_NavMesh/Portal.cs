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
    public class Portal
    {
        public NavMeshVertex Vertex1;
        public NavMeshVertex Vertex2;

        public NavMeshVertex Center;

        public int Searched = 0;

        public Portal(NavMeshVertex v1, NavMeshVertex v2)
        {
            Vertex1 = v1;
            Vertex2 = v2;

            Center = new NavMeshVertex();
            Center.Position = (v1.Position + v2.Position) * 0.5f;
        }
    }
}
