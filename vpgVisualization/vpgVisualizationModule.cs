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

// * An OpenSim Region Module to allow a user to highlight particular plants based on haplotype, genotype, or other characteristics

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Drawing;

using log4net;
using Nini.Config;
using OpenMetaverse;

using OpenSim.Framework;
using OpenSim.Region.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace vpgVisualization
{
	public class vpgVisualizationModule : IRegionModule
	{
		//Set up logging and console messages
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        //Global variables
        private List<Scene> m_scenes = new List<Scene>();
        Color4 m_Red = new Color4(1.0f, 0.0f, 0.0f, 1.0f);
		Color4 m_Blue = new Color4(0.0f, 0.0f, 1.0f, 1.0f);
		Color4 m_Purple = new Color4(1.0f, 0.0f, 1.0f, 1.0f);
		//These colors/sizes must match the vpgPlant.lsl script
		Color4 m_Green = new Color4(0.0f, 0.50f, 0.0f, 1.0f);
		Color4 m_Brown = new Color4(0.54f, 0.46f, 0.39f, 1.0f);
		Vector3 m_sporeSize = new Vector3(0.1f,0.1f,0.1f);
		Vector3 m_gametSize = new Vector3(0.2f,0.2f,0.05f);
		Vector3 m_sporoSize = new Vector3(0.5f,0.5f,0.5f);
		Vector3 m_largeSporeSize = new Vector3(0.5f, 0.5f, 0.5f);
		Vector3 m_largeGametSize = new Vector3(1.0f, 1.0f, 0.25f);
		Vector3 m_largeSporoSize = new Vector3(2.5f, 2.5f, 2.5f);
        int m_haplotypes;
        //Configuration settings
        int m_loci = 5; //Will eventually be configurable
		bool m_enabled;
        int m_listenChannel;

        #region IRegionModule interface

        public void Initialise(Scene scene, IConfigSource config)
		{
            IConfig vpgVisualizationConfig = config.Configs["vpgVisualization"];
			if (vpgVisualizationConfig != null)
			{
				m_enabled = vpgVisualizationConfig.GetBoolean("enabled", true);
                m_listenChannel = vpgVisualizationConfig.GetInt("listen_channel", 5);
			}
			if (m_enabled)
			{
                lock (m_scenes)
                {
                    if (!m_scenes.Contains(scene))
                    {
                        m_scenes.Add(scene);
                        m_log.WarnFormat("[vpgVisualization]: Initialized region {0}", scene.RegionInfo.RegionName);
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
                        scene.EventManager.OnChatFromClient += OnChat;
                        scene.EventManager.OnChatFromWorld += OnChat;
                        m_log.WarnFormat("[vpgVisualization]: Postinitialized region {0}", scene.RegionInfo.RegionName);
                    }
                }
                m_haplotypes = Convert.ToInt32(Math.Pow(2, m_loci));
            }
        }

        public void Close()
		{
        }

        public string Name
		{
            get { return "vpgVisualizationModule"; }
        }

        public bool IsSharedModule
		{
            get { return true; }
        }

        #endregion

        void DialogToAll(string message)
        {
            foreach (Scene scene in m_scenes)
            {
                IDialogModule dialogmod = scene.RequestModuleInterface<IDialogModule>();
                if (dialogmod != null)
                {
                    dialogmod.SendGeneralAlert(message);
                }
            }
        }

		void OnChat(Object sender, OSChatMessage chat)
		{
			if (chat.Channel != m_listenChannel)
				return;
			else
			{
				if (chat.Message.ToLower() == "reset")
				{
					m_log.Info("[vpgVisualization] Reset... ");
					DialogToAll("Visualization Module: Reset...");
                    foreach (Scene scene in m_scenes)
                    {
                        Reset(scene);
                    }
				}
				else if (chat.Message.ToLower() == "heterozygosity")
				{
					m_log.Info("[vpgVisualization] HeterozygosityView...");
					DialogToAll("Visualization Module: Heterozygosity view...");
					foreach (Scene scene in m_scenes)
                    {
                        HeterozygosityView(scene);
                    }
				}
				else if (chat.Message.Length == 7)
				{
					if (chat.Message.Substring(0, 5).ToLower() == "locus")
					{
						int locus = Int32.Parse(chat.Message.Substring(6));
						m_log.Info("[vpgVisualization] LocusView...");
						DialogToAll("Visualization Module: Locus View (" + locus.ToString() + ")...");
						foreach (Scene scene in m_scenes)
                        {
                            LocusView(scene, locus);
                        }
					}
				}
				else if (chat.Message.Length == 15)
				{
					if (chat.Message.Substring(0, 9).ToLower() == "haplotype")
					{
						string humanhaplotype = chat.Message.Substring(10);
						int haplotype = Binary2Decimal(humanhaplotype);
						m_log.Info("[vpgVisualization] HaplotypeView...");
						DialogToAll("Visualization Module: Haplotype View (" + humanhaplotype + ")...");
						foreach (Scene scene in m_scenes)
                        {
                            HaplotypeView(scene, haplotype);
                        }
					}
					else if (chat.Message.Substring(0, 9).ToLower() == "lifestage")
					{
						string lifestage = chat.Message.Substring(10);
						m_log.Info("[vpgVisualization] LifestageView...");
						DialogToAll("Visualization Module: Lifestage View (" + lifestage + ")...");
						foreach (Scene scene in m_scenes)
                        {
                            LifestageView(scene, lifestage);
                        }
					}
				}
				else if (chat.Message.Length == 19)
				{
					if (chat.Message.Substring(0, 8).ToLower() == "genotype")
					{
						string humangenotype = chat.Message.Substring(9); //The human readable genotype
						int genotype = Binary2Decimal(humangenotype) + Convert.ToInt32(Math.Pow(2, m_loci * 2)); //Add the extra hidden sporophyte bit
						m_log.Info("[vpgVisualization] GenotypeView...");
						DialogToAll("Visualization Module: Genotype View (" + humangenotype + ")...");
						foreach (Scene scene in m_scenes)
                        {
                            GenotypeView(scene, genotype);
                        }
					}
				}
				else
				{
					m_log.Info("[vpgVisualization] Invalid command...");
					DialogToAll("Visualization Module: Invalid command...");
				}
			}
		}

		void Reset(Scene scene)
		{ //Returns all plants to their default size, color
			EntityBase[] everyobject = scene.GetEntities();
			SceneObjectGroup sog;
			foreach (EntityBase entity in everyobject)
			{
				if (entity is SceneObjectGroup)
				{ //make sure it is an object, not an avatar
					sog = (SceneObjectGroup)entity;
					if (sog.Name.Length > 4)
					{ //Avoid an exception on non-plants with short names
						string objecttype = sog.Name.Substring(0,5);
						if (objecttype == "Spore")
						{
							Primitive.TextureEntry tex = sog.RootPart.Shape.Textures;
							tex.DefaultTexture.RGBA = m_Brown;
							sog.RootPart.Scale = m_sporeSize;
							sog.RootPart.UpdateTextureEntry(tex.GetBytes());
						}
						else if (objecttype == "Gamet")
						{
							Primitive.TextureEntry tex = sog.RootPart.Shape.Textures;
							tex.DefaultTexture.RGBA = m_Green;
							sog.RootPart.Scale = m_gametSize;
							sog.RootPart.UpdateTextureEntry(tex.GetBytes());
						}
						else if (objecttype == "Sporo")
						{
							Primitive.TextureEntry tex = sog.RootPart.Shape.Textures;
							tex.DefaultTexture.RGBA = m_Green;
							sog.RootPart.Scale = m_sporoSize;
							sog.RootPart.UpdateTextureEntry(tex.GetBytes());
						}
					}
				}
			}
		}


		void LifestageView(Scene scene, string lifestage)
		{ //Show plants at a particular lifestage
			EntityBase[] everyobject = scene.GetEntities();
			SceneObjectGroup sog;
			foreach (EntityBase entity in everyobject)
			{
				if (entity is SceneObjectGroup)
				{ //make sure it is an object, not an avatar
					sog = (SceneObjectGroup)entity;
					if (sog.Name.Length > 4)
					{ //Avoid an exception on non-plants with short names
						string objecttype = sog.Name.Substring(0,5);
                        if (objecttype.ToLower() == lifestage.ToLower())
						{
							Highlight(sog, m_Blue);
						}
					}
				}
			}
		}

		void GenotypeView(Scene scene, int genotype)
		{ // Show plants with a particular genotype
			int haplotype1 = (m_haplotypes - 1) & genotype;
			int haplotype2 = (genotype >> m_loci) & (m_haplotypes - 1);
			EntityBase[] everyobject = scene.GetEntities();
			SceneObjectGroup sog;
			foreach (EntityBase entity in everyobject)
			{
				if (entity is SceneObjectGroup)
				{ //make sure it is an object, not an avatar
					sog = (SceneObjectGroup)entity;
					if (sog.Name.Length > 4)
					{ //Avoid an exception on non-plants with short names
						string objecttype = sog.Name.Substring(0,5);
						if (objecttype == "Sporo")
						{
							int geno = Int32.Parse(sog.Name.Substring(5));
							int haplo1 = (m_haplotypes - 1) & geno;
							int haplo2 = (geno >> m_loci) & (m_haplotypes - 1);
							if (((haplo1 == haplotype1) && (haplo2 == haplotype2)) || ((haplo1 == haplotype2) && (haplo2 == haplotype1)))
							{
								Highlight(sog, m_Blue);
							}
						}
					}
				}
			}
		}

		void HaplotypeView(Scene scene, int haplotype)
		{  //Show plants with a particular haplotype
			EntityBase[] everyobject = scene.GetEntities();
			SceneObjectGroup sog;
			foreach (EntityBase entity in everyobject)
			{
				if (entity is SceneObjectGroup)
				{ //make sure it is an object, not an avatar
					sog = (SceneObjectGroup)entity;
					if (sog.Name.Length > 4)
					{ //Avoid an exception on non-plants with short names
						string objecttype = sog.Name.Substring(0,5);
						if ((objecttype == "Spore") || (objecttype == "Gamet"))
						{
							int haplo1 = Int32.Parse(sog.Name.Substring(5));
							if (haplo1 == haplotype)
							{
								Highlight(sog, m_Blue);
							}
						}
						else if (objecttype == "Sporo")
						{
							int geno = Int32.Parse(sog.Name.Substring(5));
							int haplo1 = (m_haplotypes - 1) & geno;
							int haplo2 = (geno >> m_loci) & (m_haplotypes - 1);
							if ((haplo1 == haplotype) || (haplo2 == haplotype))
							{
								Highlight(sog, m_Blue);
							}
						}
					}
				}
			}
		}

		void HeterozygosityView(Scene scene)
		{ //Show plant using grayscale representing how many loci are heterozygous
			EntityBase[] everyobject = scene.GetEntities();
			SceneObjectGroup sog;
			foreach (EntityBase entity in everyobject)
			{
				if (entity is SceneObjectGroup)
				{ //make sure it is an object, not an avatar
					sog = (SceneObjectGroup)entity;
					if (sog.Name.Length > 4)
					{ //Avoid an exception on non-plants with short names
						string objecttype = sog.Name.Substring(0,5);
						if (objecttype == "Sporo")
						{
							float heterozygosity = 0.0f;
							int locus = 1;
							int geno = Int32.Parse(sog.Name.Substring(5));
							int haplo1 = (m_haplotypes - 1) & geno;
							int haplo2 = (geno >> m_loci) & (m_haplotypes - 1);
							while (locus <= m_loci)
							{
								int locusmask = (int)(Math.Pow(2, locus-1));
								if ((haplo1 & locusmask) != (haplo2 & locusmask))
								{ // it is heterozygous
									heterozygosity = heterozygosity + (1.0f / m_loci);
								}
								locus ++;
							}
							Highlight(sog, new Color4(heterozygosity, heterozygosity, heterozygosity, 1.0f));
						}
					}
				}
			}
		}

		void LocusView(Scene scene, int locus)
		{ //For a chosen locus, colors plants based on their allele(s)
			int locusmask = (int)(Math.Pow(2, locus - 1));
			EntityBase[] everyobject = scene.GetEntities();
			SceneObjectGroup sog;
			foreach (EntityBase entity in everyobject)
			{
				if (entity is SceneObjectGroup)
				{ //make sure it is an object, not an avatar
					sog = (SceneObjectGroup)entity;
					if (sog.Name.Length > 4)
					{ //Avoid an exception on non-plants with short names
						string objecttype = sog.Name.Substring(0,5);
						if ((objecttype == "Spore") || (objecttype == "Gamet"))
						{
							int haplo1 = Int32.Parse(sog.Name.Substring(5));
							if ((haplo1 & locusmask) == locusmask)
							{ // It has the dominant allele
								Highlight(sog, m_Red); //highlight it in Red
							}
							else
							{ //It has the recessive allele
								Highlight(sog, m_Blue);
							}
						}
						else if (objecttype == "Sporo")
						{
							int geno = Int32.Parse(sog.Name.Substring(5));
							int haplo1 = (m_haplotypes - 1) & geno;
							int haplo2 = (geno >> m_loci) & (m_haplotypes - 1);
							if ((haplo1 & locusmask) == (haplo2 & locusmask))
							{ //It is homozygous...
								if ((haplo1 & locusmask) == locusmask)
								{ //...dominant
									Highlight(sog, m_Red);
								}
								else
								{ //...recessive
									Highlight(sog, m_Blue);
								}
							}
							else
							{ //It is heterozygous
								Highlight(sog, m_Purple);
							}
						}
					}
				}
			}
		}

		void Highlight(SceneObjectGroup sog, Color4 highlightcolor)
		{
			//Make it larger
			string objecttype = sog.Name.Substring(0,5);
			if (objecttype == "Spore")
			{
				sog.RootPart.Scale = m_largeSporeSize;
			}
			else if (objecttype == "Gamet")
			{
				sog.RootPart.Scale = m_largeGametSize;
			}
			else if (objecttype == "Sporo")
			{
				sog.RootPart.Scale = m_largeSporoSize;
			}
			//Change the color
			Primitive.TextureEntry tex = sog.RootPart.Shape.Textures;
			tex.DefaultTexture.RGBA = highlightcolor;
			sog.RootPart.UpdateTextureEntry(tex.GetBytes());
		}

		int Binary2Decimal(string binary)
		{
			int multiplier = 1;
			int stringlength = binary.Length - 1;
			int decimals = 0;
			while (stringlength >= 0)
			{
				decimals += Int32.Parse(binary.Substring(stringlength, 1)) * multiplier;
				multiplier *= 2;
				stringlength = stringlength - 1;
			}
			return decimals;
		}
    }
}
