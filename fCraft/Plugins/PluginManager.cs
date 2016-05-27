/*        ----
        Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com>
        All rights reserved.

        Redistribution and use in source and binary forms, with or without
        modification, are permitted provided that the following conditions are met:
         * Redistributions of source code must retain the above copyright
              notice, this list of conditions and the following disclaimer.
            * Redistributions in binary form must reproduce the above copyright
             notice, this list of conditions and the following disclaimer in the
             documentation and/or other materials provided with the distribution.
            * Neither the name of 800Craft or the names of its
             contributors may be used to endorse or promote products derived from this
             software without specific prior written permission.

        THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
        ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
        WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
        DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
        DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
        (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
        LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
        ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
        (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
        SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
        ----*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace fCraft {

    internal static class PluginManager {
        public static List<Plugin> Plugins = new List<Plugin>();

        public static void Init() {
            try {
                if ( !Directory.Exists( "plugins" ) ) {
                    Directory.CreateDirectory( "plugins" );
                }
            } catch ( Exception ex ) {
                Logger.Log( LogType.Error, "PluginManager.Initialize: " + ex );
                return;
            }

            // Load plugins
            string[] plugins = Directory.GetFiles( "plugins", "*.dll" );
            if ( plugins.Length == 0 ) {
                Logger.Log( LogType.ConsoleOutput, "PluginManager: No plugins found" );
                return;
            } else {
                Logger.Log( LogType.ConsoleOutput, "PluginManager: Loading " + plugins.Length + " plugins" );

                foreach ( string plugin in plugins ) {
                    try {
                        Type pluginType = null;
                        string args = Path.GetFileNameWithoutExtension( plugin );
                        Assembly assembly = Assembly.LoadFile( Path.GetFullPath( plugin ) );

                        if ( assembly != null ) {
                            pluginType = assembly.GetType( args + ".Init" );

                            if ( pluginType != null ) {
                                Plugins.Add( ( Plugin )Activator.CreateInstance( pluginType ) );
                            }
                        }
                    } catch ( Exception ex ) {
                        Logger.Log( LogType.Error, "PluginManager: Unable to load plugin at location " + plugin + ": " + ex );
                    }
                }
            }
            LoadPlugins();
        }

        static void LoadPlugins() {
            if ( Plugins.Count > 0 ) {
				foreach (Plugin plugin in Plugins) {
					try {
						plugin.Initialize();
						Logger.Log(LogType.ConsoleOutput, "PluginManager: Loading plugin " + plugin.Name);
					} catch (Exception ex) {
						Logger.Log(LogType.Error, "PluginManager: Failed loading plugin " + plugin.Name);
						Logger.Log(LogType.Debug, ex.ToString());
					}
				}
            }
        }
    }
}