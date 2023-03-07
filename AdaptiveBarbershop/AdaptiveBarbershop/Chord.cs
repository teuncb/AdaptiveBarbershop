namespace AdaptiveBarbershop
{
    class Chord
    {
        public Note[] notes;
        public double masterBend;
        public int startTime;
        public int duration;
        public string chordName;  // TODO meer commentaar

        public Chord(string input, int start)
        {
            // Split the input string into chord name, 4 notes, and duration
            char[] delimiters = { '(', ')', ',' };
            string[] portions = input.Split(delimiters);

            chordName = portions[0];

            // Make a Note object for each note input string
            notes = new Note[4];
            for(int i = 0; i < notes.Length; i++)
            {
                notes[i] = new Note(portions[i+1]);
            }

            startTime = start;
            duration = int.Parse(portions[5]);
            masterBend = 0;
        }
    }
}
