using Ascend.Data.Import.Attributes;
using Ascend.Data.Import.LogParser;
using CesiumLanguageWriter;
using CesiumLanguageWriter.Advanced;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ascend.Data.Import.Horus
{
    [AscendDataSetImportProviderAttribute(ProviderName = "HorusExportParser", Version = "1.0.0")]
    public class HorusMetadataParser<TData> : GenericLogParser<TData>, IDataSetImporter<TData>
    {
        private const string TABLE_HEADER_START = "Frame;";
        private const string DATA_SEPERATOR = ";";
        public HorusMetadataParser(IDataReader<TData> reader)
            : base(reader)
        {
            HeaderLineSeperaters = new[] { DATA_SEPERATOR };
            DataSeparators = new[] { DATA_SEPERATOR };
            TableHeaderStart = TABLE_HEADER_START;

            LineParsers = new Dictionary<string, ParseInfo>
            {          
                { "Frame", new ParseInfo{ PropertyName = "Frame" ,Parser = StringParser }},
                { "Altitude", new ParseInfo{ PropertyName = "Altitude" ,Parser = NumberParser }},
                { "Azimuth", new ParseInfo{ PropertyName = "Azimuth" ,Parser = NumberParser }},
                { "Heading", new ParseInfo{ PropertyName = "Heading" ,Parser = NumberParser }},
                { "Latitude", new ParseInfo{ PropertyName = "Latitude" ,Parser = NumberParser }},
                { "Longitude", new ParseInfo{ PropertyName = "Longitude" ,Parser = NumberParser }},
                { "NMEA", new ParseInfo{ PropertyName = "NMEA" ,Parser = StringParser }},
                { "NumberOfSatalites", new ParseInfo{ PropertyName = "NumberOfSatalites" ,Parser = NumberParser }},
                { "Quality", new ParseInfo{ PropertyName = "Quality" ,Parser = NumberParser }},
                { "Pitch", new ParseInfo{ PropertyName = "Pitch" ,Parser = NumberParser }},
                { "Roll", new ParseInfo{ PropertyName = "Roll" ,Parser = NumberParser }},
                { "Stamp", new ParseInfo{ PropertyName = "Stamp" ,Parser = DateTimeParser }},
            };

        }



        public static Func<string, CultureInfo, AscendEntityProperty> DateTimeParser = (s, c) =>
        {

            DateTime res;
            if (DateTime.TryParseExact(s.Trim(), "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out res))
            {

                return new AscendEntityProperty(res);
            }
            return StringParser(s, c);
        };



        private int GetFrame(string path)
        {
            var filename = Path.GetFileNameWithoutExtension(path);
            var regex = new Regex("frame_(.*)_0_(.*)_(.*)");
            var match = regex.Match(filename);
            if (!match.Success)
            {
                return int.Parse(new String(Path.GetFileNameWithoutExtension(path).Where(Char.IsNumber).ToArray()));
            }
            return int.Parse(match.Groups[1].Value);// match.Value;

        }
        public Task ImportDataSetsAsync(AscendDataSetImporter importer, IDictionary<string, TData> logs, IDictionary<string, TData> data)
        {
            return ImportDataSetsAsync(importer, logs, data, true);
        }

        public async Task ImportDataSetsAsync(AscendDataSetImporter importer, IDictionary<string, TData> logs, IDictionary<string, TData> data, bool verifyImages)
        {
            var log = logs.Keys.FirstOrDefault(k => Path.GetExtension(k).Equals(".csv", StringComparison.OrdinalIgnoreCase));

            if (log != null)
                await ReadLogAsync(data, log);

            var imgs = data.Keys.Where(p => Path.GetExtension(p).Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                       Path.GetExtension(p).Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
                       .GroupBy(GetFrame).ToDictionary(k => k.Key, v => v.ToArray());

            var missingImage = new List<int>();
            int i = 0;

            foreach (var itemProperties in Items.OrderBy(k => (DateTime)k["Stamp"].Object))
            {
                var gpsEntry = importer.CreateDataSetItem((i++).ToString("D6"), itemProperties);

                gpsEntry.ItemType = "horus.frame";
                gpsEntry.SetGeographyInformation(4326, "Longitude", "Latitude", "Altitude");
                gpsEntry.WriteCmlzPosition("Longitude", "Latitude", Time: "Stamp");
            }
        }

        public Task<bool> AcceptFileAsync(string file, TData data)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> CanImportAsync(IDictionary<string, TData> data, IDataAccessFacade<TData> access)
        {
            var csvs = data.Keys.Where(filename => Path.GetExtension(filename).Equals(".csv", StringComparison.OrdinalIgnoreCase)).ToArray();

            if (csvs.Any())
            {
                foreach (var csv in csvs)
                {
                    using (var stream = await Reader.OpenStreamProvider(await access.GetFileAsync(csv)))
                    {
                        int i = 3;
                        using (var reader = new StreamReader(stream))
                        {
                            //Goto line 3
                            string line = null;
                            while (i-- > 0)
                            {
                                line = await reader.ReadLineAsync();
                            }

                            ParseHeaderData(line);

                            if (Headers.Count == 12 && line.StartsWith(TABLE_HEADER_START))
                                return true;

                        }
                    }

                }
            }

            return false;
        }
    }
}
