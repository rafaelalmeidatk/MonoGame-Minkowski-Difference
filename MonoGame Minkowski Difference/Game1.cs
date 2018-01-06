using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame_Minkowski_Difference.Extensions;

namespace MonoGame_Minkowski_Difference
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        private Texture2D _dotTexture;
        private AABB _boxA;
        private AABB _boxB;
        private AABB _mdBox;

        private bool _isColliding;
        private Vector2 _penetractionVector;

        private SpriteFont _spriteFont;

        private KeyboardState _oldKeyboardState;
        private KeyboardState _currentKeyboardState;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            IsMouseVisible = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            _boxA = new AABB(new Vector2(graphics.PreferredBackBufferWidth / 2.0f, 0), new Vector2(10, 10),
                new Vector2(0, 100), new Vector2(0, 1000));
            _boxB = new AABB(new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight * 2 - 100) / 2,
                new Vector2(graphics.PreferredBackBufferWidth / 2.5f, 20)); 
            _mdBox = new AABB(Vector2.Zero, Vector2.Zero);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            _dotTexture = new Texture2D(graphics.GraphicsDevice, 1, 1);
            _dotTexture.SetData(new[] { Color.White });

            _spriteFont = Content.Load<SpriteFont>("Font");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            _oldKeyboardState = _currentKeyboardState;
            _currentKeyboardState = Keyboard.GetState();

            // Mouse movement
            if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                _boxA.Center = Mouse.GetState().Position.ToVector2();

            // Delta time
            var deltaTime = (float) gameTime.ElapsedGameTime.TotalSeconds;
            const float moveV = 200.0f;

            if (IsKeyDown(Keys.A) && !IsKeyDown(Keys.D))
            {
                _boxA.Velocity.X = -moveV;
            } 
            else if (!IsKeyDown(Keys.A) && IsKeyDown(Keys.D))
            {
                _boxA.Velocity.X = moveV;
            }
            else
            {
                _boxA.Velocity.X = 0;
            }

            if (IsKeyPressed(Keys.W))
            {
                _boxA.Velocity.Y = -300;
            }

            // acceleration
            _boxA.Velocity += _boxA.Acceleration * deltaTime;
            _boxB.Velocity += _boxB.Acceleration * deltaTime;

            // move
            _boxA.Center += _boxA.Velocity * deltaTime;
            _boxB.Center += _boxB.Velocity * deltaTime;

            // construct the relative velocity ray
            var rvRay = (_boxA.Velocity - _boxB.Velocity) * deltaTime;

            // collision check
            var md = _boxB.MinkowskiDifference(_boxA);
            if (md.Min.X <= 0 &&
                md.Max.X >= 0 &&
                md.Min.Y <= 0 &&
                md.Max.Y >= 0)
            {
                _isColliding = true;

                // penetration depth
                _penetractionVector = md.ClosestPointOnBoundsToPoint(Vector2.Zero);

                // move the box out of the penetration
                _boxA.Center += _penetractionVector;
                
                if (_penetractionVector != Vector2.Zero)
                {
                    var tangent = _penetractionVector.NormalizedCopy().Tangent();
                    _boxA.Velocity = Vector2.Dot(_boxA.Velocity, tangent) * tangent;
                    _boxB.Velocity = Vector2.Dot(_boxB.Velocity, tangent) * tangent;
                }
            }
            else
            {
                _isColliding = false;

                var intersectFraction = md.GetRayIntersectionFraction(Vector2.Zero, rvRay);
                if (intersectFraction < float.PositiveInfinity)
                {
                    _isColliding = true;
                    
                    // move the boxes appropriately
                    _boxA.Center += _boxA.Velocity * deltaTime * intersectFraction;
                    _boxB.Center += _boxB.Velocity * deltaTime * intersectFraction;

                    // zero out the normal of the collision
                    var nrvRay = rvRay.NormalizedCopy();
                    var tangent = new Vector2(-nrvRay.Y, nrvRay.X);//nrvRay.Tangent();
                    _boxA.Velocity = Vector2.Dot(_boxA.Velocity, tangent) * tangent;
                    _boxB.Velocity = Vector2.Dot(_boxB.Velocity, tangent) * tangent;
                }
            }

            _mdBox = md;

            base.Update(gameTime);
        }

        private bool IsKeyDown(Keys key)
        {
            return _currentKeyboardState.IsKeyDown(key);
        }

        private bool IsKeyPressed(Keys key)
        {
            return _oldKeyboardState.IsKeyUp(key) && _currentKeyboardState.IsKeyDown(key);
        }
        
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            var aabbColor = _isColliding ? Color.Green : Color.Red;
            spriteBatch.Draw(_dotTexture, new Rectangle(_boxB.Min.ToPoint(), _boxB.Size.ToPoint()), Color.Gray);
            spriteBatch.Draw(_dotTexture, new Rectangle(_boxA.Min.ToPoint(), _boxA.Size.ToPoint()), aabbColor * 0.5f);
            spriteBatch.Draw(_dotTexture, new Rectangle(_mdBox.Min.ToPoint(), _mdBox.Size.ToPoint()), Color.Blue * 0.5f);

            spriteBatch.DrawString(_spriteFont, $"Min Minkowski Difference: {_mdBox.Min}", new Vector2(20, 40), Color.White);
            spriteBatch.DrawString(_spriteFont, $"Max Minkowski Difference: {_mdBox.Max}", new Vector2(20, 60), Color.White);
            spriteBatch.DrawString(_spriteFont, $"Penetraction Vector: {_penetractionVector}", new Vector2(20, 80), Color.White);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
