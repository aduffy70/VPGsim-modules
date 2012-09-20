/*
 * Copyright (c) Contributors, VPGsim Project http://fernseed.usu.edu
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
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

using OpenSim.Region.ScriptEngine.Shared;
using LSL_List = OpenSim.Region.ScriptEngine.Shared.LSL_Types.list;


namespace vpgParametersModule
{
    public class vpgParametersModule : IvpgParametersModule
    {
        private List<Scene> m_scenes = new List<Scene>();
        bool m_enabled = true;
        bool m_localFiles = true;
        string m_parameterPath = "addon-modules/vpgsim/parameters/"; //Path to the folder where parameter files are stored
        string m_currentFile = "addon-modules/vpgsim/parameters/current"; //File where current parameters are stored so they can be reloaded after a region restart
        int m_listenChannel = 15;
        bool m_successfullyLoaded = false;

        //The big parameter list:
        string versionID; //same for all lifestages
        float cycleTime; //same for all lifestages
        float[] gametophyteNeighborhood = new float[3];
        float spermNeighborhood; //gametophyte only
        float[] sporophyteNeighborhood = new float[3];
        float[] sporophyteWeight = new float[3];
        float[] neighborShape = new float[3];
        float[] altitudeLocus = new float[3];
        float[] altitudeOpt = new float[3];
        float[] altitudeShape = new float[3];
        float[] altitudeRecOpt = new float[3];
        float[] altitudeRecShape = new float[3];
        float[] salinityLocus = new float[3];
        float[] salinityOpt = new float[3];
        float[] salinityShape = new float[3];
        float[] salinityRecOpt = new float[3];
        float[] salinityRecShape = new float[3];
        float[] drainageLocus = new float[3];
        float[] drainageOpt = new float[3];
        float[] drainageShape = new float[3];
        float[] drainageRecOpt = new float[3];
        float[] drainageRecShape = new float[3];
        float[] fertilityLocus = new float[3];
        float[] fertilityOpt = new float[3];
        float[] fertilityShape = new float[3];
        float[] fertilityRecOpt = new float[3];
        float[] fertilityRecShape = new float[3];
        float[] lifespanLocus = new float[3];
        float[] lifespan = new float[3];
        float[] lifespanRec = new float[3];
        float[] rainLocus = new float[2]; //spore and gametophyte only
        float[] rain = new float[2]; //spore and gametophyte only
        float[] rainRec = new float[2]; //spore and gametophyte only
        float advantageLocus; //sporophyte only
        float advantage; //sporophyte only
        float advantageRec; //sporophyte only
        float distanceLocus; //sporophyte only
        float distanceMax; //sporophyte only
        float distanceShape; //sporophyte only
        float distanceRecMax; //sporophyte only
        float distanceRecShape; //sporophyte only

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void Initialise(Scene scene, IConfigSource config)
        {
            IConfig vpgParametersConfig = config.Configs["vpgParameters"];
            if (vpgParametersConfig != null)
            {
                m_enabled = vpgParametersConfig.GetBoolean("enabled", true);
                m_localFiles = vpgParametersConfig.GetBoolean("local_parameters", true);
                m_parameterPath = vpgParametersConfig.GetString("parameter_path", "addon-modules/vpgsim/parameters/");
                m_currentFile = vpgParametersConfig.GetString("current_file", "addon-modules/vpgsim/parameters/current");
                m_listenChannel = vpgParametersConfig.GetInt("listen_channel", 15);
            }
            if (m_enabled)
            {
                lock (m_scenes)
                {
                    if (!m_scenes.Contains(scene))
                    {
                        m_scenes.Add(scene);
                        m_log.WarnFormat("[vpgParameters]: Initialized region {0}", scene.RegionInfo.RegionName);
                    }
                }
            }
        }

        public void PostInitialise()
        {
            if (m_enabled)
            {
                lock (m_scenes)
                {
                    foreach (Scene scene in m_scenes)
                    {
                        scene.RegisterModuleInterface<IvpgParametersModule>(this);
                        scene.EventManager.OnChatFromClient += OnChat;
                        scene.EventManager.OnChatFromWorld += OnChat;
                        m_log.WarnFormat("[vpgParameters]: PostInitialized region {0}", scene.RegionInfo.RegionName);
                    }
                }
                m_log.Info("[vpgParameters] Trying to load last used parameters");
                ReadParameters(m_currentFile, true);
            }
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "vpgParametersModule"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }

        private void OnChat(Object sender, OSChatMessage chat)
        {
            if (chat.Channel != m_listenChannel)
                return;
            else if (chat.Message.Length > 0)
            {
                m_log.Info("[vpgParameters] Loading: " + chat.Message);
                ReadParameters(System.IO.Path.Combine(m_parameterPath, chat.Message), m_localFiles);
            }
        }

        private void ReadParameters(string fileName, bool local)
        {
            string[] parametersFromFile = new string[98];
            if (local) //Read from a local file
            {
                try
                {
                    if (System.IO.File.Exists(fileName))
                    {
                        parametersFromFile = System.IO.File.ReadAllLines(fileName);
                        SaveParametersToVariables(parametersFromFile);
                        m_log.Info("[vpgParameters] Loaded parameters from file \"" + fileName + "\"...");
                        if (fileName != m_currentFile)
                        {
                            System.IO.File.Copy(fileName, m_currentFile, true);
                        }
                        foreach (Scene scene in m_scenes)
                        {
                            IDialogModule dialogmod = scene.RequestModuleInterface<IDialogModule>();
                            if (dialogmod != null)
                            {
                                dialogmod.SendGeneralAlert("Parameters Module: Loaded new parameters...");
                            }
                        }
                        m_successfullyLoaded = true;
                    }
                    else
                    {
                        m_log.Error("[vpgParameters] File \"" + fileName + "\" does not exist...");
                        foreach (Scene scene in m_scenes)
                        {
                            IDialogModule dialogmod = scene.RequestModuleInterface<IDialogModule>();
                            if (dialogmod != null)
                            {
                                dialogmod.SendGeneralAlert("Parameters Module: Error loading parameters...");
                            }
                        }
                        if (!m_successfullyLoaded)
                        {
                            LoadDefaultParameters();
                        }
                    }
                }
                catch
                {
                    m_log.Error("[vpgParameters] Error loading parameters from file \"" + fileName + "\"...");
                    foreach (Scene scene in m_scenes)
                    {
                        IDialogModule dialogmod = scene.RequestModuleInterface<IDialogModule>();
                        if (dialogmod != null)
                        {
                            dialogmod.SendGeneralAlert("Parameters Module: Error loading parameters...");
                        }
                    }
                    if (!m_successfullyLoaded)
                    {
                        LoadDefaultParameters();
                    }
                }
            }
            else //Read from a url
            {
                WebRequest parameterUrl = WebRequest.Create(fileName);
                try
                {
                    StreamReader urlData = new StreamReader(parameterUrl.GetResponse().GetResponseStream());
                    int lineCounter = 0;
                    string line;
                    while ((line = urlData.ReadLine()) != null)
                    {
                        parametersFromFile[lineCounter] = line;
                        lineCounter++;
                    }
                    SaveParametersToVariables(parametersFromFile);
                    m_log.Info("[vpgParameters] Loaded parameters from url \"" + fileName + "\"...");
                    foreach (Scene scene in m_scenes)
                    {
                        IDialogModule dialogmod = scene.RequestModuleInterface<IDialogModule>();
                        if (dialogmod != null)
                        {
                            dialogmod.SendGeneralAlert("Parameters Module: Loaded new parameters...");
                        }
                    }
                    WriteParametersToLocalCurrentFile(parametersFromFile);
                    m_successfullyLoaded = true;
                }
                catch //failed to get the data for some reason
                {
                    m_log.Error("[vpgParameters] Error loading parameters from url \"" + fileName + "\"...");
                    foreach (Scene scene in m_scenes)
                    {
                        IDialogModule dialogmod = scene.RequestModuleInterface<IDialogModule>();
                        if (dialogmod != null)
                        {
                            dialogmod.SendGeneralAlert("Parameters Module: Error loading parameters...");
                        }
                    }
                    if (!m_successfullyLoaded)
                    {
                        LoadDefaultParameters();
                    }
                }
            }
        }

        private void LoadDefaultParameters() //Just in case there is no current parameters file available at startup
        {
            string[] defaultParameters = new string[] {"0", "300", "0.0", "2.0", "0.0", "6.0", "0.0", "6.0", "6.0", "0.0", "2.0", "1.0", "0.0", "1.0", "1.0", "0", "0", "0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0", "0", "0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0", "0", "0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0", "0", "0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0", "0", "0", "10", "10", "10", "0", "0", "0", "0", "0", "0.9", "0.9", "0.0", "0.0", "0", "0", "0", "0", "75.0", "5.71", "0.0", "0.0"};
            m_log.Error("[vpgParameters] Loaded default parameters...");
            SaveParametersToVariables(defaultParameters);
            //Try to write the default parameters to the current parameters file so they will be available at startup next time.
            WriteParametersToLocalCurrentFile(defaultParameters);
        }

        private void WriteParametersToLocalCurrentFile(string[] parameters)
        {
            try
            {
                System.IO.StreamWriter outputStream;
                outputStream = System.IO.File.CreateText(m_currentFile);
                foreach(string parameter in parameters)
                {
                    outputStream.WriteLine(parameter);
                }
                outputStream.Close();
            }
            catch //failed to write for some reason
            {
                m_log.Error("[vpgParameters] Error writing to \"" + m_currentFile + "\".  Parameters will not be persistent over region restarts..");
                foreach (Scene scene in m_scenes)
                {
                    IDialogModule dialogmod = scene.RequestModuleInterface<IDialogModule>();
                    if (dialogmod != null)
                    {
                        dialogmod.SendGeneralAlert("Parameters Module: Error writing parameters to disk.  Parameters will not persist over region restarts...");
                    }
                }
            }
        }

        private void SaveParametersToVariables(string[] parameters)
        {
            versionID = parameters[0];
            cycleTime = float.Parse(parameters[1]);
            gametophyteNeighborhood = new float[3] {float.Parse(parameters[2]), float.Parse(parameters[3]), float.Parse(parameters[4])};
            spermNeighborhood = float.Parse(parameters[5]);
            sporophyteNeighborhood = new float[3] {float.Parse(parameters[6]), float.Parse(parameters[7]), float.Parse(parameters[8])};
            sporophyteWeight = new float[3] {float.Parse(parameters[9]), float.Parse(parameters[10]), float.Parse(parameters[11])};
            neighborShape = new float[3] {float.Parse(parameters[12]), float.Parse(parameters[13]), float.Parse(parameters[14])};
            altitudeLocus = new float[3] {float.Parse(parameters[15]), float.Parse(parameters[16]), float.Parse(parameters[17])};
            altitudeOpt = new float[3] {float.Parse(parameters[18]), float.Parse(parameters[19]), float.Parse(parameters[20])};
            altitudeShape = new float[3] {float.Parse(parameters[21]), float.Parse(parameters[22]), float.Parse(parameters[23])};
            altitudeRecOpt = new float[3] {float.Parse(parameters[24]), float.Parse(parameters[25]), float.Parse(parameters[26])};
            altitudeRecShape = new float[3] {float.Parse(parameters[27]), float.Parse(parameters[28]), float.Parse(parameters[29])};
            salinityLocus = new float[3] {float.Parse(parameters[30]), float.Parse(parameters[31]), float.Parse(parameters[32])};
            salinityOpt = new float[3] {float.Parse(parameters[33]), float.Parse(parameters[34]), float.Parse(parameters[35])};
            salinityShape = new float[3] {float.Parse(parameters[36]), float.Parse(parameters[37]), float.Parse(parameters[38])};
            salinityRecOpt = new float[3] {float.Parse(parameters[39]), float.Parse(parameters[40]), float.Parse(parameters[41])};
            salinityRecShape = new float[3] {float.Parse(parameters[42]), float.Parse(parameters[43]), float.Parse(parameters[44])};
            drainageLocus = new float[3] {float.Parse(parameters[45]), float.Parse(parameters[46]), float.Parse(parameters[47])};
            drainageOpt = new float[3] {float.Parse(parameters[48]), float.Parse(parameters[49]), float.Parse(parameters[50])};
            drainageShape = new float[3] {float.Parse(parameters[51]), float.Parse(parameters[52]), float.Parse(parameters[53])};
            drainageRecOpt = new float[3] {float.Parse(parameters[54]), float.Parse(parameters[55]), float.Parse(parameters[56])};
            drainageRecShape = new float[3] {float.Parse(parameters[57]), float.Parse(parameters[58]), float.Parse(parameters[59])};
            fertilityLocus = new float[3] {float.Parse(parameters[60]), float.Parse(parameters[61]), float.Parse(parameters[62])};
            fertilityOpt = new float[3] {float.Parse(parameters[63]), float.Parse(parameters[64]), float.Parse(parameters[65])};
            fertilityShape = new float[3] {float.Parse(parameters[66]), float.Parse(parameters[67]), float.Parse(parameters[68])};
            fertilityRecOpt = new float[3] {float.Parse(parameters[69]), float.Parse(parameters[70]), float.Parse(parameters[71])};
            fertilityRecShape = new float[3] {float.Parse(parameters[72]), float.Parse(parameters[73]), float.Parse(parameters[74])};
            lifespanLocus = new float[3] {float.Parse(parameters[75]), float.Parse(parameters[76]), float.Parse(parameters[77])};
            lifespan = new float[3] {float.Parse(parameters[78]), float.Parse(parameters[79]), float.Parse(parameters[80])};
            lifespanRec = new float[3] {float.Parse(parameters[81]), float.Parse(parameters[82]), float.Parse(parameters[83])};
            rainLocus = new float[2] {float.Parse(parameters[84]), float.Parse(parameters[85])};
            rain = new float[2] {float.Parse(parameters[86]), float.Parse(parameters[87])};
            rainRec = new float[2] {float.Parse(parameters[88]), float.Parse(parameters[89])};
            advantageLocus = float.Parse(parameters[90]);
            advantage = float.Parse(parameters[91]);
            advantageRec = float.Parse(parameters[92]);
            distanceLocus = float.Parse(parameters[93]);
            distanceMax = float.Parse(parameters[94]);
            distanceShape = float.Parse(parameters[95]);
            distanceRecMax = float.Parse(parameters[96]);
            distanceRecShape = float.Parse(parameters[97]);
            DisplayAllParameters();
        }

        public string GetVersionID()
        {
            return versionID;
        }

        public List<float> GetParameterList(string lifestage, int phenotype)
        {
            List<float> parameters = new List<float>();
            int lifestageIndex;
            if (lifestage == "Spore")
            {
                lifestageIndex = 0;
            }
            else if (lifestage == "Gametophyte")
            {
                lifestageIndex = 1;
            }
            else
            {
                lifestageIndex = 2;
            }
            parameters.Add(cycleTime);
            parameters.Add(gametophyteNeighborhood[lifestageIndex]);
            parameters.Add(sporophyteNeighborhood[lifestageIndex]);
            parameters.Add(sporophyteWeight[lifestageIndex]);
            parameters.Add(neighborShape[lifestageIndex]);
            parameters.Add(CalculateGeneExpression(phenotype, altitudeLocus[lifestageIndex], altitudeOpt[lifestageIndex], altitudeRecOpt[lifestageIndex]));
            parameters.Add(CalculateGeneExpression(phenotype, altitudeLocus[lifestageIndex], altitudeShape[lifestageIndex], altitudeRecShape[lifestageIndex]));
            parameters.Add(CalculateGeneExpression(phenotype, salinityLocus[lifestageIndex], salinityOpt[lifestageIndex], salinityRecOpt[lifestageIndex]));
            parameters.Add(CalculateGeneExpression(phenotype, salinityLocus[lifestageIndex], salinityShape[lifestageIndex], salinityRecShape[lifestageIndex]));
            parameters.Add(CalculateGeneExpression(phenotype, drainageLocus[lifestageIndex], drainageOpt[lifestageIndex], drainageRecOpt[lifestageIndex]));
            parameters.Add(CalculateGeneExpression(phenotype, drainageLocus[lifestageIndex], drainageShape[lifestageIndex], drainageRecShape[lifestageIndex]));
            parameters.Add(CalculateGeneExpression(phenotype, fertilityLocus[lifestageIndex], fertilityOpt[lifestageIndex], fertilityRecOpt[lifestageIndex]));
            parameters.Add(CalculateGeneExpression(phenotype, fertilityLocus[lifestageIndex], fertilityShape[lifestageIndex], fertilityRecShape[lifestageIndex]));
            parameters.Add(CalculateGeneExpression(phenotype, lifespanLocus[lifestageIndex], lifespan[lifestageIndex], lifespanRec[lifestageIndex]));
            if ((lifestage == "Spore") || (lifestage == "Gametophyte"))
            {
                parameters.Add(CalculateGeneExpression(phenotype, rainLocus[lifestageIndex], rain[lifestageIndex], rainRec[lifestageIndex]));
                if (lifestage == "Gametophyte")
                {
                    parameters.Add(spermNeighborhood);
                }
            }
            else
            {
                parameters.Add(CalculateGeneExpression(phenotype, advantageLocus, advantage, advantageRec));
                parameters.Add(CalculateGeneExpression(phenotype, distanceLocus, distanceMax, distanceRecMax));
                parameters.Add(CalculateGeneExpression(phenotype, distanceLocus, distanceShape, distanceRecShape));
            }
            return parameters;
        }

        private float CalculateGeneExpression(int phenotype, float locus, float dominantTrait, float recessiveTrait)
        {
            if ((locus == 0) || ((phenotype & ((int)Math.Pow(2, locus - 1))) != 0)) //The trait is not controlled by genetics or we have the dominant phenotype
            {
                return dominantTrait;
            }
            else //we have a recessive phenotype
            {
                return recessiveTrait;
            }
        }

        private void DisplayAllParameters()
        {
            m_log.Info("[vpgParameters] Current Parameters...");
            m_log.Info("[vpgParameters] versionID: " + versionID);
            m_log.Info("[vpgParameters] cycleTime: " + cycleTime.ToString());
            PrintArray(gametophyteNeighborhood, "gametophyteNeighborhood");
            m_log.Info("[vpgParameters] spermNeighborhood: " + spermNeighborhood.ToString());
            PrintArray(sporophyteNeighborhood, "sporophyteNeighborhood");
            PrintArray(sporophyteWeight, "sporophyteWeight");
            PrintArray(neighborShape, "NeighborShape");
            PrintArray(altitudeLocus, "altitudeLocus");
            PrintArray(altitudeOpt, "altitudeOpt");
            PrintArray(altitudeShape, "altitudeShape");
            PrintArray(altitudeRecOpt, "altitudeRecOpt");
            PrintArray(altitudeRecShape, "altitudeRecShape");
            PrintArray(salinityLocus, "salinityLocus");
            PrintArray(salinityOpt, "salinityOpt");
            PrintArray(salinityShape, "salinityShape");
            PrintArray(salinityRecOpt, "salinityRecOpt");
            PrintArray(salinityRecShape, "salinityRecShape");
            PrintArray(drainageLocus, "drainageLocus");
            PrintArray(drainageOpt, "drainageOpt");
            PrintArray(drainageShape, "drainageShape");
            PrintArray(drainageRecOpt, "drainageRecOpt");
            PrintArray(drainageRecShape, "drainageRecShape");
            PrintArray(fertilityLocus, "fertilityLocus");
            PrintArray(fertilityOpt, "fertilityOpt");
            PrintArray(fertilityShape, "fertilityShape");
            PrintArray(fertilityRecOpt, "fertilityRecOpt");
            PrintArray(fertilityRecShape, "fertilityRecShape");
            PrintArray(lifespanLocus, "lifespanLocus");
            PrintArray(lifespan, "lifespan");
            PrintArray(lifespanRec, "lifespanRec");
            PrintArray(rainLocus, "rainLocus");
            PrintArray(rain, "rain");
            PrintArray(rainRec, "rainRec");
            m_log.Info("[vpgParameters] advantageLocus: " + advantageLocus.ToString());
            m_log.Info("[vpgParameters] advantage: " + advantage.ToString());
            m_log.Info("[vpgParameters] advantageRec: " + advantageRec.ToString());
            m_log.Info("[vpgParameters] distanceLocus: " + distanceLocus.ToString());
            m_log.Info("[vpgParameters] distanceMax: " + distanceMax.ToString());
            m_log.Info("[vpgParameters] distanceShape: " + distanceShape.ToString());
            m_log.Info("[vpgParameters] distanceRecMax: " + distanceRecMax.ToString());
            m_log.Info("[vpgParameters] distanceRecShape: " + distanceRecShape.ToString());
        }


        private void PrintArray(float[] array, string labelText)
        {
            string toPrint = "";
            foreach (float element in array)
            {
                toPrint = toPrint + " " + element.ToString();
            }
            m_log.Info("[vpgParameters] " + labelText + ": " + toPrint);
        }
    }
}
