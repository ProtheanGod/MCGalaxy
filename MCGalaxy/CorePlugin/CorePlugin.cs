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
using MCGalaxy.Events;
using MCGalaxy.Events.EconomyEvents;
using MCGalaxy.Events.GroupEvents;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Tasks;

namespace MCGalaxy.Core {

    public sealed class CorePlugin : Plugin_Simple {
        public override string creator { get { return Server.SoftwareName + " team"; } }
        public override string MCGalaxy_Version { get { return Server.VersionString; } }
        public override string name { get { return "CorePlugin"; } }
        SchedulerTask clearTask;

        public override void Load(bool startup) {
            OnPlayerConnectEvent.Register(ConnectHandler.HandleConnect, Priority.Critical);
            OnPlayerCommandEvent.Register(ChatHandler.HandleCommand, Priority.Critical);
            OnPlayerConnectingEvent.Register(ConnectingHandler.HandleConnecting, Priority.Critical);
            
            OnJoinedLevelEvent.Register(MiscHandlers.HandleOnJoinedLevel, Priority.Critical);
            OnPlayerMoveEvent.Register(MiscHandlers.HandlePlayerMove, Priority.Critical);
            OnPlayerClickEvent.Register(MiscHandlers.HandlePlayerClick, Priority.Critical);
            
            OnEcoTransactionEvent.Register(EcoHandlers.HandleEcoTransaction, Priority.Critical);
            OnModActionEvent.Register(ModActionHandler.HandleModAction, Priority.Critical);
            OnGroupLoadEvent.Register(MiscHandlers.HandleGroupLoad, Priority.Critical);
            
            clearTask = Server.Background.QueueRepeat(IPThrottler.CleanupTask, null, 
                                                      TimeSpan.FromMinutes(10));
        }
        
        public override void Unload(bool shutdown) {
            OnPlayerConnectEvent.Unregister(ConnectHandler.HandleConnect);
            OnPlayerCommandEvent.Unregister(ChatHandler.HandleCommand);
            OnPlayerConnectingEvent.Unregister(ConnectingHandler.HandleConnecting);
            
            OnJoinedLevelEvent.Unregister(MiscHandlers.HandleOnJoinedLevel);
            OnPlayerMoveEvent.Unregister(MiscHandlers.HandlePlayerMove);
            OnPlayerClickEvent.Unregister(MiscHandlers.HandlePlayerClick);
            
            OnEcoTransactionEvent.Unregister(EcoHandlers.HandleEcoTransaction);
            OnModActionEvent.Unregister(ModActionHandler.HandleModAction);
            OnGroupLoadEvent.Unregister(MiscHandlers.HandleGroupLoad);
            
            Server.Background.Cancel(clearTask);
        }
    }
}
