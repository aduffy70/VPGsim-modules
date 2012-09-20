// vpgPlanter.lsl
// v1.4 Renamed AutoPlanter as vpgPlanter and ArtLifeNU as vpgFern
// VPGsim Project license applies (http://fernseed.usu.edu)

// This script rezzes spores or sporophytes at specific coordinates received from neighboring regions over the IRC bridge or from the vpgManager Module.  It also relays commands from the IRC bridge to the region modules running on this region.  A vpgPlanter object is required for the VPGsim system to function.
// The prim containing this script must also contain a copy of the vpgFern object.

//Expected message format for rezzing plants: string1,integer,string2,float1,float2
//  string1 = name of the originating region (where the message is coming from)
//  integer = genome of the new organism
//  string2 = direction from the originating region to the destination region (SE, S, SW, W, NW, N, NE, E)
//  float1 = x position where the new organism should be born in the destination region
//  float2 = y position where the new organism should be born in the destination region

//Expected format for region module commands: string1, integer, string2
//  string1 = name of originating region
//  integer = chat channel the command is from (and for)
//  string2 = the command to be relayed

//List the regions surrounding this region.  We will listen for messages on the IRC bridge from these regions.  Spelling and capitalization matter! Use "" where there is no neighbor or when we want to ignore messages from a region.
string southeastOf = ""; //1
string southOf = "";     //2         1 | 2 | 3
string southwestOf = ""; //3         ---------
string westOf = "";      //4         8 | X | 4
string northwestOf = ""; //5         ---------
string northOf = "";     //6         7 | 6 | 5
string northeastOf = ""; //7
string eastOf = "";      //8

float m_zOffset = 0.1; //Offset to keep spores from being embedded in the ground.  Should match m_zOffset in vpgFern.lsl

RequestRecycling(integer genome, float xPos, float yPos)
{
    //Ask the vpgManager Module to recycle a dead plant
    vector rezzerPosition = llGetPos();
    float zPos = llGround(<xPos - rezzerPosition.x, yPos - rezzerPosition.y, 0>);
    if (zPos >= llWater(<xPos - rezzerPosition.x, yPos - rezzerPosition.y, 0>))
    {
        vector rezLocation = <xPos, yPos, zPos>;
        modSendCommand("vpgManagerModule", "xxgenomexx" + (string)genome, (string)rezLocation);
    }
}

default
{
    state_entry()
    {
        llSetText("vpgPlanter: Do not remove", <0.0,0.0,0.0>, 1.0);
        llSay(0, "vpgPlanter is active");
        //Register this script with the vpgManager Module
        modSendCommand("vpgManagerModule", "xxregisterxx", "");
        llListen(2226, "","","");
    }

    changed(integer change)
    {
        if (change & CHANGED_REGION_RESTART)
        {
            llResetScript();
        }
    }

    on_rez(integer num)
    {
        llResetScript();
    }

    listen(integer channel, string name, key id, string message)
    {
        //Listen for messages on the IRC bridge
        list messageList = llParseString2List(message,[","],[]);
        if (llGetListLength(messageList) == 5) //it is message to rez a plant
        {
            string regionName = llList2String(messageList, 0);
            integer genome = llList2Integer(messageList, 1);
            string direction = llList2String(messageList, 2);
            float xPos = llList2Float(messageList, 3);
            float yPos = llList2Float(messageList, 4);
            if (((direction == "SE") && (southeastOf == regionName)) || ((direction == "S") && (southOf == regionName)) || ((direction == "SW") && (southwestOf == regionName)) || ((direction == "W") && (westOf == regionName)) || ((direction == "NW") && (northwestOf == regionName)) || ((direction == "N") && (northOf == regionName)) || ((direction == "NE") && (northeastOf == regionName)) || ((direction == "E") && (eastOf == regionName)))
            {
                //It is talking about this region!
                RequestRecycling(genome, xPos, yPos);
            }
        }
        else if (llGetListLength(messageList) == 3) //it is a module command
        {
            integer commandChannel = llList2Integer(messageList, 1);
            string commandMessage = llList2String(messageList, 2);
            llSay(commandChannel, commandMessage);
        }
    }

    link_message(integer sendernum, integer genome, string coordinates, key id)
    {
        //The vpgManager module couldn't recycle a spore so I need to rez one
        list xyzPos = llParseString2List(coordinates, ["<", ">", ","], [ ]);
        vector rezLocation = <llList2Float(xyzPos, 0), llList2Float(xyzPos, 1), llList2Float(xyzPos, 2) + m_zOffset>;
        llRezObject("vpgFern", rezLocation, <0,0,0>, <0,0,0,0>, genome);
    }
}
