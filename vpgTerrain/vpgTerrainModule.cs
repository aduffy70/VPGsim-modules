/*
 * Copyright (c) Contributors http://github.com/aduffy70/vpgTerrain
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the vpgTerrain Module nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Xml;
using System.Collections.Generic;

using log4net;
using Nini.Config;
using OpenMetaverse;

using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

using Mono.Addins;

[assembly: Addin("vpgTerrainModule", "0.1")]
[assembly: AddinDependency("OpenSim", "0.5")]
namespace vpgTerrainModule
{
    [Extension(Path="/OpenSim/RegionModules",NodeName="RegionModule")]
    public class vpgTerrainModule : INonSharedRegionModule
    {
        //Set up logging and dialog messages
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        IDialogModule m_dialogmod;

        //Configurable settings (in vpgTerrain.ini only)
        bool m_enabled = false;
        int m_channel = 18;

        int m_terrainMap;
        //TODO: These map values are being read from the webform but we aren't using them for anything.  Implementing this will require changes to the Soil Module and its interface.
        int m_salinityMap = 0;
        int m_drainageMap = 0;
        int m_fertilityMap = 0;

        Scene m_scene;

        #region INonSharedRegionModule interface

        public void Initialise(IConfigSource config)
        {
            IConfig vpgTerrainConfig = config.Configs["vpgTerrain"];
            if (vpgTerrainConfig != null)
            {
                m_enabled = vpgTerrainConfig.GetBoolean("enabled", false);
                m_channel = vpgTerrainConfig.GetInt("chat_channel", 18);
            }
            if (m_enabled)
            {
                m_log.Info("[vpgTerrainModule] Initializing...");
            }
        }

        public void AddRegion(Scene scene)
        {
            if (m_enabled)
            {
                m_scene = scene;
                m_dialogmod = m_scene.RequestModuleInterface<IDialogModule>();
                m_scene.EventManager.OnChatFromWorld += new EventManager.ChatFromWorldEvent(OnChat);
                m_scene.EventManager.OnChatFromClient += new EventManager.ChatFromClientEvent(OnChat);
            }
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get
            {
                return "vpgTerrainModule";
            }
        }

        public Type ReplaceableInterface
        {
            get
            {
                return null;
            }
        }

        #endregion

        void OnChat(Object sender, OSChatMessage chat)
        {
            if (chat.Channel != m_channel)
            {
                //Message is not for this module
                return;
            }
            else if (chat.Message.ToLower() == "test")
            {
                //DEBUG: Just a place to plug in temporary test code.
            }
            else
            {
                int terrainNumber;
                try
                {
                    terrainNumber = Int32.Parse(chat.Message);
                }
                catch
                {
                    terrainNumber = 0;
                }
                if (terrainNumber != 0)
                {
                    LoadTerrain(terrainNumber);
                }
                else
                {
                    //Invalid command
                    Alert("Invalid command...");
                }
            }
        }

        public void Alert(string message)
        {
            if (m_dialogmod != null)
            {
                m_dialogmod.SendGeneralAlert(String.Format("{0}: {1}", Name, message));
            }
        }

        public void Log(string message)
        {
            m_log.DebugFormat("[{0}] {1}", Name, message);
        }

        void LoadTerrain(int terrain)
        {
            //Load one of the numbered terrains from file
            ITerrainModule terrainmod = m_scene.RequestModuleInterface<ITerrainModule>();
            try
            {
                terrainmod.LoadFromFile(String.Format("terrain/Terrain{0}.png", terrain));
                Alert("Loading terrain.  Please wait...");
                //Ugly hack to solve problem of clients not consistently receiving terrain updates.
                //Pause and send it a second time.
                Thread.Sleep(3000);
                terrainmod.LoadFromFile(String.Format("terrain/Terrain{0}.png", terrain));
                Alert("Loaded terrain...");
            }
            catch
            {
                Alert("Error - Requested file not found!");
            }
        }
    }
}
