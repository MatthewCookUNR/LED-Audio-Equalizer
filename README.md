# LED-Audio-Equalizer
Application for Arduino UNO that will use audio input to output a colorful display on a LED light panel. The lights will be based on audio frequencies.

### Technologies Used
- .NET Core 3.0 Console Applciation
- PlatformIO, a VS Code plugin for an Arduino IDE
- Arduino Uno
- Adafruit's 32x16 RGB LED Matrix

### Project Status
Intial development of project is finished! Wooohooo!

### Enhancements
- Allow the LED display to be configurable. Ex) Users can change color scheme of the grid.
  This would open up a lot of possibilities if implemented.

### Known Issues
- Sometimes frequencies that can be audibly heard are not displayed. Most noticable with snare drums and other high frequency rythm instruments. 
- During Initialization of the application in Program.cs, creation of the AudioService or SerialService will cause an exception to be thrown.
