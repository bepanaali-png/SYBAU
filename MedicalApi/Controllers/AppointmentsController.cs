using MedicalApi.Models;
using Microsoft.AspNetCore.Mvc;

// Свой модуль: запись пациентов на приём к врачу
[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    public static List<Appointment> Appointments = new List<Appointment>();

    // Счётчик Id. Только растёт (тот же приём, что и в справках: Count + 1 ломается после удаления)
    internal static int NextId = 1;

    private static readonly string[] Statuses = { "scheduled", "completed", "cancelled" };
    private static readonly TimeSpan OpenTime = TimeSpan.FromHours(8);
    private static readonly TimeSpan CloseTime = TimeSpan.FromHours(20);

    // Примеры:
    //   GET /api/appointments
    //   GET /api/appointments?patientId=1
    //   GET /api/appointments?doctor=рахимов
    //   GET /api/appointments?date=2026-11-10
    //   GET /api/appointments?status=scheduled
    [HttpGet]
    [ProducesResponseType<List<Appointment>>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public ActionResult<List<Appointment>> GetAll(int? patientId, string? doctor, DateTime? date, string? status)
    {
        IEnumerable<Appointment> result = Appointments;

        if (patientId != null)
        {
            result = result.Where(a => a.PatientId == patientId);
        }
        if (!string.IsNullOrWhiteSpace(doctor))
        {
            string search = doctor.Trim();
            result = result.Where(a => a.DoctorName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }
        if (date != null)
        {
            result = result.Where(a => a.StartTime.Date == date.Value.Date);
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            string wanted = status.Trim().ToLower();
            if (!Statuses.Contains(wanted))
            {
                return BadRequest("Статус должен быть: scheduled, completed или cancelled");
            }
            result = result.Where(a => a.Status == wanted);
        }

        return result.OrderBy(a => a.StartTime).ToList();
    }

    [HttpGet("{id}")]
    [ProducesResponseType<Appointment>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Appointment> GetById(int id)
    {
        Appointment? appointment = Appointments.FirstOrDefault(a => a.Id == id);

        if (appointment == null)
        {
            return NotFound();
        }

        return appointment;
    }

    // Новая запись всегда начинается со статуса scheduled, что бы ни прислали в теле
    [HttpPost]
    [ProducesResponseType<Appointment>(StatusCodes.Status201Created)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<string>(StatusCodes.Status409Conflict)]
    public ActionResult<Appointment> Create(Appointment appointment)
    {
        Normalize(appointment);
        appointment.Status = "scheduled";

        string? error = Validate(appointment, true);
        if (error != null)
        {
            return BadRequest(error);
        }

        string? conflict = FindConflict(appointment, null);
        if (conflict != null)
        {
            return Conflict(conflict);
        }

        appointment.Id = NextId++;
        appointment.CreatedAt = DateTime.Now;
        Appointments.Add(appointment);

        return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
    }

    // Через PUT же переносят приём на другое время и меняют статус (например, на cancelled)
    [HttpPut("{id}")]
    [ProducesResponseType<Appointment>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<string>(StatusCodes.Status409Conflict)]
    public ActionResult<Appointment> Update(int id, Appointment appointment)
    {
        Appointment? existing = Appointments.FirstOrDefault(a => a.Id == id);
        if (existing == null)
        {
            return NotFound();
        }

        if (existing.Status == "completed")
        {
            return Conflict("Приём уже состоялся, изменить запись нельзя");
        }

        Normalize(appointment);

        // Время в будущем проверяем, только если приём действительно переносят
        bool timeChanged = appointment.StartTime != existing.StartTime;

        string? error = Validate(appointment, timeChanged);
        if (error != null)
        {
            return BadRequest(error);
        }

        string? conflict = FindConflict(appointment, id);
        if (conflict != null)
        {
            return Conflict(conflict);
        }

        existing.PatientId = appointment.PatientId;
        existing.DoctorName = appointment.DoctorName;
        existing.Specialty = appointment.Specialty;
        existing.StartTime = appointment.StartTime;
        existing.DurationMinutes = appointment.DurationMinutes;
        existing.Reason = appointment.Reason;
        existing.Status = appointment.Status;

        return Ok(existing);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult Delete(int id)
    {
        Appointment? appointment = Appointments.FirstOrDefault(a => a.Id == id);

        if (appointment == null)
        {
            return NotFound();
        }

        Appointments.Remove(appointment);
        return NoContent();
    }

    private static void Normalize(Appointment appointment)
    {
        appointment.DoctorName = Strings.Clean(appointment.DoctorName);
        appointment.Specialty = Strings.Clean(appointment.Specialty);
        appointment.Reason = Strings.Clean(appointment.Reason);
        appointment.Status = Strings.Clean(appointment.Status).ToLower();
        if (appointment.Status == "")
        {
            appointment.Status = "scheduled";
        }
    }

    // Проверка данных (400). timeChanged: true, если время приёма новое (POST или перенос через PUT)
    private static string? Validate(Appointment appointment, bool timeChanged)
    {
        if (appointment.PatientId <= 0)
        {
            return "Укажите patientId: номер пациента из картотеки";
        }
        if (!PatientsController.Patients.Any(p => p.Id == appointment.PatientId))
        {
            return $"Пациент с id {appointment.PatientId} не найден. Сначала создайте пациента (POST /api/patients)";
        }

        if (appointment.DoctorName == "")
        {
            return "Укажите ФИО врача";
        }
        if (appointment.DoctorName.Length < 3 || appointment.DoctorName.Length > 100)
        {
            return "ФИО врача должно быть длиной от 3 до 100 символов";
        }

        if (appointment.Specialty == "")
        {
            return "Укажите специальность врача, например: терапевт";
        }
        if (appointment.Specialty.Length > 50)
        {
            return "Специальность слишком длинная: не больше 50 символов";
        }

        if (appointment.Reason == "")
        {
            return "Укажите причину обращения";
        }
        if (appointment.Reason.Length > 300)
        {
            return "Причина обращения слишком длинная: не больше 300 символов";
        }

        if (appointment.DurationMinutes < 10 || appointment.DurationMinutes > 120)
        {
            return "Длительность приёма должна быть от 10 до 120 минут";
        }

        if (!Statuses.Contains(appointment.Status))
        {
            return "Статус должен быть: scheduled (запланирован), completed (состоялся) или cancelled (отменён)";
        }

        if (appointment.StartTime.Year < 2000)
        {
            return "Укажите время приёма в формате ГГГГ-ММ-ДДTЧЧ:ММ, например 2026-11-10T10:00";
        }
        if (appointment.StartTime.DayOfWeek == DayOfWeek.Sunday)
        {
            return "В воскресенье клиника не работает. Выберите другой день";
        }

        TimeSpan startOfDay = appointment.StartTime.TimeOfDay;
        TimeSpan endOfDay = startOfDay + TimeSpan.FromMinutes(appointment.DurationMinutes);
        if (startOfDay < OpenTime || endOfDay > CloseTime)
        {
            return "Клиника работает с 08:00 до 20:00. Приём должен начаться и закончиться в это время";
        }

        if (appointment.Status == "scheduled" && timeChanged)
        {
            if (appointment.StartTime <= DateTime.Now)
            {
                return "Время приёма должно быть в будущем";
            }
            if (appointment.StartTime > DateTime.Now.AddYears(1))
            {
                return "Записаться можно не дальше чем на год вперёд";
            }
        }

        if (appointment.Status == "completed" && appointment.StartTime > DateTime.Now)
        {
            return "Нельзя отметить приём состоявшимся: он ещё не начался";
        }

        return null;
    }

    // Пересечения по времени (409 Conflict): данные верные, но время уже занято.
    // Врач не может принимать двоих сразу, а пациент не может быть у двух врачей одновременно.
    // Отменённые приёмы время не занимают.
    private static string? FindConflict(Appointment appointment, int? selfId)
    {
        if (appointment.Status == "cancelled")
        {
            return null;
        }

        DateTime start = appointment.StartTime;
        DateTime end = appointment.StartTime.AddMinutes(appointment.DurationMinutes);

        foreach (Appointment other in Appointments)
        {
            if (other.Id == selfId || other.Status == "cancelled")
            {
                continue;
            }

            bool overlaps = start < other.EndTime && other.StartTime < end;
            if (!overlaps)
            {
                continue;
            }

            if (string.Equals(other.DoctorName, appointment.DoctorName, StringComparison.OrdinalIgnoreCase))
            {
                return $"Врач {other.DoctorName} занят с {other.StartTime:HH:mm} до {other.EndTime:HH:mm}. Выберите другое время";
            }
            if (other.PatientId == appointment.PatientId)
            {
                return $"У пациента уже есть приём {other.StartTime:dd.MM.yyyy} с {other.StartTime:HH:mm} до {other.EndTime:HH:mm}";
            }
        }

        return null;
    }
}
