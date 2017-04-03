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
        public float HeightRowDepthMod = 0.0000001f;
        public readonly SpriteTemplate MouseMap;

        public SpriteSheetTemplate Tileset;

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

        public Point WorldToMapCell(Point worldPoint, out Point localPoint)
        {
            Point mapCell = new Point(
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
    }

    public class IsometricContext : IGameContext
    {
        public readonly TileMap Map;
        private readonly List<IGameObject> objects = new List<IGameObject>();
        private readonly Store store;

        public IsometricContext(Store store)
        {
            this.Map = new TileMap(50, 50, store.Sprites("Base", "mousemap"));
            this.store = store;
        }

        public IEnumerable<IGameObject> Objects { get { return this.objects; } }

        public Store Store { get { return this.store; } }

        public void AddObject(IGameObject obj)
        {
            this.objects.Add(obj);
        }

        public void Draw(Renderer renderer)
        {
            //var font = this.Store.Fonts("Base", "debug");
            for (var y = 0; y < this.Map.Rows.Count; y++)
            {
                var rowOffset = (y % 2 == 1) ? this.Map.OddRowXOffset : 0;
                var row = this.Map.Rows[y];
                for (var x = 0; x < row.Columns.Count; x++)
                {
                    var depthOffset = 0.7f - ((x + (y * this.Map.TileSizeX)) / this.Map.MaxDepth);
                    var cell = row.Columns[x];
                    var basePos = new Vector2(x * this.Map.TileStepX + rowOffset + this.Map.BaseOffsetX, y * this.Map.TileStepY + this.Map.BaseOffsetY);
                    foreach (var tile in cell.BaseTiles)
                    {
                        this.Map.Tileset.Sprites[tile].DrawSprite(renderer.World, basePos, 1f);
                    }
                    foreach (var tile in cell.HeightTiles)
                    {
                        this.Map.Tileset.Sprites[tile].DrawSprite(
                            renderer.World,
                            basePos,
                            depthOffset);
                        basePos.Y -= this.Map.HeightTileOffset;
                        depthOffset -= this.Map.HeightRowDepthMod;
                    }
                    foreach (var tile in cell.TopperTiles)
                    {
                        this.Map.Tileset.Sprites[tile].DrawSprite(
                            renderer.World,
                            basePos,
                            depthOffset);
                    }
                    /*font.DrawString(
                        renderer.World,
                        new Vector2((x * this.Map.TileStepX) + rowOffset + this.Map.BaseOffsetX + 20, (y * this.Map.TileStepY) + this.Map.BaseOffsetY + 38),
                        $"{x},{y}",
                        Color.White);*/
                }
            }
            /*
            foreach (var obj in this.objects.OrderBy(o => (o as IsoSprite).IsoDepth))
            {
                obj.Draw(renderer);
            }
            */
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
            if (keyboard.IsKeyDown(Keys.A))
            {
                this.player.Position -= new Vector2(elapsed * 2, 0);
            }
            if (keyboard.IsKeyDown(Keys.D))
            {
                this.player.Position += new Vector2(elapsed * 2, 0);
            }
            if (keyboard.IsKeyDown(Keys.W))
            {
                this.player.Position -= new Vector2(0, elapsed * 2);
            }
            if (keyboard.IsKeyDown(Keys.S))
            {
                this.player.Position += new Vector2(0, elapsed * 2);
            }
            if (keyboard.IsKeyDown(Keys.Q))
            {
                this.player.Height += elapsed * 2;
            }
            if (keyboard.IsKeyDown(Keys.E))
            {
                this.player.Height -= elapsed * 2;
            }
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
            this.Context.Map.Tileset = this.Store.Sprites("Base", "forest_tiles") as SpriteSheetTemplate;

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

            // add some ground tiles (eg. grass)
            this.Context.Map.Rows[17].Columns[4].TopperTiles.Add(114);
            this.Context.Map.Rows[16].Columns[5].TopperTiles.Add(115);
            this.Context.Map.Rows[14].Columns[4].TopperTiles.Add(125);
            this.Context.Map.Rows[15].Columns[5].TopperTiles.Add(91);
            this.Context.Map.Rows[16].Columns[6].TopperTiles.Add(94);

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
                    if (!cell.HeightTiles.Any() && cell.TileId < 30)
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

            this.highlight = this.Store.Sprites("Base", "highlight");
        }

        public override void PreDraw(Renderer renderer)
        {
            renderer.Screen.Begin(blendState: BlendState.NonPremultiplied, samplerState: SamplerState.PointClamp);
            renderer.World.Begin(sortMode: SpriteSortMode.BackToFront, blendState: BlendState.AlphaBlend, transformMatrix: this.Camera.GetViewMatrix(), samplerState: this.Camera.SamplerState);
        }

        public override void Draw(Renderer renderer)
        {
            this.Camera.Clear(Color.Black);
            base.Draw(renderer);

            var map = this.Context.Map;

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

            this.Store.Fonts("Base", "debug").DrawString(renderer.Screen, new Vector2(0, 0), $"{world}", Color.White);
            this.Store.Fonts("Base", "debug").DrawString(renderer.Screen, new Vector2(0, 32), $"{highlightPos}", Color.White);
        }
    }
}
