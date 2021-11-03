// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Graphics
{
    public class Model
    {
        public Mesh[] Meshes { get; init; }
        public Node[] Nodes { get; init; }
        public Dictionary<string, Node> NodeNames { get; init; }
        public Dictionary<string, Animation> Animations { get; init; }

        public class Mesh
        {
            public Node Node { get; init; }
            public Primitive[] Primitives { get; init; }
            public Matrix[] InverseBindMatrices { get; init; }
            public Node[] Joints { get; init; }
        }

        public class Primitive
        {
            public VertexBuffer VertexBuffer { get; init; }
            public IndexBuffer IndexBuffer { get; init; }
            public BoundingBox BoundingBox { get; init; }
            public Texture2D Texture { get; init; }
            public bool DoubleSided { get; init; }
            public int VertexStride { get; init; }
            public int NumVertices { get; init; }
            public int PrimitiveCount { get; init; }
        }

        public class Node
        {
            public int Index { get; init; }
            public int ParentIndex { get; init; }
            public string Name { get; init; }
            public Vector3 Scale { get; init; }
            public Vector3 Translation { get; init; }
            public Quaternion Rotation { get; init; }
        }

        public class Animation
        {
            public float Duration { get; init; }
            public Dictionary<int, Channel> Channels { get; init; }

            public class Channel
            {
                public Keyframes<Vector3> Scale { get; internal set; }
                public Keyframes<Quaternion> Rotation { get; internal set; }
                public Keyframes<Vector3> Translation { get; internal set; }

                public struct Keyframes<T>
                {
                    public float[] Times { get; set; }
                    public T[] Values { get; set; }
                }
            }
        }
    }
}
