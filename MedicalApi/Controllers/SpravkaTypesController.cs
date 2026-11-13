using MedicalApi.Models;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class SpravkaTypesController : ControllerBase
{
    // Каталог заполнен прямо в коде, поэтому после перезапуска сервера виды справок на месте
    public static List<SpravkaType> Types = new List<SpravkaType>
    {
        new SpravkaType { Id = 1, Name = "Справка 083", Price = 100, ValidDays = 180, NeedsDoctorExam = true, Audience = "all" },
        new SpravkaType { Id = 2, Name = "Санитарная книжка", Price = 750, ValidDays = 365, NeedsDoctorExam = true, Audience = "adults" },
        new SpravkaType { Id = 3, Name = "Справка в бассейн", Price = 300, ValidDays = 14, NeedsDoctorExam = true, Audience = "all" },
        new SpravkaType { Id = 4, Name = "Справка в детский лагерь", Price = 500, ValidDays = 30, NeedsDoctorExam = true, Audience = "children" },
        new SpravkaType { Id = 5, Name = "Справка для водительских прав", Price = 1200, ValidDays = 365, NeedsDoctorExam = true, Audience = "adults" },
        new SpravkaType { Id = 6, Name = "Справка в спортивную секцию", Price = 400, ValidDays = 90, NeedsDoctorExam = false, Audience = "all" },
    };

    // Следующий Id идёт после самого большого из начального списка (здесь это 7)
    internal static int NextId = Types.Max(t => t.Id) + 1;

    private static readonly string[] Audiences = { "all", "adults", "children" };

    [HttpGet]
    [ProducesResponseType<List<SpravkaType>>(StatusCodes.Status200OK)]
    public List<SpravkaType> GetAll()
    {
        return Types;
    }

    [HttpGet("{id}")]
    [ProducesResponseType<SpravkaType>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<SpravkaType> GetById(int id)
    {
        SpravkaType? type = Types.FirstOrDefault(t => t.Id == id);

        if (type == null)
        {
            return NotFound();
        }

        return type;
    }

    [HttpPost]
    [ProducesResponseType<SpravkaType>(StatusCodes.Status201Created)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    public ActionResult<SpravkaType> Create(SpravkaType type)
    {
        Normalize(type);

        string? error = Validate(type, null);
        if (error != null)
        {
            return BadRequest(error);
        }

        type.Id = NextId++;
        Types.Add(type);

        return CreatedAtAction(nameof(GetById), new { id = type.Id }, type);
    }

    // Цена в каталоге меняется только для новых справок. Старые остаются с той ценой, за которую их выписали.
    [HttpPut("{id}")]
    [ProducesResponseType<SpravkaType>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<SpravkaType> Update(int id, SpravkaType type)
    {
        SpravkaType? existing = Types.FirstOrDefault(t => t.Id == id);
        if (existing == null)
        {
            return NotFound();
        }

        Normalize(type);

        string? error = Validate(type, id);
        if (error != null)
        {
            return BadRequest(error);
        }

        existing.Name = type.Name;
        existing.Price = type.Price;
        existing.ValidDays = type.ValidDays;
        existing.NeedsDoctorExam = type.NeedsDoctorExam;
        existing.Audience = type.Audience;

        return Ok(existing);
    }

    // Вид, по которому уже выписаны справки, удалить нельзя: 409 Conflict
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<string>(StatusCodes.Status409Conflict)]
    public ActionResult Delete(int id)
    {
        SpravkaType? type = Types.FirstOrDefault(t => t.Id == id);
        if (type == null)
        {
            return NotFound();
        }

        int used = SpravkiController.Spravki.Count(s => s.SpravkaTypeId == id);
        if (used > 0)
        {
            return Conflict($"Нельзя удалить вид справки: по нему уже выписано справок: {used}");
        }

        Types.Remove(type);
        return NoContent();
    }

    private static void Normalize(SpravkaType type)
    {
        type.Name = Strings.Clean(type.Name);
        type.Audience = Strings.Clean(type.Audience).ToLower();
        if (type.Audience == "")
        {
            type.Audience = "all";
        }
    }

    private static string? Validate(SpravkaType type, int? selfId)
    {
        if (type.Name == "")
        {
            return "Укажите название вида справки";
        }
        if (type.Name.Length < 3 || type.Name.Length > 100)
        {
            return "Название должно быть длиной от 3 до 100 символов";
        }

        if (type.Price <= 0)
        {
            return "Цена должна быть больше 0";
        }
        if (type.Price > 1000000)
        {
            return "Цена не может быть больше 1 000 000 тенге";
        }
        if (decimal.Round(type.Price, 2) != type.Price)
        {
            return "В цене не больше двух знаков после запятой";
        }

        if (type.ValidDays < 1 || type.ValidDays > 3650)
        {
            return "Срок действия справки должен быть от 1 до 3650 дней";
        }

        if (!Audiences.Contains(type.Audience))
        {
            return "Для кого справка: укажите all (все), adults (взрослые) или children (дети)";
        }

        SpravkaType? duplicate = Types.FirstOrDefault(t =>
            t.Id != selfId &&
            string.Equals(t.Name, type.Name, StringComparison.OrdinalIgnoreCase));
        if (duplicate != null)
        {
            return $"Вид справки «{type.Name}» уже есть в каталоге (id {duplicate.Id})";
        }

        return null;
    }
}
