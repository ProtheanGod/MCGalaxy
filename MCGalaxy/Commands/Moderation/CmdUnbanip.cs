/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCGalaxy)
    
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
using System.Net;
using MCGalaxy.Events;

namespace MCGalaxy.Commands.Moderation {
    public sealed class CmdUnbanip : Command {
        public override string name { get { return "UnbanIP"; } }
        public override string type { get { return CommandTypes.Moderation; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
        public override CommandAlias[] Aliases {
            get { return new CommandAlias[] { new CommandAlias("UnIPBan") }; }
        }

        public override void Use(Player p, string message) {
            if (message.Length == 0) { Help(p); return; }
            string[] args = message.SplitSpaces(2);
            args[0] = ModActionCmd.FindIP(p, args[0], "un-IP ban", "unbanip");
            if (args[0] == null) return;

            IPAddress ip;
            if (!IPAddress.TryParse(args[0], out ip)) { Player.Message(p, "\"{0}\" is not a valid IP.", args[0]); return; }
            if (p != null && p.ip == args[0]) { Player.Message(p, "You cannot un-IP ban yourself."); return; }
            if (!Server.bannedIP.Contains(args[0])) { Player.Message(p, args[0] + " is not a banned IP."); return; }
            
            string reason = args.Length > 1 ? args[1] : "";
            reason = ModActionCmd.ExpandReason(p, reason);
            if (reason == null) return;
            
            ModAction action = new ModAction(args[0], p, ModActionType.UnbanIP, reason);
            OnModActionEvent.Call(action);
        }
        
        public override void Help(Player p)  {
            Player.Message(p, "%T/UnbanIP [ip/player]");
            Player.Message(p, "%HUn-bans an IP, or the IP the given player is on.");
        }
    }
}
