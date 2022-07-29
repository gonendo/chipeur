![chipeur](https://github.com/gonendo/chipeur/blob/main/assets/chipeur.png?raw=true)

# chipeur
CHIP8/SuperChip Emulator written in C# using Veldrid, ImageSharp and ImgUI.

### Requirements:
You need a .NET runtime (>= 6.0) to run this program. You can download one [here](https://aka.ms/dotnet/download).

### Usage:
- Launch the Emulator (chipeur executable)
- File > Load a rom

You can also automatically launch a rom from the command line with this syntax:

<code>./chipeur ROM</code>

### Recommended settings
For original CHIP 8, 500hz speed is good. As for SuperChip 1000hz is recommended for smoother gameplay.
If you are not sure of what system your game is for, just pick the SuperChip (it's backward compatible).

### Controls:
Chip8
| 1 | 2 | 3 | C |
|---|---|---|---|
| 4 | 5 | 6 | D |
| 7 | 8 | 9 | E |
| A | 0 | B | F |

*Azerty keyboard*:
| 1 | 2 | 3 | 4 |
|---|---|---|---|
| A | Z | E | R |
| Q | S | D | F |
| W | X | C | V |

*Qwerty keyboard*:
| 1 | 2 | 3 | 4 |
|---|---|---|---|
| Q | W | E | R |
| A | S | D | F |
| Z | X | C | V |

N.b: You can change the keyboard layout in the options.


## If a game doesn't seem to work, try to change the compatibility. Adjust the speed if need be.
