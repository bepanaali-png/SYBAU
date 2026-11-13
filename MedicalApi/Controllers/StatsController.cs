using MedicalApi.Models;
using Microsoft.AspNetCore.Mvc;

// Статистика для главврача: сколько справок выписано и на какую сумму
[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    // GET /api/stats  или  GET /api/stats?dateFrom=2026-11-01&dateTo=2026-11-30  (за период, включая последний день)
    [HttpGet]
    [ProducesResponseType<ClinicStats>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public ActionResult<ClinicStats> Get(DateTime? dateFrom, DateTime? dateTo)
    {
        if (dateFrom != null && dateTo != null && dateFrom > dateTo)
        {
            return BadRequest("Дата dateFrom не может быть позже даты dateTo");
        }

        IEnumerable<Spravka> spravki = SpravkiController.Spravki;
        if (dateFrom != null)
        {
            spravki = spravki.Where(s => s.IssuedAt >= dateFrom.Value.Date);
        }
        if (dateTo != null)
        {
            spravki = spravki.Where(s => s.IssuedAt < dateTo.Value.Date.AddDays(1));
        }

        List<Spravka> list = spravki.ToList();

        ClinicStats stats = new ClinicStats
        {
            PatientsCount = PatientsController.Patients.Count,
            SpravkiCount = list.Count,
            TotalRevenue = list.Sum(s => s.Price),
            UpcomingAppointments = AppointmentsController.Appointments
                .Count(a => a.Status == "scheduled" && a.StartTime > DateTime.Now),
            ByType = list
                .GroupBy(s => s.SpravkaTypeId)
                .Select(g => new SpravkaTypeStats
                {
                    SpravkaTypeId = g.Key,
                    Name = g.First().Name,
                    Count = g.Count(),
                    Revenue = g.Sum(s => s.Price)
                })
                .OrderByDescending(t => t.Revenue)
                .ToList()
        };

        return stats;
    }
}
