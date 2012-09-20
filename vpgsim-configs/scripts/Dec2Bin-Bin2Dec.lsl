// Dec2Bin-Bin2Dec.lsl

// VPGsim Project license applies (http://fernseed.usu.edu)

// This script converts binary numbers to digital numbers and digital numbers to binary numbers.
// Enter a positive decimal integer on channel 2 to get the binary number.
// Enter a binary number on channel 3 to get the decimal number.


integer Binary2Decimal(string binary)
{
    integer multiplier = 1;
    integer stringLength = llStringLength(binary) -1;
        integer decimal;
        while (stringLength >= 0)
        {
            decimal = decimal + (((integer)llGetSubString(binary, stringLength, stringLength) * multiplier));
             multiplier = multiplier * 2;
            stringLength = stringLength - 1;
        }
    return decimal;
}


string Decimal2Binary(integer decimal)
{
    integer count = 0;
    string binary;
    string binaryDigit;
    list binaryList;
    while (decimal > 0)
    {
        binaryDigit = (string)((integer)(decimal%2));
        binaryList = binaryList + [binaryDigit];
        decimal = decimal / 2;
        count++;
    }
    while (count>=0)
    {
        binary = binary + llList2String(binaryList, count);
        count = count - 1;
    }
    return binary;
}

default
{
    state_entry()
    {
        llSay(0, "Enter a positive decimal integer on channel 2 to get the binary number.");
        llSay(0, "Enter a binary number on channel 3 to get the decimal number.");
        llListen(2, "", "", "");
        llListen(3, "", "", "");
    }

    listen(integer channel, string name, key id, string message)
    {
        if (channel == 2)
        {
            llSay(0, Decimal2Binary((integer)message));
           }
           if (channel == 3)
           {
               llSay(0, (string)Binary2Decimal(message));
           }
       }
}