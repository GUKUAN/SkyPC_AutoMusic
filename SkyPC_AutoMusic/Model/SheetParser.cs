using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace SkyPC_AutoMusic.Model
{
    //把各种曲谱文本统一解析成内部 Sheet（songNotes 结构）
    static class SheetParser
    {
        //音名 -> 调性等级
        private static readonly Dictionary<string, int> pitchLevels = new Dictionary<string, int>
        {
            { "C", 0 }, { "C#", 1 }, { "Db", 1 }, { "D♭", 1 }, { "D", 2 }, { "D#", 3 }, { "Eb", 3 }, { "E♭", 3 },
            { "E", 4 }, { "F", 5 }, { "F#", 6 }, { "Gb", 6 }, { "G♭", 6 }, { "G", 7 }, { "G#", 8 }, { "Ab", 8 },
            { "A♭", 8 }, { "A", 9 }, { "A#", 10 }, { "Bb", 10 }, { "B♭", 10 }, { "B", 11 }
        };

        public static Sheet Parse(string json)
        {
            JObject root = JObject.Parse(json);

            //VSRG 是轨道式谱面，得先把 hitObjects 摊平成 songNotes
            if (root["hitObjects"] != null || root["tracks"] != null)
                return FromVsrg(root);

            return root.ToObject<Sheet>();
        }

        private static Sheet FromVsrg(JObject root)
        {
            Sheet sheet = new Sheet
            {
                name = (string)root["name"],
                author = (string)root["author"],
                transcribedBy = (string)root["transcribedBy"],
                bpm = (int?)root["bpm"] ?? 0,
                bitsPerPage = 16,
                pitchLevel = PitchLevel((string)root["pitch"]),
                songNotes = new List<SongNote>()
            };

            //把所有轨道（多人演奏）合并成一首
            foreach (JToken track in root["tracks"] ?? new JArray())
            {
                foreach (JToken hit in track["hitObjects"] ?? new JArray())
                {
                    JArray item = hit as JArray;
                    if (item == null || item.Count < 4)
                        continue;
                    if (!(item[3] is JArray pitches))
                        continue;

                    int time = (int)Math.Round((double)item[1]);
                    foreach (JToken pitch in pitches)
                    {
                        int index = (int)pitch;
                        if (index < 0)
                            continue;
                        sheet.songNotes.Add(new SongNote { time = time, key = "1Key" + index });
                    }
                }
            }

            return sheet;
        }

        private static int PitchLevel(string pitch)
        {
            if (pitch == null)
                return 0;
            int level;
            return pitchLevels.TryGetValue(pitch, out level) ? level : 0;
        }
    }
}
