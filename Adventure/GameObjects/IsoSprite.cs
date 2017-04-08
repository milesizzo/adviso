using GameEngine.GameObjects;
using System;
using Microsoft.Xna.Framework;
using GameEngine.Templates;
using GameEngine.Graphics;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using Adventure.Scenes;

namespace Adventure.GameObjects
{
    public class IsoSprite : AbstractObject
    {
        private Vector3 position;
        private float frame;
        private SpriteTemplate sprite;

        public IsoSprite(IGameContext context) : base(context)
        {
            //
        }

        public new IsometricContext Context
        {
            get { return base.Context as IsometricContext; }
        }

        public override Vector2 Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override Vector3 Position3D
        {
            get { return this.position; }
            set { this.position = value; }
        }

        public SpriteTemplate Sprite
        {
            get { return this.sprite; }
            set { this.sprite = value; }
        }

        public float Height
        {
            get { return this.Context.GetHeight(this.Position); }
        }

        public float IsoDepth
        {
            get { return this.position.X + this.position.Y; }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Draw(Renderer renderer, GameTime gameTime)
        {
            this.frame += gameTime.GetElapsedSeconds() * this.Sprite.FPS;
            var intFrame = (int)Math.Floor(this.frame);
            if (intFrame >= this.Sprite.NumberOfFrames)
            {
                intFrame = 0;
                this.frame = 0;
            }

            var pos = this.Context.Project(this.Position3D);

            //var height = this.Height;
            //var pos = this.Context.Map.WorldToMapCell(this.Position);
            //var depthOffset = 0.7f - ((pos.X + (pos.Y * this.Context.Map.TileSizeX + height)) / this.Context.Map.MaxDepth);
            //depthOffset -= this.Context.Map.HeightRowDepthMod * (this.Context.Map.Rows[pos.Y].Columns[pos.X].HeightTiles.Count * this.Context.Map.HeightTileOffset + height);

            //var depthOffset = 0.7f - ((this.position.X + (this.position.Y * this.Context.Map.WorldWidth)) / (this.Context.Map.WorldWidth * this.Context.Map.WorldHeight * 10));
            //var depthOffset = 0.7f - (pos.Y / (float)this.Context.Map.MapHeight) / 10 - (this.Height + this.Context.Map.HeightTileOffset) * this.Context.Map.HeightRowDepthMod;
            var depth = this.Context.CalculateDepth(this.Position3D);

            this.Sprite.DrawSprite(renderer.World, intFrame, pos, Color.White, 0, Vector2.One, SpriteEffects.None, 1f - depth);
        }
    }
}
