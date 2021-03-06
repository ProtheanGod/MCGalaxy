﻿/*
    Copyright 2015 MCGalaxy team

    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using MCGalaxy.Blocks;
using MCGalaxy.Maths;

namespace MCGalaxy.Commands {
    
    /// <summary> Provides helper methods for parsing arguments for commands. </summary>
    public static class CommandParser {
        
        /// <summary> Attempts to parse the given argument as a boolean. </summary>
        public static bool GetBool(Player p, string input, ref bool result) {
            if (input.CaselessEq("1") || input.CaselessEq("true")
                || input.CaselessEq("yes") || input.CaselessEq("on")) {
                result = true; return true;
            }
            
            if (input.CaselessEq("0") || input.CaselessEq("false")
                || input.CaselessEq("no") || input.CaselessEq("off")) {
                result = false; return true;
            }
            
            Player.Message(p, "\"{0}\" is not a valid boolean.", input);
            Player.Message(p, "Value must be either 1/yes/on or 0/no/off");
            return false;
        }
        
        /// <summary> Attempts to parse the given argument as an enumeration member. </summary>
        public static bool GetEnum<TEnum>(Player p, string input, string argName,
                                          ref TEnum result) where TEnum : struct {
            try {
                result = (TEnum)Enum.Parse(typeof(TEnum), input, true);
                if (Enum.IsDefined(typeof(TEnum), result)) return true;
            } catch {
            }
            
            string[] names = Enum.GetNames(typeof(TEnum));
            Player.Message(p, argName + " must be one of the following: " + names.Join());
            return false;
        }
        
        /// <summary> Attempts to parse the given argument as an timespan in short form. </summary>
        public static bool GetTimespan(Player p, string input, ref TimeSpan span,
                                       string action, string defUnit) {
            span = TimeSpan.Zero;
            try {
                span = input.ParseShort(defUnit);
                return true;
            } catch (OverflowException) {
                Player.Message(p, "Timespan given is too big.");
            } catch (FormatException ex) {
                Player.Message(p, "{0} is not a valid quantifier.", ex.Message);
                Player.Message(p, TimespanHelp, action);
            }
            return false;
        }
        public const string TimespanHelp = "For example, to {0} 25 and a half hours, use \"1d1h30m\".";
        
        
        /// <summary> Attempts to parse the given argument as an integer. </summary>
        public static bool GetInt(Player p, string input, string argName, ref int result,
                                  int min = int.MinValue, int max = int.MaxValue) {
            int value;
            if (!int.TryParse(input, out value)) {
                Player.Message(p, "\"{0}\" is not a valid integer.", input); return false;
            }
            
            if (value < min || value > max) {
                // Try to provide more helpful range messages
                if (max == int.MaxValue) {
                    Player.Message(p, "{0} must be {1} or greater", argName, min);
                } else if (min == int.MinValue) {
                    Player.Message(p, "{0} must be {1} or less", argName, max);
                } else {
                    Player.Message(p, "{0} must be between {1} and {2}", argName, min, max);
                }
                return false;
            }
            
            result = value; return true;
        }
        
        /// <summary> Attempts to parse the given argument as a real number. </summary>
        public static bool GetReal(Player p, string input, string argName, ref float result,
                                   float min, float max) {
            float value;
            if (!Utils.TryParseDecimal(input, out value)) {
                Player.Message(p, "\"{0}\" is not a valid number.", input); return false;
            }
            
            if (value < min || value > max) {
            	Player.Message(p, "{0} must be between {1} and {2}", argName, 
            	               min.ToString("F4"), max.ToString("F4"));
                return false;
            }
            result = value; return true;
        }
        
        
        /// <summary> Attempts to parse the given argument as an byte. </summary>
        public static bool GetByte(Player p, string input, string argName, ref byte result,
                                   byte min = byte.MinValue, byte max = byte.MaxValue) {
            int temp = 0;
            if (!GetInt(p, input, argName, ref temp, min, max)) return false;
            
            result = (byte)temp; return true;
        }
        
        /// <summary> Attempts to parse the given argument as an byte. </summary>
        public static bool GetUShort(Player p, string input, string argName, ref ushort result,
                                     ushort min = ushort.MinValue, ushort max = ushort.MaxValue) {
            int temp = 0;
            if (!GetInt(p, input, argName, ref temp, min, max)) return false;
            
            result = (ushort)temp; return true;
        }
        
        
        /// <summary> Attempts to parse the given argument as a hex color. </summary>
        public static bool GetHex(Player p, string input, ref ColorDesc col) {
            if (input.Length > 0 && input[0] == '#')
                input = input.Substring(1);
            
            if (!Utils.IsValidHex(input)) {
                Player.Message(p, "\"#{0}\" is not a valid HEX color.", input); return false;
            }
            
            col = Colors.ParseHex(input); return true;
        }
        
        internal static bool GetCoords(Player p, string[] args, int argsOffset, ref Vec3S32 P) {
            return
                GetCoord(p, args[argsOffset + 0], P.X, "X coordinate", out P.X) &&
                GetCoord(p, args[argsOffset + 1], P.Y, "Y coordinate", out P.Y) &&
                GetCoord(p, args[argsOffset + 2], P.Z, "Z coordinate", out P.Z);
        }
        
        static bool GetCoord(Player p, string arg, int cur, string axis, out int value) {
            bool relative = arg[0] == '~';
            if (relative) arg = arg.Substring(1);
            value = 0;
            // ~ should work as ~0
            if (relative && arg.Length == 0) { value += cur; return true; }
            
            if (!GetInt(p, arg, axis, ref value)) return false;
            if (relative) value += cur;
            return true;
        }
        
        
        /// <summary> Attempts to parse the given argument as either a block name or a block ID. </summary>
        public static bool GetBlock(Player p, string input, out ExtBlock block, bool allowSkip = false) {
            block = default(ExtBlock);
            // Skip/None block for draw operations
            if (allowSkip && (input.CaselessEq("skip") || input.CaselessEq("none"))) {
                block = ExtBlock.Invalid; return true;
            }
            
            block = RawGetBlock(p, input);
            if (block.IsInvalid) Player.Message(p, "&cThere is no block \"{0}\".", input);
            return !block.IsInvalid;
        }
        
        /// <summary> Attempts to parse the given argument as either a block name or a block ID. </summary>
        /// <remarks> This does not output any messages to the player. </remarks>
        public static ExtBlock RawGetBlock(Player p, string input) {
            BlockDefinition[] defs = p == null ? BlockDefinition.GlobalDefs : p.level.CustomBlockDefs;
            byte id;
            // raw ID is treated specially, before names
            if (byte.TryParse(input, out id) && (id < Block.CpeCount || defs[id] != null)) {
                return ExtBlock.FromRaw(id);
            }
            
            int raw = BlockDefinition.GetBlock(input, defs);
            if (raw != -1) return ExtBlock.FromRaw((byte)raw);
            
            id = Block.Byte(input);
            if (id != Block.Invalid) return new ExtBlock(id, 0);
            
            return ExtBlock.Invalid;
        }

        /// <summary> Attempts to parse the given argument as either a block name or a block ID. </summary>
        /// <remarks> Also ensures the player is allowed to place the given block. </remarks>
        public static bool GetBlockIfAllowed(Player p, string input, out ExtBlock block, bool allowSkip = false) {
            if (!GetBlock(p, input, out block, allowSkip)) return false;
            if (allowSkip && block == ExtBlock.Invalid) return true;
            return IsBlockAllowed(p, "draw with", block);
        }
        
        /// <summary> Returns whether the player is allowed to place/modify/delete the given block. </summary>
        /// <remarks> Outputs information of which ranks can modify the block if not. </remarks>
        public static bool IsBlockAllowed(Player p, string action, ExtBlock block) {
            if (p == null || BlockPerms.UsableBy(p, block.BlockID)) return true;
            BlockPerms.List[block.BlockID].MessageCannotUse(p, action);
            return false;
        }
    }
}
