using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Bot.Framework
{
    public class AntiFloodManager : IDisposable
    {
        public TimeSpan AntiFloodTimespan { get; set; } = TimeSpan.FromMinutes(1);
        public uint AntiFloodMessageCount { get; set; } = 0;
        List<FloodEntry> _floodList = new List<FloodEntry>();
        public AntiFloodManager()
        {
            _floodList = LoadAntiFloodData("flood.csv");
        }
        public void SaveAntiFloodData()
        {
            SaveFloodList("flood.csv", _floodList);
        }
        public bool CheckFlood(long telegramUserId)
        {
            return CheckFlood(telegramUserId, DateTime.Now);
        }
        /// <summary>
        /// Returns true if no flood / everything ok
        /// </summary>
        /// <param name="telegramUserId"></param>
        /// <param name="requestTime"></param>
        /// <returns></returns>
        public bool CheckFlood(long telegramUserId, DateTime requestTime)
        {
            if (_floodList.FirstOrDefault(x => x.TelegramUserId == telegramUserId) is FloodEntry entry)
            {
                if (entry.BlockedUntil >= requestTime)
                    return false;
                lock (entry.RequestTimes)
                {
                    entry.RequestTimes.Add(requestTime);
                    var rTimes = entry.RequestTimes.SkipWhile(rqt => requestTime - rqt > AntiFloodTimespan);
                    entry.RequestTimes = rTimes.ToList();
                    if (rTimes.Count() > AntiFloodMessageCount)
                    {
                        if (entry.BlockedUntil == DateTime.MinValue)
                            entry.BlockedUntil = requestTime.AddDays(1);
                        return AntiFloodMessageCount == 0 || false;
                    }
                }
                return entry.BlockedUntil < requestTime;
            }
            else
            {
                _floodList.Add(new FloodEntry(telegramUserId));
                return true;
            }
        }
        private List<FloodEntry> LoadAntiFloodData(string path)
        {
            List<FloodEntry> floodList = new List<FloodEntry>();
            if (File.Exists(path))
            {
                string[] lines = File.ReadAllLines(path, Encoding.UTF8).Reverse().ToArray();
                foreach (string line in lines)
                {
                    string[] columns = line.Split(';');
                    if (columns.Length >= 2)
                    {
                        if (long.TryParse(columns[0], out long telegramUserId))
                            if (DateTime.TryParse(columns[1], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime blockedUntil))
                                floodList.Add(new FloodEntry(telegramUserId, blockedUntil));

                    }
                }
            }
            return floodList;
        }
        private void SaveFloodList(string path, List<FloodEntry> floodList)
        {
            List<string> lines = new List<string>();
            foreach (var item in floodList)
                lines.Add(item.TelegramUserId + ";" + item.BlockedUntil.ToString(CultureInfo.InvariantCulture));
            File.WriteAllLines(path, lines);
        }

        public void Dispose()
        {
            SaveAntiFloodData();
            GC.SuppressFinalize(this);
        }
    }
    public class FloodEntry
    {
        public long TelegramUserId { get; set; } = 0;
        public DateTime BlockedUntil { get; set; } = DateTime.MinValue;
        public List<DateTime> RequestTimes { get; set; } = new List<DateTime>();
        public FloodEntry()
        {

        }
        public FloodEntry(long telegramUserId, DateTime? blockedUntil = null)
        {
            TelegramUserId = telegramUserId;
            BlockedUntil = blockedUntil ?? DateTime.MinValue;
        }
    }
}
