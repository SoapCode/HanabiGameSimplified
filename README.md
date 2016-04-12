# HanabiGameSimplified
This single cs file implements a [Hanabi](https://en.wikipedia.org/wiki/Hanabi_(card_game)) card game with simplified rules.

There are only 2 players playing, each turn they swap.

Game starts with specific start game command

"Start game with deck(\s[RGBYW][1-5]){11,50}"

Than player can start entering commands.

Player can enter 1 of 4 commands:

1."Play card [0-4]{1,5}"
Attempts to add card to the table, if attempt fails - game restarts.

2."Drop card [0-4]"
Removes card from players hand, and adds it to discard.

3."Tell color (Red|Blue|Yellow|Green|White) for cards [0-4]{1,5}"
Player gives next player information about all cards of the specified color in his hand.

4."Tell rank [0-4]{1,5}"
Same as with tell color, but with rank.

Game continues until one of the players makes incorrect turn, or enters incorrect input.
In this case game immediately terminates.
