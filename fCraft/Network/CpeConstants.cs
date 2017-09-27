// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

namespace fCraft
{
    /// <summary> Client Protocol Extension list.
    /// See http://wiki.vg/Classic_Protocol_Extension for details. </summary>
    public enum CpeExt
    {
        None,
        
        /// <summary> Extend or restricts the distance at which the user may click blocks. </summary>
        ClickDistance,

        /// <summary> Indicates the client supports the predefined CPE custom blocks. </summary>
        CustomBlocks,

        /// <summary> Provides a way for the client to notify the server about the block type that it is
        /// currently holding, and for the server to change the currently-held block type. </summary>
        HeldBlock,

        /// <summary> Indicates that the client can render emotes in chat properly,
        /// without padding or suffixes that are required for vanilla client. </summary>
        EmoteFix,

        /// <summary> Allows the server to define "hotkeys" for certain commands. </summary>
        TextHotKey,

        /// <summary> Provides more flexibility in naming of players and loading of skins,
        /// autocompletion, and player tab-list display. Separates tracking of in-game
        /// entities (spawned player models) and names on the player list. </summary>
        ExtPlayerList,

        /// <summary> Provides more flexibility in naming of players and loading of skins,
        /// autocompletion, and player tab-list display. Separates tracking of in-game
        /// entities (spawned player models) and names on the player list. </summary>
        ExtPlayerList2,

        /// <summary> Sets environment colors used by the client when rendering. </summary>
        EnvColors,

        /// <summary> Allows highlighting parts of a world. Applications include zoning,
        /// previewing draw command size, previewing undo commands. </summary>
        SelectionCuboid,

        /// <summary> Allows disabling placing/deleting blocks client side. </summary>
        BlockPermissions,

        /// <summary> Allows changing appearance of player models in supporting clients. </summary>
        ChangeModel,

        /// <summary> Allows specifying custom terrain textures, and tweaking appearance 
        /// of map edges. </summary>
        EnvMapAppearance,
        
        /// <summary> Allows specifying custom terrain textures, and tweaking appearance 
        /// of map edges, clouds height, and max fog distance. </summary>
        EnvMapAppearance2,

        /// <summary> Allows changing weather in the world. </summary>
        EnvWeatherType,

        /// <summary> Allows changing the user's hacking abilities. </summary>
        HackControl,

        /// <summary> Allows the server to send different message types. </summary>
        MessageType,

        /// <summary> Tells the server extended information about when a player clicks. </summary>
        PlayerClick,

        /// <summary> This extension lets the player send longer chat messages </summary>
        LongerMessages,

        /// <summary> This extension lets the player send characters from Code Page 437 </summary>
        FullCP437,
        
        /// <summary> Lets users define their own custom blocks. </summary>
        BlockDefinitions,
        
        /// <summary> Lets users define their own custom blocks. </summary>
        BlockDefinitionsExt,
        
        /// <summary> Lets users define their own custom blocks. </summary>
        BlockDefinitionsExt2,
        
        /// <summary> Lets servers send multiple block updates much more efficiently. </summary>
        BulkBlockUpdate,
        
        /// <summary> Allows users to define their own custom text colors displayed in chat. </summary>
        TextColors,
        
        /// <summary> This extension allows the server to specify custom terrain textures,
        /// and various aspects of the map's environment. </summary>
        EnvMapAspect,
        
        /// <summary> Allows for players to be moved to and see other players, below -1024 or above 1024 or any axis. </summary>
        ExtPlayerPositions,
        
        /// <summary> Allows for setting entity properties, such as rotation on X/Z axis. </summary>
        EntityProperty,
        
        /// <summary> Allows both client and server to measure average ping. </summary>
        TwoWayPing,
        
        /// <summary> Allows hiding blocks from and reordering blocks in the inventory. </summary>
        InventoryOrder,
    }


    public enum EnvVariable : byte {
        SkyColor = 0,
        CloudColor = 1,
        FogColor = 2,
        Shadow = 3,
        Sunlight = 4
    }

    public enum WeatherType : byte {
        Sunny = 0,
        Raining = 1,
        Snowing = 2
    }

    public enum MessageType : byte {
        Chat = 0,
        Status1 = 1,
        Status2 = 2,
        Status3 = 3,
        BottomRight1 = 11,
        BottomRight2 = 12,
        BottomRight3 = 13,
        Announcement = 100
    }
    
    public enum EnvProp : byte {
        SidesBlock = 0,
        EdgeBlock = 1,
        EdgeLevel = 2,
        CloudsLevel = 3,
        MaxFog = 4,
        CloudsSpeed = 5,
        WeatherSpeed = 6,
        WeatherFade = 7,
        ExpFog = 8,
        SidesOffset = 9,
        SkyboxHorSpeed = 10,
        SkyboxVerSpeed = 11,
    }
    
    public enum EntityProp : byte {
        RotationX = 0,
        RotationY = 1,
        RotationZ = 2,
    }
}
