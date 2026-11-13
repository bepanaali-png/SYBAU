namespace MedicalApi.Models;

// Статистика для главврача (задание 7)
public class ClinicStats
{
    public int PatientsCount { get; set; }

    public int SpravkiCount { get; set; }

    // На какую сумму выписано справок
    public decimal TotalRevenue { get; set; }

    // Разбивка по видам справок, сначала самые прибыльные
    public List<SpravkaTypeStats> ByType { get; set; } = new List<SpravkaTypeStats>();

    // Сколько приёмов ещё впереди
    public int UpcomingAppointments { get; set; }
}

public class SpravkaTypeStats
{
    public int SpravkaTypeId { get; set; }

    public string Name { get; set; } = "";

    public int Count { get; set; }

    public decimal Revenue { get; set; }
}
