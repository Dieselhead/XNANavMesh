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
    public class Pylon
    {
        public BoundingSphere Sphere;

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

        public Game Game;

        public float Radius { get; set; }

        public BoundingBox AABB;
        

        public Pylon(Game game, float radius)
        {
            Game = game;

            Model = new CModel(game.Content.Load<Model>("Tree"), Vector3.Zero, Vector3.Zero, Vector3.One, game.GraphicsDevice, game.Content.Load<Effect>("DepthNormalDiffuse"));
            Position = Vector3.Zero;
            Rotation = Vector3.Zero;
            Scale = Vector3.One;

            Model.DiffuseColor = Color.Pink;
            Model.EmissiveColor = Color.Pink;
            Model.EmissiveStrength = 1.0f;

            (game as Game1).Renderer.AddModel(Model);

            Radius = radius;

            Sphere = new BoundingSphere(Position, Radius);

            AABB = new BoundingBox();

        }

        public void Remove()
        {
            (Game as Game1).Renderer.RemoveModel(Model);
            (Game as Game1).Pylons.Remove(this);

        }

        public void Update(GameTime gameTime)
        {
            Position = (Vector3.UnitX + Vector3.UnitZ) * Position + Vector3.UnitY * Scale.Y * 0.5f;
            Model.Update();

            Sphere.Center = Position;
            Sphere.Radius = Radius;

            AABB.Min = Position - (Scale * 0.5f);
            AABB.Max = Position + (Scale * 0.5f);

        }
    }
}
