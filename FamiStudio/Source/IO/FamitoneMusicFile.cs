﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    public class FamitoneMusicFile
    {
        private Project project;
        private List<string> lines = new List<string>();

        private string db = ".byte";
        private string dw = ".word";
        private string ll = "@";
        private string lo = ".lobyte";

        //private int globalReferenceId = 0;
        private List<List<byte>> globalPacketPatternBuffers = new List<List<byte>>(); 

        private const int MinPatternLength = 6;
        private const int MaxRepeatCount = 60;
        private const int MaxSongs = (256 - 5) / 14;
        private const int MaxPatterns = 128 * MaxSongs;
        private const int MaxPackedPatterns = (5 * MaxPatterns * MaxSongs);

        public enum OutputFormat
        {
            NESASM,
            CA65,
            ASM6
        };

        private void CleanupEnvelopes()
        {
            // ALl instruments must have a volume envelope.
            foreach (var instrument in project.Instruments)
            {
                var env = instrument.Envelopes[Envelope.Volume];
                if (env == null)
                {
                    env = new Envelope(); 
                    instrument.Envelopes[Envelope.Volume] = env;
                }
                if (env.Length == 0)
                {
                    env.Length = 1;
                    env.Loop = -1;
                    env.Values[0] = 15;
                }
            }
        }

        private int OutputHeader(bool separateSongs)
        {
            string name = Utils.MakeNiceAsmName(separateSongs ? project.Songs[0].Name : project.Name);

            lines.Add($";this file for FamiTone2 library generated by Famitone Studio");
            lines.Add("");
            lines.Add($"{name}_music_data:");
            lines.Add($"\t{db} {project.Songs.Count}");
            lines.Add($"\t{dw} {ll}instruments");
            lines.Add($"\t{dw} {ll}samples-3");

            int size = 5;

            for (int i = 0; i < project.Songs.Count; i++)
            {
                var song = project.Songs[i];
                var line = $"\t{dw} ";

                for (int chn = 0; chn < 5; ++chn)
                {
                    line += $"{ll}song{i}ch{chn},";
                }

                int tempoPal  = 256 * song.Tempo / (50 * 60 / 24);
                int tempoNtsc = 256 * song.Tempo / (60 * 60 / 24);

                line += $"{tempoPal},{tempoNtsc}";
                lines.Add(line);

                size += 14;
            }

            lines.Add("");

            return size;
        }

        private byte[] ProcessEnvelope(Envelope env)
        {
            if (env.IsEmpty)
                return null;

            var data = new byte[256];

            byte ptr = 0;
            byte ptr_loop = 0xff;
            byte rle_cnt = 0;
            byte prev_val = (byte)(env.Values[0] + 1);//prevent rle match

            for (int j = 0; j < env.Length; j++)
            {
                if (j == env.Loop) ptr_loop = ptr;

                byte val;

                if (env.Values[j] < -64)
                    val = unchecked((byte)-64);
                else if (env.Values[j] > 63)
                    val = 63;
                else
                    val = (byte)env.Values[j];

                val += 192;

                if (prev_val != val || j == env.Length - 1)
                {
                    if (rle_cnt != 0)
                    {
                        if (rle_cnt == 1)
                        {
                            data[ptr++] = prev_val;
                        }
                        else
                        {
                            while (rle_cnt > 126)
                            {
                                data[ptr++] = 126;
                                rle_cnt -= 126;
                            }

                            data[ptr++] = rle_cnt;
                        }

                        rle_cnt = 0;
                    }

                    data[ptr++] = val;

                    prev_val = val;
                }
                else
                {
                    ++rle_cnt;
                }
            }

            if (ptr_loop == 0xff)
                ptr_loop = (byte)(ptr - 1);
            else if (data[ptr_loop] < 128)
                ++ptr_loop;//ptr_loop increased if it points at RLEd repeats of a previous value

            data[ptr++] = 0;
            data[ptr++] = ptr_loop;

            Array.Resize(ref data, ptr);

            return data;
        }

        private int OutputInstruments()
        {
            // Process all envelope, make unique, etc.
            var uniqueEnvelopes = new SortedList<uint, byte[]>();
            var instrumentEnvelopes = new Dictionary<Envelope, uint>();

            var defaultEnv = new byte[] { 0xc0, 0x00, 0x00 };
            var defaultEnvCRC = CRC32.Compute(defaultEnv);
            uniqueEnvelopes.Add(defaultEnvCRC, defaultEnv);

           foreach (var instrument in project.Instruments)
           {
               foreach (var env in instrument.Envelopes)
               {
                    var processed = ProcessEnvelope(env);

                    if (processed == null)
                    {
                        instrumentEnvelopes[env] = defaultEnvCRC;
                    }
                    else
                    {
                        uint crc = CRC32.Compute(processed);
                        uniqueEnvelopes[crc] = processed;
                        instrumentEnvelopes[env] = crc;
                    }
               }
           }

            int size = 0;

            // Write instruments
            lines.Add($"{ll}instruments:");

            for (int i = 0; i < project.Instruments.Count; i++)
            {
                var instrument = project.Instruments[i];

                var volumeEnvIdx   = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[Envelope.Volume]]);
                var arpeggioEnvIdx = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[Envelope.Arpeggio]]);
                var pitchEnvIdx    = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[Envelope.Pitch]]);

                lines.Add($"\t{db} ${(instrument.DutyCycle << 6) | 0x30:x2} ;instrument {i:x2} ({instrument.Name})");
                lines.Add($"\t{dw} {ll}env{volumeEnvIdx},{ll}env{arpeggioEnvIdx},{ll}env{pitchEnvIdx}");
                lines.Add($"\t{db} $00");

                size += 2 * 3 + 2;
            }

            lines.Add("");

            // Write samples.
            lines.Add($"{ll}samples:");

            if (project.UsesSamples)
            {
                for (int i = 1; i < project.SamplesMapping.Length; i++)
                {
                    var mapping = project.SamplesMapping[i];
                    var sampleOffset = 0;
                    var sampleSize = 0;
                    var samplePitchAndLoop = 0;
                    var sampleName = "";

                    if (mapping != null && mapping.Sample != null)
                    {
                        sampleOffset = project.GetAddressForSample(mapping.Sample) >> 6;
                        sampleSize = mapping.Sample.Data.Length >> 4;
                        sampleName = $"({mapping.Sample.Name})";
                        samplePitchAndLoop = mapping.Pitch | ((mapping.Loop ? 1 : 0) << 6);
                    }

                    lines.Add($"\t{db} ${sampleOffset:x2}+{lo}(FT_DPCM_PTR),${sampleSize:x2},${samplePitchAndLoop:x2}\t;{i} {sampleName}");
                    size += 3;
                }

                lines.Add("");
            }

            // Write envelopes.
            int idx = 0;
            foreach (var env in uniqueEnvelopes.Values)
            {
                lines.Add($"{ll}env{idx++}:");
                lines.Add($"\t{db} {String.Join(",", env.Select(i => $"${i:x2}"))}");
                size += env.Length;
            }

            return size;
        }

        private void OutputSamples(string filename)
        {
            if (project.UsesSamples)
            {
                var sampleData = new byte[project.GetTotalSampleSize()];
                foreach (var sample in project.Samples)
                {
                    Array.Copy(sample.Data, 0, sampleData, project.GetAddressForSample(sample), sample.Data.Length);
                }

                var path = Path.GetDirectoryName(filename);
                var projectname = Utils.MakeNiceAsmName(Path.GetFileNameWithoutExtension(project.Filename));

                File.WriteAllBytes(Path.Combine(path, projectname + ".dmc"), sampleData);
            }
        }

        private int FindEffect(Song song, int patternIdx, int noteIdx, int effect)
        {
            foreach (var channel in song.Channels)
            {
                var pattern = channel.PatternInstances[patternIdx];
                if (pattern != null && pattern.Notes[noteIdx].Effect == effect)
                {
                    return pattern.Notes[noteIdx].EffectParam;
                }
            }

            return -1;
        }

        private int FindSkip(Song song, int patternIdx)
        {
            for (int i = 0; i < song.PatternLength; i++)
            {
                var skip = FindEffect(song, patternIdx, i, Note.EffectSkip);
                if (skip >= 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindLoopPoint(Song song)
        {
            for (int p = 0; p < song.Length; p++)
            {
                for (int i = 0; i < song.PatternLength; i++)
                {
                    int fx = FindEffect(song, p, i, Note.EffectJump);
                    if (fx >= 0)
                    {
                        return fx;
                    }
                }
            }

            return 0;
        }

        private int OutputSong(Song song, int songIdx, int speedChannel, int factor, bool test)
        {
            var packedPatternBuffers = new List<List<byte>>(globalPacketPatternBuffers);
            var size = 0;
            var loopPoint = FindLoopPoint(song);
            var emptyPattern = new Pattern(-1, song, 0, "");

            for (int c = 0; c < song.Channels.Length; c++)
            {
                if (!test)
                    lines.Add($"\n{ll}song{songIdx}ch{c}:");

                var channel = song.Channels[c];
                var isSpeedChannel = c == speedChannel;
                var instrument = (Instrument)null;

                if (isSpeedChannel)
                {
                    if (!test)
                        lines.Add($"\t{db} $fb, ${song.Speed:x2}");
                    size += 2;
                }

                var isSkipping = false;

                for (int p = 0; p < song.Length; p++)
                {
                    var pattern = channel.PatternInstances[p] == null ? emptyPattern : channel.PatternInstances[p];
                    var patternBuffer = new List<byte>();

                    // If we had split the song and we find a skip to the next
                    // pattern, we need to ignore the extra patterns we generated.
                    if (isSkipping && (p % factor) != 0)
                    {
                        continue;
                    }

                    if (!test && p == loopPoint)
                    {
                        lines.Add($"{ll}song{songIdx}ch{c}loop:");
                    }

                    var i = 0;
                    var patternLength = FindSkip(song, p);

                    if (patternLength >= 0)
                    {
                        isSkipping = true;
                    }
                    else
                    {
                        isSkipping = false;
                        patternLength = song.PatternLength;
                    }

                    var numValidNotes = patternLength;

                    while (i < patternLength)
                    {
                        var note = pattern.Notes[i];

                        if (isSpeedChannel)
                        {
                            var speed = FindEffect(song, p, i, Note.EffectSpeed);
                            if (speed >= 0)
                            {
                                patternBuffer.Add(0xfb);
                                patternBuffer.Add((byte)speed);
                            }
                        }

                        i++;

                        if (note.IsValid)
                        {
                            // Instrument change.
                            if (!note.IsStop && note.Instrument != instrument)
                            {
                                int idx = project.Instruments.IndexOf(note.Instrument);
                                patternBuffer.Add((byte)(0x80 | (idx << 1)));
                                instrument = note.Instrument;
                            }

                            int numNotes = 0;

                            // Note -> Empty -> Note special encoding.
                            if (i < patternLength - 1)
                            {
                                var nextNote1 = pattern.Notes[i + 0];
                                var nextNote2 = pattern.Notes[i + 1];

                                var valid1 = nextNote1.IsValid || (isSpeedChannel && FindEffect(song, p, i + 0, Note.EffectSpeed) >= 0);
                                var valid2 = nextNote2.IsValid || (isSpeedChannel && FindEffect(song, p, i + 1, Note.EffectSpeed) >= 0);

                                if (!valid1 && valid2)
                                {
                                    i++;
                                    numValidNotes--;
                                    numNotes = 1;
                                }
                            }

                            patternBuffer.Add((byte)((note.Value << 1) | numNotes));
                        }
                        else
                        {
                            int numEmptyNotes = 0;

                            while (i < patternLength)
                            {
                                var emptyNote = pattern.Notes[i];

                                if (numEmptyNotes >= MaxRepeatCount || emptyNote.IsValid || (isSpeedChannel && FindEffect(song, p, i, Note.EffectSpeed) >= 0))
                                    break;

                                i++;
                                numEmptyNotes++;
                            }

                            numValidNotes -= numEmptyNotes;
                            patternBuffer.Add((byte)(0x81 | (numEmptyNotes << 1)));
                        }
                    }

                    int matchingPatternIdx = -1;

                    if (patternBuffer.Count > 4)
                    {
                        for (int j = 0; j < packedPatternBuffers.Count; j++)
                        {
                            if (packedPatternBuffers[j].SequenceEqual(patternBuffer))
                            {
                                matchingPatternIdx = j;
                                break;
                            }
                        }
                    }

                    if (matchingPatternIdx < 0)
                    {
                        if (packedPatternBuffers.Count > MaxPackedPatterns)
                            return -1; // TODO: Error.

                        packedPatternBuffers.Add(patternBuffer);

                        size += patternBuffer.Count;

                        if (!test)
                        {
                            lines.Add($"{ll}ref{packedPatternBuffers.Count - 1}:");
                            lines.Add($"\t{db} {String.Join(",", patternBuffer.Select(x => $"${x:x2}"))}");
                        }
                    }
                    else
                    {
                        if (!test)
                        {
                            lines.Add($"\t{db} $ff,${numValidNotes:x2}");
                            lines.Add($"\t{dw} {ll}ref{matchingPatternIdx}");
                        }

                        size += 4;
                    }
                }

                if (!test)
                {
                    lines.Add($"\t{db} $fd");
                    lines.Add($"\t{dw} {ll}song{songIdx}ch{c}loop");
                }

                size += 3;
            }

            if (!test)
            {
                globalPacketPatternBuffers = packedPatternBuffers;
            }

            return size;
        }

        private int ProcessAndOutputSong(int songIdx)
        {
            var song = project.Songs[songIdx];

            int minSize = 65536;
            int bestChannel = 0;
            int bestFactor = 1;

            for (int speedChannel = 0; speedChannel < Channel.Count; speedChannel++)
            {
                for (int factor = 1; factor <= song.PatternLength; factor++)
                {
                    if ((song.PatternLength % factor) == 0 && 
                        (song.PatternLength / factor) >= MinPatternLength)
                    {
                        var splitSong = song.Clone();
                        if (splitSong.Split(factor))
                        {
                            int size = OutputSong(splitSong, songIdx, speedChannel, factor, true);

                            if (size < minSize)
                            {
                                minSize = size;
                                bestChannel = speedChannel;
                                bestFactor = factor;
                            }
                        }
                    }
                }
            }

            var bestSplitSong = song.Clone();
            bestSplitSong.Split(bestFactor);

            return OutputSong(bestSplitSong, songIdx, bestChannel, bestFactor, false);
        }
        
        private void SetupFormat(OutputFormat format)
        {
            switch (format)
            {
                case OutputFormat.NESASM:
                    db = ".db";
                    dw = ".dw";
                    ll = ".";
                    lo = "LOW";
                    break;
                case OutputFormat.CA65:
                    db = ".byte";
                    dw = ".word";
                    ll = "@";
                    lo =  ".lobyte";
                    break;
                case OutputFormat.ASM6:
                    db = "db";
                    dw = "dw";
                    ll = "@";
                    lo = "<";
                    break;
            }
        }

        private void SetupProject(Project originalProject, int[] songIds)
        {
            // Work on a temporary copy.
            project = originalProject.Clone();
            project.Filename = originalProject.Filename;

            for (int i = 0; i < project.Songs.Count; i++)
            {
                if (!songIds.Contains(project.Songs[i].Id))
                {
                    project.DeleteSong(project.Songs[i]);
                    i--;
                }
            }

            project.DeleteUnusedInstruments(); 
        }

        public bool Save(Project originalProject, int[] songIds, bool separateSongs, string filename, OutputFormat format)
        {
            SetupProject(originalProject, songIds);
            SetupFormat(format);
            CleanupEnvelopes();
            OutputHeader(separateSongs);
            OutputInstruments();
            OutputSamples(filename);

            for (int i = 0; i < project.Songs.Count; i++)
            {
                ProcessAndOutputSong(i);
            }

            File.WriteAllLines(filename, lines);

            return true;
        }
    }
}
