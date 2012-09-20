#! /usr/bin/env python
"""
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
"""

# parametersform.py
# Creates a list of parameters to be used by the vpgParameters module.
# This script performs the same function as the online webform, but locally.
# It also provides buttons to plot the fitness curves and dispersal curve.

import tkFileDialog
import os
import time
from Tkinter import *
from tkMessageBox import showinfo
import matplotlib
matplotlib.use('TkAgg')
from matplotlib.pyplot import xlabel
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg
from matplotlib.figure import Figure
from matplotlib.legend import Legend
from pylab import setp
from math import log
import tkFont


class GuiSection(Frame):
    def __init__(self, parent=None, sectionTitle="Default Section Title"):
        Frame.__init__(self, parent)
        showHideButton = Button(self, text='+', width=1, height=1, command=lambda: self.HideDataEntryFrame(showHideButton, self.dataEntryFrame))
        showHideButton.grid(row=0, column=0)
        Label(self, text=sectionTitle, font=tkFont.Font(weight='bold', size=14), fg='darkblue').grid(row=0, column=1, sticky=W)
        self.dataEntryFrame = Frame(self)
        self.hidden = True

    def HideDataEntryFrame(self, button, dataFrame):
        if self.hidden:
            button.config(text='-')
            self.hidden = False
            dataFrame.grid(row=1, column=1)
        else:
            button.config(text="+")
            self.hidden = True
            dataFrame.grid_forget()
        m_app.outerFrame.update_idletasks()
        m_app.canvas.configure(scrollregion=(0, 0, m_app.outerFrame.winfo_width(), m_app.outerFrame.winfo_height()))


class App:
    def __init__(self, master):
        #Define fonts - I didn't have to do this on python 2.5 but something changed in 2.6
        self.boldFont = tkFont.Font(size=14, weight='bold')
        self.italicFont = tkFont.Font(size=11, slant='italic')
        #Setup the gui with scrollbars
        #We have a scrollable canvas containing a frame - all form objects go into the frame
        master.grid_rowconfigure(0, weight=1)
        master.grid_columnconfigure(0, weight=1)
        master.title("Parameter Adjustment Form")
        self.canvas = Canvas(master, width=700, height=700)
        self.canvas.grid(row=0, column=0, sticky='nswe', padx=15)
        vScroll = Scrollbar(master, orient=VERTICAL, command=self.canvas.yview)
        vScroll.grid(row=0, column=1, sticky='ns')
        hScroll = Scrollbar(master, orient=HORIZONTAL, command=self.canvas.xview)
        hScroll.grid(row=1, column=0, sticky='we')
        self.canvas.configure(xscrollcommand=hScroll.set, yscrollcommand=vScroll.set)
        self.outerFrame = Frame(self.canvas)
        self.canvas.create_window(0, 0, window=self.outerFrame, anchor='nw')
        #Add the rest of the items to the form
        self.AddWidgets(self.outerFrame)
        #Load default values
        self.ResetForm()
        #Reset things to grab the new size of the frame so the scrollbars work correctly
        self.outerFrame.update_idletasks()
        self.canvas.configure(scrollregion=(0, 0, self.outerFrame.winfo_width(), self.outerFrame.winfo_height()))

    def ResetForm(self):
        #Fill the form with some good default starting values
        defaultParameters = ["0", "300", "0.0", "2.0", "0.0", "6.0", "0.0", "6.0", "6.0", "0.0", "2.0", "1.0", "0.0", "1.0", "1.0", "0", "0", "0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0", "0", "0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0", "0", "0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0", "0", "0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0", "0", "0", "10", "10", "10", "0", "0", "0", "0", "0", "0.9", "0.9", "0.0", "0.0", "0", "0", "0", "0", "75.0", "5.71", "0.0", "5.71"]
        self.PopulateFormFields(defaultParameters)

    def SaveParameters(self):
        #Ask where to save the file
        fileName = tkFileDialog.asksaveasfilename(parent=None, initialdir='./', initialfile='temp')
        file = open(fileName, 'w')
        if (len(fileName) > 0):
            for parameter in m_values:
                file.write(str(parameter.get()) + '\n')
            file.close()
            showinfo(title='File Ready', message='Your parameters file is ready to load.\n\nTo load the parameters:\nTouch the Load New Parameters object in the 3D environment,\n...or...\nType in the chat window: /15 %s\n\nOnce loaded, each plant will begin using the new parameters on its next cycle.' % os.path.basename(fileName))

    def ReadParametersFromFile(self):
        #Ask for a file to load
        file = tkFileDialog.askopenfile(parent=None, initialdir='./')
        newParameters = []
        for line in file:
            newParameters.append(line.rstrip())
        self.PopulateFormFields(newParameters)

    def PopulateFormFields(self, parameters):
        #Generate a timestamp for the Version Identifier
        m_values[0].delete(0, END)
        m_values[0].insert(0, int(time.time()))
        #Load values into Text Entry Fields
        for i in range(1, 15):
            m_values[i].delete(0, END)
            m_values[i].insert(0, parameters[i])
        for i in range(18, 30):
            m_values[i].delete(0, END)
            m_values[i].insert(0, parameters[i])
        for i in range(33, 45):
            m_values[i].delete(0, END)
            m_values[i].insert(0, parameters[i])
        for i in range(48, 60):
            m_values[i].delete(0, END)
            m_values[i].insert(0, parameters[i])
        for i in range(63, 75):
            m_values[i].delete(0, END)
            m_values[i].insert(0, parameters[i])
        for i in range(78, 84):
            m_values[i].delete(0, END)
            m_values[i].insert(0, parameters[i])
        for i in range(86, 90):
            m_values[i].delete(0, END)
            m_values[i].insert(0, parameters[i])
        for i in range(91, 93):
            m_values[i].delete(0, END)
            m_values[i].insert(0, parameters[i])
        for i in range(94, 98):
            m_values[i].delete(0, END)
            m_values[i].insert(0, parameters[i])
        #Set Radiobuttons
        for i in [15, 16, 17, 30, 31, 32, 45, 46, 47, 60, 61, 62, 75, 76, 77, 84, 85, 90, 93]:
            m_values[i].set(parameters[i])

    def PlotFitness(self, attributeName, index):
        if (attributeName == 'Altitude'):
            xValues = [20.0, 25.0, 30.0, 35.0, 40.0, 45.0, 50.0, 55.0, 60.0, 65.0, 70.0]
            scaleMultiplier = 2500
        else:
            xValues = [0.0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0]
            scaleMultiplier = 1
        yDomSpore = [] #Lists to hold y values for each xaxis value
        yDomGamet = []
        yDomSporo = []
        yRecSpore = []
        yRecGamet = []
        yRecSporo = []
        for x in xValues:
            #calculate the y values
            yDomSpore.append(1.0 - (float(m_values[index+3].get()) * (((x -  float(m_values[index].get()))**2) / scaleMultiplier)))
            yRecSpore.append(1.0 - (float(m_values[index+9].get()) * (((x -  float(m_values[index+6].get()))**2) / scaleMultiplier)))
            yDomGamet.append(1.0 - (float(m_values[index+4].get()) * (((x -  float(m_values[index+1].get()))**2) / scaleMultiplier)))
            yRecGamet.append(1.0 - (float(m_values[index+10].get()) * (((x -  float(m_values[index+7].get()))**2) / scaleMultiplier)))
            yDomSporo.append(1.0 - (float(m_values[index+5].get()) * (((x -  float(m_values[index+2].get()))**2) / scaleMultiplier)))
            yRecSporo.append(1.0 - (float(m_values[index+11].get()) * (((x -  float(m_values[index+8].get()))**2) / scaleMultiplier)))
        #Set up the plot and the figure to hold the plot
        fitnessFigure = Figure(figsize=(6,5), dpi=100)
        fitnessPlot = fitnessFigure.add_subplot(111)
        fitnessPlot.plot(xValues, yDomSpore, 'k',  xValues, yRecSpore, 'k--', xValues, yDomGamet, 'y', xValues, yRecGamet, 'y--', xValues, yDomSporo, 'b', xValues, yRecSporo, 'b--')
        if (attributeName == 'Altitude'):
            fitnessPlot.axis([19.9, 70.1, -0.01, 1.01])
        else:
            fitnessPlot.axis([-0.01, 1.01, -0.01, 1.01]) #x and y ranges to include in the plot area
        fitnessPlot.set_xlabel('%s Value' % attributeName)
        fitnessPlot.set_ylabel('Fitness Index')
        setp(fitnessPlot.get_xticklines(), visible=False)
        setp(fitnessPlot.get_yticklines(), visible=False)
        plotLegend = fitnessPlot.legend(('Spores-Dominant', 'Spores-Recessive', 'Gametophytes-Dominant', 'Gametophytes-Recessive', 'Sporophytes-Dominant', 'Sporophytes-Recessive'), loc='lower right')
        for t in plotLegend.get_texts():
            t.set_fontsize(6)
        #Put the plot in a gui window with a Quit button
        plotWindow = Toplevel()
        plotWindow.title('%s Fitness curve' % attributeName)
        plotCanvas = FigureCanvasTkAgg(fitnessFigure, master=plotWindow)
        plotCanvas.show()
        plotCanvas.get_tk_widget().pack(side=TOP, fill=BOTH, expand=1)
        Button(master=plotWindow, text='Close', width= 8, command=plotWindow.destroy).pack(side=BOTTOM, anchor=E, padx=30)

    def PlotDispersalCurve(self, index):
        xValues = [0.0, 0.1, 0.2, 0.4, 1.0, 2.0, 3.0, 4.0, 5.0, 6.5, 8.0, 10.0, 15.0, 20.0, 25.0, 30.0, 45.0, 60.0, 75.0, 90.0, 105.0, 120.0, 135.0, 150.0, 165.0, 180.0, 195.0, 210.0, 225.0, 240.0, 255.0, 270.0, 285.0, 300.0] #Plotting enough points to get a fairly smooth curve - even at low x values
        yDomSporo = [] #Lists to hold y values for each xaxis value
        yRecSporo = []
        for x in xValues:
            #calculate the y values
            yDomSporo.append(float(m_values[index].get()) - ((float(m_values[index].get()) / float(m_values[index+1].get())) * log(x + 1)))
            yRecSporo.append(float(m_values[index+2].get()) - ((float(m_values[index+2].get()) / float(m_values[index+3].get())) * log(x + 1)))
        #Set up the plot and the figure to hold the plot
        fitnessFigure = Figure(figsize=(6,5), dpi=100)
        fitnessPlot = fitnessFigure.add_subplot(111)
        fitnessPlot.plot(xValues, yDomSporo, 'b', xValues, yRecSporo, 'b--')
        maxY = 100.0
        if (float(m_values[index].get()) > float(m_values[index+2].get())):
            maxY = float(m_values[index].get()) + 0.1
        else:
            maxY = float(m_values[index+2].get()) + 0.1
        fitnessPlot.axis([-0.3, 300.3, -0.1, maxY])
        fitnessPlot.set_xlabel('Random # Selected')
        fitnessPlot.set_ylabel('Dispersal Distance')
        setp(fitnessPlot.get_xticklines(), visible=False)
        setp(fitnessPlot.get_yticklines(), visible=False)
        plotLegend = fitnessPlot.legend(('Sporophytes-Dominant', 'Sporophytes-Recessive'), loc='upper right')
        for t in plotLegend.get_texts():
            t.set_fontsize(6)
        #Put the plot in a gui window with a Quit button
        plotWindow = Toplevel()
        plotWindow.title('Dispersal Distribution')
        plotCanvas = FigureCanvasTkAgg(fitnessFigure, master=plotWindow)
        plotCanvas.show()
        plotCanvas.get_tk_widget().pack(side=TOP, fill=BOTH, expand=1)
        Button(master=plotWindow, text='Close', width=8, command=plotWindow.destroy).pack(side=BOTTOM, anchor=E, padx=30)

    # Functions to setup the graphical user interface.  I could/should have made better use of OOP here...

    def AddWidgets(self, frame):
        Message(frame, text="This form controls the life history and genetic parameters of a simulated population of ferns growing in a 3D virtual environment.  Changes made here will not take effect until they are enabled from within that environment.", width=650, font=self.boldFont).pack(anchor=W)
        Message(frame, text=" ", width=650, font=("", 6, "")).pack(anchor=W) #Blank line
        topButtonSection = Frame(frame)
        topButtonSection.pack(anchor=W)
        Button(topButtonSection, text="Reset Form", command=self.ResetForm, width=10).pack(side=LEFT)
        Button(topButtonSection, text="Open File", command=self.ReadParametersFromFile, width=10).pack(side=LEFT)
        Button(topButtonSection, text="Save", command=self.SaveParameters, width=7).pack(side=LEFT)
        Button(topButtonSection, text="Quit", command=frame.master.quit, width=7).pack(side=LEFT)
        Message(frame, text=" ", width=650, font=("", 6, "")).pack(anchor=W) #Blank line
        cycleTimeSection = GuiSection(frame, "Cycle Time / Neighborhood Sizes")
        cycleTimeSection.pack(anchor=W)
        self.GuiEntryBlock(cycleTimeSection.dataEntryFrame, "Version Identifier:", "A unique code to identify this parameter set.", "Generated automatically", 12, 0, 0)
        self.GuiEntryBlock(cycleTimeSection.dataEntryFrame, "Cycle Time (seconds):", "Controls how often the plants calculate fitness and determines the speed of the simulation.", "30-3600", 5, 3, 1)
        self.GuiLifestageEntryBlock3Description(cycleTimeSection.dataEntryFrame, "Gametophyte Neighborhood (meters):", "Gametophytes effect the fitness of neighbors within this radius.", "0.0-10.0", 6, 2)
        self.GuiLifestageEntryBlock1Description(cycleTimeSection.dataEntryFrame, "Sperm Neighborhood (meters):", "Distance that sperm can travel to a mate.", "0.0-10.0", "Gametophytes", 9, 5)
        self.GuiLifestageEntryBlock3Description(cycleTimeSection.dataEntryFrame, "Sporophyte Neighborhood (meters):", "Sporophytes effect the fitness of neighbors within this radius.", "0.0-10.0", 12, 6)
        self.GuiLifestageEntryBlock3Description(cycleTimeSection.dataEntryFrame, "Sporophyte Weight:", "How many times more effect sporophytes have on their neighbors than gametophytes.", "0.0-10.0", 15, 9)
        self.GuiLifestageEntryBlock3Description(cycleTimeSection.dataEntryFrame, "Neighbor Shape:", "The shape of the fitness curve based on the weighted number of neighbors.", "0.0-10.0", 18, 12)
        altitudeSection = GuiSection(frame, "Altitude")
        altitudeSection.pack(anchor=W)
        self.GuiRadiobuttonBlock3(altitudeSection.dataEntryFrame, "Locus controlling response to Altitude:", 0, 15)
        self.GuiLifestageEntryBlock3(altitudeSection.dataEntryFrame, "Optimum Altitude (meters) for dominant phenotype:", "0.0-500.0", 4, 18)
        self.GuiLifestageEntryBlock3(altitudeSection.dataEntryFrame, "Altitude fitness curve for dominant phenotype:", "0.0-10.0", 6, 21)
        self.GuiLifestageEntryBlock3(altitudeSection.dataEntryFrame, "Optimum Altitude (meters) for recessive phenotype:", "0.0-500.0", 8, 24)
        self.GuiLifestageEntryBlock3(altitudeSection.dataEntryFrame, "Altitude fitness curve for recessive phenotype:", "0.0-10.0", 10, 27)
        Button(altitudeSection.dataEntryFrame, text='View altitude fitness curves', command=(lambda: self.PlotFitness('Altitude', 18))).grid(row=12, column=0, columnspan=2)
        salinitySection = GuiSection(frame, "Soil Salinity")
        salinitySection.pack(anchor=W)
        self.GuiRadiobuttonBlock3(salinitySection.dataEntryFrame, "Locus controlling response to Soil Salinity:", 0, 30)
        self.GuiLifestageEntryBlock3(salinitySection.dataEntryFrame, "Optimum Salinity for dominant phenotype:", "0.0-1.0", 4, 33)
        self.GuiLifestageEntryBlock3(salinitySection.dataEntryFrame, "Salinity fitness curve for dominant phenotype:", "0.0-10.0", 6, 36)
        self.GuiLifestageEntryBlock3(salinitySection.dataEntryFrame, "Optimum Salinity for recessive phenotype:", "0.0-1.0", 8, 39)
        self.GuiLifestageEntryBlock3(salinitySection.dataEntryFrame, "Salinity fitness curve for recessive phenotype:", "0.0-10.0", 10, 42)
        Button(salinitySection.dataEntryFrame, text='View salinity fitness curves', command=(lambda: self.PlotFitness('Soil Salinity', 33))).grid(row=12, column=0, columnspan=2)
        drainageSection = GuiSection(frame, "Soil Drainage")
        drainageSection.pack(anchor=W)
        self.GuiRadiobuttonBlock3(drainageSection.dataEntryFrame, "Locus controlling response to Soil Drainage:", 0, 45)
        self.GuiLifestageEntryBlock3(drainageSection.dataEntryFrame, "Optimum Drainage for dominant phenotype:", "0.0-1.0", 4, 48)
        self.GuiLifestageEntryBlock3(drainageSection.dataEntryFrame, "Drainage fitness curve for dominant phenotype:", "0.0-10.0", 6, 51)
        self.GuiLifestageEntryBlock3(drainageSection.dataEntryFrame, "Optimum Drainage for recessive phenotype:", "0.0-1.0", 8, 54)
        self.GuiLifestageEntryBlock3(drainageSection.dataEntryFrame, "Drainage fitness curve for recessive phenotype:", "0.0-10.0", 10, 57)
        Button(drainageSection.dataEntryFrame, text='View drainage fitness curves', command=(lambda: self.PlotFitness('Soil Drainage', 48))).grid(row=12, column=0, columnspan=2, sticky=E)
        fertilitySection = GuiSection(frame, "Soil Fertility")
        fertilitySection.pack(anchor=W)
        self.GuiRadiobuttonBlock3(fertilitySection.dataEntryFrame, "Locus controlling response to Soil Fertility:", 0, 60)
        self.GuiLifestageEntryBlock3(fertilitySection.dataEntryFrame, "Optimum Fertility for dominant phenotype:", "0.0-1.0", 4, 63)
        self.GuiLifestageEntryBlock3(fertilitySection.dataEntryFrame, "Fitness fitness curve for dominant phenotype:", "0.0-10.0", 6, 66)
        self.GuiLifestageEntryBlock3(fertilitySection.dataEntryFrame, "Optimum Fertility for recessive phenotype:", "0.0-1.0", 8, 69)
        self.GuiLifestageEntryBlock3(fertilitySection.dataEntryFrame, "Fertility fitness curve for recessive phenotype:", "0.0-10.0", 10, 72)
        Button(fertilitySection.dataEntryFrame, text='View fertility fitness curves', command=(lambda: self.PlotFitness('Soil Fitness', 63))).grid(row=12, column=0, columnspan=2, sticky=W)
        lifespanSection = GuiSection(frame, "Maximum Lifespan")
        lifespanSection.pack(anchor=W)
        self.GuiRadiobuttonBlock3(lifespanSection.dataEntryFrame, "Locus controlling Maximum Lifespan:", 0, 75)
        self.GuiLifestageEntryBlock3(lifespanSection.dataEntryFrame, "Maximum Lifespan (# of cycles) for dominant phenotype:", "0-100", 4, 78)
        self.GuiLifestageEntryBlock3(lifespanSection.dataEntryFrame, "Maximum Lifespan (# of cycles) for recessive phenotype:", "0-100", 6, 81)
        humiditySection = GuiSection(frame, "Humidity / Moisture Requirements")
        humiditySection.pack(anchor=W)
        self.GuiRadiobuttonBlock2(humiditySection.dataEntryFrame, "Locus controlling Minimum Cloudcover needed for germination or mating:", "Cloudcover is a proxy for humidity & precipitation.", 0, 84)
        self.GuiLifestageEntryBlock2(humiditySection.dataEntryFrame, "Minimum Cloudcover for dominant phenotype:", "0.0-1.0", 4, 86)
        self.GuiLifestageEntryBlock2(humiditySection.dataEntryFrame, "Minimum Cloudcover for recessive phenotype:", "0.0-1.0", 6, 88)
        advantageSection = GuiSection(frame, "Sporulation Rate")
        advantageSection.pack(anchor=W)
        self.GuiRadiobuttonBlock1Description(advantageSection.dataEntryFrame, "Locus controlling Sporulation Advantage:", "Sporulation Advantage is the number of additional spores a perfectly fit sporophyte releases each time it sporulates. For less fit sporophytes, the additional number of spores is scaled based on fitness.", 0, 90)
        self.GuiLifestageEntryBlock1(advantageSection.dataEntryFrame, "Sporulation Advantage (# of additional spores) for dominant phenotype:", "0-10", "Sporophytes", 3, 91)
        self.GuiLifestageEntryBlock1(advantageSection.dataEntryFrame, "Sporulation Advantage for recessive phenotype:", "0-10", "Sporophytes", 5, 92)
        dispersalSection = GuiSection(frame, "Dispersal")
        dispersalSection.pack(anchor=W)
        self.GuiRadiobuttonBlock1(dispersalSection.dataEntryFrame, "Locus controlling Maximum Dispersal Distance (meters):", 0, 93)
        self.GuiLifestageEntryBlock1(dispersalSection.dataEntryFrame, "Maximum Dispersal Distance for dominant phenotype:", "0.0-1000.0", "Sporophytes", 2, 94)
        self.GuiLifestageEntryBlock1(dispersalSection.dataEntryFrame, "Dispersal Distance Shape for dominant phenotype:", "0.0-100.0", "Sporophytes", 4, 95)
        self.GuiLifestageEntryBlock1(dispersalSection.dataEntryFrame, "Maximum Dispersal Distance for recessive phenotype:", "0.0-1000.0", "Sporophytes", 6, 96)
        self.GuiLifestageEntryBlock1(dispersalSection.dataEntryFrame, "Dispersal Distance Shape for recessive phenotype:", "0.0-100.0", "Sporophytes", 8, 97)
        Button(dispersalSection.dataEntryFrame, text='View dispersal distance distributions', command=(lambda: self.PlotDispersalCurve(94))).grid(row=10, column=0, columnspan=2, sticky=W)
        Message(frame, text=" ", width=650, font=("", 20, "")).pack(anchor=W) #Blank line
        bottomButtonSection = Frame(frame)
        bottomButtonSection.pack(side=BOTTOM, anchor=W)
        Button(bottomButtonSection, text="Reset Form", command=self.ResetForm, width=10).pack(side=LEFT)
        Button(bottomButtonSection, text="Open File", command=self.ReadParametersFromFile, width=10).pack(side=LEFT)
        Button(bottomButtonSection, text="Save", command=self.SaveParameters, width=7).pack(side=LEFT)
        Button(bottomButtonSection, text="Quit", command=frame.master.quit, width=7).pack(side=LEFT)

    def GuiTitle(self, frame, title, startRow):
        Label(frame, text=title, font=self.boldFont).grid(row=startRow, column=0, columnspan=12, sticky=W)

    def GuiDescription(self, frame, description, startRow):
        Message(frame, text=description, width=650).grid(row=startRow, column=0, columnspan=12, sticky=W)

    def GuiRadiobutton(self, frame, lifestage, rightmost, startRow, index):
        Label(frame, text=lifestage + ":").grid(row=startRow, column=0, sticky=E)
        if rightmost:
            Label(frame, text="(right-most locus)").grid(row=startRow, column=7, sticky=W)
        m_values[index] = IntVar()
        Radiobutton(frame, text="None", variable=m_values[index], value=0).grid(row=startRow, column=1)
        Radiobutton(frame, text="5", variable=m_values[index], value=5).grid(row=startRow, column=2)
        Radiobutton(frame, text="4", variable=m_values[index], value=4).grid(row=startRow, column=3)
        Radiobutton(frame, text="3", variable=m_values[index], value=3).grid(row=startRow, column=4)
        Radiobutton(frame, text="2", variable=m_values[index], value=2).grid(row=startRow, column=5)
        Radiobutton(frame, text="1", variable=m_values[index], value=1).grid(row=startRow, column=6)

    def GuiRadiobuttonBlock3(self, frame, title, startRow, index):
        for i in range(3):
            m_values.append(0)
        self.GuiTitle(frame, title, startRow)
        self.GuiRadiobutton(frame, "Spores", True, startRow+1, index)
        self.GuiRadiobutton(frame, "Gametophytes", False, startRow + 2, index + 1)
        self.GuiRadiobutton(frame, "Sporophytes", False, startRow + 3, index + 2)

    def GuiRadiobuttonBlock2(self, frame, title, description, startRow, index):
        for i in range(2):
            m_values.append(0)
        self.GuiTitle(frame, title, startRow)
        self.GuiDescription(frame, description, startRow + 1)
        self.GuiRadiobutton(frame, "Spores", True, startRow + 2, index)
        self.GuiRadiobutton(frame, "Gametophytes", False, startRow + 3, index + 1)

    def GuiRadiobuttonBlock1Description(self, frame, title, description, startRow, index):
        m_values.append(0)
        self.GuiTitle(frame, title, startRow)
        self.GuiDescription(frame, description, startRow + 1)
        self.GuiRadiobutton(frame, "Sporophytes", True, startRow + 2, index)

    def GuiRadiobuttonBlock1(self, frame, title, startRow, index):
        m_values.append(0)
        self.GuiTitle(frame, title, startRow)
        self.GuiRadiobutton(frame, "Sporophytes", True, startRow + 1, index)

    def GuiEntry(self, frame, limit, wide, startRow, index):
        Label(frame, text=limit, font=self.italicFont).grid(row=startRow, column=1, columnspan=11, sticky=W)
        m_values.append(Entry(frame, width=wide, justify=CENTER))
        m_values[index].grid(row=startRow, column=0)

    def GuiEntryBlock(self, frame, title, description, limit, wide, startRow, index):
        self.GuiTitle(frame, title, startRow)
        self.GuiDescription(frame, description, startRow + 1)
        self.GuiEntry(frame, limit, wide, startRow + 2, index)

    def GuiLifestageEntryBlock3Description(self, frame, title, description, limit, startRow, index):
        self.GuiTitle(frame, title, startRow)
        self.GuiDescription(frame, description, startRow + 1)
        Label(frame, text="Spores:").grid(row=startRow + 2, column=0, sticky=E)
        Label(frame, text="Gametophytes:").grid(row=startRow + 2, column=2, sticky=E)
        Label(frame, text="Sporophytes:").grid(row=startRow + 2, column=4, sticky=E)
        Label(frame, text=limit, font=self.italicFont).grid(row=startRow + 2, column=6, columnspan=11, sticky=W)
        m_values.append(Entry(frame, width=5, justify=CENTER))
        m_values[index].grid(row=startRow + 2, column=1, sticky=W)
        m_values.append(Entry(frame, width=5, justify=CENTER))
        m_values[index + 1].grid(row=startRow + 2, column=3, sticky=W)
        m_values.append(Entry(frame, width=5, justify=CENTER))
        m_values[index + 2].grid(row=startRow + 2, column=5, sticky=W)

    def GuiLifestageEntryBlock3(self, frame, title, limit, startRow, index):
        self.GuiTitle(frame, title, startRow)
        Label(frame, text="Spores:").grid(row=startRow + 1, column=0, sticky=E)
        Label(frame, text="Gametophytes:").grid(row=startRow + 1, column=2, sticky=E)
        Label(frame, text="Sporophytes:").grid(row=startRow + 1, column=4, sticky=E)
        Label(frame, text=limit, font=self.italicFont).grid(row=startRow + 1, column=6, columnspan=11, sticky=W)
        m_values.append(Entry(frame, width=5, justify=CENTER))
        m_values[index].grid(row=startRow + 1, column=1, sticky=W)
        m_values.append(Entry(frame, width=5, justify=CENTER))
        m_values[index + 1].grid(row=startRow + 1, column=3, sticky=W)
        m_values.append(Entry(frame, width=5, justify=CENTER))
        m_values[index + 2].grid(row=startRow + 1, column=5, sticky=W)

    def GuiLifestageEntryBlock2(self, frame, title, limit, startRow, index):
        self.GuiTitle(frame, title, startRow)
        Label(frame, text="Spores:").grid(row=startRow + 1, column=0, sticky=E)
        Label(frame, text="Gametophytes:").grid(row=startRow + 1, column=2, sticky=E)
        Label(frame, text=limit, font=self.italicFont).grid(row=startRow + 1, column=4, columnspan=11, sticky=W)
        m_values.append(Entry(frame, width=5, justify=CENTER))
        m_values[index].grid(row=startRow + 1, column=1, sticky=W)
        m_values.append(Entry(frame, width=5, justify=CENTER))
        m_values[index + 1].grid(row=startRow + 1, column=3, sticky=W)

    def GuiLifestageEntryBlock1Description(self, frame, title, description, limit, lifestage, startRow, index):
        self.GuiTitle(frame, title, startRow)
        self.GuiDescription(frame, description, startRow + 1)
        Label(frame, text=lifestage + ":").grid(row=startRow + 2, column=0, sticky=E)
        Label(frame, text=limit, font=self.italicFont).grid(row=startRow + 2, column=2, columnspan=11, sticky=W)
        m_values.append(Entry(frame, width=5, justify=CENTER))
        m_values[index].grid(row=startRow + 2, column=1, sticky=W)

    def GuiLifestageEntryBlock1(self, frame, title, limit, lifestage, startRow, index):
        self.GuiTitle(frame, title, startRow)
        Label(frame, text=lifestage + ":").grid(row=startRow + 1, column=0, sticky=E)
        Label(frame, text=limit, font=self.italicFont).grid(row=startRow + 1, column=2, columnspan=11, sticky=W)
        m_values.append(Entry(frame, width=5, justify=CENTER))
        m_values[index].grid(row=startRow + 1, column=1, sticky=W)


m_values = []
m_root = Tk()
m_app = App(m_root)
m_root.mainloop()
