namespace MedicalApi.Models;

public class Spravka
{
    public int Id { get; set; }

    // Название и цена копируются из каталога видов справок в момент выписки.
    // Если потом цена в каталоге изменится, старые справки останутся с той ценой, за которую их продали.
    public string Name { get; set; } = "";

    public decimal Price { get; set; }

    // Кому выписана справка (пациент из картотеки)
    public int PatientId { get; set; }

    // Какой вид справки (из каталога)
    public int SpravkaTypeId { get; set; }

    // Когда выписана и до какого числа действует. Считает сервер
    public DateTime IssuedAt { get; set; }

    public DateTime ValidUntil { get; set; }
}
