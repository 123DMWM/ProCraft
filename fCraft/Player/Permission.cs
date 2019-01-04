﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>

namespace fCraft {

    // See comment at the top of Config.cs for a history of changes.

    /// <summary> Enumeration of permission types/categories.
    /// Every rank definition contains a combination of these. </summary>
    public enum Permission {
        /// <summary> Ability to chat and to PM players.
        /// Note that players without this permission can still
        /// type in commands, receive PMs, and read chat. </summary>
        Chat,

        /// <summary> Ability to place blocks on maps.
        /// This is a baseline permission that can be overridden by
        /// world-specific and zone-specific permissions. </summary>
        Build,

        /// <summary> Ability to delete or replace blocks on maps.
        /// This is a baseline permission that can be overridden by
        /// world-specific and zone-specific permissions. </summary>
        Delete,

        /// <summary> Ability to place grass blocks. </summary>
        PlaceGrass,
        
        /// <summary> Ability to place water blocks. </summary>
        PlaceWater,

        /// <summary> Ability to place lava blocks. </summary>
        PlaceLava,

        /// <summary> Ability to build admincrete. </summary>
        PlaceAdmincrete,

        /// <summary> Ability to delete or replace admincrete. </summary>
        DeleteAdmincrete,

        /// <summary> Ability to view extended information about other players. </summary>
        ViewOthersInfo,

        /// <summary> Ability to see any players' IP addresses. </summary>
        ViewPlayerIPs,

        /// <summary> Ability to edit the player database directly.
        /// This also adds the ability to promote/demote/ban players by name,
        /// even if they have not visited the server yet. Also allows to
        /// manipulate players' records, and to promote/demote players in batches. </summary>
        EditPlayerDB,

        /// <summary> Ability to use /Say command. </summary>
        Say,

        /// <summary> Ability to use /Timer command. </summary>
        UseTimers,

        /// <summary> Ability to read /Staff chat. </summary>
        ReadStaffChat,

        /// <summary> Ability to use color codes in chat messages. </summary>
        UseColorCodes,

        /// <summary> Ability to use emotes in chat messages. </summary>
        UseEmotes,

        /// <summary> Ability to move at a faster-than-normal rate (using hacks). </summary>
        UseSpeedHack,

        /// <summary> Ability to kick players from the server. </summary>
        Kick,

        /// <summary> Ability to ban/unban individual players from the server. </summary>
        Ban,

        /// <summary> Ability to ban/unban IP addresses from the server. </summary>
        BanIP,

        /// <summary> Ability to ban/unban a player account, his IP, and all other
        /// accounts that used the IP. </summary>
        BanAll,

        /// <summary> Ability to promote players to a higher rank. </summary>
        Promote,

        /// <summary> Ability to demote players to a lower rank. </summary>
        Demote,

        /// <summary> Ability to appear hidden from other players. You can still chat,
        /// build/delete blocks, use all commands, and join worlds while hidden.
        /// Hidden players are completely invisible to other players. </summary>
        Hide,

        /// <summary> Ability to use drawing tools (commands capable of affecting
        /// many blocks at once). This permission can be overridden by world-specific
        /// and zone-specific building permissions. </summary>
        Draw,

        /// <summary> Ability to use advanced draw commands: sphere, torus, brushes. </summary>
        DrawAdvanced,

        /// <summary> Ability to copy (or cut) and paste blocks. The total number of
        /// blocks that can be copied or pasted at a time is affected by the draw limit. </summary>
        CopyAndPaste,

        /// <summary> Ability to undo actions of other players (UndoArea and UndoPlayer). </summary>
        UndoOthersActions,

        /// <summary> Ability to undo actions of everyone at once, regardless of UndoOthersActions limit. </summary>
        UndoAll,

        /// <summary> Ability to teleport to other players. </summary>
        Teleport,

        /// <summary> Ability to bring/summon other players to your location,
        /// or to another player. </summary>
        Bring,

        /// <summary> Ability to bring/summon many players at a time. </summary>
        BringAll,

        /// <summary> Ability to patrol lower-ranked players.
        /// "Patrolling" means teleporting to other players to check on them, usually while hidden. </summary>
        Patrol,

        /// <summary> Ability to use /Spectate. </summary>
        Spectate,

        /// <summary> Ability to freeze/unfreeze players.
        /// Frozen players cannot move or build/delete. </summary>
        Freeze,

        /// <summary> Ability to temporarily mute players.
        /// Muted players cannot write chat messages or send PMs,
        /// but they can still type in commands, receive PMs, and read chat. </summary>
        Mute,

        /// <summary> Ability to change the spawn point of a world or a player. </summary>
        SetSpawn,

        /// <summary> Ability to lock/unlock maps.
        /// "Locking" a map puts it into a protected read-only state. </summary>
        Lock,

        /// <summary> Ability to manipulate Signs: adding, and removing Signs. </summary>
        ManageSigns,

        /// <summary> Ability to manipulate zones: adding, editing,
        /// renaming, and removing zones. </summary>
        ManageZones,

        /// <summary> Ability to manipulate speical zones (signs/text/checkpoints etc)
        /// : adding, editing, renaming, and removing special zones. </summary>
        ManageSpecialZones,

        /// <summary> Ability to manipulate the world list:
        /// adding, renaming, and deleting worlds, loading/saving maps,
        /// changing per-world permissions, and using the map generator. </summary>
        ManageWorlds,

        /// <summary> Ability to flush pending draw commands with /WFlush. </summary>
        FlushWorlds,

        /// <summary> Ability to enable/disable, clear, and configure BlockDB. </summary>
        ManageBlockDB,

        /// <summary> Ability to import rank and ban lists from files.
        /// Useful if you are switching from another server software. </summary>
        Import,

        /// <summary> Ability to reload the configuration file without restarting. </summary>
        ReloadConfig,

        /// <summary> Ability to shut down or restart the server remotely.
        /// Useful for servers that run on dedicated machines. </summary>
        ShutdownServer,

        /// <summary> Ability to change own name capitalizations</summary>
        ChangeNameCaps,

        /// <summary> Ability to change own name color</summary>
        ChangeNameColor,

        /// <summary> Ability to use the bot function</summary>
        UseBot,
        
        /// <summary> Ability to define and modify global custom blocks. </summary>
        DefineCustomBlocks,
        
        /// <summary> Ability to define and modify level custom blocks. </summary>
        DefineLevelCustomBlocks,        

        /// <summary> Ability to create portals. </summary>
        CreatePortals,
    }
}
