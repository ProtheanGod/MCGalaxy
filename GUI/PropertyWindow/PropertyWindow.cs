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
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MCGalaxy.Blocks;
using MCGalaxy.Commands;
using MCGalaxy.Games;

namespace MCGalaxy.Gui {
    public partial class PropertyWindow : Form {
        ZombieProperties zsSettings = new ZombieProperties();
        LavaProperties lsSettings = new LavaProperties();

        public PropertyWindow() {
            InitializeComponent();
            lsSettings.LoadFromServer();
            zsSettings.LoadFromServer();
            propsZG.SelectedObject = zsSettings;
            pg_lava.SelectedObject = lsSettings;
        }

        void PropertyWindow_Load(object sender, EventArgs e) {
            ToggleIrcSettings(ServerConfig.UseIRC);

            GuiPerms.UpdateRankNames();
            rank_cmbDefault.Items.AddRange(GuiPerms.RankNames);
            rank_cmbOsMap.Items.AddRange(GuiPerms.RankNames);
            blk_cmbMin.Items.AddRange(GuiPerms.RankNames);
            cmd_cmbMin.Items.AddRange(GuiPerms.RankNames);

            //Load server stuff
            LoadProperties();
            LoadRanks();
            try {
                LoadCommands();
                LoadBlocks();
            } catch (Exception ex) {
                Logger.LogError(ex);
                Logger.Log(LogType.Warning, "Failed to load commands and blocks!");
            }

            try {
                LoadLavaSettings();
                UpdateLavaMapList();
                UpdateLavaControls();
            } catch (Exception ex) {
                Logger.LogError(ex);
                Logger.Log(LogType.Warning, "Failed to load Lava Survival settings!");
            }

            try {
                lavaUpdateTimer = new System.Timers.Timer(10000) { AutoReset = true };
                lavaUpdateTimer.Elapsed += delegate {
                    UpdateLavaControls();
                    UpdateLavaMapList(false);
                };
                lavaUpdateTimer.Start();
            } catch {
                Logger.Log(LogType.Warning, "Failed to start lava control update timer!");
            }
        }

        void PropertyWindow_Unload(object sender, EventArgs e) {
            lavaUpdateTimer.Dispose();
            Window.prevLoaded = false;
            tw_selected = null;
        }

        void LoadProperties() {
            SrvProperties.Load();
            LoadGeneralProps();
            LoadChatProps();
            LoadIrcSqlProps();
            LoadMiscProps();
            LoadRankProps();
            LoadSecurityProps();
            zsSettings.LoadFromServer();
        }

        void SaveProperties() {
            try {
                ApplyGeneralProps();
                ApplyChatProps();
                ApplyIrcSqlProps();
                ApplyMiscProps();
                ApplyRankProps();
                ApplySecurityProps();
                zsSettings.ApplyToServer();
                lsSettings.ApplyToServer();
                
                SrvProperties.Save();
                CommandExtraPerms.Save();
            } catch( Exception ex ) {
                Logger.LogError(ex);
                Logger.Log(LogType.Warning, "SAVE FAILED! properties/server.properties");
            }
        }
        
        Color GetColor(string name) {
            string code = Colors.Parse(name);
            if (code.Length == 0) return SystemColors.Control;
            if (Colors.IsStandard(code[1])) return Color.FromName(name);
            
            ColorDesc col = Colors.Get(code[1]);
            return Color.FromArgb(col.R, col.G, col.B);
        }

        void btnSave_Click(object sender, EventArgs e) { SaveChanges(); Dispose(); }
        void btnApply_Click(object sender, EventArgs e) { SaveChanges(); }

        void SaveChanges() {
            SaveProperties();
            SaveRanks();
            SaveCommands();
            SaveBlocks();
            try { SaveLavaSettings(); }
            catch { Logger.Log(LogType.Warning, "Error saving Lava Survival settings!"); }
            try { ZSConfig.SaveSettings(); }
            catch { Logger.Log(LogType.Warning, "Error saving Zombie Survival settings!"); }

            SrvProperties.Load(); // loads when saving?
            CommandPerms.Load();
        }

        void btnDiscard_Click(object sender, EventArgs e) { Dispose(); }

        void GetHelp(string toHelp) {
            ConsoleHelpPlayer player = new ConsoleHelpPlayer();
            Command.all.FindByName("Help").Use(player, toHelp);
            
            MessageBox.Show(Colors.Strip(player.HelpOutput),
                            "Help information for " + toHelp);
        }
        
        sealed class ConsoleHelpPlayer : Player {
            public string HelpOutput = "";
            
            public ConsoleHelpPlayer() : base("(console)") {
                group = Group.NobodyRank;
            }
            
            public override void SendMessage(byte id, string message) {
                HelpOutput += message + "\r\n";
            }
        }
    }
}
