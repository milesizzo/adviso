using GameEngine.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using GameEngine.Templates;
using GameEngine.Graphics;
using Microsoft.Xna.Framework.Graphics;

namespace Adventure.GameObjects
{
    public class IsoSprite : AbstractObject
    {
        private const float TileSizeX = 32;
        private const float TileSizeY = 16;
        private Vector2 position;
        public float Height;
        public SpriteTemplate Sprite;

        public IsoSprite(IGameContext context) : base(context)
        {
            //
        }

        public override Vector2 Position
        {
            get { return this.position; }
            set { this.position = value; }
        }

        public float IsoDepth
        {
            get { return this.position.X + this.position.Y; }
        }

        public override void Draw(Renderer renderer)
        {
            var x = (this.position.X - this.position.Y) * TileSizeX / 2;
            var y = (this.position.X + this.position.Y - this.Height) * TileSizeY / 2;
            this.Sprite.DrawSprite(renderer.World, new Vector2(x, y), Color.White, 0, Vector2.One, SpriteEffects.None);
        }
    }
}
