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

/* This module keeps track of dead plants and recycles them, listens for
 * commands to start a new population, and listens for commands to load/save
 * oars.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using log4net;
using Nini.Config;
using OpenMetaverse;

using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace vpgManagerModule
{
    public class ScriptAndHostPair
    {
        //This is just a way to pass and store script and host uuids as a unit.
        private UUID m_scriptuuid;
        private string m_hostuuid;

        public ScriptAndHostPair(UUID scriptuuid, string hostuuid)
        {
            m_scriptuuid = scriptuuid;
            m_hostuuid = hostuuid;
        }

        public ScriptAndHostPair()
        {
        }

        public UUID scriptUUID
        {
            get { return m_scriptuuid; }
            set { m_scriptuuid = value; }
        }

        public string hostUUID
        {
            get { return m_hostuuid; }
            set { m_hostuuid = value; }
        }
    }


    public class vpgManagerModule : IRegionModule
    {
        //Set up logging and console messages
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        IDialogModule m_dialogmod;
        IScriptModuleComms m_scriptmod;
        //Class member variables
        Scene m_scene;
        bool m_overLimit = false;
        bool m_resetRequested = false;
        //Script UUIDs of dead objects available for recycling.
        Queue<ScriptAndHostPair> m_recyclables = new Queue<ScriptAndHostPair>();
        //Configuration settings
        bool m_enabled;
        int m_listenChannel;
        int m_populationLimit;
        string m_oarPath;
        int m_maxFailures;
        int m_loci = 5; //TODO: Will eventually be configurable
        ScriptAndHostPair m_vpgPlanter = new ScriptAndHostPair();
        Random m_randomClass = new Random();

        #region IRegionModule interface

        public void Initialise(Scene scene, IConfigSource config)
        {
            IConfig vpgManagerConfig = config.Configs["vpgManager"];
            m_scene = scene;
            if (vpgManagerConfig != null)
            {
                m_enabled = vpgManagerConfig.GetBoolean("enabled", true);
                m_listenChannel = vpgManagerConfig.GetInt("listen_channel", 4);
                m_populationLimit = vpgManagerConfig.GetInt("population_limit", 8000);
                m_maxFailures = vpgManagerConfig.GetInt("max_failures", 10000);
                m_oarPath = vpgManagerConfig.GetString("oar_path", "addon-modules/vpgsim/oars/");
            }
            if (m_enabled)
            {
                m_log.Info("[vpgManager] Initialized... ");
            }
        }

        public void PostInitialise()
        {
            if (m_enabled)
            {
                //Register for modSendCommand events from inworld scripts
                //If we try to do this as part of Initialise() we get an UnhandledEventException.
                m_scriptmod = m_scene.RequestModuleInterface<IScriptModuleComms>();
                m_scriptmod.OnScriptCommand += new ScriptCommand(OnModSendCommand);
                //Register for chat commands so we can receive orders to generate new plants
                m_scene.EventManager.OnChatFromClient += OnChat;
                m_scene.EventManager.OnChatFromWorld += OnChat;
                //Register for IDialogModule so we can send notices
                m_dialogmod = m_scene.RequestModuleInterface<IDialogModule>();
                m_log.Info("[vpgManager] Post-initialized... ");
            }
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "vpgManagerModule"; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        #endregion

        int Binary2Decimal(string binaryNumber)
        {
            //Convert a binary string to a decimal number.
            int decimalNumber = (int)Convert.ToInt64(binaryNumber,2);
            return decimalNumber;
        }

        float GroundLevel(Vector3 location)
        {
            //Return the ground level at the specified location.
            //The first part of this function performs essentially the same function as llGroundNormal() without having to be called by a prim.
            //Find two points in addition to the position to define a plane
            Vector3 p0 = new Vector3(location.X, location.Y, (float)m_scene.Heightmap[(int)location.X, (int)location.Y]);
            Vector3 p1 = new Vector3();
            Vector3 p2 = new Vector3();
            if ((location.X + 1.0f) >= m_scene.Heightmap.Width)
                p1 = new Vector3(location.X + 1.0f, location.Y, (float)m_scene.Heightmap[(int)location.X, (int)location.Y]);
            else
                p1 = new Vector3(location.X + 1.0f, location.Y, (float)m_scene.Heightmap[(int)(location.X + 1.0f), (int)location.Y]);
            if ((location.Y + 1.0f) >= m_scene.Heightmap.Height)
                p2 = new Vector3(location.X, location.Y + 1.0f, (float)m_scene.Heightmap[(int)location.X, (int)location.Y]);
            else
                p2 = new Vector3(location.X, location.Y + 1.0f, (float)m_scene.Heightmap[(int)location.X, (int)(location.Y + 1.0f)]);
            //Find normalized vectors from p0 to p1 and p0 to p2
            Vector3 v0 = new Vector3(p1.X - p0.X, p1.Y - p0.Y, p1.Z - p0.Z);
            Vector3 v1 = new Vector3(p2.X - p0.X, p2.Y - p0.Y, p2.Z - p0.Z);
            v0.Normalize();
            v1.Normalize();
            //Find the cross product of the vectors (the slope normal).
            Vector3 vsn = new Vector3();
            vsn.X = (v0.Y * v1.Z) - (v0.Z * v1.Y);
            vsn.Y = (v0.Z * v1.X) - (v0.X * v1.Z);
            vsn.Z = (v0.X * v1.Y) - (v0.Y * v1.X);
            vsn.Normalize();
            //The second part of this function does the same thing as llGround() without having to be called from a prim
            //Get the height for the integer coordinates from the Heightmap
            float baseheight = (float)m_scene.Heightmap[(int)location.X, (int)location.Y];
            //Calculate the difference between the actual coordinates and the integer coordinates
            float xdiff = location.X - (float)((int)location.X);
            float ydiff = location.Y - (float)((int)location.Y);
            //Use the equation of the tangent plane to adjust the height to account for slope
            return (((vsn.X * xdiff) + (vsn.Y * ydiff)) / (-1 * vsn.Z)) + baseheight;
        }

        float WaterLevel(Vector3 location)
        {
            //Return the water level at the specified location.
            //This function performs essentially the same function as llWater() without having to be called by a prim.
            return (float)m_scene.RegionInfo.RegionSettings.WaterHeight;
        }

        Vector3 GenerateRandomLocation(int xMin, int xMax, int yMin, int yMax)
        {
            //Select a random x,y location from within a specified range and get the ground level at that location.  Return this x,y,z postion as a Vector of floats.
            Vector3 randomLocation = new Vector3();
            randomLocation.X = (float)(xMin + (m_randomClass.NextDouble() * (xMax - xMin)));
            randomLocation.Y = (float)(yMin + (m_randomClass.NextDouble() * (yMax - yMin)));
            randomLocation.Z = GroundLevel(randomLocation);
            return randomLocation;
        }

        int GenerateGenotype(string geneticInfo)
        {
            //Parse the input string defining the desired genotype and generate one.
            //If an allele is specified ('1' or '0') for a locus, use the specified allele.
            //If no allele is specified ('r') randomly select a '1' or '0'.
            //Return the decimal value of the genotype (including the diploid/haploid bit).
            string genotype = "";
            int geneticInfoLength = geneticInfo.Length;
            for (int i=0; i<geneticInfoLength; i++)
            {
                string allele = geneticInfo[i].ToString();
                if ((allele == "1") || (allele == "0"))
                {
                    genotype = genotype + allele;
                }
                else if (allele == "r")
                {
                    //Randomly select 1 or 0
                    int randomAllele = m_randomClass.Next(2);
                    genotype = genotype + randomAllele.ToString();
                }
                else
                {
                    //The genotype string is invalid.
                    return -1;
                }
            }
            if (geneticInfoLength == 5)
            {
                return Binary2Decimal(genotype);
            }
            else if (geneticInfoLength == 10)
            {
                //Add the bit to indicate it is a diploid genotype.
                return 1024 + Binary2Decimal(genotype);
            }
            else
            {
                //The genotype string is invalid.
                return -1;
            }
        }


        void OnChat(Object sender, OSChatMessage chat)
        {
            if ((chat.Channel != m_listenChannel) || (m_resetRequested))
                return;
            else if (chat.Message.ToLower() == "kill")
            {
                //Immediately stop accepting commands from the simulation
                m_resetRequested = true;
                m_dialogmod.SendGeneralAlert("Manager Module: Deleting all objects and resetting module.  Please be patient...");
                //Remove all objects, clear the list of recyclables and reset the population size limit.
                m_scene.DeleteAllSceneObjects();
                m_overLimit = false;
                m_recyclables = new Queue<ScriptAndHostPair>();
                if (m_dialogmod != null)
                m_dialogmod.SendGeneralAlert("Manager Module: Cleared old population.  Reloading region with default objects...");
                //Start accepting commands from the simulation again
                m_resetRequested = false;
                //Reload content from the default oar file
                try
                {
                    IRegionArchiverModule archivermod = m_scene.RequestModuleInterface<IRegionArchiverModule>();
                    if (archivermod != null)
                    {
                        string oarFilePath = System.IO.Path.Combine(m_oarPath, "vpgsim_default_content.oar");
                        archivermod.DearchiveRegion(oarFilePath);
                        m_dialogmod.SendGeneralAlert("Manager Module: Loaded default objects...");
                    }
                }
                catch
                {
                    m_dialogmod.SendGeneralAlert("Manager Module: Couldn't load default objects!");
                    m_log.WarnFormat("[vpgManager] Couldn't load default objects...");
                }
            }
            else if (chat.Message.Length > 9)
            {
                if (chat.Message.Substring(0,3).ToLower() == "oar")
                {
                    IRegionArchiverModule archivermod = m_scene.RequestModuleInterface<IRegionArchiverModule>();
                    if (chat.Message.Substring(3,5).ToLower() =="-load")
                    {
                        //Load the specified oar file
                        if (archivermod != null)
                        {
                            //Immediately stop accepting commands from the simulation
                            m_resetRequested = true;
                            m_dialogmod.SendGeneralAlert("Manager Module: Loading archive file.  Please be patient...");
                            //Remove all objects from the scene.  The Archiver Module does this anyway,
                            //but we need to be able to reset the list of recyclables after the scene
                            //objects are deleted but before the new objects start to load, so the module
                            //is ready to receive registration requests from recyclables in the oar.
                            m_scene.DeleteAllSceneObjects();
                            m_overLimit = false;
                            m_recyclables = new Queue<ScriptAndHostPair>();
                            //Start accepting commands from the simulation again
                            m_resetRequested = false;
                            try
                            {
                                string oarFilePath = System.IO.Path.Combine(m_oarPath, chat.Message.Substring(9));
                                archivermod.DearchiveRegion(oarFilePath);
                                m_dialogmod.SendGeneralAlert("Manager Module: Loaded archive file...");
                            }
                            catch
                            {
                                m_dialogmod.SendGeneralAlert("Manager Module: Couldn't load archive file!");
                                m_log.WarnFormat("[vpgManager] Couldn't load archive file...");
                            }
                        }
                    }
                    else if (chat.Message.Substring(3,5).ToLower() =="-save")
                    {
                        //Save the specified oar file
                        if (archivermod != null)
                        {
                            m_dialogmod.SendGeneralAlert("Manager Module: Saving archive file.  Please be patient...");
                            try
                            {
                                string oarFilePath = System.IO.Path.Combine(m_oarPath, chat.Message.Substring(9));
                                archivermod.ArchiveRegion(oarFilePath, new Dictionary<string, object>());
                                if (m_dialogmod != null)
                                m_dialogmod.SendGeneralAlert("Manager Module: Saved archive file...");
                            }
                            catch
                            {
                                m_dialogmod.SendGeneralAlert("Manager Module: Couldn't save archive file!");
                                m_log.WarnFormat("[vpgManager] Couldn't save archive file..");
                            }
                        }
                    }
                }
                else
                {
                    //Try to parse the string as a new plant generation command
                    string[] parsedMessage = chat.Message.Split(',');
                    if (parsedMessage.Length != 7)
                    {
                        //Invalid message string
                        m_log.WarnFormat("[vpgManager] Invalid new plant generation string...");
                        m_dialogmod.SendGeneralAlert("Manager Module: Invalid command - wrong number of arguments.  No new plants generated...");
                    }
                    else
                    {
                        m_dialogmod.SendGeneralAlert("Manager Module: Processing request.  Please be patient...");
                        string geneticInfo = parsedMessage[0];
                        int xMin = int.Parse(parsedMessage[1]);
                        int xMax = int.Parse(parsedMessage[2]);
                        int yMin = int.Parse(parsedMessage[3]);
                        int yMax = int.Parse(parsedMessage[4]);
                        int quantity = int.Parse(parsedMessage[5]);
                        int strictQuantity = int.Parse(parsedMessage[6]);
                        bool errorStatus = false;
                        int failureCount = 0;
                        int successCount = 0;
                        //The webform already checks these same things before creating the input string, but check them again in case the user manually edits the input string
                        if ((xMin < 0) || (xMin > 256) || (xMax < 0) || (xMax > 256) || (yMin < 0) || (yMin > 256) || (yMax < 0) || (yMax > 256) || (xMin >= xMax) || (yMin >= yMax) || (quantity <=0 ) || (quantity > 500) || !((geneticInfo.Length == 5) || (geneticInfo.Length == 10)))
                        {
                            m_dialogmod.SendGeneralAlert("Manager Module: Invalid command string.");
                            errorStatus = true;
                        }
                        while ((quantity > 0) && (!errorStatus) && (failureCount < m_maxFailures))
                        {
                            int randomGenotype = GenerateGenotype(geneticInfo);
                            Vector3 randomLocation = GenerateRandomLocation(xMin, xMax, yMin, yMax);
                            if (randomGenotype < 0)
                            {
                                m_dialogmod.SendGeneralAlert("Manager Module: Invalid command - Incorrect genotype format.  No new plants generated...");
                                errorStatus = true;
                            }
                            else if (randomLocation.Z > WaterLevel(randomLocation))
                            {
                                ScriptAndHostPair messageTarget = new ScriptAndHostPair();
                                lock (m_recyclables)
                                {
                                    if (m_recyclables.Count == 0) //Nothing available to recycle
                                    {
                                        //Nothing available to recycle. vpgPlanter will need to rez a new one
                                        messageTarget.scriptUUID = m_vpgPlanter.scriptUUID;
                                    }
                                    else
                                    {
                                        //There are objects available to recycle.  Recycle one from the queue
                                        messageTarget = m_recyclables.Dequeue();
                                    }
                                }
                                //Format the coordinates in the vector format expected by the LSL script
                                string coordinates = '<' + randomLocation.X.ToString() + ',' + randomLocation.Y.ToString() + ',' + randomLocation.Z.ToString() + '>';
                                //Send message to vpgPlanter or plant to be recycled
                                m_scriptmod.DispatchReply(messageTarget.scriptUUID, randomGenotype, coordinates, "");
                                //We have to pause between messages or the vpgPlanter gets overloaded and puts all the plants in the same location.
                                Thread.Sleep(50);
                                successCount++;
                                quantity--;
                            }
                            else if (strictQuantity == 0)
                            {
                                failureCount++;
                                quantity--;
                            }
                            else
                            {
                                failureCount++;
                                if (failureCount == m_maxFailures)
                                {
                                    m_dialogmod.SendGeneralAlert("Manager Module: Too many failed attempts.  Are you sure there is dry land in the selected range? Successfully planted:" + successCount.ToString() + "  Failures:" + failureCount.ToString());
                                    errorStatus = true;
                                }
                            }
                        }
                        if (m_dialogmod != null)
                        {
                            if (errorStatus == false)
                            {
                                m_dialogmod.SendGeneralAlert("Manager Module: Successfully planted:" + successCount.ToString() + "  Failures:" + failureCount.ToString());
                            }
                        }
                    }
                }
            }
        }


        void OnModSendCommand(UUID messageSourceScript, string messageId, string messageDestinationModule, string command, string coordinates)
        {
            if ((messageDestinationModule != Name) || (m_resetRequested))
            {
                //Message is not intended for this module
                return;
            }
            else if (command == "xxdeadxx")
            {
                //message from a dying plant.  Add it to the list of recyclables.
                ScriptAndHostPair scriptAndHost = new ScriptAndHostPair(messageSourceScript, coordinates);
                lock (m_recyclables)
                {
                    m_recyclables.Enqueue(scriptAndHost);
                }
            }
            else if (command.Substring(0, 10) == "xxgenomexx")
            {
                //message from parent plant wanting to reproduce (or vpgPlanter passing along a reproduction request heard on IRC)
                ScriptAndHostPair messageTarget = new ScriptAndHostPair();
                bool isRecycled = false;
                lock (m_recyclables)
                {
                    if (m_recyclables.Count == 0)
                    {
                        //Nothing available to recycle so the parent plant or vpgPlanter will be the target of our message
                        messageTarget.scriptUUID = messageSourceScript;
                    }
                    else
                    {
                        //There are objects available to recycle so the plant that will be recycled will be the target of our message
                        messageTarget = m_recyclables.Dequeue();
                        isRecycled = true;
                    }
                }
                if (isRecycled)
                {
                    int genome = Int32.Parse(command.Substring(10));
                    //Send message to Recycled plant
                    m_scriptmod.DispatchReply(messageTarget.scriptUUID, genome, coordinates, "");
                    if (genome >= Convert.ToInt32(Math.Pow(2, m_loci * 2)))
                    {
                        //The recycled plant became a sporophyte so we need to pass its key to its mother
                        m_scriptmod.DispatchReply(messageSourceScript, -1, "", messageTarget.hostUUID);
                    }
                }
                else if (!m_overLimit)
                {
                    if (m_scene.GetEntities().Length < m_populationLimit)
                    {
                        int genome = Int32.Parse(command.Substring(10));
                        //Send message to parent plant, vpgPlanter
                        m_scriptmod.DispatchReply(messageTarget.scriptUUID, genome, coordinates, "");
                    }
                    else
                    {
                        //Population size limit reached.  Don't rez any more new plants!
                        m_log.Info("[vpgManager] Population limit reached...");
                        m_overLimit = true;
                    }
                }
            }
            else if (command == "xxregisterxx")
            {
                //message from vpgPlanter registering with the module
                //Possible problem: If more than one is rezzed in the region, only the last one registered will be used. So if there is more than one vpgPlanter in the region and you delete the last one that registered, this module will not recognize that the others are still there.
                m_vpgPlanter.scriptUUID = messageSourceScript;
                m_log.Info("[vpgManager] Registered a vpgPlanter...");
            }
            else
            {
                m_log.Warn("[vpgManager] Unexpected modSendCommand received!"); //Debug
            }
        }
    }
}
