# WindowWorld
<img width="640" alt="windowWorld1" src="https://github.com/user-attachments/assets/79f7e578-fb00-40a7-a48d-4add0104ea8a" />
Project includes all assets to build the unity project. The adurino code, the wireing and the right Port are not essential for the project to run. An UI-Button also allows players to join.

# Description of the project
Players can scan the QR-code on screen to get redirected to a website containing instructions on how to play. There is a photoresistor module that is ment to register bright lights (taking a photo with flash of the setup) and will trigger the join-phase by turning on the lcd lights connected to the adurino. During this phase the windows will open and the people can see themself on screen inside the building. A crosshair in the center will read the rgb-value of that pixel and scan the window area of that color (within a range). If enough pixels are found and are not directly on the edge, the window boarder indicates the callibration status. If it is full, the lcd light of that corresponding player will turn green, showing a successfull registration and the width and height of their device is calulcated. The tracking of the phonescreen will continuous use the pixel value of the center and updates each of the 4 corner points individual. By comparing the side-length vertical and horizontal, different tilting and rotating movements can be registered.
Upon successfull registration/callibration, a snake with a flower in that players color is spawned inside the players window and is yeetedin onto grass. The player then can steer the snakes head using the phone screen in their hand and try to catch the beatles. If the player successfully catches a beatle, the flower on the snakes head will grow in size.
If the player runs into a wall, they will be respawned at the window.

 
# Sensors/LEDs used
 - Ardurino UNO R4 WIFI
 - Breadboard
 - 6 two-color LED module (Ky-029)
 -1 Photoresistor Module ( Ky-018)

