// AtypicalFernLifecycleDemo.lsl
// VPGsim Project license applies (http://fernseed.usu.edu)

// This script controls a demonstration of an asexual (gemmae-based) fern lifecycle

string sporeTex = "5748decc-f629-461c-9a36-a35a221fe21f";
string gametTex = "043e9b16-def0-48ce-b391-19ea0be72807";
string sporoTex = "cd5457ad-f050-4b6e-b08a-e24dabee62ae";
string invisTex = "00000000-0000-2222-3333-100000001007";
integer whichSide = 0; //Are we on the first (0) or second (1) side of the display?
integer touchCount = 0; //Track how many times we've been touched
integer waitingForTimer = 0; //So rapid touch events won't confuse the script
//Link numbers so changing links doesn't kill the script
integer dispersed1;
integer dispersed2;
integer gametophyte1a;
integer gametophyte1b;
integer gametophyte2a;
integer gametophyte2b;
integer gemma1a;
integer gemma1b;
integer gemma1c;
integer gemma1d;
integer gemma1e;
integer gemma1f;
integer gemma1g;
integer gemma1h;
integer gemma1i;
integer gemma1j;
integer gemma2a;
integer gemma2b;
integer gemma2c;
integer gemma2d;
integer gemma2e;
integer gemma2f;
integer gemma2g;
integer gemma2h;
integer gemma2i;
integer gemma2j;

GetLinkNumbers()
{
    integer linkedPrims = llGetNumberOfPrims();
    integer i = 0;
    for (; i<=linkedPrims; i++)
    {
        string currentLinkName = llGetLinkName(i);
        //llSay(0, currentLinkName + " " + (string)i);
        if (currentLinkName == "Dispersed1")
            dispersed1 = i;
        else if (currentLinkName == "Dispersed2")
            dispersed2 = i;
        else if (currentLinkName == "Gametophyte1a")
            gametophyte1a = i;
        else if (currentLinkName == "Gametophyte1b")
            gametophyte1b = i;
        else if (currentLinkName == "Gametophyte2a")
            gametophyte2a = i;
        else if (currentLinkName == "Gametophyte2b")
            gametophyte2b = i;
        else if (currentLinkName == "Gemma1a")
            gemma1a = i;
        else if (currentLinkName == "Gemma1b")
            gemma1b = i;
        else if (currentLinkName == "Gemma1c")
            gemma1c = i;
        else if (currentLinkName == "Gemma1d")
            gemma1d = i;
        else if (currentLinkName == "Gemma1e")
            gemma1e = i;
        else if (currentLinkName == "Gemma1f")
            gemma1f = i;
        else if (currentLinkName == "Gemma1g")
            gemma1g = i;
        else if (currentLinkName == "Gemma1h")
            gemma1h = i;
        else if (currentLinkName == "Gemma1i")
            gemma1i = i;
        else if (currentLinkName == "Gemma1j")
            gemma1j = i;
        else if (currentLinkName == "Gemma2a")
            gemma2a = i;
        else if (currentLinkName == "Gemma2b")
            gemma2b = i;
        else if (currentLinkName == "Gemma2c")
            gemma2c = i;
        else if (currentLinkName == "Gemma2d")
            gemma2d = i;
        else if (currentLinkName == "Gemma2e")
            gemma2e = i;
        else if (currentLinkName == "Gemma2f")
            gemma2f = i;
        else if (currentLinkName == "Gemma2g")
            gemma2g = i;
        else if (currentLinkName == "Gemma2h")
            gemma2h = i;
        else if (currentLinkName == "Gemma2i")
            gemma2i = i;
        else if (currentLinkName == "Gemma2j")
            gemma2j = i;
    }
}

GemmaeGrowth()
{
    llSay(0, "The gemma grows into a gametophyte.\nThe gametophyte may produce sperm and/or eggs,\nbut fertilization does not occur or the resulting sporophyte is not viable.\n  ");
    if (whichSide == 0)
    {
        llSetLinkTexture(dispersed1, invisTex, 0); //Dispersed gemma
        llSetLinkTexture(gametophyte1a, gametTex, 0); //Gametophyte
        llSetLinkTexture(gametophyte1b, gametTex, 0);
    }
    else
    {
        llSetLinkTexture(dispersed2, invisTex, 0); //Dispersed gemma
        llSetLinkTexture(gametophyte2a, gametTex, 0); //Gametophyte
        llSetLinkTexture(gametophyte2b, gametTex, 0);
    }
}

GemmaeProduction()
{
    llSay(0, "The gametophyte produces multicellular buds or gemmae.\nEach gemma is genetically identical to the gametophyte.\n  ");
    if (whichSide == 0)
    {
        llSetLinkTexture(gemma1a, gametTex, 0); //Gemmae
        llSetLinkTexture(gemma1b, gametTex, 0);
        llSetLinkTexture(gemma1c, gametTex, 0);
        llSetLinkTexture(gemma1d, gametTex, 0);
        llSetLinkTexture(gemma1e, gametTex, 0);
        llSetLinkTexture(gemma1f, gametTex, 0);
        llSetLinkTexture(gemma1g, gametTex, 0);
        llSetLinkTexture(gemma1h, gametTex, 0);
        llSetLinkTexture(gemma1i, gametTex, 0);
        llSetLinkTexture(gemma1j, gametTex, 0);
    }
    else
    {
        llSetLinkTexture(gemma2a, gametTex, 0); //Gemmae
        llSetLinkTexture(gemma2b, gametTex, 0);
        llSetLinkTexture(gemma2c, gametTex, 0);
        llSetLinkTexture(gemma2d, gametTex, 0);
        llSetLinkTexture(gemma2e, gametTex, 0);
        llSetLinkTexture(gemma2f, gametTex, 0);
        llSetLinkTexture(gemma2g, gametTex, 0);
        llSetLinkTexture(gemma2h, gametTex, 0);
        llSetLinkTexture(gemma2i, gametTex, 0);
        llSetLinkTexture(gemma2j, gametTex, 0);
    }
}

GemmaeDispersal()
{
    llSay(0, "The gemma are easily broken off and dispersed over short distances\n  ");
    if (whichSide == 0)
    {
        llSetLinkTexture(dispersed2, gametTex, 0); //Dispersed gemma
        llSetLinkTexture(gemma1b, invisTex, 0); //Make some of the gemmae disappear
        llSetLinkTexture(gemma1d, invisTex, 0);
        llSetLinkTexture(gemma1f, invisTex, 0);
        llSetLinkTexture(gemma1g, invisTex, 0);
    }
    else
    {
        llSetLinkTexture(dispersed1, gametTex, 0); //Dispersed gemma
        llSetLinkTexture(gemma2b, invisTex, 0); //Make some of the gemmae disappear
        llSetLinkTexture(gemma2d, invisTex, 0);
        llSetLinkTexture(gemma2f, invisTex, 0);
        llSetLinkTexture(gemma2g, invisTex, 0);
    }
    waitingForTimer = 1;
    llSetTimerEvent(2);
}

default
{
    on_rez(integer num)
    {
        llResetScript();
    }

    state_entry() //Set Dispersed gemma 1 visible
    {
        GetLinkNumbers();
        llSetText("Touch to step through\nan example of an\natypical fern lifecycle", <1,1,1>, 1);
        llSay(0, "A gemma lands on suitable substrate.\n  ");
        llSetLinkTexture(dispersed1, gametTex, 0); //Dispersed Gemmae
        llSetLinkTexture(dispersed2, invisTex, 0);
        llSetLinkTexture(gametophyte2a, invisTex, 0); //Gametophytes
        llSetLinkTexture(gametophyte2b, invisTex, 0);
        llSetLinkTexture(gametophyte1a, invisTex, 0);
        llSetLinkTexture(gametophyte1b, invisTex, 0);
        llSetLinkTexture(gemma2a, invisTex, 0); //Gemmae
        llSetLinkTexture(gemma2b, invisTex, 0);
        llSetLinkTexture(gemma2c, invisTex, 0);
        llSetLinkTexture(gemma2d, invisTex, 0);
        llSetLinkTexture(gemma2e, invisTex, 0);
        llSetLinkTexture(gemma2f, invisTex, 0);
        llSetLinkTexture(gemma2g, invisTex, 0);
        llSetLinkTexture(gemma2h, invisTex, 0);
        llSetLinkTexture(gemma2i, invisTex, 0);
        llSetLinkTexture(gemma2j, invisTex, 0);
        llSetLinkTexture(gemma1a, invisTex, 0);
        llSetLinkTexture(gemma1b, invisTex, 0);
        llSetLinkTexture(gemma1c, invisTex, 0);
        llSetLinkTexture(gemma1d, invisTex, 0);
        llSetLinkTexture(gemma1e, invisTex, 0);
        llSetLinkTexture(gemma1f, invisTex, 0);
        llSetLinkTexture(gemma1g, invisTex, 0);
        llSetLinkTexture(gemma1h, invisTex, 0);
        llSetLinkTexture(gemma1i, invisTex, 0);
        llSetLinkTexture(gemma1j, invisTex, 0);
    }

    touch_start(integer num)
    {
        if (!waitingForTimer)
        {
            if (touchCount == 0)
            {
                touchCount = 1;
                GemmaeGrowth();
            }
            else if (touchCount == 1)
            {
                touchCount = 2;
                GemmaeProduction();
            }
            else if (touchCount == 2)
            {
                touchCount = 0;
                GemmaeDispersal();
                if (whichSide == 0)
                {
                    whichSide = 1;
                }
                else
                {
                    whichSide = 0;
                }
            }
            else
            {
                llSay(0, "Something has gone horribly wrong!");
            }
        }
    }


    timer()
    {
        llSetTimerEvent(0);
        llSay(0, "The gametophyte eventually dies,\nthough they may be long-lived and form large clonal colonies.\n  ");
        if (whichSide == 0)
        {
            llSetLinkTexture(gametophyte2a, invisTex, 0); //Gametophytes
            llSetLinkTexture(gametophyte2b, invisTex, 0);
            llSetLinkTexture(gemma2a, invisTex, 0); //Gemmae
            llSetLinkTexture(gemma2c, invisTex, 0);
            llSetLinkTexture(gemma2e, invisTex, 0);
            llSetLinkTexture(gemma2f, invisTex, 0);
            llSetLinkTexture(gemma2h, invisTex, 0);
            llSetLinkTexture(gemma2i, invisTex, 0);
            llSetLinkTexture(gemma2j, invisTex, 0);
        }
        else
        {
            llSetLinkTexture(gametophyte1a, invisTex, 0);
            llSetLinkTexture(gametophyte1b, invisTex, 0);
            llSetLinkTexture(gemma1a, invisTex, 0); //Gemmae
            llSetLinkTexture(gemma1c, invisTex, 0);
            llSetLinkTexture(gemma1e, invisTex, 0);
            llSetLinkTexture(gemma1f, invisTex, 0);
            llSetLinkTexture(gemma1h, invisTex, 0);
            llSetLinkTexture(gemma1i, invisTex, 0);
            llSetLinkTexture(gemma1j, invisTex, 0);
        }
        waitingForTimer = 0;
    }

}