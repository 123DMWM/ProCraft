﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
// With contributions by Conrad "Redshift" Morgan
using System;
using fCraft.Drawing;

namespace fCraft {
    /// <summary> Integer 3D vector. </summary>
    public struct Vector3I : IEquatable<Vector3I>, IComparable<Vector3I>, IComparable<Vector3F> {
        /// <summary> Zero-length vector (0,0,0) </summary>
        public static readonly Vector3I Zero = new Vector3I( 0, 0, 0 );

        /// <summary> Upwards-facing unit vector (0,0,1) </summary>
        public static readonly Vector3I Up = new Vector3I( 0, 0, 1 );

        /// <summary> Downwards-facing unit vector (0,0,-1) </summary>
        public static readonly Vector3I Down = new Vector3I( 0, 0, -1 );


        public int X, Y, Z;

        public int X2 {
            get { return X * X; }
        }

        public int Y2 {
            get { return Y * Y; }
        }

        public int Z2 {
            get { return Z * Z; }
        }


        public Vector3I( int x, int y, int z ) {
            X = x;
            Y = y;
            Z = z;
        }


        public Vector3I( Vector3I other ) {
            X = other.X;
            Y = other.Y;
            Z = other.Z;
        }


        public Vector3I( Vector3F other ) {
            X = (int)other.X;
            Y = (int)other.Y;
            Z = (int)other.Z;
        }


        public float Length {
            get { return (float)Math.Sqrt( (double)X * X + (double)Y * Y + (double)Z * Z ); }
        }

        public int LengthSquared {
            get { return X * X + Y * Y + Z * Z; }
        }


        public int this[ int i ] {
            get {
                switch( i ) {
                    case 0:
                        return X;
                    case 1:
                        return Y;
                    default:
                        return Z;
                }
            }
            set {
                switch( i ) {
                    case 0:
                        X = value;
                        return;
                    case 1:
                        Y = value;
                        return;
                    default:
                        Z = value;
                        return;
                }
            }
        }


        public int this[ Axis i ] {
            get {
                switch( i ) {
                    case Axis.X:
                        return X;
                    case Axis.Y:
                        return Y;
                    default:
                        return Z;
                }
            }
            set {
                switch( i ) {
                    case Axis.X:
                        X = value;
                        return;
                    case Axis.Y:
                        Y = value;
                        return;
                    default:
                        Z = value;
                        return;
                }
            }
        }


        #region Operations

        public static Vector3I operator +( Vector3I a, Vector3I b ) {
            return new Vector3I( a.X + b.X, a.Y + b.Y, a.Z + b.Z );
        }


        public static Vector3I operator -( Vector3I a, Vector3I b ) {
            return new Vector3I( a.X - b.X, a.Y - b.Y, a.Z - b.Z );
        }


        public static Vector3I operator *( Vector3I a, int scalar ) {
            return new Vector3I( a.X * scalar, a.Y * scalar, a.Z * scalar );
        }


        public static Vector3I operator *( int scalar, Vector3I a ) {
            return new Vector3I( a.X * scalar, a.Y * scalar, a.Z * scalar );
        }


        public static Vector3F operator *( Vector3I a, float scalar ) {
            return new Vector3F( a.X * scalar, a.Y * scalar, a.Z * scalar );
        }


        public static Vector3F operator *( float scalar, Vector3I a ) {
            return new Vector3F( a.X * scalar, a.Y * scalar, a.Z * scalar );
        }


        /// <summary> Integer division! </summary>
        public static Vector3I operator /( Vector3I a, int scalar ) {
            return new Vector3I( a.X / scalar, a.Y / scalar, a.Z / scalar );
        }


        public static Vector3F operator /( Vector3I a, float scalar ) {
            return new Vector3F( a.X / scalar, a.Y / scalar, a.Z / scalar );
        }


        #endregion


        #region Equality

        public override bool Equals( object obj ) {
            if( obj is Vector3I ) {
                return Equals( (Vector3I)obj );
            } else {
                return base.Equals( obj );
            }
        }


        public bool Equals( Vector3I other ) {
            return ( X == other.X ) && ( Y == other.Y ) && ( Z == other.Z );
        }


        public static bool operator ==( Vector3I a, Vector3I b ) {
            return a.Equals( b );
        }


        public static bool operator !=( Vector3I a, Vector3I b ) {
            return !a.Equals( b );
        }


        public override int GetHashCode() {
            return X + Z * 1625 + Y * 2642245;
        }

        #endregion


        #region Comparison

        public int CompareTo( Vector3I other ) {
            return Math.Sign( LengthSquared - other.LengthSquared );
        }


        public int CompareTo( Vector3F other ) {
            return Math.Sign( LengthSquared - other.LengthSquared );
        }


        public static bool operator >( Vector3I a, Vector3I b ) {
            return a.LengthSquared > b.LengthSquared;
        }


        public static bool operator <( Vector3I a, Vector3I b ) {
            return a.LengthSquared < b.LengthSquared;
        }


        public static bool operator >=( Vector3I a, Vector3I b ) {
            return a.LengthSquared >= b.LengthSquared;
        }


        public static bool operator <=( Vector3I a, Vector3I b ) {
            return a.LengthSquared <= b.LengthSquared;
        }

        #endregion


        public int Dot( Vector3I b ) {
            return ( X * b.X ) + ( Y * b.Y ) + ( Z * b.Z );
        }


        public float Dot( Vector3F b ) {
            return ( X * b.X ) + ( Y * b.Y ) + ( Z * b.Z );
        }


        public Vector3I Cross( Vector3I b ) {
            return new Vector3I( ( Y * b.Z ) - ( Z * b.Y ),
                                 ( Z * b.X ) - ( X * b.Z ),
                                 ( X * b.Y ) - ( Y * b.X ) );
        }


        public Vector3F Cross( Vector3F b ) {
            return new Vector3F( ( Y * b.Z ) - ( Z * b.Y ),
                                 ( Z * b.X ) - ( X * b.Z ),
                                 ( X * b.Y ) - ( Y * b.X ) );
        }


        public Axis LongestAxis {
            get {
                int maxVal = Math.Max( Math.Abs( X ), Math.Max( Math.Abs( Y ), Math.Abs( Z ) ) );
                if( maxVal == Math.Abs( X ) ) return Axis.X;
                if( maxVal == Math.Abs( Y ) ) return Axis.Y;
                return Axis.Z;
            }
        }

        public Axis ShortestAxis {
            get {
                int maxVal = Math.Min( Math.Abs( X ), Math.Min( Math.Abs( Y ), Math.Abs( Z ) ) );
                if( maxVal == Math.Abs( X ) ) return Axis.X;
                if( maxVal == Math.Abs( Y ) ) return Axis.Y;
                return Axis.Z;
            }
        }


        public override string ToString() {
            return String.Format("&S(X:&f{0}&S Y:&f{1}&S Z:&f{2}&S)", X, Y, Z);
        }


        public Vector3I Abs() {
            return new Vector3I( Math.Abs( X ), Math.Abs( Y ), Math.Abs( Z ) );
        }


        public Vector3F Normalize() {
            if( X == 0 && Y == 0 && Z == 0 ) return Vector3F.Zero;
            float len = Length;
            return new Vector3F( X / len, Y / len, Z / len );
        }


        #region Conversion

        public static explicit operator Position( Vector3I a ) {
            return new Position( a.X, a.Y, a.Z );
        }


        public static explicit operator Vector3F( Vector3I a ) {
            return new Vector3F( a.X, a.Y, a.Z );
        }


        public Position ToPlayerCoords() {
            return new Position( X * 32 + 16, Y * 32 + 16, Z * 32 + 16 );
        }
        
        public static Vector3I FlatDirection(byte yaw, byte pitch) {
            int dirX = 0, dirY = 0, dirZ = 0;
            if (pitch <= 32 || pitch >= 255) {
                if (yaw <= 32 || yaw >= 255) {
                    dirY = -1;
                } else if (yaw >= 33 && yaw <= 96) {
                    dirX = +1;
                } else if (yaw >= 97 && yaw <= 160) {
                    dirY = +1;
                } else if (yaw >= 161 && yaw <= 224) {
                    dirX = -1;
                }
            } else if (pitch >= 192 && pitch <= 224) {
                dirZ = +1;
            } else if (pitch >= 33 && pitch <= 65) {
                dirZ = -1;
            }
            return new Vector3I(dirX, dirY, dirZ);
        }

        #endregion
    }
}