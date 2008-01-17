using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Engine
{
    public class SceneManager : ISceneManager
    {
        LinkedList<ISceneObject> sceneObjects = new LinkedList<ISceneObject>();

        #region ISceneManager Members

        public void Add(ISceneObject sceneObject)
        {
            sceneObjects.AddFirst(sceneObject);
        }

        public void Remove(ISceneObject sceneObject)
        {
            System.Diagnostics.Debug.Assert(sceneObjects.Contains(sceneObject));
            sceneObjects.Remove(sceneObject);
        }

        public void Update(GameTime gameTime)
        {
            foreach (ISceneObject sceneObject in sceneObjects)
                sceneObject.Update(gameTime);
        }

        public void Draw(GameTime gameTime)
        {
            foreach (ISceneObject sceneObject in sceneObjects)
                sceneObject.Draw(gameTime);
        }

        public IEnumerable<ISceneObject> SceneObjects
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool PointSceneIntersects(Vector3 point)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool RaySceneIntersects(Ray ray)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool SceneObjectIntersects(ISceneObject object1, ISceneObject object2)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IEnumerable<ISceneObject> SceneObjectsFromPoint(Vector3 point)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IEnumerable<ISceneObject> SceneObjectsFromRay(Ray ray)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IEnumerable<ISceneObject> SceneObjectsFromRegion(BoundingBox boundingBox)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public ISceneObject SceneObjectFromName(string name)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}
