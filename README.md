# AdaptiveBarbershop
An adaptive just intonation system, that specifically follows the conventions of the barbershop genre. This project was Teun Buwalda's Bachelor's thesis for Kunstmatige Intelligentie at Utrecht University. The full explanation of what it does can be found in the [paper](2023-06-01_BachelorThesis_TeunBuwalda_FixedBibNDate.pdf).

# Basic usage
Change the code in `Program.cs`  to access the tuning system's API:
```csharp
// Initialise the tuner type with parameters
BSTuner tuner = new BSTuner(tieRadius: 3, leadRadius: 10, prio: 'l');

// Create the Song object using a .txt file in /Songs
Song song = new Song("out_there");

// Optionally, export the equal temperament MIDI file for comparison
song.WriteMidiFile("out_there_untuned");

// Tune the song
tuner.TuneSong(song, analyze: true);

// Export the MIDI file again now that the song is tuned
song.WriteMidiFile("out_there_tuned");