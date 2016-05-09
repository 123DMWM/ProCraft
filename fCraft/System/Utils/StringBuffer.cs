// ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;

namespace fCraft {
    
    public class StringBuffer {
        
        public char[] value;
        public int chunkSize, length;
        readonly char[] numBuffer = new char[20];
        
        public StringBuffer( int chunkSize ) {
            this.chunkSize = chunkSize;
            value = new char[chunkSize];
        }
        
        public StringBuffer Append( string s ) {
            if (s == null) return this;
            CheckAdd( s.Length );
            for( int i = 0; i < s.Length; i++ )
                value[length++] = s[i];
            return this;
        }
        
        public StringBuffer Append( char c ) {
            CheckAdd( 1 );
            value[length++] = c;
            return this;
        }
        
        public StringBuffer Append( char c, int count ) {
            CheckAdd( count );
            for( int i = 0; i < count; i++ )
                value[length++] = c;
            return this;
        }
        
        public StringBuffer Append( bool value ) {
            return Append( value ? "True" : "False" );
        }
        
        public StringBuffer Append( object value ) {
            if (value == null) return this;
            return Append( value.ToString() );
        }
        
        public StringBuffer Append( long num ) {
            int numLen = MakeNum( num );
            CheckAdd( numLen );
            
            for( int i = numLen - 1; i >= 0; i-- )
                value[length++] = numBuffer[i];
            return this;
        }
        
        int MakeNum( long num ) {
            int len = 0;
            numBuffer[len++] = (char)('0' + (num % 10)); num /= 10;
            
            while( num > 0 ) {
                numBuffer[len++] = (char)('0' + (num % 10)); num /= 10;
            }
            return len;
        }
        
        public StringBuffer Append( int num ) {
            int numLen = MakeNum( num );
            CheckAdd( numLen );
            
            for( int i = numLen - 1; i >= 0; i-- )
                value[length++] = numBuffer[i];
            return this;
        }
        
        int MakeNum( int num ) {
            int len = 0;
            numBuffer[len++] = (char)('0' + (num % 10)); num /= 10;
            
            while( num > 0 ) {
                numBuffer[len++] = (char)('0' + (num % 10)); num /= 10;
            }
            return len;
        }
        
        public StringBuffer AppendEscaped( string str ) {
            if( String.IsNullOrEmpty( str ) ) return this;
            
            for( int i = 0; i < str.Length; i++ ) {
                char c = str[i];
                Append( c == ',' ? '\xFF' : c );
            }
            return this;
        }
        
        public StringBuffer AppendTicks( TimeSpan time ) {
            if( time != TimeSpan.Zero )
                Append( time.Ticks / TimeSpan.TicksPerSecond );
            return this;
        }
        
        public StringBuffer AppendUnixTime( DateTime date ) {
            if( date != DateTime.MinValue )
                Append( date.ToUnixTime() );
            return this;
        }
        
        void CheckAdd(int len) {
            if (length + len > value.Length)
                Array.Resize(ref value, value.Length + chunkSize);
        }

        public override string ToString() {
            return new String( value, 0, length );
        }
    }
}