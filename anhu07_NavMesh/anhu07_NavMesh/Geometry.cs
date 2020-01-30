using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Surgeon;

namespace anhu07_NavMesh
{
    public class Geometry
    {
        public Vector3 Position
        {
            get { return Model.Position; }
            set { Model.Position = value; }
        }

        public Vector3 Rotation
        {
            get { return Model.Rotation; }
            set { Model.Rotation = value; }
        }

        public Vector3 Scale
        {
            get { return Model.Scale; }
            set { Model.Scale = value; }
        }

        public CModel Model { get; private set; }
        public BoundingBox AABB;

        public bool bSelected { get; private set; }

        public Game Game;
        

        public Geometry(Game game)
        {
            Model = new CModel(game.Content.Load<Model>("UnitCube"), Vector3.Zero, Vector3.Zero, Vector3.One, game.GraphicsDevice, game.Content.Load<Effect>("DepthNormalDiffuse"));
            Position = Vector3.Zero;
            Rotation = Vector3.Zero;
            Scale = Vector3.One;

            (game as Game1).Renderer.AddModel(Model);

            AABB = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));

            Game = game;

            Unselect();
        }


        

        public void Update(GameTime gameTime)
        {
            Position = (Vector3.UnitX + Vector3.UnitZ) * Position + Vector3.UnitY * Scale.Y * 0.5f;
            Model.Update();

            AABB.Min = Position - (Scale * 0.5f);
            AABB.Max = Position + (Scale * 0.5f);
        }

        public void Select()
        {
            bSelected = true;
            Model.DiffuseColor = Color.Red;
            Model.EmissiveColor = Color.Red;
            Model.EmissiveStrength = 0.5f;
            Model.Update();
        }

        public void Unselect()
        {
            bSelected = false;
            Model.DiffuseColor = Color.Gray;

            Model.EmissiveColor = Color.Gray;
            Model.EmissiveStrength = 0.5f;
            Model.Update();
        }
    }
}
