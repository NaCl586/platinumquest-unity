using UnityEngine;

[CreateAssetMenu(fileName = "SceneryDatabase", menuName = "Game/Scenery Database")]
public class SceneryDatabase : ScriptableObject
{
    [Header("Clouds")]
    public GameObject Cloud48;
    public GameObject Cloud36;
    public GameObject Cloud24;
    public GameObject FlatLargeClouds;
    public GameObject OrbitingClouds;

    [Header("Skies")]
    public GameObject clear;
    public GameObject Cloudy;
    public GameObject dusk;
    public GameObject Wintry;    

    [Header("Fences")]
    public GameObject Fence_1TilesLength;
    public GameObject Fence_2TilesLength;
    public GameObject Fence_3TilesLength;
    public GameObject Fence_4TilesLength;
    public GameObject Fence_5TilesLength;
    public GameObject FencePole;

    public GameObject Metal_End_Fence_Short;
    public GameObject Metal_Start_Fence_Short;
    public GameObject Metal_Pole_Fence_Short;
    public GameObject Metal_End_Fence_Tall;
    public GameObject Metal_Start_Fence_Tall;
    public GameObject Metal_Pole_Fence_Tall;

    public GameObject Plastic_End_Fence_Short;
    public GameObject Plastic_Start_Fence_Short;
    public GameObject Plastic_Pole_Fence_Short;
    public GameObject Plastic_End_Fence_Tall;
    public GameObject Plastic_Start_Fence_Tall;
    public GameObject Plastic_Pole_Fence_Tall;

    [Header("Vegetation")]
    public GameObject Plant01;
    public GameObject Fern01;
    public GameObject Flowers;

    public GameObject Tulip;
    public GameObject Scarce_Tulips;
    public GameObject Dense_Tulips;
    public GameObject Scarce_tulips_3tiles;

    public GameObject EffectPlant;

    public GameObject Grass;
    public GameObject LargeGrass;
    public GameObject Grass02Small;
    public GameObject Grass02DenseSmall;
    public GameObject Grass02DenseLarge;

    public GameObject NaturalPlant;

    public GameObject VinesWideLong;
    public GameObject VinesWideShort;
    public GameObject VinesThinLong;
    public GameObject VinesThinShort;

    public GameObject Tree01;
    public GameObject Tree02;
    public GameObject Tree03;
    public GameObject TreeBare01;
    public GameObject TreeBare02;
    public GameObject TreeBare03;

    public GameObject Rock01;
    public GameObject Rock02;
    public GameObject Rock03;
    public GameObject Rock04;

    [Header("Graffiti")]
    public GameObject Marble_Graffiti;
    public GameObject Marble_Graffiti_2;
    public GameObject SuperJump_Graffiti;
    public GameObject Cannon_Graffiti;
    public GameObject PQ_Graffiti;
    public GameObject PQRulez_Graffiti;
    public GameObject PQRulez_Graffiti_2;
    public GameObject Logo_Graffiti;
    public GameObject GG_Graffiti;
    public GameObject GGlogo_Graffiti;
    public GameObject PhilsEmpire_Graffiti;
    public GameObject Tornado_Graffiti;
    public GameObject Hourglass_Graffiti;

    [Header("Sand Hills")]
    public GameObject Sandhill01;
    public GameObject Sandhill02;
    public GameObject Sandhill03;
    public GameObject Sandhill04;
    public GameObject Sandhill05;

    [Header("Space")]
    public GameObject Asteroid;

    [Header("Windows")]
    public GameObject Window01;
    public GameObject Window01_light;
    public GameObject Window01_3x3;
    public GameObject Window01_3x3_light;
    public GameObject Window01_6x6;
    public GameObject Window01_6x6_light;
    public GameObject Window01_12x12;
    public GameObject Window01_12x12_light;
    public GameObject Window01_3x12;
    public GameObject Window01_3x12_light;

    public GameObject Window01O;
    public GameObject Window01O_light;
    public GameObject Window01O_3x3;
    public GameObject Window01O_3x3_light;
    public GameObject Window01O_6x6;
    public GameObject Window01O_6x6_light;

    public GameObject Window02;
    public GameObject Window02_light;
    public GameObject Window02_3x3;
    public GameObject Window02_3x3_light;

    public GameObject Window02O;
    public GameObject Window02O_light;
    public GameObject Window02O_3x3;
    public GameObject Window02O_3x3_light;

    public GameObject Window03;
    public GameObject Window03_light;
    public GameObject Window03_3x3;
    public GameObject Window03_3x3_light;

    public GameObject Window03O;
    public GameObject Window03O_light;
    public GameObject Window03O_3x3;
    public GameObject Window03O_3x3_light;

    public GameObject Window04;
    public GameObject Window04_light;
    public GameObject Window04_3x3;
    public GameObject Window04_3x3_light;

    public GameObject Window04O;
    public GameObject Window04O_light;
    public GameObject Window04O_3x3;
    public GameObject Window04O_3x3_light;

    [Header("Construction")]
    public GameObject Barrier;

    [Header("PQ Signs")]
    public GameObject Sign01;
    public GameObject Sign02;

    public GameObject RoadsignYellow;
    public GameObject RoadsignRed;
    public GameObject ConstructonRoadsignYellow;
    public GameObject ConstructonRoadsignRed;
    public GameObject DetourRoadsignYellow;
    public GameObject DetourRoadsignRed;

    public GameObject Cardboardsign;
    public GameObject Carboardsign_L;
    public GameObject Carboardsign_R;
    public GameObject Carboardsign_UP_L;
    public GameObject Carboardsign_UP_R;
    public GameObject Carboardsign_DOWN_L;
    public GameObject Carboardsign_DOWN_R;

    [Header("Others")]
    public GameObject PillowOnUse;
    public GameObject Spectrum;
    public GameObject Spectrum2;
    public GameObject Spectrum3;
    public GameObject Spectrum4;
    public GameObject WireBall;
    public GameObject soundstage;
    public GameObject Marblius;

    [Header("Halloween")]
    public GameObject Bat;
}
