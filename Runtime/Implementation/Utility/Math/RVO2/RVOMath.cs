/*
 * RVOMath.cs
 * RVO2 Library C#
 *
 * SPDX-FileCopyrightText: 2008 University of North Carolina at Chapel Hill
 * SPDX-License-Identifier: Apache-2.0
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * Please send all bug reports to <geom@cs.unc.edu>.
 *
 * The authors may be contacted via:
 *
 * Jur van den Berg, Stephen J. Guy, Jamie Snape, Ming C. Lin, Dinesh Manocha
 * Dept. of Computer Science
 * 201 S. Columbia St.
 * Frederick P. Brooks, Jr. Computer Science Bldg.
 * Chapel Hill, N.C. 27599-3175
 * United States of America
 *
 * <http://gamma.cs.unc.edu/RVO2/>
 */

using System;
using XDay;

namespace RVO
{
    /**
     * <summary>Contains functions and constants used in multiple classes.
     * </summary>
     */
    public class RVOMath
    {
        /**
         * <summary>A sufficiently small positive number.</summary>
         */
        internal static FixedPoint RVO_EPSILON = (FixedPoint)0.00001f;

        /**
         * <summary>Computes the length of a specified two-dimensional vector.
         * </summary>
         *
         * <param name="vector">The two-dimensional vector whose length is to be
         * computed.</param>
         * <returns>The length of the two-dimensional vector.</returns>
         */
        public static FixedPoint abs(FixedVector2 vector)
        {
            return sqrt(absSq(vector));
        }

        /**
         * <summary>Computes the squared length of a specified two-dimensional
         * vector.</summary>
         *
         * <returns>The squared length of the two-dimensional vector.</returns>
         *
         * <param name="vector">The two-dimensional vector whose squared length
         * is to be computed.</param>
         */
        public static FixedPoint absSq(FixedVector2 vector)
        {
            return FixedVector2.Dot(vector, vector);
        }

        /**
         * <summary>Computes the normalization of the specified two-dimensional
         * vector.</summary>
         *
         * <returns>The normalization of the two-dimensional vector.</returns>
         *
         * <param name="vector">The two-dimensional vector whose normalization
         * is to be computed.</param>
         */
        public static FixedVector2 normalize(FixedVector2 vector)
        {
            return vector / abs(vector);
        }

        /**
         * <summary>Computes the determinant of a two-dimensional square matrix
         * with rows consisting of the specified two-dimensional vectors.
         * </summary>
         *
         * <returns>The determinant of the two-dimensional square matrix.
         * </returns>
         *
         * <param name="vector1">The top row of the two-dimensional square
         * matrix.</param>
         * <param name="vector2">The bottom row of the two-dimensional square
         * matrix.</param>
         */
        internal static FixedPoint det(FixedVector2 vector1, FixedVector2 vector2)
        {
            return vector1.X * vector2.Y - vector1.Y * vector2.X;
        }

        /**
         * <summary>Computes the squared distance from a line segment with the
         * specified endpoints to a specified point.</summary>
         *
         * <returns>The squared distance from the line segment to the point.
         * </returns>
         *
         * <param name="vector1">The first endpoint of the line segment.</param>
         * <param name="vector2">The second endpoint of the line segment.
         * </param>
         * <param name="vector3">The point to which the squared distance is to
         * be calculated.</param>
         */
        internal static FixedPoint distSqPointLineSegment(FixedVector2 vector1, FixedVector2 vector2, FixedVector2 vector3)
        {
            FixedPoint r =  FixedVector2.Dot(vector3 - vector1, vector2 - vector1) / absSq(vector2 - vector1);

            if (r < FixedPoint.Zero)
            {
                return absSq(vector3 - vector1);
            }

            if (r > FixedPoint.One)
            {
                return absSq(vector3 - vector2);
            }

            return absSq(vector3 - (vector1 + r * (vector2 - vector1)));
        }

        /**
         * <summary>Computes the absolute value of a FixedPoint.</summary>
         *
         * <returns>The absolute value of the FixedPoint.</returns>
         *
         * <param name="scalar">The FixedPoint of which to compute the absolute
         * value.</param>
         */
        internal static FixedPoint fabs(FixedPoint scalar)
        {
            return FixedMath.Abs(scalar);
        }

        /**
         * <summary>Computes the signed distance from a line connecting the
         * specified points to a specified point.</summary>
         *
         * <returns>Positive when the point c lies to the left of the line ab.
         * </returns>
         *
         * <param name="a">The first point on the line.</param>
         * <param name="b">The second point on the line.</param>
         * <param name="c">The point to which the signed distance is to be
         * calculated.</param>
         */
        internal static FixedPoint leftOf(FixedVector2 a, FixedVector2 b, FixedVector2 c)
        {
            return det(a - c, b - a);
        }

        /**
         * <summary>Computes the square of a FixedPoint.</summary>
         *
         * <returns>The square of the FixedPoint.</returns>
         *
         * <param name="scalar">The FixedPoint to be squared.</param>
         */
        internal static FixedPoint sqr(FixedPoint scalar)
        {
            return scalar * scalar;
        }

        /**
         * <summary>Computes the square root of a FixedPoint.</summary>
         *
         * <returns>The square root of the FixedPoint.</returns>
         *
         * <param name="scalar">The FixedPoint of which to compute the square root.
         * </param>
         */
        internal static FixedPoint sqrt(FixedPoint scalar)
        {
            return FixedMath.Sqrt(scalar);
        }
    }
}
