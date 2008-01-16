using System;
using System.Collections.Generic;
using System.Text;

namespace Isles.Engine
{
    public class GameScene : ISceneManager
    {
        #region ISceneManager Members

        public void Add(ISceneObject sceneObject)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Remove(ISceneObject sceneObject)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IEnumerable<ISceneObject> SceneObjects
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool PointSceneIntersects(Microsoft.Xna.Framework.Vector3 point)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool RaySceneIntersects(Microsoft.Xna.Framework.Ray ray)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool SceneObjectIntersects(ISceneObject object1, ISceneObject object2)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IEnumerable<ISceneObject> SceneObjectsFromPoint(Microsoft.Xna.Framework.Vector3 point)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IEnumerable<ISceneObject> SceneObjectsFromRay(Microsoft.Xna.Framework.Ray ray)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public IEnumerable<ISceneObject> SceneObjectsFromRegion(Microsoft.Xna.Framework.BoundingBox boundingBox)
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
