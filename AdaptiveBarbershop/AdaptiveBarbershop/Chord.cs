using System;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;

namespace AdaptiveBarbershop
{
    class Chord
    {
        public Note[] notes;
        public double masterBend;
        public int startTime;
        public int duration;
        public int root;
        public char chordType; // TODO meer commentaar
        public string fullName;

        private static HashSet<char> chordTypes = new HashSet<char>() { 'M', 'm', '7', 'o', '0' };
        private static char[] delimiters = { '(', ')', ',' };

        public Chord(string input, int start)
        {
            // Comments can be added after a percent sign
            string noComments = input.Split('%')[0];

            // Split the input string into chord name, 4 notes, and duration
            string[] portions = noComments.Split(delimiters);

            // Declare the root as a Note based on the string, in lowercase
            try
            {
                root = Note.noteNames[portions[0].Substring(0, 2).ToLower()];
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new ArgumentOutOfRangeException(
                    string.Format("Chord root {0} not recognised in {1}", portions[0].Substring(0, 2), input));
            }

            // The chord type is stored in the same way as the input language: M, m, 7, o or 0
            fullName = portions[0];
            chordType = portions[0][2];
            if (!chordTypes.Contains(chordType))
                throw new ArgumentException(
                    string.Format("Chord type {0} in not recognised in {1}", chordType.ToString(), input));

            // Make a Note object for each note input string
            notes = new Note[4];
            for (int i = 0; i < notes.Length; i++)
            {
                notes[i] = new Note(portions[i + 1]);
            }

            startTime = start;
            duration = int.Parse(portions[5]);
            masterBend = 0;
        }

        // TODO handling of DeltaTime for completely silent and completely tied chords
        public (List<ChannelEvent>, int) MidiEvents(bool[] previousTies, int firstDeltaTime = 0)
        {
            if (previousTies.Length != 4)
                throw new ArgumentException("previousTies didn't have length 4 when getting midi events, which is required");

            List<ChannelEvent> events = new List<ChannelEvent>();

            // Add pitch bends
            events.Add(new PitchBendEvent(BSTuner.MIDIBend(masterBend, notes[0]))
            { Channel = (FourBitNumber)0, DeltaTime = firstDeltaTime });
            for (int i = 1; i < notes.Length; i++)
                events.Add(new PitchBendEvent(BSTuner.MIDIBend(masterBend, notes[i])) 
                { Channel = (FourBitNumber)i });

            // Add note-on for new notes that aren't silent in this chord and aren't already playing
            for (int i = 0; i < notes.Length; i++)
                if (notes[i].playing && !previousTies[i])
                    events.Add(new NoteOnEvent((SevenBitNumber)notes[i].midiNoteID, (SevenBitNumber)100) 
                    { Channel = (FourBitNumber)i });

            // Add note-off for non-tied notes that aren't silent in this chord
            bool addedDeltaTime = false;
            for (int i = 0; i < notes.Length; i++)
                if (notes[i].playing && !notes[i].tied)
                {
                    if (addedDeltaTime)
                    {
                        events.Add(new NoteOffEvent((SevenBitNumber)notes[i].midiNoteID, (SevenBitNumber)80)
                        { Channel = (FourBitNumber)i });
                    }
                    else
                    {
                        events.Add(new NoteOffEvent((SevenBitNumber)notes[i].midiNoteID, (SevenBitNumber)80)
                        { Channel = (FourBitNumber)i, DeltaTime = duration });
                        addedDeltaTime = true;
                    }
                }

            // If no delta time could be added yet, pass it to the next Chord
            return (events, addedDeltaTime?0:duration);
        }

        public override string ToString()
        {
            return fullName;
        }
        public string PrintChord()
            /// A formatted string with bend values for each note
        {
            string result = string.Format("{0} - MB:{1:+0.0000;-0.0000;=0.0000}. Post bends:", fullName, masterBend);
            for(int i = 0; i < notes.Length; i++)
            {
                double posteriorBend = masterBend + notes[i].indivBend;
                result += string.Format(" {0}: {1:+0.0000;-0.0000;=0.0000};",notes[i].noteName,posteriorBend);
            }
            return result;
        }
    }
}
