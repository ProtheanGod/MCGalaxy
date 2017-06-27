﻿/*
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
using MCGalaxy.Config;
using MCGalaxy.Games;

namespace MCGalaxy {
    public sealed class LevelConfig {

        [ConfigString("MOTD", "General", null, "ignore", true, null, 128)]
        public string motd = "ignore";
        [ConfigBool("LoadOnGoto", "General", null, true)]
        public bool loadOnGoto = true;
        [ConfigString("Theme", "General", null, "Normal", true)]
        public string theme = "Normal";
        [ConfigBool("Unload", "General", null, true)]
        public bool unload = true;
        /// <summary> true if this map may see server-wide chat, false if this map has level-only/isolated chat </summary>
        [ConfigBool("WorldChat", "General", null, true)]
        public bool worldChat = true;

        [ConfigBool("UseBlockDB", "Other", null, true)]
        public bool UseBlockDB = true;
        [ConfigInt("LoadDelay", "Other", null, 0, 0, 2000)]
        public int LoadDelay = 0;
        
        public byte jailrotx, jailroty;
        [ConfigInt("JailX", "Jail", null, 0, 0, 65535)]
        public int jailx;
        [ConfigInt("JailY", "Jail", null, 0, 0, 65535)]
        public int jaily;
        [ConfigInt("JailZ", "Jail", null, 0, 0, 65535)]
        public int jailz;       

        // Environment settings
        [ConfigInt("Weather", "Env", null, 0, 0, 2)]
        public int Weather;       
        [ConfigString("Texture", "Env", null, "", true, null, NetUtils.StringSize)]
        public string terrainUrl = "";
        [ConfigString("TexturePack", "Env", null, "", true, null, NetUtils.StringSize)]
        public string texturePackUrl = "";
        /// <summary> Color of the clouds (RGB packed into an int). Set to -1 to use client defaults. </summary>
        [ConfigString("CloudColor", "Env", null, "", true)]
        public string CloudColor = "";
        /// <summary> Color of the fog (RGB packed into an int). Set to -1 to use client defaults. </summary>
        [ConfigString("FogColor", "Env", null, "", true)]
        public string FogColor = "";
        /// <summary> Color of the sky (RGB packed into an int). Set to -1 to use client defaults. </summary>
        [ConfigString("SkyColor", "Env", null, "", true)]
        public string SkyColor = "";
        /// <summary> Color of the blocks in shadows (RGB packed into an int). Set to -1 to use client defaults. </summary>
        [ConfigString("ShadowColor", "Env", null, "", true)]
        public string ShadowColor = "";
        /// <summary> Color of the blocks in the light (RGB packed into an int). Set to -1 to use client defaults. </summary>
        [ConfigString("LightColor", "Env", null, "", true)]
        public string LightColor = "";
        
        /// <summary> Elevation of the "ocean" that surrounds maps. Default is map height / 2. </summary>
        [ConfigInt("EdgeLevel", "Env", null, -1, short.MinValue, short.MaxValue)]
        public int EdgeLevel;
        /// <summary> Offset of the "bedrock" that surrounds map sides from edge level. Default is -2. </summary>
        [ConfigInt("SidesOffset", "Env", null, -2, short.MinValue, short.MaxValue)]
        public int SidesOffset = -2;
        /// <summary> Elevation of the clouds. Default is map height + 2. </summary>
        [ConfigInt("CloudsHeight", "Env", null, -1, short.MinValue, short.MaxValue)]
        public int CloudsHeight;
        
        /// <summary> Max fog distance the client can see. Default is 0, means use client-side defined max fog distance. </summary>
        [ConfigInt("MaxFog", "Env", null, 0, short.MinValue, short.MaxValue)]
        public int MaxFogDistance;
        /// <summary> Clouds speed, in units of 256ths. Default is 256 (1 speed). </summary>
        [ConfigInt("clouds-speed", "Env", null, 256, short.MinValue, short.MaxValue)]
        public int CloudsSpeed = 256;
        /// <summary> Weather speed, in units of 256ths. Default is 256 (1 speed). </summary>
        [ConfigInt("weather-speed", "Env", null, 256, short.MinValue, short.MaxValue)]
        public int WeatherSpeed = 256;
        /// <summary> Weather fade, in units of 256ths. Default is 256 (1 speed). </summary>
        [ConfigInt("weather-fade", "Env", null, 128, short.MinValue, short.MaxValue)]
        public int WeatherFade = 128;
        /// <summary> The block which will be displayed on the horizon. </summary>
        [ConfigInt("HorizonBlock", "Env", null, Block.water, 0, 255)]
        public int HorizonBlock = Block.water;
        /// <summary> The block which will be displayed on the edge of the map. </summary>
        [ConfigInt("EdgeBlock", "Env", null, Block.blackrock, 0, 255)]
        public int EdgeBlock = Block.blackrock;
         /// <summary> Whether exponential fog mode is used client-side. </summary>
        [ConfigBool("ExpFog", "Env", null, false)]
        public bool ExpFog;
        
        // Permission settings
        [ConfigString("RealmOwner", "Permissions", null, "", true)]
        public string RealmOwner = "";
        [ConfigBool("Buildable", "Permissions", null, true)]
        public bool Buildable = true;
        [ConfigBool("Deletable", "Permissions", null, true)]
        public bool Deletable = true;
        
        [ConfigPerm("PerBuildMax", "Permissions", null, LevelPermission.Nobody)]
        public LevelPermission perbuildmax = LevelPermission.Nobody;
        [ConfigPerm("PerBuild", "Permissions", null, LevelPermission.Guest)]
        public LevelPermission permissionbuild = LevelPermission.Guest;
        // What ranks can go to this map (excludes banned)
        [ConfigPerm("PerVisit", "Permissions", null, LevelPermission.Guest)]
        public LevelPermission permissionvisit = LevelPermission.Guest;
        [ConfigPerm("PerVisitMax", "Permissions", null, LevelPermission.Nobody)]
        public LevelPermission pervisitmax = LevelPermission.Nobody;       
        // Other blacklists/whitelists
        [ConfigStringList("VisitWhitelist", "Permissions", null)]
        public List<string> VisitWhitelist = new List<string>();
        [ConfigStringList("VisitBlacklist", "Permissions", null)]
        public List<string> VisitBlacklist = new List<string>();
        [ConfigStringList("BuildWhitelist", "Permissions", null)]
        public List<string> BuildWhitelist = new List<string>();
        [ConfigStringList("BuildBlacklist", "Permissions", null)]
        public List<string> BuildBlacklist = new List<string>();

        // Physics settings
        [ConfigInt("Physics overload", "Physics", null, 250)]
        public int overload = 1500;
        [ConfigBool("RandomFlow", "Physics", null, true)]
        public bool randomFlow = true;
        [ConfigInt("Physics speed", "Physics", null, 250)]
        public int speedPhysics = 250;
        [ConfigBool("LeafDecay", "Physics", null, false)]
        public bool leafDecay;
        [ConfigBool("Finite mode", "Physics", null, false)]
        public bool finite;
        [ConfigBool("GrowTrees", "Physics", null, false)]
        public bool growTrees;
        [ConfigBool("Animal AI", "Physics", null, true)]
        public bool ai = true;
        [ConfigBool("GrassGrowth", "Physics", null, true)]
        public bool GrassGrow = true;
        [ConfigString("TreeType", "Physics", null, "fern", false)]
        public string TreeType = "fern";
        
        // Survival settings
        [ConfigInt("Drown", "Survival", null, 70)]
        public int drown = 70;
        [ConfigBool("Edge water", "Survival", null, true)]
        public bool edgeWater;
        [ConfigInt("Fall", "Survival", null, 9)]
        public int fall = 9;
        [ConfigBool("Guns", "Survival", null, false)]
        public bool guns = false;
        [ConfigBool("Survival death", "Survival", null, false)]
        public bool Death;
        [ConfigBool("Killer blocks", "Survival", null, true)]
        public bool Killer = true;       
        
        // Games settings
        public bool ctfmode;
        [ConfigInt("Likes", "Game", null, 0)]
        public int Likes;
        [ConfigInt("Dislikes", "Game", null, 0)]
        public int Dislikes;
        [ConfigString("Authors", "Game", null, "", true)]
        public string Authors = "";
        [ConfigBool("Pillaring", "Game", null, false)]
        public bool Pillaring = !ZombieGameProps.NoPillaring;
        
        [ConfigEnum("BuildType", "Game", null, BuildType.Normal, typeof(BuildType))]
        public BuildType BuildType = BuildType.Normal;
        
        [ConfigInt("MinRoundTime", "Game", null, 4)]
        public int MinRoundTime = 4;
        [ConfigInt("MaxRoundTime", "Game", null, 7)]
        public int MaxRoundTime = 7;
        [ConfigBool("DrawingAllowed", "Game", null, true)]
        public bool DrawingAllowed = true;
        [ConfigInt("RoundsPlayed", "Game", null, 0)]
        public int RoundsPlayed = 0;
        [ConfigInt("RoundsHumanWon", "Game", null, 0)]
        public int RoundsHumanWon = 0;
    }
}