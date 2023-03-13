using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace AdaptiveBarbershop
{
    class Song
    {
        public Chord[] chords;

        public Song(string path)
        {
            string[] lines = File.ReadAllLines(path);
            chords = new Chord[lines.Length];

            // Build the Song from one Chord per line, keeping track of how much time has passed
            // using the Chord durations. Those times are then used as the start time for new Chords.
            int time = 0;
            for(int i = 0; i < lines.Length; i++)
            {
                chords[i] = new Chord(lines[i], time);
                time += chords[i].duration;

                // Error message for if tied notes don't stay the same in the next chord
                if(i > 0)
                {
                    // For each note in the last chord...
                    for(int n = 0; n < 4; n++)
                    {
                        // ...if the note is tied and the current chord doesn't have the same note in that voice...
                        if (chords[i - 1].notes[n].tied &&
                            (chords[i - 1].notes[n].noteNum != chords[i].notes[n].noteNum ||
                            chords[i - 1].notes[n].octave != chords[i].notes[n].octave))
                        {
                            // ...throw an exception
                            throw new FormatException(string.Format(
                                "The chords on lines {0} and {1} contain conflicting tie/pitch information in {2}",
                                i - 1, i, path));
                        }
                    }
                }
            }
        }
    }
}
