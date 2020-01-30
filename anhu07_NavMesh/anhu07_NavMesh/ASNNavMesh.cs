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

using Surgeon;

namespace anhu07_NavMesh
{
    public class ASNNavMesh : AStarNode
    {
        public NavMeshVertex Vertex;
        public Portal Edge;
        public Portal Portal;

        public ASNNavMesh(NavMeshVertex vertex)
            : base(0.1f, vertex.Position)
        {
            Vertex = vertex;
        }
    }
}
