using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RegroupUserUpdater.Interfaces;
using RegroupUserUpdater.Models;
using System.Text;

namespace RegroupUserUpdater.Services
{
    public class CsvService : ICsvService
    {
        public async Task<List<CsvData>> ParseCsvFileAsync(IFormFile file)
        {
            var csvDataList = new List<CsvData>();
            Dictionary<string, int> headerIndexes = new Dictionary<string, int>();
            string[]? headers = null;

            using (var stream = new StreamReader(file.OpenReadStream()))
            {
                string? line;
                bool isFirstLine = true;
                while ((line = await stream.ReadLineAsync()) != null)
                {
                    if (isFirstLine)
                    {
                        headers = ParseCsvLine(line)
                            .Select(h => h.Trim().ToLower()
                                .Replace("/", "")
                                .Replace(" ", "")
                                .Replace("-", "")
                                .Replace("\\", ""))
                            .ToArray();
                        
                        for (int i = 0; i < headers.Length; i++)
                        {
                            headerIndexes[headers[i]] = i;
                        }
                        
                        isFirstLine = false;
                        continue;
                    }

                    var parts = ParseCsvLine(line);
                    if (headers != null && parts.Length >= headers.Length)
                    {
                        var csvData = new CsvData();
                        
                        for (int i = 0; i < headers.Length; i++)
                        {
                            if (i < parts.Length)
                            {
                                var propertyName = headers[i];
                                var property = typeof(CsvData).GetProperties()
                                    .FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
                                if (property != null)
                                {
                                    property.SetValue(csvData, parts[i].Trim());
                                }
                            }
                        }
                        
                        csvDataList.Add(csvData);
                    }
                }
            }

            return csvDataList;
        }

        public async Task<List<InfoRequests>> ParseDailyAlertCsvFileAsync(IFormFile file)
        {
            var infoRequestsList = new List<InfoRequests>();

            using (var stream = new StreamReader(file.OpenReadStream()))
            {
                infoRequestsList = await ParseDailyAlertStream(stream);
            }

            return infoRequestsList;
        }

        public async Task<List<InfoRequests>>  ParseDailyAlertStream(StreamReader stream)
        {
            List<InfoRequests> infoRequestsList = new List<InfoRequests>();
            Dictionary<string, int> headerIndexes = new Dictionary<string, int>();
            string[]? headers = null;
            string? line;
            bool isFirstLine = true;
            while ((line = await stream.ReadLineAsync()) != null)
            {
                if (isFirstLine)
                {
                    headers = ParseCsvLine(line)
                        .Select(h => h.Trim().ToLower()
                            .Replace("/", "")
                            .Replace(" ", "")
                            .Replace("-", "")
                            .Replace("\\", ""))
                        .ToArray();
                        
                    for (int i = 0; i < headers.Length; i++)
                    {
                        headerIndexes[headers[i]] = i;
                    }
                        
                    isFirstLine = false;
                    continue;
                }

                var parts = ParseCsvLine(line);
                if (headers != null && parts.Length >= headers.Length)
                {
                    var infoRequest = new InfoRequests();
                        
                    for (int i = 0; i < headers.Length; i++)
                    {
                        if (i < parts.Length)
                        {
                            var propertyName = headers[i].Replace(" ", "");
                            var property = typeof(InfoRequests).GetProperties()
                                .FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
                            if (property != null)
                            {
                                property.SetValue(infoRequest, parts[i].Trim());
                            }
                        }
                    }
                        
                    infoRequestsList.Add(infoRequest);
                }
            }

            return infoRequestsList;
        }

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;
            bool escapeNext = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (escapeNext)
                {
                    current.Append(c);
                    escapeNext = false;
                    continue;
                }

                if (c == '\\')
                {
                    escapeNext = true;
                    continue;
                }

                if (c == '"')
                {
                    if (!inQuotes)
                    {
                        inQuotes = true;
                    }
                    else if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(c);
            }

            result.Add(current.ToString());
            return result.ToArray();
        }
    }
} 