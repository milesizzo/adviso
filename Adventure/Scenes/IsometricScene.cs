using GameEngine.GameObjects;
using GameEngine.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameEngine.Content;
using GameEngine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using GameEngine.Templates;
using Adventure.GameObjects;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using CommonLibrary;

namespace Adventure.Scenes
{
    public class Tile
    {
        public bool Blocks;
        public SpriteTemplate Sprite;
    }

    public class MapCell
    {
        public readonly List<int> BaseTiles = new List<int>();
        public readonly List<int> HeightTiles = new List<int>();
        public readonly List<int> TopperTiles = new List<int>();
        public int SlopeMap = -1;

        public MapCell(int tileId)
        {
            this.TileId = tileId;
        }

        public int TileId
        {
            get { return this.BaseTiles.Count > 0 ? this.BaseTiles[0] : 0; }
            set
            {
                if (this.BaseTiles.Count > 0)
                {
                    this.BaseTiles[0] = value;
                }
                else
                {
                    this.BaseTiles.Add(value);
                }
            }
        }
    }

    public class MapRow
    {
        public readonly List<MapCell> Columns = new List<MapCell>();
    }

    public class TileMap
    {
        public readonly List<MapRow> Rows = new List<MapRow>();
        public readonly int MapWidth;
        public readonly int MapHeight;
        public int TileSizeX = 64;
        public int TileSizeY = 64;
        public int TileStepX = 64;
        public int TileStepY = 16;
        public int OddRowXOffset = 32;
        public int HeightTileOffset = 32;
        //public int BaseOffsetX = -32;
        //public int BaseOffsetY = -64;
        public int BaseOffsetX = 0;
        public int BaseOffsetY = 0;
        public float HeightRowDepthMod = 0.000000001f;
        public readonly SpriteTemplate MouseMap;
        public readonly HashSet<int> Impassable = new HashSet<int>();

        public SpriteSheetTemplate Tileset;
        public SpriteSheetTemplate SlopeMap;

        public TileMap(int width, int height, SpriteTemplate mousemap)
        {
            for (var y = 0; y < height; y++)
            {
                var row = new MapRow();
                for (var x = 0; x < width; x++)
                {
                    row.Columns.Add(new Scenes.MapCell(0));
                }
                this.Rows.Add(row);
            }
            this.MapWidth = width;
            this.MapHeight = height;
            this.MouseMap = mousemap;
        }

        public float MaxDepth
        {
            get { return ((this.MapWidth + 1) + (this.MapHeight + 1) * this.TileSizeX) * 10; }
        }

        public float WorldWidth
        {
            get { return this.MapWidth * this.TileStepX; }
        }

        public float WorldHeight
        {
            get { return this.MapHeight * this.TileStepY; }
        }

        public Point WorldToMapCell(Point worldPoint, out Point localPoint)
        {
            var mapCell = new Point(
               (int)(worldPoint.X / this.MouseMap.Width),
               ((int)(worldPoint.Y / this.MouseMap.Height)) * 2
               );

            int localPointX = worldPoint.X % this.MouseMap.Width;
            int localPointY = worldPoint.Y % this.MouseMap.Height;

            int dx = 0;
            int dy = 0;

            uint[] myUint = new uint[1];

            if (new Rectangle(0, 0, this.MouseMap.Width, this.MouseMap.Height).Contains(localPointX, localPointY))
            {
                this.MouseMap.Texture.GetData(0, new Rectangle(localPointX, localPointY, 1, 1), myUint, 0, 1);

                if (myUint[0] == 0xFF0000FF) // Red
                {
                    dx = -1;
                    dy = -1;
                    localPointX = localPointX + (this.MouseMap.Width / 2);
                    localPointY = localPointY + (this.MouseMap.Height / 2);
                }

                if (myUint[0] == 0xFF00FF00) // Green
                {
                    dx = -1;
                    localPointX = localPointX + (this.MouseMap.Width / 2);
                    dy = 1;
                    localPointY = localPointY - (this.MouseMap.Height / 2);
                }

                if (myUint[0] == 0xFF00FFFF) // Yellow
                {
                    dy = -1;
                    localPointX = localPointX - (this.MouseMap.Width / 2);
                    localPointY = localPointY + (this.MouseMap.Height / 2);
                }

                if (myUint[0] == 0xFFFF0000) // Blue
                {
                    dy = +1;
                    localPointX = localPointX - (this.MouseMap.Width / 2);
                    localPointY = localPointY - (this.MouseMap.Height / 2);
                }
            }

            mapCell.X += dx;
            mapCell.Y += dy - 2;

            localPoint = new Point(localPointX, localPointY);

            return mapCell;
        }

        public Point WorldToMapCell(Point worldPoint)
        {
            Point dummy;
            return WorldToMapCell(worldPoint, out dummy);
        }

        public Point WorldToMapCell(Vector2 worldPoint)
        {
            return this.WorldToMapCell(new Point((int)worldPoint.X, (int)worldPoint.Y));
        }
    }

    public class IsometricContext : IGameContext
    {
        public readonly TileMap Map;
        private readonly List<IGameObject> objects = new List<IGameObject>();
        private readonly Store store;

        public IsometricContext(Store store)
        {
            this.Map = new TileMap(50, 50, store.Sprites<SpriteTemplate>("Base", "mousemap"));
            this.store = store;
        }

        public IEnumerable<IGameObject> Objects { get { return this.objects; } }

        public Store Store { get { return this.store; } }

        public void AddObject(IGameObject obj)
        {
            this.objects.Add(obj);
        }

        private float GetSlopeHeightOffset(Point mapPos, Point localPoint)
        {
            var slopeId = this.Map.Rows[mapPos.Y].Columns[mapPos.X].SlopeMap;
            if (slopeId >= 0 && slopeId < this.Map.SlopeMap.Sprites.Count)
            {
                var sprite = this.Map.SlopeMap.Sprites[slopeId];
                if (localPoint.X >= 0 && localPoint.X < sprite.Width && localPoint.Y >= 0 && localPoint.Y < sprite.Height)
                {
                    var slopeColour = new Color[1];
                    sprite.GetData(new Rectangle(localPoint.X, localPoint.Y, 1, 1), slopeColour, 0, 1);
                    return ((255f - slopeColour[0].R) / 255f) * this.Map.HeightTileOffset;
                }
            }
            return 0;
        }

        public float GetHeight(Vector2 world)
        {
            Point localPoint;
            var mapPos = this.Map.WorldToMapCell(new Point((int)world.X, (int)world.Y), out localPoint);
            var height = (float)this.Map.Rows[mapPos.Y].Columns[mapPos.X].HeightTiles.Count * this.Map.HeightTileOffset;
            height += this.GetSlopeHeightOffset(mapPos, localPoint);
            return height;
        }

        public Vector2 Project(Vector3 location)
        {
            //var angle = Math.PI / 6;
            //var transform = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            return new Vector2((location.X - location.Y) * 32, (((location.X + location.Y) * 16) - location.Z * this.Map.HeightTileOffset));
        }

        public Vector3 Unproject(Vector2 projected, float z)
        {
            // px = (x - y) * 32
            // py = (x + y) * 16 - z * H

            // x = px / 32 + y
            // y = ((py + z * H) / 16 - px / 32) / 2
            var y = ((projected.Y + z * this.Map.HeightTileOffset) / 16 - projected.X / 32) / 2;
            var x = projected.X / 32 + y;
            return new Vector3(x, y, z);
        }

        public float CalculateDepth(Vector3 location)
        {
            return (location.X + location.Y * this.Map.MapWidth) / (this.Map.MapWidth * this.Map.MapHeight) / 10f + (location.Z * 1.25f * this.Map.HeightRowDepthMod);
        }

        public void Draw(Renderer renderer, GameTime gameTime)
        {
            //var font = this.Store.Fonts("Base", "debug");
            for (var y = 0; y < this.Map.Rows.Count; y++)
            {
                var row = this.Map.Rows[y];
                for (var x = 0; x < row.Columns.Count; x++)
                {
                    //var depthOffset = 0.7f - ((x + (y * this.Map.TileSizeX)) / this.Map.MaxDepth);
                    //var depthOffset = 0.7f - ((y / (float)this.Map.WorldHeight) / 10;
                    //var depth = x + y;
                    //var depthOffset = 0.7f - ((x * this.Map.TileStepX + (y * this.Map.TileStepY * this.Map.WorldWidth)) / (this.Map.WorldWidth * this.Map.WorldHeight * 10));
                    var cell = row.Columns[x];
                    //var basePos = new Vector2(x * this.Map.TileStepX + rowOffset + this.Map.BaseOffsetX, y * this.Map.TileStepY + this.Map.BaseOffsetY);
                    var world = new Vector3(x, y, 0);
                    var pos = this.Project(world);
                    var depth = this.CalculateDepth(world);
                    //var depth = (float)(x + y * this.Map.MapWidth) / (this.Map.MapWidth * this.Map.MapHeight) / 10;
                    foreach (var tile in cell.BaseTiles)
                    {
                        this.Map.Tileset.Sprites[tile].DrawSprite(renderer.World, pos, 1f);
                    }
                    var z = 0f;
                    foreach (var tile in cell.HeightTiles)
                    {
                        pos = this.Project(new Vector3(x, y, z));
                        if (tile >= 0)
                        {
                            this.Map.Tileset.Sprites[tile].DrawSprite(
                                renderer.World,
                                pos,
                                1f - depth);
                        }
                        z += 1;
                        depth += 1.25f * this.Map.HeightRowDepthMod;
                        //basePos.Y -= this.Map.HeightTileOffset;
                        //depthOffset -= this.Map.HeightRowDepthMod * this.Map.HeightTileOffset;
                    }
                    //depthOffset += this.Map.HeightRowDepthMod * cell.HeightTiles.Count;
                    foreach (var tile in cell.TopperTiles)
                    {
                        this.Map.Tileset.Sprites[tile].DrawSprite(
                            renderer.World,
                            pos,
                            1f - depth);
                    }
                    /*font.DrawString(
                        renderer.World,
                        new Vector2((x * this.Map.TileStepX) + rowOffset + this.Map.BaseOffsetX + 20, (y * this.Map.TileStepY) + this.Map.BaseOffsetY + 38),
                        $"{x},{y}",
                        Color.White);*/
                }
            }
            foreach (var obj in this.objects)
            {
                obj.Draw(renderer, gameTime);
                renderer.World.DrawCircle(this.Project(obj.Position3D), 3, 4, Color.White);
            }
        }

        public void RemoveObject(IGameObject obj)
        {
            this.objects.Remove(obj);
        }

        public void Reset()
        {
            this.objects.Clear();
        }

        public void ScheduleObject(IGameObject obj, float waitTime)
        {
            throw new NotImplementedException();
        }

        public void Update(GameTime gameTime)
        {
        }
    }

    public class IsometricScene : GameScene<IsometricContext>
    {
        private string playerAnimation;
        private IsoSprite player;
        private SpriteTemplate highlight;

        public IsometricScene(string name, GraphicsDevice graphics, Store store) : base(name, graphics, store)
        {
            //
        }

        protected override IsometricContext CreateContext()
        {
            return new IsometricContext(this.Store);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            var mouse = Mouse.GetState();
            var keyboard = Keyboard.GetState();
            var elapsed = gameTime.GetElapsedSeconds();
            if (keyboard.IsKeyDown(Keys.Right))
            {
                this.Camera.Position += new Vector2(elapsed * 100, 0);
            }
            if (keyboard.IsKeyDown(Keys.Left))
            {
                this.Camera.Position -= new Vector2(elapsed * 100, 0);
            }
            if (keyboard.IsKeyDown(Keys.Up))
            {
                this.Camera.Position -= new Vector2(0, elapsed * 100);
            }
            if (keyboard.IsKeyDown(Keys.Down))
            {
                this.Camera.Position += new Vector2(0, elapsed * 100);
            }

            var animation = $"Idle{this.playerAnimation.Substring(4)}";
            var moveVector = Vector2.Zero;
            var amount = elapsed;
            if (keyboard.IsKeyDown(Keys.A))
            {
                moveVector += new Vector2(-1, +1);
            }
            if (keyboard.IsKeyDown(Keys.D))
            {
                moveVector += new Vector2(+1, -1);
            }
            if (keyboard.IsKeyDown(Keys.W))
            {
                moveVector += new Vector2(-1, -1);
            }
            if (keyboard.IsKeyDown(Keys.S))
            {
                moveVector += new Vector2(+1, +1);
            }
            if (keyboard.IsKeyDown(Keys.LeftShift))
            {
                //moveVector *= 4;
                amount *= 4;
            }
            /*if (keyboard.IsKeyDown(Keys.Q))
            {
                this.player.Height += elapsed * 2;
            }
            if (keyboard.IsKeyDown(Keys.E))
            {
                this.player.Height -= elapsed * 2;
            }*/
            if (moveVector.Length() != 0)
            {
                moveVector.Normalize();
                moveVector *= amount;
                if (moveVector.X < 0)
                {
                    if (moveVector.Y < 0)
                    {
                        animation = "WalkNorth";
                    }
                    else if (moveVector.Y > 0)
                    {
                        animation = "WalkWest";
                    }
                    else
                    {
                        animation = "WalkNorthWest";
                    }
                }
                else if (moveVector.X > 0)
                {
                    if (moveVector.Y < 0)
                    {
                        animation = "WalkEast";
                    }
                    else if (moveVector.Y > 0)
                    {
                        animation = "WalkSouth";
                    }
                    else
                    {
                        animation = "WalkSouthEast";
                    }
                }
                else if (moveVector.Y < 0)
                {
                    animation = "WalkNorthEast";
                }
                else if (moveVector.Y > 0)
                {
                    animation = "WalkSouthWest";
                }
                /*
                var newXPos = this.Context.Map.WorldToMapCell(this.player.Position + new Vector2(moveVector.X, 0));
                var newYPos = this.Context.Map.WorldToMapCell(this.player.Position + new Vector2(0, moveVector.Y));
                var newPos = this.Context.Map.WorldToMapCell(this.player.Position + moveVector);
                var cellXPos = this.Context.Map.Rows[newXPos.Y].Columns[newXPos.Y];
                var cellYPos = this.Context.Map.Rows[newYPos.Y].Columns[newYPos.Y];
                var cellNewPos = this.Context.Map.Rows[newPos.Y].Columns[newPos.Y];
                if (cellXPos.TopperTiles.Any(id => this.Context.Map.Impassable.Contains(id)))
                {
                    moveVector.X = 0;
                }
                if (cellYPos.TopperTiles.Any(id => this.Context.Map.Impassable.Contains(id)))
                {
                    moveVector.Y = 0;
                }
                */
                this.player.Position3D = new Vector3(
                    MathHelper.Clamp(this.player.Position3D.X + moveVector.X, 0, this.Context.Map.MapWidth),
                    MathHelper.Clamp(this.player.Position3D.Y + moveVector.Y, 0, this.Context.Map.MapHeight),
                    this.player.Position3D.Z);
            }
            if (animation != this.playerAnimation)
            {
                this.playerAnimation = animation;
                this.player.Sprite = this.PlayerAnimation(animation);
            }
            this.Camera.LookAt(this.Context.Project(this.player.Position3D));

            /*
            var cameraOffset = Vector2.Zero;
            if (this.Camera.BoundingRectangle.Left < this.Context.Map.TileSizeX)
            {
                cameraOffset += new Vector2(this.Context.Map.TileSizeX - this.Camera.BoundingRectangle.Left, 0);
            }
            if (this.Camera.BoundingRectangle.Right > this.Context.Map.WorldWidth)
            {
                cameraOffset += new Vector2(this.Context.Map.WorldWidth - this.Camera.BoundingRectangle.Right, 0);
            }
            if (this.Camera.BoundingRectangle.Top < this.Context.Map.TileSizeY)
            {
                cameraOffset += new Vector2(0, this.Context.Map.TileSizeY - this.Camera.BoundingRectangle.Top);
            }
            if (this.Camera.BoundingRectangle.Bottom > this.Context.Map.WorldHeight)
            {
                cameraOffset += new Vector2(0, this.Context.Map.WorldHeight - this.Camera.BoundingRectangle.Bottom);
            }
            if (cameraOffset.Length() != 0)
            {
                this.Camera.Position += cameraOffset;
            }
            */
        }

        private SpriteTemplate PlayerAnimation(string key)
        {
            this.playerAnimation = key;
            return this.Store.Sprites<NamedAnimatedSpriteSheetTemplate>("Base", "player").GetAnimation(key);
        }

        public override void SetUp()
        {
            base.SetUp();

            /*
            var random = new Random();

            var seen = new HashSet<Vector2>();
            for (var i = 0; i < 100; i++)
            {
                string sprite = string.Empty;
                switch (random.Next(2))
                {
                    case 0:
                        sprite = "basic_tile";
                        break;
                    case 1:
                        sprite = "basic_high_tower";
                        break;
                }
                int x, y, z;
                do
                {
                    x = random.Next(20);
                    y = random.Next(20);
                    z = 0;
                } while (this.Context.Map[z, y, x] != null);
                this.Context.Map[z, y, x] = new Tile
                {
                    Sprite = this.Store.Sprites("Base", sprite),
                    Blocks = sprite == "basic_high_tower",
                };
            }
            this.player = new IsoSprite(this.Context);
            this.player.Position = Vector2.Zero;
            this.player.Sprite = this.Store.Sprites("Base", "ball");
            this.Context.AddObject(this.player);
            */
            this.player = new IsoSprite(this.Context);
            this.player.Position3D = new Vector3(10, 10, 0);
            this.player.Sprite = this.PlayerAnimation("IdleNorth");
            this.Context.AddObject(this.player);

            /*
            var tree = new IsoSprite(this.Context);
            tree.Position = new Vector2(900, 492);
            tree.Sprite = this.Store.Sprites<SingleSpriteTemplate>("Base", "tree1");
            this.Context.AddObject(tree);

            var bush = new IsoSprite(this.Context);
            bush.Position = new Vector2(1000, 492);
            bush.Sprite = this.Store.Sprites<SingleSpriteTemplate>("Base", "bush1");
            this.Context.AddObject(bush);
            */

            this.Context.Map.Tileset = this.Store.Sprites<SpriteSheetTemplate>("Base", "forest_tiles");
            this.Context.Map.SlopeMap = this.Store.Sprites<SpriteSheetTemplate>("Base", "slope_tiles");

            this.Context.Map.Impassable.UnionWith(new[]
            {
                50, 51, 52, 53, 54, 55, 56, 57, 58, 59,
                60, 61, 62, 63, 64, 65, 69,
                70,
                80, 81, 82, 83, 84, 85, 86, 87, 88, 89,
                90, 91, 92, 93, 94, 95, 96, 97, 98, 99,
                100, 101, 102,
                119,
                120, 121, 126, 127, 128, 129,
            });

            // add some height objects
            this.Context.Map.Rows[16].Columns[4].HeightTiles.Add(54);

            this.Context.Map.Rows[17].Columns[3].HeightTiles.Add(54);

            this.Context.Map.Rows[15].Columns[3].HeightTiles.Add(54);
            this.Context.Map.Rows[16].Columns[3].HeightTiles.Add(53);

            this.Context.Map.Rows[15].Columns[4].HeightTiles.Add(54);
            this.Context.Map.Rows[15].Columns[4].HeightTiles.Add(54);
            this.Context.Map.Rows[15].Columns[4].HeightTiles.Add(51);

            this.Context.Map.Rows[18].Columns[3].HeightTiles.Add(51);
            this.Context.Map.Rows[19].Columns[3].HeightTiles.Add(50);
            this.Context.Map.Rows[18].Columns[4].HeightTiles.Add(55);

            this.Context.Map.Rows[14].Columns[4].HeightTiles.Add(54);

            this.Context.Map.Rows[14].Columns[5].HeightTiles.Add(62);
            this.Context.Map.Rows[14].Columns[5].HeightTiles.Add(61);
            this.Context.Map.Rows[14].Columns[5].HeightTiles.Add(63);

            // add a hill
            this.Context.Map.Rows[12].Columns[9].HeightTiles.Add(34);
            this.Context.Map.Rows[11].Columns[9].HeightTiles.Add(34);
            this.Context.Map.Rows[11].Columns[8].HeightTiles.Add(34);
            this.Context.Map.Rows[10].Columns[9].HeightTiles.Add(34);

            this.Context.Map.Rows[12].Columns[8].TopperTiles.Add(31);
            this.Context.Map.Rows[12].Columns[8].SlopeMap = 0;
            this.Context.Map.Rows[13].Columns[8].TopperTiles.Add(31);
            this.Context.Map.Rows[13].Columns[8].SlopeMap = 0;

            this.Context.Map.Rows[12].Columns[10].TopperTiles.Add(32);
            this.Context.Map.Rows[12].Columns[10].SlopeMap = 1;
            this.Context.Map.Rows[13].Columns[9].TopperTiles.Add(32);
            this.Context.Map.Rows[13].Columns[9].SlopeMap = 1;

            this.Context.Map.Rows[14].Columns[9].TopperTiles.Add(30);
            this.Context.Map.Rows[14].Columns[9].SlopeMap = 4;

            // add some ground tiles (eg. grass)
            this.Context.Map.Rows[17].Columns[4].TopperTiles.Add(114);
            this.Context.Map.Rows[16].Columns[5].TopperTiles.Add(115);
            this.Context.Map.Rows[14].Columns[4].TopperTiles.Add(125);
            this.Context.Map.Rows[15].Columns[5].TopperTiles.Add(91);
            this.Context.Map.Rows[16].Columns[6].TopperTiles.Add(94);

            // add some trees
            /*
            this.Context.Map.Rows[20].Columns[15].HeightTiles.Add(132);
            this.Context.Map.Rows[20].Columns[15].HeightTiles.Add(-1);
            this.Context.Map.Rows[20].Columns[15].HeightTiles.Add(122);

            this.Context.Map.Rows[20].Columns[17].HeightTiles.Add(133);
            this.Context.Map.Rows[20].Columns[17].HeightTiles.Add(-1);
            this.Context.Map.Rows[20].Columns[17].HeightTiles.Add(123);
            */

            var random = new Random();
            var map = this.Context.Map;
            for (var y = 0; y < map.MapHeight; y++)
                for (var x = 0; x < map.MapWidth; x++)
                {
                    var cell = map.Rows[y].Columns[x];
                    var id = random.Next(0, 18);
                    if (id > 6) id += 3;
                    if (id > 17) id += 2;
                    cell.TileId = id;

                    id = random.Next(110, 118);
                    if (!cell.HeightTiles.Any() && !cell.TopperTiles.Any())
                    {
                        if (random.Next(0, 3) == 0)
                        {
                            cell.TopperTiles.Add(id);
                        }
                        else if (random.Next(0, 5) == 0)
                        {
                            cell.TopperTiles.Add(random.Choice(70, 119, 120, 121, 124, 125, 126, 127, 128, 129));
                        }
                    }
                }

            this.Camera.LookAt(new Vector2(0, 0));
            this.Camera.Zoom = 2f;
            this.Camera.SamplerState = SamplerState.PointClamp;

            this.highlight = this.Store.Sprites<SpriteTemplate>("Base", "highlight");
        }

        public override void PreDraw(Renderer renderer)
        {
            renderer.Screen.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp);
            renderer.World.Begin(sortMode: SpriteSortMode.BackToFront, blendState: BlendState.AlphaBlend, transformMatrix: this.Camera.GetViewMatrix(), samplerState: this.Camera.SamplerState);
        }

        public override void Draw(Renderer renderer, GameTime gameTime)
        {
            this.Camera.Clear(Color.Black);
            base.Draw(renderer, gameTime);

            var map = this.Context.Map;

            /*
            var world = this.Camera.ScreenToWorld(Mouse.GetState().X, Mouse.GetState().Y);
            renderer.World.DrawPoint(world, Color.White);
            var highlightPos = map.WorldToMapCell(new Point((int)world.X, (int)world.Y));
            var highlightRowOffset = ((highlightPos.Y % 2) == 1) ? map.OddRowXOffset : 0;
            this.highlight.DrawSprite(
                renderer.World,
                new Vector2(highlightPos.X * map.TileStepX + highlightRowOffset + map.BaseOffsetX, (highlightPos.Y + 2) * map.TileStepY + map.BaseOffsetY),
                Color.White * 0.3f,
                0,
                Vector2.One);
            */
            var projected = this.Camera.ScreenToWorld(Mouse.GetState().X, Mouse.GetState().Y);
            var world = this.Context.Unproject(projected, 0);
            var tilePos = new Vector3((int)world.X, (int)world.Y, 0);
            var tileProj = this.Context.Project(tilePos);
            this.highlight.DrawSprite(renderer.World, tileProj, Color.White * 0.3f, 0, Vector2.One);


            //var playerMap = this.Context.Map.WorldToMapCell(new Point((int)this.player.Position.X, (int)this.player.Position.Y));
            var playerProjected = this.Context.Project(this.player.Position3D);

            var text = new StringBuilder();
            text.AppendLine($"Mouse:");
            text.AppendLine($"  Projected: {projected}");
            text.AppendLine($"      World: {world}");
            //text.AppendLine($"       Tile: {tilePos}");
            text.AppendLine($"Player:");
            text.AppendLine($"  Projected: {playerProjected}");
            text.AppendLine($"      World: {this.player.Position3D}");
            text.AppendLine($"Camera:");
            text.AppendLine($"  Projected: {this.Camera.Position}");
            text.AppendLine($"     Origin: {this.Camera.Origin}");
            this.Store.Fonts("Base", "debug").DrawString(renderer.Screen, new Vector2(0, 0), text.ToString(), Color.White);
            //this.Store.Fonts("Base", "debug").DrawString(renderer.Screen, new Vector2(0, 0), $"Mouse: (projected) {projected}\n(world) {world} (map) {tilePos}", Color.White);
            //this.Store.Fonts("Base", "debug").DrawString(renderer.Screen, new Vector2(0, 16), $"Player: (world) {this.player.Position3D} (projected) {playerProjected}", Color.White);
            //this.Store.Fonts("Base", "debug").DrawString(renderer.Screen, new Vector2(0, 32), $"Camera: (world) {this.Camera.Position} (origin) {this.Camera.Origin}", Color.White);
        }
    }
}
