using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RegroupUserUpdater.Interfaces;
using RegroupUserUpdater.Models;

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
                        headers = line.Split(',').Select(h => h.Trim().ToLower()).ToArray();
                        for (int i = 0; i < headers.Length; i++)
                        {
                            headerIndexes[headers[i]] = i;
                        }
                        
                        isFirstLine = false;
                        continue;
                    }

                    var parts = line.Split(',');
                    if (headers != null && parts.Length >= headers.Length)
                    {
                        var csvData = new CsvData();
                        
                        for (int i = 0; i < headers.Length; i++)
                        {
                            if (i < parts.Length)
                            {
                                var propertyName = headers[i].Replace(" ", "");
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
    }
} 