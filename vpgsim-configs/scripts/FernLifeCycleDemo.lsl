// FernLifecycleDemo.lsl
// VPGsim Project license applies (http://fernseed.usu.edu)

//This script controls a demo of the typical fern lifecycle

string sporeTex = "5748decc-f629-461c-9a36-a35a221fe21f";
string gametTex = "043e9b16-def0-48ce-b391-19ea0be72807";
string sporoTex = "cd5457ad-f050-4b6e-b08a-e24dabee62ae";
string invisTex = "00000000-0000-2222-3333-100000001007";
integer whichSide = 0; //Are we on the first (0) or second (1) side of the display?
integer timerType = 0; //Is the SexualReproduction (0) or the Sporulation (1) function calling the timer event?
integer touchCount = 0; //Track how many times we've been touched
integer waitingForTimer = 0; //So rapid touch events don't confuse the script
//Link Numbers: (so if they change later, it won't kill the script)
integer spore1;
integer spore2;
integer sporophyte1a;
integer sporophyte1b;
integer sporophyte1c;
integer sporophyte1d;
integer sporophyte2a;
integer sporophyte2b;
integer sporophyte2c;
integer sporophyte2d;
integer gametophyte1a;
integer gametophyte1b;
integer gametophyte2a;
integer gametophyte2b;


GetLinkNumbers()
{
    integer linkedPrims = llGetNumberOfPrims();
    integer i = 0;
    for (; i<=linkedPrims; i++)
    {
        string currentLinkName = llGetLinkName(i);
        //llSay(0, currentLinkName + " " + (string)i);
        if (currentLinkName == "Spore1")
            spore1 = i;
        else if (currentLinkName == "Spore2")
            spore2 = i;
        else if (currentLinkName == "Sporophyte1a")
            sporophyte1a = i;
        else if (currentLinkName == "Sporophyte1b")
            sporophyte1b = i;
        else if (currentLinkName == "Sporophyte1c")
            sporophyte1c = i;
        else if (currentLinkName == "Sporophyte1d")
            sporophyte1d = i;
        else if (currentLinkName == "Sporophyte2a")
            sporophyte2a = i;
        else if (currentLinkName == "Sporophyte2b")
            sporophyte2b = i;
        else if (currentLinkName == "Sporophyte2c")
            sporophyte2c = i;
        else if (currentLinkName == "Sporophyte2d")
            sporophyte2d = i;
        else if (currentLinkName == "Gametophyte1a")
            gametophyte1a = i;
        else if (currentLinkName == "Gametophyte1b")
            gametophyte1b = i;
        else if (currentLinkName == "Gametophyte2a")
            gametophyte2a = i;
        else if (currentLinkName == "Gametophyte2b")
            gametophyte2b = i;
    }
}

Germination()
{
    llSay(0, "The spore grows into a gametophyte.\nThe gametophytes produce sperm and/or eggs.\n  ");
    if (whichSide == 0)
    {
        llSetLinkTexture(spore2, invisTex, 0); //Spore
        llSetLinkTexture(gametophyte2a, gametTex, 0); //Gametophyte
        llSetLinkTexture(gametophyte2b, gametTex, 0);
    }
    else
    {
        llSetLinkTexture(spore1, invisTex, 0); //Spore
        llSetLinkTexture(gametophyte1a, gametTex, 0); //Gametophyte
        llSetLinkTexture(gametophyte1b, gametTex, 0);
    }
}

SexualReproduction()
{
    llSay(0, "If moisture is available, sperm swim to the egg, and a sporophyte is produced\nThe sporophyte is a new separate individual.\n  ");
    if (whichSide == 0)
    {
        llSetLinkTexture(sporophyte2a, sporoTex, 1); //Sporophyte
        llSetLinkTexture(sporophyte2a, sporoTex, 3);
        llSetLinkTexture(sporophyte2b, sporoTex, 1);
        llSetLinkTexture(sporophyte2b, sporoTex, 3);
        llSetLinkTexture(sporophyte2c, sporoTex, 1);
        llSetLinkTexture(sporophyte2c, sporoTex, 3);
        llSetLinkTexture(sporophyte2d, sporoTex, 1);
        llSetLinkTexture(sporophyte2d, sporoTex, 3);
    }
    else
    {
        llSetLinkTexture(sporophyte1a, sporoTex, 1); //Sporophyte
        llSetLinkTexture(sporophyte1a, sporoTex, 3);
        llSetLinkTexture(sporophyte1b, sporoTex, 1);
        llSetLinkTexture(sporophyte1b, sporoTex, 3);
        llSetLinkTexture(sporophyte1c, sporoTex, 1);
        llSetLinkTexture(sporophyte1c, sporoTex, 3);
        llSetLinkTexture(sporophyte1d, sporoTex, 1);
        llSetLinkTexture(sporophyte1d, sporoTex, 3);
    }
    timerType = 0;
    waitingForTimer = 1;
    llSetTimerEvent(2);
}

Sporulation()
{
    llSay(0, "The Sporophyte produces numerous spores through meiosis.\nThese spores are new separate individuals and are dispersed by the wind.\n  ");
    if (whichSide == 0)
    {
        llSetLinkTexture(spore1, sporeTex, 0); //Spore
    }
    else
    {
        llSetLinkTexture(spore2, sporeTex, 0); //Spore
    }
    timerType = 1;
    waitingForTimer = 1;
    llSetTimerEvent(2);
}

default
{
    on_rez(integer num)
    {
        llResetScript();
    }

    state_entry() //Set spore 1 visible
    {
        GetLinkNumbers();
        llSetText("Touch to step through\nthe lifecycle", <1,1,1>, 1);
        llSay(0, "A spore lands on soil.\n ");
        llSetLinkTexture(spore2, sporeTex, 0); //Spores
        llSetLinkTexture(spore1, invisTex, 0);
        llSetLinkTexture(gametophyte2a, invisTex, 0); //Gametophytes
        llSetLinkTexture(gametophyte2b, invisTex, 0);
        llSetLinkTexture(gametophyte1a, invisTex, 0);
        llSetLinkTexture(gametophyte1b, invisTex, 0);
        llSetLinkTexture(sporophyte2a, invisTex, 1); //Sporophytes
        llSetLinkTexture(sporophyte2a, invisTex, 3);
        llSetLinkTexture(sporophyte2b, invisTex, 1);
        llSetLinkTexture(sporophyte2b, invisTex, 3);
        llSetLinkTexture(sporophyte2c, invisTex, 1);
        llSetLinkTexture(sporophyte2c, invisTex, 3);
        llSetLinkTexture(sporophyte2d, invisTex, 1);
        llSetLinkTexture(sporophyte2d, invisTex, 3);
        llSetLinkTexture(sporophyte1a, invisTex, 1);
        llSetLinkTexture(sporophyte1a, invisTex, 3);
        llSetLinkTexture(sporophyte1b, invisTex, 1);
        llSetLinkTexture(sporophyte1b, invisTex, 3);
        llSetLinkTexture(sporophyte1c, invisTex, 1);
        llSetLinkTexture(sporophyte1c, invisTex, 3);
        llSetLinkTexture(sporophyte1d, invisTex, 1);
        llSetLinkTexture(sporophyte1d, invisTex, 3);
    }

    touch_start(integer num)
    {
        if (!waitingForTimer)
        {
            if (touchCount == 0)
            {
                touchCount = 1;
                Germination();
            }
            else if (touchCount == 1)
            {
                touchCount = 2;
                SexualReproduction();
            }
            else if (touchCount == 2)
            {
                touchCount = 0;
                Sporulation();
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
        if (timerType == 0)
        {
            llSay(0, "The gametophyte eventually dies.\n ");
            if (whichSide == 0)
            {
                llSetLinkTexture(gametophyte2a, invisTex, 0); //Gametophytes
                llSetLinkTexture(gametophyte2b, invisTex, 0);

            }
            else
            {
                llSetLinkTexture(gametophyte1a, invisTex, 0);
                llSetLinkTexture(gametophyte1b, invisTex, 0);
            }
        }
        else
        {
            llSay(0, "The sporophyte eventually dies.\n  ");
            if (whichSide == 1)
            {
                llSetLinkTexture(sporophyte2a, invisTex, 1); //Sporophyte
                llSetLinkTexture(sporophyte2a, invisTex, 3);
                llSetLinkTexture(sporophyte2b, invisTex, 1);
                llSetLinkTexture(sporophyte2b, invisTex, 3);
                llSetLinkTexture(sporophyte2c, invisTex, 1);
                llSetLinkTexture(sporophyte2c, invisTex, 3);
                llSetLinkTexture(sporophyte2d, invisTex, 1);
                llSetLinkTexture(sporophyte2d, invisTex, 3);
            }
            else
            {
                llSetLinkTexture(sporophyte1a, invisTex, 1);
                llSetLinkTexture(sporophyte1a, invisTex, 3);
                llSetLinkTexture(sporophyte1b, invisTex, 1);
                llSetLinkTexture(sporophyte1b, invisTex, 3);
                llSetLinkTexture(sporophyte1c, invisTex, 1);
                llSetLinkTexture(sporophyte1c, invisTex, 3);
                llSetLinkTexture(sporophyte1d, invisTex, 1);
                llSetLinkTexture(sporophyte1d, invisTex, 3);
            }
        }
        waitingForTimer = 0;
    }

}