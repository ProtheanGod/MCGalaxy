﻿/*
    Copyright 2015 MCGalaxy
    
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
using MCGalaxy.Games.ZS;

namespace MCGalaxy.Commands.Fun {
    public sealed class CmdMapSet : Command {
        public override string name { get { return "MapSet"; } }
        public override string shortcut { get { return "MSet"; } }
        public override string type { get { return CommandTypes.Games; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
        public override CommandEnable Enabled { get { return CommandEnable.Zombie | CommandEnable.Lava; } }
        public override bool SuperUseable { get { return false; } }
        public override CommandAlias[] Aliases {
            get { return new[] { new CommandAlias("RoundTime", null, "roundtime") }; }
        }
        
        public override void Use(Player p, string message) {
            if (message.Length == 0) {
                Player.Message(p, "Map authors: " + p.level.Config.Authors);
                Player.Message(p, "Pillaring allowed: " + p.level.Config.Pillaring);
                Player.Message(p, "Build type: " + p.level.Config.BuildType);
                Player.Message(p, "Min round time: " + p.level.Config.MinRoundTime + " minutes");
                Player.Message(p, "Max round time: " + p.level.Config.MaxRoundTime + " minutes");
                Player.Message(p, "Drawing commands allowed: " + p.level.Config.DrawingAllowed);
                return;
            }
            
            string[] args = message.SplitSpaces(2);
            if (args.Length == 1) { Player.Message(p, "You need to provide a value."); return; }
            
            if (args[0].CaselessEq("author") || args[0].CaselessEq("authors")) {
                p.level.Config.Authors = args[1].Replace(" ", "%S, ");
                Player.Message(p, "Sets the authors of the map to: " + args[1]);
            } else if (args[0].CaselessEq("pillar") || args[0].CaselessEq("pillaring")) {
                bool value = false;
                if (!CommandParser.GetBool(p, args[1], ref value)) return;
                
                p.level.Config.Pillaring = value;
                Player.Message(p, "Set pillaring allowed to: " + value);
                HUD.UpdateAllSecondary(Server.zombie);
            } else if (args[0].CaselessEq("build") || args[0].CaselessEq("buildtype")) {
                BuildType value = BuildType.Normal;
                if (!CommandParser.GetEnum(p, args[1], "Build type", ref value)) return;
                
                p.level.Config.BuildType = value;
                p.level.UpdateBlockPermissions();
                Player.Message(p, "Set build type to: " + value);
                HUD.UpdateAllSecondary(Server.zombie);
            } else if (args[0].CaselessEq("minroundtime") || args[0].CaselessEq("minround")) {
                byte time = GetRoundTime(p, args[1]);
                if (time == 0) return;
                
                if (time > p.level.Config.MaxRoundTime) {
                    Player.Message(p, "Min round time must be less than or equal to max round time"); return;
                }
                p.level.Config.MinRoundTime = time;
                Player.Message(p, "Set min round time to: " + time + " minutes");
            } else if (args[0].CaselessEq("maxroundtime") || args[0].CaselessEq("maxround")) {
                byte time = GetRoundTime(p, args[1]);
                if (time == 0) return;
                
                if (time < p.level.Config.MinRoundTime) {
                    Player.Message(p, "Max round time must be greater than or equal to min round time"); return;
                }
                p.level.Config.MaxRoundTime = time;
                Player.Message(p, "Set max round time to: " + time + " minutes");
            } else if (args[0].CaselessEq("roundtime") || args[0].CaselessEq("round")) {
                byte time = GetRoundTime(p, args[1]);
                if (time == 0) return;
                
                p.level.Config.MinRoundTime = time;
                p.level.Config.MaxRoundTime = time;
                Player.Message(p, "Set round time to: " + time + " minutes");
            } else if (args[0].CaselessEq("drawingallowed") || args[0].CaselessEq("drawingenabled")) {
                bool value = false;
                if (!CommandParser.GetBool(p, args[1], ref value)) return;
                
                p.level.Config.DrawingAllowed = value;
                Player.Message(p, "Set drawing commands allowed to: " + value);
            } else {
                Player.Message(p, "Unrecognised property \"" + args[0] + "\"."); return;
            }
            Level.SaveSettings(p.level);
        }
        
        static byte GetRoundTime(Player p, string arg) {
            byte time = 0;
            if (!CommandParser.GetByte(p, arg, "Minutes", ref time, 1, 10)) return 0;
            
            return time;
        }
        
        public override void Help(Player p) {
            Player.Message(p, "%HThis sets the various options for games on this map.");
            Player.Message(p, "%T/MapSet authors [name1] <name2> <name3>...");
            Player.Message(p, "%HThis is shown to players at the start of rounds.");
            Player.Message(p, "%T/MapSet pillaring [yes/no]");
            Player.Message(p, "%T/MapSet build [normal/modifyonly/nomodify]");
            Player.Message(p, "%T/MapSet minroundtime [minutes]");
            Player.Message(p, "%T/MapSet maxroundtime [minutes]");
            Player.Message(p, "%T/MapSet drawingallowed [yes/no]");
        }
    }
}
