using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Engine
{
    /// <summary>
    /// Interface for a scene manager.
    /// A scene manager provides manages ISceneObject instances,
    /// provide fast intersection detection, object occlusion, object
    /// query...
    /// Potential scene managers use techniques like BSP(Binary Space Partition),
    /// K-D Tree(QuadTree, OctTree...), Portal to efficiently manage
    /// the visibility and intersection of scene objects.
    /// </summary>
    public interface ISceneManager
    {

    }

    /// <summary>
    /// Interface for an object that can be drawed on the scene.
    /// ISceneObject instances are managed by a ISceneManager for
    /// efficient visibility test, collision detection, ray casting, etc.
    /// </summary>
    public interface ISceneObject
    {
        /// <summary>
        /// Gets the name of the scene object.
        /// You can query for a scene object by its name
        /// </summary>
        /// <remarks>
        /// DO NOT change the name of a scene object after it has
        /// been added to the scene manager.
        /// </remarks>
        string Name { get; }

        /// <summary>
        /// Gets the position of the scene object
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// Gets the axis-aligned bounding box of the scene object
        /// </summary>
        BoundingBox BoundingBox { get; }

        /// <summary>
        /// Gets or sets whether the position or bounding box of the
        /// scene object has changed since last update.
        /// </summary>
        /// <remarks>
        /// By mark the IsDirty property of a scene object, the scene
        /// manager will be able to adjust its internal data structure
        /// to adopt to the change of transformation.
        /// </remarks>
        bool IsDirty { get; set; }

        /// <summary>
        /// Update the scene object
        /// </summary>
        /// <param name="gameTime"></param>
        void Update(GameTime gameTime);

        /// <summary>
        /// Draw the scene object
        /// </summary>
        /// <param name="gameTime"></param>
        void Draw(GameTime gameTime);
    }
}
