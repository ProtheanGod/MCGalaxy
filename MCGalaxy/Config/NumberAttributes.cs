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

namespace MCGalaxy.Config {
    
    public class ConfigIntAttribute : ConfigAttribute {
        
        /// <summary> Minimum integer allowed for a value. </summary>
        public int MinValue;
        
        /// <summary> Maximum value integer allowed for a value. </summary>
        public int MaxValue;
        
        public ConfigIntAttribute(string name, string section, int defValue,
                                  int min = int.MinValue, int max = int.MaxValue)
            : base(name, section, defValue) {
            MinValue = min; MaxValue = max;
        }
        
        public override object Parse(string value) {
            int integer;
            if (!int.TryParse(value, out integer)) {
                Logger.Log(LogType.Warning, "Config key \"{0}\" is not a valid integer, using default of {1}", Name, DefaultValue);
                return DefaultValue;
            }
            
            if (integer < MinValue) {
                Logger.Log(LogType.Warning, "Config key \"{0}\" is too small an integer, using {1}", Name, MinValue);
                return MinValue;
            }
            if (integer > MaxValue) {
                Logger.Log(LogType.Warning, "Config key \"{0}\" is too big an integer, using {1}", Name, MaxValue);
                return MaxValue;
            }
            return integer;
        }
    }   
    
    public class ConfigByteAttribute : ConfigIntAttribute {
        
        public ConfigByteAttribute(string name, string section, int defValue,
                                  int min = byte.MinValue, int max = byte.MaxValue)
            : base(name, section, defValue, min, max) {
        }
        
        public override object Parse(string value) {
            int intValue = (int)base.Parse(value);
            return (byte)intValue; // Can't directly unbox object to byte - must unbox to int, then cast to byte
        }
    }
    
    public class ConfigRealAttribute : ConfigAttribute {
        
        /// <summary> Minimum real number allowed for a value. </summary>
        public float MinValue;
        
        /// <summary> Maximum real number allowed for a value. </summary>
        public float MaxValue;
        
        public ConfigRealAttribute(string name, string section, float defValue, float min, float max)
            : base(name, section, defValue) {
            MinValue = min; MaxValue = max;
        }
        
        public override object Parse(string value) {
            float real;
            if (!Utils.TryParseDecimal(value, out real)) {
                Logger.Log(LogType.Warning, "Config key \"{0}\" is not a valid number, using default of {1}", Name, DefaultValue);
                return DefaultValue;
            }
            
            if (real < MinValue) {
                Logger.Log(LogType.Warning, "Config key \"{0}\" is too small a number, using {1}", Name, MinValue);
                return MinValue;
            }
            if (real > MaxValue) {
                Logger.Log(LogType.Warning, "Config key \"{0}\" is too big a number, using {1}", Name, MaxValue);
                return MaxValue;
            }
            return real;
        }
    }
}
