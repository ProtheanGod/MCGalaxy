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
using System.Collections.Generic;
using MCGalaxy.Commands;

namespace MCGalaxy {
    
    /// <summary> Encapuslates access permissions (visit or build) for a level/zone. </summary>
    public abstract class AccessController {
        
        public abstract LevelPermission Min { get; set; }
        public abstract LevelPermission Max { get; set; }
        public abstract List<string> Whitelisted { get; }        
        public abstract List<string> Blacklisted { get; }
        
        protected abstract string ColoredName { get; }
        protected abstract string Action { get; }
        protected abstract string ActionIng { get; }
        protected abstract string Type { get; }
        protected abstract string MaxCmd { get; }
        
        
        /// <summary> Returns the allowed state for the given player. </summary>
        public AccessResult Check(Player p) { return Check(p.name, p.group); }
        
        /// <summary> Returns the allowed state for the given player. </summary>
        public AccessResult Check(string name, Group rank) {
            if (Blacklisted.CaselessContains(name))
                return AccessResult.Blacklisted;
            if (Whitelisted.CaselessContains(name))
                return AccessResult.Whitelisted;
            
            if (rank.Permission < Min) return AccessResult.BelowMinRank;
            if (rank.Permission > Max && MaxCmd != null && rank.Permission < CommandExtraPerms.MinPerm(MaxCmd)) {
                return AccessResult.AboveMaxRank;
            }
            return AccessResult.Allowed;
        }

        public bool CheckDetailed(Player p, bool ignoreRankPerm = false) {
            AccessResult result = Check(p);
            if (result == AccessResult.Allowed) return true;
            if (result == AccessResult.Whitelisted) return true;
            if (result == AccessResult.AboveMaxRank && ignoreRankPerm) return true;
            if (result == AccessResult.BelowMinRank && ignoreRankPerm) return true;
            
            if (result == AccessResult.Blacklisted) {
                Player.Message(p, "You are blacklisted from {0} {1}", ActionIng, ColoredName);
                return false;
            }
            
            string whitelist = "";
            if (Whitelisted.Count > 0) {
                whitelist = "(and " + Whitelisted.Join(pl => PlayerInfo.GetColoredName(p, pl)) + "%S) ";
            }
            
            if (result == AccessResult.BelowMinRank) {
                Player.Message(p, "Only {2}%S+ {3}may {0} {1}",
                               Action, ColoredName, Group.GetColoredName(Min), whitelist);
            } else if (result == AccessResult.AboveMaxRank) {
                Player.Message(p, "Only {2} %Sand below {3}may{0} {1}",
                               Action, ColoredName, Group.GetColoredName(Max), whitelist);
            }
            return false;
        }
        

        public bool SetMin(Player p, Group grp) {
            string minType = "Min " + Type;
            if (!CheckRank(p, Min, minType, false)) return false;
            if (!CheckRank(p, grp.Permission, minType, true)) return false;
            
            Min = grp.Permission;
            OnPermissionChanged(p, grp, minType);
            return true;
        }

        public bool SetMax(Player p, Group grp) {
            string maxType = "Max " + Type;
            const LevelPermission ignore = LevelPermission.Nobody;
            if (Max != ignore && !CheckRank(p, Max, maxType, false)) return false;
            if (grp.Permission != ignore && !CheckRank(p, grp.Permission, maxType, true)) return false;
            
            Max = grp.Permission;
            OnPermissionChanged(p, grp, maxType);
            return true;
        }

        public bool Whitelist(Player p, string target) {
            if (!CheckList(p, target, true)) return false;
            if (Whitelisted.CaselessContains(target)) {
                Player.Message(p, "{0} %Sis already whitelisted.", PlayerInfo.GetColoredName(p, target));
                return true;
            }
            
            bool removed = true;
            if (!Blacklisted.CaselessRemove(target)) {
                Whitelisted.Add(target);
                removed = false;
            }
            OnListChanged(p, target, true, removed);
            return true;
        }
        
        public bool Blacklist(Player p, string target) {
            if (!CheckList(p, target, false)) return false;
            if (Blacklisted.CaselessContains(target)) {
                Player.Message(p, "{0} %Sis already blacklisted.", PlayerInfo.GetColoredName(p, target));
                return true;
            }
            
            bool removed = true;
            if (!Whitelisted.CaselessRemove(target)) {
                Blacklisted.Add(target);
                removed = false;
            }
            OnListChanged(p, target, false, removed);
            return true;
        }


        public abstract void OnPermissionChanged(Player p, Group grp, string type);
        public abstract void OnListChanged(Player p, string name, bool whitelist, bool removedFromOpposite);
        
        bool CheckRank(Player p, LevelPermission perm, string type, bool newPerm) {
            if (p != null && perm > p.Rank) {
                Player.Message(p, "You cannot change the {0} rank of this level{1} higher than yours.",
                               type.ToLower(),
                               newPerm ? " to a rank" : ", as its current " + type.ToLower() + " rank is");
                return false;
            }
            return true;
        }
        
        /// <summary> Returns true if the player is allowed to modify these access permissions,
        /// and is also allowed to change the access permissions for the target player. </summary>
        bool CheckList(Player p, string name, bool whitelist) {
            if (p != null && !CheckDetailed(p)) {
                string mode = whitelist ? "whitelist" : "blacklist";
                Player.Message(p, "Hence you cannot modify the {0} {1}.", Type, mode); return false;
            }
            
            bool higherRank = p != null && PlayerInfo.GetGroup(name).Permission > p.Rank;
            if (!higherRank) return true;
            
            if (!whitelist) {
                Player.Message(p, "You cannot blacklist players of a higher rank.");
                return false;
            } else if (Check(name, Group.GroupIn(name)) == AccessResult.Blacklisted) {
                Player.Message(p, "{0} %Sis blacklisted from {1} {2}%S.",
                               PlayerInfo.GetColoredName(p, name), ActionIng, ColoredName);
                return false;
            }
            return true;
        }
    }
    
    /// <summary> Encapuslates access permissions (visit or build) for a level. </summary>
    public sealed class LevelAccessController : AccessController {
        
        public readonly bool IsVisit;
        readonly Level lvl;
        readonly LevelConfig cfg;
        readonly string lvlName;
        
        public LevelAccessController(Level lvl, bool isVisit) {
            this.lvl = lvl;
            this.cfg = lvl.Config;
            IsVisit = isVisit;
        }
        
        public LevelAccessController(LevelConfig cfg, string levelName, bool isVisit) {
            this.cfg = cfg;
            this.lvlName = levelName;
            IsVisit = isVisit;
        }

        public override LevelPermission Min {
            get { return IsVisit ? cfg.VisitMin : cfg.BuildMin; }
            set {
                if (IsVisit) cfg.VisitMin = value;
                else cfg.BuildMin = value;
            }
        }

        public override LevelPermission Max {
            get { return IsVisit ? cfg.VisitMax : cfg.BuildMax; }
            set {
                if (IsVisit) cfg.VisitMax = value;
                else cfg.BuildMax = value;
            }
        }

        public override List<string> Whitelisted {
            get { return IsVisit ? cfg.VisitWhitelist : cfg.BuildWhitelist; }
        }

        public override List<string> Blacklisted {
            get { return IsVisit ? cfg.VisitBlacklist : cfg.BuildBlacklist; }
        }
        
        protected override string ColoredName {
            get { return lvl != null ? lvl.ColoredName : cfg.Color + lvlName; }
        }
        protected override string Action { get { return IsVisit ? "go to" : "build in"; } }
        protected override string ActionIng { get { return IsVisit ? "going to" : "building in"; } }
        protected override string Type { get { return IsVisit ? "visit" : "build"; } }
        protected override string MaxCmd { get { return IsVisit ? "PerVisit" : "PerBuild"; } }
        

        public override void OnPermissionChanged(Player p, Group grp, string type) {
            Update();
            Logger.Log(LogType.UserActivity, "{0} rank changed to {1} on {2}.", type, grp.Name, lvl.name);
            Chat.MessageLevel(lvl, type + " rank changed to " + grp.ColoredName + "%S.");
            if (p != null && p.level != lvl) {
                Player.Message(p, "{0} rank changed to {1} %Son {2}%S.", type, grp.ColoredName, ColoredName);
            }
        }
 
        public override void OnListChanged(Player p, string name, bool whitelist, bool removedFromOpposite) {
            string type = IsVisit ? "visit" : "build";
            string msg = PlayerInfo.GetColoredName(p, name);
            if (removedFromOpposite) {
                msg += " %Swas removed from the " + type + (whitelist ? " blacklist" : " whitelist");
            } else {
                msg += " %Swas " + type + (whitelist ? " whitelisted" : " blacklisted");
            }
            
            Update();
            Logger.Log(LogType.UserActivity, "{0} on {1}", msg, lvl.name);
            Chat.MessageLevel(lvl, msg);
            if (p != null && p.level != lvl) {
                Player.Message(p, "{0} on %S{1}", msg, ColoredName);
            }
        }
        
        
        void Update() {
            Level.SaveSettings(lvl);
            UpdateAllowBuild();
            UpdateAllowVisit();
        }
        
        void UpdateAllowBuild() {
            if (IsVisit) return;
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player p in players) {
                if (p.level != lvl) continue;
                
                AccessResult access = Check(p);
                p.AllowBuild = access == AccessResult.Whitelisted || access == AccessResult.Allowed;
            }
        }
        
        void UpdateAllowVisit() {
            if (!IsVisit || lvl == Server.mainLevel) return;
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player p in players) {
                if (p.level != lvl) continue;
                
                AccessResult access = Check(p);
                bool allowVisit = access == AccessResult.Whitelisted || access == AccessResult.Allowed;
                if (allowVisit) continue;
                
                Player.Message(p, "&cNo longer allowed to visit %S{0}", lvl.ColoredName);
                PlayerActions.ChangeMap(p, Server.mainLevel);
            }
        }
    }
    
    public enum AccessResult {
        
        /// <summary> The player is whitelisted and always allowed. </summary>
        Whitelisted,
        
        /// <summary> The player is blacklisted and never allowed. </summary>
        Blacklisted,
        
        /// <summary> The player is allowed (by their rank) </summary>
        Allowed,
        
        /// <summary> The player's rank is below the minimum rank allowed. </summary>
        BelowMinRank,
        
        /// <summary> The player's rank is above the maximum rank allowed. </summary>
        AboveMaxRank,
    }
}