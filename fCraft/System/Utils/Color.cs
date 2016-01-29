﻿// Part of fCraft | Copyright 2009-2015 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt //Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com> //Copyright (c) <2012-2014> <LeChosenOne, DingusBungus> | ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace fCraft
{

    /// <summary> Static class with definitions of Minecraft color codes,
    /// parsers, converters, and utilities. </summary>
    public static class Color
    {
#pragma warning disable 1591
        public const string Black = "&0",
                            Navy = "&1",
                            Green = "&2",
                            Teal = "&3",
                            Maroon = "&4",
                            Purple = "&5",
                            Olive = "&6",
                            Silver = "&7",
                            Gray = "&8",
                            Blue = "&9",
                            Lime = "&a",
                            Aqua = "&b",
                            Red = "&c",
                            Magenta = "&d",
                            Yellow = "&e",
                            White = "&f";
#pragma warning restore 1591

        // User-defined color assignments. Set by Config.ApplyConfig.
        /// <summary> Color of system messages, nickserv, chanserv. </summary>
        public static string Sys { get; set; }

        /// <summary> Color of help messages, /help. </summary>
        public static string Help { get; set; }

        /// <summary> Color of say messages (/say) and timer announcements. </summary>
        public static string Say { get; set; }

        /// <summary> Color of announcements, server announcements. </summary>
        public static string Announcement { get; set; }

        /// <summary> Color of personal messages. </summary>
        public static string PM { get; set; }

        /// <summary> Color of IRC chat. </summary>
        public static string IRC { get; set; }

        /// <summary> Color of /me command. </summary>
        public static string Me { get; set; }

        /// <summary> Color of warning messages. </summary>
        public static string Warning { get; set; }


        // Defaults for user-defined colors.
        /// <summary> Default color of system messages, nickserv, chanserv. Yellow. </summary>
        public const string SysDefault = Yellow;

        /// <summary> Default color of help messages, /help. Lime. </summary>
        public const string HelpDefault = Lime;

        /// <summary> Default color of say messages (/say) and timer announcements. Green. </summary>
        public const string SayDefault = Green;

        /// <summary> Default color of announcements, server announcements. Green. </summary>
        public const string AnnouncementDefault = Green;

        /// <summary> Default color of personal messages. Aqua. </summary>
        public const string PMDefault = Aqua;

        /// <summary> Default color of IRC chat. Purple. </summary>
        public const string IRCDefault = Purple;

        /// <summary> Default color of /me command. Purple. </summary>
        public const string MeDefault = Purple;

        /// <summary> Default color of warning messages. Red. </summary>
        public const string WarningDefault = Red;


        /// <summary> List of color names indexed by their id. </summary>
        public static readonly SortedList<char, string> ColorNames = new SortedList<char, string> {
            { '0', "black" },
            { '1', "navy" },
            { '2', "green" },
            { '3', "teal" },
            { '4', "maroon" },
            { '5', "purple" },
            { '6', "olive" },
            { '7', "silver" },
            { '8', "gray" },
            { '9', "blue" },
            { 'a', "lime" },
            { 'b', "aqua" },
            { 'c', "red" },
            { 'd', "magenta" },
            { 'e', "yellow" },
            { 'f', "white" }
        };


        /// <summary> Looks up color name for the given character color code. Codes are case-insensitive.
        /// Both standard (0-F) and fCraft-specific (H, I, M, P, R, S, W, and Y) color codes are accepted. 
        /// Assigned (standard) colors are substituted for fCraft-specific color codes. </summary>
        /// <param name="code"> Color code character. </param>
        /// <returns> Lowercase color name if input code was recognized; otherwise null. </returns>
        [CanBeNull, Pure]
        public static string GetName(char code)
        {
            code = Char.ToLower(code);
            if (IsStandardColorCode(code))
            {
                return ColorNames[code];
            }
            string color = Parse(code);
            if (color != null)
            {
                return ColorNames[color[1]];
            }
            return null;
        }


        /// <summary> Looks up color name for the given color string.
        /// Accepts any input format that is recognized by Color.Parse(String). </summary>
        /// <param name="color"> String representation of a color, empty string, or null. </param>
        /// <returns> Lowercase color name.
        /// If input is an empty string, returns an empty string.
        /// If input is null or cannot be parsed, returns null. </returns>
        [CanBeNull, Pure]
        public static string GetName([CanBeNull] string color)
        {
            if (color == null)
            {
                return null;
            }
            else if (color.Length == 0)
            {
                return "";
            }
            else
            {
                string parsedColor = Parse(color);
                if (parsedColor == null)
                {
                    return null;
                }
                else
                {
                    return GetName(parsedColor[1]);
                }
            }
        }


        /// <summary> Converts the given character color code into standard representation (ampersand-color-code).
        /// Codes are case-insensitive.
        /// Both standard (0-F) and fCraft-specific (H, I, M, P, R, S, W, and Y) color codes are accepted.
        /// Assigned (standard) colors are substituted for fCraft-specific color codes. </summary>
        /// <param name="code"> Color code character. </param>
        /// <returns> Standard Minecraft ampersand-color-code if input code was recognized; otherwise null. </returns>
        [CanBeNull, Pure]
        public static string Parse(char code)
        {
            code = Char.ToLower(code);
            if (IsStandardColorCode(code))
            {
                return "&" + code;
            }
            else
            {
                switch (code)
                {
                    case 's':
                        return Sys;
                    case 'y':
                        return Say;
                    case 'p':
                        return PM;
                    case 'r':
                        return Announcement;
                    case 'h':
                        return Help;
                    case 'w':
                        return Warning;
                    case 'm':
                        return Me;
                    case 'i':
						return IRC;
					case 't':
						return White;
                    default:
                        return null;
                }
            }
        }

        /// <summary> Converts the given character color code into standard representation (ampersand-color-code).
        /// Accepts 2-character ampersand color codes, single character codes, and color names.
        /// Does not accept 2-character percent-codes. All input is case-insensitive.
        /// Both standard (0-F) and fCraft-specific (H, I, M, P, R, S, W, and Y) color codes are accepted.
        /// Assigned (standard) colors are substituted for fCraft-specific color codes. </summary>
        /// <param name="color"> String representation of a color, empty string, or null. </param>
        /// <returns> If input could be parsed, returns a standard Minecraft ampersand-color-code.
        /// If input is an empty string, returns an empty string.
        /// If input is null or cannot be parsed, returns null. </returns>
        [CanBeNull, Pure]
        public static string Parse([CanBeNull] string color)
        {
            if (color == null)
                return null;
            switch (color.Length)
            {
                case 2:
                    if (color[0] == '&')
                        return Parse(color[1]);
                    break;

                case 1:
                    return Parse(color[0]);

                case 0:
                    return "";
            }
            color = color.ToLower();
            if (ColorNames.ContainsValue(color))
                return "&" + ColorNames.Keys[ColorNames.IndexOfValue(color)];
            else
                return null;
        }


        /// <summary> Checks whether a color code is valid (is a recognized standard color code).
        /// Standard color codes are hexadecimal digits. Both uppercase and lowercase digits are accepted.
        /// Does not recognize fCraft-specific color codes. </summary>
        /// <returns> True if given char is a recognized standard color code; otherwise false. </returns>
        [Pure]
        public static bool IsStandardColorCode(char code)
        {
            return (code >= '0' && code <= '9') || (code >= 'a' && code <= 'f') || (code >= 'A' && code <= 'F');
        }


        /// <summary> Checks whether a color code is valid. Both uppercase and lowercase digits are accepted.
        /// Both standard (0-F) and fCraft-specific (H, I, M, P, R, S, W, and Y) color codes are accepted. </summary>
        /// <returns> True if given char is a recognized color code; otherwise false. </returns>
        [Pure]
        public static bool IsColorCode(char code)
        {
            return (code >= '0' && code <= '9') || (code >= 'a' && code <= 'f') ||
                   (code >= 'A' && code <= 'F') || code == 'H' || code == 'h' ||
                   code == 'I' || code == 'i' || code == 'M' || code == 'm' ||
                   code == 'P' || code == 'p' || code == 'R' || code == 'r' ||
                   code == 'S' || code == 's' || code == 'W' || code == 'w' ||
				   code == 'Y' || code == 'y' || code == 'T' || code == 't';
        }


        /// <summary> Substitutes all fCraft-specific ampersand color codes (like &amp;S/Color.Sys)
        /// with the assigned Minecraft colors (like &amp;E/Color.Yellow).
        /// Strips any unrecognized sequences. Does not replace percent-codes.
        /// Note that LineWrapper itself does this substitution internally. </summary>
        /// <param name="sb"> StringBuilder, contents of which will be processed. </param>
        /// <returns> Processed string. </returns>
        /// <exception cref="ArgumentNullException"> sb is null. </exception>
        public static void SubstituteSpecialColors([NotNull] StringBuilder sb)
        {
            if (sb == null) throw new ArgumentNullException("sb");
            for (int i = sb.Length - 1; i > 0; i--)
            {
                if (sb[i - 1] == '&')
                {
                    switch (Char.ToLower(sb[i]))
                    {
                        case 's':
                            sb[i] = Sys[1];
                            break;
                        case 'y':
                            sb[i] = Say[1];
                            break;
                        case 'p':
                            sb[i] = PM[1];
                            break;
                        case 'r':
                            sb[i] = Announcement[1];
                            break;
                        case 'h':
                            sb[i] = Help[1];
                            break;
                        case 'w':
                            sb[i] = Warning[1];
                            break;
                        case 'm':
                            sb[i] = Me[1];
                            break;
                        case 'i':
                            sb[i] = IRC[1];
							break;
						case 't':
							sb[i] = White[1];
							break;
                        default:
                            if (!IsStandardColorCode(sb[i]))
                            {
                                sb.Remove(i - 1, 2);
                                i--;
                            }
                            break;
                    }
                }
            }
        }


        /// <summary> Substitutes all fCraft-specific ampersand color codes (like &amp;S/Color.Sys)
        /// with the assigned Minecraft colors (like &amp;E/Color.Yellow).
        /// Strips any unrecognized sequences. Does not replace percent-codes.
        /// Note that LineWrapper itself does this substitution internally. </summary>
        /// <param name="input"> String to process. </param>
        /// <returns> Processed string. </returns>
        /// <exception cref="ArgumentNullException"> input is null. </exception>
        [NotNull, Pure]
        public static string SubstituteSpecialColors([NotNull] string input)
        {
            if (input == null) throw new ArgumentNullException("input");
            StringBuilder sb = new StringBuilder(input);
            SubstituteSpecialColors(sb);
            return sb.ToString();
        }


        /// <summary> Strips Minecraft color codes from a given string.
        /// Removes all ampersand-character sequences, including standard, fCraft-specific color codes, and newline codes.
        /// Does not remove percent-color-codes. </summary>
        /// <param name="message"> String to process. </param>
        /// <returns> A processed string. </returns>
        /// <exception cref="ArgumentNullException"> message is null. </exception>
        [NotNull]
        public static string StripColors([NotNull] string message)
        {
            if (message == null) throw new ArgumentNullException("message");
            int start = message.IndexOf('&');
            if (start == -1)
            {
                return message;
            }
            int lastInsert = 0;
            StringBuilder output = new StringBuilder(message.Length);
            while (start != -1)
            {
                output.Append(message, lastInsert, start - lastInsert);
                lastInsert = Math.Min(start + 2, message.Length);
                start = message.IndexOf('&', lastInsert);
            }
            output.Append(message, lastInsert, message.Length - lastInsert);
            return output.ToString();
        }


        #region IRC Colors

        /// <summary> String that resets formatting for following part of an IRC message. </summary>
        public const string IRCReset = "\u0003\u000f";

        /// <summary> String that toggles bold text on/off in IRC messages. </summary>
        public const string IRCBold = "\u0002";

        static readonly Dictionary<string, string> MinecraftToIRCColors = new Dictionary<string, string> {
            { White, "\u000300" },
            { Black, "\u000301" },
            { Navy, "\u000302" },
            { Green, "\u000303" },
            { Red, "\u000304" },
            { Maroon, "\u000305" },
            { Purple, "\u000306" },
            { Olive, "\u000307" },
            { Yellow, "\u000308" },
            { Lime, "\u000309" },
            { Teal, "\u000310" },
            { Aqua, "\u000311" },
            { Blue, "\u000312" },
            { Magenta, "\u000313" },
            { Gray, "\u000314" },
            { Silver, "\u000315" },
        };


        /// <summary> Replaces Minecraft color codes with equivalent IRC color codes, in the given StringBuilder.
        /// Opposite of IrcToMinecraftColors method. </summary>
        /// <param name="sb"> StringBuilder objects, the contents of which will be processed. </param>
        /// <exception cref="ArgumentNullException"> sb is null. </exception>
        public static void MinecraftToIrcColors([NotNull] StringBuilder sb)
        {
            if (sb == null) throw new ArgumentNullException("sb");
            SubstituteSpecialColors(sb);
            foreach (var codePair in MinecraftToIRCColors)
            {
                sb.Replace(codePair.Key, codePair.Value);
            }
        }


        /// <summary> Replaces Minecraft color codes with equivalent IRC color codes, in the given string.
        /// Opposite of IrcToMinecraftColors method. </summary>
        /// <param name="input"> String to process. </param>
        /// <returns> A processed string. </returns>
        /// <exception cref="ArgumentNullException"> input is null. </exception>
        [NotNull, Pure]
        public static string MinecraftToIrcColors([NotNull] string input)
        {
            if (input == null) throw new ArgumentNullException("input");
            StringBuilder sb = new StringBuilder(input);
            MinecraftToIrcColors(sb);
            return sb.ToString();
        }


        static readonly Regex IrcTwoColorCode = new Regex("(\x03\\d{1,2}),\\d{1,2}");

        /// <summary> Replaces IRC color codes with equivalent Minecraft color codes, in the given string.
        /// Opposite of MinecraftToIrcColors method. </summary>
        /// <param name="input"> String to process. </param>
        /// <returns> A processed string. </returns>
        /// <exception cref="ArgumentNullException"> input is null. </exception>
        [NotNull, Pure]
        public static string IrcToMinecraftColors([NotNull] string input)
        {
            if (input == null) throw new ArgumentNullException("input");
            input = IrcTwoColorCode.Replace(input, "$1");
            StringBuilder sb = new StringBuilder(input);
            foreach (var codePair in MinecraftToIRCColors)
            {
                sb.Replace(codePair.Value, codePair.Key);
            }
            sb.Replace("\u0003", White); // color reset
            sb.Replace("\u000f", White); // reset
            return sb.ToString();
        }

        #endregion
        
        
        #region Custom colors
        
        
        public static CustomColor[] ExtColors = new CustomColor[256];
        
        public static char GetFallback(char c) {
            return (int)c >= 256 ? '\0' : ExtColors[c].Fallback;
        }
        
        static string GetExtColor(string name) {
            for (int i = 0; i < ExtColors.Length; i++) {
                CustomColor col = ExtColors[i];
                if (col.Undefined) continue;
                if (col.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return "&" + col.Code;
            }
            return "";
        }
        
        public static void AddExtColor(CustomColor col) { SetExtCol(col); }
        
        public static void RemoveExtColor(char code) {
            CustomColor col = default(CustomColor);
            col.Code = code;
            SetExtCol(col);
        }
        
        static void SetExtCol(CustomColor col) {
            ExtColors[col.Code] = col;
            Player[] players = Server.Players;
            foreach (Player p in players) {
                if (!p.Supports(CpeExt.TextColors)) continue;
                p.Send(Packet.MakeSetTextColor(col));
            }
            SaveExtColors();
        }
        
        internal static void SaveExtColors() {
            using (StreamWriter w = new StreamWriter("customcolors.txt")) {
                foreach (CustomColor col in ExtColors) {
                    if (col.Undefined) continue;
                    w.WriteLine(col.Code + " " + col.Fallback + " " + col.Name + " " +
                            col.R + " " + col.G + " " + col.B + " " + col.A);              
                }
            }
        }
        
        internal static void LoadExtColors() {
            if (!File.Exists("customcolors.txt")) return;
            string[] lines = File.ReadAllLines("customcolors.txt");
            CustomColor col = default(CustomColor);
            
            for (int i = 0; i < lines.Length; i++) {
                string[] parts = lines[i].Split(' ');
                if (parts.Length != 7) continue;
                col.Code = parts[0][0]; col.Fallback = parts[1][0];
                col.Name = parts[2];
                
                if (!Byte.TryParse(parts[3], out col.R) || !Byte.TryParse(parts[4], out col.G) ||
                    !Byte.TryParse(parts[5], out col.B) || !Byte.TryParse(parts[6], out col.A))
                    continue;
                ExtColors[col.Code] = col;
            }
        }        
        #endregion
    }
    
     public struct CustomColor {
        public char Code, Fallback;
        public byte R, G, B, A;
        public string Name;
        
        public bool Undefined { get { return Fallback == '\0'; } }
    }
}