/*
    Copyright 2011 MCForge
        
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
using System.Collections.Generic;
using System.Data;
using MCGalaxy.Blocks;
using MCGalaxy.SQL;
using MCGalaxy.Util;

namespace MCGalaxy.Commands.Building {
    public sealed class CmdPortal : Command {
        public override string name { get { return "Portal"; } }
        public override string shortcut { get { return "o"; } }
        public override string type { get { return CommandTypes.Building; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.AdvBuilder; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string message) {
            PortalArgs data = new PortalArgs();
            data.Multi = false;
            string[] args = message.SplitSpaces();
            string block = message.Length == 0 ? "" : args[0].ToLower();

            if (args.Length >= 2 && args[1].CaselessEq("multi")) {
                data.Multi = true;
            } else if (args.Length >= 2) {
                Help(p); return;
            }

            data.Block = GetBlock(p, block);
            if (data.Block == ExtBlock.Invalid) return;
            if (!CommandParser.IsBlockAllowed(p, "place a portal of", data.Block)) return;
            data.Entries = new List<PortalPos>();
            
            Player.Message(p, "Place an &aEntry block %Sfor the portal");
            p.ClearBlockchange();
            p.blockchangeObject = data;
            p.Blockchange += EntryChange;
        }
        
        ExtBlock GetBlock(Player p, string name) {
            if (name == "show") { ShowPortals(p); return ExtBlock.Invalid; }
            ExtBlock block = CommandParser.RawGetBlock(p, name);
            if (!block.IsInvalid && p.level.Props[block.Index].IsPortal)
                return block;
            
            // Hardcoded aliases for backwards compatibility
            block.BlockID = Block.Invalid; block.ExtID = 0;
            if (name.Length == 0) block.BlockID = Block.Portal_Blue;
            if (name == "blue") block.BlockID = Block.Portal_Blue;
            if (name == "orange") block.BlockID = Block.Portal_Orange;
            if (name == "air") block.BlockID = Block.Portal_Air;
            if (name == "water") block.BlockID = Block.Portal_Water;
            if (name == "lava") block.BlockID = Block.Portal_Lava;
            
            if (p.level.Props[block.Index].IsPortal) return block;
            Help(p); return ExtBlock.Invalid;
        }

        void EntryChange(Player p, ushort x, ushort y, ushort z, ExtBlock block) {
            PortalArgs args = (PortalArgs)p.blockchangeObject;
            ExtBlock old = p.level.GetBlock(x, y, z);
            if (!p.level.CheckAffectPermissions(p, x, y, z, old, args.Block)) {
                p.RevertBlock(x, y, z); return;
            }
            p.ClearBlockchange();

            if (args.Multi && block.BlockID == Block.Red && args.Entries.Count > 0) {
                ExitChange(p, x, y, z, block); return;
            }

            p.level.UpdateBlock(p, x, y, z, args.Block);
            p.SendBlockchange(x, y, z, (ExtBlock)Block.Green);
            PortalPos Port;

            Port.Map = p.level.name;
            Port.x = x; Port.y = y; Port.z = z;
            args.Entries.Add(Port);
            p.blockchangeObject = args;

            if (!args.Multi) {
                p.Blockchange += ExitChange;
                Player.Message(p, "&aEntry block placed");
            } else {
                p.Blockchange += EntryChange;
                Player.Message(p, "&aEntry block placed. &c{0} block for exit",
                               p.Level.BlockName((ExtBlock)Block.Red));
            }
        }
        
        void ExitChange(Player p, ushort x, ushort y, ushort z, ExtBlock block) {
            p.ClearBlockchange();
            p.RevertBlock(x, y, z);
            PortalArgs args = (PortalArgs)p.blockchangeObject;
            string dstMap = p.level.name.UnicodeToCp437();

            foreach (PortalPos P in args.Entries) {
                string lvlName = P.Map;
                object locker = ThreadSafeCache.DBCache.GetLocker(lvlName);
                
                lock (locker) {
                    Database.Backend.CreateTable("Portals" + lvlName, LevelDB.createPortals);
                    Level map = LevelInfo.FindExact(P.Map);
                    if (map != null) map.hasPortals = true;

                    int count = 0;
                    using (DataTable portals = Database.Backend.GetRows("Portals" + lvlName, "*",
                                                                        "WHERE EntryX=@0 AND EntryY=@1 AND EntryZ=@2", P.x, P.y, P.z)) {
                        count = portals.Rows.Count;
                    }
                    
                    if (count == 0) {
                        Database.Backend.AddRow("Portals" + lvlName, "EntryX, EntryY, EntryZ, ExitX, ExitY, ExitZ, ExitMap",
                                                P.x, P.y, P.z, x, y, z, dstMap);
                    } else {
                        Database.Backend.UpdateRows("Portals" + lvlName, "ExitMap=@6, ExitX=@3, ExitY=@4, ExitZ=@5",
                                                    "WHERE EntryX=@0 AND EntryY=@1 AND EntryZ=@2", P.x, P.y, P.z, x, y, z, dstMap);
                    }
                }
                if (P.Map == p.level.name)
                    p.SendBlockchange(P.x, P.y, P.z, args.Block);
            }

            Player.Message(p, "&3Exit %Sblock placed");
            if (!p.staticCommands) return;
            args.Entries.Clear();
            p.blockchangeObject = args;
            p.Blockchange += EntryChange;
        }

        class PortalArgs { public List<PortalPos> Entries; public ExtBlock Block; public bool Multi; }
        struct PortalPos { public ushort x, y, z; public string Map; }

        
        void ShowPortals(Player p) {
            p.showPortals = !p.showPortals;
            int count = 0;
            
            if (p.level.hasPortals) {
                using (DataTable table = Database.Backend.GetRows("Portals" + p.level.name, "*")) {
                    count = table.Rows.Count;
                    if (p.showPortals) { ShowPortals(p, table); } 
                    else { HidePortals(p, table); }
                }
            }            
            Player.Message(p, "Now {0} %Sportals.", p.showPortals ? "showing &a" + count : "hiding");
        }
        
        static void ShowPortals(Player p, DataTable table) {
            foreach (DataRow row in table.Rows) {
                if (row["ExitMap"].ToString() == p.level.name) {
                    p.SendBlockchange(U16(row["ExitX"]), U16(row["ExitY"]), U16(row["ExitZ"]), (ExtBlock)Block.Red);
                }
                p.SendBlockchange(U16(row["EntryX"]), U16(row["EntryY"]), U16(row["EntryZ"]), (ExtBlock)Block.Green);
            }
        }
        
        static void HidePortals(Player p, DataTable table) {
            foreach (DataRow row in table.Rows) {
                if (row["ExitMap"].ToString() == p.level.name) {
                    p.RevertBlock(U16(row["ExitX"]), U16(row["ExitY"]), U16(row["ExitZ"]));
                }
                p.RevertBlock(U16(row["EntryX"]), U16(row["EntryY"]), U16(row["EntryZ"]));
            }
        }
        
        static ushort U16(object x) { return Convert.ToUInt16(x); }
        
        
        static string Format(ExtBlock block, Level lvl, BlockProps[] props) {
            if (!props[block.Index].IsPortal) return null;
            
            // We want to use the simple aliases if possible
            if (block.BlockID == Block.Portal_Orange) return "orange";
            if (block.BlockID == Block.Portal_Blue)   return "blue";
            if (block.BlockID == Block.Portal_Air)    return "air";
            if (block.BlockID == Block.Portal_Lava)   return "lava";
            if (block.BlockID == Block.Portal_Water)  return "water";
            
            return lvl == null ? Block.Name(block.BlockID) : lvl.BlockName(block);
        }
        
        static void GetAllNames(Player p, List<string> names) {
            GetCoreNames(names, p.level);
            for (int i = Block.CpeCount; i < Block.Count; i++) {
                ExtBlock block = ExtBlock.FromRaw((byte)i);
                string name = Format(block, p.level, p.level.Props);
                if (name != null) names.Add(name);
            }
        }
        
        static void GetCoreNames(List<string> names, Level lvl) {
            BlockProps[] props = lvl != null ? lvl.Props : Block.Props;
            for (int i = Block.Air; i < Block.Count; i++) {
                ExtBlock block = ExtBlock.FromIndex(i);
                if (block.BlockID == Block.custom_block) continue;
                
                string name = Format(block, lvl, props);
                if (name != null) names.Add(name);
            }
        }
        
        public override void Help(Player p) {
            Player.Message(p, "%T/Portal [block]");
            Player.Message(p, "%HPlace a block for the entry, then another block for exit.");
            Player.Message(p, "%T/Portal [block] multi");
            Player.Message(p, "%HPlace multiple blocks for entries, then a red block for exit.");
            Player.Message(p, "%H  Note: The exit can be on a different level.");
            
            List<string> names = new List<string>();
            if (Player.IsSuper(p)) GetCoreNames(names, null);
            else GetAllNames(p, names);
            
            Player.Message(p, "%H  Supported blocks: %S{0}", names.Join());
            Player.Message(p, "%T/Portal show %H- Shows portals (green = entry, red = exit)");
        }
    }
}
