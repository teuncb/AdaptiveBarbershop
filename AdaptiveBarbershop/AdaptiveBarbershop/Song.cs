using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;

namespace AdaptiveBarbershop
{
    class Song
    {
        public Chord[] chords;

        public Song(string path)
            /// Initialises a Song from a path in the described song language
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

            for (int i = 0; i < chords[chords.Length - 1].notes.Length; i++)
            {
                if (chords[chords.Length - 1].notes[i].tied)
                    throw new FormatException(string.Format("Last chord cannot have tied notes"));
            }
        }
        public void WriteMidiFile(string fileName, bool overwriteFile = true)
            /// Writes this entire song to a new MIDI file
        {
            string path = "../../../../../OutputMIDI/" + fileName + ".mid";
            MIDISong().Write(path, overwriteFile);
            Console.WriteLine("Successfully wrote a MIDI file to SongMidi/" + fileName + ".mid");
        }

        public MidiFile MIDISong(int tempo = 150000, int instrument = 72)
        {
            List<MidiEvent> events = new List<MidiEvent>();

            // Make everyone sound like saxophones
            List<MidiEvent> instrumentEvents = new List<MidiEvent>(){
                new ProgramChangeEvent((SevenBitNumber)instrument) { Channel = (FourBitNumber)0 },
                new ProgramChangeEvent((SevenBitNumber)instrument) { Channel = (FourBitNumber)1 },
                new ProgramChangeEvent((SevenBitNumber)instrument) { Channel = (FourBitNumber)2 },
                new ProgramChangeEvent((SevenBitNumber)instrument) { Channel = (FourBitNumber)3 }
            };
            events.AddRange(instrumentEvents);

            // Initial call to MidiEvents: no notes are playing yet
            (List<ChannelEvent> newEvents, int dt) = chords[0].MidiEvents(new bool[4] { false, false, false, false });
            events.AddRange(newEvents);

            // Add all pitch bend, note-on and note-off events from each chord to a big List
            for(int i = 1; i < chords.Length; i++)
            {
                // The current chord needs to know which notes were tied in the previous chord
                // The notes in previousTies don't get new note-on messages
                bool[] previousTies = new bool[4];
                for(int j = 0; j < previousTies.Length; j++)
                {
                    previousTies[j] = chords[i - 1].notes[j].tied;
                }

                (newEvents, dt) = chords[i].MidiEvents(previousTies, dt);
                events.AddRange(newEvents);
            }

            MidiFile songFile = new MidiFile(
                new TrackChunk(
                    new SetTempoEvent(tempo)
                    ),
                new TrackChunk(
                    events
                    )
                );

            return songFile;
        }
    }
}
