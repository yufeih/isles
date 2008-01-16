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
        /// <summary>
        /// Adds a new scene object to the scene manager
        /// </summary>
        /// <param name="sceneObject"></param>
        void Add(ISceneObject sceneObject);

        /// <summary>
        /// Removes an existing scene object from the scene manager
        /// </summary>
        /// <param name="sceneObject"></param>
        void Remove(ISceneObject sceneObject);

        /// <summary>
        /// Updates the scene manager and all scene objects under control
        /// </summary>
        /// <param name="gameTime"></param>
        void Update(GameTime gameTime);

        /// <summary>
        /// Draw all the scene objects under control
        /// </summary>
        /// <param name="gameTime"></param>
        void Draw(GameTime gameTime);

        /// <summary>
        /// Gets all scene objects managed by this scene manager
        /// </summary>
        IEnumerable<ISceneObject> SceneObjects { get; }

        /// <summary>
        /// Tests to see if a point intersects the scene
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        bool PointSceneIntersects(Vector3 point);

        /// <summary>
        /// Tests to see if a ray intersects the scene
        /// </summary>
        /// <param name="ray"></param>
        /// <returns></returns>
        bool RaySceneIntersects(Ray ray);

        /// <summary>
        /// Tests to see if two scene objects intersects
        /// </summary>
        /// <param name="object1"></param>
        /// <param name="object2"></param>
        /// <returns></returns>
        bool SceneObjectIntersects(ISceneObject object1, ISceneObject object2);

        /// <summary>
        /// Performs a point vs scene test and returns all the ISceneObjects occupy the point
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        IEnumerable<ISceneObject> SceneObjectsFromPoint(Vector3 point);

        /// <summary>
        /// Performs a ray vs scene test and returns all the ISceneObjects occupy the point
        /// </summary>
        /// <param name="ray"></param>
        /// <returns></returns>
        IEnumerable<ISceneObject> SceneObjectsFromRay(Ray ray);

        /// <summary>
        /// Gets all the scene objects within a bounding box
        /// </summary>
        /// <param name="boundingBox"></param>
        /// <returns></returns>
        IEnumerable<ISceneObject> SceneObjectsFromRegion(BoundingBox boundingBox);

        /// <summary>
        /// Gets the scene object from a name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        ISceneObject SceneObjectFromName(string name);
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
