/* Based on implementation of 3D Perlin Noise after Ken Perlin's reference implementation by Rene Schulte
 * Original Copyright (c) 2009 Rene Schulte
 * License.txt reproduced below

Microsoft Public License (Ms-PL)
[OSI Approved License]

This license governs use of the accompanying software. If you use the software, you
accept this license. If you do not accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the
same meaning here as under U.S. copyright law.
A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.

 */

using System;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Implementation of 3D Perlin Noise after Ken Perlin's reference implementation. </summary>
    public sealed class PerlinNoise3D {
        #region Fields

        private readonly int[] permutation, p;

        #endregion

        #region Properties

        public float Frequency { get; set; }
        public float Amplitude { get; set; }
        public float Persistence { get; set; }
        public int Octaves { get; set; }

        #endregion

        #region Contructors

        public PerlinNoise3D( [NotNull] Random rand ) {
            if( rand == null ) throw new ArgumentNullException( "rand" );
            permutation = new int[256];
            p = new int[permutation.Length * 2];
            InitNoiseFunctions( rand );

            // Default values
            Frequency = 0.023f;
            Amplitude = 2.2f;
            Persistence = 0.9f;
            Octaves = 2;
        }

        #endregion

        #region Methods

        public void InitNoiseFunctions( [NotNull] Random rand ) {
            if( rand == null ) throw new ArgumentNullException( "rand" );

            // Fill empty
            for( int i = 0; i < permutation.Length; i++ ) {
                permutation[i] = -1;
            }

            // Generate random numbers
            for( int i = 0; i < permutation.Length; i++ ) {
                while( true ) {
                    int iP = rand.Next() % permutation.Length;
                    if( permutation[iP] == -1 ) {
                        permutation[iP] = i;
                        break;
                    }
                }
            }

            // Copy
            for( int i = 0; i < permutation.Length; i++ ) {
                p[permutation.Length + i] = p[i] = permutation[i];
            }
        }


        public float Compute( float x, float y, float z ) {
            float noise = 0;
            float amp = Amplitude;
            float freq = Frequency;
            for( int i = 0; i < Octaves; i++ ) {
                noise += Noise( x * freq, y * freq, z * freq ) * amp;
                freq *= 2;                                // octave is the double of the previous frequency
                amp *= Persistence;
            }
            return noise;
        }


        private float Noise( float x, float y, float z ) {
            int xF = x >= 0 ? (int)x : (int)x - 1; // Floor the values
            int yF = y >= 0 ? (int)y : (int)y - 1;
            int zF = z >= 0 ? (int)z : (int)z - 1;
            
            // Find unit cube that contains point
            int iX = xF & 255, iY = yF & 255, iZ = zF & 255;

            // Find relative x, y, z of the point in the cube.
            x -= xF; y -= yF; z -= zF;

            // Compute fade curves for each of x, y, z
            float u = Fade( x );
            float v = Fade( y );
            float w = Fade( z );

            // Hash coordinates of the 8 cube corners
            int a = p[iX] + iY, aa = p[a] + iZ, ab = p[a + 1] + iZ;
            int b = p[iX + 1] + iY, ba = p[b] + iZ, bb = p[b + 1] + iZ;

            // And add blended results from 8 corners of cube.
            return Lerp( w, Lerp( v, Lerp( u, Grad( p[aa], x, y, z ),
                               Grad( p[ba], x - 1, y, z ) ),
                       Lerp( u, Grad( p[ab], x, y - 1, z ),
                               Grad( p[bb], x - 1, y - 1, z ) ) ),
               Lerp( v, Lerp( u, Grad( p[aa + 1], x, y, z - 1 ),
                               Grad( p[ba + 1], x - 1, y, z - 1 ) ),
                       Lerp( u, Grad( p[ab + 1], x, y - 1, z - 1 ),
                               Grad( p[bb + 1], x - 1, y - 1, z - 1 ) ) ) );
        }


        private static float Fade( float t ) {
            // Smooth interpolation parameter
            return (t * t * t * (t * (t * 6 - 15) + 10));
        }


        private static float Lerp( float alpha, float a, float b ) {
            // Linear interpolation
            return (a + alpha * (b - a));
        }


        private static float Grad( int hashCode, float x, float y, float z ) {
            // Convert lower 4 bits of hash code into 12 gradient directions
            switch( hashCode & 0xF ) {
                case 0x0: return  x + y;
                case 0x1: return -x + y;
                case 0x2: return  x - y;
                case 0x3: return -x - y;
                case 0x4: return  x + z;
                case 0x5: return -x + z;
                case 0x6: return  x - z;
                case 0x7: return -x - z;
                case 0x8: return  y + z;
                case 0x9: return -y + z;
                case 0xA: return  y - z;
                case 0xB: return -y - z;
                case 0xC: return  y + x;
                case 0xD: return -y + z;
                case 0xE: return  y - x;
                case 0xF: return -y - z;
                default: return 0; // never happens
            }
        }

        #endregion
    }
}