/*
 * Copyright (c) Contributors, VPGsim Project http://fernseed.usu.edu/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the VPGsim Project nor the
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
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace vpgSoilModule
{
    public class vpgSoilModule : IvpgSoilModule
    {
		private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        bool m_enabled = true;
        bool m_localFiles = true;
		string m_soilXPath = "addon-modules/vpgsim/soil/DefaultSoilX.txt";
        string m_soilYPath = "addon-modules/vpgsim/soil/DefaultSoilY.txt";
        string m_soilZPath = "addon-modules/vpgsim/soil/DefaultSoilZ.txt";
		private Vector3[] m_soilType = new Vector3[256 * 256];

        public void Initialise(Scene scene, IConfigSource config)
        {
            IConfig vpgSoilConfig = config.Configs["vpgSoil"];
			if (vpgSoilConfig != null)
			{
				m_enabled = vpgSoilConfig.GetBoolean("enabled", true);
                m_localFiles = vpgSoilConfig.GetBoolean("local_files", true);
				m_soilXPath = vpgSoilConfig.GetString("soil_x_path", "addon-modules/vpgsim/soil/DefaultSoilX.txt");
                m_soilYPath = vpgSoilConfig.GetString("soil_y_path", "addon-modules/vpgsim/soil/DefaultSoilY.txt");
                m_soilZPath = vpgSoilConfig.GetString("soil_z_path", "addon-modules/vpgsim/soil/DefaultSoilZ.txt");
			}
			if (m_enabled)
			{
				scene.RegisterModuleInterface<IvpgSoilModule>(this);
				GenerateSoilType();
			}
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "vpgSoilModule"; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        public Vector3 SoilType(int x, int y, int z)
        {
            Vector3 type = new Vector3(0f, 0f, 0f);
            if (x < 0) x = 0;
            if (x > 255) x = 255;
            if (y < 0) y = 0;
            if (y > 255) y = 255;

            if (m_soilType != null)
            {
                type = m_soilType[y * 256 + x];
            }

            return type;
        }

        /// <summary>
        /// Calculate the soil type across the region.
        /// </summary>
        private void GenerateSoilType()
        {
			string[] soilX = new string[256*256];
			string[] soilY = new string[256*256];
			string[] soilZ = new string[256*256];
            if (m_localFiles) //Read from a local file
            {
                if (System.IO.File.Exists(m_soilXPath))
			    {
                    soilX = System.IO.File.ReadAllLines(m_soilXPath);
				    m_log.Info("[vpgSoil] Loaded Soil.X values from " + m_soilXPath);
                }
			    else
			    {
				    m_log.Info("[vpgSoil] " + m_soilXPath + " not found.  Loaded default values for Soil.X");
				    for(int index=0; index<256*256; index++)
				    {
                        soilX[index] = "0.5"; //If no soil values are available, use 0.5
				    }
			    }
			    if (System.IO.File.Exists(m_soilYPath))
			    {
                    soilY = System.IO.File.ReadAllLines(m_soilYPath);
				    m_log.Info("[vpgSoil] Loaded Soil.Y values from " + m_soilYPath);
                }
			    else
			    {
				    m_log.Info("[vpgSoil] " + m_soilYPath + " not found.  Loaded default values for Soil.Y");
                    for(int index=0; index<256*256; index++)
                    {
                        soilY[index] = "0.5"; //If no soil values are available, use 0.5
                    }
			    }
			    if (System.IO.File.Exists(m_soilZPath))
			    {
                    soilZ = System.IO.File.ReadAllLines(m_soilZPath);
				    m_log.Info("[vpgSoil] Loaded Soil.Z values from " + m_soilZPath);
			    }
			    else
                {
                    m_log.Info("[vpgSoil] " + m_soilZPath + " not found.  Loaded default values for Soil.Z");
                    for(int index=0; index<256*256; index++)
                    {
                        soilZ[index] = "0.5"; //If no soil values are available, use 0.5
                    }
			    }
            }
            else //read from a url
            {
                WebRequest soilUrl = WebRequest.Create(m_soilXPath);
                try
                {
                    StreamReader urlData = new StreamReader(soilUrl.GetResponse().GetResponseStream());
                    int lineCounter = 0;
                    string line;
                    while ((line = urlData.ReadLine()) != null)
                    {
                        soilX[lineCounter] = line;
                        lineCounter++;
                    }
                    m_log.Info("[vpgSoil] Loaded Soil.X values from " + m_soilXPath);
                }
                catch //failed to get the data for some reason
                {
                    m_log.Info("[vpgSoil] " + m_soilXPath + " not found.  Loaded default values for Soil.X");
				    for(int index=0; index<256*256; index++)
				    {
                        soilX[index] = "0.5"; //If no soil values are available, use 0.5
				    }
                }
                soilUrl = WebRequest.Create(m_soilYPath);
                try
                {
                    StreamReader urlData = new StreamReader(soilUrl.GetResponse().GetResponseStream());
                    int lineCounter = 0;
                    string line;
                    while ((line = urlData.ReadLine()) != null)
                    {
                        soilY[lineCounter] = line;
                        lineCounter++;
                    }
                    m_log.Info("[vpgSoil] Loaded Soil.Y values from " + m_soilYPath);
                }
                catch //failed to get the data for some reason
                {
                    m_log.Info("[vpgSoil] " + m_soilYPath + " not found.  Loaded default values for Soil.Y");
				    for(int index=0; index<256*256; index++)
				    {
                        soilY[index] = "0.5"; //If no soil values are available, use 0.5
				    }
                }
                soilUrl = WebRequest.Create(m_soilZPath);
                try
                {
                    StreamReader urlData = new StreamReader(soilUrl.GetResponse().GetResponseStream());
                    int lineCounter = 0;
                    string line;
                    while ((line = urlData.ReadLine()) != null)
                    {
                        soilZ[lineCounter] = line;
                        lineCounter++;
                    }
                    m_log.Info("[vpgSoil] Loaded Soil.Z values from " + m_soilZPath);
                }
                catch //failed to get the data for some reason
                {
                    m_log.Info("[vpgSoil] " + m_soilZPath + " not found.  Loaded default values for Soil.Z");
				    for(int index=0; index<256*256; index++)
				    {
                        soilZ[index] = "0.5"; //If no soil values are available, use 0.5
				    }
                }
            }
			for (int y = 0; y < 256; y++)
            {
                for (int x = 0; x < 256; x++)
                {
                    int index = y * 256 + x;
					m_soilType[index] = new Vector3(Convert.ToSingle(soilX[index]),
Convert.ToSingle(soilY[index]), Convert.ToSingle(soilZ[index]));
                }
            }
        }
    }
}
