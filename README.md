# HanabiGameSimplified
This single cs file implements a [Hanabi](https://en.wikipedia.org/wiki/Hanabi_(card_game)) 
card game with simplified rules.

There are only 2 players in the game, each turn they swap.

Game starts with specific start game command

"Start new game with deck(\s[RGBYW][1-5]){11,50}" - for example:
"Start new game with deck R1 R2 R3 R4 R5 R1 R2 R3 R4 Y1 Y2 Y3".
Remember that according to the rules there is a specific amount of cards of the same rank per color:
(111 22 33 44 5), so you can't have four R1's or two Y5's. And amount of cards in the deck must be in range of 11 to 50.

Then player can start entering commands.

Player can enter 1 of 4 commands:

1."Play card [0-4]{1,5}" - 
attempts to add card to the table, if attempt fails - game restarts.

2."Drop card [0-4]" - 
removes card from players hand, and adds it to discard.

3."Tell color (Red|Blue|Yellow|Green|White) for cards [0-4]{1,5}" - 
player gives next player information about all cards of the specified color in his hand.

4."Tell rank [0-4]{1,5}" - 
same as with tell color, but with rank.

Game continues until one of players makes incorrect turn, gives incorrect information, in this case game restarts and waits for the new "Start new game command". In case players enters incorrect input - game immediately terminates.
