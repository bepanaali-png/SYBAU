namespace MedicalApi.Models;

public class SpravkaType
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public decimal Price { get; set; }

    // Сколько дней справка действует
    public int ValidDays { get; set; } = 30;

    // Нужен ли осмотр врача
    public bool NeedsDoctorExam { get; set; }

    // Для кого: "adults" (взрослые), "children" (дети до 18 лет) или "all" (все)
    public string Audience { get; set; } = "all";
}
