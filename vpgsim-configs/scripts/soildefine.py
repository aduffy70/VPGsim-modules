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

# soildefine.py
# creates a list of soil values to be used by the vpgSoil module

import random
import tkFileDialog
import os
from optparse import OptionParser
from Tkinter import *
import tkFont


class GUIFramework(Frame):

    def __init__(self, master=None):
        #Define fonts (required after moving from 2.5 to 2.6?)
        #boldFont = tkFont.Font(size=14, weight='bold')
        #smallFont = tkFont.Font(size=10)
        #Setup the gui
        Frame.__init__(self, master)
        self.m_soilValues = []
        self.master.title("Soil Value Generator")
        self.grid(padx=10,pady=10)
        Label(self, text="Size of the block of opensim regions:", font=tkFont.Font(size=14, weight='bold')).grid(row=0, column=0, columnspan=3, sticky=NW, pady=5)
        Label(self, text="Regions wide (1-4):").grid(row=1, column=0, sticky=NW)
        Label(self, text="Regions high (1-3):").grid(row=2, column=0, sticky=NW)
        self.regionsWide = Entry(self, width=5, justify=CENTER)
        self.regionsWide.grid(row=1, column=1, sticky=W)
        self.regionsWide.insert(0,"1")
        self.regionsHigh = Entry(self, width=5, justify=CENTER)
        self.regionsHigh.insert(0, "1")
        self.regionsHigh.grid(row=2, column=1, sticky=W)
        Label(self, text="Automata Resolution:", font=tkFont.Font(size=14, weight='bold')).grid(row=3, column=0, columnspan=3, sticky=NW, pady=5)
        self.resolutionMultiplier = IntVar()
        Radiobutton(self, text="highest", variable=self.resolutionMultiplier, value=1).grid(row=4, column=0, columnspan=2, sticky=NW)
        Radiobutton(self, text="high", variable=self.resolutionMultiplier, value=2).grid(row=5, column=0, columnspan=2, sticky=NW)
        self.midRadioButton = Radiobutton(self, text="mid", variable=self.resolutionMultiplier, value=4)
        self.midRadioButton.select()
        self.midRadioButton.grid(row=6, column=0, columnspan=2, sticky=NW)
        Radiobutton(self, text="low", variable=self.resolutionMultiplier, value=8).grid(row=7, column=0, columnspan=2, sticky=NW)
        Radiobutton(self, text="lowest", variable=self.resolutionMultiplier, value=16).grid(row=8, column=0, columnspan=2, sticky=NW)
        self.canvas = Canvas(self)
        self.quitButton = Button(self, text="Quit", command=self.quit)
        self.quitButton.grid(row=30, column=2, sticky=SW, pady=5)
        self.generateButton = Button(self, text="Generate Soil Values", command=self.GenerateRandomMatrix)
        self.generateButton.grid(row=30, column=0, sticky=SW, columnspan=2, pady=5)

    def GenerateRandomMatrix(self):
        # Generate a matrix of random numbers between 0.0 and 1.0
        self.saveButton = Button(self, text = "Save", command=self.SplitFiles)
        self.saveButton.grid(row=30, column=8, sticky=SW)
        self.fileName = Entry(self, width=12, justify=LEFT)
        self.fileName.insert(0,"mysoilfile.txt")
        self.fileName.grid(row=30, column=5, columnspan=2, sticky=W)
        Label(self, text="File basename:", font=tkFont.Font(size=14, weight='bold')).grid(row=30, column=3, columnspan=2, sticky=E)
        Label(self, text="(One file will be saved for each region,", font=tkFont.Font(size=10)).grid(row=31, column = 3, columnspan = 3, sticky = SW)
        Label(self, text="numbered with an X,Y index)", font=tkFont.Font(size=10)).grid(row=32, column = 4, columnspan = 3, sticky = NW)
        resolutionMultiplier = self.resolutionMultiplier.get()
        self.height = int(self.regionsHigh.get())
        self.width = int(self.regionsWide.get())
        xMatrixSize = self.width * 256
        yMatrixSize = self.height * 256
        soilValues = []
        for x in range(0, (xMatrixSize / resolutionMultiplier) * (yMatrixSize / resolutionMultiplier)):
            soilValues.append(random.uniform(0.0, 1.0))
        self.m_soilValues = self.CellularAutomata(soilValues, xMatrixSize, yMatrixSize, resolutionMultiplier)
        self.PlotResult(self.m_soilValues, xMatrixSize, yMatrixSize)

    def CellularAutomata(self, soilValues, xMatrixSize, yMatrixSize, resolutionMultiplier):
        # Modify matrix values using a cellular automaton
        ySize = yMatrixSize / resolutionMultiplier
        xSize = xMatrixSize / resolutionMultiplier
        for i in range(random.randint(2,7)): #Run the automataton a random # of times
            newValues = []
            for y in range(ySize):
                if (y==0):
                    rowAbove = y+1
                    rowBelow = ySize - 1
                elif (y== ySize - 1):
                    rowAbove = 0
                    rowBelow = y-1
                else:
                    rowAbove = y+1
                    rowBelow = y-1
                for x in range(xSize):
                    if (x==0):
                        columnRight = x+1
                        columnLeft = xSize - 1
                    elif (x == xSize - 1):
                        columnRight = 0
                        columnLeft = x-1
                    else:
                        columnRight = x+1
                        columnLeft = x-1
                    neighborAverage = (soilValues[rowBelow * xSize + columnLeft] + soilValues[y * xSize + columnLeft] + soilValues[rowAbove * xSize + columnLeft] + soilValues[rowBelow * xSize + x] + soilValues[rowAbove * xSize + x] + soilValues[rowBelow * xSize + columnRight] + soilValues[y * xSize + columnRight] + soilValues[rowAbove * xSize + columnRight] + soilValues[y * xSize + x]) / 9
                    newValues.append((neighborAverage + 0.175) % 1.0)
            soilValues = newValues[:]
        fullSizeArray = []
        for y in range(ySize):
            fullSizeRow = []
            for x in range(xSize):
                for repeat in range(resolutionMultiplier):
                    fullSizeArray.append(soilValues[y * xSize + x])
                    fullSizeRow.append(soilValues[y * xSize + x])
            for repeat in range(resolutionMultiplier - 1):
                fullSizeArray.extend(fullSizeRow)
        return fullSizeArray

    def SaveResults(self, soilValues, fileName):
        #Save the values to a file
        file = open(fileName, 'w')
        for value in soilValues:
            writeValue = str(value) + '\n'
            file.write(writeValue)
        file.close()

    def PlotResult(self, soilValues, xMatrixSize, yMatrixSize):
        #Provide a graphical visualization of the values in greyscale
        self.canvas.grid_forget()
        self.canvas = Canvas(self, width = xMatrixSize + 5, height = yMatrixSize + 5)
        self.canvas.grid(row=0, column=4, columnspan = 30, rowspan=30)
        for y in range(yMatrixSize):
            for x in range(xMatrixSize):
                soilValue = soilValues[y * xMatrixSize + x] * 256
                color = '#%02x%02x%02x' % tuple((soilValue,soilValue,soilValue))
                self.canvas.create_rectangle(x, y, x+1, y+1, fill=color, outline=color)

    def SplitFiles(self):
        #Divide the dataset into individual files for each region
        #Also, ask what directory to save into
        dirname = tkFileDialog.askdirectory(parent=self, initialdir="~", title='Please select a directory')
        for yBlock in range(self.height):
            for xBlock in range(self.width):
                blockValues = []
                for y in range(256 * yBlock, 256 * yBlock + 256):
                    for x in range(256 * xBlock, 256 * xBlock + 256):
                        blockValues.append(self.m_soilValues[y * (self.width * 256) + x])
                self.SaveResults(blockValues, os.path.normpath(dirname + "/" + str(xBlock) + str(yBlock) + self.fileName.get()))


if __name__ == "__main__":
    guiFrame = GUIFramework()
    guiFrame.mainloop()
