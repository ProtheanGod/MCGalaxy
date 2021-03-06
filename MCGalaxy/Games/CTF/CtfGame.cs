/*
    Copyright 2011 MCForge
    
    Written by fenderrock87
    
    Dual-licensed under the    Educational Community License, Version 2.0 and
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
using System.IO;
using System.Threading;
using MCGalaxy.Commands.World;
using MCGalaxy.Maths;
using MCGalaxy.SQL;

namespace MCGalaxy.Games {
    
    internal sealed class CtfData {
        public Player p;
        public int Captures, Tags, Points;
        public bool hasflag, tagging, TeamChatting;
        public CtfData(Player p) { this.p = p; }
    }

    public sealed partial class CTFGame : IGame {
        public System.Timers.Timer tagging = new System.Timers.Timer(500);
        public bool voting = false;
        internal int vote1 = 0, vote2 = 0, vote3 = 0;
        internal string map1 = "", map2 = "", map3 = "";
        public bool started = false;
        
        /// <summary> Gets whether CTF is currently running. </summary>
        public override bool Running { get { return started; } }
        
        public CtfTeam2 Red, Blue;
        List<CtfData> cache = new List<CtfData>();
        
        public CTFConfig Config = new CTFConfig();
        CtfPlugin plugin = new CtfPlugin();
        
        /// <summary> Create a new CTF object </summary>
        public CTFGame() {
            Red = new CtfTeam2("Red", Colors.red);
            Blue = new CtfTeam2("Blue", Colors.blue);
            
            tagging.Elapsed += CheckTagging;
            tagging.Start();
            plugin.Game = this;
            plugin.Load(false); 
        }
        
        
        internal CtfData Get(Player p) {
            foreach (CtfData d in cache) {
                if (d.p == p) return d;
            }
            return null;
        }

        
        /// <summary> Load a map into CTF </summary>
        public void SetMap(string mapName) {
            MapName = mapName;
            CmdLoad.LoadLevel(null, mapName);
            Map = LevelInfo.FindExact(mapName);
            Map.SaveChanges = false;
            UpdateConfig();
        }
        
        public void UpdateConfig() {
            Config.SetDefaults(Map);
            Config.Retrieve(Map.name);
            CTFConfig cfg = Config;
            
            Red.FlagBlock = ExtBlock.FromRaw(cfg.RedFlagBlock);
            Red.FlagPos = new Vec3U16((ushort)cfg.RedFlagX, (ushort)cfg.RedFlagY, (ushort)cfg.RedFlagZ);
            Red.SpawnPos = new Position(cfg.RedSpawnX, cfg.RedSpawnY, cfg.RedSpawnZ);
            
            Blue.FlagBlock = ExtBlock.FromRaw(cfg.BlueFlagBlock);
            Blue.FlagPos = new Vec3U16((ushort)cfg.BlueFlagX, (ushort)cfg.BlueFlagY, (ushort)cfg.BlueFlagZ);
            Blue.SpawnPos = new Position(cfg.BlueSpawnX, cfg.BlueSpawnY, cfg.BlueSpawnZ);
        }
        
        List<string> GetCtfMaps() {
            //Load some configs
            if (!Directory.Exists("CTF")) Directory.CreateDirectory("CTF");
            if (!File.Exists("CTF/maps.config")) return new List<string>();
            
            string[] lines = File.ReadAllLines("CTF/maps.config");
            return new List<string>(lines);
        }
        
        
        void CheckTagging(object sender, System.Timers.ElapsedEventArgs e) {
            Player[] online = PlayerInfo.Online.Items;
            foreach (Player p in online) {
                if (p.level != Map) continue;
                
                CtfTeam2 team = TeamOf(p);
                if (team == null || Get(p).tagging) continue;
                if (!OnOwnTeamSide(p.Pos.BlockZ, team)) continue;
                CtfTeam2 opposing = Opposing(team);
                
                Player[] opponents = opposing.Members.Items;
                foreach (Player other in opponents) {
                    if (!MovementCheck.InRange(p, other, 2 * 32)) continue;

                    Get(other).tagging = true;
                    Player.Message(other, p.ColoredName + " %Stagged you!");
                    Command.all.FindByName("Spawn").Use(other, "");
                    Thread.Sleep(300);
                    
                    if (Get(other).hasflag) DropFlag(p, opposing);
                    Get(p).Points += Config.Tag_PointsGained;
                    Get(other).Points -= Config.Tag_PointsLost;
                    Get(p).Tags++;
                    Get(other).tagging = false;
                }
            }
        }

        static ColumnDesc[] createSyntax = new ColumnDesc[] {
            new ColumnDesc("ID", ColumnType.Integer, priKey: true, autoInc: true, notNull: true),
            new ColumnDesc("Name", ColumnType.VarChar, 20),
            new ColumnDesc("Points", ColumnType.UInt24),
            new ColumnDesc("Captures", ColumnType.UInt24),
            new ColumnDesc("tags", ColumnType.UInt24),
        };
        
        /// <summary> Start the CTF game </summary>
        public bool Start(Player p) {
            if (started) {
                Player.Message(p, "CTF game already running."); return false;
            }
            
            List<string> maps = GetCtfMaps();
            if (maps.Count == 0) {
                Player.Message(p, "No CTF maps were found."); return false;
            }
            
            Blue = new CtfTeam2("Blue", Colors.blue);
            Red = new CtfTeam2("Red", Colors.red);
            SetMap(maps[new Random().Next(maps.Count)]);
            
            Logger.Log(LogType.GameActivity, "[CTF] Running...");
            started = true;
            Database.Backend.CreateTable("CTF", createSyntax);
            return true;
        }
        
        /// <summary> Stop the CTF game if running. </summary>
        public void Stop() {
            tagging.Stop();
            tagging.Dispose();
            Map = null;
            started = false;
        }
        
        string Vote() {
            started = false;
            vote1 = 0;
            vote2 = 0;
            vote3 = 0;
            Random rand = new Random();
            List<string> maps = GetCtfMaps();
            map1 = maps[rand.Next(maps.Count)];
            maps.Remove(map1);
            map2 = maps[rand.Next(maps.Count)];
            maps.Remove(map2);
            map3 = maps[rand.Next(maps.Count)];
            Chat.MessageLevel(Map, "%2VOTE:");
            Chat.MessageLevel(Map, "1. " + map1 + " 2. " + map2 + " 3. " + map3);
            voting = true;
            int seconds = rand.Next(15, 61);
            Chat.MessageLevel(Map, "You have " + seconds + " seconds to vote!");
            Thread.Sleep(seconds * 1000);
            voting = false;
            Chat.MessageLevel(Map, "VOTING ENDED!");
            Thread.Sleep(rand.Next(1, 10) * 1000);
            if (vote1 > vote2 && vote1 > vote3)
            {
                Chat.MessageLevel(Map, map1 + " WON!");
                return map1;
            }
            if (vote2 > vote1 && vote2 > vote3)
            {
                Chat.MessageLevel(Map, map2 + " WON!");
                return map2;
            }
            if (vote3 > vote2 && vote3 > vote1)
            {
                Chat.MessageLevel(Map, map3 + " WON!");
                return map3;
            }
            else
            {
                Chat.MessageLevel(Map, "There was a tie!");
                Chat.MessageLevel(Map, "I'll choose!");
                return maps[rand.Next(maps.Count)];
            }
        }
        
        public override void EndRound() {
            started = false;
            if (Blue.Points >= Config.RoundPoints || Blue.Points > Red.Points) {
                Chat.MessageLevel(Map, Blue.ColoredName + " %Swon this round of CTF!");
            } else if (Red.Points >= Config.RoundPoints || Red.Points > Blue.Points) {
                Chat.MessageLevel(Map, Red.ColoredName + " %Swon this round of CTF!");
            } else {
                Chat.MessageLevel(Map, "The round ended in a tie!");
            }
            
            Thread.Sleep(4000);
            //MYSQL!
            cache.ForEach(delegate(CtfData d) {
                              d.hasflag = false;
                              Database.Backend.UpdateRows("CTF", "Points=@1, Captures=@2, tags=@3",
                                                          "WHERE Name = @0", d.p.name, d.Points, d.Captures, d.Tags);
                          });
            
            string nextmap = Vote();
            Chat.MessageLevel(Map, "Starting a new game!");
            Blue.Members.Clear();
            Red.Members.Clear();
            Thread.Sleep(2000);
            SetMap(nextmap);
        }
        
        
        /// <summary> Called when the given player takes the opposing team's flag. </summary>
        public void TakeFlag(Player p, CtfTeam2 team) {
            CtfTeam2 opposing = Opposing(team);
            Chat.MessageLevel(Map, team.Color + p.DisplayName + " took the " + opposing.ColoredName + " %Steam's FLAG");
            Get(p).hasflag = true;
        }
        
        /// <summary> Called when the given player, while holding opposing team's flag, clicks on their own flag. </summary>
        public void ReturnFlag(Player p, CtfTeam2 team) {
            Vec3U16 flagPos = team.FlagPos;
            p.RevertBlock(flagPos.X, flagPos.Y, flagPos.Z);
            p.cancelBlock = true;
            
            CtfData data = Get(p);
            if (data.hasflag) {
                Chat.MessageLevel(Map, team.Color + p.DisplayName + " RETURNED THE FLAG!");
                data.hasflag = false;
                data.Points += Config.Capture_PointsGained;
                data.Captures++;
                
                CtfTeam2 opposing = Opposing(team);
                team.Points++;
                flagPos = opposing.FlagPos;
                Map.Blockchange(flagPos.X, flagPos.Y, flagPos.Z, opposing.FlagBlock);
                
                if (team.Points >= Config.RoundPoints) EndRound();
            } else {
                Player.Message(p, "You cannot take your own flag!");
            }
        }

        /// <summary> Called when the given player drops the opposing team's flag. </summary>
        public void DropFlag(Player p, CtfTeam2 team) {
            CtfData data = Get(p);
            if (!data.hasflag) return;
            
            data.hasflag = false;
            Chat.MessageLevel(Map, team.Color + p.DisplayName + " DROPPED THE FLAG!");
            data.Points -= Config.Capture_PointsLost;
            
            CtfTeam2 opposing = Opposing(team);
            Vec3U16 pos = opposing.FlagPos;
            Map.Blockchange(pos.X, pos.Y, pos.Z, opposing.FlagBlock);
        }
        

        public void JoinTeam(Player p, CtfTeam2 team) {
            if (Get(p) == null) {
                cache.Add(new CtfData(p));
            } else {
                Get(p).hasflag = false;
            }
            
            team.Members.Add(p);
            Chat.MessageLevel(Map, p.ColoredName + " %Sjoined the " + team.ColoredName + " %Steam");
            Player.Message(p, "You are now on the " + team.ColoredName + " team!");
        }
        
        bool OnOwnTeamSide(int z, CtfTeam2 team) {
            int baseZ = team.FlagPos.Z, zline = Config.ZDivider;
            if (baseZ < zline && z < zline) return true;
            if (baseZ > zline && z > zline) return true;
            return false;
        }
        
        public CtfTeam2 TeamOf(Player p) {
            if (Red.Members.Contains(p)) return Red;
            if (Blue.Members.Contains(p)) return Blue;
            return null;
        }
        
        public CtfTeam2 Opposing(CtfTeam2 team) {
            return team == Red ? Blue : Red;
        }
    }
}
