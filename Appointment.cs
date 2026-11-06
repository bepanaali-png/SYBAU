namespace MedicalApi.Models;

// Запись пациента на приём к врачу (свой модуль, задание 6)
public class Appointment
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public string DoctorName { get; set; } = "";

    // Специальность: терапевт, педиатр, окулист...
    public string Specialty { get; set; } = "";

    public DateTime StartTime { get; set; }

    public int DurationMinutes { get; set; } = 30;

    // Причина обращения
    public string Reason { get; set; } = "";

    // "scheduled" (запланирован), "completed" (состоялся), "cancelled" (отменён)
    public string Status { get; set; } = "scheduled";

    public DateTime CreatedAt { get; set; }

    // Когда приём заканчивается. Считается сам
    public DateTime EndTime
    {
        get { return StartTime.AddMinutes(DurationMinutes); }
    }
}
