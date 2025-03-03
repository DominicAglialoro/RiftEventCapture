# Rift Event Capture

This is a mod that captures detailed information about every enemy you hit while playing a chart

## To Use

1. Download the lastest version of BepInEx at <https://github.com/BepInEx/BepInEx/releases/latest>
2. Extract the contents of the BepInEx zip archive to your Rift of the Necrodancer game directory (C:\Program Files (x86)\Steam\steamapps\common\RiftOfTheNecroDancerOSTVolume1)
3. Open Rift of the Necrodancer to generate BepInEx config files
4. Download the latest version of RiftEventCapture at <https://github.com/DominicAglialoro/RiftEventCapture/releases/latest>
5. Extract the contents of the RiftEventCapture zip archive to your BepInEx plugins folder (C:\Program Files (x86)\Steam\steamapps\common\RiftOfTheNecroDancerOSTVolume1\BepInEx\plugins)
6. Open Rift of the Necrodancer and play through a chart
7. Upon reaching the results screen, a binary file (*ChartName*\_*ChartDifficulty*\_#.txt) containing the captured events will be written to the RiftEventCapture folder in your Documents folder

The Config file in BepInEx/config lets you set whether events should be captured when playing with or without the Golden Lute modifier enabled

You may include RiftEventCapture.Common in your own C# projects to load captured data

## Binary File Format

The generated binary file consists of the following values, with the specified types:

* The string "RIFT_EVENT_CAPTURE" (string)
* The version number of the file format (int)
* The name of the chart (string)
* The level ID of the chart (string)
* The difficulty level of the chart (int), with: \
  0 = Easy, \
  1 = Medium, \
  2 = Hard, \
  3 = Impossible
* The number of pins (modifiers) enabled (int)
* For each enabled pin, the name of the pin (string)
* The BPM of the chart (int)
* The number of divisions per beat (int)
* The number of beat timings stored in the chart (int)
* For each beat timing, the value of that timing (double)
* The number of captured events (int)
* For each captured event:
  * The type of the event (int), with: \
    0 = Enemy Hit, \
    1 = Enemy Missed (not implemented), \
    2 = Overpress (not implemented), \
    3 = Hold Segment Scored, \
    4 = Hold Completed, \
    5 = Vibe Gained, \
    6 = Vibe Activated (not implemented), \
    7 = Vibe Ended (not implemented)
  * The time of the event (double)
  * The beat number of the event (double)
  * The target time of the enemy hit (double)
  * The target beat of the enemy hit (double)
  * The type of the enemy hit (int), with: \
    0 = None, \
    1 = Green Slime, \
    2 = Blue Slime, \
    3 = Yellow Slime, \
    4 = Blue Bat, \
    5 = Yellow Bat, \
    6 = Red Bat, \
    7 = Green Zombie, \
    8 = Blue Zombie, \
    9 = Red Zombie, \
    10 = White Skeleton, \
    11 = White Shield Skeleton, \
    12 = White Double Shield Skeleton, \
    13 = Yellow Skeleton, \
    14 = Yellow Shield Skeleton, \
    15 = Black Skeleton, \
    16 = Black Shield Skeleton, \
    17 = Blue Armadillo, \
    18 = Red Armadillo, \
    19 = Yellow Armadillo, \
    20 = Wyrm, \
    21 = Green Harpy, \
    22 = Blue Harpy, \
    23 = Red Harpy, \
    24 = Blademaster, \
    25 = Blue Blademaster, \
    26 = Yellow Blademaster, \
    27 = White Skull, \
    28 = Blue Skull, \
    29 = Red Skull, \
    30 = Apple, \
    31 = Cheese, \
    32 = Drumstick, \
    33 = Ham
  * The column the enemy is in (int)
  * The total score gained from the event (int)
  * The base score (before multipliers and Perfect bonus) gained from the event (int)
  * The base score multiplier (before Vibe) applied to the base score (int)
  * The Vibe score multiplier (2 if Vibe is active, 1 otherwise) applied to the base score (int)
  * The Perfect bonus added to the base score (int)
  * Whether or not the enemy hit is part of a Vibe chain (bool)
