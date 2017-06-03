using GameEngine.GameObjects;
using System;
using Microsoft.Xna.Framework;
using GameEngine.Templates;
using GameEngine.Graphics;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using Adventure.Scenes;
using Microsoft.Xna.Framework.Audio;
using System.Collections.Generic;
using System.Linq;

namespace Adventure.GameObjects
{
    public class AABB
    {
        private Vector3 position;
        private readonly float width;
        private readonly float height;
        private readonly float depth;

        public AABB(Vector3 pos, float width, float depth, float height)
        {
            this.position = pos;
            this.width = width;
            this.depth = depth;
            this.height = height;
        }

        public Vector3 Position
        {
            get { return this.position; }
            set { this.position = value; }
        }

        public Vector3 Min
        {
            get { return this.position; }
        }

        public Vector3 Max
        {
            get { return this.position + new Vector3(this.width, this.depth, this.height); }
        }

        public float Width { get { return this.width; } }

        public float Height { get { return this.height; } }

        public float Depth { get { return this.depth; } }

        public HexBounds Hex
        {
            get
            {
                //var top = HexCoords.FromWorld(this.position + new Vector3(0, 0, this.height));
                var topLeft = HexCoords.FromWorld(this.position + new Vector3(0, this.depth, this.height));
                var topRight = HexCoords.FromWorld(this.position + new Vector3(this.width, 0, this.height));
                var bottom = HexCoords.FromWorld(this.position + new Vector3(this.width, this.depth, 0));
                return new HexBounds
                {
                    Min = new HexCoords
                    {
                        x = topLeft.x,
                        y = topRight.y,
                        h = topLeft.h,
                        //v = top.v
                    },
                    Max = new HexCoords
                    {
                        x = bottom.x,
                        y = bottom.y,
                        h = topRight.h,
                        //v = bottom.v
                    }
                };
            }
        }

        public bool IsInFrontOf(AABB other)
        {
            if (this.Min.X >= other.Max.X)
            {
                return false;
            }
            else if (other.Min.X >= this.Max.X)
            {
                return true;
            }

            if (this.Min.Y >= other.Max.Y)
            {
                return false;
            }
            else if (other.Min.Y >= this.Max.Y)
            {
                return true;
            }

            if (this.Min.Z >= other.Max.Z)
            {
                return true;
            }
            else if (other.Min.Z >= this.Max.Z)
            {
                return false;
            }

            return false;
        }
    }

    public struct HexCoords
    {
        public float x;
        public float y;
        public float h;
        public float v;

        public static HexCoords FromWorld(Vector3 world)
        {
            var isoX = world.X + world.Z;
            var isoY = world.Y + world.Z;
            return new HexCoords
            {
                x = isoX,
                y = isoY,
                h = isoX - isoY,
                v = (isoX + isoY) / 2
            };
        }
    }

    public struct HexBounds
    {
        public HexCoords Min;
        public HexCoords Max;

        public static bool IsOverlapping(HexBounds hex1, HexBounds hex2)
        {
            if (hex1.Min.x >= hex2.Max.x || hex2.Min.x >= hex1.Max.x)
            {
                return false;
            }
            if (hex1.Min.y >= hex2.Max.y || hex2.Min.y >= hex1.Max.y)
            {
                return false;
            }
            if (hex1.Min.h >= hex2.Max.h || hex2.Min.h >= hex1.Max.h)
            {
                return false;
            }
            return true;
        }
    }

    public class IsoSprite : AbstractObject
    {
        //private Vector3 position;
        private float frame;
        private ISpriteTemplate sprite;
        public AABB Bounds;

        public IsoSprite(IGameContext context) : base(context)
        {
            this.Bounds = new AABB(Vector3.Zero, 1, 1, 1);
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
            get { return this.Bounds.Position; }
            set { this.Bounds.Position = value; }
        }
        
        public ISpriteTemplate Sprite
        {
            get { return this.sprite; }
            set { this.sprite = value; }
        }

        public float IsoDepth
        {
            get { return this.Position3D.X + this.Position3D.Y; }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            this.Position3D = new Vector3(this.Position3D.X, this.Position3D.Y, this.Context.GetHeight(this.Position3D));
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
            //pos.X = (float)Math.Truncate(pos.X);
            //pos.Y = (float)Math.Truncate(pos.Y);

            //var height = this.Height;
            //var pos = this.Context.Map.WorldToMapCell(this.Position);
            //var depthOffset = 0.7f - ((pos.X + (pos.Y * this.Context.Map.TileSizeX + height)) / this.Context.Map.MaxDepth);
            //depthOffset -= this.Context.Map.HeightRowDepthMod * (this.Context.Map.Rows[pos.Y].Columns[pos.X].HeightTiles.Count * this.Context.Map.HeightTileOffset + height);

            //var depthOffset = 0.7f - ((this.position.X + (this.position.Y * this.Context.Map.WorldWidth)) / (this.Context.Map.WorldWidth * this.Context.Map.WorldHeight * 10));
            //var depthOffset = 0.7f - (pos.Y / (float)this.Context.Map.MapHeight) / 10 - (this.Height + this.Context.Map.HeightTileOffset) * this.Context.Map.HeightRowDepthMod;
            var depth = this.Context.CalculateDepth(this.Position3D);

            this.Sprite.DrawSprite(renderer.World, intFrame, pos, Color.White, 0, Vector2.One, SpriteEffects.None, 0.7f - depth);

            // draw the bounding box
            /*
            var w = this.Bounds.Width;
            var d = this.Bounds.Depth;
            var h = this.Bounds.Height;
            var points = new List<Tuple<Vector3, Vector3>>
            {
                Tuple.Create(new Vector3(0, 0, 0), new Vector3(w, 0, 0)),
                Tuple.Create(new Vector3(w, 0, 0), new Vector3(w, d, 0)),
                Tuple.Create(new Vector3(w, d, 0), new Vector3(0, d, 0)),
                Tuple.Create(new Vector3(0, d, 0), new Vector3(0, 0, 0)),

                Tuple.Create(new Vector3(0, 0, h), new Vector3(w, 0, h)),
                Tuple.Create(new Vector3(w, 0, h), new Vector3(w, d, h)),
                Tuple.Create(new Vector3(w, d, h), new Vector3(0, d, h)),
                Tuple.Create(new Vector3(0, d, h), new Vector3(0, 0, h)),

                Tuple.Create(new Vector3(0, 0, 0), new Vector3(0, 0, h)),
                Tuple.Create(new Vector3(w, 0, 0), new Vector3(w, 0, h)),
                Tuple.Create(new Vector3(w, d, 0), new Vector3(w, d, h)),
                Tuple.Create(new Vector3(0, d, 0), new Vector3(0, d, h)),
            };
            foreach (var point in points)
            {
                var p1 = this.Context.Project(this.Position3D + point.Item1);
                var p2 = this.Context.Project(this.Position3D + point.Item2);
                renderer.World.DrawLine(p1, p2, Color.White);
            }
            */

            /*
            var mapPos = new Point((int)Math.Truncate(this.Position3D.X), (int)Math.Truncate(this.Position3D.Y));
            var localPos = new Vector2(this.Position3D.X - mapPos.X, this.Position3D.Y - mapPos.Y);
            var localPoint = this.Context.Project(localPos);
            var cell = this.Context.Map.Rows[mapPos.Y].Columns[mapPos.X];
            var slopeId = cell.SlopeMap;
            if (slopeId >= 0 && slopeId < this.Context.Map.SlopeMap.Sprites.Count)
            {
                var sprite = this.Context.Map.SlopeMap.Sprites[slopeId];
                localPoint.X += 32;
                if (localPoint.X >= 0 && localPoint.X < sprite.Width && localPoint.Y >= 0 && localPoint.Y < sprite.Height)
                {
                    var slopeColour = new Color[1];
                    sprite.GetData(new Rectangle(localPoint.X, localPoint.Y, 1, 1), slopeColour, 0, 1);
                    sprite.DrawSprite(renderer.Screen, new Vector2(0, 800), 0);
                    renderer.Screen.DrawPoint(new Vector2(localPoint.X, localPoint.Y + 800f), Color.White);
                }
            }
            */
        }
    }
}
