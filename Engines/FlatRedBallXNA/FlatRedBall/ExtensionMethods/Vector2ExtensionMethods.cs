﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Xna.Framework
{
    public static class Vector2ExtensionMethods
    {
        public static Vector2 FromAngle(float angle)
        {
            return new Vector2((float)Math.Cos(angle),
                (float)Math.Sin(angle));
        }

        public static float? Angle(this Vector2 vector)
        {
            if (vector.X == 0 && vector.Y == 0)
            {
                return null;
            }
            else
            {
                return (float)System.Math.Atan2(vector.Y, vector.X);
            }
        }

        public static Vector3 ToVector3(this Vector2 vector2)
        {
            var toReturn = new Vector3();

            toReturn.X = vector2.X;
            toReturn.Y = vector2.Y;

            return toReturn;
        }

        /// <summary>
        /// Attempts to normalize the vector, or returns Vector2.Zero if the argument vector has a lenth of 0.
        /// </summary>
        /// <param name="vector">The vector to normalize.</param>
        /// <returns>A normalized vector (length 1) or Vector2.Zero if the argument vector has a length of 0.</returns>
        public static Vector2 NormalizedOrZero(this Vector2 vector)
        {
            if (vector.X != 0 || vector.Y != 0)
            {
                vector.Normalize();
                return vector;
            }
            else
            {
                return Vector2.Zero;
            }
        }

        /// <summary>
        /// Returns a normalized vector. Throws an exception if the argument vector has a length of 0.
        /// </summary>
        /// <param name="vector">The vector to normalize.</param>
        /// <returns></returns>
        public static Vector2 Normalized(this Vector2 vector)
        {
            if(vector.X != 0 || vector.Y != 0)
            {
                vector.Normalize();
                return vector;
            }
            else
            {
                throw new InvalidOperationException("This vector is of length 0, so it cannot be normalized");
            }
        }

        /// <summary>
        /// Returns a vector in the same direction as the argument vector, but of the length specified by the length argument.
        /// </summary>
        /// <param name="vector2">The vector specifying the direction.</param>
        /// <param name="length">The desired length.</param>
        /// <returns>The resulting vector in the same direction as the argument of the desired length, or a vector of 0 length if the argument has 0 length.</returns>
        public static Vector2 AtLength(this Vector2 vector2, float length)
        {
            return vector2.NormalizedOrZero() * length;
        }
    }
}
