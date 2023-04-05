﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;

namespace AdaptiveBarbershop
{
    class BSTuner
    {
        // Scale for the double values used in the algorithm, usually just 1.0 (so a cent is 0.01)
        private double halfStepSize;
        // The 4 voices in order of importance. When optimising tied notes, the lead should be
        // handled first, then the bass, then the tenor and lastly the baritone.
        private static int[] voicesOrder = new int[4] { 2, 0, 3, 1 };
        private static string[] voices = new string[4] { "bass", "baritone", "lead", "tenor" };
        // Either l for lead or t for tied notes: which one should be optimised first?
        private char priority;
        private static HashSet<char> prioTypes = new HashSet<char> { 'l', 't' };

        private Range tieRange; // How much tied notes are allowed to retune
        private Range leadRange; // How much the lead is allowed to differentiate from 12TET

        // Maps a chord type to its array of interval ratios in just intonation
        private Dictionary<char, Fraction[]> tuningTables;

        // Initialise the tuner type, setting the global parameters
        public BSTuner(double stepSize = 1, double tieRadius = 0.03, double leadRadius = 0.20, char prio = 't',
            string pathMaj     = "../../../../../TuningTables/maj_lim17.txt",
            string pathMin     = "../../../../../TuningTables/min_lim7.txt",
            string pathDom     = "../../../../../TuningTables/maj_lim17.txt",
            string pathDim7    = "../../../../../TuningTables/dim7_lim17.txt",
            string pathHalfDim = "../../../../../TuningTables/min_lim7.txt")
        {
            if (!prioTypes.Contains(prio))
                throw new ArgumentException(prio.ToString() + " is not a valid priority type, should be either l or t");

            halfStepSize = stepSize;
            priority = prio;

            tieRange = new Range(-tieRadius, tieRadius);
            leadRange = new Range(-leadRadius, leadRadius);

            tuningTables = new Dictionary<char, Fraction[]>(5);
            tuningTables.Add('M', TuningTable(pathMaj));
            tuningTables.Add('m', TuningTable(pathMin));
            tuningTables.Add('7', TuningTable(pathDom));
            tuningTables.Add('o', TuningTable(pathDim7));
            tuningTables.Add('0', TuningTable(pathHalfDim));
        }

        // Master method for the tuning algorithm: tunes an entire song
        public string TuneSong(Song song, bool analyze = true, bool print = false)
        {
            string analysis = "";

            if (analyze)
            {
                song.drifts = new double[song.chords.Length];
                song.maxTieDiffs = new (double, int)[song.chords.Length];
                song.leadDevs = new double[song.chords.Length];
            }

            // Carry out the vertical step for each chord
            for (int i = 0; i < song.chords.Length; i++)
            {
                SetIndivBends(song.chords[i], print);
            }

            // Set the lead to equal temperament in the first chord
            InitialMasterBend(song.chords[0]);

            // Carry out the horizontal step for each subsequent chord
            for (int i = 1; i < song.chords.Length; i++)
            {
                double mb = SetMasterBend(song.chords[i - 1], song.chords[i]);
                if (analyze)
                {
                    song.drifts[i] = mb - song.chords[i - 1].masterBend;

                    // Find the biggest tie difference in this chord
                    for (int v = 0; v < 4; v++)
                    {
                        Note oldNote = song.chords[i - 1].notes[v];
                        Note newNote = song.chords[i].notes[v];
                        if (oldNote.playing && newNote.playing)
                        {
                            double postTieDiff = (oldNote.indivBend + song.chords[i - 1].masterBend) - (newNote.indivBend + song.chords[i].masterBend);

                            if (oldNote.tied &&
                                (Math.Abs(postTieDiff) > Math.Abs(song.maxTieDiffs[i].Item1)))
                            {
                                song.maxTieDiffs[i] = (postTieDiff, v);
                            }
                        }
                    }

                    // Find how much the lead deviates from equal temperament
                    Note oldLead = song.chords[i - 1].notes[2];
                    Note newLead = song.chords[i].notes[2];
                    double postLeadDev = (oldLead.indivBend + song.chords[i - 1].masterBend) - (newLead.indivBend + song.chords[i].masterBend);
                    song.leadDevs[i] = postLeadDev;
                }
                if(print)
                    Console.WriteLine("Set master bend for chord {0} to {1:0.0000}", i, mb);
            }

            if (analyze)
            {
                analysis = AnalyzeTuning(song);
                song.WriteResults(song.songTitle + "_analysis");
            }

            return analysis;
        }

        public string AnalyzeTuning(Song song)
            /// Gives some overall statistics on how tuning this song went.
        {
            string analysis = "";

            Console.WriteLine("--------------------------------");
            Console.WriteLine("The song was successfully tuned. Here are some fun facts:");
            Console.WriteLine("Overall pitch drift: {0}", song.chords[song.chords.Length - 1].masterBend);

            // Find the maximum pitch drift from one chord to the next
            double max = 0;
            int maxIdx = 0;
            for(int i = 0; i < song.drifts.Length; i++)
            {
                if (Math.Abs(song.drifts[i]) > Math.Abs(max))
                {
                    max = song.drifts[i];
                    maxIdx = i;
                }
            }
            if (max == 0)
                Console.WriteLine("There was no pitch drift at all in this song!");
            else
                Console.WriteLine("Most dramatic pitch drift moment: {0:0.0000} when going from chord {1} ({2}) to chord {3} ({4})",
                    max, maxIdx - 1, song.chords[maxIdx - 1], maxIdx, song.chords[maxIdx]);

            analysis += string.Format("{0:0.0000};", max);

            // Find the biggest retuning jump in a tied note
            max = 0;
            maxIdx = -1;
            int maxVoice = -1;
            for(int c = 0; c < song.maxTieDiffs.Length; c++)
            {
                (double diff, int v) = song.maxTieDiffs[c];
                if (Math.Abs(diff) > Math.Abs(max))
                {
                    max = diff;
                    maxIdx = c;
                    maxVoice = v;
                }
            }
            if(maxIdx == -1)
            {
                Console.WriteLine("Not a single tied note had to retune, great!");
            }
            else
                Console.WriteLine("Most dramatic tie change: {0:0.0000} in the {1} from chord {2} ({3}) to chord {4} ({5})",
                max, voices[maxVoice], maxIdx - 1, song.chords[maxIdx - 1], maxIdx, song.chords[maxIdx]);

            analysis += string.Format("{0:0.0000};", max);

            // Find the biggest deviation from equal temperament in the lead voice
            max = 0;
            maxIdx = -1;
            for(int c = 0; c < song.leadDevs.Length; c++)
            {
                if(Math.Abs(song.leadDevs[c]) > Math.Abs(max))
                {
                    max = song.leadDevs[c];
                    maxIdx = c;
                }
            }
            if(maxIdx == -1)
            {
                Console.WriteLine("Every lead interval was exactly like equal temperament, great!");
            }
            else
                Console.WriteLine("Most dramatic ET deviation in the lead: {0:0.0000} from chord {1} ({2}) to chord {3} ({4})",
                max, maxIdx - 1, song.chords[maxIdx - 1], maxIdx, song.chords[maxIdx]);

            analysis += string.Format("{0:0.0000}", max);

            Console.WriteLine("--------------------------------");
            return analysis;
        }

        public Fraction[] TuningTable(string tablePath)
            /// Initialise a table of interval fractions from a file
            /// These are the fractions that will be used for just intonation intervals in the vertical step
        {
            // Require exactly 12 fractions
            string[] lines = File.ReadAllLines(tablePath);
            if(lines.Length != 12)
            {
                throw new FormatException(string.Format("The file {0} doesn't have exactly 12 lines, which is required", tablePath));
            }

            // Build an array of the 12 interval fractions in the file
            Fraction[] tuningTable = new Fraction[12];

            for(int i = 0; i < lines.Length; i++)
            {
                tuningTable[i] = new Fraction(lines[i]);
            }

            return tuningTable;
        }

        // Vertically tunes the notes inside a chord to just intonation relative to the root
        public void SetIndivBends(Chord chord, bool print = false)
        {
            foreach (Note note in chord.notes)
            {
                if (note.playing)
                    note.indivBend = GetIndivBend(note.noteNum, chord.root, tuningTables[chord.chordType], print);
                else
                    note.indivBend = 0;
            }
        }

        public double GetIndivBend(int note, int root, Fraction[] tuningTable, bool print = false)
        {
            // The number of half steps until note is reached, counting upwards from root
            int distInHalfSteps = note - root;
            if (distInHalfSteps < 0)
                distInHalfSteps += 12;
            else if (distInHalfSteps >= 12)
                distInHalfSteps -= 12;

            Fraction interval = tuningTable[distInHalfSteps];
            // The microtonal distance between note and root in just intonation, counting upwards from root
            double fullDist = 12 * halfStepSize * Math.Log2(interval.ToFactor());

            // Return how much note should deviate from equal temperament
            double indivBend = fullDist - distInHalfSteps * halfStepSize;
            if(print)
                Console.WriteLine("Tuning note {0} with root {1} to value {2:0.0000} using fraction {3}", note, root, indivBend, interval);
            return indivBend;
        }

        // Set the masterBend for the very first Chord such that the lead sings an equal temperament note
        public double InitialMasterBend(Chord firstChord)
        {
            firstChord.masterBend = -firstChord.notes[2].indivBend;
            return firstChord.masterBend;
        }

        // TODO this currently prioritises ties over lead, make that a parameter option by changing the order of ranges
        // TODO optionally, add the lead functionality to the bass as well
        public double SetMasterBend(Chord prevChord, Chord currChord, bool print = false)
        {
            // Make a list of note indices that have a tie property, ordered like voicesOrder
            List<int> ties = new List<int>();
            foreach (int voice in voicesOrder)
            {
                if (prevChord.notes[voice].tied)
                {
                    ties.Add(voice);
                }
            }

            // tieDiffs is an array of differences between the tuning of the previous and current notes
            double[] tieDiffs = new double[ties.Count];
            for(int i = 0; i < tieDiffs.Length; i++)
            {
                // For this tied note, get the current difference in tuning between the previous chord and the current chord
                // currChord.masterBend should be 0, but that's been added for completeness.
                tieDiffs[i] = 
                    (prevChord.masterBend + prevChord.notes[ties[i]].indivBend) - 
                    (currChord.masterBend + currChord.notes[ties[i]].indivBend);
            }

            // Same difference for the lead voice
            double leadDiff =
                (prevChord.masterBend + prevChord.notes[2].indivBend) -
                (currChord.masterBend + currChord.notes[2].indivBend);

            // Determine the optimal masterBend for currChord
            // Option 1: all can be optimised by setting the masterBend to 0
            if (tieDiffs.All(diff => tieRange.Contains(diff)) && leadRange.Contains(leadDiff))
            {
                currChord.masterBend = 0;
                return 0;
            }
            // Option 2: pick a masterbend that is within the ranges of all tied notes and the lead interval
            else
            {
                // ranges contains, for each tied note & lead note in these chords,
                // its mvmnt needed to reach bottom bound (range.lower) and mvmnt needed to reach top bound (range.upper)
                // In other words, it's the amount that the masterbend is allowed to move up and down to satisfy this note
                Range[] ranges = new Range[tieDiffs.Length + 1];

                // Ties are more important
                if (priority == 't')
                {
                    for (int i = 0; i < tieDiffs.Length; i++)
                    {
                        ranges[i] = tieRange.MoveBy(tieDiffs[i]);
                    }

                    ranges[ranges.Length - 1] = leadRange.MoveBy(leadDiff);
                }
                // Lead is more important
                else
                {
                    ranges[ranges.Length - 1] = leadRange.MoveBy(leadDiff);

                    for (int i = 0; i < tieDiffs.Length; i++)
                    {
                        ranges[i] = tieRange.MoveBy(tieDiffs[i]);
                    }
                }

                // Set the initial boundaries in which the new masterBend should be chosen
                Range masterBendRange = ranges[0];

                // Collapse the boundaries until you've either reached the last note,
                // or you can choose the optimal masterBend for minimal pitch drift
                for (int i = 1; i < ranges.Length; i++)
                {
                    double overlapResult = Range.Distance(masterBendRange, ranges[i]);
                    // There is overlap; continue collapsing the boundaries of masterBendRange
                    if (overlapResult == 0)
                    {
                        masterBendRange = Range.GetOverlap(masterBendRange, ranges[i]);
                    }
                    // Range for next tied/lead note is completely above the current range
                    // Ignore pitch drift and subsequent notes, compromise with the next note
                    else if (overlapResult > 0)
                    {
                        if(print)
                            Console.WriteLine("Range with index {0} cannot be satisfied, choosing the highest possible masterBend.", i);
                        currChord.masterBend = masterBendRange.upper;
                        return masterBendRange.upper;
                    }
                    // Range for next tied/lead note is completely below the current range
                    // Ignore pitch drift and subsequent notes, compromise with the next note
                    else
                    {
                        if(print)
                            Console.WriteLine("Range with index {0} cannot be satisfied, choosing the lowest possible masterBend.", i);
                        currChord.masterBend = masterBendRange.lower;
                        return masterBendRange.lower;
                    }
                }

                // Return the possible value that is closest to 0
                if (masterBendRange.lower > 0)
                {
                    if(print)
                        Console.WriteLine("All ranges satisfied, choosing lowest possible masterBend.");
                    currChord.masterBend = masterBendRange.lower;
                    return masterBendRange.lower;
                }
                else if (masterBendRange.upper < 0)
                {
                    if(print)
                        Console.WriteLine("All ranges satisfied, choosing highest possible masterBend.");
                    currChord.masterBend = masterBendRange.upper;
                    return masterBendRange.upper;
                }
                else
                {
                    // This should be unreachable code, since we covered this in option 1
                    if(print)
                        Console.WriteLine("Warning: reached code that should be unreachable in getMasterBend()");
                    currChord.masterBend = 0;
                    return 0;
                }
            }
        }
        public static ushort MIDIBend(double masterBend, Note note)
            /// Given a masterBend en indivBend, adds the two and maps it to the range in the actual MIDI format
        {
            if (!note.playing)
                return 0;

            double fullNoteBend = masterBend + note.indivBend;
            // Map fullNoteBend to the range 0-16383
            int midiNoteBend = (int)(8192 + Math.Round(fullNoteBend * 4096));

            // MIDI maximum bend range is 2 half steps. If the bend exceeds that, just send a different MIDI noteID
            // TODO this doesn't quite work yet
            if (midiNoteBend >= 16383 || midiNoteBend < 0)
            {
                int distInHalfSteps = (midiNoteBend - 8192) / 4086; // 4086 = 1 semitone
                note.midiNoteID += distInHalfSteps; // choose a new MIDI noteID
                midiNoteBend -= (distInHalfSteps) * 4086; // subtract the necessary semitones
            }

            // If midiNoteBend is outside this range AGAIN, then the algorithm wants to tune
            // more than 8192 half steps up or down, that's not allowed.
            if (midiNoteBend >= 16383 || midiNoteBend <= -16383)
                throw new ArgumentOutOfRangeException("Too much bend!!");
            
            return (ushort)midiNoteBend;
        }

        public void RandomlyAssignIndivBends(Chord chord, double bendRange = 0.3)
        /// Given a bend range as a fraction of a half step, randomly assign individual bends to each note in a chord
        /// This function was mostly useful for debugging
        {
            Random random = new Random();

            foreach (Note n in chord.notes)
            {
                n.indivBend = 0.8 + (random.NextDouble() * (bendRange * 2)) - bendRange;
            }
        }
    }

    struct Range
    {
        public double lower;
        public double upper;
        public Range(double l, double u)
        {
            lower = l;
            upper = u;
        }

        public bool Contains(double x)
        {
            return (x >= lower && x <= upper);
        }

        // Returns 'o' if r1 and r2 overlap; 'h' if r2 is completely above r1; 'l' if r2 is completely below r1.
        public static double Distance(Range r1, Range r2)
        {
            if (r2.lower <= r1.upper && r2.upper >= r1.lower)
            {
                return 0;
            }
            // Positive result if r2 is higher than r1
            else if (r2.lower > r1.upper)
            {
                return r2.lower - r1.upper;
            }
            // Negative result if r2 is lower than r1
            else
            {
                return r2.upper - r1.lower;
            }
        }

        // Check whether two ranges overlap and return the overlapping part
        public static Range GetOverlap(Range r1, Range r2)
        {
            if (r1.lower > r1.upper || r2.lower > r2.upper)
                throw new ArgumentException("Invalid range in overlap method");

            Range ol = r1;

            if (Distance(r1, r2) == 0)
            {
                if (r2.lower > ol.lower)
                    ol.lower = r2.lower;
                if (r2.upper < ol.upper)
                    ol.upper = r2.upper;
                return ol;
            }
            else
            {
                // There is no overlap
                throw new ArgumentException("These two ranges do not overlap.");
            }
        }
        // Move both bounds of a Range up by a given distance
        public Range MoveBy(double distance)
        {
            Range res = this;
            res.lower += distance;
            res.upper += distance;
            return res;
        }

        // Define equality operators for the Range struct
        public static bool operator ==(Range r1, Range r2)
        {
            return r1.upper == r2.upper && r1.lower == r2.lower;
        }
        public static bool operator !=(Range r1, Range r2)
        {
            return r1.upper != r2.upper || r1.lower != r2.lower;
        }
    }

    public struct Fraction
    {
        public int numerator;
        public int denominator;

        public Fraction(int num, int denom)
        {
            numerator = num;
            denominator = denom;
        }
        public Fraction(string input)
        {
            // Comments can be added after a hashtag
            string noComments = input.Split('#')[0];

            string[] portions = noComments.Split('/');

            if (!input.Contains('/') || portions.Length != 2)
                throw new FormatException(string.Format("The fraction {0} can't be parsed", input));

            numerator = int.Parse(portions[0]);
            denominator = int.Parse(portions[1]);

            if (denominator == 0)
                throw new DivideByZeroException(string.Format("The fraction {0} has 0 as its denominator, which is impossible", input));
        }
        public double ToFactor()
        {
            return ((double)numerator / (double)denominator);
        }
        public override string ToString()
        {
            return numerator.ToString() + "/" + denominator.ToString();
        }
    }
}
