// vpgFern.lsl
// v1.5 Renamed ArtLifeNU objects as vpgFern
// VPGsim Project license applies (http://fernseed.usu.edu)

// This script simulates a virtual fern lifecycle.  This version uses non-physical spore dispersal and the old-style (ugly) sporophytes and gametophytes.
//Place this script in a prim with the following characteristics: Sphere, size <0.1, 0.1, 0.1>, Color <0.54 (138), 0.46 (117), 0.39 (100)>, Non-Physical, Phantom, blank texture, named "vpgFern"

//****************************************************************************************
float m_zOffset = 0.1;  //Z position offset to keep plants above ground
float m_penalty = 0.75;  //Proportion of health lost when changing states, reproducing, or sporulating
integer m_loci = 5; //Number of loci in a haplotype.  Must match the Population Summary Region Module.  Changing this is not trivial... yet.
float m_cycleTime; //Average cycle time
float m_cycleVariance; //Variation in cycletime
float m_gametophyteNeighborhood; //Radius where gametophytes affect their neighbors
float m_spermNeighborhood; //Distance sperm can swim to an egg
float m_sporophyteNeighborhood;  //Radius where sporophytes affect their neighbors
float m_sporophyteWeight;  //How many more times a sporophyte affects its neighbors
float m_neighborShape; //Shape of the fitness curve based on neighbor effects.
integer m_lifespan; //Max # of cycles spent in a lifestage
float m_health;  //Health status:  Die at 0.0, advance at 1.0
integer m_alreadyReproduced;  //Changes to 1 when a sporophyte has been produced
vector m_myPosition;  //The x,y,z location of the object
float m_neighborGametophytes;  //Number of gametophyte neighbors weighted by distance
float m_neighborSporophytes;  //Number of sporophyte neighbors weighted by distance
float m_neighborIndex;  //Used to calculate health. Ranges from 0-1
float m_altitudeIndex;  //Used to calculate health. Ranges from 0-1
float m_optAltitude;  //Optimal altitude for use in fitness calculations
float m_altitudeShape;  //Shape of the Altitude fitness curve
float m_salinityIndex;  //Used to calculate health. Ranges from 0-1
float m_optSalinity;  //Optimal Salinity for use in fitness calculations
float m_salinityShape;  //Shape of the Salinity fitness curve
float m_drainageIndex;  //Used to calculate health. Ranges from 0-1
float m_optDrainage;  //Optimal Drainage for use in fitness calculations
float m_drainageShape;  //Shape of the Drainage fitness curve
float m_fertilityIndex;  //Used to calculate health. Ranges from 0-1
float m_optFertility;  //Optimal Fertility for use in fitness calculations
float m_fertilityShape;  //Shape of the Fertility fitness curve
float m_fitness;  //Fitness under the current conditions.  Ranges from -0.5 to 0.5
integer m_age;  //Number of health cycles since the last lifestage change
integer m_genotype;  //The decimal integer of the binary genotype (Loci loci, 2 alleles)
integer m_haplotype1;  //The decimal integer of the right(or only) haplotype
integer m_haplotype2;  //The decimal integer of the left haplotype in diploids
integer m_phenotype;  //The decimal integer of the binary phenotype
string m_humanHaplotype1;  //Human readable haplotype1 string;
string m_humanHaplotype2;  //Human readable haplotype2 string;
integer m_mateHaplotype;  //The decimal integer of the mate's haplotype
key m_childID;  //The UUID of the offspring sporophyte
integer m_sporesProduced;  //Number of spores produced by a sporophyte
vector m_soilType; //Contains the Salinity, Drainage, and Fertility values
integer m_extraSpores;  //How many additional spores are created based on fitness level
integer m_advantage; //How many extra spores a perfectly fit individual releases each sporulation
float m_rain; //Minimum cloud value to germinate or reproduce
float m_maxDistance; //Maximum spore dispersal distance
float m_distanceShape; //Shape of the spore dispersal distance curve
string m_parameterVersion; //Keeps track of whether the user parameters on the server have been updated
float m_sensorRadius; //Distance that is scanned for neighbors.  Equal to the largest of m_gametophyteNeighborhood, m_sporophyteNeighborhood & m_spermNeighborhood

//****************************************************************************************

Sporulation()
{
    //Ask the vpgManager module to generate a new spore with a genotype generated at random from the parental genotypes
    integer childGenotype = 0; // The decimal integer of the new spore's haplotype
    integer count = 1;
    while (count <= (integer)llPow(2, m_loci - 1))
    {
        integer randomAllele = (integer)llFrand(2.0);
        if (randomAllele==0)
        {
            childGenotype = childGenotype + (m_genotype & count);
        }
        else
        {
            childGenotype = childGenotype + ((m_genotype>>m_loci) & count);
        }
        count = count * 2;
    }
    //Choose a location for the new spore based on wind direction and a dispersal distance distribution
    float xyRandom = m_maxDistance - ((m_maxDistance / m_distanceShape) * (llLog(llFrand(300) + 1.0))); //Need to replace this with OS command to draw from real distributions
    vector wind = llVecNorm(llWind(ZERO_VECTOR)); //Wind controls dispersal direction but not distance
    float rezX = wind.x * xyRandom;
    float rezY = wind.y * xyRandom;
    float rezZ = llGround(<rezX, rezY, 0.0>);
    vector rezLocation = <m_myPosition.x + rezX, m_myPosition.y + rezY, rezZ>;
    //Decide if the new location is within this region
    integer inOtherRegion = 0;
    string direction = "";
    if (rezLocation.y  >= 256)
    {
        inOtherRegion = 1;
        direction = direction + "N";
        rezLocation.y = rezLocation.y - 256;
    }
    else
    {
        if (rezLocation.y < 0)
        {
            inOtherRegion = 1;
            direction = direction + "S";
            rezLocation.y = 256 + rezLocation.y;
        }
    }
    if (rezLocation.x >= 256)
    {
        inOtherRegion = 1;
        direction = direction + "E";
        rezLocation.x = rezLocation.x - 256;
    }
    else
    {
        if (rezLocation.x < 0)
        {
            inOtherRegion = 1;
            direction = direction + "W";
            rezLocation.x = 256 + rezLocation.x;
        }
    }
    if (inOtherRegion == 0) //Tell the vpgManager module to recycle one if it can
    {
        modSendCommand("vpgManagerModule", "xxgenomexx" + (string)childGenotype, (string)rezLocation);
    }
    else //Tell another region to handle it
    {
        //Format of this message needs to match what the IRC Bridge Module is expecting
        llSay(2225, "passwd," + (string)childGenotype + "," + direction + "," + (string)rezLocation.x + "," + (string)rezLocation.y);
    }
    m_sporesProduced++;
}

float CalculateFitness()
{
    //Calculate the current fitness of the organism
    m_neighborIndex = 1 - (m_neighborShape * (m_neighborGametophytes + m_neighborSporophytes) * (m_neighborGametophytes + m_neighborSporophytes));
    if (m_neighborIndex < 0)
    {
        m_neighborIndex = 0;
    }
    m_altitudeIndex = 1 - (m_altitudeShape * ((m_myPosition.z - m_optAltitude) * (m_myPosition.z - m_optAltitude)) / 2500);
    if (m_altitudeIndex < 0)
    {
        //Don't allow an index less than zero
        m_altitudeIndex = 0;
    }
    m_soilType = osSoilType(ZERO_VECTOR);
    m_salinityIndex = 1 - (m_salinityShape * (m_soilType.x - m_optSalinity) * (m_soilType.x - m_optSalinity));
    if (m_salinityIndex < 0)
    {
        m_salinityIndex = 0;
    }
    m_drainageIndex = 1 - (m_drainageShape * (m_soilType.y - m_optDrainage) * (m_soilType.y - m_optDrainage));
    if (m_drainageIndex < 0)
    {
        m_drainageIndex = 0;
    }
    m_fertilityIndex = 1 - (m_fertilityShape * (m_soilType.z - m_optFertility) * (m_soilType.z - m_optFertility));
    if (m_fertilityIndex < 0)
    {
        m_fertilityIndex = 0;
    }
    float fitness = ((m_neighborIndex * m_altitudeIndex * m_salinityIndex * m_drainageIndex * m_fertilityIndex) - 0.5);
    return fitness;
}

string Float2String (float num, integer places)
{
    //Convert a float to a string
    float f = llPow( 10.0, places );
    integer i = llRound(llFabs(num) * f);
    string s = "00000" + (string)i;
    if(num < 0.0)
        return "-" + (string)( (integer)(i / f) ) + "." + llGetSubString( s, -places, -1);
    return (string)( (integer)(i / f) ) + "." + llGetSubString( s, -places, -1);
}

string Decimal2Binary(integer decimal)
{
    //Convert a decimal integer to a human-readable binary string
    integer count = 0;
    string binary;
    string binaryDigit;
    list binaryList;
    while ((count < m_loci) || (decimal >0))
    {
        binaryDigit = (string)((integer)(decimal%2));
        binaryList = binaryList + [binaryDigit];
        decimal /= 2;
        count++;
    }
    while (count>=0)
    {
        binary += llList2String(binaryList, count);
        count = count - 1;
    }
    return binary;
}

DieIfUnderwater()
{
    //Compare altitude to water level and die if underwater
	m_myPosition = llGetPos();
    if (m_myPosition.z  <= (llWater(ZERO_VECTOR) + (m_zOffset * 2)))//Fudging to keep plants from growing over water when the viewer shows ground slightly below water but llGround says it is slightly above.  This results from the pathetic hack used in Opensim to calculate ground level on slopes (It was MY hack... sorry!)
    {
        state Dead;
    }
}

GetAdjustableParameters(string lifeStage, integer phenotype)
{
    //Get user specified parameters from the server & assign to variables
	list parameterList = osGetParameterList(lifeStage, phenotype);
	m_cycleTime = llList2Float(parameterList, 0);
	m_cycleVariance = m_cycleTime * 0.05; // +/- 5% variation in cycletimes to avoid all scripts triggering at the same time
	m_gametophyteNeighborhood = llList2Float(parameterList, 1);
	m_sporophyteNeighborhood = llList2Float(parameterList, 2);
	m_sporophyteWeight = llList2Float(parameterList, 3);
	m_neighborShape = llList2Float(parameterList, 4);
	m_optAltitude = llList2Float(parameterList, 5);
	m_altitudeShape = llList2Float(parameterList, 6);
	m_optSalinity = llList2Float(parameterList, 7);
	m_salinityShape = llList2Float(parameterList, 8);
	m_optDrainage = llList2Float(parameterList, 9);
	m_drainageShape = llList2Float(parameterList, 10);
	m_optFertility = llList2Float(parameterList, 11);
	m_fertilityShape = llList2Float(parameterList, 12);
	m_lifespan = llList2Integer(parameterList, 13);
	if ((lifeStage == "Spore") | (lifeStage == "Gametophyte"))
    {
        //Spore & Gametophyte parameters
		m_rain = llList2Float(parameterList, 14);
		if (lifeStage == "Gametophyte")
        { //Gametophyte parameters
			m_spermNeighborhood = llList2Float(parameterList, 15);
		}
	}
	else
    {
        //Sporophyte only parameters
		m_advantage = llList2Integer(parameterList, 14);
		m_maxDistance = llList2Float(parameterList, 15);
		m_distanceShape = llList2Float(parameterList, 16);
	}
}

float GetLargestNeighborhood(integer includeSperm)
{
    //Return the largest Neighborhood size including or not including the m_spermNeighborhood
    float largest = 0;
    if ((m_spermNeighborhood > largest) && (includeSperm == 1))
    {
        largest = m_spermNeighborhood;
    }
    if (m_gametophyteNeighborhood > largest)
    {
        largest = m_gametophyteNeighborhood;
    }
    if (m_sporophyteNeighborhood > largest)
    {
        largest = m_sporophyteNeighborhood;
    }
    return largest;
}


string PlantStatistics(string lifeStage)
{
    //Create a string summarizing my current statistics
	string line1 = "";
	string line2 = "Fitness:" + Float2String(m_fitness, 2) + "\tHealth:" + Float2String(m_health, 2) + "\n";
    string line3 = "\nAltitude:" + Float2String(m_myPosition.z, 1) + "m\tIndex:" + Float2String(m_altitudeIndex, 2) + "\n\tOpt:" + Float2String(m_optAltitude, 1) + "m\tShape:" + Float2String(m_altitudeShape, 2) + "\n";
    string line4 = "Salinity:" + Float2String(m_soilType.x, 2) + "\tIndex:" + Float2String(m_salinityIndex, 2) + "\n\tOpt:" + Float2String(m_optSalinity, 1) + "\tShape:" + Float2String(m_salinityShape, 2) + "\n";
    string line5 = "Drainage:" + Float2String(m_soilType.y, 2) + "\tIndex:" + Float2String(m_drainageIndex, 2) + "\n\tOpt:" + Float2String(m_optDrainage, 1) + "\tShape:" + Float2String(m_drainageShape, 2) + "\n";
    string line6 = "Fertility:" + Float2String(m_soilType.z, 2) + "\tIndex:" + Float2String(m_fertilityIndex, 2) + "\n\tOpt:" + Float2String(m_optFertility, 1) + "\tShape:" + Float2String(m_fertilityShape, 2) + "\n";
	string line7 = "";
	string line8 = "";
	string line9 = "";
	if (lifeStage == "Spore")
    {
		line1 = "\nHaplotype:" + m_humanHaplotype1 + "\tAge:" + m_age + "\tLifespan:" + m_lifespan + "\nCycle:" + (integer)m_cycleTime + " +/-" + (integer)m_cycleVariance + "s\n";
		line8 = "\nCloudcover:" + Float2String(llCloud(ZERO_VECTOR), 2) + "\tRequired:" + Float2String(m_rain, 2) + "\n";
	}
	else if (lifeStage == "Gametophyte")
    {
		line1 = "\nHaplotype:" + m_humanHaplotype1 + "\tAge:" + m_age + "\tLifespan:" + m_lifespan + "\nCycle:" + (integer)m_cycleTime + " +/-" + (integer)m_cycleVariance + "s\n";
		line7 = "\nNeighbors (weighted)\n\tIndex:" + Float2String(m_neighborIndex, 2) + "\tShape:" + Float2String(m_neighborShape, 2) + "\n\tGametophytes:" + Float2String(m_neighborGametophytes, 2) + "\tSporophytes:" + Float2String(m_neighborSporophytes, 2) + "\nAffected Distance\n\tGametophytes:" + Float2String(m_gametophyteNeighborhood, 1) + "m\tSporophytes:" + Float2String(m_sporophyteNeighborhood, 1) + "m\n\tSperm:" + Float2String(m_spermNeighborhood, 1) + "m\n";
		line8 = "\nCloudcover:" + Float2String(llCloud(ZERO_VECTOR), 2) + "\tRequired:" + Float2String(m_rain, 2) + "\n";
	}
	else //It is a Sporophyte
    {
		line1 = "\nGenotype:" + m_humanHaplotype1 + " " + m_humanHaplotype2 + "\tAge:" + m_age + "\tLifespan:" + m_lifespan + "\nCycle:" + (integer)m_cycleTime + " +/-" + (integer)m_cycleVariance + "s\tSpores:" + m_sporesProduced + "\n";
		line7 = "\nNeighbors (weighted)\n\tIndex:" + Float2String(m_neighborIndex, 2) + "\tShape:" + Float2String(m_neighborShape, 2) + "\n\tGametophytes:" + Float2String(m_neighborGametophytes, 2) + "\tSporophytes:" + Float2String(m_neighborSporophytes, 2) + "\nAffected Distance\n\tGametophytes:" + Float2String(m_gametophyteNeighborhood, 1) + "m\tSporophytes:" + Float2String(m_sporophyteNeighborhood, 1) + "m\n\tSperm:" + Float2String(m_spermNeighborhood, 1) + "m\n";
		line8 = "\nWind direction\n";
        vector windNow = llVecNorm(llWind(ZERO_VECTOR));
        line9 = "\tX:" + Float2String(windNow.x, 2) + "\tY:" + Float2String(windNow.y, 2) + "\nMax dispersal distance:" + (integer)m_maxDistance + "\tShape:" + Float2String(m_distanceShape, 2);
    }
    return (line1 + line2 + line3 + line4 + line5 + line6 + line7 + line8 + line9);
}

rotation Vec2Rot(vector pointAt, string lifeStage)
{
    //Used to help figure out which way is up in my current lifestage so I can point in the groundnormal direction (vector pointAt).
    pointAt = llVecNorm(pointAt);
    vector up = <0.0,0.0,1.0>;
    vector left;
    if (lifeStage == "Gamet")
    {
        left = llVecNorm(pointAt%up);
    }
    else
    {
        left = llVecNorm(up%pointAt);
    }
    up = llVecNorm(pointAt%left);
    return llAxes2Rot(pointAt, left, up);
}

//****************************************************************************************

default
{
    on_rez(integer startParameter)  //Can I use a negative StartingParameter to indicate a gametophyte (gemma)?
    {
        m_genotype = startParameter;
        if ((m_genotype & (integer)llPow(2, 2 * m_loci)) == (integer)llPow(2, 2 * m_loci))
        {
            //If the first character of the binary genotype is a 1 then I am a sporophyte
            state Sporophyte;
        }
        else
        {
            //otherwise I am a spore
            state Spore;
        }
    }
}

//****************************************************************************************

state Spore
{
    state_entry()
    {
        DieIfUnderwater();
        llSetObjectName("Spore" + (string)m_genotype);
        m_humanHaplotype1 = Decimal2Binary(m_genotype);
        llSetPrimitiveParams(
        [
            PRIM_COLOR, ALL_SIDES, <0.54, 0.46, 0.39>, 1.0,
            PRIM_SIZE, <0.1, 0.1, 0.1>,
            PRIM_ROTATION, <0,0,0,1>,
            PRIM_TYPE, PRIM_TYPE_SPHERE, 0, <0.0, 1.0 ,0.0>, 0.0, ZERO_VECTOR, <0.0, 1.0, 0.0>
        ]);
        m_health = 0.25;
        m_age = 0;
		m_parameterVersion = osGetVersionID();
		GetAdjustableParameters("Spore", m_genotype);
        m_sensorRadius = GetLargestNeighborhood(0);
        llSensor("", NULL_KEY, ACTIVE | PASSIVE | SCRIPTED, m_sensorRadius, PI);
        llSetTimerEvent(m_cycleTime + llFrand(2 * m_cycleVariance) - m_cycleVariance);
    }

    timer()
    {
        integer isStoppedTimer = 0;
        m_age = m_age + 1;
        if (m_parameterVersion != osGetVersionID())
        {
            llSetTimerEvent(0); //Stop the old timer cause I may have a new CycleTime
            isStoppedTimer = 1;
            m_parameterVersion = osGetVersionID();
            GetAdjustableParameters("Spore", m_genotype);
            m_sensorRadius = GetLargestNeighborhood(0);
        }
        if (m_age <= m_lifespan)
        {
            llSensor("", NULL_KEY, ACTIVE | PASSIVE | SCRIPTED, m_sensorRadius, PI);
        }
        else
        {
            state Dead;
        }
        if (isStoppedTimer)
        { //Restart the timer with new CycleTime if I stopped it
            llSetTimerEvent(m_cycleTime + llFrand(2 * m_cycleVariance) - m_cycleVariance);
        }
	}

    sensor(integer numberDetected)
    {
        //I detected neighbors
        m_neighborGametophytes=0;
        m_neighborSporophytes=0;
        integer loop;
        for(loop=0; loop < numberDetected; loop++)
        {
            float distance = llFabs(llVecDist(llDetectedPos(loop), m_myPosition));
            if (llGetSubString(llDetectedName(loop), 0, 4) == "Gamet")
            {
                if (distance < m_gametophyteNeighborhood)
                {
                    m_neighborGametophytes = m_neighborGametophytes + (1 - (distance / m_gametophyteNeighborhood));
                }
            }
            if (llGetSubString(llDetectedName(loop), 0, 4) == "Sporo")
            {
                if (distance < m_sporophyteNeighborhood)
                {
                    m_neighborSporophytes = m_neighborSporophytes + ((1 - (distance / m_sporophyteNeighborhood)) * m_sporophyteWeight);
                }
            }
        }
        m_fitness = CalculateFitness();
        m_health = m_health + m_fitness;
        float grayscale = m_fitness + 0.5;
        llSetText(m_humanHaplotype1, <grayscale, grayscale, grayscale>, 1.0);
        if (m_health <= 0.0)
        {
            state Dead;
        }
        if (m_health >= 1.0)
        {
            if (llCloud(ZERO_VECTOR) < m_rain)
            {
                m_health = 1.0;
            }
            else
            {
                state Gametophyte;
            }
        }
    }

    no_sensor()
    {
        //I didn't detect any neighbors
        m_neighborGametophytes=0;
        m_neighborSporophytes=0;
        m_fitness = CalculateFitness();
        m_health = m_health + m_fitness;
        float grayscale = m_fitness + 0.5;
        llSetText(m_humanHaplotype1, <grayscale, grayscale, grayscale>, 1.0);
        if (m_health <= 0.0)
        {
            state Dead;
        }
        if (m_health >= 1.0)
        {
            if (llCloud(ZERO_VECTOR) < m_rain)
            {
                m_health = 1.0;
            }
            else
            {
                state Gametophyte;
            }
        }
    }

    touch_start(integer number)
    {
        //Report my current statistics
        llDialog(llDetectedKey(0), PlantStatistics("Spore"), ["OK"], -1);
    }
}

//****************************************************************************************

state Gametophyte
{
    state_entry()
    {
        llSetObjectName("Gamet" + (string)m_genotype);
        vector slope = llGroundSlope(<0.0, 0.0, 0.0>);
        if (slope == <0.0, 0.0, 0.0>)
        {
            slope = <1.0, 0.0, 0.0>;
        }
        llSetPrimitiveParams(
        [
            PRIM_COLOR, ALL_SIDES, <0.0, 0.5, 0.0>, 1.0,
            PRIM_SIZE, <0.2, 0.2, 0.05>,
            PRIM_ROTATION, Vec2Rot(-1 * (slope + <llFrand(0.2) - 0.1, llFrand(0.2) - 0.1, llFrand(0.2) - 0.1>), "Gamet"),
            PRIM_TYPE, PRIM_TYPE_CYLINDER, 0, <0.0, 1.0, 0.0>, 0.95, <180.0, -180.0, 0.0>, <-1.0, -1.0, 0.0>, ZERO_VECTOR
        ]);
        m_age = 0;
        m_health = 0.25;
		m_alreadyReproduced = 0;
		m_parameterVersion = osGetVersionID();
		GetAdjustableParameters("Gametophyte", m_genotype);
		m_sensorRadius = GetLargestNeighborhood(1);
        llSensor("", NULL_KEY, ACTIVE | PASSIVE | SCRIPTED, m_sensorRadius, PI);
        llSetTimerEvent(m_cycleTime + llFrand(2 * m_cycleVariance) - m_cycleVariance);
    }

    timer()
    {
        integer isStoppedTimer = 0;
        m_age = m_age + 1;
        if (m_parameterVersion != osGetVersionID())
        {
            llSetTimerEvent(0); //Stop the old timer cause I may have a new CycleTime
            isStoppedTimer = 1;
            m_parameterVersion = osGetVersionID();
            GetAdjustableParameters("Gametophyte", m_genotype);
            m_sensorRadius = GetLargestNeighborhood(1);
        }
        if (m_age <= m_lifespan)
        {
            llSensor("", NULL_KEY, ACTIVE | PASSIVE | SCRIPTED, m_sensorRadius, PI);
        }
        else
        {
            state Dead;
        }
        if (isStoppedTimer)
        { //Restart the timer with new CycleTime if I stopped it
            llSetTimerEvent(m_cycleTime + llFrand(2 * m_cycleVariance) - m_cycleVariance);
        }
	}

    sensor(integer numberDetected)
    {
        //I detected neighbors
        m_neighborGametophytes=0;
        m_neighborSporophytes=0;
        integer loop;
        integer closestMate = 0;
        integer childPresent = 0;
        for(loop=0; loop < numberDetected; loop++)
        {
            float distance = llFabs(llVecDist(llDetectedPos(loop), m_myPosition));
            if (llGetSubString(llDetectedName(loop), 0, 4) == "Gamet")
            {
                if (distance < m_gametophyteNeighborhood)
                {
                    m_neighborGametophytes = m_neighborGametophytes + (1 - (distance / m_gametophyteNeighborhood));
                }
                if ((closestMate == 0) && (distance <= m_spermNeighborhood))
                {
                    m_mateHaplotype = ((integer)llPow(2, m_loci) - 1) & ((integer)llGetSubString(llDetectedName(loop), 5, -1));
                    closestMate = 1;
                }
            }
            if (llGetSubString(llDetectedName(loop), 0, 4) == "Sporo")
            {
                if (distance < m_sporophyteNeighborhood)
                {
                    m_neighborSporophytes = m_neighborSporophytes + ((1 - (distance / m_sporophyteNeighborhood)) * m_sporophyteWeight);
                    if (llDetectedKey(loop) == m_childID)
                    {
                        childPresent = 1;
                    }
                }
            }
        }
        m_fitness = CalculateFitness();
        m_health = m_health + m_fitness;
        float grayscale = m_fitness + 0.5;
        llSetText(m_humanHaplotype1, <grayscale, grayscale, grayscale>, 1.0);
        if (m_health <= 0.0)
        {
            state Dead;
        }
        if (m_health >= 1.0)
        {
            if ((closestMate == 0) || ((m_alreadyReproduced == 1) && (childPresent == 1)) || (llCloud(ZERO_VECTOR) < m_rain))
            {
                m_health = 1.0;
            }
            else
            {
                m_alreadyReproduced = 1;
                m_health = m_health - m_penalty;
                //Produce a sporophyte
                integer childGenotype = (m_mateHaplotype << m_loci) + m_genotype + (integer)llPow(2, 2 * m_loci); // The decimal genotype of the new sporophyte
                //Tell the vpgManager module to recycle one if it can
                modSendCommand("vpgManagerModule", "xxgenomexx" + (string)childGenotype, (string)m_myPosition);
            }
        }
    }

    no_sensor()
    {
        //I didn't detect any neighbors
        m_neighborGametophytes=0;
        m_neighborSporophytes=0;
        m_fitness = CalculateFitness();
        m_health = m_health + m_fitness;
        float grayscale = m_fitness + 0.5;
        llSetText(m_humanHaplotype1, <grayscale, grayscale, grayscale>, 1.0);
        if (m_health <= 0.0)
        {
            state Dead;
        }
        else
        {
            if (m_health >= 1.0)
            {
                m_health = 1.0;
            }
        }
    }

    link_message(integer sender_num, integer genome, string coordinates, key id)
    {
        if (genome == -1)
        {
            //The vpgManager module rezzed a sporophyte for me and is giving me its key so I won't produce another unless it dies
            m_childID = id;
        }
        else
        {
            //The vpgManager module couldn't recycle a sporophyte so I need to rez one
            llRezObject("vpgFern", m_myPosition, <0,0,0>, <0,0,0,0>, genome);
        }
    }

    object_rez(key item)
    {
        //I rezzed a sporophyte and need to give it a copy of itself so it can reproduce later.
        llGiveInventory(item, "vpgFern");
        //I also need to remember its key so I won't produce another unless it dies
        m_childID = item;
    }

    touch_start(integer number)
    {
        //Report my current statistics
        llDialog(llDetectedKey(0), PlantStatistics("Gametophyte"), ["OK"], -1);
    }
}

//****************************************************************************************

state Sporophyte
{
    state_entry()
    {
        DieIfUnderwater();
        llSetObjectName("Sporo" + (string)m_genotype);
        m_haplotype1 = ((integer)llPow(2, m_loci) - 1) & m_genotype;
        m_haplotype2 = ((((integer)llPow(2, m_loci) - 1) << m_loci) & m_genotype) >> m_loci;
        m_phenotype = m_haplotype1 | m_haplotype2; //Allele 1 is dominant to Allele 0
        m_humanHaplotype1 = Decimal2Binary(m_haplotype1);
        m_humanHaplotype2 = Decimal2Binary(m_haplotype2);
        llSetPrimitiveParams(
        [
            PRIM_COLOR, ALL_SIDES, <0.0, 0.5, 0.0>, 1.0,
            PRIM_SIZE, <0.5, 0.5, 0.5>,
            PRIM_ROTATION, Vec2Rot(-1 * (llGroundNormal(<0,0,0>) + <llFrand(0.2) - 0.1, llFrand(0.2) - 0.1, llFrand(0.75) + 0.5>), "Sporo"),
            PRIM_TYPE, PRIM_TYPE_TUBE, 0,<0.0, 1.0 ,0.0>, 95.0, ZERO_VECTOR, <1.0, 0.5, 0.0>, ZERO_VECTOR, <0.25,0.75,0>, <0.4,0,0>, 0.0, 0.0, 0.0
        ]);
        m_age = 0;
        m_health = 0.25;
        m_sporesProduced = 0;
        m_parameterVersion = osGetVersionID();
		GetAdjustableParameters("Sporophyte", m_phenotype);
        m_sensorRadius = GetLargestNeighborhood(0);
        llSensor("", NULL_KEY, ACTIVE | PASSIVE | SCRIPTED, m_sensorRadius, PI);
        llSetTimerEvent(m_cycleTime + llFrand(2 * m_cycleVariance) - m_cycleVariance);
    }

    timer()
    {
        integer isStoppedTimer = 0;
        m_age = m_age + 1;
        if (m_parameterVersion != osGetVersionID())
        {
            llSetTimerEvent(0); //Stop the old timer cause I may have a new CycleTime
            isStoppedTimer = 1;
            m_parameterVersion = osGetVersionID();
            GetAdjustableParameters("Sporophyte", m_phenotype);
            m_sensorRadius = GetLargestNeighborhood(0);
        }
        if (m_age <= m_lifespan)
        {
            llSensor("", NULL_KEY, ACTIVE | PASSIVE | SCRIPTED, m_sensorRadius, PI);
        }
        else
        {
            state Dead;
        }
        if (isStoppedTimer)
        { //Restart the timer with new CycleTime if I stopped it
            llSetTimerEvent(m_cycleTime + llFrand(2 * m_cycleVariance) - m_cycleVariance);
        }
	}

    sensor(integer numberDetected)
    {
        //I did detect neighbors
		m_neighborGametophytes=0;
		m_neighborSporophytes=0;
		integer loop;
		for(loop=0; loop < numberDetected; loop++)
        {
			float distance = llFabs(llVecDist(llDetectedPos(loop), m_myPosition));
            if (llGetSubString(llDetectedName(loop), 0, 4) == "Gamet")
            {
                if (distance < m_gametophyteNeighborhood)
                {
                    m_neighborGametophytes = m_neighborGametophytes + (1 - (distance / m_gametophyteNeighborhood));
                }
            }
			if (llGetSubString(llDetectedName(loop), 0, 4) == "Sporo")
            {
                if (distance < m_sporophyteNeighborhood)
                {
				    m_neighborSporophytes = m_neighborSporophytes + ((1 - (distance / m_sporophyteNeighborhood)) * m_sporophyteWeight);
                }
			}
		}
        m_fitness = CalculateFitness();
		m_health = m_health + m_fitness;
        float grayscale = m_fitness + 0.5;
        llSetText(m_humanHaplotype1 + " " + m_humanHaplotype2, <grayscale, grayscale, grayscale>, 1.0);
		if (m_health <= 0.0)
        {
			state Dead;
		}
		if (m_health >= 1.0)
        {
			m_health = m_health - m_penalty;
			Sporulation();
			m_extraSpores = (integer)(m_fitness * 2 * m_advantage);
			while(m_extraSpores > 0)
            {
				Sporulation();
                m_extraSpores--;
			}
		}
    }

    no_sensor()
    {
        //I didn't detect any neighbors
		m_neighborGametophytes=0;
		m_neighborSporophytes=0;
        m_fitness = CalculateFitness();
		m_health = m_health + m_fitness;
        float grayscale = m_fitness + 0.5;
        llSetText(m_humanHaplotype1 + " " + m_humanHaplotype2, <grayscale, grayscale, grayscale>, 1.0);
		if (m_health <= 0.0)
        {
			state Dead;
		}
		if (m_health >= 1.0)
        {
			m_health = m_health - m_penalty;
			Sporulation();
			m_extraSpores = (integer)(m_fitness * 2 * m_advantage);
			while(m_extraSpores > 0)
            {
				Sporulation();
                m_extraSpores--;
			}
		}
    }

    link_message(integer sendernum, integer genome, string coordinates, key id)
    {
        //The vpgManager module couldn't recycle a spore so I need to rez one
        list xyzPos = llParseString2List(coordinates, ["<", ">", ","], [ ]);
        vector rezLocation = <llList2Float(xyzPos, 0), llList2Float(xyzPos, 1), llList2Float(xyzPos, 2)>;
        llRezObject("vpgFern", rezLocation, <0,0,0>, <0,0,0,0>, genome);
    }

    object_rez(key item)
    {
        //I rezzed a spore and need to give it a copy of itself so it can reproduce later
        llGiveInventory(item, "vpgFern");
    }

    touch_start(integer number)
    {
        //Report my current statistics
		llDialog(llDetectedKey(0), PlantStatistics("Sporophyte"), ["OK"], -1);
    }
}

state Dead
{
    state_entry()
    {
        //Hide from visitors and other plants until I get recycled
        llSetObjectName("xxdeadxx");
        llSetText("", <0,0,0>, 0);
        llSetAlpha(0, ALL_SIDES); //Hide while it awaits recycling
        //Tell vpgManager Module I am available
        modSendCommand("vpgManagerModule", "xxdeadxx", llGetKey());
    }

    link_message(integer sendernum, integer genome, string coordinates, key id)
    {
        //I am getting recycled!  Move to new coordinates
        list xyzPos = llParseString2List(coordinates, ["<", ">", ","], [ ]);
        llSetPos(<llList2Float(xyzPos, 0), llList2Float(xyzPos, 1), llList2Float(xyzPos, 2) + m_zOffset>);
        //Set the genotype and start a new life in the appropriate lifestage
        m_genotype = genome;
        if ((m_genotype & (integer)llPow(2, 2 * m_loci)) == (integer)llPow(2, 2 * m_loci))
        {
            //If the first character of the binary genotype is a 1 then I am a sporophyte
            state Sporophyte;
        }
        else
        {
            //otherwise I am a spore
            state Spore;
        }
    }

    changed(integer change)
    {
        //Remind the vpgManager Module that I am available after a region restart
        if (change & CHANGED_REGION_RESTART)
        {
            modSendCommand("vpgManagerModule", "xxdeadxx", llGetKey());
        }
    }
}
