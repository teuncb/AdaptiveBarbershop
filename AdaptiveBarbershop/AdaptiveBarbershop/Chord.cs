﻿using System;
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

        private static HashSet<char> chordTypes = new HashSet<char>() { 'M', 'm', '7', 'o', '0' };

        public Chord(string input, int start)
        {
            // Split the input string into chord name, 4 notes, and duration
            char[] delimiters = { '(', ')', ',' };
            string[] portions = input.Split(delimiters);

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
        public List<MidiEvent> MidiMessages()
            /// Not finished yet!
        {
            List<MidiEvent> events = new List<MidiEvent>();

            // TODO add to different channels, not just 0
            // TODO add pitch wheel message based indivBend and masterBend
            for(int i = 0; i < notes.Length; i++)
            {
                if (notes[i].playing)
                {
                    // TODO don't add this if note is already playing (somehow, maybe outside this method)
                    events.Add(new NoteOnEvent((SevenBitNumber)notes[i].midiKey, (SevenBitNumber)100));
                    if (!notes[i].tied)
                    {
                        events.Add(new NoteOffEvent((SevenBitNumber)notes[i].midiKey, (SevenBitNumber)80));
                    }
                }
            }

            return events;
        }
    }
}
