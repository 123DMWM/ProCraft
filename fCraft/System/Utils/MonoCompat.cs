﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Class dedicated to solving Mono compatibility issues </summary>
    public static class MonoCompat {

        /// <summary> Whether the current filesystem is case-sensitive. </summary>
        public static bool IsCaseSensitive { get; private set; }

        /// <summary> Whether we are currently running under Mono. </summary>
        public static bool IsMono { get; private set; }

        /// <summary> Whether Mono's generational GC is available. </summary>
        public static bool IsSGenCapable { get; private set; }

        /// <summary> Full Mono version string. May be null if we are running a REALLY old version. </summary>
        public static string MonoVersionString { get; private set; }

        /// <summary> Mono version number. May be null if we are running a REALLY old version. </summary>
        public static Version MonoVersion { get; private set; }

        /// <summary> Whether we are under a Windows OS (under either .NET or Mono). </summary>
        public static bool IsWindows { get; private set; }


        const string UnsupportedMessage = "Your Mono version is not supported. Update to at least Mono 2.6+ (recommended 2.10+)";
        static readonly Regex VersionRegex = new Regex( @"^(\d)+\.(\d+)\.(\d)\D" );

        const BindingFlags MonoMethodFlags = BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding;
        static MonoCompat() {
            Type monoRuntimeType = typeof( object ).Assembly.GetType( "Mono.Runtime" );

            if( monoRuntimeType != null ) {
                IsMono = true;
                MethodInfo getDisplayNameMethod = monoRuntimeType.GetMethod( "GetDisplayName", MonoMethodFlags, null, Type.EmptyTypes, null );

                if( getDisplayNameMethod != null ) {
                    MonoVersionString = (string)getDisplayNameMethod.Invoke( null, null );

                    try {
                        Match versionMatch = VersionRegex.Match( MonoVersionString );
                        int major = Int32.Parse( versionMatch.Groups[1].Value );
                        int minor = Int32.Parse( versionMatch.Groups[2].Value );
                        int revision = Int32.Parse( versionMatch.Groups[3].Value );
                        MonoVersion = new Version( major, minor, revision );
                        IsSGenCapable = (major >= 2 && minor >= 8);
                    } catch( Exception ex ) {
                        throw new Exception( UnsupportedMessage, ex );
                    }

                    if( MonoVersion.Major < 2 && MonoVersion.Major < 6 ) {
                        throw new Exception( UnsupportedMessage );
                    }

                } else {
                    throw new Exception( UnsupportedMessage );
                }
            }

            switch( Environment.OSVersion.Platform ) {
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                    IsMono = true;
                    IsWindows = false;
                    break;
                default:
                    IsWindows = true;
                    break;
            }

            IsCaseSensitive = !IsWindows;

        }

        /// <summary> Starts a .NET process, using Mono if necessary. </summary>
        /// <param name="assemblyLocation"> .NET executable path. </param>
        /// <param name="assemblyArgs"> Arguments to pass to the executable. </param>
        /// <param name="detachIfMono"> If true, new process will be detached under Mono. </param>
        /// <returns>Process object</returns>
        public static Process StartDotNetProcess( [NotNull] string assemblyLocation, [NotNull] string assemblyArgs, bool detachIfMono ) {
            if( assemblyLocation == null ) throw new ArgumentNullException( "assemblyLocation" );
            if( assemblyArgs == null ) throw new ArgumentNullException( "assemblyArgs" );
            string binaryName, args;
            if( IsMono ) {
                if( IsSGenCapable ) {
                    binaryName = "mono-sgen";
                } else {
                    binaryName = "mono";
                }
                args = "\"" + assemblyLocation + "\"";
                if( !String.IsNullOrEmpty( assemblyArgs ) ) {
                    args += " " + assemblyArgs;
                }
                if( detachIfMono ) {
                    args += " &";
                }
            } else {
                binaryName = assemblyLocation;
                args = assemblyArgs;
            }
            return Process.Start( binaryName, args );
        }


        /// <summary> Prepends the correct Mono name to the .NET executable, if needed. </summary>
        public static string PrependMono( [NotNull] string dotNetExecutable ) {
            if( dotNetExecutable == null ) throw new ArgumentNullException( "dotNetExecutable" );
            if( IsMono ) {
                if( IsSGenCapable ) {
                    return "mono-sgen " + dotNetExecutable;
                } else {
                    return "mono " + dotNetExecutable;
                }
            } else {
                return dotNetExecutable;
            }
        }
    }
}