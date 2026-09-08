# PlatinumQuest Unity Port

The Unity Port of PlatinumQuest, based on [Marble Blast Gold Unity remake](https://github.com/NaCl586/marble-blast-gold-unity/) that I did. This remake is based on older versions of PlatinumQuest (researched through Matan's videos) and Marble Blast Platinum 1.50 UI made by A-Game. Codes are based on [RandomityGuy's PQ Haxe Port](https://github.com/RandomityGuy/MBHaxe/tree/pq) and the [Original PQ Repo](https://github.com/The-New-Platinum-Team/PlatinumQuest-Dev/). Some levels and interiors are modified to ensure the game works, and I also added some other Quality of Life features that is not present in Original PQ and/or Haxe Port.

Future versions are to implement Particle Systems, Replay Center, and Leaderboards (same server as with my [Marble Blast Platinum Unity Port](https://github.com/NaCl586/marble-blast-platinum-unity}))

<img src="https://i.imgur.com/SZaGCwN.png" width="640">
<img src="https://i.imgur.com/Q9dihpE.png" width="640">
<img src="https://i.imgur.com/4I1Ea6b.png" width="640">
<img src="https://i.imgur.com/bhWNua4.png" width="640">
<img src="https://i.imgur.com/vA50eao.png" width="640">
<img src="https://i/imgur.com/5d8rShA.png" width="640">
<img src="https://i.imgur.com/aVixidr.png" width="640">
<img src="https://i.imgur.com/32rYXEM.png" width="640">
<img src="https://i.imgur.com/32rYXEM.png" width="640">
<img src="https://i.imgur.com/ZMucrey.png" width="640">

## Download the Windows build [here](https://github.com/NaCl586/platinumquest-unity/releases/)

If you find bugs or things that are not faithful with the original Marble Blast Gold, feel free to message me on discord NaCl586#8479.

Special thanks to RandomityGuy for helping me whenever I have problems when making this project.

## Save Data

<img src="https://i.imgur.com/u2wAziG.png" width="640">

Save data uses [PlayerPrefs](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PlayerPrefs.html), which can be accessed via Registry Editor (see picture). If you wanna unlock the levels, you can create a key or edit existing key called "QualifiedLevel[Difficulty]" to a large integer like 9999. The PlayerPrefs essentially is equivalent to prefs.cs in vanilla Marble Blast.

## Other Notes

This game does have custom music support and jukebox the same as with Marble Blast Platinum 1.14 Unity Port, because this game was made from that. However, despite working, custom levels are not guaranteed to be loaded correctly in the game, and custom levels with custom codes is a big no.
