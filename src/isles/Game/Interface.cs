// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Xml;
using Isles.Graphics;
using Microsoft.Xna.Framework;

namespace Isles.Engine
{
    /// <summary>
    /// Represents a game screen.
    /// </summary>
    public interface IScreen : IEventListener
    {
        /// <summary>
        /// Handle game updates.
        /// </summary>
        /// <param name="gameTime"></param>
        void Update(GameTime gameTime);

        /// <summary>
        /// Handle game draw event.
        /// </summary>
        /// <param name="gameTime"></param>
        void Draw(GameTime gameTime);
    }

    /// <summary>
    /// Interface for a class of object that can be load from a file
    /// and drawed on the scene.
    /// E.g., Fireballs, sparks, static rocks...
    /// </summary>
    public interface IWorldObject
    {
        /// <summary>
        /// Gets the position of the scene object.
        /// </summary>
        Vector3 Position { get; set; }

        /// <summary>
        /// Gets the axis-aligned bounding box of the scene object.
        /// </summary>
        BoundingBox BoundingBox { get; }

        /// <summary>
        /// Gets or sets scene manager data.
        /// </summary>
        object SceneManagerTag { get; set; }

        /// <summary>
        /// By marking the IsDirty property of a scene object, the scene
        /// manager will be able to adjust its internal data structure
        /// to adopt to the change of transformation.
        /// </summary>
        bool IsDirty { get; set; }

        /// <summary>
        /// Gets or sets whether this world object is active.
        /// </summary>
        /// <remarks>
        /// This property is internally used by ISceneManager.
        /// If you want to active or deactive a world object, call
        /// ISceneManager.Active or ISceneManager.Deactive instead.
        /// </remarks>
        bool IsActive { get; set; }

        /// <summary>
        /// Every world object belongs to a class represented by a string.
        /// The classID is not necessarily equal to the name of a class.
        /// This property is mainly used for serialization/deserialization.
        /// </summary>
        string ClassID { get; set; }

        /// <summary>
        /// Update the scene object.
        /// </summary>
        /// <param name="gameTime"></param>
        void Update(GameTime gameTime);

        /// <summary>
        /// Draw the scene object.
        /// </summary>
        /// <param name="gameTime"></param>
        void Draw(GameTime gameTime);

        /// <summary>
        /// Draw the scene object to a shadow map.
        /// </summary>
        void DrawShadowMap(GameTime gameTime, ShadowEffect shadow);

        /// <summary>
        /// Draw the scene object to a reflection map.
        /// </summary>
        void DrawReflection(GameTime gameTime, Matrix view, Matrix projection);

        /// <summary>
        /// Read and initialize the scene object from a set of attributes.
        /// </summary>
        /// <param name="reader"></param>
        void Deserialize(XmlElement xml);
    }

    /// <summary>
    /// Interface for a landscape.
    /// The landscape lays on the XY plane, Z value is used to represent the height.
    /// The position of the landscape is fixed at (0, 0).
    /// </summary>
    public interface ILandscape
    {
        /// <summary>
        /// Gets the size of the landscape.
        /// </summary>
        Vector3 Size { get; }

        /// <summary>
        /// Gets the height (Z value) of a point (x, y) on the landscape.
        /// </summary>
        float GetHeight(float x, float y);

        /// <summary>
        /// Gets the number of grids.
        /// </summary>
        Point GridCount { get; }

        /// <summary>
        /// Gets whether the point is walkable (E.g., above water).
        /// </summary>
        bool IsPointOccluded(float x, float y);
    }
}
