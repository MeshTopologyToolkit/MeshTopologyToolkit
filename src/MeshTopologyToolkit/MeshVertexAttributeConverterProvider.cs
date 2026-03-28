using MeshTopologyToolkit.Converters;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public class MeshVertexAttributeConverterProvider : IMeshVertexAttributeConverterProvider
    {
        public static readonly MeshVertexAttributeConverterProvider Default = new MeshVertexAttributeConverterProvider();

        struct Key
        {
            public Type From;
            public Type To;

            public Key(Type from, Type to)
            {
                From = from;
                To = to;
            }

            public override string ToString()
            {
                return $"{From.Name} -> {To.Name}";
            }
        }

        private Dictionary<Key, IMeshVertexAttributeConverter> _converters = new Dictionary<Key, IMeshVertexAttributeConverter>();

        public MeshVertexAttributeConverterProvider()
        {
            _converters[new Key(typeof(Vector2), typeof(Vector3))] = new Vector2ToVector3();
            _converters[new Key(typeof(Vector2), typeof(Vector4))] = new Vector2ToVector4();
            _converters[new Key(typeof(Vector3), typeof(Vector2))] = new Vector3ToVector2();
            _converters[new Key(typeof(Vector3), typeof(Vector4))] = new Vector3ToVector4();
            _converters[new Key(typeof(Vector4), typeof(Vector2))] = new Vector4ToVector2();
            _converters[new Key(typeof(Vector4), typeof(Vector3))] = new Vector4ToVector3();
            _converters[new Key(typeof(Vector4), typeof(Color32))] = new Vector4ToColor32();
            _converters[new Key(typeof(Vector3), typeof(Color32))] = new Vector3ToColor32();
            _converters[new Key(typeof(Color32), typeof(Vector4))] = new Color32ToVector4();
            _converters[new Key(typeof(Color32), typeof(Vector3))] = new Color32ToVector3();
        }

        public bool TryGetConverter<TFrom, TTo>(out IMeshVertexAttributeConverter<TFrom, TTo>? converter)
        {
            var key = new Key(typeof(TFrom), typeof(TTo));
            if (_converters.TryGetValue(key, out var value))
            {
                converter = (IMeshVertexAttributeConverter<TFrom, TTo>)value;
                return true;
            }

            converter = null;
            return false;
        }

        public bool TryGetConverter(Type from, Type to, out IMeshVertexAttributeConverter? converter)
        {
            var key = new Key(from, to);
            if (_converters.TryGetValue(key, out var value))
            {
                converter = value;
                return true;
            }

            converter = null;
            return false;
        }
    }
}
