using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RegroupUserUpdater.Models;

namespace RegroupUserUpdater.Interfaces
{
    public interface ICsvService
    {
        Task<List<CsvData>> ParseCsvFileAsync(IFormFile file);
        Task<List<InfoRequests>> ParseDailyAlertCsvFileAsync(IFormFile file);
        Task<List<InfoRequests>> ParseDailyAlertStream(StreamReader stream);
    }
} 