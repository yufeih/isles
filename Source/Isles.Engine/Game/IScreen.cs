using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Graphics;

namespace Isles.Engine
{
    /// <summary>
    /// Represents a game screen
    /// </summary>
    public interface IScreen : IDisposable
    {
        /// <summary>
        /// Called when this screen is activated
        /// </summary>
        void Enter();

        /// <summary>
        /// Called when this screen is deactivated
        /// </summary>
        void Leave();

        /// <summary>
        /// Handle game updates
        /// </summary>
        /// <param name="gameTime"></param>
        void Update(GameTime gameTime);

        /// <summary>
        /// Handle game draw event
        /// </summary>
        /// <param name="gameTime"></param>
        void Draw(GameTime gameTime);

        /// <summary>
        /// Load your graphics content.  If loadAllContent is true, you should
        /// load content from both ResourceManagementMode pools.  Otherwise, just
        /// load ResourceManagementMode.Manual content.
        /// </summary>
        /// <param name="loadAllContent">Which type of content to load.</param>
        void LoadContent();
        
        /// <summary>
        /// Unload your graphics content.  If unloadAllContent is true, you should
        /// unload content from both ResourceManagementMode pools.  Otherwise, just
        /// unload ResourceManagementMode.Manual content.  Manual content will get
        /// Disposed by the GraphicsDevice during a Reset.
        /// </summary>
        /// <param name="unloadAllContent">Which type of content to unload.</param>
        void UnloadContent();
    }
}
