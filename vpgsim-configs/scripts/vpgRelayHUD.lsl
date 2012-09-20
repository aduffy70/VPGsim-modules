// vpgRelayHUD.lsl
// v1.1 Renamed commandRelayHUD as vpgRelayHUD and AutoPlanter as vpgPlanter
// VPGsim Project license applies (http://fernseed.usu.edu)

// This script relays chat commands for region modules to the IRC Bridge so I can give a command in one region and have it take effect in all regions.  The vpgPlanter listens for these messages at the other end.

integer isActivated;
integer listen1;
integer listen3;
integer listen4;
integer listen5;
integer listen15;

default
{
    attach(key id)
    {
        llResetScript();
    }

    state_entry()
    {
        llSetColor(<1.0, 0.0, 0.0>, ALL_SIDES);
        listen3 = llListen(3, "", llGetOwner(), ""); //vpgSummary Module channel
        llListenControl(listen3, FALSE);
        listen4 = llListen(4, "", llGetOwner(), ""); //vpgManager Module channel
        llListenControl(listen4, FALSE);
        listen5 = llListen(5, "", llGetOwner(), ""); //vpgVisualization Module channel
        llListenControl(listen5, FALSE);
        listen15 = llListen(15, "", llGetOwner(), ""); //vpgParameters Module channel
        llListenControl(listen15, FALSE);
        listen1 = llListen(1, "", llGetOwner(), ""); // Used to be for planting, but now unused?
        llListenControl(listen1, FALSE);
        llOwnerSay("OFF");
    }

    touch_start(integer num)
    {
        //Toggle listens on and off
        isActivated = !isActivated;
        llListenControl(listen1, isActivated);
        llListenControl(listen3, isActivated);
        llListenControl(listen4, isActivated);
        llListenControl(listen5, isActivated);
        llListenControl(listen15, isActivated);
        if (isActivated)
        {
            llOwnerSay("ON");
            //Green for on
            llSetColor(<0.0, 1.0, 0.0>, ALL_SIDES);
        }
        else
        {
            llOwnerSay("OFF");
            //Red for off
            llSetColor(<1.0, 0.0, 0.0>, ALL_SIDES);
        }
    }

    listen(integer channel, string name, key id, string message)
    {
        //Relay the message and provide feedback to owner
        llSay(2225, "passwd," + (string)channel + "," + message);
        llOwnerSay("Message relayed");
    }
}
