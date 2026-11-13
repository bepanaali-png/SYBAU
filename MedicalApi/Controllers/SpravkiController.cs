using MedicalApi.Models;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class SpravkiController : ControllerBase
{
    // Список публичный: к нему обращаются контроллеры пациентов, видов справок и статистики
    public static List<Spravka> Spravki = new List<Spravka>();

    // БАГ №1 был здесь: раньше Id считался как _spravki.Count + 1.
    // Создали три справки, удалили вторую, создали ещё одну: Count + 1 = 3, и Id 3 уже занят.
    // Теперь Id берётся из счётчика, который только растёт и после удаления не откатывается.
    internal static int NextId = 1;

    // Примеры:
    //   GET /api/spravki
    //   GET /api/spravki?patientId=3                  только справки пациента 3
    //   GET /api/spravki?spravkaTypeId=2              только санитарные книжки
    //   GET /api/spravki?sortBy=price&desc=true       сначала самые дорогие (sortBy: price, date, name)
    [HttpGet]
    [ProducesResponseType<List<Spravka>>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public ActionResult<List<Spravka>> GetAll(int? patientId, int? spravkaTypeId, string? sortBy, bool desc = false)
    {
        IEnumerable<Spravka> result = Spravki;

        if (patientId != null)
        {
            result = result.Where(s => s.PatientId == patientId);
        }
        if (spravkaTypeId != null)
        {
            result = result.Where(s => s.SpravkaTypeId == spravkaTypeId);
        }

        switch ((sortBy ?? "").ToLower())
        {
            case "":
                break;
            case "price":
                result = desc ? result.OrderByDescending(s => s.Price) : result.OrderBy(s => s.Price);
                break;
            case "date":
                result = desc ? result.OrderByDescending(s => s.IssuedAt) : result.OrderBy(s => s.IssuedAt);
                break;
            case "name":
                result = desc ? result.OrderByDescending(s => s.Name) : result.OrderBy(s => s.Name);
                break;
            default:
                return BadRequest("Сортировать можно по: price (цена), date (дата выписки) или name (название)");
        }

        return result.ToList();
    }

    [HttpGet("{id}")]
    [ProducesResponseType<Spravka>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Spravka> GetById(int id)
    {
        Spravka? spravka = Spravki.FirstOrDefault(s => s.Id == id);

        if (spravka == null)
        {
            return NotFound();
        }

        return spravka;
    }

    // В теле нужны только patientId и spravkaTypeId.
    // Название и цену сервер берёт из каталога, поэтому присланные name и price игнорируются.
    [HttpPost]
    [ProducesResponseType<Spravka>(StatusCodes.Status201Created)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public ActionResult<Spravka> Create(Spravka spravka)
    {
        if (!TryResolve(spravka, out _, out SpravkaType type, out string error))
        {
            return BadRequest(error);
        }

        spravka.Id = NextId++;
        spravka.Name = type.Name;
        spravka.Price = type.Price;
        spravka.IssuedAt = DateTime.Now;
        spravka.ValidUntil = spravka.IssuedAt.AddDays(type.ValidDays);
        Spravki.Add(spravka);

        return CreatedAtAction(nameof(GetById), new { id = spravka.Id }, spravka);
    }

    [HttpPut("{id}")]
    [ProducesResponseType<Spravka>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Spravka> Update(int id, Spravka spravka)
    {
        Spravka? existing = Spravki.FirstOrDefault(s => s.Id == id);
        if (existing == null)
        {
            return NotFound();
        }

        if (!TryResolve(spravka, out _, out SpravkaType type, out string error))
        {
            return BadRequest(error);
        }

        // Вид справки поменяли: название, цена и срок берутся из нового вида.
        // Вид тот же: оставляем цену, по которой справку выписали.
        if (existing.SpravkaTypeId != spravka.SpravkaTypeId)
        {
            existing.Name = type.Name;
            existing.Price = type.Price;
            existing.ValidUntil = existing.IssuedAt.AddDays(type.ValidDays);
        }

        existing.PatientId = spravka.PatientId;
        existing.SpravkaTypeId = spravka.SpravkaTypeId;

        return Ok(existing);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult Delete(int id)
    {
        Spravka? spravka = Spravki.FirstOrDefault(s => s.Id == id);

        if (spravka == null)
        {
            return NotFound();
        }

        Spravki.Remove(spravka);
        return NoContent();
    }

    // Общие проверки для POST и PUT: пациент и вид справки должны существовать,
    // а вид должен подходить пациенту по возрасту. Если всё хорошо, отдаёт найденные объекты.
    private static bool TryResolve(Spravka spravka, out Patient patient, out SpravkaType type, out string error)
    {
        patient = null!;
        type = null!;
        error = "";

        if (spravka.PatientId <= 0)
        {
            error = "Укажите patientId: номер пациента из картотеки";
            return false;
        }
        if (spravka.SpravkaTypeId <= 0)
        {
            error = "Укажите spravkaTypeId: номер вида справки (список видов: GET /api/SpravkaTypes)";
            return false;
        }

        Patient? foundPatient = PatientsController.Patients.FirstOrDefault(p => p.Id == spravka.PatientId);
        if (foundPatient == null)
        {
            error = $"Пациент с id {spravka.PatientId} не найден. Сначала создайте пациента (POST /api/patients)";
            return false;
        }

        SpravkaType? foundType = SpravkaTypesController.Types.FirstOrDefault(t => t.Id == spravka.SpravkaTypeId);
        if (foundType == null)
        {
            error = $"Вид справки с id {spravka.SpravkaTypeId} не найден. Посмотрите список: GET /api/SpravkaTypes";
            return false;
        }

        if (foundType.Audience == "children" && foundPatient.Age >= 18)
        {
            error = $"«{foundType.Name}» выдаётся только детям до 18 лет, а пациенту {foundPatient.Age}";
            return false;
        }
        if (foundType.Audience == "adults" && foundPatient.Age < 18)
        {
            error = $"«{foundType.Name}» выдаётся только взрослым с 18 лет, а пациенту {foundPatient.Age}";
            return false;
        }

        patient = foundPatient;
        type = foundType;
        return true;
    }
}
