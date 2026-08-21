using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Knightworld.Core
{
    public sealed class RailSaveState
    {
        public string MapTitle = "";
        public string StartTownId = "";
        public int SeedState;
        public int Score;
        public int SeatCount = RailSession.StartingSeats;
        public int SeatUpgradesBought;
        public int CarriagesBought;
        public int NextPassengerId = 1;
        public string CurrentTownId = "";
        public string TravelFromId = "";
        public string TravelToId = "";
        public long TravelDepartUtcTicks;
        public float TravelDurationSeconds;
        public List<string> Unlocked = new List<string>();
        public List<string> CompletedQuests = new List<string>();
        public List<PassengerRec> Onboard = new List<PassengerRec>();
        public List<PassengerRec> Waiting = new List<PassengerRec>();

        public static PassengerRec FromPassenger(Passenger person)
        {
            return new PassengerRec
            {
                Id = person.Id,
                Name = person.Name,
                OriginId = person.OriginId,
                DestId = person.DestId,
                Fare = person.Fare,
                IsQuest = person.IsQuest,
                QuestKey = person.QuestKey ?? ""
            };
        }

        public sealed class PassengerRec
        {
            public int Id;
            public string Name;
            public string OriginId;
            public string DestId;
            public int Fare;
            public bool IsQuest;
            public string QuestKey;

            public Passenger ToPassenger()
            {
                if (string.IsNullOrEmpty(OriginId) || string.IsNullOrEmpty(DestId))
                    return null;
                return new Passenger(
                    Id,
                    string.IsNullOrEmpty(Name) ? "Rider" : Name,
                    OriginId,
                    DestId,
                    Fare,
                    string.IsNullOrEmpty(QuestKey) ? null : QuestKey,
                    IsQuest);
            }
        }
    }

    public static class RailSaveCodec
    {
        public const string Header = "KNIGHTSAVE 1";

        public static string Write(RailSaveState state)
        {
            var text = new StringBuilder();
            text.AppendLine(Header);
            Field(text, "map", Escape(state.MapTitle));
            Field(text, "start", state.StartTownId);
            Field(text, "rng", state.SeedState.ToString(CultureInfo.InvariantCulture));
            Field(text, "score", state.Score.ToString(CultureInfo.InvariantCulture));
            Field(text, "seats", state.SeatCount.ToString(CultureInfo.InvariantCulture));
            Field(text, "seatbuys", state.SeatUpgradesBought.ToString(CultureInfo.InvariantCulture));
            Field(text, "carriages", state.CarriagesBought.ToString(CultureInfo.InvariantCulture));
            Field(text, "next", state.NextPassengerId.ToString(CultureInfo.InvariantCulture));
            Field(text, "town", state.CurrentTownId);
            Field(text, "trip", string.Join("\t", new[]
            {
                state.TravelFromId ?? "",
                state.TravelToId ?? "",
                state.TravelDepartUtcTicks.ToString(CultureInfo.InvariantCulture),
                state.TravelDurationSeconds.ToString("R", CultureInfo.InvariantCulture)
            }));
            for (int i = 0; i < state.Unlocked.Count; i++)
                Field(text, "unlock", state.Unlocked[i]);
            for (int i = 0; i < state.CompletedQuests.Count; i++)
                Field(text, "questdone", state.CompletedQuests[i]);
            for (int i = 0; i < state.Onboard.Count; i++)
                Field(text, "onboard", PassengerLine(state.Onboard[i]));
            for (int i = 0; i < state.Waiting.Count; i++)
                Field(text, "wait", PassengerLine(state.Waiting[i]));
            return text.ToString();
        }

        public static bool TryRead(string text, out RailSaveState state)
        {
            state = new RailSaveState();
            if (string.IsNullOrEmpty(text))
                return false;
            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            if (lines.Length == 0 || lines[0].Trim() != Header)
                return false;
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0)
                    continue;
                int space = line.IndexOf(' ');
                string key = space < 0 ? line : line.Substring(0, space);
                string value = space < 0 ? "" : line.Substring(space + 1);
                switch (key)
                {
                    case "map":
                        state.MapTitle = Unescape(value);
                        break;
                    case "start":
                        state.StartTownId = value;
                        break;
                    case "rng":
                        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out state.SeedState);
                        break;
                    case "score":
                        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out state.Score);
                        break;
                    case "seats":
                        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out state.SeatCount);
                        break;
                    case "seatbuys":
                        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out state.SeatUpgradesBought);
                        break;
                    case "carriages":
                        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out state.CarriagesBought);
                        break;
                    case "next":
                        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out state.NextPassengerId);
                        break;
                    case "town":
                        state.CurrentTownId = value;
                        break;
                    case "trip":
                        ReadTrip(state, value);
                        break;
                    case "unlock":
                        state.Unlocked.Add(value);
                        break;
                    case "questdone":
                        state.CompletedQuests.Add(value);
                        break;
                    case "onboard":
                        ReadPassenger(state.Onboard, value);
                        break;
                    case "wait":
                        ReadPassenger(state.Waiting, value);
                        break;
                }
            }

            return !string.IsNullOrEmpty(state.CurrentTownId);
        }

        public static bool MatchesMap(RailSaveState state)
        {
            if (state == null || RailroadGraph.Map == null)
                return false;
            if (state.MapTitle != RailroadGraph.Map.Title)
                return false;
            if (!string.IsNullOrEmpty(state.StartTownId) && state.StartTownId != RailroadGraph.StartTownId)
                return false;
            return true;
        }

        private static void ReadTrip(RailSaveState state, string value)
        {
            var parts = value.Split('\t');
            if (parts.Length < 4)
                return;
            state.TravelFromId = parts[0];
            state.TravelToId = parts[1];
            long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out state.TravelDepartUtcTicks);
            float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out state.TravelDurationSeconds);
        }

        private static void ReadPassenger(List<RailSaveState.PassengerRec> list, string value)
        {
            var parts = value.Split('\t');
            if (parts.Length < 5)
                return;
            int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id);
            int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int fare);
            bool isQuest = parts.Length >= 6 && parts[5] == "1";
            list.Add(new RailSaveState.PassengerRec
            {
                Id = id,
                Name = Unescape(parts[1]),
                OriginId = parts[2],
                DestId = parts[3],
                Fare = fare,
                IsQuest = isQuest,
                QuestKey = parts.Length >= 7 ? Unescape(parts[6]) : ""
            });
        }

        private static string PassengerLine(RailSaveState.PassengerRec person)
        {
            return string.Join("\t", new[]
            {
                person.Id.ToString(CultureInfo.InvariantCulture),
                Escape(person.Name),
                person.OriginId ?? "",
                person.DestId ?? "",
                person.Fare.ToString(CultureInfo.InvariantCulture),
                person.IsQuest ? "1" : "0",
                Escape(person.QuestKey ?? "")
            });
        }

        private static void Field(StringBuilder text, string key, string value)
        {
            text.Append(key);
            text.Append(' ');
            text.Append(value);
            text.Append('\n');
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\n", "\\n");
        }

        private static string Unescape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\\\", "\\");
        }
    }
}
