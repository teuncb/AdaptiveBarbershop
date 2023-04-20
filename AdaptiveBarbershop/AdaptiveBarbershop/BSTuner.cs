using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;

namespace AdaptiveBarbershop
{
    class BSTuner
    {
        // The 4 voices in order of importance. When optimising tied notes, the lead should be
        // handled first, then the bass, then the tenor and lastly the baritone.
        private static int[] voicesOrder = new int[4] { 2, 0, 3, 1 };
        private static string[] voiceNames = new string[4] { "bass", "baritone", "lead", "tenor" };
        // Either l for lead or t for tied notes: which one should be optimised first?
        private char priority;
        private static HashSet<char> prioTypes = new HashSet<char> { 'l', 't' };

        private Range tieRange; // How much tied notes are allowed to retune
        private Range leadRange; // How much the lead is allowed to differentiate from 12TET

        // Maps a chord type to its array of interval ratios in just intonation
        private Dictionary<char, Fraction[]> tuningTables;

        /// <summary>
        /// Initialise the tuner type, setting all global parameters for tuning.
        /// </summary>
        /// <param name="tieRadius">How much tied notes are allowed to retune, in semitones.</param>
        /// <param name="leadRadius">How much lead intervals are allowed to deviate from ET intervals, in semitones.</param>
        /// <param name="prio">Either 'l' for lead or 't' for tie: which constraint should be satisfied first?</param>
        /// <param name="pathMaj">Path to a file with just intonation fractions to use for major (M) chords.</param>
        /// <param name="pathMin">Path to a file with just intonation fractions to use for minor (m) chords.</param>
        /// <param name="pathDom">Path to a file with just intonation fractions to use for dominant (7) chords.</param>
        /// <param name="pathDim7">Path to a file with just intonation fractions to use for diminished (o) chords.</param>
        /// <param name="pathHalfDim">Path to a file with just intonation fractions to use for half-diminished (0) chords.</param>
        public BSTuner(double tieRadius = 0.03, double leadRadius = 0.20, char prio = 't',
            string pathMaj     = "../../../../../TuningTables/maj_lim17.txt",
            string pathMin     = "../../../../../TuningTables/min_lim7.txt",
            string pathDom     = "../../../../../TuningTables/maj_lim17.txt",
            string pathDim7    = "../../../../../TuningTables/dim7_lim17.txt",
            string pathHalfDim = "../../../../../TuningTables/min_lim7.txt")
        {
            if (!prioTypes.Contains(prio))
                throw new ArgumentException(prio.ToString() + " is not a valid priority type, should be either l or t");

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

        /// Initialise a table of interval fractions from a file
        /// These are the fractions that will be used for just intonation intervals in the vertical step
        public Fraction[] TuningTable(string tablePath)
        {
            // Require exactly 12 fractions
            string[] lines = File.ReadAllLines(tablePath);
            if (lines.Length != 12)
            {
                throw new FormatException(string.Format("The file {0} doesn't have exactly 12 lines, which is required", tablePath));
            }

            // Build an array of the 12 interval fractions in the file
            Fraction[] tuningTable = new Fraction[12];

            for (int i = 0; i < lines.Length; i++)
            {
                tuningTable[i] = new Fraction(lines[i]);
            }

            return tuningTable;
        }

        /// <summary>
        /// Master method for the tuning algorithm: tunes an entire Song
        /// </summary>
        /// <param name="song">The Song to tune.</param>
        /// <param name="analyze">Whether to return an analysis string (the string will be empty if false).</param>
        /// <param name="print">Whether to print debug messages throughout the tuning process.</param>
        /// <returns>An analysis line in CSV format, containing the information: tieRadius;leadRadius;prio;posterior_drift;max_drift;max_retuning;max_deviation;total_drift;total_retuning;total_deviation;n_retunings;n_deviations</returns>
        public string TuneSong(Song song, bool analyze = true, bool print = false)
        {
            string analysis = "";

            if (analyze)
            {
                song.drifts = new double[song.chords.Length];
                song.maxTieDiffs = new (double, int)[song.chords.Length];
                song.tieDiffs = new double[song.chords.Length][];
                song.leadDevs = new double[song.chords.Length];
            }

            // Carry out the vertical step for each chord
            for (int i = 0; i < song.chords.Length; i++)
            {
                SetIndivBends(song.chords[i], print);
            }

            // Set the lead to equal temperament in the first chord
            InitialMasterBend(song.chords[0]);
            if (analyze)
                song.tieDiffs[0] = new double[4] { 0, 0, 0, 0 };

            // Carry out the horizontal step for each subsequent chord
            for (int i = 1; i < song.chords.Length; i++)
            {
                double mb = SetMasterBend(song.chords[i - 1], song.chords[i], print);

                if (analyze)
                {
                    song.AnalyzeMasterBend(i);
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

        /// <summary>
        /// Vertically tunes the notes inside a chord to just intonation relative to the root
        /// </summary>
        /// <param name="chord"></param>
        /// <param name="print"></param>
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

        /// <summary>
        /// Given a note and its harmonic context, computes its individual bend value in semitones.
        /// </summary>
        /// <param name="noteNum">0 for c, 1 for c#, etc.</param>
        /// <param name="root">The root of the chord this note is in.</param>
        /// <param name="tuningTable">The fractions for this chord type.</param>
        /// <param name="print"></param>
        /// <returns></returns>
        public double GetIndivBend(int noteNum, int root, Fraction[] tuningTable, bool print = false)
        {
            // The number of half steps until note is reached, counting upwards from root
            int distInHalfSteps = noteNum - root;
            if (distInHalfSteps < 0)
                distInHalfSteps += 12;
            else if (distInHalfSteps >= 12)
                distInHalfSteps -= 12;

            Fraction interval = tuningTable[distInHalfSteps];
            // The microtonal distance between note and root in just intonation, counting upwards from root
            double fullDist = 12 * Math.Log2(interval.ToFactor());

            // Return how much note should deviate from equal temperament
            double indivBend = fullDist - distInHalfSteps;
            if(print)
                Console.WriteLine("Tuning note {0} with root {1} to value {2:0.0000} using fraction {3}",
                    noteNum, root, indivBend, interval);
            return indivBend;
        }

        /// Set the masterBend for the very first Chord such that the lead sings an equal temperament note
        public double InitialMasterBend(Chord firstChord)
        {
            firstChord.masterBend = -firstChord.notes[2].indivBend;
            return firstChord.masterBend;
        }

        /// Horizontally tunes each Chord to optimise the tie, lead and drift constraints of the algorithm
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
                tieDiffs[i] = prevChord.posteriorBend(ties[i]) - currChord.posteriorBend(ties[i]);
            }

            // Same difference for the lead voice
            double leadDiff = prevChord.posteriorBend(2) - currChord.posteriorBend(2);

            // Determine the optimal masterBend for currChord
            // Option 1: all can be optimised by setting the masterBend to 0
            if (tieDiffs.All(diff => tieRange.Contains(diff)) && leadRange.Contains(leadDiff))
            {
                if (print)
                    Console.WriteLine("0 is in all ranges, setting master bend to 0");
                currChord.masterBend = 0;
                return 0;
            }
            // Option 2: pick a masterbend that is within the ranges of all tied notes and the lead interval
            else
            {
                // ranges contains, for each tied note & lead note in these chords, the range in which the 
                // can move according to this note. In other words, it's the amount that the masterbend is
                // allowed to move up and down while satisfying the lead and/or tie constraint for this note.
                Range[] ranges = new Range[tieDiffs.Length + 1];

                // Ties are more important
                if (priority == 't')
                {
                    // Add ranges for tied notes
                    for (int i = 0; i < tieDiffs.Length; i++)
                    {
                        ranges[i] = tieRange.MoveBy(tieDiffs[i]);
                    }
                    
                    // Add range for the lead
                    ranges[ranges.Length - 1] = leadRange.MoveBy(leadDiff);
                }
                // Lead is more important
                else
                {
                    // Add range for the lead
                    ranges[0] = leadRange.MoveBy(leadDiff);

                    // Add ranges for tied notes
                    for (int i = 0; i < tieDiffs.Length; i++)
                    {
                        ranges[i + 1] = tieRange.MoveBy(tieDiffs[i]);
                    }
                }

                // Set the initial boundaries in which the new masterBend should be chosen
                Range masterBendRange = ranges[0];

                // Collapse the boundaries until you've either reached the last note,
                // or you can choose the optimal masterBend for minimal pitch drift
                for (int i = 1; i < ranges.Length; i++)
                {
                    double overlapResult = Range.Distance(masterBendRange, ranges[i]);
                    if (overlapResult == 0)
                    // There is overlap; continue collapsing the boundaries of masterBendRange
                    {
                        masterBendRange = Range.GetOverlap(masterBendRange, ranges[i]);
                    }
                    else if (overlapResult > 0)
                    // Range for next tied/lead note is completely above the current range
                    // Ignore pitch drift and subsequent notes, compromise with the next note
                    {
                        if (print)
                            Console.WriteLine("Range with index {0} cannot be satisfied, choosing the highest possible masterBend.", i);
                        currChord.masterBend = masterBendRange.upper;
                        return masterBendRange.upper;
                    }
                    else
                    // Range for next tied/lead note is completely below the current range
                    // Ignore pitch drift and subsequent notes, compromise with the next note
                    {
                        if (print)
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
                // This should be unreachable code, since we covered this in option 1
                {
                    if (print)
                        Console.WriteLine("Warning: reached code that should be unreachable in SetMasterBend()");
                    currChord.masterBend = 0;
                    return 0;
                }
            }
        }

        /// <summary>
        /// Maps a note's posterior bend to the range in the actual MIDI format
        /// </summary>
        /// <param name="chord">The Chord the note is in.</param>
        /// <param name="note">The Note to calculate the MIDI bend value of.</param>
        /// <returns>A number between 0 and 16383, where 8192 represents a bend of 0, 16383 two semitones up and 0 two semitones down.</returns>
        public static ushort MIDIBend(Chord chord, Note note)
        {
            if (!note.playing)
                return 0;

            // Map fullNoteBend to the range 0-16383
            int midiNoteBend = (int)(8192 + Math.Round(chord.posteriorBend(note) * 4096));

            // MIDI maximum bend range is 2 half steps. If the bend exceeds that, just send a different MIDI noteID
            // I'm not 100% sure about this code
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

        /// Gives some overall statistics on how tuning this song went.
        public string AnalyzeTuning(Song song)
        {
            string analysis = "";

            Console.WriteLine("--------------------------------");
            Console.WriteLine("The song was successfully tuned. Here are some fun facts:");
            Console.WriteLine("Overall pitch drift: {0:0.0000}", song.chords[song.chords.Length - 1].masterBend);

            analysis += string.Format("{0:0.0000};", song.chords[song.chords.Length - 1].masterBend);

            // Find the maximum and total pitch drift from one chord to the next
            double max = 0;
            int maxIdx = 0;
            double totalDrift = 0;
            for (int c = 1; c < song.drifts.Length; c++)
            {
                totalDrift += Math.Abs(song.drifts[c]);
                if (Math.Abs(song.drifts[c]) > Math.Abs(max))
                {
                    max = song.drifts[c];
                    maxIdx = c;
                }
            }

            Console.WriteLine("Total pitch drift: {0:0.0000}", totalDrift);
            if (max == 0)
                Console.WriteLine("There was no pitch drift at all in this song!");
            else
                Console.WriteLine("Most dramatic pitch drift moment: {0:0.0000} when going from chord {1} ({2}) to chord {3} ({4})",
                    max, maxIdx - 1, song.chords[maxIdx - 1], maxIdx, song.chords[maxIdx]);

            analysis += string.Format("{0:0.0000};", max);

            // Find the number of retuning jumps in tied notes over 3 cents, and the total tie retuning
            int retunings = 0;
            double totalTie = 0;
            for (int c = 0; c < song.tieDiffs.Length; c++)
                for (int v = 0; v < 4; v++)
                {
                    totalTie += Math.Abs(song.tieDiffs[c][v]);
                    if (song.tieDiffs[c][v] > 0.03)
                        retunings++;
                }
            Console.WriteLine("Tied notes had to be retuned audibly {0} times", retunings);
            Console.WriteLine("Total tie retuning: {0:0.0000}", totalTie);

            // Find the biggest retuning jump in a tied note
            max = 0;
            maxIdx = -1;
            int maxVoice = -1;
            for (int c = 0; c < song.maxTieDiffs.Length; c++)
            {
                (double diff, int v) = song.maxTieDiffs[c];
                if (Math.Abs(diff) > Math.Abs(max))
                {
                    max = diff;
                    maxIdx = c;
                    maxVoice = v;
                }
            }
            if (maxIdx == -1)
            {
                Console.WriteLine("Not a single tied note had to retune, great!");
            }
            else
                Console.WriteLine("Most dramatic tie change: {0:0.0000} in the {1} from chord {2} ({3}) to chord {4} ({5})",
                max, voiceNames[maxVoice], maxIdx - 1, song.chords[maxIdx - 1], maxIdx, song.chords[maxIdx]);

            analysis += string.Format("{0:0.0000};", max);

            // Find the number of deviations from equal temperament in the lead over 10 cents
            int deviations = 0;
            double totalLead = 0;
            for (int c = 0; c < song.leadDevs.Length; c++)
            {
                totalLead += Math.Abs(song.leadDevs[c]);
                if (song.leadDevs[c] > 0.10)
                    deviations++;
            }
            Console.WriteLine("Lead intervals had to deviate audibly from ET {0} times", deviations);
            Console.WriteLine("Total lead deviation: {0:0.0000}", totalLead);

            // Find the biggest deviation from equal temperament in the lead voice
            max = 0;
            maxIdx = -1;
            for (int c = 0; c < song.leadDevs.Length; c++)
            {
                if (Math.Abs(song.leadDevs[c]) > Math.Abs(max))
                {
                    max = song.leadDevs[c];
                    maxIdx = c;
                }
            }
            if (maxIdx == -1)
            {
                Console.WriteLine("Every lead interval was exactly like equal temperament, great!");
            }
            else
                Console.WriteLine("Most dramatic ET deviation in the lead: {0:0.0000} from chord {1} ({2}) to chord {3} ({4})",
                max, maxIdx - 1, song.chords[maxIdx - 1], maxIdx, song.chords[maxIdx]);

            analysis += string.Format("{0:0.0000};", max);
            analysis += string.Format("{0:0.0000};{1:0.0000};{2:0.0000};", totalDrift, totalTie, totalLead); // Total drift, retuning, deviation
            analysis += string.Format("{0};{1}", retunings, deviations); // Number of retunings and deviations

            Console.WriteLine("--------------------------------");
            return analysis;
        }

        /// Given a bend range as a fraction of a half step, randomly assign individual bends to each note in a chord
        /// This function was mostly useful for debugging
        public void RandomlyAssignIndivBends(Chord chord, double bendRange = 0.3)
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

        /// Determines whether a number is within this range
        public bool Contains(double x)
        {
            return (x >= lower - 0.0000001 && x <= upper + 0.0000001);
        }

        /// Returns the distance between two ranges if they don't overlap,
        /// or 0 if they do overlap.
        public static double Distance(Range r1, Range r2)
        {
            if (r2.lower <= r1.upper + 0.0000001 && r2.upper >= r1.lower - 0.0000001)
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

        /// Checks whether two ranges overlap and return the overlapping part
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

        /// Moves both bounds of a Range up by a given distance
        public Range MoveBy(double distance)
        {
            Range res = this;
            res.lower += distance;
            res.upper += distance;
            return res;
        }

        /// Define equality operators for the Range struct
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
