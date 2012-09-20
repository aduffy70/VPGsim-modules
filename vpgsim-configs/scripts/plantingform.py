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

#plantingform.py
#Creates a command string that can be chatted inworld to start a population.
#This script performs the same function as the online webform (at
#http://fernseed.usu.edu/planting-form), but locally.
#Tested with python v2.5.1 & v2.6.5

import Tkinter
import tkFont
from Tkinter import W, END, LEFT, VERTICAL, HORIZONTAL, CENTER


class ExpandableSubsection(Tkinter.Frame):
    """Defines an expandable subsection of the form."""

    def __init__(self, parent=None, section_title="Default Section Title"):
        Tkinter.Frame.__init__(self, parent)
        show_hide_button = Tkinter.Button(self, text='+', width=1, height=1,
                                          command=lambda: self.hide_data_frame(
                                          show_hide_button, self.data_frame))
        show_hide_button.grid(row=0, column=0)
        Tkinter.Label(self, text=section_title, font=tkFont.Font(
                      weight='bold'), fg='darkblue').grid(row=0,
                      column=1, sticky=W)
        self.data_frame = Tkinter.Frame(self)
        self.hidden = True

    def hide_data_frame(self, button, data_frame):
        """Button expands and collapses the subsection."""
        if self.hidden:
            button.config(text='-')
            self.hidden = False
            data_frame.grid(row=1, column=1)
        else:
            button.config(text="+")
            self.hidden = True
            data_frame.grid_forget()
        m_app.outer_frame.update_idletasks()
        m_app.canvas.configure(scrollregion=(0, 0,
                               m_app.outer_frame.winfo_width(),
                               m_app.outer_frame.winfo_height()))


class App:
    """Defines the form and methods to process the data collected"""

    def __init__(self, master):
        """Creates a scrollable canvas containing a frame with widgets."""
        #Define fonts
        #TODO use the system default size +1 for bold and -1 for italic
        #Currently bold, italic, & normal font size may be different if the
        #user is not using their operating system's defaults.  For bold
        #font this is acceptable, but italics look out of place so I am
        #using normal font instead.
        self.bold_font = tkFont.Font(weight='bold')
        master.grid_rowconfigure(0, weight=1)
        master.grid_columnconfigure(0, weight=1)
        master.title("Planting Form")
        self.canvas = Tkinter.Canvas(master, width=600, height=700)
        self.canvas.grid(row=0, column=0, sticky='nswe', padx=15)
        vScroll = Tkinter.Scrollbar(master, orient=VERTICAL,
                                    command=self.canvas.yview)
        vScroll.grid(row=0, column=1, sticky='ns')
        hScroll = Tkinter.Scrollbar(master, orient=HORIZONTAL,
                                    command=self.canvas.xview)
        hScroll.grid(row=1, column=0, sticky='we')
        self.canvas.configure(xscrollcommand=hScroll.set,
                              yscrollcommand=vScroll.set)
        self.outer_frame = Tkinter.Frame(self.canvas)
        self.canvas.create_window(0, 0, window=self.outer_frame, anchor='nw')
        #Add the rest of the items to the form
        self.add_widgets(self.outer_frame)
        self.reset_form()
        #Grab the new size of the frame so the scrollbars work correctly
        self.outer_frame.update_idletasks()
        self.canvas.configure(scrollregion=(0, 0,
                              self.outer_frame.winfo_width(),
                              self.outer_frame.winfo_height()))

    def add_widgets(self, frame):
        """Adds buttons and labels to the frame."""
        #Form header
        Tkinter.Message(frame, text="This form generates virtual plants" +
                        " in a simulated population of ferns growing in" +
                        " a 3D virtual environment.  Changes made here" +
                        " will not take effect until they are enabled from" +
                        " within that environment.", width=550,
                        font=self.bold_font).pack(anchor=W)
        #Reset, submit, & quit buttons
        top_button_section = Tkinter.Frame(frame)
        top_button_section.pack(anchor=W)
        Tkinter.Button(top_button_section, text="Reset Form",
                       command=self.reset_form, width=10).pack(side=LEFT)
        Tkinter.Button(top_button_section, text="Submit",
                       command=self.validate_form_data, width=7).pack(side=LEFT)
        Tkinter.Button(top_button_section, text="Quit",
                       command=frame.master.quit, width=7).pack(
                       side=LEFT, padx=50)
        #Genotype subsection
        genotype_section = ExpandableSubsection(frame, "Genotype")
        genotype_section.pack(anchor=W)
        self.insert_genotype_fields(genotype_section.data_frame, 0)
        #Range subsection
        range_section = ExpandableSubsection(frame, "Range")
        range_section.pack(anchor=W)
        self.insert_range_fields(range_section.data_frame, 0)
        #Quantity subsection
        qty_section = ExpandableSubsection(frame, "Quantity")
        qty_section.pack(anchor=W)
        self.insert_qty_field(qty_section.data_frame, 0)
        #Retry section
        retry_section = ExpandableSubsection(frame, "Retries")
        retry_section.pack(anchor=W)
        self.insert_retry_field(retry_section.data_frame, 0)
        #Message block to hold error messages and planting instructions
        self.output_block = Tkinter.Message(frame, text="",width=550,
                                            font=tkFont.Font(weight='bold'))
        #Form field to show the command string
        self.command_block = Tkinter.Entry(frame, width=33, justify=CENTER,
                                            font = tkFont.Font(weight='bold'))

    def insert_description(self, frame, description, row_start,
                           col_start, width=1, height=1):
        """Inserts text with default format into the specified frame at the
        specified starting row and column and extending for width and height.
        Uses grid.
        """
        Tkinter.Message(frame, text=description, width=550).grid(row=row_start,
                        column=col_start, columnspan=width, rowspan=height,
                        sticky=W)

    def insert_genotype_fields(self, frame, row_start):
        """Inserts the form fields and labels for Genotype."""
        self.insert_description(frame, "Specify the genotypes to be" +
                                " generated.", row_start, 1, 15, 1)
        self.insert_description(frame, "Haplotype 1", row_start + 1, 3, 5, 1)
        #Spacer columns to spread things out...
        self.insert_description(frame, "   ", row_start + 1, 8, 1, 1)
        self.insert_description(frame, "   ", row_start + 1, 1, 1, 1)
        self.insert_description(frame, "Haplotype 2", row_start + 1, 9, 5, 1)
        self.insert_description(frame, "Leave\nHaplotype 2\nblank for\nspores",
                                row_start + 4, 14, 1, 3)
        self.insert_description(frame, "Locus:", row_start + 2, 2, 1, 1)
        self.insert_description(frame, "Dominant(1):", row_start + 3, 2, 1, 1)
        self.insert_description(frame, "Recessive(0):", row_start + 4, 2, 1, 1)
        self.insert_description(frame, "Randomize:", row_start + 5, 2, 1, 1)
        self.insert_description(frame, "Blank:", row_start + 6, 2, 1, 1)
        #Radiobuttons for each locus in both haplotypes
        for i in range(5,0, -1):
            m_haplotype1[i-1] = Tkinter.StringVar()
            m_haplotype2[i-1] = Tkinter.StringVar()
            m_haplotype1[i-1].set('r')
            m_haplotype2[i-1].set('b')
            self.insert_description(frame, str(i), row_start + 2, 8-i, 1, 1)
            self.insert_description(frame, str(i), row_start + 2, 14-i, 1, 1)
            for j in range(row_start + 3, row_start + 7):
                button_value = ''
                both_haplotypes = True
                if (j==3):
                    button_value = '1'
                elif (j==4):
                    button_value = '0'
                elif (j==5):
                    button_value = 'r'
                else:
                    button_value = 'b'
                    both_haplotypes = False
                if (both_haplotypes):
                    Tkinter.Radiobutton(frame, variable=m_haplotype1[i-1],
                                        value=button_value).grid(row=j,
                                        column=8-i)
                Tkinter.Radiobutton(frame, variable=m_haplotype2[i-1],
                                    value=button_value).grid(row=j,
                                    column=14-i)

    def insert_qty_field(self, frame, row_start):
        """Inserts the form field and labels for Quantity."""
        self.insert_description(frame, "Specify the number of spores or" +
                                " sporophytes to generate.",
                                row_start, 1, 15, 1)
        self.insert_description(frame, "Qty:", row_start + 2, 2)
        self.insert_description(frame, "(1-500)", row_start + 2, 4)
        m_qty_retry[0] = Tkinter.Entry(frame, width=4, justify=CENTER)
        m_qty_retry[0].grid(row=row_start + 2, column=3)

    def insert_range_fields(self, frame, row_start):
        """Inserts the form fields and labels for Range."""
        self.insert_description(frame, "Specify the range (in region" +
                                " coordinates) where the plants should" +
                                " be generated.", row_start, 1, 15, 1)
        self.insert_description(frame, "X Range:", row_start + 2, 2)
        self.insert_description(frame, "Y Range:", row_start + 3, 2)
        self.insert_description(frame, "    -", row_start + 2, 4)
        self.insert_description(frame, "    -", row_start + 3, 4)
        self.insert_description(frame, "(0-256)", row_start + 2, 6)
        self.insert_description(frame, "(0-256)", row_start + 3, 6)
        #XMin
        m_range[0] = Tkinter.Entry(frame, width=4, justify=CENTER)
        m_range[0].grid(row=row_start + 2, column=3)
        #XMax
        m_range[1] = Tkinter.Entry(frame, width=4, justify=CENTER)
        m_range[1].grid(row=row_start + 2, column=5)
        #YMin
        m_range[2] = Tkinter.Entry(frame, width=4, justify=CENTER)
        m_range[2].grid(row=row_start + 3, column=3)
        #YMax
        m_range[3] = Tkinter.Entry(frame, width=4, justify=CENTER)
        m_range[3].grid(row=row_start + 3, column=5)

    def insert_retry_field(self, frame, row_start):
        """Inserts the form field and label for Retries."""
        self.insert_description(frame, "When a randomly selected location" +
                                " is underwater nothing will be planted." +
                                " To ensure that the specified number of" +
                                " plants is generated, click here.",
                                row_start, 1, 15, 1)
        self.insert_description(frame, "Retry to reach exact quantity:",
                                row_start + 2, 2)
        m_qty_retry[1] = Tkinter.IntVar()
        Tkinter.Checkbutton(frame, variable=m_qty_retry[1]).grid(
                            row=row_start + 2, column = 3)

    def process_form_data(self, genotype_pattern, int_range, qty, retry):
        """Generates a command string from the submitted form data."""
        lifestage = "spores"
        if (len(genotype_pattern) > 5):
            lifestage = "sporophytes"
        instructions = "The %s are ready to load.\n" % lifestage + \
                        "To generate the population:\n" + \
                        "\tMove your avatar into the desired region.\n" + \
                        "\tPaste the following text into the chat window:"
        self.output_block['text'] = instructions
        self.output_block['fg'] = 'Black'
        self.output_block.pack()
        command_string = "/4 %s,%s,%s,%s,%s,%s,%s" % (genotype_pattern,
                         int_range[0], int_range[1], int_range[2], int_range[3],
                         qty, retry)
        self.command_block.delete(0, END)
        self.command_block.insert(0, command_string)
        self.command_block.pack()

    def reset_form(self):
        """Sets all form widgets to their default values."""
        default_haplotype1 = ['r', 'r', 'r', 'r', 'r']
        default_haplotype2 = ['b', 'b', 'b', 'b', 'b']
        default_range = [0, 256, 0, 256]
        default_quantity = 1
        for i in range(5):
            m_haplotype1[i].set(default_haplotype1[i])
            m_haplotype2[i].set(default_haplotype2[i])
        for i in range(4):
            m_range[i].delete(0, END)
            m_range[i].insert(0, default_range[i])
        m_qty_retry[0].delete(0, END)
        m_qty_retry[0].insert(0, default_quantity)
        m_qty_retry[1].set(1)
        self.output_block.pack_forget()
        self.command_block.pack_forget()

    def validate_form_data(self):
        """
        Validates form data and converts it to the types needed for
        processing.
        """
        error_string = ""
        error_status = False
        #Haplotype 2 must have ALL 'b's or NO b's
        blank_count = 0
        #Store hap1 to a genotype string
        genotype_pattern = ""
        for locus in m_haplotype1:
            genotype_pattern += locus.get()
        for locus in m_haplotype2:
            if (locus.get() == 'b'):
                blank_count += 1
            else:
                #Store all non-'b' alleles to a genotype string
                genotype_pattern += locus.get()
        if (blank_count > 0) and (blank_count < 5):
            error_status = True
            error_string += "Error: Allele not specified for some loci" + \
                            " of Haplotype 2!\n"
        #All range values must be specified, non-negative integers, and <=256
        int_range = []
        for coordinate in m_range:
            if (coordinate.get().isdigit()):
                int_range.append(int(coordinate.get()))
                if (int_range[-1] > 256):
                    error_status = True
                    error_string += "Error: Coordinate is out of range!\n"
            else:
                error_status = True
                error_string += "Error: Coordinate is not a" + \
                                " positive integer!\n"
        #Min range coordinates must be less than Max range coordinates
        if (not error_status):
            if ((int_range[0] >= int_range[1]) or
                (int_range[2] >= int_range[3])):
                error_status = True
                error_string += "Error: Invalid range!\n"
        #Qty must be specified, non-negative and <=500
        qty = 1
        if (m_qty_retry[0].get().isdigit()):
            qty = int(m_qty_retry[0].get())
            if (qty > 500):
                error_status = True
                error_string += "Error: Qty too large!\n"
        else:
            error_status = True
            error_string += "Error: Qty is not a positive integer!\n"
        #Retry is from a checkbox so it will always be valid
        retry = m_qty_retry[1].get()
        #Clear any existing error or command block
        self.output_block.pack_forget()
        self.command_block.pack_forget()
        if (error_status):
            self.output_block['fg'] = 'Red'
            self.output_block['text'] = error_string
            self.output_block.pack()
        else:
            self.process_form_data(genotype_pattern, int_range, qty, retry)
        #Reset the canvas so the scrollbars adjust to the new form length
        self.outer_frame.update_idletasks()
        self.canvas.configure(scrollregion=(0, 0,
                              self.outer_frame.winfo_width(),
                              self.outer_frame.winfo_height()))


#I think these values will always get overwritten in App.reset_form(), but
#when I left the lists empty (or tried to use discrete variables instead of
#lists I was fighting issues with types.
m_haplotype1 = ['r', 'r', 'r', 'r', 'r']
m_haplotype2 = ['b', 'b', 'b', 'b', 'b']
#XMin, XMax, YMin, YMax
m_range = [0, 256, 0, 256]
m_qty_retry = [1, True]
m_root = Tkinter.Tk()
m_app = App(m_root)
m_root.mainloop()
