using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Displays the hardcoded Marble Blast Platinum help/credits pages.
/// The page bodies retain the original MBP markup and are converted to
/// TextMeshPro markup at runtime by MBPMarkupParser.
/// </summary>
public class HelpCreditsManager : MonoBehaviour
{
    [Header("Page List")]
    [SerializeField] private RectTransform pageListContent;
    [SerializeField] private Button pageButtonPrefab;

    [Header("Manual Content")]
    [SerializeField] private ScrollRect contentScrollRect;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;

    [Header("Formatting")]
    [Tooltip("TMP font asset used for the original MBP 'Marker Felt' headings.")]
    [SerializeField] private TMP_FontAsset markerFeltFont;
    private TMP_FontAsset requestedFontAsset;
    [SerializeField] private Color linkColor = Color.red;
    [SerializeField] private bool underlineLinks = true;

    [Header("Navigation")]
    [SerializeField] private Button homeButton;

    private readonly List<Button> pageButtons = new List<Button>();
    private int selectedPage = -1;
    private MBPMarkupParser markupParser;
    private HelpCreditsLinkHandler linkHandler;

    private void Awake()
    {
        if (contentText != null)
            contentText.richText = true;

        requestedFontAsset = markerFeltFont;

        // Explicitly handle <font="..."> requests made by TMP.
        TMP_Text.OnFontAssetRequest += OnFontAssetRequest;

        markupParser = new MBPMarkupParser(
            markerFeltFont,
            linkColor,
            underlineLinks
        );

        if (contentText != null)
        {
            linkHandler =
                contentText.GetComponent<HelpCreditsLinkHandler>();

            if (linkHandler == null)
            {
                linkHandler =
                    contentText.gameObject.AddComponent<HelpCreditsLinkHandler>();
            }

            linkHandler.SetText(contentText);
        }
    }

    private TMP_FontAsset OnFontAssetRequest(
    int hashCode,
    string fontName)
    {
        if (requestedFontAsset == null)
            return null;

        /*
         * Our parser generates:
         *
         * <font="Marker Felt">
         *
         * TMP gives us "Marker Felt" here.
         *
         * Return the actual asset assigned in the Inspector.
         */
        if (string.Equals(
            fontName,
            requestedFontAsset.name,
            StringComparison.OrdinalIgnoreCase))
        {
            return requestedFontAsset;
        }

        if (string.Equals(
            fontName,
            "Marker Felt",
            StringComparison.OrdinalIgnoreCase))
        {
            return requestedFontAsset;
        }

        return null;
    }

    private void Start()
    {
        BuildPageList();

        if (homeButton != null)
            homeButton.onClick.AddListener(ReturnHome);

        SelectPage(0);
    }

    private void OnDestroy()
    {
        TMP_Text.OnFontAssetRequest -= OnFontAssetRequest;

        if (homeButton != null)
            homeButton.onClick.RemoveListener(ReturnHome);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ReturnHome();
    }

    private void BuildPageList()
    {
        if (pageListContent == null || pageButtonPrefab == null)
        {
            Debug.LogWarning("HelpCreditsManager: Page List Content or Page Button Prefab is not assigned.");
            return;
        }

        // Preserve the first child if it is being used as a layout/header object.
        for (int i = pageListContent.childCount - 1; i >= 1; i--)
            Destroy(pageListContent.GetChild(i).gameObject);

        pageButtons.Clear();

        for (int i = 0; i < Pages.Length; i++)
        {
            int pageIndex = i;
            Button button = Instantiate(pageButtonPrefab, pageListContent);
            button.gameObject.SetActive(true);
            button.name = $"PageButton_{i + 1}";

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = PageTitles[i];

            button.onClick.AddListener(() => SelectPage(pageIndex));
            pageButtons.Add(button);
        }
    }

    private void SelectPage(int index)
    {
        if (index < 0 || index >= Pages.Length)
            return;

        selectedPage = index;

        if (titleText != null)
            titleText.text = PageTitles[index];

        if (contentText != null)
        {
            contentText.text = "\n" + markupParser.Parse(Pages[index]);
            contentText.ForceMeshUpdate();
        }

        if (contentScrollRect != null)
        {
            contentScrollRect.StopMovement();
            contentScrollRect.verticalNormalizedPosition = 1f;
        }

        for (int i = 0; i < pageButtons.Count; i++)
            pageButtons[i].interactable = i != selectedPage;

        RefreshTMPLayout(titleText);
        RefreshTMPLayout(contentText);

        Canvas.ForceUpdateCanvases();
    }

    private void RefreshTMPLayout(TextMeshProUGUI tmp)
    {
        if (tmp == null)
            return;

        tmp.ForceMeshUpdate();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tmp.rectTransform);
    }

    private void ReturnHome()
    {
        SceneManager.LoadScene("MainMenu");
    }

    // Page titles are the first line of each original MBP help page.
    private static readonly string[] PageTitles =
    {
        @"Marble Blast Platinum - Help",
        @"Credits",
        @"New players: Start Here!",
        @"Main Menu Interface",
        @"Options Menu",
        @"Replay Center / Demo Recording",
        @"Single-Player Level Select",
        @"Jukebox",
        @"The End Game Screen",
        @"Online: Login Interface",
        @"Online: The Main Screen",
        @"An Important Note",
        @"Online: Single Player Level Select",
        @"Online: Challenges",
        @"Online: Multiplayer - Join Server",
        @"Online: Multiplayer - Level Select",
        @"Adding custom missions",
        @"Additions to the Level Editor",
        @"Demo Recording Problems",
        @"Hidden Preferences",
        @"Console, Bugs & Contact Us",
        @"Libraries and Licensing",
    };

    // Everything after the title line of the original MBP page.
    // The original MBP markup is intentionally retained here.
    private static readonly string[] Pages =
    {
        @"Welcome! This is the help section of Marble Blast Platinum (MBP).

This section does NOT repeat anything that is said in any levels (basic rolling movement, hazards, etc.), or explains objects like Easter Eggs and Checkpoints, or what is an Ultimate Score, as they are all considered basic knowledge that can be understood by just playing the game.

This section, however, will explain our recommendation for new players of Marble Blast, as in the recommended order to play levels, a detailed explanation of every button in most interfaces, and what you can access. Likewise, the options menu will be explained in detail with our recommendations, and additionally we will reveal the hidden preferences that one may change to further enhance MBP.  To top it all off, a number of useful guides are included towards the end.

If you have any further questions, do not hesitate to either ask us on the forums or e-mail us at marbleblastforums@gmail.com",
        @"In some games and mods, credits come last. Not here. The following people have helped develop Marble Blast Platinum over the last years both directly and indirectly. Their contribution has made this mod what it is today, and we cannot thank them enough. Whether you want to praise them, or just say a Thank You next time you see them, make sure they feel good about their hard work.

The developers of Marble Blast Gold (the game that this mod is based on):
Alex Swanson, Mark Frohnmayer, Jeff Tunnell, Brian Hahn, Liam Ryan, Tim Gift, Rick Overman, Kevin Ryan, Timothy Clarke, Jay Moore, Pat Wilson and John Quigley. Special thanks to Kurtis Seebaldt for integrating Ogg/Vorbis streaming into the Torque engine, and to the Ogg/Vorbis team. Special thanks also go to Markus F.X.J. Oberhumer, Laszlo Molnar and the UPX team for the UPX executable packer.

<just:center><spush><font:Marker Felt:24>The Marble Blast Platinum team<spop>

<spush><font:Marker Felt:24>Project Founders<spop>
Phil
Matan
Jase

<spush><font:Marker Felt:24>Marble Blast Platinum 1.50+ Project Leaders<spop>
Matan
HiGuy
Jeff

<spush><font:Marker Felt:24>Core Programming<spop>
<spush><font:Marker Felt:18>Engine Modifications<spop>
Amd42
HiGuy
Jeff

<spush><font:Marker Felt:18>Gameplay and Features<spop>
HiGuy
Jeff
Amd42
Seizure22
Whirligig
Spy47
ShadowMarble

<spush><font:Marker Felt:24>Online & Leaderboards<spop>
<spush><font:Marker Felt:18>Programming and Protocol Design<spop>
HiGuy
Jeff
Amd42
Aayrl
Spy47

<spush><font:Marker Felt:18>Multiplayer & Challenges<spop>
Matan
HiGuy
Jeff

<spush><font:Marker Felt:24>Graphics & User Interface<spop>
HiGuy
Phil
Jase
Spy47
MadMarioSkills

<spush><font:Marker Felt:24>Level Design<spop>
Matan
Phil
Pablo
Ian
Technostick
Andrew Sears
Perishingflames
Lonestar
Oaky
Jase
Moshe
Threefolder

<spush><font:Marker Felt:18>Additional Level Design<spop>
Xelna
Darkness Shadow

<spush><font:Marker Felt:24>Texture Design<spop>
Phil
Jase
Ian
Mkbul
Lonestar

<spush><font:Marker Felt:24>3D Modeling<spop>
CyberFox
Phil
RDs.Empire

<spush><font:Marker Felt:24>Music and Effects<spop>
Beau
Phil
Matan
Buzzmusic

<spush><font:Marker Felt:24>Community Hype-ist<spop>
Aayrl

<just:center><spush><font:Marker Felt:24>Special thanks to:<spop>

GarageGames
Alex Swanson
Ben
Our parents and friends
Tsuf
Thistle
The Marble Blast community
",
        @"Marble Blast is an excellent game that is pretty easy to pick up. However before you jump into it, you should know there is a certain game order. Follow it and you'll gain maximum enjoyment and you'll become good as well.

Marble Blast Platinum (MBP) was developed and released at a time where hard and sometimes long levels were all the rage. As such its levels are much harder than those in Marble Blast Gold (MBG), and they offer advanced techniques to match. These techniques are used in many custom levels as well, making it a must to understand how they work and quickly get better at them.

If this is your first time in Marble Blast, stop right here. Your first priority, after configuring the Options, is to go to Gold's levels from the singleplayer level select screen, and choose the beginner difficulty. Start progressing from there. These easy levels will help you to learn how to control the marble, and they will teach you the basic techniques of this game. It is recommended you play all of these levels (and Platinum's) on the leaderboards, as you can get ratings and achievements, talk to community members, and make friends.

Once you're through beginner, continue to intermediate and then advanced. Levels will progressively get harder, but you will learn. If you get stuck, take advantage of the removal of any qualifications; you can pick any level to play, so skip right to the next one. Eventually you'll run out and just be stuck with a number of levels you cannot complete.

At this stage you should go to Platinum's beginner levels, and start from there. You will immediately notice the difficulty increasing, and the techniques you learn are more advanced. For example, learning diagonal movement as the new basic form of movement, and advanced techniques like wall hit and edge hit. You will find them difficult, but keep pressing on and you'll pass them.

After you've finished the beginners, you can continue with Platinum intermediates and onwards. Again you'll find the difficulty becoming harder pretty quickly, so once you're stuck, stop there and go back to Gold. You will find the previously hard levels much easier to beat, and you will be able to beat most if not all of them.

Now that you've done that, start aiming to beat as many Gold Times as possible. Thanks to the techniques from Platinum, you'll be able to attain a lot of them. As you get better there, you will find yourself returning to Platinum and beating levels there more easily. Eventually you will get to the point where you can start beating the Ultimate Times, Platinum's hardest challenge. Further progress from there includes watching videos and mimicking others, learning their tricks and improving all the time.

While you're doing all that, you can also hop in to Multiplayer. You will find a range of players with different skills. Don't worry if you get beaten down, as some players have played this game for years and have had thousands if not tens of thousands of hours of experience, grinding down many levels. Some players will be easier to beat and you'll learn how to play Multiplayer with their help; don't hesitate to ask for helpful advice as well, we'll be happy to teach.

Our community is very friendly and we are always happy to meet new people. Don't be afraid if you feel bad, we all started where you are and got better with practice. Essentially if you are fun to play with, you will always get people to play with you, no matter your skill level.

Enjoy Marble Blast!",
        @"The Main Menu is composed of seven buttons:
-	The Play button allows you to play Marble Blast Gold and Platinum offline, as well as custom levels which you can download at MarbleBlast.com
-	The Online button allows you to play competitively against users of the Marble Blast Community
-	The Options button allows you to configure many basic options in Marble Blast Platinum to suit your liking
-	The Quit button to quit the game. Don't press it :)
-	The Tip of the Day button, which provides news and updates or just tips. Disclaimer: does not actually provide a tip on a daily basis.
-	The Replay Center button allows you an easy access to all recordings you or other users have done in Marble Blast.
-	The Help button, which leads you here. Not to this page, no, but the first one.",
        @"You can access many configurable items in the Options menu. This page is dedicated to explain most of the options (as some are rather obvious), and it gives out a few recommendations and tips.
Please note that both General and Hotkeys tabs have a scrollbar on the right, so you can scroll down for more options.

General Tab:

Screen Resolution: Choose how big you want your Marble Blast window to be. If you choose Windowed mode in Screen Style, it will let you choose a maximum size that doesn't fill the whole window. We recommend 1280x720 and above for HD monitors, and 1024x768 or greater for SD monitors.

Screen Style: Choose whether to play full-screen, or in a windowed mode so you can see other programs.

Color Depth: 16-bit or 32-bit. Default to 32-bit, you should rarely change to 16-bit unless bad quality is something you like, or have a bad graphics card.

Graphics Quality: Low, Medium or High. Default to High, you should only use a lower quality if you experience graphics driver bugs, or older hardware where shaders (OpenGL 2.1+) cannot be run.

Anti-Aliasing: Default to Off, this makes the geometry avoid jagged lines and graphical anomalies. The range is from off to 16 samples. 
Note: The Mac engine defaults to 4X AA and cannot be changed.

Frame Rate: Defaulted to Visible. Allows you to see your current framerate in-game. Windows users will find 64fps as maximum, while Mac users can go into the hundreds unless VSync is enabled, where it will then drop to 64fps as well.

Out of Bounds (OoB) Insults: Defaulted to Disabled. If enabled, this will add a randomized insult every 200 OoBs. Some people like it.

Free-Look: Defaulted to Enabled. Allows the camera to move in all directions just by moving the mouse/trackpad. Makes gameplay easier.

Live Replays: Defaulted to Disabled. When enabled, it activates when you beat a level and then replay that level. It will show you your best-time live-replay recording so you can race against yourself and beat it ad infinitum.

Invert Mouse: Defaulted to None. You can select between X axis, Y axis or both X and Y axes With an inverted X axis, moving the mouse right will move the camera left, and inverting the Y axis and moving the mouse up will move the camera down.

Thousandths: Defaulted to Disabled. When Enabled, all in-game timers including displayed times will show time to the thousandths of a second. Makes you feel horrible to be beaten by one thousandth of a second.

Music Volume: Move the slider to change the music volume.

Sound Volume: Move the slider to change the sound volume.

Mouse Speed: Move the slider to change the speed the camera will move when the mouse is moved. Put at the highest for super-crazy not-fun times!

Keyboard Speed: Move the slider to change the speed the camera will move when the keyboard keys are pressed. Put at the highest for super-crazy not-fun times!

Field of View: Defaulted to 90. Increase to see more of the level around you, and decrease to have the camera really close to the marble. Since the extremes are pretty unplayable, we kept it at levels that people often like playing, which are 60 to 140.


Hotkeys tab:

This section is split into three main parts: Basic Controls, Online Controls, and Multiplayer Taunts.

Basic Controls:

Movement keys are self-explanatory: select the buttons on your keyboard and mouse which you want to use when moving your marble, your keyboard camera, jumping, and usage of powerups.

Free Look: This only works when always free-look is disabled. This allows you to press a key before you can move your mouse to look anywhere freely.

Respawn: Select a button which, when pressed, will respawn your marble. In Singleplayer games it will restart the level for you. If a checkpoint was hit, it will respawn you at the last checkpoint. A long press (1 second) will restart the level fully. Respawning in Multiplayer games will respawn your marble somewhere random on the map. To stop abuse of this ability in Multiplayer games, you can only respawn once every 25 seconds. If you are the host, holding the respawn button for 3 seconds will completely restart the match. You can respawn as many times as you want in Singleplayer; there is no limit.

Screenshot mode: Hides the interface (time, chat, etc) so you can take awesome screenshots. Press once to hide the interface, and once more to show it again.


Online Controls:

Use Blast: Defaulted to 'E'. Select a key which will allow you to use the blast in Ultra and Multiplayer levels/games.

Toggle Radar: Defaulted to 'Tab'. Select a key which will allow you to toggle between the three radar modes (or disable radar). The purpose of the radar is to indicate where gems are and their type: red, yellow, blue or platinum colored.
The first radar mode shows you a small radar on the top right. The second radar mode has the radar take up the whole screen. The third radar mode will lock into the gems when they are in sight (small gem icons on-screen). When they are not on camera, gem arrows will point in their direction.

Spectate Mode: Defaulted to 'C'. When spectating, this changes between being able to see the entire field and move the camera around (free-fly mode) or follow a player's marble.
Pressing Ctrl+C allows you to enter or leave Spectate Mode. This cannot be rebound.

Score List: Defaulted to 'O'. Allows you to see detailed scores of all players in the match – including the number of red, yellow, and blue gems that every player caught.

Global Chat: Defaulted to 'T'. Allows you to talk to other users online while playing in Singleplayer and Multiplayer matches, as well as during Challenges and Super Challenges.

Server Chat: Defaulted to 'P'. Allows you to talk to users within the current server. Multiplayer only.

Team Chat: Defaulted to 'M'. Allows you to talk to your team-mates without anyone else interfering. Multiplayer in-game only when Team mode is enabled.

Multiplayer Taunts: Select keys to shout a taunt during those exciting Multiplayer matches. Available in Multiplayer only.
Alternatively you can write down ' /v1 ' to ' /v17 ' in server chat to shout taunts. As you may have noticed not all taunts have keys and that is intentional.


Online Tab:

Score Predictor: Defaulted to Disabled. When enabled, the game will attempt to predict your end-game score as the game progresses based on the amount of points you have collected. It becomes pretty accurate as the game passes the half-way mark. Multiplayer only.

Radar Z-Axis: Defaulted to Enabled. Makes gems appear smaller or larger on the radar depending on their height (Z-Axis) when compared to your marble.

Server Port: Defaulted to 28000. Select a port number that you want to have Marble Blast Platinum be port-forwarded from. 28000 is a legacy number from when Marble Blast Gold was created. You can choose any other port number, such as 44431. Range of ports allowed: 1-65535.
When port forwarded, others may join your Multiplayer matches when you host.
To join matches, you do not need to port forward.

Server: Defaulted to leaderboards.marbleblast.com. Cannot be changed as this is the server currently in-use when connecting online to the Marble Blast Platinum leaderboards.

Overview Speed: Defaulted to 120. Click on the button and move it along the slider to change the speed in which the camera will rotate around levels in the preview screen. Multiplayer only.

Auto-Login As: Defaulted to None. This enables you to automatically log in as a guest or a user to the Online section, bypassing the log in screen. If you select User, you need to enter your Online Username and Password to their respective fields.",
        @"The Replay Center is the hub of saved demo recording in Marble Blast Platinum. When recording a demo anywhere in MBP, you are asked for a filename for the demo, and on completion of the demo (exiting the level counts as such) you can give a short description, the demo's actual name and your name as the author.

When first choosing the demo recording option, it also asks you about an OOB count. This is the amount of times a player can go Out of Bounds and restart the level before the recording will automatically reload the level. Hitting a checkpoint and going Out of Bounds does not affect this.

Every demo displayed in the Replay Center goes by the name you've given it at the end of your demo recording session. Click on any of the names to show the demo's level played, author and description.
You can then press Play to playback the demo file. Press Home to return to the Main Menu.

You can find demo recording files (file type extensions .rec and .inf) in platinum/client/demos. .inf files contain info that is seen in the Replay Center. It is recommended not to edit this file as it can cause desyncs.

Remember:
You cannot view .rec files using any other programs except Marble Blast.

You can read more about the problems with Demo Recording in Marble Blast Platinum in its appropriate page later in this Help section.",
        @"The Level Select screen is where you can choose which levels you want to play, and the game or category to play. Available to play are Marble Blast Gold levels, Platinum levels and Custom levels.

Marble Blast Platinum 1.50 changed the way the interface works.
To access Gold, Platinum, and Custom levels, you will have to press on the difficulty dropdown menu on the top. This will also list the available difficulties playable: Beginner, Intermediate, Advanced, and if Platinum is selected, then Expert as well. Custom levels are accessible if Gold levels are chosen.

Right below the difficulty tab you will see the level's name, description, author, and whether you need to beat a Qualify Time (if Gold) or Par Time (if Platinum). These times are a challenge, with the goal being to beat the level under a time pressure. If an Easter Egg is present in the level, then a small grey and white image of an Easter Egg will appear on the bottom right of the level picture. When you collect the egg, this small image will turn blue and white to match the egg's colors.

Hover above 'Best Time' below the picture to see the text switch to 'Show 5 Top Times'. Pressing it will change the description display to your top times display. If you're playing Gold, the level's Qualify and Gold Times will appear on the bottom to show you what your goal is. If you are playing Platinum, it will instead show the level's Platinum and Ultimate Times. Hovering above the 'Best Time' again will change to 'Hide 5 Top Times,' and pressing it goes back to the level description text.

The three buttons that appear on the bottom left of Level Select screen are:
Menu: Go back to the Main Menu
More...: Press it to reveal additional options
Magnifier icon: Press to open the Search window to search levels.

Using the search:
When searching for levels, the search window will list all the levels in the current game: Gold levels in Gold, Platinum levels in Platinum, and Custom levels in Custom.
You can search using the level's name (Title) or press Options to search by the Artist or file name (e.g. Custom/MyLevelRules.mis). You can press the dice button to automatically load a random level; an excellent choice if you don't know which one to play.

The More... tab lists the following buttons (Left to Right order):

Marble Select: Choose between 40 default marbles with different skins and sizes. When pressing the 'Cat' (Category) buttons, you can switch to custom marbles like unique CyberFox marbles and the Marble Blast Ultra marbles.

Statistics: Whether you are playing Platinum or Gold, you can see up-to-date statistics about your game, as well as some other tracked data. The 'Grand Total' is all your best times on all completed levels combined, and the 'Total Wasted Time' is how long you have played Platinum overall. It starts counting from the application's launch until you exit it, and updates every time you launch the statistics.
Statistics are disabled when Custom is selected.

Achievements: 8 achievements are available when playing the Platinum levels. Can you beat them all?
Achievements are disabled when Gold or Custom are selected.

Level Editor: Press to enable the Level Editor. When you first launch it, a help window will explain how to activate it and where. You can uncheck the 'Show that reminder again!' if you don't want this window to appear again when activating the Level Editor.
In case you forget, F11 activates the level editor.

Demo Recording: Press to activate Demo Recording. Fill in the file name of the demo (e.g. AwesomeDemo) and move the slider to choose the amount of times you can go Out of Bounds before the recording will automatically reload the level. Hitting a checkpoint and going Out of Bounds does not affect this.",
        @"The Jukebox can be entered anywhere from the game by pressing Ctrl+F5. In the window that opens, there will be a list of all available soundtracks and the one currently being played.
Ctrl+F6 plays the previous soundtrack.
Ctrl+F7 stops the resumes the current soundtrack.
Ctrl+F8 plays the next soundtrack.

You can add your own soundtracks in platinum/data/sounds/music. These must be in .ogg (Ogg/Vorbis) format.
You can convert soundtracks from .wav and .mp3 to .ogg format using Audacity, which is available for Windows and Mac free of charge.

The Jukebox also appears on the bottom right of the pause menu screen, so you can access it from there as well.",
        @"The End Game screen is mostly self-explanatory. The only part to note is that the level picture on the bottom right of the screen can be clicked on to go immediately to the next level, whereas with 'Continue' you will be sent back to the Level Select screen. You can press Replay to play the level again.

In Multiplayer, the End Game screen lists player names, teams names (if Team Mode is enabled), total score, a breakdown of gems collected, and marble skin used. Ratings are displayed if ratings are being calculated (otherwise it shows N/A), and how much a player won or lost.
On the right side of the screen, from top to bottom, the host of the match can:
Replay the level
Quit to Level Select
Quit to the Leaderboards chat
Save Spawns: Save the match's spawns as they appeared in the match.

Note: When playing Singleplayer levels online, you will be shown the rank for that particular level on the leaderboards based on time you reach.",
        @"Assuming you have not set Auto-Login in the Options, this screen is the first you'll meet when you enter the leaderboards.
This screen shows you the amount of players currently online on the top left, and asks for a Username and Password. You can tick a checkbox to remember your password. Pressing the Login button then logs you in to Marble Blast Platinum Online.
You can also log in as a Guest by pressing the Guest button. Guests cannot write to other users anywhere, have no statistics tracked, and their times, scores and achievements are not recorded.
Press the Home button to go back to the Main Menu.

The Create Account button allows you to create an account for both the website and Marble Blast Platinum Online. It asks for a Username and Password of your choice, as well as an e-mail. You MUST provide a working e-mail, as you will be sent an activation key that, without it, you won't be able to login.
Please read the Terms of Service, and tick the checkbox on the bottom.

If your version is lower than the one on the server, you will be asked to download the latest version.",
        @"Welcome to Marble Blast Platinum Online! This is the main hub where you can access everything that deals with Platinum Online.

The main window is dedicated to the server chat, and a list of who is online. Every user has in brackets his current status, such as Hosting, Playing, Super Challenge, Away and more.

The buttons that exist are:
Single Player: Play Singleplayer levels of: Gold, Platinum, Ultra and Customs. Please note you cannot add your own custom levels, and therefore are limited to the ones provided for you. There are more than 500 levels to play, so there is definitely no shortage. You can also play Challenges and Super Challenges from there. More on these later.

Multiplayer: Play Multiplayer levels; you can either play the official levels (which have ratings enabled) or custom levels (ratings disabled). You can add your own custom levels and share with the community, but more on that later.

Leaderboards: Check out the current Leaderboards ranking on 7 different areas: General ratings, Platinum levels, Gold levels, Ultra levels, Custom levels, Challenge points, and Multiplayer ratings. General ratings are cumulative of Platinum, Gold, Ultra, Custom and achievement points won from these games. Challenge and Multiplayer are affected by their respective achievements as well, and do not contribute to General ratings. You can press the right and left arrows to scroll between the rankings.

My Profile: You can loads your profile information and statistics from here, and also see who are your friends. The buttons on the bottom are:
Close: Close your profile and return to the main hub
Achievements: Load the achievements window. It will load all the achievements that can be completed Online in this sequence: Gold/Platinum, Ultra, Challenges and Multiplayer.
Statistics: Loads all your statistics for the various games, and gives you a thorough breakdown.

Options: Go to the Options Menu

Log Out: Quit Platinum Online and go back to the Main Menu. Don't press it :)

You can press on anyone's username on the right side to load their profile. You can then see their user information, and also check out their achievements and statistics.
Most users are in black color. Users in blue color are Moderators, and those in red color are Administrators.

Helpful commands to write in chat:
/help format : Tells you how to use basic formatting styles in chat, such as bold and italics, as well as using colors.
/help cmdlist : Tells you a number of commands that you can type in chat, such as /me and /slap among others.",
        @"Please be aware that during all Online plays, be it Singleplayer, Multiplayer, Challenges or Super Challenges, pressing the ESC button will NOT pause the game. The timer and all in-game events will continue normally. That is because pausing online doesn't actually pause the game.

The reason behind that is that pausing the game causes the game to stop ALL events and not just game-related events. Some of these stopped events include incoming and outgoing connections. This causes loss of connection to Platinum Online.",
        @"The Level Select screen is quite similar at the top half to that of its offline counterpart. You no longer need to hover anywhere to see your times to see what are the Qualify/Par, Gold/Platinum, and Ultimate Times to beat.

Instead the level select shows you your top 10 times, as well as the ratings each one gave. Only your top time and rating goes to your total rating score. The 'Global Best Times' shows you all the times from all users* for a particular level, and you can click on the arrows next to it to go to the next ranks. If you beat a level but did not beat its Qualify/Par time, your score and ratings will still be calculated.

The top category dropdown now lists Marble Blast Ultra levels, and Custom levels. Pressing Custom will give its own list; Director's Cut are additional Platinum levels that didn't make it to the official levels, and Level Packs are packs of 50 levels (except level packs 1-9, which is 43) that can be played. The level packs were done based on numerous themes:
Level packs 1-9 are made from Level of the Month/Year winners (2007-2008)
Level packs 10-19 are made from old MBG custom levels (2003-2005) that were downloadable via GarageGames
Level Packs 20-29 are made up from several themes: LotM/Y winners (2009-2010), old MBG custom levels (2003-2005), levels from Bobby, Phil's Old Levels, and several of Matan's levels
Level packs 30-39 are made up from a selection of levels from Custom Levels Archive, 15 of Buzzmusic's favourite levels, and levels there were released on MarbleBlast.com. These levels range from 2006-2014.

The buttons on the bottom of the Level Select are identical in function to their offline counterparts. Nonetheless the options in 'More...' do not have the Level Editor button (as it's disabled Online), and instead lists the following (left to right order): Marble Select, Statistics (Online), Achievements, Demo Recording, Challenges and Super Challenges.

* Note: users with an asterisk (*) next to their rating are those who haven't imported their scores on MarbleBlast.com, but those would be their ratings they'd get if they were to. That is because in 1.50+ the rating formulas and score calculations slightly changed for every level, hence the original PhilsEmpire ratings are now different to the current ratings.
",
        @"
Challenges and Super Challenges are two new game modes, introduced in 1.50. Challenges allows you to play a 1v1 against a person online on a certain level, and the person who has either the lowest time or finishes a level first, wins. Super Challenges take place on several levels in a row, and can have up to 4 players competing against each other.

Note: you can only challenge players who are idling in chat. Those currently playing in Singleplayer, Challenges, Super Challenges, or Multiplayer, cannot be invited. For obvious reasons, you cannot Challenge or Super Challenge players who are in the Webchat or in other mods, such as Fubar or PlatinumQuest.



When pressing Challenge, the level you are currently on in the Level Select screen is the level that will be played.

Challenges window shows you the available players to challenge as well as the following:



Race: If selected, two players race to the finish. First to beat a level, wins.



Attempts: If selected, two players will replay the same level a number of times, and the person with the lowest Final Time at the end of their attempts will win. Final Time is the time displayed in-game.



Timeout: When a player finishes a Race or their Attempts, a Timeout countdown timer will appear on the bottom right of the screen, above the chat, for the opposing player. That player has the amount of time pre-selected to finish the level (or Attempts) or risk an automatic loss. The Timeout timer is in minutes, and goes from 1 to 20 minutes.



Attempts: Appears when Attempts mode is selected. Move the slider to choose how many attempts every player can have for the chosen level. An Attempt counts as: a player finishes the level, the player goes Out of Bounds (no checkpoints hit), the player resets the level. If you finished the level, you wlll be forced to replay the level again to try and get a better time.





Super Challenges work slightly different to Challenges. Super Challenges contain several levels which are played in a row, and the objective is to be the first to finish all of them. You can see what levels each Super Challenge contains on the level slideshow on the bottom right, as well as the amount of levels in the Super Challenge. In addition the cumulative total of Gold/Platinum and Ultimate Times display, as well as your own progress of the Super Challenge, and if completed, it will display your best score and percentage. The percentage is based on the amount of levels completed and how many times you have had to restart either manually or because you fell Out of Bounds without hitting checkpoints.



Super Challenges can be played with up to 4 players (selecting three other players from the list), or played by yourself in practice mode (select practice mode on the list). You can also see which achievements are available (these are for both Challenges and Super Challenges), and select the following options:



Timeout: When a player finishes a Super Challenge, the other opponent(s) receive a Timeout countdown timer on their bottom right of the screen, above the chat. That player has the amount of time pre-selected to finish the level (or Attempts) or risk an automatic loss. The Timeout timer is in minutes, and is usually either 1 or 2 minutes. Most Super Challenges contain a pre-defined Countdown timer that lasts from 5 minutes and up to 1 hour, depending on the length of the Super Challenge.



Show Progress: Show your progress on the bottom left of the screen (in-game).



Show Percents: Show your percentage as you progress in the Super Challenge (in-game). The more times you restart a level, either manually or by going Out of Bounds if no checkpoints are hit, then the lower the percentage is.



Real-Time Attack (Practice): Changes the way practice mode works by comparing your best time to your current run, and then reports to you the time difference (whether you're behind or ahead) on every level. It also gives you your best time ever, your Personal Best (PB), the total time you've currently played, your current time, and the Sum of Best Segments (sum of your best times on every level in Super Challenge RTA mode). This is an alternative to LiveSplit and other timer programs, and is used for speedrunners who want to stream.



Winning Challenges and Super Challenges rewards you with Challenge points. Super Challenges are worth more points, and the more players playing in a Super Challenge, the more points you gain for winning that particular Super Challenge. Players who have the best lowest overall time, gain an additional bonus of several points. This is a good incentive to get the lowest overall time even if you have lost the Super Challenge.

Note: Even if you lost a Challenge or a Super Challenge, you still gain at least one point for completion.



Achieving either Platinum or Ultimate on your percentage nets you an additional amount of points per Super Challenge. This is awarded every time you finish a Super Challenge in Platinum or Ultimate percentages.



It is also possible to forfeit from Challenges or Super Challenges. However doing so loses points, and this point loss is greater as there are more players in the Super Challenge and as the Super Challenge is more difficult or long.

",
        @"The Join Server screen is where you can join and host Multiplayer matches, as well as find information about the different servers that are being hosted. Every server shows the following information (left to right):
Lock icon: If appears, server requires a password to be able to join
Scores icon: If appears, server has ratings enabled
Server Name: [Status] and Server Name. The two statuses possible are: Lobby, which means players are in the main Level Select screen, and Playing which means a match is in progress.
Players: Displayed as: Amount of players in the server / Maximum players that the server supports
Mode: FFA or Teams. FFA is Free For All, where everyone plays against each other. Teams mode pits players one team against players in other teams.
Ping: Displayed number is your current ping to the server. The lower the number, the better. Higher numbers mean more lag, which affects gameplay badly.
Min Rating: By default it's N/A (Not Applicable), but if the host sets it, it will show the minimum rating that any player can have in order to be able to join.

When you click on a server, the right side will fill in with the server info, such as the Server Name, Server Description and a list of Handicaps (if any are set).

On the bottom of the Join screen you will find the following buttons (left to right order):
Leave: Go back to the main hub
Direct Connect: If you can't directly connect to a user, you can input their IP address and join from there.
Refresh: Refresh the list, and check for available servers.
Server Settings: Press it to open up your Server Settings
Host/Join: Self-explanatory. Either press Host to host a Multiplayer match, or click on a server and press Join to join that server.

By default there should be three dedicated servers running: Thistle, Tsuf Mk 1 and Tsuf Mk 3. Tsuf Mk 3 is a development server only, and is accessible only to Administrators.

Server Settings:  In here you can control most of the settings your server will use when you host a Multiplayer match. This is what everything does:

Server Name: Give a name to your server. Otherwise it uses your username as the Server Name.
Password: Putting a password means that only users who know the password can join, which happens if you tell them. Leave empty to let everyone in.

Display on Server List: Checked by default. Allows everyone to see your server on the Join Server window. If you uncheck it, other users can only join if you give them your Server Address (on the bottom of the screen) by inputting it via Direct Connect window. This will take up to 2 minutes before taking effect.

Preload Missions: Checked by default. When pressing Preload button on the Level Select screen, the missions will load on that screen rather than throw users to the in-game Loading screen. If you really want the old loading screen when you load levels, uncheck this.

Fast PowerUps: Unchecked by default. Normally when playing Multiplayers matches, those who join your server take a certain amount of time before a PowerUp they collect is registered as picked up. This amount of time depends on lag, and the worse their lag (higher ping), the longer this takes. Fast PowerUps solves this problem by giving the players the PowerUp immediately, regardless of lag. The downside is that sometimes picked up powerups aren't registered as picked up by the player. While it is unchecked by default, it is encouraged to have it on, and ask those in your server if they want it on or not.

Calculate Ratings: Checked by default. When a match is being played, all players (except Spectators) receive points when the match ends. Whether they win points or lose depends on how their ranking at the end of the match. Maximum gain/loss is 32 points.
New players to Multiplayer have their rating and ranking invisible, and written as Provisional instead. These players will maintain their Provisional status for 20 rated games, whereupon then they are listed as Established players and will have their rating and rank show up. Provisional players win/lose less points than Established players.
After 50 games, a player can reset his rating back to 1500 points and wipe out Multiplayer scores, and as such becoming Provisional again. You can reset your rating by going on the MarbleBlast.com website, and viewing your profile where one of the options allows you to reset your rating.
If Handicaps are enabled, then depending on the level the winner and loser will get further adjustment to their ratings based on the handicaps enabled. That makes the maximum rating gain/loss higher than 32 points.

Force Spectators: Unchecked by default. When players join a match, they will automatically be asked whether they want to Spectate the match (meaning become an Observer) or join the match and play while it is in progress. By enabling Force Spectators, all players joining a match in progress will automatically become Spectators until the match ends.

Max Players: 8 by default. Select how many players can play in your server. Use the + and - boxes to increase or decrease the value. Minimum is 2, maximum is 64.

Min Rating: N/A (Not Applicable) by default. If you press the + (increase) button, you will limit the players who can join your server to those with the minimum rating listed and above. Pressing - (decrease) will decrease it back down to 0 (N/A). The increase and decrease is in multiples of 50, and the maximum rating limit is -500 points.

Port Mapping: If you have not managed to port forward (see Options for the Server Port to see what port number you should forward), then a LAN Only message will be displayed. If the message shows Loading, it means the game hasn't found out yet if you managed to port forward. If you managed to port forward, the message will be Successful.

Server Address: Your IP address. Give it out only if players need to use Direct Connect to enter your server.

Server Info: Enter a description for your server. Can be left blank.",
        @"The Multiplayer Level Select screen is where you will be able to select and play with other users Multiplayer matches, as well as change many server settings.

At the top there is dropdown difficulty menu for the official levels: Beginner, Intermediate and Advanced. Custom can be chosen if you want to play Custom levels with others, or share your own Custom levels with players currently in your server. Custom levels NEVER give ratings even if Calculate Ratings is enabled.

Every level has a Duration. This is the amount of time the level will be played (time counts down rather than up) before the match ends.
Official levels also have a Par, Platinum Ultimate Scores. Those are fun challenges that you may attempt to beat. If other players join your match, these scores change to lower amounts so you can try and beat those while playing against others. They do not affect ratings, but can contribute to Achievements.
You will also see in the level description the author of the level as well as the current game mode, Free For All or Teams.

Below the level picture, you will be able to see the following (left to right order):
Download Options: Offer other users in your server to download your level. You can check Enable Download to allow players to download the level. Checking Allow All gives everyone access to download the level. If you don't want to allow everyone, you can individually select players who can download by checking the checkbox next to their name.

Best: Your best score on the level and whether it is Par, Platinum or Ultimate. This changes between solo player and versus.

Top Scores/Level Information: Toggle the description between Multiplayer scores leaderboards and the Level Information. When Top Scores is toggle, you can see your top 5 scores, the scores of everyone in the current server, and the All-Time scores. You can press the right and left arrows to see everyone's score and rank on the level. When Top Scores is viewed, you will also be able to toggle between Solo and Versus (Vs.) scores. Solo is when playing by yourself, whereas Versus is against other players.
When Top Scores is toggled on, you will notice below that every user in the server has information:
Rating: User's current rating and last change. +0 if this is the player's first match in the server, else it will show + or - if the player had won or lost the previous match.
Last: Last score of the last game the player has played, regardless of current map chosen in the Level Select screen.
Best: The player's best score in Versus mode in the current map that is chosen.

The bottom half of the screen is mainly dedicated to Server/Global chat, and the userlist of who is currently in the server or online. You can talk to players in the server privately in this manner without annoying everyone else in the global chat.

On the bottom, the buttons that are available (left to right order) are:

Leave: Go back to the main hub of the Leaderboards

More...: Press it to reveal additional options

Magnifier icon: Press to open the Search window to search levels. This acts in exactly the same way as Singleplayer search, except it includes Custom levels as well.

Kick/Ban Players: Press to be able to kick or ban players from your server. Select the player and press Kick or Ban to kick or ban them from your server. Banned players cannot join back. Press on a banned user and press Unban to allow them entry to your server again.

Show Server/Global Chat: Toggle between Server and Global chats (and userlists). If the button highlights in green, someone has mentioned you in the chat. Feeling special?

Preload/Play: Preload the level, and then press Play to start. When a level is preloaded, you cannot change the level, use Spawn Saves, use the search button, change difficulty, and use the Download Options buttons. To cancel preload, click the left or right buttons and a message window will appear asking you if you want to cancel it. If Preload Missions is not checked in Server Settings, then the button will automatically show up as Play.

Spawn Saves: Press it to load a Spawn Save, which was saved from a match that has been played. It will load the spawns in the same way that the match had, so that you can see how far some matches could have been played, or to show to others how much bad luck you got. All Spawn Saves matches are unrated. To select a Spawn Save, simply click on the name of the Spawn Save you have given it and press Preload/Play (the Spawn Save button will turn green when one is selected). Click the Spawn Save again to not play the Spawn Save (Spawn Save button turns back to purple).


The More... button lists the following buttons (Left to Right order):
Marble Select: Choose between all available skins for your marble. You can unlock additional skins if you complete enough Multiplayer Achievements.
Server Settings: Change current Server Settings
Achievements: Take on 30 challenging achievements.
Teams Mode: Toggle Teams Mode on or off. If toggled on a new button, Team Information, will appear between the Kick/Ban Players and the Show Global/Server Chat buttons. In addition, the Free For All text changes to 'Team Name:' and your team's name. On the bottom right of the screen, the team name of every player is listed next to their names.
Handicaps: Press it to show all possible Gameplay Settings.

Handicaps/Gameplay Settings: In this window you can disable certain gameplay elements from being playable, or select one of three additional game modes:
Matan Mode: Named after the titular player, in this game mode the gems will respawn automatically every 10 seconds, regardless of whether all the gems in the previous gem spawn were collected.
Glass Mode: For two or more players only. Adds a glass in the middle of the map, and players scramble to see who gets the luckiest in terms of gem spawns.
Balanced Mode: The game looks at how many points you have and compares it to the Ultimate Score and the time remaining on the match. The closer your score is to the Ultimate Score, the less chance blue and yellow gems will appear, and the further away the gems will spawn from you. In Versus mode, only the top player's score is checked. The time remaining is used to estimate your final score.
Note: all of these three modes will disable ratings.

Team Information: Opens the Team Info and Team Chat window. Here you can see your Team name and description, as well as your current role in the team and the players that are in it.
Pressing the 'Join' button will open the 'Join Team' window, where you can select which team to join. Simply click on the team and press Join.

If you want to make your own team, press the 'Create' button in the Join Team window. The newly opened Create Team window will allow you to give a Team Name/Description, as well as choose a Team Color. You can also choose whether to make the team a Private team, meaning that the team is invite-only. Once set as Private, only the Team Leader and can assign players to that team. However the Server Host can join any team he wants even if it's set as Private.

Once you've pressed the 'Create' button, you will be able to either close and go back to the Level Select screen, or press the new 'Options' button which gives you Team Options. These options include changing the Team Name/Description, Team Color and whether the team is private or not.
In addition to those, you will see which players are on the team, and a button appears on whether you want to leave the team or not.
The Team Leader also sees the following buttons:
Promote: Promote a selected player to become the new Team Leader.
Kick: Kick someone from your team. They probably deserved it.
Invite: Invite a player to your team. You want those players on your side!
Delete: Delete the team. The next one will be better anyway.
Leave: Any player (not just the Team Leader) can leave the team, but if the Team Leader leaves, then someone random on the team becomes the new Team Leader.


In addition on making your own servers, you can also join either Tsuf Mk 1 or Thistle. If you're the first to join them, you become the Host. While the Dedicated Servers' Level Select screen is identical to that if you were to make your own server, the only real difference is with the Dedicated Server Settings (under 'More...' button, where the Server Settings button is). The Dedicated Server Settings allow you to change the following options: Calculate Ratings, Force Spectators, Max Players.
When you leave the Dedicated Server, the settings will go back to their defaults even if someone else becomes the host.",
        @"You can add directories under platinum/data/missions/custom such as custom/mylevels or custom/Jayer, add missions there, and then select the custom subdirectory you have created. This way you can organize levels however you would prefer.

To add custom missions to the game, follow these guidelines:
Important: If you are using Windows, it is better if you go to Tools -> Folder Options -> View and untick  'Hide extensions for known file types'. This will make all extensions visible, which is very helpful when moving files within Marble Blast, and also when you want to know what is prefs.cs.

A mission file (file type extension .mis) usually has another image file (file type extensions .jpg or .png) that has the same name as the mission file. For example: MyLevel.mis and MyLevel.jpg . These two files go to your custom directory or a subdirectory within custom.

As most custom levels were created in QuArK or Constructor, they have their own custom interiors and sometimes textures. Any interior (file type extension .dif) and associated textures (.jpg and .png) go to platinum/data/interiors *. For convenience, MBP comes pre-installed with over 500 textures, which are all located in a .zip file so you don't have to copy textures again.

Some custom levels also come with shape files (file type extension .dts) and associated textures (.jpg and .png). These are often put in platinum/data/shapes **.
Likewise, skyboxes are directories with 6-8 images and a .dml file extension file, which lists how to organize the skybox in-game. Every mission (.mis), when opened, has a Sky object that contains a materialList line. This line tells the game which skybox to use, and the path to it. For example:
materialList = ""~/data/skies/Beginner/Beginner_Sky.dml"";
This tells us the directory is called Beginner, and in it we will have a number of image files and a .dml file called Beginner_Sky.dml .
Custom skyboxes such as Atoll will be listed as:
materialList = ""~/data/skies/Atoll/Atoll.dml"";
That means you need to place the Atoll directory inside the skies directory. Atoll.dml is inside the Atoll directory.

* - Some custom missions will put their interiors and textures in a subdirectory, e.g. platinum/data/interiors/RandomName . Custom Multiplayer levels normally use platinum/data/multiplayer/interiors/custom where interiors are located.

** - Some shapes such as marbles, PowerUps or hazards go to specific subdirectories, such as /balls, /items and so on. You can check with the level designer's level info on where these are put.

Also note that you can open the mission file (.mis) using any text editing program, such as Notepad, Wordpad and TextEdit. You can then scroll through the mission and check where interiors, shapes, and skybox are all located.",
        @"The level editor included in Marble Blast Gold is the same as Platinum, but Platinum does change a few things. You can see a good guide about using the Level Editor in MBG here: <a:www.youtube.com/watch?v=YPmR7spHITI>https://www.youtube.com/watch?v=YPmR7spHITI</a>
Please note that the guide is old and some information in there is now known to be incorrect, but it does cover more than enough about the basics.
In addition to that guide, also use this: <a:marbleblast.com/index.php/forum/mb-level-building>http://marbleblast.com/index.php/forum/mb-level-building</a>
It has a lot of useful topics lying about, including a sticked topic about using the level editor, QuArK and Constructor. It is extremely recommended to browse through this board.

This section of the help page only looks at the new additions and changes to the editor in Platinum and explains them in detail:

Create tab: Allows you to add instantly any number of items that are used in Multiplayer. Spawn Triggers, Mega Marbles and Ultra Blasts have no effect in Singleplayer. You can also add an autosized Bounds Trigger where, if the marble leaves it, it goes Out of Bounds.
You can select multiple gems at once and place them in a gem group, so that if the group is selected, all gems (or number of) in the group are spawned. Multiplayer only.

Special tab:
Make Gem Group: Like the Gem Group open in Create tab, this creates a gem group. Select gems first before choosing this option.
Destroy GemGroups: Remove all Gem Groups. Does not remove gems.
Generate Bounds: Same as the Bounds Trigger in the Create tab, this created an In-Bounds Trigger.
Drop at Ground: Drops any item to the ground (interior) and aligns it perfectly.
Round Coordinates: rounds any item's coordinates to the closest number based on your world editor movement scale. Example: 0.1813 becomes 0.2.
Magic Button: Adds all instances as listed in the interior data (items, gems, pads, moving platforms) to the level editor, making life so much easier.
Anti-OCD Button: Select one or more items, triggers, interiors etc. and press this button. Everything selected will become misaligned. Rated A++: would recommend.

Window tab: Allows you to switch on the go between the World Editor, Inspector and Creator.

In the World Editor Inspector, pressing on the MissionInfo – ScriptObject at the top will open a window that lists all the Mission Info fields that currently exist in the mission. You can either modify them or press Add Field and add new values. Press close to save the MissionInfo.

To make things easier, pressing on any Marker object (of Moving Platforms) will draw a line from one marker to the next, effectively showing you the entire path the Moving Platform takes. When several markers are used in a path, every line will be colored based on its relative duration.",
        @"Demo recording in Marble Blast Platinum is very interesting. It normally works fine, but there are a few things to watch out for.
Note: the term Desync is often used. It means the marble goes off-route and Out of Bounds, thus the recording never finishes properly.

Source for the following was taken from: <a:marbleblast.com/index.php/forum/general-mb-discussion/240-recording-demos-replays-using-marble-blast>http://marbleblast.com/index.php/forum/general-mb-discussion/240-recording-demos-replays-using-marble-blast</a>
The topic above is a must read for Marble Blast Gold demo recording, as it's different than that in Platinum. Please also note the topic refers to recording in Platinum in ways that no longer exist in 1.50. Don't worry about them.

Due to 1.50+ changes, some of the problems listed in the topic are no longer an issue. These are noted below.

Marble Blast Ultra Levels
Marble Blast does not know how to handle the addition of the Blast, as it is was never included in the original game. Recordings using the Blast will not work. To make things easier, demo recording in Marble Blast Ultra has been disabled.

Cross Platform
A Windows user can only see recordings from a Windows user. If he's given Mac/Linux user recordings, there will likely be a desync at some point.
Likewise, Mac users can only watch other Mac users' recordings, and Linux to Linux. In some cases the recording will simply not start.
Note - There may be rare occurrences where recordings will actually function correctly. It could be due to a very short replay file.

Version Number
A recording created in different versions of Marble Blast, even if the same level is recorded, will fail when attempted to replay. For example, a recording created in Marble Blast Platinum 1.14 will not work in 1.50.
One of the main reasons is that even a single script change seems to modify some values within the recording system, making it desync when replaying in different versions or the game.

Out of Bounds & Restarting
This is no longer an issue in Marble Blast Platinum 1.50 and above.

Replay and Continue Options at the End Level Screen
This is no longer an issue in Marble Blast Platinum 1.50 and above.

Desyncing
Although you may be doing a successful run of a certain level, the game sometimes messes up the movement at a certain point or not jumping or activating a PowerUp. This often causes the marble to go out of bounds.

Known causes of such desyncs are:
Fans, Gravity Modifiers, Tornados, and long levels (usually several minutes length). It can also be caused by lag during the recording of the demo.

Even if a replay fails, you should replay it a couple of times, maybe even restart Marble Blast and try again. There have been known cases where a replay fails, but several attempts later the playback fully works again. Sometimes it works only once.

Usage of screen recorders causes lag, which when combined with the aforementioned factors, may stop a playback from working properly.

Custom levels do not work properly
During playback it is recommended to have all interiors required by the mission file for successful playback and hope that the recorder himself was not missing any interiors.

Path of mission file & mission name
This is no longer an issue in any version of Marble Blast Platinum.

A certain recording ALWAYS starts first
This problem is exclusive to Marble Blast Gold and is here for documentation purposes.
Sometimes you may have (by accident) put a .rec file in the client folder (marble -> client). The game usually reads the client folder before the demos folder in search of .rec files.
Alternatively, if several demo files are listed as say, Demo1, Demo2, Demo 3, Demo15, Demo21, then Marble Blast will launch them in this order: Demo3, Demo21, Demo2, Demo15, Demo1",
        @"If you would like to further customize MBP, here are some options which aren't shown in-game. Modify them in either MBPPrefs.cs in your platinum/client directory or prefs.cs in platinum/leaderboards/multiplayer directory.
Multiplayer related preferences usually begin with MPPref.


$MPPref::AllowServerChat - Defaulted to true. Change to false to get some peace and quiet.

$MPPref::AllowQuickRespawn - Defaulted to true. Allow players to use quick respawn, change to false if you don't want them or yourself to be able to hit the respawn button in a desperate time of need. Multiplayer only.

$MPPref::OverviewFinishSpeed - Defaulted to 0. A zoom-in animation of the camera towards the marble when starting a Multiplayer game. Value is in milliseconds of animation or 0 to disable.

$MPPref::BackupClients - Defaulted to true. Save peoples' score progress when they leave and rejoin. Value is true/false. Multiplayer only.

$MPPref::DisplayWinners - Defaulted to disable. Show winners' names in server chat when they win. Value is true/false. Multiplayer only.

$pref::OpenGL::gammaCorrection - Defaulted to 0.5. We put this here as an option you can change even though you absolutely mustn't. If you want to play a prank on somebody, put it as 0 and launch Marble Blast. Values are 0.0 - 1.0.

$pref::Music::Songs[""LB""], $pref::Music::Songs[""Menu""], $pref::Music::Songs[""Game""] - Defaulted to ""LB"" => ""Comforting Mystery.ogg"", ""Menu"" => ""Pianoforte.ogg"", ""Game"" => ""*"" . Change which songs are played in which places, use ""*"" for wildcard (any song), value is a list of song file names with names separated by \t .

$pref::UseLowResGlass - Defaulted to disable. Use the same looking glass shape in MBU/Multiplayer levels without collision on the borders. value is true/false.",
        @"If you require further help or assistance, you are welcome to post on the Marble Blast Forums under the Support Board. You should read the board rules so that when you post an inquiry we may assist you better. You can also e-mail us at marbleblastforums@gmail.com

We also welcome bug reports. Some bugs that you may encounter are known and documented, and may be fixed in the next patch or version. Some bugs will not be fixed as they are related to the Marble Blast engine. While we do our best to modify the engine to fix bugs, some would take too long and would have little compensation. Modifications of the engine are extremely hard and require good knowledge of how it's built; it is easy enough to miss a byte and have the game not even launch.

The console has been equipped with several buttons that can help both you and us. From left to right order the buttons are:
Trace: A very useful button, it spams out all functions and calls in the console. It is recommended to use it before demonstrating a bug, as that it will help us identify how the bug has happened. Not that in some cases it doesn't tell us anything, but it is quite reliable. Note that it spams a lot very quickly and will cause Marble Blast to crash if left enabled for too long. Filesize on crash may very, but is usually 10 Megabytes or more.
Fast: Enables 'Fast' mode. Disables features like loading bars, marble smoothing and rotation among others. Not recommended unless you lag terribly during Multiplayer matches.
ModPaths: A quick 'relaunch' button that reloads the game's filesystem, so it can detect whether you changed files, added or removed. Great if you want to change custom levels without having to restart the game. Don't press too many times as it will crash Marble Blast.
Debug: Enabled 'Debug' mode. The console will output additional DEBUG information that can help in the identification of bugs. Useful when sending a crash report.


The following is a list of bugs and errors that are known at this point in time:

1) Team score gem count ingame show 0/0/0/etc

2) Team scores don't have platinum gems count (for PQ levels) in the end game screen

3) Dedicated servers still have some odd bugs in them that have yet to be ironed out, such as the search showing levels incorrectly


For a further FAQ if you require, <a:marbleblast.com/index.php/forum/general-mb-discussion/7568-mbp-1-50-rc1-faq-thread>please check out this topic.</a>",
        @"<spush><font:Marker Felt:24>MBExtender<spop>
Copyright (c) 2014 Aaron Dierking

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

<spush><font:Marker Felt:24>Theora<spop>
Copyright (C) 2002-2009 Xiph.org Foundation

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

- Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

- Neither the name of the Xiph.org Foundation nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE FOUNDATION OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

<spush><font:Marker Felt:24>Vorbis<spop>
Copyright (c) 2002-2008 Xiph.org Foundation

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

- Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

- Neither the name of the Xiph.org Foundation nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE FOUNDATION OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

<spush><font:Marker Felt:24>Ogg<spop>
Copyright (c) 2002, Xiph.org Foundation

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

- Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

- Neither the name of the Xiph.org Foundation nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE FOUNDATION OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

<spush><font:Marker Felt:24>Crypto++<spop>
The Crypto++ Library (as a compilation) is currently licensed under the Boost Software License 1.0 (http://www.boost.org/users/license.html).

Boost Software License - Version 1.0 - August 17th, 2003

Permission is hereby granted, free of charge, to any person or organization obtaining a copy of the software and accompanying documentation covered by this license (the ""Software"") to use, reproduce, display, distribute, execute, and transmit the Software, and to prepare derivative works of the Software, and to permit third-parties to whom the Software is furnished to do so, all subject to the following:

The copyright notices in the Software and this entire statement, including the above license grant, this restriction and the following disclaimer, must be included in all copies of the Software, in whole or in part, and all derivative works of the Software, unless such copies or derivative works are solely in the form of machine-executable object code generated by a source language processor.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, TITLE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE COPYRIGHT HOLDERS OR ANYONE DISTRIBUTING THE SOFTWARE BE LIABLE FOR ANY DAMAGES OR OTHER LIABILITY, WHETHER IN CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

<spush><font:Marker Felt:24>The OpenGL Extension Wrangler Library<spop>
Copyright (C) 2008-2014, Nigel Stewart <nigels[]users sourceforge net>
Copyright (C) 2002-2008, Milan Ikits <milan ikits[]ieee org>
Copyright (C) 2002-2008, Marcelo E. Magallon <mmagallo[]debian org>
Copyright (C) 2002, Lev Povalahev
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation  and/or other materials provided with the distribution.
    * The name of the author may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS ""AS IS"" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

<spush><font:Marker Felt:24>MiniUPnPc<spop>
Copyright (c) 2005-2015, Thomas BERNARD
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
    * The name of the author may not be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS ""AS IS"" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

<spush><font:Marker Felt:24>LibNAT-PMP<spop>
Copyright (c) 2007-2008, Thomas BERNARD 
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
    * The name of the author may not be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS ""AS IS"" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
",
    };
}
