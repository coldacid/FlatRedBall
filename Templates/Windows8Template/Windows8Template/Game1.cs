using System;
using System.Collections.Generic;
using System.Reflection;

using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Screens;
using Microsoft.Xna.Framework;

using System.Linq;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Windows8Template
{
    public partial class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;

        partial void GeneratedInitialize();
        partial void GeneratedUpdate(Microsoft.Xna.Framework.GameTime gameTime);
        partial void GeneratedDraw(Microsoft.Xna.Framework.GameTime gameTime);

        public Game1() : base()
        {
            graphics = new GraphicsDeviceManager(this);

#if WINDOWS_PHONE || ANDROID || IOS

            // Frame rate is 30 fps by default for Windows Phone,
            // so let's keep that for other phones too
            TargetElapsedTime = TimeSpan.FromTicks(333333);
            graphics.IsFullScreen = true;
#elif WINDOWS || DESKTOP_GL
            graphics.PreferredBackBufferWidth = 800;
            graphics.PreferredBackBufferHeight = 600;
#endif


#if WINDOWS_8
            FlatRedBall.Instructions.Reflection.PropertyValuePair.TopLevelAssembly = 
                this.GetType().GetTypeInfo().Assembly;
#endif

        }

        protected override void Initialize()
        {
            #if IOS
            var bounds = UIKit.UIScreen.MainScreen.Bounds;
            var nativeScale = UIKit.UIScreen.MainScreen.Scale;
            var screenWidth = (int)(bounds.Width * nativeScale);
            var screenHeight = (int)(bounds.Height * nativeScale);
            graphics.PreferredBackBufferWidth = screenWidth;
            graphics.PreferredBackBufferHeight = screenHeight;
            #endif
        
            FlatRedBallServices.InitializeFlatRedBall(this, graphics);


            Type startScreenType = null;

            var commandLineArgs = Environment.GetCommandLineArgs();
            if (commandLineArgs.Length > 0)
            {
                var thisAssembly = this.GetType().Assembly;
                // see if any of these are screens:
                foreach (var item in commandLineArgs)
                {
                    var type = thisAssembly.GetType(item);

                    if (type != null)
                    {
                        startScreenType = type;
                        break;
                    }
                }
            }

            // Call this before starting the screens, so that plugins can initialize their systems.
            GeneratedInitialize();

            if (startScreenType != null)
            {
                FlatRedBall.Screens.ScreenManager.Start(startScreenType);
            }


            base.Initialize();
        }


        protected override void Update(GameTime gameTime)
        {
            FlatRedBallServices.Update(gameTime);

            FlatRedBall.Screens.ScreenManager.Activity();

            GeneratedUpdate(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            FlatRedBallServices.Draw();

            GeneratedDraw(gameTime);

            base.Draw(gameTime);
        }
    }
}
