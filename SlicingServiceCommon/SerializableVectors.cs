using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SlicingServiceCommon
{
    [System.Serializable]
    public class SerializableVector3
    {
        /// <summary>
        /// x component
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// y component
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// z component
        /// </summary>
        public float Z { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="rX"></param>
        /// <param name="rY"></param>
        /// <param name="rZ"></param>
        public SerializableVector3(float rX, float rY, float rZ)
        {
            X = rX;
            Y = rY;
            Z = rZ;
        }

        public SerializableVector3()
        {
            X = 0f;
            Y = 0f;
            Z = 0f;
        }

        /// <summary>
        /// Returns a string representation of the object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("[{0}, {1}, {2}]", X, Y, Z);
        }

        /// <summary>
        /// Automatic conversion from SerializableVector3 to Vector3
        /// </summary>
        /// <param name="rValue"></param>
        /// <returns></returns>
        public static implicit operator Vector3(SerializableVector3 rValue)
        {
            return new Vector3(rValue.X, rValue.Y, rValue.Z);
        }

        /// <summary>
        /// Automatic conversion from Vector3 to SerializableVector3
        /// </summary>
        /// <param name="rValue"></param>
        /// <returns></returns>
        public static implicit operator SerializableVector3(Vector3 rValue)
        {
            return new SerializableVector3(rValue.X, rValue.Y, rValue.Z);
        }
    }

    [System.Serializable]
    public class SerializableVector2
    {
        /// <summary>
        /// x component
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// y component
        /// </summary>
        public float Y { get; set; }

        public SerializableVector2()
        {
            X = 0f;
            Y = 0f;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="rX"></param>
        /// <param name="rY"></param>
        public SerializableVector2(float rX, float rY)
        {
            X = rX;
            Y = rY;
        }

        /// <summary>
        /// Returns a string representation of the object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("[{0}, {1}]", X, Y);
        }

        /// <summary>
        /// Automatic conversion from SerializableVector3 to Vector3
        /// </summary>
        /// <param name="rValue"></param>
        /// <returns></returns>
        public static implicit operator Vector2(SerializableVector2 rValue)
        {
            return new Vector2(rValue.X, rValue.Y);
        }

        /// <summary>
        /// Automatic conversion from Vector3 to SerializableVector3
        /// </summary>
        /// <param name="rValue"></param>
        /// <returns></returns>
        public static implicit operator SerializableVector2(Vector2 rValue)
        {
            return new SerializableVector2(rValue.X, rValue.Y);
        }
    }

}
