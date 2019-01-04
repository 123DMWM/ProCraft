﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2019 Joseph Beauvais <123DMWM@gmail.com>
using System;

namespace fCraft {
    /// <summary> Exception that is thrown when an invalid operation is attempted on a world. </summary>
    public sealed class WorldOpException : Exception {
        /// <summary> Error code associated with the error. </summary>
        public WorldOpExceptionCode ErrorCode { get; private set; }

        /// <summary> Creates a new instance of fCraft.WorldOpException, with the specified world and error code. </summary>
        /// <param name="worldName"> World where exception took place. May be null if no relevant world exists. </param>
        /// <param name="errorCode"> Error that took place. </param>
        public WorldOpException( string worldName, WorldOpExceptionCode errorCode )
            : base( GetMessage( worldName, errorCode ) ) {
            ErrorCode = errorCode;
        }

        public WorldOpException( WorldOpExceptionCode errorCode, string message )
            : base( message ) {
            ErrorCode = errorCode;
        }

        public WorldOpException( string worldName, WorldOpExceptionCode errorCode, Exception innerException )
            : base( GetMessage( worldName, errorCode ), innerException ) {
            ErrorCode = errorCode;
        }

        public WorldOpException( WorldOpExceptionCode errorCode, string message, Exception innerException )
            : base( message, innerException ) {
            ErrorCode = errorCode;
        }

        public static string GetMessage( string worldName, WorldOpExceptionCode code ) {
            if( worldName != null ) {
                switch( code ) {
                    case WorldOpExceptionCode.CannotDoThatToMainWorld:
                        return "This operation cannot be done on the main world (" +
                               worldName + "). Assign a new main world and try again.";

                    case WorldOpExceptionCode.DuplicateWorldName:
                        return "A world with this name (\"" + worldName + "\") already exists.";

                    case WorldOpExceptionCode.InvalidWorldName:
                        return "World name \"" + worldName + "\" is invalid. " +
                               "Expected an alphanumeric name between 1 and 16 characters long.";

                    case WorldOpExceptionCode.MapLoadError:
                        return "Failed to load the map file for world \"" + worldName + "\".";

                    case WorldOpExceptionCode.MapMoveError:
                        return "Failed to rename/move the map file for world \"" + worldName + "\".";

                    case WorldOpExceptionCode.MapNotFound:
                        return "Could not find the map file for world \"" + worldName + "\".";

                    case WorldOpExceptionCode.MapPathError:
                        return "Map file path is not valid for world \"" + worldName + "\".";

                    case WorldOpExceptionCode.MapSaveError:
                        return "Failed to save the map file for world \"" + worldName + "\".";

                    case WorldOpExceptionCode.NoChangeNeeded:
                        return "No change needed for world \"" + worldName + "\".";

                    case WorldOpExceptionCode.Cancelled:
                        return "Operation for world \"" + worldName + "\" was cancelled by a plugin.";

                    case WorldOpExceptionCode.SecurityError:
                        return "You are not allowed to do this operation to world \"" + worldName + "\".";

                    case WorldOpExceptionCode.Unexpected:
                        return "Unexpected problem occurred with world \"" + worldName + "\".";

                    case WorldOpExceptionCode.WorldNotFound:
                        return "No world found with the name \"" + worldName + "\".";

                    default:
                        return "Unexpected error occurred while working on world \"" + worldName + "\"";
                }
            } else {
                switch( code ) {
                    case WorldOpExceptionCode.CannotDoThatToMainWorld:
                        return "This operation cannot be done on the main world. " +
                               "Assign a new main world and try again.";

                    case WorldOpExceptionCode.DuplicateWorldName:
                        return "A world with this name already exists.";

                    case WorldOpExceptionCode.InvalidWorldName:
                        return "Given world name is invalid. " +
                               "Expected an alphanumeric name between 1 and 16 characters long.";

                    case WorldOpExceptionCode.MapLoadError:
                        return "Failed to load the map file.";

                    case WorldOpExceptionCode.MapMoveError:
                        return "Failed to rename/move the map file.";

                    case WorldOpExceptionCode.MapNotFound:
                        return "Could not find the map file.";

                    case WorldOpExceptionCode.MapPathError:
                        return "Map file path is not valid.";

                    case WorldOpExceptionCode.MapSaveError:
                        return "Failed to save the map file.";

                    case WorldOpExceptionCode.NoChangeNeeded:
                        return "No change needed.";

                    case WorldOpExceptionCode.Cancelled:
                        return "Operation cancelled by a plugin.";

                    case WorldOpExceptionCode.SecurityError:
                        return "You are not allowed to do this operation.";

                    case WorldOpExceptionCode.WorldNotFound:
                        return "Specified world was not found.";

                    default:
                        return "Unexpected error occurred.";
                }
            }
        }
    }


    /// <summary> List of common world operation issues. Used by WorldOpException. </summary>
    public enum WorldOpExceptionCode {
        /// <summary> Something new, exciting and dangerous happened! We don't know what it was though. </summary>
        Unexpected,

        /// <summary> No changes were needed or made (e.g. renaming a world to the same name). </summary>
        NoChangeNeeded,

        /// <summary> Given world name was of invalid format. </summary>
        InvalidWorldName,

        /// <summary> Given world could not be found by name. </summary>
        WorldNotFound,

        /// <summary> A world could not be added or renamed because the name is taken by another world. </summary>
        DuplicateWorldName,

        /// <summary> A permission issue prohibited the operation. </summary>
        SecurityError,

        /// <summary> Operation may not be done on the world designated as main. </summary>
        CannotDoThatToMainWorld,

        /// <summary> Specified map file could not be found. </summary>
        MapNotFound,

        /// <summary> Given map path was invalid or inaccessible. </summary>
        MapPathError,

        /// <summary> Map file was found but could not be loaded. </summary>
        MapLoadError,

        /// <summary> Map file could not be saved. </summary>
        MapSaveError,

        /// <summary> Map file could not be renamed, replaced, or moved. </summary>
        MapMoveError,

        /// <summary> A plugin callback cancelled the operation. </summary>
        Cancelled
    }
}