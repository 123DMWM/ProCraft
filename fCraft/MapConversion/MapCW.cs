// ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using JetBrains.Annotations;
using ServiceStack.Text;

namespace fCraft.MapConversion {
    /// <summary> ClassicWorld map conversion implementation, for converting ClassicWorld map format into fCraft's default map format. </summary>
    public sealed class MapCW : IMapImporter {

        public string ServerName {
            get { return "ClassicalSharp/ClassiCube client"; }
        }

        public bool SupportsImport {
            get { return true; }
        }

        public bool SupportsExport {
            get { return false; }
        }

        public string FileExtension {
            get { return "cw"; }
        }

        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }


        public MapFormat Format {
            get { return MapFormat.ClassicWorld; }
        }


        public bool ClaimsName( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return fileName.CaselessEnds( ".cw" );
        }


        public bool Claims( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            try {
                using( FileStream mapStream = File.OpenRead( fileName ) ) {
                    GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true );
                    BinaryReader bs = new BinaryReader( gs );
                    return ( bs.ReadByte() == 10 && NBTag.ReadString( bs ) == "ClassicWorld" );
                }
            } catch( Exception ) {
                return false;
            }
        }


        public Map LoadHeader( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            Map map = Load( fileName );
            map.Blocks = null;
            return map;
        }


        public Map Load( string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true );
                NBTag root = NBTag.ReadStream( gs );

                // ReSharper disable UseObjectOrCollectionInitializer
                Map map = new Map( null,
                                  root["X"].GetShort(),
                                  root["Z"].GetShort(),
                                  root["Y"].GetShort(),
                                  false );
                // ReSharper restore UseObjectOrCollectionInitializer
                
                NBTag spawnTag = root["Spawn"];
                map.Spawn = new Position {
                    X = (spawnTag["X"].GetShort() * 32),
                    Y = (spawnTag["Z"].GetShort() * 32),
                    //I think a negative Player.CharacterHeight is being used somewhere and is breaking spawn height
                    Z = (spawnTag["Y"].GetShort() * 32)+Player.CharacterHeight,
                    R = spawnTag["H"].GetByte(),
                    L = spawnTag["P"].GetByte(),
                };
                
                // read UUID
                map.Guid = new Guid( root["UUID"].GetBytes() );
                
                // read creation/modification dates of the file (for fallback)
                DateTime fileCreationDate = File.GetCreationTime( fileName );
                DateTime fileModTime = File.GetCreationTime( fileName );

                // try to read embedded creation date
                if( root.Contains( "TimeCreated" ) ) {
                    map.DateCreated = DateTimeUtil.ToDateTime( root["TimeCreated"].GetLong() );
                } else {
                    // for fallback, pick the older of two filesystem dates
                    map.DateCreated = (fileModTime > fileCreationDate) ? fileCreationDate : fileModTime;
                }

                // try to read embedded modification date
                if( root.Contains( "LastModified" ) ) {
                    map.DateModified = DateTimeUtil.ToDateTime( root["LastModified"].GetLong() );
                } else {
                    // for fallback, use file modification date
                    map.DateModified = fileModTime;
                }

                map.Blocks = root["BlockArray"].GetBytes();
                if (root.Contains("Metadata")) ReadMetadata(root["Metadata"], map, fileName);
                return map;
            }
        }
        void ReadMetadata(NBTag root, Map map, String fileName) {
            if (!root.Contains("CPE"))
                return;
            NBTag cpe = root["CPE"];

            if (cpe.Contains("EnvWeatherType"))
                map.Metadata.Add("CPE", "Weather", cpe["EnvWeatherType"]["WeatherType"].GetByte().ToString());
            if (cpe.Contains("ClickDistance"))
                map.Metadata.Add("CPE", "ClickDistance", cpe["ClickDistance"]["Distance"].GetShort().ToString());
            if (cpe.Contains("EnvMapAppearance"))
                ParseEnvMapAppearance(cpe, map);
            if (cpe.Contains("EnvColors"))
                ParseEnvColors(cpe, map);
            if (cpe.Contains("BlockDefinitions"))
                ParseBlockDefinitions(cpe, map, fileName);
        }


        static void ParseEnvMapAppearance(NBTag cpe, Map map) {
            map.Metadata.Add("CPE", "HasEnvMapAppearance", "true");
            NBTag comp = cpe["EnvMapAppearance"];
            map.Metadata.Add("CPE", "HorizonBlock", comp["EdgeBlock"].GetByte().ToString());
            map.Metadata.Add("CPE", "EdgeBlock", comp["SideBlock"].GetByte().ToString());
            map.Metadata.Add("CPE", "EdgeLevel", comp["SideLevel"].GetShort().ToString());
            if (!comp.Contains("TextureURL"))
                return;

            string url = comp["TextureURL"].GetString();
            map.Metadata.Add("CPE", "Texture", url == Server.DefaultTerrain ? "default" : url);
        }
        static void ParseEnvColors(NBTag cpe, Map map) {
            map.Metadata.Add("CPE", "HasEnvColors", "true");
            NBTag comp = cpe["EnvColors"];
            map.Metadata.Add("CPE", "SkyColor", GetColor(comp, "Sky"));
            map.Metadata.Add("CPE", "CloudColor", GetColor(comp, "Cloud"));
            map.Metadata.Add("CPE", "FogColor", GetColor(comp, "Fog"));
            map.Metadata.Add("CPE", "LightColor", GetColor(comp, "Sunlight"));
            map.Metadata.Add("CPE", "ShadowColor", GetColor(comp, "Ambient"));
        }

        static string GetColor(NBTag comp, string type) {
            if (!comp.Contains(type))
                return "";
            NBTag rgb = comp[type];
            short r = rgb["R"].GetShort(), g = rgb["G"].GetShort(), b = rgb["B"].GetShort();

            if (r < 0 || r > 255 || g < 0 || g > 255 || b < 0 || b > 255)
                return "";
            return r.ToString("X2") + g.ToString("X2") + b.ToString("X2");
        }

        public static BlockDefinition[] blockDefs = new BlockDefinition[256]; 

        static void ParseBlockDefinitions(NBTag cpe, Map map, String fileName) {
            NBTag blocks = cpe["BlockDefinitions"];
            bool hasBlockDefs = false;
            blockDefs = new BlockDefinition[256];

            foreach (NBTag tag in blocks) {
                if (tag.Type != NBTType.Compound) continue;

                NBTag props = tag;
                BlockDefinition def = new BlockDefinition();
                def.BlockID = props["ID"].GetByte();
                // can't change "ID" to short since backwards compatibility
                if (props.Contains("ID2")) {
                    ushort tempID = (ushort)props["ID2"].GetShort();
                    if (tempID >= 256) continue;
                    def.BlockID = (byte)tempID;
                }

                def.Name = props["Name"].GetString();
                def.CollideType = props["CollideType"].GetByte();
                def.Speed = props["Speed"].GetFloat();

                def.BlocksLight = props["TransmitsLight"].GetByte() == 0;
                def.WalkSound = props["WalkSound"].GetByte();
                def.FullBright = props["FullBright"].GetByte() != 0;
                def.Shape = props["Shape"].GetByte();
                def.BlockDraw = props["BlockDraw"].GetByte();

                byte[] fog = props["Fog"].GetBytes();
                def.FogDensity = fog[0];
                // Fix for older ClassicalSharp versions which saved wrong value for density = 0
                if (def.FogDensity == 0xFF)
                    def.FogDensity = 0;
                def.FogR = fog[1];
                def.FogG = fog[2];
                def.FogB = fog[3];

                byte[] tex = props["Textures"].GetBytes();
                def.TopTex = tex[0];
                def.BottomTex = tex[1];
                def.LeftTex = tex[2];
                def.RightTex = tex[3];
                def.FrontTex = tex[4];
                def.BackTex = tex[5];

                byte[] coords = props["Coords"].GetBytes();
                def.MinX = coords[0];
                def.MinZ = coords[1];
                def.MinY = coords[2];
                def.MaxX = coords[3];
                def.MaxZ = coords[4];
                def.MaxY = coords[5];

                // Don't define level custom block if same as global custom block
                if (PropsEquals(def, BlockDefinition.GlobalDefs[def.BlockID]))
                    continue;

                blockDefs[def.BlockID] = def;
                hasBlockDefs = true;
            }

            if (hasBlockDefs) {
                BlockDefinition[] realDefs = new BlockDefinition[256];
                int count = 0;
                for (int i = 0;i < 256;i++) {
                    if (blockDefs[i] == BlockDefinition.GlobalDefs[i]) realDefs[i] = null;
                    else {
                        count++;
                        realDefs[i] = blockDefs[i];
                    }
                }
                blockDefs = realDefs;

                string path = Paths.BlockDefsDirectory;
                path = Path.Combine(path, Path.GetFileName(fileName) + ".txt");
                map.Metadata["CPE", "HasBlockDefFile"] = "true";
                map.Metadata["CPE", "BlockDefFileName"] = Path.GetFileName(fileName);
                try {
                    using (Stream s = File.Create(path))
                        JsonSerializer.SerializeToStream(blockDefs, s);
                }
                catch (Exception ex) {
                    Logger.Log(LogType.Error, "BlockDefinitions.Save: " + ex);
                }
            }
        }
        static bool PropsEquals(BlockDefinition a, BlockDefinition b) {
            if (b == null || b.Name == null)
                return false;
            return a.Name == b.Name && a.CollideType == b.CollideType && a.Speed == b.Speed && a.TopTex == b.TopTex
                && a.BottomTex == b.BottomTex && a.BlocksLight == b.BlocksLight && a.WalkSound == b.WalkSound
                && a.FullBright == b.FullBright && a.Shape == b.Shape && a.BlockDraw == b.BlockDraw
                && a.FogDensity == b.FogDensity && a.FogR == b.FogR && a.FogG == b.FogG && a.FogB == b.FogB
                && a.MinX == b.MinX && a.MinY == b.MinY && a.MinZ == b.MinZ && a.MaxX == b.MaxX
                && a.MaxY == b.MaxY && a.MaxZ == b.MaxZ && a.LeftTex == b.LeftTex && a.RightTex == b.RightTex
                && a.FrontTex == b.FrontTex && a.BackTex == b.BackTex;
        }
    }
}