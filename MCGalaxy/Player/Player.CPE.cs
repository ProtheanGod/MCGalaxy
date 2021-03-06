/*
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
using MCGalaxy.Blocks;
using MCGalaxy.Network;

namespace MCGalaxy {
    public partial class Player {
        
        class ExtEntry {
            public string ExtName;
            public byte ClientExtVersion, ServerExtVersion = 1;
            
            public ExtEntry(string extName) { ExtName = extName; }
            public ExtEntry(string extName, byte extVersion) {
                ExtName = extName; ServerExtVersion = extVersion;
            }
        }
        
        ExtEntry[] extensions = new ExtEntry[] {
            new ExtEntry(CpeExt.ClickDistance),    new ExtEntry(CpeExt.CustomBlocks),
            new ExtEntry(CpeExt.HeldBlock),        new ExtEntry(CpeExt.TextHotkey),
            new ExtEntry(CpeExt.ExtPlayerList, 2), new ExtEntry(CpeExt.EnvColors),
            new ExtEntry(CpeExt.SelectionCuboid),  new ExtEntry(CpeExt.BlockPermissions),
            new ExtEntry(CpeExt.ChangeModel),      new ExtEntry(CpeExt.EnvMapAppearance, 2),
            new ExtEntry(CpeExt.EnvWeatherType),   new ExtEntry(CpeExt.HackControl),
            new ExtEntry(CpeExt.EmoteFix),         new ExtEntry(CpeExt.MessageTypes),
            new ExtEntry(CpeExt.LongerMessages),   new ExtEntry(CpeExt.FullCP437),
            new ExtEntry(CpeExt.BlockDefinitions), new ExtEntry(CpeExt.BlockDefinitionsExt, 2),
            new ExtEntry(CpeExt.TextColors),       new ExtEntry(CpeExt.BulkBlockUpdate),
            new ExtEntry(CpeExt.EnvMapAspect),     new ExtEntry(CpeExt.PlayerClick),
            new ExtEntry(CpeExt.EntityProperty),   new ExtEntry(CpeExt.ExtEntityPositions),
            new ExtEntry(CpeExt.TwoWayPing),       new ExtEntry(CpeExt.InventoryOrder),
            new ExtEntry(CpeExt.InstantMOTD),
        };
        
        ExtEntry FindExtension(string extName) {
            foreach (ExtEntry ext in extensions) {
                if (ext.ExtName.CaselessEq(extName)) return ext;
            }
            return null;
        }
        
        // these are checked very frequently, so avoid overhead of HasCpeExt
        public bool hasCustomBlocks, hasBlockDefs, hasTextColors, 
        hasChangeModel, hasExtList, hasCP437, hasTwoWayPing, hasBulkBlockUpdate;

        void AddExtension(string extName, int version) {
            ExtEntry ext = FindExtension(extName.Trim());
            if (ext == null) return;
            ext.ClientExtVersion = (byte)version;
            
            if (ext.ExtName == CpeExt.CustomBlocks) {
                if (version == 1) Send(Packet.CustomBlockSupportLevel(1));
                hasCustomBlocks = true;
            } else if (ext.ExtName == CpeExt.ChangeModel) {
                hasChangeModel = true;
            } else if (ext.ExtName == CpeExt.FullCP437) {
                hasCP437 = true;
            } else if (ext.ExtName == CpeExt.ExtPlayerList) {
                hasExtList = true;
            } else if (ext.ExtName == CpeExt.BlockDefinitions) {
                hasBlockDefs = true;
            } else if (ext.ExtName == CpeExt.TextColors) {
                hasTextColors = true;
                for (int i = 0; i < Colors.List.Length; i++) {
                    if (!Colors.List[i].IsModified()) continue;
                    Send(Packet.SetTextColor(Colors.List[i]));
                }
            } else if (ext.ExtName == CpeExt.ExtEntityPositions) {
                hasExtPositions = true;
            } else if (ext.ExtName == CpeExt.TwoWayPing) {
                hasTwoWayPing = true;
            } else if (ext.ExtName == CpeExt.BulkBlockUpdate) {
                hasBulkBlockUpdate = true;
            }
        }

        /// <summary> Returns whether this player's client supports the given CPE extension. </summary>
        public bool Supports(string extName, int version = 1) {
            if (!hasCpe) return false;
            ExtEntry ext = FindExtension(extName);
            return ext != null && ext.ClientExtVersion == version;
        }
        
        string lastUrl = "";
        public void SendCurrentMapAppearance() {
            byte side = (byte)level.Config.EdgeBlock, edge = (byte)level.Config.HorizonBlock;
            if (!hasBlockDefs) side = level.RawFallback(side);
            if (!hasBlockDefs) edge = level.RawFallback(edge);
            
            if (Supports(CpeExt.EnvMapAspect)) {
                string url = GetTextureUrl();
                // reset all other textures back to client default.
                if (url != lastUrl) Send(Packet.EnvMapUrl("", hasCP437));
                Send(Packet.EnvMapUrl(url, hasCP437));
                
                Send(Packet.EnvMapProperty(EnvProp.SidesBlock, side));
                Send(Packet.EnvMapProperty(EnvProp.EdgeBlock, edge));               
                Send(Packet.EnvMapProperty(EnvProp.EdgeLevel, level.Config.EdgeLevel));
                Send(Packet.EnvMapProperty(EnvProp.SidesOffset, level.Config.SidesOffset));
                Send(Packet.EnvMapProperty(EnvProp.CloudsLevel, level.Config.CloudsHeight));
                
                Send(Packet.EnvMapProperty(EnvProp.MaxFog, level.Config.MaxFogDistance));
                Send(Packet.EnvMapProperty(EnvProp.CloudsSpeed, level.Config.CloudsSpeed));
                Send(Packet.EnvMapProperty(EnvProp.WeatherSpeed, level.Config.WeatherSpeed));
                Send(Packet.EnvMapProperty(EnvProp.WeatherFade, level.Config.WeatherFade));
                Send(Packet.EnvMapProperty(EnvProp.ExpFog, level.Config.ExpFog ? 1 : 0));
                Send(Packet.EnvMapProperty(EnvProp.SkyboxHorSpeed, level.Config.SkyboxHorSpeed));
                Send(Packet.EnvMapProperty(EnvProp.SkyboxVerSpeed, level.Config.SkyboxVerSpeed));
            } else if (Supports(CpeExt.EnvMapAppearance, 2)) {
                string url = GetTextureUrl();
                // reset all other textures back to client default.
                if (url != lastUrl) {
                    Send(Packet.MapAppearanceV2("", side, edge, level.Config.EdgeLevel,
                                                level.Config.CloudsHeight, level.Config.MaxFogDistance, hasCP437));
                }
                Send(Packet.MapAppearanceV2(url, side, edge, level.Config.EdgeLevel,
                                            level.Config.CloudsHeight, level.Config.MaxFogDistance, hasCP437));
                lastUrl = url;
            } else if (Supports(CpeExt.EnvMapAppearance)) {
                string url = level.Config.Terrain.Length == 0 ? ServerConfig.DefaultTerrain : level.Config.Terrain;
                Send(Packet.MapAppearance(url, side, edge, level.Config.EdgeLevel, hasCP437));
            }
        }
        
        public string GetTextureUrl() {
            string url = level.Config.TexturePack.Length == 0 ? level.Config.Terrain : level.Config.TexturePack;
            if (url.Length == 0)
                url = ServerConfig.DefaultTexture.Length == 0 ? ServerConfig.DefaultTerrain : ServerConfig.DefaultTexture;
            return url;
        }
        
        public void SendCurrentEnvColors() {
            SendEnvColor(0, level.Config.SkyColor);
            SendEnvColor(1, level.Config.CloudColor);
            SendEnvColor(2, level.Config.FogColor);
            SendEnvColor(3, level.Config.ShadowColor);
            SendEnvColor(4, level.Config.LightColor);
        }
        
        public void SendEnvColor(byte type, string hex) {
            if (String.IsNullOrEmpty(hex)) {
                Send(Packet.EnvColor(type, -1, -1, -1)); return;
            }
            
            try {
                ColorDesc c = Colors.ParseHex(hex);
                Send(Packet.EnvColor(type, c.R, c.G, c.B));
            } catch (ArgumentException) {
                Send(Packet.EnvColor(type, -1, -1, -1));
            }
        }
        
        public void SendCurrentBlockPermissions() {
            if (!Supports(CpeExt.BlockPermissions)) return;
            
            // Write the block permissions as one bulk TCP packet
            int count = NumBlockPermissions();
            byte[] bulk = new byte[4 * count];
            WriteBlockPermissions(bulk);
            Send(bulk);
        }
        
        int NumBlockPermissions() {
            int count = hasCustomBlocks ? Block.CpeCount : Block.OriginalCount;
            if (!hasBlockDefs) return count;

            return count + (Block.Count - Block.CpeCount);
        }
        
        void WriteBlockPermissions(byte[] bulk) {
            int coreCount = hasCustomBlocks ? Block.CpeCount : Block.OriginalCount;
            for (byte i = 0; i < coreCount; i++) {
                bool place = BlockPerms.UsableBy(this, i) && level.CanPlace;
                bool delete = BlockPerms.UsableBy(this, i) && level.CanDelete;
                Packet.WriteBlockPermission(i, place, delete, bulk, i * 4);
            }
            
            if (!hasBlockDefs) return;
            int j = coreCount * 4;
            
            for (int i = Block.CpeCount; i < Block.Count; i++) {
                Packet.WriteBlockPermission((byte)i, level.CanPlace, level.CanDelete, bulk, j);
                j += 4;
            }
        }
    }
    
    public static class CpeExt {
        public const string ClickDistance = "ClickDistance";
        public const string CustomBlocks = "CustomBlocks";
        public const string HeldBlock = "HeldBlock";
        public const string TextHotkey = "TextHotKey";
        public const string ExtPlayerList = "ExtPlayerList";
        public const string EnvColors = "EnvColors";
        public const string SelectionCuboid = "SelectionCuboid";
        public const string BlockPermissions = "BlockPermissions";
        public const string ChangeModel = "ChangeModel";
        public const string EnvMapAppearance = "EnvMapAppearance";
        public const string EnvWeatherType = "EnvWeatherType";
        public const string HackControl = "HackControl";
        public const string EmoteFix = "EmoteFix";
        public const string MessageTypes = "MessageTypes";
        public const string LongerMessages = "LongerMessages";
        public const string FullCP437 = "FullCP437";
        public const string BlockDefinitions = "BlockDefinitions";
        public const string BlockDefinitionsExt = "BlockDefinitionsExt";
        public const string TextColors = "TextColors";
        public const string BulkBlockUpdate = "BulkBlockUpdate";
        public const string EnvMapAspect = "EnvMapAspect";
        public const string PlayerClick = "PlayerClick";
        public const string EntityProperty = "EntityProperty";
        public const string ExtEntityPositions = "ExtEntityPositions";
        public const string TwoWayPing = "TwoWayPing";
        public const string InventoryOrder = "InventoryOrder";
        public const string InstantMOTD = "InstantMOTD";
    }
    
    public enum CpeMessageType : byte {
        Normal = 0, Status1 = 1, Status2 = 2, Status3 = 3,
        BottomRight1 = 11, BottomRight2 = 12, BottomRight3 = 13,
        Announcement = 100,
    }
    
    public enum EnvProp : byte {
        SidesBlock = 0, EdgeBlock = 1, EdgeLevel = 2,
        CloudsLevel = 3, MaxFog = 4, CloudsSpeed = 5,
        WeatherSpeed = 6, WeatherFade = 7, ExpFog = 8,
        SidesOffset = 9, SkyboxHorSpeed = 10, SkyboxVerSpeed = 11,
    }
    
    public enum EntityProp : byte {
        RotX = 0, RotY = 1, RotZ = 2, ScaleX = 3, ScaleY = 4, ScaleZ = 5,
    }
}