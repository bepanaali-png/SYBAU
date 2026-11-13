namespace MedicalApi.Models;

public class Patient
{
    public int Id { get; set; }

    // ФИО
    public string Name { get; set; } = "";

    public DateTime BirthDate { get; set; }

    // "male" или "female" (можно оставить пустым)
    public string Gender { get; set; } = "";

    public string Phone { get; set; } = "";

    public string Address { get; set; } = "";

    // Номер страхового полиса
    public string InsuranceNumber { get; set; } = "";

    public bool HasAllergies { get; set; }

    // Когда пациента завели в картотеку. Ставит сервер, из запроса не берётся
    public DateTime RegisteredAt { get; set; }

    // Возраст считается сам из даты рождения. Фронтенду удобно: сразу видно, ребёнок или взрослый
    public int Age
    {
        get
        {
            DateTime today = DateTime.Today;
            int age = today.Year - BirthDate.Year;
            if (BirthDate.Date > today.AddYears(-age))
            {
                age--;
            }
            return age;
        }
    }
}
