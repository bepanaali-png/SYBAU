using MedicalApi.Models;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    // Список публичный: к нему обращаются другие контроллеры (справки, приёмы, статистика)
    public static List<Patient> Patients = new List<Patient>();

    // Счётчик Id. Только растёт, поэтому после удаления Id никогда не повторяются
    internal static int NextId = 1;

    // GET /api/patients  или  GET /api/patients?name=али  (поиск по части имени)
    [HttpGet]
    [ProducesResponseType<List<Patient>>(StatusCodes.Status200OK)]
    public List<Patient> GetAll(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Patients;
        }

        string search = name.Trim();
        return Patients
            .Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    [HttpGet("{id}")]
    [ProducesResponseType<Patient>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Patient> GetById(int id)
    {
        Patient? patient = Patients.FirstOrDefault(p => p.Id == id);

        if (patient == null)
        {
            return NotFound();
        }

        return patient;
    }

    [HttpPost]
    [ProducesResponseType<Patient>(StatusCodes.Status201Created)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public ActionResult<Patient> Create(Patient patient)
    {
        Normalize(patient);

        string? error = Validate(patient, null);
        if (error != null)
        {
            return BadRequest(error);
        }

        patient.Id = NextId++;
        patient.RegisteredAt = DateTime.Now;
        Patients.Add(patient);

        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
    }

    [HttpPut("{id}")]
    [ProducesResponseType<Patient>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Patient> Update(int id, Patient patient)
    {
        Patient? existing = Patients.FirstOrDefault(p => p.Id == id);
        if (existing == null)
        {
            return NotFound();
        }

        Normalize(patient);

        string? error = Validate(patient, id);
        if (error != null)
        {
            return BadRequest(error);
        }

        // Переписываем все поля, кроме Id и RegisteredAt
        existing.Name = patient.Name;
        existing.BirthDate = patient.BirthDate;
        existing.Gender = patient.Gender;
        existing.Phone = patient.Phone;
        existing.Address = patient.Address;
        existing.InsuranceNumber = patient.InsuranceNumber;
        existing.HasAllergies = patient.HasAllergies;

        return Ok(existing);
    }

    // Что со справками удалённого пациента? Решение: просто так не удаляем.
    // Если у пациента есть справки или записи на приём, отвечаем 409 Conflict.
    // Удалить всё вместе можно явно: DELETE /api/patients/5?cascade=true
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<string>(StatusCodes.Status409Conflict)]
    public ActionResult Delete(int id, bool cascade = false)
    {
        Patient? patient = Patients.FirstOrDefault(p => p.Id == id);
        if (patient == null)
        {
            return NotFound();
        }

        int spravkiCount = SpravkiController.Spravki.Count(s => s.PatientId == id);
        int appointmentsCount = AppointmentsController.Appointments.Count(a => a.PatientId == id);

        if ((spravkiCount > 0 || appointmentsCount > 0) && !cascade)
        {
            return Conflict($"У пациента есть справок: {spravkiCount}, записей на приём: {appointmentsCount}. " +
                            "Чтобы удалить пациента вместе с ними, повторите запрос с параметром cascade=true");
        }

        SpravkiController.Spravki.RemoveAll(s => s.PatientId == id);
        AppointmentsController.Appointments.RemoveAll(a => a.PatientId == id);
        Patients.Remove(patient);

        return NoContent();
    }

    // Справки пациента для его карточки на фронтенде
    [HttpGet("{id}/spravki")]
    [ProducesResponseType<List<Spravka>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<List<Spravka>> GetSpravki(int id)
    {
        if (!Patients.Any(p => p.Id == id))
        {
            return NotFound();
        }

        return SpravkiController.Spravki.Where(s => s.PatientId == id).ToList();
    }

    // Записи пациента на приём
    [HttpGet("{id}/appointments")]
    [ProducesResponseType<List<Appointment>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<List<Appointment>> GetAppointments(int id)
    {
        if (!Patients.Any(p => p.Id == id))
        {
            return NotFound();
        }

        return AppointmentsController.Appointments
            .Where(a => a.PatientId == id)
            .OrderBy(a => a.StartTime)
            .ToList();
    }

    // Убираем пробелы по краям и null, чтобы "  Алиса " и "Алиса" считались одинаково
    private static void Normalize(Patient patient)
    {
        patient.Name = Strings.Clean(patient.Name);
        patient.Gender = Strings.Clean(patient.Gender).ToLower();
        patient.Phone = Strings.Clean(patient.Phone);
        patient.Address = Strings.Clean(patient.Address);
        patient.InsuranceNumber = Strings.Clean(patient.InsuranceNumber);
    }

    // Общие проверки для POST и PUT. Вернёт текст ошибки или null, если всё хорошо.
    // selfId: при PUT это Id самого пациента, чтобы он не считался дубликатом самого себя.
    private static string? Validate(Patient patient, int? selfId)
    {
        if (patient.Name == "")
        {
            return "Укажите ФИО пациента";
        }
        if (patient.Name.Length < 2 || patient.Name.Length > 100)
        {
            return "ФИО должно быть длиной от 2 до 100 символов";
        }

        if (patient.BirthDate.Year < 1900)
        {
            return "Укажите дату рождения в формате ГГГГ-ММ-ДД, например 2000-05-15";
        }
        if (patient.BirthDate > DateTime.Now)
        {
            return "Дата рождения не может быть в будущем";
        }

        if (patient.Gender != "" && patient.Gender != "male" && patient.Gender != "female")
        {
            return "Пол должен быть male или female (или оставьте поле пустым)";
        }

        if (patient.Phone != "")
        {
            bool onlyAllowedChars = patient.Phone.All(c => char.IsDigit(c) || c == '+' || c == ' ' || c == '-' || c == '(' || c == ')');
            int digits = patient.Phone.Count(char.IsDigit);
            if (!onlyAllowedChars || digits < 10 || digits > 15)
            {
                return "Телефон должен состоять из 10-15 цифр, можно с плюсом, пробелами и скобками. Пример: +996 555 123 456";
            }
        }

        if (patient.Address.Length > 200)
        {
            return "Адрес слишком длинный: не больше 200 символов";
        }
        if (patient.InsuranceNumber.Length > 20)
        {
            return "Номер полиса слишком длинный: не больше 20 символов";
        }

        // Алиса и алиса с одной датой рождения - один и тот же человек
        Patient? duplicate = Patients.FirstOrDefault(p =>
            p.Id != selfId &&
            p.BirthDate.Date == patient.BirthDate.Date &&
            string.Equals(p.Name, patient.Name, StringComparison.OrdinalIgnoreCase));
        if (duplicate != null)
        {
            return $"Пациент «{patient.Name}» с датой рождения {patient.BirthDate:dd.MM.yyyy} уже есть в картотеке (id {duplicate.Id})";
        }

        return null;
    }
}
