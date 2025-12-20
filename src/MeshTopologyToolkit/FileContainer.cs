using System;
using System.Collections;
using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public class FileContainer
    {
        internal class Collection<T>: IList<T>, IReadOnlyList<T>
        {
            private HashSet<T> _existingValues = new HashSet<T>();
            private List<T> _values = new List<T>();

            public T this[int index] { get => ((IList<T>)_values)[index]; set => ((IList<T>)_values)[index] = value; }

            public int Count => ((ICollection<T>)_values).Count;

            public bool IsReadOnly => false;

            public bool TryAdd(T item)
            {
                if (item == null)
                    return false;

                if (_existingValues.Add(item))
                {
                    _values.Add(item);
                    return true;
                }

                return false;
            }

            public void Add(T item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                if (_existingValues.Add(item))
                {
                    _values.Add(item);
                }
                else
                {
                    throw new ArgumentException("Item already exists in collection", nameof(item));
                }
            }

            public void Clear()
            {
                _existingValues.Clear();
                _values.Clear();
            }

            public bool Contains(T item)
            {
                if (item == null)
                    return false;
                return _existingValues.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                ((ICollection<T>)_values).CopyTo(array, arrayIndex);
            }

            public IEnumerator<T> GetEnumerator()
            {
                return ((IEnumerable<T>)_values).GetEnumerator();
            }

            public int IndexOf(T item)
            {
                if (item == null)
                    return -1;
                return ((IList<T>)_values).IndexOf(item);
            }

            public void Insert(int index, T item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));
                if (_existingValues.Add(item))
                {
                    ((IList<T>)_values).Insert(index, item);
                    _values.Add(item);
                }
                else
                {
                    throw new ArgumentException("Item already exists in collection", nameof(item));
                }
            }

            public bool Remove(T item)
            {
                if (item == null)
                    return false;
                if (_existingValues.Remove(item))
                {
                    return ((ICollection<T>)_values).Remove(item);
                }
                return false;
            }

            public void RemoveAt(int index)
            {
                var item = _values[index];
                _existingValues.Remove(item);
                _values.RemoveAt(index);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_values).GetEnumerator();
            }
        }

        private readonly Collection<IMesh> _meshes = new Collection<IMesh>();
        private readonly Collection<Material> _materials = new Collection<Material>();
        private readonly Collection<Scene> _scenes = new Collection<Scene>();
        private readonly Collection<Texture> _textures = new Collection<Texture>();
        
        public IList<IMesh> Meshes => _meshes;

        public IList<Material> Materials => _materials;

        public IList<Scene> Scenes => _scenes;

        public IList<Texture> Textures => _textures;

        public bool Add(IMesh mesh)
        {
            return _meshes.TryAdd(mesh);
        }

        public bool Add(Material material)
        {
            return _materials.TryAdd(material);
        }

        public bool Add(Scene scene)
        {
            return _scenes.TryAdd(scene);
        }

        public bool Add(Texture texture)
        {
            return _textures.TryAdd(texture);
        }

        public MeshReference AddSingleMeshScene(IMesh mesh)
        {
            Add(mesh);
            var scene = new Scene(mesh.Name);
            var node = new Node(mesh.Name);
            node.Mesh = new MeshReference(mesh);
            scene.AddChild(node);
            Add(scene);
            return node.Mesh;
        }

        public MeshReference AddSingleMeshScene(MeshReference mesh)
        {
            if (mesh.Mesh == null)
                throw new ArgumentException($"No mesh provided");
            Add(mesh.Mesh);
            var scene = new Scene(mesh.Mesh.Name);
            var node = new Node(mesh.Mesh.Name);
            node.Mesh = mesh;
            scene.AddChild(node);
            Add(scene);
            foreach (var m in mesh.Materials)
            {
                if (m != null)
                {
                    Add(m);
                    foreach (var t in m.TextureParams)
                    {
                        Add(t.Value);
                    }
                }
            }
            return node.Mesh;
        }
    }
}
