﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MathNet.Numerics.LinearAlgebra.Double;

namespace HomographySharp
{
    public static class HomographyHelper
    {
        /// <summary>
        /// return DenseVector.OfArray(new double[] { x, y })
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>DenseVector.OfArray(new double[] { x, y })</returns>
        public static DenseVector CreateVector2(double x, double y)
        {
            return DenseVector.OfArray(new double[] { x, y });
        }

        /// <summary>
        /// </summary>
        /// <param name="matrix">(疑似)逆行列にされる行列</param>
        /// <param name="dstVector"></param>
        /// <param name="pointNum">対応点の数</param>
        /// <returns></returns>
        private static DenseMatrix InverseAndMultiplicate(DenseMatrix matrix, DenseVector dstVector, int pointNum)
        {
            MathNet.Numerics.LinearAlgebra.Matrix<double> inverseA;

            if (pointNum == 4)
            {
                inverseA = matrix.Inverse();
            }
            else
            {
                inverseA = matrix.PseudoInverse();
            }

            var parameterVec = inverseA * dstVector;

            return DenseMatrix.OfArray(new double[,]
            {
                {parameterVec[0], parameterVec[1], parameterVec[2]},
                {parameterVec[3], parameterVec[4], parameterVec[5]},
                {parameterVec[6], parameterVec[7], 1}
            });
        }

        /// <summary>
        /// All vectors contained in srcPoints and dstPoints must be two dimensional(x and y).
        /// </summary>
        /// <param name="srcPoints">need 4 or more points before translate</param>
        /// <param name="dstPoints">need 4 or more points after translate</param>
        /// <exception cref="ArgumentException">srcPoints and dstPoints must require 4 or more points</exception>
        /// <exception cref="ArgumentException">srcPoints and dstPoints must same num</exception>
        /// <exception cref="ArgumentException">All vectors contained in srcPoints and dstPoints must be two dimensional(x and y).</exception>
        /// <returns>Homography Matrix</returns>
        public static DenseMatrix FindHomography(List<DenseVector> srcPoints, List<DenseVector> dstPoints)
        {
            if (srcPoints.Count < 4 || dstPoints.Count < 4)
            {
                throw new ArgumentException("srcPoints and dstPoints must require 4 or more points");
            }

            if (srcPoints.Count != dstPoints.Count)
            {
                throw new ArgumentException("srcPoints and dstPoints must same num");
            }

            if (srcPoints.Any(x => x.Count != 2) || dstPoints.Any(x => x.Count != 2))
            {
                throw new ArgumentException("All vectors contained in srcPoints and dstPoints must be two dimensional(x and y).");
            }

            //q(dstのベクトル) = A(作成するべきnx8行列) * P(射影変換のパラメータ)
            //P = A^-1 * q
            //でパラメータが求まる。
            int pointNum = srcPoints.Count;
            DenseMatrix a = DenseMatrix.Create(pointNum * 2, 8, 0);

            for (int i = 0; i < pointNum; i++)
            {
                var src = srcPoints[i];
                var dst = dstPoints[i];

                var srcX = src[0];
                var dstX = dst[0];

                var srcY = src[1];
                var dstY = dst[1];

                var row1 = DenseVector.OfArray(new double[] { srcX, srcY, 1, 0, 0, 0, -dstX * srcX, -dstX * srcY });
                var row2 = DenseVector.OfArray(new double[] { 0, 0, 0, srcX, srcY, 1, -dstY * srcX, -dstY * srcY });

                a.SetRow(2 * i, row1);
                a.SetRow(2 * i + 1, row2);
            }

            var dstVec = DenseVector.Create(pointNum * 2, 0);

            for (int i = 0; i < pointNum; i++)
            {
                dstVec[i * 2] = dstPoints[i][0];
                dstVec[i * 2 + 1] = dstPoints[i][1];
            }

            return InverseAndMultiplicate(a, dstVec, pointNum);
        }

        /// <summary>
        /// </summary>
        /// <param name="srcPoints">need 4 or more points before translate </param>
        /// <param name="dstPoints">need 4 or more points after translate</param>
        /// <exception cref="ArgumentException">srcPoints and dstPoints must require 4 or more points</exception>
        /// <exception cref="ArgumentException">srcPoints and dstPoints must same num</exception>
        /// <returns>Homography Matrix</returns>
        public static DenseMatrix FindHomography(List<PointF> srcPoints, List<PointF> dstPoints)
        {
            if (srcPoints.Count < 4 || dstPoints.Count < 4)
            {
                throw new ArgumentException("srcPoints and dstPoints must require 4 or more points");
            }
            if (srcPoints.Count != dstPoints.Count)
            {
                throw new ArgumentException("srcPoints and dstPoints must same num");
            }

            //q(dstのベクトル) = A(作成するべきnx8行列) * P(射影変換のパラメータ)
            //P = A^-1 * q
            //でパラメータが求まる。
            int pointNum = srcPoints.Count;
            DenseMatrix a = DenseMatrix.Create(pointNum * 2, 8, 0);

            for (int i = 0; i < pointNum; i++)
            {
                var src = srcPoints[i];
                var dst = dstPoints[i];

                var row1 = DenseVector.OfArray(new double[] { src.X, src.Y, 1, 0, 0, 0, -dst.X * src.X, -dst.X * src.Y });
                var row2 = DenseVector.OfArray(new double[] { 0, 0, 0, src.X, src.Y, 1, -dst.Y * src.X, -dst.Y * src.Y });

                a.SetRow(i * 2, row1);
                a.SetRow(i * 2 + 1, row2);
            }

            var dstVec = DenseVector.Create(pointNum * 2, 0);

            for (int i = 0; i < pointNum; i++)
            {
                dstVec[i * 2] = dstPoints[i].X;
                dstVec[i * 2 + 1] = dstPoints[i].Y;
            }

            return InverseAndMultiplicate(a, dstVec, pointNum);
        }

        public static (double dstX, double dstY) Translate(DenseMatrix homography, double srcX, double srcY)
        {
            // ↓ in this case, allocation occurs
            //var vec = DenseVector.OfArray(new double[] { srcX, srcY, 1 });
            //var dst = homography * vec;
            //return (dst[0] / dst[2], dst[1] / dst[2]);

            var dst1 = homography[0, 0] * srcX + homography[0, 1] * srcY + homography[0, 2];
            var dst2 = homography[1, 0] * srcX + homography[1, 1] * srcY + homography[1, 2];
            var dst3 = homography[2, 0] * srcX + homography[2, 1] * srcY + homography[2, 2];
            return (dst1 / dst3, dst2 / dst3);
        }
    }
}
