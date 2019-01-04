﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Provides utility methods for working with IP addresses and ranges. </summary>
    public static class IPAddressUtil {
        static readonly Regex RegexIP =
            new Regex( @"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b",
                       RegexOptions.Compiled );


        /// <summary> Checks whether an IP address may belong to LAN or localhost (192.168.0.0/16, 10.0.0.0/24, or 127.0.0.0/24). </summary>
        /// <param name="addr"> IPv4 address to check. </param>
        /// <returns> True if given IP is local; otherwise false. </returns>
        /// <exception cref="ArgumentNullException"> If addr is null. </exception>
        public static bool IsLocal( [NotNull] this IPAddress addr ) {
            if( addr == null ) throw new ArgumentNullException( "addr" );
            byte[] bytes = addr.GetAddressBytes();
            return ( bytes[0] == 192 && bytes[1] == 168 ) ||
                   ( bytes[0] == 10 ) ||
                   ( bytes[0] == 127 );
        }


        /// <summary> Represents an IPv4 address as an unsigned integer. </summary>
        /// <exception cref="ArgumentNullException"> If thisAddr is null. </exception>
        public static uint AsUInt( [NotNull] this IPAddress thisAddr ) {
            if( thisAddr == null ) throw new ArgumentNullException( "thisAddr" );
            return (uint)IPAddress.HostToNetworkOrder( BitConverter.ToInt32( thisAddr.GetAddressBytes(), 0 ) );
        }


        /// <summary> Represents an IPv4 address as a signed integer. </summary>
        /// <exception cref="ArgumentNullException"> If thisAddr is null. </exception>
        public static int AsInt( [NotNull] this IPAddress thisAddr ) {
            if( thisAddr == null ) throw new ArgumentNullException( "thisAddr" );
            return IPAddress.HostToNetworkOrder( BitConverter.ToInt32( thisAddr.GetAddressBytes(), 0 ) );
        }


        /// <summary> Checks to see if the specified string is a valid IPv4 address. </summary>
        /// <param name="ipString"> String representation of the IPv4 address. </param>
        /// <returns> Whether or not the string is a valid IPv4 address. </returns>
        public static bool IsIP( [NotNull] string ipString ) {
            if( ipString == null ) throw new ArgumentNullException( "ipString" );
            return RegexIP.IsMatch( ipString );
        }


        public static bool Match( [NotNull] this IPAddress thisAddr, uint otherAddr, uint mask ) {
            if( thisAddr == null ) throw new ArgumentNullException( "thisAddr" );
            uint thisAsUInt = thisAddr.AsUInt();
            return ( thisAsUInt & mask ) == ( otherAddr & mask );
        }


        [NotNull]
        public static IPAddress RangeMin( [NotNull] this IPAddress thisAddr, byte range ) {
            if( thisAddr == null ) throw new ArgumentNullException( "thisAddr" );
            if( range > 32 ) throw new ArgumentOutOfRangeException( "range" );
            int thisAsInt = thisAddr.AsInt();
            int mask = (int)NetMask( range );
            return new IPAddress( IPAddress.HostToNetworkOrder( thisAsInt & mask ) );
        }


        [NotNull]
        public static IPAddress RangeMax( [NotNull] this IPAddress thisAddr, byte range ) {
            if( thisAddr == null ) throw new ArgumentNullException( "thisAddr" );
            if( range > 32 ) throw new ArgumentOutOfRangeException( "range" );
            int thisAsInt = thisAddr.AsInt();
            int mask = (int)~NetMask( range );
            return new IPAddress( (uint)IPAddress.HostToNetworkOrder( thisAsInt | mask ) );
        }


        public static uint NetMask( byte range ) {
            if( range > 32 ) throw new ArgumentOutOfRangeException( "range" );
            if( range == 0 ) {
                return 0;
            } else {
                return 0xffffffff << ( 32 - range );
            }
        }
    }


    /// <summary> Provides methods JoinToString/JoinToClassyString methods
    /// for merging lists and enumerations into strings. </summary>
    public static class EnumerableUtil {
        /// <summary> Joins all items in a collection into one comma-separated string.
        /// If the items are not strings, .ToString() is called on them. </summary>
        [NotNull]
        public static string JoinToString<T>( [NotNull] this IEnumerable<T> items ) {
            if( items == null ) throw new ArgumentNullException( "items" );
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach( T item in items ) {
                if( !first ) sb.Append( ',' ).Append( ' ' );
                sb.Append( item );
                first = false;
            }
            return sb.ToString();
        }


        /// <summary> Joins all items in a collection into one string separated with the given separator.
        /// If the items are not strings, .ToString() is called on them. </summary>
        [NotNull]
        public static string JoinToString<T>( [NotNull] this IEnumerable<T> items, [NotNull] string separator ) {
            if( items == null ) throw new ArgumentNullException( "items" );
            if( separator == null ) throw new ArgumentNullException( "separator" );
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach( T item in items ) {
                if( !first ) sb.Append( separator );
                sb.Append( item );
                first = false;
            }
            return sb.ToString();
        }


        /// <summary> Joins all items in a collection into one string separated with the given separator.
        /// A specified string conversion function is called on each item before contactenation. </summary>
        [NotNull]
        public static string JoinToString<T>( [NotNull] this IEnumerable<T> items, [NotNull] Func<T, string> stringConversionFunction ) {
            if( items == null ) throw new ArgumentNullException( "items" );
            if( stringConversionFunction == null ) throw new ArgumentNullException( "stringConversionFunction" );
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach( T item in items ) {
                if( !first ) sb.Append( ',' ).Append( ' ' );
                sb.Append( stringConversionFunction( item ) );
                first = false;
            }
            return sb.ToString();
        }


        /// <summary> Joins all items in a collection into one string separated with the given separator.
        /// A specified string conversion function is called on each item before contactenation. </summary>
        [NotNull]
        public static string JoinToString<T>( [NotNull] this IEnumerable<T> items, [NotNull] string separator, [NotNull] Func<T, string> stringConversionFunction ) {
            if( items == null ) throw new ArgumentNullException( "items" );
            if( separator == null ) throw new ArgumentNullException( "separator" );
            if( stringConversionFunction == null ) throw new ArgumentNullException( "stringConversionFunction" );
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach( T item in items ) {
                if( !first ) sb.Append( separator );
                sb.Append( stringConversionFunction( item ) );
                first = false;
            }
            return sb.ToString();
        }


        /// <summary> Joins formatted names of all IClassy objects in a collection into one comma-separated string. </summary>
        [NotNull]

        public static string JoinToClassyString( [NotNull] this IEnumerable<IClassy> items ) {
            if( items == null ) throw new ArgumentNullException( "items" );
            return items.JoinToString( ", ", p => p.ClassyName );
        }

        public static string JoinToRealString([NotNull] this IEnumerable<IClassy> items)
        {
            string list = "";
            if (items == null) throw new ArgumentNullException("items");
            foreach (Player item in items) {
                list += ", " + item.Info.Rank.Color + item.Info.Name;
            }
            return list.Substring(2);
        }
        
        internal static bool ArrayAny<TSource>(this TSource[] source, Func<TSource, bool> predicate) {
            for (int i = 0; i < source.Length; i++) {
                if (predicate(source[i])) return true;
            }
            return false;
        }
    }
    
    
    /// <summary> Provides utility methods for working with strings. </summary>
    public static class StringUtil {
        
        /// <summary> Returns whether the string is null, or if all the 
        /// characters in the string are whitespace. </summary>
        public static bool NullOrWhiteSpace(this string value) {
            if (value == null) return true;
            foreach (char c in value) {
                if (!Char.IsWhiteSpace(c)) return false;
            }
            return true;
        }
        
        /// <summary> Returns whether 'a' caselessly equals 'b'. </summary>
        public static bool CaselessEquals(this string a, string b) {
            return a.Equals(b, StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary> Returns whether 'a' caselessly starts with 'b'. </summary>
        public static bool CaselessStarts(this string a, string b) {
            return a.StartsWith(b, StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary> Returns whether 'a' caselessly ends with 'b'. </summary>
        public static bool CaselessEnds(this string a, string b) {
            return a.EndsWith(b, StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary> Returns whether 'a' caselessly contains 'b'. </summary>
        public static bool CaselessContains(this string a, string b) {
            return a.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        
        public static string ReplaceString(this string str, string oldValue, string newValue, StringComparison comparison) {
            StringBuilder sb = new StringBuilder();

            int previousIndex = 0;
            int index = str.IndexOf(oldValue, comparison);
            while (index != -1) {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = str.IndexOf(oldValue, index, comparison);
            }
            sb.Append(str.Substring(previousIndex));

            return sb.ToString();
        }
    }


    // Helper methods for working with strings and for serialization/parsing
    static unsafe class FormatUtil {
        // Quicker StringBuilder.Append(int) by Sam Allen of http://www.dotnetperls.com
        [NotNull]
        public static StringBuilder Digits( [NotNull] this StringBuilder builder, int number ) {
            if( builder == null ) throw new ArgumentNullException( "builder" );
            if( number >= 100000000 || number < 0 ) {
                // Use system ToString.
                builder.Append( number );
                return builder;
            }
            int copy;
            int digit;
            if( number >= 10000000 ) {
                // 8.
                copy = number % 100000000;
                digit = copy / 10000000;
                builder.Append( (char)( digit + 48 ) );
            }
            if( number >= 1000000 ) {
                // 7.
                copy = number % 10000000;
                digit = copy / 1000000;
                builder.Append( (char)( digit + 48 ) );
            }
            if( number >= 100000 ) {
                // 6.
                copy = number % 1000000;
                digit = copy / 100000;
                builder.Append( (char)( digit + 48 ) );
            }
            if( number >= 10000 ) {
                // 5.
                copy = number % 100000;
                digit = copy / 10000;
                builder.Append( (char)( digit + 48 ) );
            }
            if( number >= 1000 ) {
                // 4.
                copy = number % 10000;
                digit = copy / 1000;
                builder.Append( (char)( digit + 48 ) );
            }
            if( number >= 100 ) {
                // 3.
                copy = number % 1000;
                digit = copy / 100;
                builder.Append( (char)( digit + 48 ) );
            }
            if( number >= 10 ) {
                // 2.
                copy = number % 100;
                digit = copy / 10;
                builder.Append( (char)( digit + 48 ) );
            }
            if( number >= 0 ) {
                // 1.
                copy = number % 10;
                builder.Append( (char)( copy + 48 ) );
            }
            return builder;
        }


        // UppercaseFirst by Sam Allen of http://www.dotnetperls.com
        [NotNull]
        public static string UppercaseFirst( this string s ) {
            if( string.IsNullOrEmpty( s ) ) {
                return string.Empty;
            }
            char[] a = s.ToCharArray();
            a[0] = char.ToUpper( a[0] );
            return new string( a );
        }


        [NotNull]
        public static string ToStringInvariant( this int i ) {
            return i.ToString( CultureInfo.InvariantCulture );
        }


        public static int IndexOfOrdinal( [NotNull] this string haystack, [NotNull] string needle ) {
            return haystack.IndexOf( needle, StringComparison.Ordinal );
        }
        
        public static char UnicodeToCp437(this char c) {
            int cpIndex = 0;
            if (c >= ' ' && c <= '~') {
                return c;
            } else if ((cpIndex = Chat.ControlCharReplacements.IndexOf(c)) >= 0 ) {
                return (char)cpIndex;
            } else if ((cpIndex = Chat.ExtendedCharReplacements.IndexOf(c)) >= 0 ) {
                return (char)(cpIndex + 127);
            }
            return '?';
        }
    }


    /// <summary> Provides utility methods for working with byte arrays and pointers. </summary>
    public static unsafe class BufferUtil {
        /// <summary> Fills the entire given byte array with a specified byte value, as efficiently as possible. </summary>
        /// <param name="array"> Array to work with. </param>
        /// <param name="value"> Value to assign to each byte of the array. </param>
        /// <exception cref="ArgumentNullException"> If array is null. </exception>
        public static void MemSet( [NotNull] this byte[] array, byte value ) {
            if( array == null ) throw new ArgumentNullException( "array" );
            byte[] rawValue = new[] { value, value, value, value, value, value, value, value };
            Int64 fillValue = BitConverter.ToInt64( rawValue, 0 );

            fixed( byte* ptr = array ) {
                Int64* dest = (Int64*)ptr;
                int length = array.Length;
                while( length >= 8 ) {
                    *dest = fillValue;
                    dest++;
                    length -= 8;
                }
                byte* bDest = (byte*)dest;
                for( byte i = 0; i < length; i++ ) {
                    *bDest = value;
                    bDest++;
                }
            }
        }


        /// <summary> Fills a section of the given byte array with a specified byte value, as efficiently as possible. </summary>
        /// <param name="array"> Array to work with. </param>
        /// <param name="value"> Value to assign to each byte of the array. </param>
        /// <param name="startIndex"> Index of the first byte that should be set. </param>
        /// <param name="length"> Number of bytes of the array to set. </param>
        /// <exception cref="ArgumentNullException"> If array is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If length is negative, if startIndex is negative,
        /// or if (length+startIndex) is greater than array length. </exception>
        public static void MemSet( [NotNull] this byte[] array, byte value, int startIndex, int length ) {
            if( array == null ) throw new ArgumentNullException( "array" );
            if( length < 0 || length > array.Length ) {
                throw new ArgumentOutOfRangeException( "length" );
            }
            if( startIndex < 0 || startIndex + length > array.Length ) {
                throw new ArgumentOutOfRangeException( "startIndex" );
            }

            byte[] rawValue = new[] { value, value, value, value, value, value, value, value };
            Int64 fillValue = BitConverter.ToInt64( rawValue, 0 );

            fixed( byte* ptr = &array[startIndex] ) {
                Int64* dest = (Int64*)ptr;
                while( length >= 8 ) {
                    *dest = fillValue;
                    dest++;
                    length -= 8;
                }
                byte* bDest = (byte*)dest;
                for( byte i = 0; i < length; i++ ) {
                    *bDest = value;
                    bDest++;
                }
            }
        }


        /// <summary> Copies contents of src buffer to dest buffer, as efficiently as possible. </summary>
        /// <param name="src"> Source array/pointer. </param>
        /// <param name="dest"> Destination array/pointer. </param>
        /// <param name="len"> Number of bytes to copy. </param>
        public static void MemCpy( [NotNull] byte* src, [NotNull] byte* dest, int len ) {
            if( src == null ) throw new ArgumentNullException( "src" );
            if( dest == null ) throw new ArgumentNullException( "dest" );
            if( len >= 0x10 ) {
                do {
                    *( (int*)dest ) = *( (int*)src );
                    *( (int*)( dest + 4 ) ) = *( (int*)( src + 4 ) );
                    *( (int*)( dest + 8 ) ) = *( (int*)( src + 8 ) );
                    *( (int*)( dest + 12 ) ) = *( (int*)( src + 12 ) );
                    dest += 0x10;
                    src += 0x10;
                } while( ( len -= 0x10 ) >= 0x10 );
            }
            if( len > 0 ) {
                if( ( len & 8 ) != 0 ) {
                    *( (int*)dest ) = *( (int*)src );
                    *( (int*)( dest + 4 ) ) = *( (int*)( src + 4 ) );
                    dest += 8;
                    src += 8;
                }
                if( ( len & 4 ) != 0 ) {
                    *( (int*)dest ) = *( (int*)src );
                    dest += 4;
                    src += 4;
                }
                if( ( len & 2 ) != 0 ) {
                    *( (short*)dest ) = *( (short*)src );
                    dest += 2;
                    src += 2;
                }
                if( ( len & 1 ) != 0 ) {
                    dest++;
                    src++;
                    dest[0] = src[0];
                }
            }
        }


        /// <summary> Checks whether the sequence of bytes in data at the given offset matches the sequence of ASCII characters in value.
        /// Basically, checks whether the byte array contains the string at this offset. </summary>
        /// <param name="data"> Byte array to search. </param>
        /// <param name="offset"> Offset, in bytes, from the start of data. </param>
        /// <param name="value"> Value to search for. Must contain only ASCII characters. </param>
        /// <returns> Whether the pattern was found. </returns>
        /// <exception cref="ArgumentNullException">If data or value is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"> If offset is less than 0 or greater than data.Length. </exception>
        [Pure]
        public static bool MemCmp( [NotNull] byte[] data, int offset, [NotNull] string value ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            if( value == null ) throw new ArgumentNullException( "value" );
            if( offset < 0 || offset > data.Length ) throw new ArgumentOutOfRangeException( "offset" );
            for( int i = 0; i < value.Length; i++ ) {
                if( offset + i >= data.Length || data[offset + i] != value[i] ) return false;
            }
            return true;
        }


        /// <summary> Reads a number of bytes from source that matches the length of destination byte array. </summary>
        /// <param name="source"> Stream to read from. </param>
        /// <param name="destination"> Byte array to write to. Length of this array is used. </param>
        /// <exception cref="ArgumentNullException"> If source or destination is null. </exception>
        /// <exception cref="EndOfStreamException"> If the end of stream was reached before destination array was filled. </exception>
        public static void ReadAll( [NotNull] Stream source, [NotNull] byte[] destination ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( destination == null ) throw new ArgumentNullException( "destination" );
            int bytesRead = 0;
            int bytesLeft = destination.Length;
            while( bytesLeft > 0 ) {
                int readPass = source.Read( destination, bytesRead, bytesLeft );
                if( readPass == 0 ) throw new EndOfStreamException();
                bytesRead += readPass;
                bytesLeft -= readPass;
            }
        }

        // Write at most 512 MiB at a time.
        const int MaxWriteChunk = 512 * 1024 * 1024;
        /// <summary> Writes contents of given byte array to a stream, in chunks of at most 512 MiB. 
        /// Works around an overflow in BufferedStream.Write(byte[]) that happens on 1 GiB+ writes. </summary>
        /// <param name="source"> Byte array to read from. Must not be null. </param>
        /// <param name="destination"> Stream to write to. Must not be null. </param>
        /// <exception cref="ArgumentNullException"> source or destination is null. </exception>
        public static void WriteAll([NotNull] byte[] source, [NotNull] Stream destination) {
            if (source == null)
                throw new ArgumentNullException("source");
            if (destination == null)
                throw new ArgumentNullException("destination");
            int written = 0;
            while (written < source.Length) {
                int toWrite = Math.Min(MaxWriteChunk, source.Length - written);
                destination.Write(source, written, toWrite);
                written += toWrite;
            }
        }
    }


    /// <summary> Provides TryParse method, for parsing enumerations. </summary>
    public static class EnumUtil {
        /// <summary> Tries to parse a given value as an enumeration.
        /// Even if value is numeric, this method still ensures that given number is among the enumerated constants.
        /// This differs in behavior from Enum.Parse, which accepts any valid numeric string (that fits into enumeration's base type). </summary>
        /// <typeparam name="TEnum"> Enumeration type. </typeparam>
        /// <param name="value"> Raw string value to parse. </param>
        /// <param name="output"> Parsed enumeration to output. Set to default(TEnum) on failure. </param>
        /// <param name="ignoreCase"> Whether parsing should be case-insensitive. </param>
        /// <returns> Whether parsing succeeded. </returns>
        public static bool TryParse<TEnum>( [NotNull] string value, out TEnum output, bool ignoreCase ) {
            if( value == null ) throw new ArgumentNullException( "value" );
            try {
                output = (TEnum)Enum.Parse( typeof( TEnum ), value, ignoreCase );
                return Enum.IsDefined( typeof( TEnum ), output );
            } catch( ArgumentException ) {
                output = default( TEnum );
                return false;
            }
        }

        /// <summary> Tries to parse a given value as an enumeration, with partial-name completion.
        /// If there is an exact name match, exact numeric match, or a single partial name match, completion succeeds.
        /// If there are no matches, multiple partial matches, or if value was an empty string, completion fails.
        /// Even if value is numeric, this method still ensures that given number is among the enumerated constants.
        /// This differs in behavior from Enum.Parse, which accepts any valid numeric string (that fits into enumeration's base type). </summary>
        /// <typeparam name="TEnum"> Enumeration type. </typeparam>
        /// <param name="value"> Raw string value to parse. </param>
        /// <param name="output"> Parsed enumeration to output. Set to default(TEnum) on failure. </param>
        /// <param name="ignoreCase"> Whether parsing should be case-insensitive. </param>
        /// <returns> Whether parsing/completion succeeded. </returns>
        /// <exception cref="ArgumentNullException"> value is null. </exception>
        public static bool TryComplete<TEnum>([NotNull] string value, out TEnum output, bool ignoreCase) {
            if (value == null)
                throw new ArgumentNullException("value");
            output = default(TEnum);

            // first, try to find an exact match
            try {
                output = (TEnum)Enum.Parse(typeof(TEnum), value, ignoreCase);
                if (Enum.IsDefined(typeof(TEnum), output)) {
                    return true;
                }
            } catch (ArgumentException) {
                // No exact match found. Proceed unless value was an empty string.
                if (value.Length == 0)
                    return false;
            } catch (OverflowException) {
                // Value was a numeric string, beyond enum's scope.
                return false;
            }

            // Try name completion
            bool matchFound = false;
            StringComparison comparison = (ignoreCase
                                               ? StringComparison.OrdinalIgnoreCase
                                               : StringComparison.Ordinal);
            foreach (string name in Enum.GetNames(typeof(TEnum))) {
                if (name.StartsWith(value, comparison)) {
                    if (matchFound) {
                        // Multiple matches found. Fail.
                        output = default(TEnum);
                        return false;
                    } else {
                        // First (and hopefully only) partial match found.
                        output = (TEnum)Enum.Parse(typeof(TEnum), name);
                        matchFound = true;
                    }
                }
            }

            // Either 0 or 1 match found.
            return matchFound;
        }
    }
}