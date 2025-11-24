using MeshTopologyToolkit.Converters;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public class MeshVertexAttributeConverterProvider: IMeshVertexAttributeConverterProvider
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
        }

        Dictionary<Key, IMeshVertexAttributeConverter> _converters = new Dictionary<Key, IMeshVertexAttributeConverter> ();

        public MeshVertexAttributeConverterProvider()
        {
            _converters[new Key(typeof(Vector2), typeof(Vector3))] = new Vector2ToVector3();
            _converters[new Key(typeof(Vector2), typeof(Vector4))] = new Vector2ToVector4();
            _converters[new Key(typeof(Vector3), typeof(Vector2))] = new Vector3ToVector2();
            _converters[new Key(typeof(Vector3), typeof(Vector4))] = new Vector3ToVector4();
            _converters[new Key(typeof(Vector4), typeof(Vector2))] = new Vector4ToVector2();
            _converters[new Key(typeof(Vector4), typeof(Vector3))] = new Vector4ToVector3();
        }

        public bool TryGetConverter<TFrom, TTo>(out IMeshVertexAttributeConverter<TFrom, TTo>? converter)
        {
            if (_converters.TryGetValue(new Key(typeof(TFrom), typeof(TFrom)), out var value))
            {
                converter = (IMeshVertexAttributeConverter<TFrom, TTo>)value;
                return true;
            }

            converter = null;
            return false;
        }
    }
}
